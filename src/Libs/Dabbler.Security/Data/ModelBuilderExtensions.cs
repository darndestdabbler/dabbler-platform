using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Data;

/// <summary>
/// Provides extension methods for <see cref="ModelBuilder"/> to configure the Dabbler.Security data model.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies all entity configurations for the Dabbler.Security data model to the <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> to configure.</param>
    public static void ApplyDabblerSecurity(this ModelBuilder modelBuilder)
    {
        ConfigureAppUser(modelBuilder);
        ConfigureAppOrganization(modelBuilder);
        ConfigureAppRole(modelBuilder);
        ConfigureAppUserOrganizationMembership(modelBuilder);
        ConfigureAppUserRole(modelBuilder);
        ConfigureRolePermissions(modelBuilder);
        ConfigureRuntimePermissions(modelBuilder);
        ConfigureMagicLinkToken(modelBuilder);
        ConfigureAppUserMfa(modelBuilder);
        ConfigureAppUserRoleAssignment(modelBuilder);
        ConfigureAppExternalLogin(modelBuilder);
    }

    private static void ConfigureAppUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.ToTable("AppUsers", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppUsersHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
        });
    }

    private static void ConfigureAppRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppRole>(e =>
        {
            e.ToTable("AppRoles", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppRolesHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.Property(x => x.InheritsFromRoleName).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
        });
    }

    private static void ConfigureAppOrganization(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppOrganization>(e =>
        {
            e.ToTable("AppOrganizations", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppOrganizationsHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
        });
    }

    private static void ConfigureAppUserOrganizationMembership(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserOrganizationMembership>(e =>
        {
            e.ToTable("AppUserOrganizationMemberships", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppUserOrganizationMembershipsHistory");
            }));

            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.OrganizationId }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.IsDefault })
                .HasFilter("[IsDefault] = 1");
            e.HasOne(x => x.User)
                .WithMany(x => x.OrganizationMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Organization)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAppUserRole(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserRole>(e =>
        {
            e.ToTable("AppUserRoles", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppUserRolesHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasDefaultValueSql("NEWID()");
            e.HasIndex(x => new { x.UserId, x.RoleId })
                .IsUnique()
                .HasFilter("[OrganizationMembershipId] IS NULL");
            e.HasIndex(x => new { x.UserId, x.RoleId, x.OrganizationMembershipId })
                .IsUnique()
                .HasFilter("[OrganizationMembershipId] IS NOT NULL");
            e.HasOne(x => x.User).WithMany(x => x.Roles).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.Role).WithMany(x => x.Users).HasForeignKey(x => x.RoleId);
            e.HasOne(x => x.OrganizationMembership)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => x.OrganizationMembershipId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureRolePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppRolePermission>(e =>
        {
            e.ToTable("AppRolePermissions", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppRolePermissionsHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.EntityName).HasMaxLength(200).IsRequired();
            e.Property(x => x.PermissionType).HasMaxLength(50).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.EntityName, x.PermissionType }).IsUnique();
        });

        modelBuilder.Entity<AppFieldPermission>(e =>
        {
            e.ToTable("AppFieldPermissions", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppFieldPermissionsHistory");
            }));

            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.EntityName, x.FieldName }).IsUnique();
        });

        modelBuilder.Entity<AppPagePermission>(e =>
        {
            e.ToTable("AppPagePermissions", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppPagePermissionsHistory");
            }));

            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.RoleId, x.PageRoute }).IsUnique();
        });
    }

    private static void ConfigureRuntimePermissions(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppEndpointPermission>(e =>
        {
            e.ToTable("AppEndpointPermissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.EndpointKey).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.EndpointKey }).IsUnique();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<AppControlPermission>(e =>
        {
            e.ToTable("AppControlPermissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.PageKey).HasMaxLength(500).IsRequired();
            e.Property(x => x.ControlKey).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.PageKey, x.ControlKey }).IsUnique();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<AppRowFilter>(e =>
        {
            e.ToTable("AppRowFilters");
            e.HasKey(x => x.Id);
            e.Property(x => x.EntityKey).HasMaxLength(200).IsRequired();
            e.Property(x => x.FilterExpression).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.RoleId, x.EntityKey }).IsUnique();
            e.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        });
    }

    private static void ConfigureMagicLinkToken(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MagicLinkToken>(e =>
        {
            e.ToTable("MagicLinkTokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(254).IsRequired();
            e.Property(x => x.TokenHash).IsRequired();
            e.Property(x => x.CodeHash).IsRequired();
        });
    }

    private static void ConfigureAppUserMfa(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserMfa>(e =>
        {
            e.ToTable("AppUserMfa", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppUserMfaHistory");
            }));

            e.HasKey(x => x.UserId);
            e.HasOne(x => x.User).WithOne().HasForeignKey<AppUserMfa>(x => x.UserId);
        });
    }

    private static void ConfigureAppExternalLogin(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppExternalLogin>(e =>
        {
            e.ToTable("AppExternalLogins", b => b.IsTemporal(t =>
            {
                t.HasPeriodStart("ValidFrom");
                t.HasPeriodEnd("ValidTo");
                t.UseHistoryTable("AppExternalLoginsHistory");
            }));

            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(100).IsRequired();
            e.Property(x => x.ProviderSubjectId).HasMaxLength(200).IsRequired();
            e.Property(x => x.ProviderEmail).HasMaxLength(254);
            e.HasIndex(x => new { x.Provider, x.ProviderSubjectId }).IsUnique();
            e.HasIndex(x => x.UserId);
            e.HasOne(x => x.User).WithMany(x => x.ExternalLogins).HasForeignKey(x => x.UserId);
        });
    }

    private static void ConfigureAppUserRoleAssignment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUserRoleAssignment>(e =>
        {
            e.ToTable("AppUserRoleAssignments");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(256).IsRequired();
            e.Property(x => x.RoleName).HasMaxLength(100).IsRequired();
            e.Property(x => x.AssignedBy).HasMaxLength(256);
            e.HasIndex(x => new { x.UserId, x.RoleName }).IsUnique();
            e.HasIndex(x => x.UserId);
        });
    }
}
