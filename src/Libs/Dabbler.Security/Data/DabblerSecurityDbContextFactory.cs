using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dabbler.Security.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (e.g., `dotnet ef migrations add`).
/// This is not used by the application at runtime.
/// </summary>
public class DabblerSecurityDbContextFactory : IDesignTimeDbContextFactory<DabblerSecurityDbContext>
{
    /// <summary>
    /// Creates a new instance of a <see cref="DabblerSecurityDbContext"/>.
    /// </summary>
    /// <param name="args">Arguments provided by the design-time service.</param>
    /// <returns>A new <see cref="DabblerSecurityDbContext"/> instance.</returns>
    public DabblerSecurityDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DabblerSecurityDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=DabblerSecuritySample;Trusted_Connection=True;TrustServerCertificate=True");
        return new DabblerSecurityDbContext(optionsBuilder.Options);
    }
}
