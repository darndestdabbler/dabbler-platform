using System.Data;
using Bunit;
using Dabbler.Platform.Stubs;
using Dabbler.Reports.Blazor.Tests.TestHelpers;
using Dabbler.Reports.Core.Data;
using Dabbler.Reports.Core.Rdf;
using FluentAssertions;
using MudBlazor;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class ParameterPromptFormTests
{
    [Fact]
    public async Task InitialValuesAndLookups_LoadStubbedChoicesAndConvertTypedRunValues()
    {
        using var ctx = new TestContext().SetupForDabblerReports();

        var provider = new RecordingReportDataProvider();
        provider.AddLookupResult("LOOKUP_REGIONS", CreateLookupTable(("North", "north"), ("South", "south")));
        provider.AddLookupResult("LOOKUP_CUSTOMERS", parameters =>
        {
            parameters.Should().ContainKey("Region").WhoseValue.Should().Be("north");
            return CreateLookupTable(("Ada Lovelace", "ada"), ("Grace Hopper", "grace"));
        });

        Dictionary<string, object?>? submittedValues = null;
        var initialValues = new Dictionary<string, object?>
        {
            ["Region"] = "north",
            ["Customer"] = "ada",
            ["StartDate"] = new DateTime(2026, 2, 1),
            ["IncludeInactive"] = true,
            ["MaxRows"] = 25m
        };

        var cut = ctx.RenderComponent<ParameterPromptForm>(parameters => parameters
            .Add(x => x.Parameters, BuildContractParameters())
            .Add(x => x.DataProvider, provider)
            .Add(x => x.InitialValues, initialValues)
            .Add(x => x.OnRunReport, values => submittedValues = values));

        cut.WaitForAssertion(() =>
        {
            provider.LookupCalls.Should().HaveCount(2);
            provider.LookupCalls[0].Query.Should().Be("LOOKUP_REGIONS");
            provider.LookupCalls[1].Query.Should().Be("LOOKUP_CUSTOMERS");
            provider.LookupCalls[1].Parameters.Should().ContainKey("Region").WhoseValue.Should().Be("north");
        });

        cut.Instance.GetCurrentValues().Should().Contain(new KeyValuePair<string, object?>("Customer", "ada"));
        cut.Markup.Should().Contain("aria-required=\"true\"");

        await cut.InvokeAsync(() => cut.FindComponent<MudForm>().Instance.ValidateAsync());
        cut.WaitForAssertion(() =>
            cut.FindAll("button").Single(button => button.TextContent.Contains("Run Report", StringComparison.Ordinal))
                .HasAttribute("disabled").Should().BeFalse());

        cut.FindAll("button").Single(button => button.TextContent.Contains("Run Report", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            submittedValues.Should().NotBeNull();
            submittedValues.Should().ContainKey("Region").WhoseValue.Should().Be("north");
            submittedValues.Should().ContainKey("Customer").WhoseValue.Should().Be("ada");
            submittedValues.Should().ContainKey("StartDate").WhoseValue.Should().BeOfType<DateTime>();
            submittedValues.Should().ContainKey("IncludeInactive").WhoseValue.Should().Be(true);
            submittedValues.Should().ContainKey("MaxRows").WhoseValue.Should().Be(25);
        });
    }

    [Fact]
    public async Task MissingRequiredValue_ShowsValidationMessageAndPreventsRun()
    {
        using var ctx = new TestContext().SetupForDabblerReports();

        Dictionary<string, object?>? submittedValues = null;
        var cut = ctx.RenderComponent<ParameterPromptForm>(parameters => parameters
            .Add(x => x.Parameters,
            [
                new ParameterDefinition
                {
                    Name = "AccountCode",
                    DataType = "String",
                    PromptText = "Account Code",
                    ControlType = ParameterControlType.TextBox,
                    IsRequired = true,
                    SortOrder = 1,
                    Validation = new ParameterValidation
                    {
                        Pattern = "^ACCT-\\d{3}$"
                    }
                }
            ])
            .Add(x => x.OnRunReport, values => submittedValues = values));

        await cut.InvokeAsync(() => cut.FindComponent<MudForm>().Instance.ValidateAsync());

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Account Code is required");
            cut.Markup.Should().Contain("aria-required=\"true\"");
            cut.Find("input").GetAttribute("aria-invalid").Should().Be("true");
        });

        cut.FindAll("button").Single(button => button.TextContent.Contains("Run Report", StringComparison.Ordinal)).Click();

        submittedValues.Should().BeNull();
    }

    private static IReadOnlyList<ParameterDefinition> BuildContractParameters() =>
    [
        new()
        {
            Name = "Region",
            DataType = "String",
            PromptText = "Region",
            ControlType = ParameterControlType.ComboBox,
            LookupQuery = "LOOKUP_REGIONS",
            LookupDisplayField = "Display",
            LookupValueField = "Value",
            IsRequired = true,
            SortOrder = 1
        },
        new()
        {
            Name = "Customer",
            DataType = "String",
            PromptText = "Customer",
            ControlType = ParameterControlType.ComboBox,
            LookupQuery = "LOOKUP_CUSTOMERS",
            LookupDisplayField = "Display",
            LookupValueField = "Value",
            DependsOn = "Region",
            IsRequired = true,
            SortOrder = 2
        },
        new()
        {
            Name = "StartDate",
            DataType = "DateTime",
            PromptText = "Start Date",
            ControlType = ParameterControlType.DatePicker,
            IsRequired = true,
            SortOrder = 3
        },
        new()
        {
            Name = "IncludeInactive",
            DataType = "Boolean",
            PromptText = "Include inactive customers",
            ControlType = ParameterControlType.CheckBox,
            IsRequired = false,
            SortOrder = 4
        },
        new()
        {
            Name = "MaxRows",
            DataType = "Int32",
            PromptText = "Max Rows",
            ControlType = ParameterControlType.NumericUpDown,
            IsRequired = true,
            SortOrder = 5,
            Validation = new ParameterValidation
            {
                MinValue = "1",
                MaxValue = "100"
            }
        }
    ];

    private static DataTable CreateLookupTable(params (string Display, string Value)[] items)
    {
        var table = new DataTable();
        table.Columns.Add("Display", typeof(string));
        table.Columns.Add("Value", typeof(string));

        foreach (var item in items)
        {
            table.Rows.Add(item.Display, item.Value);
        }

        return table;
    }

    private sealed class RecordingReportDataProvider : IReportDataProvider
    {
        private readonly Dictionary<string, Func<IReadOnlyDictionary<string, object?>, DataTable>> _lookupFactories =
            new(StringComparer.OrdinalIgnoreCase);

        public List<(string Query, IReadOnlyDictionary<string, object?> Parameters)> LookupCalls { get; } = [];

        public RecordingReportDataProvider AddLookupResult(string query, DataTable table)
        {
            _lookupFactories[query] = _ => table.Copy();
            return this;
        }

        public RecordingReportDataProvider AddLookupResult(
            string query,
            Func<IReadOnlyDictionary<string, object?>, DataTable> factory)
        {
            _lookupFactories[query] = factory;
            return this;
        }

        public Task<DataTable> ExecuteQueryAsync(
            string query,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DataTable());

        public Task<DataTable> ExecuteLookupAsync(
            string query,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            var callParameters = parameters is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase);

            LookupCalls.Add((query, callParameters));

            if (_lookupFactories.TryGetValue(query, out var factory))
            {
                return Task.FromResult(factory(callParameters));
            }

            return Task.FromResult(new DataTable());
        }
    }
}
