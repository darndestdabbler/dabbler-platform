using Microsoft.EntityFrameworkCore;
using SmokeApp.Shared.Models;

namespace SmokeApp.Api.Data;

public class SmokeDbContext : DbContext
{
    public SmokeDbContext(DbContextOptions<SmokeDbContext> options) : base(options) { }

    public DbSet<Smoke> Smokes => Set<Smoke>();
}
