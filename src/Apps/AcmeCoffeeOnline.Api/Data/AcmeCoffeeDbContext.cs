using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace AcmeCoffeeOnline.Api.Data;

public class AcmeCoffeeDbContext : DbContext
{
    public AcmeCoffeeDbContext(DbContextOptions<AcmeCoffeeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcmeCoffeeDbContext).Assembly);
    }
}
