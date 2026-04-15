using Dabbler.Components.Data.Metadata;

namespace AcmeCoffeeOnline.Web.Data;

/// <summary>
/// Provides entity metadata for the sample distributor application's entities.
/// Drives navigation panel, query pages, detail forms, and child relationship tabs.
/// </summary>
public class AcmeCoffeeMetadataProvider : IEntityMetadataProvider
{
    private readonly Dictionary<string, EntityMetadata> _entities;

    public AcmeCoffeeMetadataProvider()
    {
        _entities = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Product"] = BuildProductMetadata(),
            ["Customer"] = BuildCustomerMetadata(),
            ["Order"] = BuildOrderMetadata(),
            ["OrderItem"] = BuildOrderItemMetadata(),
            ["OrderNote"] = BuildOrderNoteMetadata()
        };
    }

    public EntityMetadata? GetEntityMetadata(string entityName)
    {
        return _entities.TryGetValue(entityName, out var meta) ? meta : null;
    }

    public IReadOnlyList<EntityMetadata> GetAllEntities()
    {
        return _entities.Values.ToList();
    }

    public IReadOnlyList<EntityMetadata> GetNavigableEntities()
    {
        return _entities.Values
            .Where(e => e.ShowInNavigation)
            .OrderBy(e => e.NavigationOrder)
            .ToList();
    }

    private static EntityMetadata BuildProductMetadata() => new()
    {
        EntityName = "Product",
        DisplayName = "Products",
        DisplayNameSingular = "Product",
        RecordLabelExpression = "[Name]",
        IconName = "LocalCafe",
        ShowInNavigation = true,
        NavigationOrder = 1,
        SmartFilterTopN = 5,
        IsLeafEntity = true,
        PopupFieldThreshold = 20,
        Fields = new()
        {
            new() { FieldName = "Id", DisplayLabel = "ID", DataType = "int", IsPrimaryKey = true, IsReadOnly = true, DisplayOrder = 0 },
            new() { FieldName = "Name", DisplayLabel = "Name", DataType = "string", IsRequired = true, DisplayOrder = 1, DetailFormColumn = 0 },
            new() { FieldName = "RoastLevel", DisplayLabel = "Roast Level", DataType = "string", IsRequired = true, DisplayOrder = 2, DetailFormColumn = 0 },
            new() { FieldName = "CaffeineContent", DisplayLabel = "Caffeine", DataType = "string", IsRequired = true, DisplayOrder = 3, DetailFormColumn = 0 },
            new() { FieldName = "GrindType", DisplayLabel = "Grind Type", DataType = "string", IsRequired = true, DisplayOrder = 4, DetailFormColumn = 1 },
            new() { FieldName = "UnitPrice", DisplayLabel = "Unit Price", DataType = "decimal", FormatString = "C2", IsRequired = true, DisplayOrder = 5, DetailFormColumn = 1 },
            new() { FieldName = "IsAvailable", DisplayLabel = "Available", DataType = "bool", DisplayOrder = 6, DetailFormColumn = 1 },
            new() { FieldName = "Description", DisplayLabel = "Description", DataType = "string", DisplayOrder = 7, DetailFormColumn = 0 },
            new() { FieldName = "ImageUrl", DisplayLabel = "Image URL", DataType = "string", DisplayOrder = 8, DetailFormColumn = 1 },
            new() { FieldName = "CreatedDate", DisplayLabel = "Created", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 9 },
            new() { FieldName = "ModifiedDate", DisplayLabel = "Modified", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 10 },
        },
        StandaloneColumns = new() { "Name", "RoastLevel", "CaffeineContent", "GrindType", "UnitPrice", "IsAvailable" },
        ParentedColumns = new() { "Name", "RoastLevel", "CaffeineContent", "GrindType", "UnitPrice", "IsAvailable" },
        SearchColumns = new() { "Name", "RoastLevel", "CaffeineContent", "GrindType", "UnitPrice", "IsAvailable" },
        DetailFormFields = new() { "Id", "Name", "RoastLevel", "CaffeineContent", "GrindType", "UnitPrice", "IsAvailable", "Description", "ImageUrl", "CreatedDate", "ModifiedDate" },
        ChildRelationships = new(),
        SmartFilters = new()
        {
            new()
            {
                Name = "in stock",
                Category = SmartFilterCategory.Custom,
                FilterExpression = "IsAvailable == true",
                DisplayOrder = 1
            }
        }
    };

    private static EntityMetadata BuildCustomerMetadata() => new()
    {
        EntityName = "Customer",
        DisplayName = "Retail Businesses",
        DisplayNameSingular = "Retail Business",
        RecordLabelExpression = "[FirstName] + \" \" + [LastName]",
        IconName = "People",
        ShowInNavigation = true,
        NavigationOrder = 2,
        SmartFilterTopN = 5,
        IsLeafEntity = false,
        PopupFieldThreshold = 20,
        Fields = new()
        {
            new() { FieldName = "Id", DisplayLabel = "ID", DataType = "int", IsPrimaryKey = true, IsReadOnly = true, DisplayOrder = 0 },
            new() { FieldName = "FirstName", DisplayLabel = "Primary Contact First Name", DataType = "string", IsRequired = true, DisplayOrder = 1, DetailFormColumn = 0 },
            new() { FieldName = "LastName", DisplayLabel = "Primary Contact Last Name", DataType = "string", IsRequired = true, DisplayOrder = 2, DetailFormColumn = 0 },
            new() { FieldName = "Email", DisplayLabel = "Account Email", DataType = "string", IsRequired = true, DisplayOrder = 3, DetailFormColumn = 0 },
            new() { FieldName = "Phone", DisplayLabel = "Phone", DataType = "string", DisplayOrder = 4, DetailFormColumn = 1 },
            new() { FieldName = "Address", DisplayLabel = "Address", DataType = "string", DisplayOrder = 5, DetailFormColumn = 1 },
            new() { FieldName = "City", DisplayLabel = "City", DataType = "string", DisplayOrder = 6, DetailFormColumn = 1 },
            new() { FieldName = "State", DisplayLabel = "State", DataType = "string", DisplayOrder = 7, DetailFormColumn = 2 },
            new() { FieldName = "ZipCode", DisplayLabel = "Zip Code", DataType = "string", DisplayOrder = 8, DetailFormColumn = 2 },
            new() { FieldName = "IsActive", DisplayLabel = "Active", DataType = "bool", DisplayOrder = 9, DetailFormColumn = 2 },
            new() { FieldName = "CreatedDate", DisplayLabel = "Created", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 10 },
            new() { FieldName = "ModifiedDate", DisplayLabel = "Modified", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 11 },
            new() { FieldName = "ModifiedBy", DisplayLabel = "Modified By", DataType = "string", IsReadOnly = true, DisplayOrder = 12 },
        },
        StandaloneColumns = new() { "FirstName", "LastName", "Email", "City", "State", "IsActive" },
        ParentedColumns = new() { "FirstName", "LastName", "Email", "City", "State" },
        SearchColumns = new() { "FirstName", "LastName", "Email", "City", "State" },
        DetailFormFields = new() { "Id", "FirstName", "LastName", "Email", "Phone", "Address", "City", "State", "ZipCode", "IsActive", "CreatedDate", "ModifiedDate", "ModifiedBy" },
        ChildRelationships = new()
        {
            new()
            {
                RelationshipName = "Orders",
                ChildEntityName = "Order",
                ForeignKeyField = "CustomerId",
                DisplayOrder = 1,
                ShowAsTab = true
            }
        },
        SmartFilters = new()
        {
            new()
            {
                Name = "active accounts",
                Category = SmartFilterCategory.Custom,
                FilterExpression = "IsActive == true",
                DisplayOrder = 1
            },
            new()
            {
                Name = "added since yesterday",
                Category = SmartFilterCategory.Temporal,
                DateFieldName = "CreatedDate",
                RelativeDateRange = "1d",
                DisplayOrder = 2
            },
            new()
            {
                Name = "my last account updates",
                Category = SmartFilterCategory.Audit,
                AuditFieldName = "ModifiedDate",
                AuditUserFieldName = "ModifiedBy",
                DisplayOrder = 3
            }
        }
    };

    private static EntityMetadata BuildOrderMetadata() => new()
    {
        EntityName = "Order",
        DisplayName = "Orders",
        DisplayNameSingular = "Order",
        RecordLabelExpression = "\"Order #\" + [Id]",
        IconName = "ShoppingCart",
        ShowInNavigation = true,
        NavigationOrder = 3,
        SmartFilterTopN = 5,
        IsLeafEntity = false,
        PopupFieldThreshold = 20,
        Fields = new()
        {
            new() { FieldName = "Id", DisplayLabel = "Order #", DataType = "int", IsPrimaryKey = true, IsReadOnly = true, DisplayOrder = 0 },
            new() { FieldName = "CustomerId", DisplayLabel = "Retail Business", DataType = "int", IsForeignKey = true, ForeignKeyEntity = "Customer", IsRequired = true, DisplayOrder = 1, DetailFormColumn = 0 },
            new() { FieldName = "CustomerName", DisplayLabel = "Retail Business Contact", DataType = "string", IsReadOnly = true, DisplayOrder = 2, DetailFormColumn = 0 },
            new() { FieldName = "OrderDate", DisplayLabel = "Order Date", DataType = "DateTime", IsRequired = true, DisplayOrder = 3, DetailFormColumn = 0 },
            new() { FieldName = "ShipDate", DisplayLabel = "Ship Date", DataType = "DateTime", DisplayOrder = 4, DetailFormColumn = 1 },
            new() { FieldName = "Status", DisplayLabel = "Status", DataType = "string", IsRequired = true, DisplayOrder = 5, DetailFormColumn = 1 },
            new() { FieldName = "Total", DisplayLabel = "Total", DataType = "decimal", FormatString = "C2", IsReadOnly = true, DisplayOrder = 6, DetailFormColumn = 1 },
            new() { FieldName = "Notes", DisplayLabel = "Notes", DataType = "string", DisplayOrder = 7, DetailFormColumn = 0 },
            new() { FieldName = "CreatedDate", DisplayLabel = "Created", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 8 },
            new() { FieldName = "ModifiedDate", DisplayLabel = "Modified", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 9 },
            new() { FieldName = "ModifiedBy", DisplayLabel = "Modified By", DataType = "string", IsReadOnly = true, DisplayOrder = 10 },
        },
        StandaloneColumns = new() { "Id", "CustomerName", "OrderDate", "Status", "Total" },
        ParentedColumns = new() { "Id", "OrderDate", "Status", "Total" },
        SearchColumns = new() { "Id", "CustomerName", "OrderDate", "Status", "Total" },
        DetailFormFields = new() { "Id", "CustomerId", "CustomerName", "OrderDate", "ShipDate", "Status", "Total", "Notes", "CreatedDate", "ModifiedDate", "ModifiedBy" },
        ChildRelationships = new()
        {
            new()
            {
                RelationshipName = "Items",
                ChildEntityName = "OrderItem",
                ForeignKeyField = "OrderId",
                DisplayOrder = 1,
                ShowAsTab = true,
                IsJunctionEntity = true,
                FarSideEntityName = "Product",
                FarSideForeignKeyField = "ProductId",
                JunctionColumns = new() { "Quantity", "UnitPrice", "Discount" },
                FarSideColumns = new() { "Name", "RoastLevel" }
            },
            new()
            {
                RelationshipName = "Notes",
                ChildEntityName = "OrderNote",
                ForeignKeyField = "OrderId",
                DisplayOrder = 2,
                ShowAsTab = true,
                ClickAction = ChildClickAction.OpenPopup
            }
        },
        SmartFilters = new()
        {
            new()
            {
                Name = "for selected retail business",
                Category = SmartFilterCategory.Relational,
                RelatedEntityName = "Customer",
                ForeignKeyFieldName = "CustomerId",
                DisplayOrder = 1
            },
            new()
            {
                Name = "since yesterday",
                Category = SmartFilterCategory.Temporal,
                DateFieldName = "OrderDate",
                RelativeDateRange = "1d",
                DisplayOrder = 2
            },
            new()
            {
                Name = "my last edits",
                Category = SmartFilterCategory.Audit,
                AuditFieldName = "ModifiedDate",
                AuditUserFieldName = "ModifiedBy",
                DisplayOrder = 3
            }
        }
    };

    private static EntityMetadata BuildOrderItemMetadata() => new()
    {
        EntityName = "OrderItem",
        DisplayName = "Order Items",
        DisplayNameSingular = "Order Item",
        RecordLabelExpression = "\"Item #\" + [Id]",
        ShowInNavigation = false,
        IsLeafEntity = true,
        PopupFieldThreshold = 10,
        Fields = new()
        {
            new() { FieldName = "Id", DisplayLabel = "ID", DataType = "int", IsPrimaryKey = true, IsReadOnly = true, DisplayOrder = 0 },
            new() { FieldName = "OrderId", DisplayLabel = "Order", DataType = "int", IsForeignKey = true, ForeignKeyEntity = "Order", IsRequired = true, DisplayOrder = 1 },
            new() { FieldName = "ProductId", DisplayLabel = "Product", DataType = "int", IsForeignKey = true, ForeignKeyEntity = "Product", IsRequired = true, DisplayOrder = 2 },
            new() { FieldName = "ProductName", DisplayLabel = "Product Name", DataType = "string", IsReadOnly = true, DisplayOrder = 3 },
            new() { FieldName = "Quantity", DisplayLabel = "Quantity", DataType = "int", IsRequired = true, DisplayOrder = 4, DetailFormColumn = 0 },
            new() { FieldName = "UnitPrice", DisplayLabel = "Unit Price", DataType = "decimal", FormatString = "C2", IsRequired = true, DisplayOrder = 5, DetailFormColumn = 0 },
            new() { FieldName = "Discount", DisplayLabel = "Discount %", DataType = "decimal", FormatString = "N0", DisplayOrder = 6, DetailFormColumn = 1 },
        },
        StandaloneColumns = new() { "ProductName", "Quantity", "UnitPrice", "Discount" },
        ParentedColumns = new() { "ProductName", "Quantity", "UnitPrice", "Discount" },
        SearchColumns = new() { "ProductName", "Quantity", "UnitPrice", "Discount" },
        DetailFormFields = new() { "Id", "OrderId", "ProductId", "ProductName", "Quantity", "UnitPrice", "Discount" },
        ChildRelationships = new(),
        SmartFilters = new()
    };

    private static EntityMetadata BuildOrderNoteMetadata() => new()
    {
        EntityName = "OrderNote",
        DisplayName = "Order Notes",
        DisplayNameSingular = "Order Note",
        RecordLabelExpression = "[NoteDate] + \" - \" + [Author]",
        ShowInNavigation = false,
        IsLeafEntity = true,
        PopupFieldThreshold = 20,
        Fields = new()
        {
            new() { FieldName = "Id", DisplayLabel = "ID", DataType = "int", IsPrimaryKey = true, IsReadOnly = true, DisplayOrder = 0 },
            new() { FieldName = "OrderId", DisplayLabel = "Order", DataType = "int", IsForeignKey = true, ForeignKeyEntity = "Order", IsRequired = true, DisplayOrder = 1 },
            new() { FieldName = "NoteText", DisplayLabel = "Note", DataType = "string", IsRequired = true, DisplayOrder = 2, DetailFormColumn = 0 },
            new() { FieldName = "NoteDate", DisplayLabel = "Date", DataType = "DateTime", IsReadOnly = true, DisplayOrder = 3, DetailFormColumn = 1 },
            new() { FieldName = "Author", DisplayLabel = "Author", DataType = "string", IsRequired = true, DisplayOrder = 4, DetailFormColumn = 1 },
        },
        StandaloneColumns = new() { "NoteDate", "Author", "NoteText" },
        ParentedColumns = new() { "NoteDate", "Author", "NoteText" },
        SearchColumns = new() { "NoteDate", "Author", "NoteText" },
        DetailFormFields = new() { "Id", "OrderId", "NoteText", "NoteDate", "Author" },
        ChildRelationships = new(),
        SmartFilters = new()
    };
}
