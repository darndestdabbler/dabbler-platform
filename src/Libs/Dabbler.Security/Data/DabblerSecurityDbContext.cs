using Dabbler.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dabbler.Security.Data;

/// <summary>
/// The Entity Framework Core database context for Dabbler.Security entities.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class DabblerSecurityDbContext(DbContextOptions<DabblerSecurityDbContext> options) : DbContext(options)
{
    /// <summary>Gets or sets the <see cref="DbSet{AppUser}"/> of application users.</summary>
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    /// <summary>Gets or sets the <see cref="DbSet{AppRole}"/> of application roles.</summary>
    public DbSet<AppRole> AppRoles => Set<AppRole>();
    /// <summary>Gets or sets the <see cref="DbSet{AppUserRole}"/> linking users to roles.</summary>
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();
    /// <summary>Gets or sets the <see cref="DbSet{AppRolePermission}"/> for general role-based permissions.</summary>
    public DbSet<AppRolePermission> AppRolePermissions => Set<AppRolePermission>();
    /// <summary>Gets or sets the <see cref="DbSet{AppFieldPermission}"/> for field-level permissions.</summary>
    public DbSet<AppFieldPermission> AppFieldPermissions => Set<AppFieldPermission>();
    /// <summary>Gets or sets the <see cref="DbSet{AppPagePermission}"/> for page access permissions.</summary>
    public DbSet<AppPagePermission> AppPagePermissions => Set<AppPagePermission>();
    /// <summary>Gets or sets the <see cref="DbSet{MagicLinkToken}"/> for magic link authentication.</summary>
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    /// <summary>Gets or sets the <see cref="DbSet{AppUserMfa}"/> for user MFA configurations.</summary>
    public DbSet<AppUserMfa> AppUserMfas => Set<AppUserMfa>();
    /// <summary>Gets or sets the <see cref="DbSet{AppEndpointPermission}"/> for API endpoint permissions.</summary>
    public DbSet<AppEndpointPermission> AppEndpointPermissions => Set<AppEndpointPermission>();
    /// <summary>Gets or sets the <see cref="DbSet{AppControlPermission}"/> for UI control permissions.</summary>
    public DbSet<AppControlPermission> AppControlPermissions => Set<AppControlPermission>();
    /// <summary>Gets or sets the <see cref="DbSet{AppRowFilter}"/> for data row filtering rules.</summary>
    public DbSet<AppRowFilter> AppRowFilters => Set<AppRowFilter>();
    /// <summary>Gets or sets the <see cref="DbSet{AppUserRoleAssignment}"/> for user-role assignment records from an external store.</summary>
    public DbSet<AppUserRoleAssignment> AppUserRoleAssignments => Set<AppUserRoleAssignment>();
    /// <summary>Gets or sets the <see cref="DbSet{AppExternalLogin}"/> for linking user accounts to external identity providers.</summary>
    public DbSet<AppExternalLogin> AppExternalLogins => Set<AppExternalLogin>();

    /// <summary>
    /// Configures the model for this context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyDabblerSecurity();
        base.OnModelCreating(modelBuilder);
    }
}
