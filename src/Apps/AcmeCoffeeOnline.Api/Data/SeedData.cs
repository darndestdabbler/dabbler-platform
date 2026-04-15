using AcmeCoffeeOnline.Shared.Enums;
using AcmeCoffeeOnline.Shared.Models;
using Dabbler.Security.Entities;

namespace AcmeCoffeeOnline.Api.Data;

public static class SeedData
{
    private static Random CreateRandom() => new(42);

    private static readonly string[] FirstNames =
    [
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda",
        "David", "Elizabeth", "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
        "Thomas", "Sarah", "Christopher", "Karen", "Charles", "Lisa", "Daniel", "Nancy",
        "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
        "Steven", "Kimberly", "Paul", "Emily", "Andrew", "Donna", "Joshua", "Michelle",
        "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Dorothy", "George", "Melissa",
        "Timothy", "Deborah"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
        "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson",
        "White", "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker",
        "Young", "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill",
        "Flores", "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell",
        "Mitchell", "Carter", "Roberts"
    ];

    private static readonly (string City, string State, string Zip)[] Locations =
    [
        ("New York", "NY", "10001"), ("Los Angeles", "CA", "90001"), ("Chicago", "IL", "60601"),
        ("Houston", "TX", "77001"), ("Phoenix", "AZ", "85001"), ("Philadelphia", "PA", "19101"),
        ("San Antonio", "TX", "78201"), ("San Diego", "CA", "92101"), ("Dallas", "TX", "75201"),
        ("Austin", "TX", "78701"), ("Jacksonville", "FL", "32099"), ("Fort Worth", "TX", "76101"),
        ("Columbus", "OH", "43085"), ("Charlotte", "NC", "28201"), ("San Francisco", "CA", "94101"),
        ("Indianapolis", "IN", "46201"), ("Seattle", "WA", "98101"), ("Denver", "CO", "80201"),
        ("Nashville", "TN", "37201"), ("Portland", "OR", "97201"), ("Oklahoma City", "OK", "73101"),
        ("Las Vegas", "NV", "89101"), ("Memphis", "TN", "38101"), ("Louisville", "KY", "40201"),
        ("Baltimore", "MD", "21201"), ("Milwaukee", "WI", "53201"), ("Albuquerque", "NM", "87101"),
        ("Tucson", "AZ", "85701"), ("Fresno", "CA", "93701"), ("Sacramento", "CA", "95801")
    ];

    private static readonly string[] NoteTexts =
    [
        "Retail business requested shelf talkers for the weekend promotion.",
        "Include tasting cards for the cafe launch team.",
        "Deliver to the receiving dock behind the retail storefront.",
        "Buyer confirmed this blend matches the retail account's decaf program.",
        "Rush order — the retail business needs replenishment before Friday open.",
        "Second wholesale order this week from the same retail account.",
        "Retail business asked about standing distributor restock options.",
        "Delivery address confirmed with the store manager by phone.",
        "Partial shipment approved by the retail business contact.",
        "Retail account prefers early-morning delivery before doors open.",
        "Add launch promo cards to the case pack.",
        "Retail buyer called to confirm order details.",
        "Shipping insurance requested for multi-store drop.",
        "Retail business wants tracking details emailed to the buyer.",
        "Order placed on behalf of a multi-location retail group."
    ];

    private static readonly string[] NoteAuthors =
    [
        "system", "jdoe@acmecoffee.com", "asmith@acmecoffee.com",
        "mwilson@acmecoffee.com", "kbrown@acmecoffee.com", "tjones@acmecoffee.com"
    ];

    public static List<Product> GetProducts()
    {
        var now = DateTime.UtcNow;
        return
        [
            new() { Name = "Deep Dark Roast - Regular - Whole Bean", RoastLevel = RoastLevel.DeepDark, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.WholeBean, UnitPrice = 17.99m, Description = "Bold and intense, our deepest roast for serious coffee lovers.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Deep Dark Roast - Regular - Drip", RoastLevel = RoastLevel.DeepDark, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Drip, UnitPrice = 18.49m, Description = "Smoky and robust, ground perfectly for drip brewers.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Deep Dark Roast - Decaf - Whole Bean", RoastLevel = RoastLevel.DeepDark, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.WholeBean, UnitPrice = 18.99m, Description = "All the bold flavor, none of the caffeine.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Deep Dark Roast - Decaf - Espresso", RoastLevel = RoastLevel.DeepDark, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.Espresso, UnitPrice = 19.49m, Description = "Rich decaf espresso grind for the perfect evening shot.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Dark Roast - Regular - Whole Bean", RoastLevel = RoastLevel.Dark, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.WholeBean, UnitPrice = 16.49m, Description = "Classic dark roast with chocolate and caramel undertones.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Dark Roast - Regular - Drip", RoastLevel = RoastLevel.Dark, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Drip, UnitPrice = 16.99m, Description = "Full-bodied dark roast, ready for your morning brew.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Dark Roast - Regular - Espresso", RoastLevel = RoastLevel.Dark, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Espresso, UnitPrice = 17.49m, Description = "Finely ground dark roast for espresso machines.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Dark Roast - Decaf - Drip", RoastLevel = RoastLevel.Dark, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.Drip, UnitPrice = 17.99m, Description = "Smooth decaf dark roast for evening enjoyment.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Medium Roast - Regular - Whole Bean", RoastLevel = RoastLevel.Medium, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.WholeBean, UnitPrice = 14.99m, Description = "Balanced and approachable, our most popular roast.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Medium Roast - Regular - Drip", RoastLevel = RoastLevel.Medium, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Drip, UnitPrice = 15.49m, Description = "Smooth medium roast, perfect for everyday brewing.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Medium Roast - Regular - Espresso", RoastLevel = RoastLevel.Medium, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Espresso, UnitPrice = 15.99m, Description = "Versatile medium roast espresso grind.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Medium Roast - Decaf - Whole Bean", RoastLevel = RoastLevel.Medium, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.WholeBean, UnitPrice = 15.99m, Description = "Our classic medium roast, decaffeinated.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Medium Roast - Decaf - Drip", RoastLevel = RoastLevel.Medium, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.Drip, UnitPrice = 16.49m, Description = "Mellow decaf medium roast for drip coffee lovers.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Blonde Roast - Regular - Whole Bean", RoastLevel = RoastLevel.Blonde, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.WholeBean, UnitPrice = 13.99m, Description = "Light and bright with citrus notes.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Blonde Roast - Regular - Drip", RoastLevel = RoastLevel.Blonde, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Drip, UnitPrice = 14.49m, Description = "Delicate blonde roast ground for drip brewing.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Blonde Roast - Decaf - Whole Bean", RoastLevel = RoastLevel.Blonde, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.WholeBean, UnitPrice = 14.99m, Description = "Gentle decaf with floral and citrus highlights.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Blonde Roast - Decaf - Espresso", RoastLevel = RoastLevel.Blonde, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.Espresso, UnitPrice = 15.49m, Description = "Smooth decaf espresso with a light finish.", IsAvailable = false, CreatedDate = now, ModifiedDate = now },
            new() { Name = "Light Roast - Regular - Whole Bean", RoastLevel = RoastLevel.Light, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.WholeBean, UnitPrice = 12.99m, Description = "Bright acidity and complex fruit flavors.", CreatedDate = now, ModifiedDate = now },
            new() { Name = "Light Roast - Regular - Drip", RoastLevel = RoastLevel.Light, CaffeineContent = CaffeineContent.Regular, GrindType = GrindType.Drip, UnitPrice = 13.49m, Description = "Delicate light roast for a nuanced cup.", IsAvailable = false, CreatedDate = now, ModifiedDate = now },
            new() { Name = "Light Roast - Decaf - Whole Bean", RoastLevel = RoastLevel.Light, CaffeineContent = CaffeineContent.Decaf, GrindType = GrindType.WholeBean, UnitPrice = 13.99m, Description = "Fruity and floral decaf for the discerning palate.", CreatedDate = now, ModifiedDate = now },
        ];
    }

    public static List<Customer> GetCustomers()
    {
        var random = CreateRandom();
        var now = DateTime.UtcNow;
        var customers = new List<Customer>(50);

        for (int i = 0; i < 50; i++)
        {
            var firstName = FirstNames[i];
            var lastName = LastNames[i];
            var location = Locations[random.Next(Locations.Length)];

            // Most customers created 30-730 days ago; a few created recently for filter testing
            var createdDate = i < 3
                ? now.AddDays(-1).AddHours(-random.Next(0, 12))
                : now.AddDays(-random.Next(30, 730));

            var isActive = i >= 45 ? false : true; // Last 5 customers inactive

            customers.Add(new Customer
            {
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                FirstName = firstName,
                LastName = lastName,
                Phone = $"({random.Next(200, 999)}) {random.Next(200, 999)}-{random.Next(1000, 9999)}",
                Address = $"{random.Next(100, 9999)} {LastNames[random.Next(LastNames.Length)]} {(random.Next(2) == 0 ? "St" : "Ave")}",
                City = location.City,
                State = location.State,
                ZipCode = location.Zip,
                IsActive = isActive,
                CreatedDate = createdDate,
                ModifiedDate = i < 5
                    ? now.AddHours(-random.Next(1, 24))
                    : createdDate.AddDays(random.Next(0, 30)),
                ModifiedBy = i < 8 ? NoteAuthors[random.Next(1, NoteAuthors.Length)] : null
            });
        }

        return customers;
    }

    public static List<Order> GetOrders(List<Customer> customers, List<Product> products)
    {
        var random = CreateRandom();
        var now = DateTime.UtcNow;
        var orders = new List<Order>(550);

        foreach (var customer in customers)
        {
            // Power distribution biased toward lower counts: covers 1-50, averages ~10 per customer (~500 total)
            var orderCount = Math.Max(1, (int)Math.Ceiling(Math.Pow(random.NextDouble(), 4) * 50));
            var maxDaysFromCreation = Math.Max(1, (int)(now - customer.CreatedDate).TotalDays);

            for (int o = 0; o < orderCount; o++)
            {
                var orderDate = customer.CreatedDate.AddDays(random.Next(0, maxDaysFromCreation));
                var daysAgo = (now - orderDate).TotalDays;

                var status = PickStatus(random, daysAgo);
                DateTime? shipDate = null;
                if (status is OrderStatus.Shipped or OrderStatus.Delivered)
                {
                    shipDate = orderDate.AddDays(random.Next(1, 8));
                    if (shipDate > now) shipDate = now;
                }

                var order = new Order
                {
                    CustomerId = customer.Id,
                    OrderDate = orderDate,
                    ShipDate = shipDate,
                    Status = status,
                    CreatedDate = orderDate,
                    ModifiedDate = shipDate ?? orderDate.AddDays(random.Next(0, 3)),
                    ModifiedBy = random.Next(4) == 0 ? NoteAuthors[random.Next(1, NoteAuthors.Length)] : null
                };

                // 1-5 order items per order (retry on duplicate products)
                var itemCount = random.Next(1, 6);
                decimal orderTotal = 0;
                var usedProductIds = new HashSet<int>();

                for (int it = 0; it < itemCount; it++)
                {
                    Product? product = null;
                    for (int attempt = 0; attempt < 20; attempt++)
                    {
                        var candidate = products[random.Next(products.Count)];
                        if (usedProductIds.Add(candidate.Id))
                        {
                            product = candidate;
                            break;
                        }
                    }
                    if (product is null) break;

                    var quantity = random.Next(1, 6);
                    var discount = random.NextDouble() < 0.2
                        ? (decimal)random.Next(5, 26)
                        : 0m;

                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = quantity,
                        UnitPrice = product.UnitPrice,
                        Discount = discount
                    });

                    orderTotal += quantity * product.UnitPrice * (1 - discount / 100m);
                }

                order.Total = Math.Round(orderTotal, 2);

                // ~40% of orders get a note
                if (random.NextDouble() < 0.4)
                {
                    var noteDate = orderDate.AddHours(random.Next(1, 72));
                    if (noteDate > now) noteDate = now;

                    order.OrderNotes.Add(new OrderNote
                    {
                        NoteText = NoteTexts[random.Next(NoteTexts.Length)],
                        NoteDate = noteDate,
                        Author = NoteAuthors[random.Next(NoteAuthors.Length)]
                    });
                }

                orders.Add(order);
            }
        }

        return orders;
    }

    public static List<AppRole> GetSecurityRoles()
    {
        var now = DateTime.UtcNow;
        return
        [
            new() { Name = "Customer", Description = "Retail-business account with self-service access to its own orders and profile.", IsSystemRole = true, CreatedAt = now },
            new() { Name = "BackofficeUser", Description = "Internal staff with read access to all retail-business accounts and orders.", IsSystemRole = true, InheritsFromRoleName = "Customer", CreatedAt = now },
            new() { Name = "BackofficeUserAdmin", Description = "Internal operations admin who can manage staff accounts and roles.", IsSystemRole = true, InheritsFromRoleName = "BackofficeUser", CreatedAt = now },
            new() { Name = "SystemAdmin", Description = "Full system administrator with unrestricted access.", IsSystemRole = true, InheritsFromRoleName = "BackofficeUserAdmin", CreatedAt = now },
        ];
    }

    public static List<AppUser> GetSecurityUsers(List<Customer> customers)
    {
        var now = DateTime.UtcNow;
        var users = new List<AppUser>(56);

        // System administrator
        users.Add(new AppUser { Email = "admin@acmecoffee.com", DisplayName = "Internal Platform Admin", CreatedAt = now });

        // Internal staff users (3 staff + 2 admins)
        users.Add(new AppUser { Email = "jdoe@acmecoffee.com", DisplayName = "Jordan Ellis", CreatedAt = now });
        users.Add(new AppUser { Email = "asmith@acmecoffee.com", DisplayName = "Avery Smith", CreatedAt = now });
        users.Add(new AppUser { Email = "mwilson@acmecoffee.com", DisplayName = "Morgan Wilson", CreatedAt = now });
        users.Add(new AppUser { Email = "kbrown@acmecoffee.com", DisplayName = "Kai Brown", CreatedAt = now });
        users.Add(new AppUser { Email = "tjones@acmecoffee.com", DisplayName = "Taylor Jones", CreatedAt = now });

        // Retail-business contacts (one per current customer record, matching by email)
        foreach (var customer in customers)
        {
            users.Add(new AppUser
            {
                Email = customer.Email,
                DisplayName = $"{customer.FirstName} {customer.LastName}",
                IsActive = customer.IsActive,
                CreatedAt = customer.CreatedDate
            });
        }

        return users;
    }

    public static List<AppUserRole> GetUserRoleAssignments(List<AppUser> users, List<AppRole> roles)
    {
        var now = DateTime.UtcNow;
        var assignments = new List<AppUserRole>(56);

        var customerRole = roles.First(r => r.Name == "Customer");
        var staffRole = roles.First(r => r.Name == "BackofficeUser");
        var adminRole = roles.First(r => r.Name == "BackofficeUserAdmin");
        var sysAdminRole = roles.First(r => r.Name == "SystemAdmin");

        var systemAdmin = users.First(u => u.Email == "admin@acmecoffee.com");

        // System admin
        assignments.Add(new AppUserRole { UserId = systemAdmin.Id, RoleId = sysAdminRole.Id, AssignedAt = now, AssignedBy = systemAdmin.Id });

        // Internal operations admins
        foreach (var email in new[] { "kbrown@acmecoffee.com", "tjones@acmecoffee.com" })
        {
            var user = users.First(u => u.Email == email);
            assignments.Add(new AppUserRole { UserId = user.Id, RoleId = adminRole.Id, AssignedAt = now, AssignedBy = systemAdmin.Id });
        }

        // Internal staff
        foreach (var email in new[] { "jdoe@acmecoffee.com", "asmith@acmecoffee.com", "mwilson@acmecoffee.com" })
        {
            var user = users.First(u => u.Email == email);
            assignments.Add(new AppUserRole { UserId = user.Id, RoleId = staffRole.Id, AssignedAt = now, AssignedBy = systemAdmin.Id });
        }

        // Retail-business contacts
        foreach (var user in users.Where(u => u.Email.EndsWith("@example.com")))
        {
            assignments.Add(new AppUserRole { UserId = user.Id, RoleId = customerRole.Id, AssignedAt = now, AssignedBy = systemAdmin.Id });
        }

        return assignments;
    }

    /// <summary>
    /// Maps DevIdp users (by OID) to AppUser records via AppExternalLogin.
    /// The OIDs must match those seeded in DevIdp's SeedData.
    /// </summary>
    public static List<AppExternalLogin> GetExternalLogins(List<AppUser> users)
    {
        var now = DateTime.UtcNow;

        // DevIdp OID → AppUser email mapping
        var mappings = new (string Oid, string AppUserEmail)[]
        {
            ("aaaa0000-0000-0000-0000-000000000001", "admin@acmecoffee.com"),
            ("aaaa0000-0000-0000-0000-000000000002", "kbrown@acmecoffee.com"),
            ("aaaa0000-0000-0000-0000-000000000003", "jdoe@acmecoffee.com"),
            ("aaaa0000-0000-0000-0000-000000000004", "asmith@acmecoffee.com"),
            ("aaaa0000-0000-0000-0000-000000000005", "mwilson@acmecoffee.com"),
        };

        var logins = new List<AppExternalLogin>();
        foreach (var (oid, email) in mappings)
        {
            var user = users.FirstOrDefault(u => u.Email == email);
            if (user is null) continue;

            logins.Add(new AppExternalLogin
            {
                UserId = user.Id,
                Provider = "EntraId",
                ProviderSubjectId = oid,
                ProviderEmail = email,
                LinkedAt = now
            });
        }

        return logins;
    }

    private static OrderStatus PickStatus(Random random, double daysAgo)
    {
        if (daysAgo > 30)
        {
            var r = random.NextDouble();
            return r < 0.80 ? OrderStatus.Delivered
                 : r < 0.90 ? OrderStatus.Cancelled
                 : OrderStatus.Shipped;
        }

        if (daysAgo > 7)
        {
            var r = random.NextDouble();
            return r < 0.45 ? OrderStatus.Delivered
                 : r < 0.70 ? OrderStatus.Shipped
                 : r < 0.85 ? OrderStatus.Confirmed
                 : OrderStatus.Cancelled;
        }

        var rv = random.NextDouble();
        return rv < 0.40 ? OrderStatus.Pending
             : rv < 0.70 ? OrderStatus.Confirmed
             : OrderStatus.Shipped;
    }
}
