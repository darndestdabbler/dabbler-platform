using System.Data;
using Dabbler.Components.Data.Metadata;
using Dabbler.Platform.Stubs;
using MudBlazor;

namespace AcmeCoffeeOnline.Web.StubMode;

public static class PlatformStubModeRegistration
{
    public const string CustomerSummaryQuery =
        "SELECT CustomerName, City, Tier FROM StubCustomerSummary ORDER BY CustomerName";

    public const string CustomerSummaryRdf = """
{
  "metadata": {
    "name": "Stub Customer Summary",
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
      "connectionName": "Stub",
      "providerType": "Custom",
      "query": "SELECT CustomerName, City, Tier FROM StubCustomerSummary ORDER BY CustomerName"
    }
  ],
  "fields": [
    { "name": "CustomerName", "dataType": "String", "sourceColumn": "CustomerName" },
    { "name": "City", "dataType": "String", "sourceColumn": "City" },
    { "name": "Tier", "dataType": "String", "sourceColumn": "Tier" }
  ],
  "bands": [
    {
      "name": "reportHeader",
      "type": "ReportHeader",
      "height": 0.55,
      "elements": [
        {
          "type": "StaticText",
          "x": 0,
          "y": 0,
          "width": 4.0,
          "height": 0.3,
          "value": "Stub Customer Summary",
          "styleName": "reportTitle"
        }
      ]
    },
    {
      "name": "detail",
      "type": "Detail",
      "height": 0.24,
      "elements": [
        {
          "type": "TextField",
          "x": 0,
          "y": 0,
          "width": 2.4,
          "height": 0.2,
          "fieldName": "CustomerName",
          "styleName": "detailText"
        },
        {
          "type": "TextField",
          "x": 2.5,
          "y": 0,
          "width": 1.6,
          "height": 0.2,
          "fieldName": "City",
          "styleName": "detailText"
        },
        {
          "type": "TextField",
          "x": 4.2,
          "y": 0,
          "width": 1.2,
          "height": 0.2,
          "fieldName": "Tier",
          "styleName": "detailText"
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
    }
  ]
}
""";

    public static IServiceCollection AddPlatformStubMode(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var customers = BuildCustomers();
        var definition = BuildDefinition();
        var reportProvider = new StubReportDataProvider()
            .AddQueryResult(CustomerSummaryQuery, BuildCustomerSummaryTable(customers));

        services.AddSingleton(definition);
        services.AddSingleton(new StubEntityMetadataProvider(definition.Entities));
        services.AddSingleton(new StubNavigationDataProvider(definition.Navigation));
        services.AddSingleton(new StubGridDataProvider<StubCustomerRow>(customers));
        services.AddSingleton(reportProvider);

        return services;
    }

    private static IReadOnlyList<StubCustomerRow> BuildCustomers() =>
    [
        new(101, "Ada Lovelace", "Boston", "Priority", "northwind"),
        new(102, "Grace Hopper", "Chicago", "Standard", "northwind"),
        new(103, "Katherine Johnson", "Atlanta", "Priority", "orbit")
    ];

    private static StubPlatformDefinition BuildDefinition()
    {
        var customerMetadata = new EntityMetadata
        {
            EntityName = "Customer",
            DisplayName = "Customers",
            DisplayNameSingular = "Customer",
            ShowInNavigation = true,
            NavigationOrder = 0,
            SmartFilterTopN = 5,
            SmartFilters =
            [
                new SmartFilterDefinition
                {
                    Name = "Recent Customers",
                    Category = SmartFilterCategory.Custom,
                    DisplayOrder = 0,
                    IsEnabled = true,
                    FilterExpression = "true"
                }
            ]
        };

        var navigation = new StubNavigationDataset()
            .AddRecords(
                "Customer",
                "Recent Customers",
                [
                    new NavigationRecord(101, "Ada Lovelace"),
                    new NavigationRecord(102, "Grace Hopper")
                ]);

        return new StubPlatformDefinition
        {
            Entities = [customerMetadata],
            Navigation = navigation,
            ReportItems =
            [
                new NavigationReportItem(
                    "CustomerSummary",
                    "Stub Customer Summary",
                    Icons.Material.Filled.Assessment)
            ],
            HomePageConfiguration = new HomePageConfiguration
            {
                DefaultHomePage = "Form:Customer"
            }
        };
    }

    private static DataTable BuildCustomerSummaryTable(IEnumerable<StubCustomerRow> customers)
    {
        var table = new DataTable();
        table.Columns.Add("CustomerName", typeof(string));
        table.Columns.Add("City", typeof(string));
        table.Columns.Add("Tier", typeof(string));

        foreach (var customer in customers)
        {
            table.Rows.Add(customer.Name, customer.City, customer.Tier);
        }

        return table;
    }
}
