using Microsoft.EntityFrameworkCore;
using SmokeApi.Shared.Models;

namespace SmokeApi.Api.Data;

public class SmokeDbContext : DbContext
{
    public SmokeDbContext(DbContextOptions<SmokeDbContext> options) : base(options) { }

    public DbSet<Smoke> Smokes => Set<Smoke>();
}
