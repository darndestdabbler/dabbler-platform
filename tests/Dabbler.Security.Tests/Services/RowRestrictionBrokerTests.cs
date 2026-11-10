using System.Security.Claims;
using Dabbler.Security.Abstractions;
using Dabbler.Security.Authorization;
using Dabbler.Security.Configuration;
using Dabbler.Security.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dabbler.Security.Tests.Services;

public class RowRestrictionBrokerTests
{
    private sealed class Order
    {
        public int CustomerId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    private static RowRestrictionBroker CreateBroker(
        SecurityConfiguration configuration,
        ClaimsPrincipal principal,
        string? switchedRole = null)
    {
        var store = new Mock<ISecurityConfigurationStore>();
        store.Setup(s => s.LoadAsync(default)).ReturnsAsync(configuration);
        var manager = new SecurityConfigurationManager(store.Object);
        manager.EnsureLoadedAsync().GetAwaiter().GetResult();

        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var switcher = new SessionRoleSwitcher();
        if (!string.IsNullOrEmpty(switchedRole))
        {
            switcher.SwitchToRole(switchedRole);
        }

        return new RowRestrictionBroker(
            accessor,
            switcher,
            manager,
            new PlatformBakedInRoleProvider(),
            NullLogger<RowRestrictionBroker>.Instance);
    }

    private static ClaimsPrincipal BuildPrincipal(
        IEnumerable<string> roleNames,
        int? userId = null,
        int? customerId = null)
    {
        var claims = new List<Claim>();
        if (userId is int uid)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, uid.ToString()));
        }

        if (customerId is int cid)
        {
            claims.Add(new Claim("CustomerId", cid.ToString()));
        }

        claims.AddRange(roleNames.Select(r => new Claim(ClaimTypes.Role, r)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public void NoAuthenticatedPrincipal_ReturnsNull()
    {
        var broker = CreateBroker(new SecurityConfiguration(), new ClaimsPrincipal(new ClaimsIdentity()));

        broker.GetRestriction<Order>("Order").Should().BeNull();
    }

    [Fact]
    public void BakedInRole_ShortCircuitsToNull()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Customer",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "Order",
                            FilterExpression = "CustomerId == @CurrentUserCustomerId"
                        }
                    }
                }
            }
        };

        // User carries both a user-type claim and a baked-in role claim.
        // The broker should defer to compiled baked-in privileges and return null.
        var principal = BuildPrincipal(
            roleNames: [BakedInRoles.SystemAdmin, "Customer"],
            userId: 1,
            customerId: 7);

        var broker = CreateBroker(configuration, principal);

        broker.GetRestriction<Order>("Order").Should().BeNull();
    }

    [Fact]
    public void UserTypeRoleWithMatchingRestriction_ReturnsCompiledFilter()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Customer",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "Order",
                            FilterExpression = "CustomerId == @CurrentUserCustomerId"
                        }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["Customer"], userId: 1, customerId: 7);
        var broker = CreateBroker(configuration, principal);

        var expr = broker.GetRestriction<Order>("Order");
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { CustomerId = 7 }).Should().BeTrue();
        compiled(new Order { CustomerId = 8 }).Should().BeFalse();
    }

    [Fact]
    public void UserTypeRoleWithoutMatchingEntity_ReturnsNull()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "BackofficeViewer",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "SomeOther",
                            FilterExpression = "Id == 1"
                        }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["BackofficeViewer"], userId: 1);
        var broker = CreateBroker(configuration, principal);

        // No RowRestriction for "Order" on this role → role is default-permissive
        // for orders → broker returns null so the caller skips any row narrowing.
        broker.GetRestriction<Order>("Order").Should().BeNull();
    }

    [Fact]
    public void EntityKeyMatchIsCaseInsensitive()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Customer",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "ORDER",
                            FilterExpression = "CustomerId == @CurrentUserCustomerId"
                        }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["Customer"], userId: 1, customerId: 3);
        var broker = CreateBroker(configuration, principal);

        broker.GetRestriction<Order>("order").Should().NotBeNull();
    }

    [Fact]
    public void UnparseableRestriction_ProducesDenyAllForThatRole()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Customer",
                    RowRestrictions =
                    {
                        new RowRestriction
                        {
                            EntityKey = "Order",
                            FilterExpression = "NotAColumn <=> garbage"
                        }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["Customer"], userId: 1, customerId: 7);
        var broker = CreateBroker(configuration, principal);

        var expr = broker.GetRestriction<Order>("Order");
        expr.Should().NotBeNull();
        expr!.Compile()(new Order { CustomerId = 7 }).Should().BeFalse();
    }

    [Fact]
    public void MultipleUserTypeRolesWithRestrictions_CombineViaOr()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "RoleA",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 1" }
                    }
                },
                new RoleDefinition
                {
                    RoleName = "RoleB",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 2" }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["RoleA", "RoleB"]);
        var broker = CreateBroker(configuration, principal);

        var expr = broker.GetRestriction<Order>("Order");
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { CustomerId = 1 }).Should().BeTrue();
        compiled(new Order { CustomerId = 2 }).Should().BeTrue();
        compiled(new Order { CustomerId = 3 }).Should().BeFalse();
    }

    [Fact]
    public void MultipleRolesWhenOneIsUnrestricted_ReturnsNull()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "Restricted",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 1" }
                    }
                },
                new RoleDefinition
                {
                    RoleName = "Unrestricted"
                    // No RowRestrictions — role is default-permissive for Order.
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["Restricted", "Unrestricted"]);
        var broker = CreateBroker(configuration, principal);

        // The unrestricted role wins because multi-role resolution is the
        // union of what each role allows.
        broker.GetRestriction<Order>("Order").Should().BeNull();
    }

    [Fact]
    public void InheritanceIsNotApplied()
    {
        // Role A "InheritsFrom" B in legacy data. The new broker must NOT pick
        // up B's restriction when the user is in A only — inheritance is gone.
#pragma warning disable CS0618 // Legacy field intentionally populated for this regression check.
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "RoleA",
                    InheritsFrom = "RoleB"
                    // No RowRestrictions on A.
                },
                new RoleDefinition
                {
                    RoleName = "RoleB",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 999" }
                    }
                }
            }
        };
#pragma warning restore CS0618

        var principal = BuildPrincipal(roleNames: ["RoleA"]);
        var broker = CreateBroker(configuration, principal);

        // A has no restriction for Order → default-permissive → null.
        // If inheritance were (wrongly) applied, we'd get B's CustomerId == 999 filter.
        broker.GetRestriction<Order>("Order").Should().BeNull();
    }

    [Fact]
    public void SwitchedRole_UsesOnlyThatRolesRestriction()
    {
        var configuration = new SecurityConfiguration
        {
            UserTypeRoles =
            {
                new RoleDefinition
                {
                    RoleName = "RoleA",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 1" }
                    }
                },
                new RoleDefinition
                {
                    RoleName = "RoleB",
                    RowRestrictions =
                    {
                        new RowRestriction { EntityKey = "Order", FilterExpression = "CustomerId == 2" }
                    }
                }
            }
        };

        var principal = BuildPrincipal(roleNames: ["RoleA", "RoleB"]);
        var broker = CreateBroker(configuration, principal, switchedRole: "RoleB");

        var expr = broker.GetRestriction<Order>("Order");
        expr.Should().NotBeNull();
        var compiled = expr!.Compile();
        compiled(new Order { CustomerId = 1 }).Should().BeFalse();
        compiled(new Order { CustomerId = 2 }).Should().BeTrue();
    }
}
