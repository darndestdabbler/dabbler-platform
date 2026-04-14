using System.Data;
using Bunit;
using Dabbler.Components;
using Dabbler.Reports.Blazor.Tests.TestHelpers;
using Dabbler.Reports.Core.Data;
using FluentAssertions;
using Xunit;

namespace Dabbler.Reports.Blazor.Tests;

public class DabblerReportPageTests
{
    [Fact]
    public void AutoRun_CoreReportWorkflowLoadsRdfExecutesQueryAndComposesPages()
    {
        using var ctx = new TestContext().SetupForDabblerReports();

        var provider = new RecordingReportDataProvider();
        var initialParameters = new Dictionary<string, object?>
        {
            ["StartDate"] = new DateTime(2026, 1, 1),
            ["EndDate"] = new DateTime(2026, 1, 31)
        };

        var cut = ctx.RenderComponent<DabblerReportPage>(parameters => parameters
            .Add(x => x.RdfPath, "reports/test-sales.rdf.json")
            .Add(x => x.RdfLoader, _ => Task.FromResult(TestSalesReportRdf))
            .Add(x => x.DataProvider, provider)
            .Add(x => x.InitialParameterValues, initialParameters)
            .Add(x => x.AutoRunOnLoad, true));

        cut.WaitForAssertion(() =>
        {
            provider.QueryCalls.Should().HaveCount(1);
            cut.Instance.HasRun.Should().BeTrue();
            cut.Instance.Pages.Should().NotBeNull();
            cut.Instance.Pages!.Should().NotBeEmpty();
            cut.Markup.Should().Contain("Test Sales Summary");
        });

        provider.LastParameters.Should().ContainKey("StartDate");
        provider.LastParameters.Should().ContainKey("EndDate");
    }

    private sealed class RecordingReportDataProvider : IReportDataProvider
    {
        public List<string> QueryCalls { get; } = [];

        public IReadOnlyDictionary<string, object?> LastParameters { get; private set; } =
            new Dictionary<string, object?>();

        public Task<DataTable> ExecuteQueryAsync(
            string query,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            QueryCalls.Add(query);
            LastParameters = new Dictionary<string, object?>(parameters);

            var table = new DataTable();
            table.Columns.Add("ProductName", typeof(string));
            table.Columns.Add("Quantity", typeof(int));
            table.Columns.Add("LineTotal", typeof(decimal));
            table.Rows.Add("House Blend", 3, 27.50m);
            return Task.FromResult(table);
        }

        public Task<DataTable> ExecuteLookupAsync(
            string query,
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DataTable());
        }
    }

    private const string TestSalesReportRdf = """
{
  "metadata": {
    "name": "Test Sales Summary",
    "pageWidth": 8.5,
    "pageHeight": 11.0,
    "marginTop": 0.5,
    "marginBottom": 0.5,
    "marginLeft": 0.5,
    "marginRight": 0.5,
    "defaultFontFamily": "Segoe UI",
    "defaultFontSize": 10
  },
  "dataSources": [
    {
      "name": "main",
      "connectionName": "Test",
      "providerType": "Custom",
      "query": "SELECT ProductName, Quantity, LineTotal FROM Sales WHERE OrderDate BETWEEN @StartDate AND @EndDate"
    }
  ],
  "parameters": [
    {
      "name": "StartDate",
      "dataType": "DateTime",
      "controlType": "DatePicker",
      "promptText": "Start Date",
      "isRequired": true,
      "sortOrder": 1
    },
    {
      "name": "EndDate",
      "dataType": "DateTime",
      "controlType": "DatePicker",
      "promptText": "End Date",
      "isRequired": true,
      "sortOrder": 2
    }
  ],
  "fields": [
    { "name": "ProductName", "dataType": "String", "sourceColumn": "ProductName" },
    { "name": "Quantity", "dataType": "Int32", "sourceColumn": "Quantity" },
    { "name": "LineTotal", "dataType": "Decimal", "sourceColumn": "LineTotal", "formatString": "C2" }
  ],
  "bands": [
    {
      "name": "reportHeader",
      "type": "ReportHeader",
      "height": 0.6,
      "elements": [
        {
          "type": "StaticText",
          "x": 0,
          "y": 0,
          "width": 3.5,
          "height": 0.3,
          "value": "Test Sales Summary",
          "styleName": "reportTitle"
        }
      ]
    },
    {
      "name": "detail",
      "type": "Detail",
      "height": 0.25,
      "elements": [
        {
          "type": "TextField",
          "x": 0,
          "y": 0,
          "width": 2.5,
          "height": 0.2,
          "fieldName": "ProductName",
          "styleName": "detailText"
        },
        {
          "type": "TextField",
          "x": 2.6,
          "y": 0,
          "width": 1.0,
          "height": 0.2,
          "fieldName": "Quantity",
          "styleName": "detailNumber"
        },
        {
          "type": "TextField",
          "x": 3.7,
          "y": 0,
          "width": 1.2,
          "height": 0.2,
          "fieldName": "LineTotal",
          "styleName": "detailCurrency"
        }
      ]
    }
  ],
  "styles": [
    {
      "name": "reportTitle",
      "fontFamily": "Segoe UI",
      "fontSize": 16,
      "isBold": true
    },
    {
      "name": "detailText",
      "fontSize": 10
    },
    {
      "name": "detailNumber",
      "fontSize": 10,
      "horizontalAlignment": "Right"
    },
    {
      "name": "detailCurrency",
      "fontSize": 10,
      "horizontalAlignment": "Right"
    }
  ]
}
""";
}
