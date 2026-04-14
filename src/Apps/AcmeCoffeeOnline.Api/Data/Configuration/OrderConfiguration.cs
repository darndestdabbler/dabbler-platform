using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeCoffeeOnline.Api.Data.Configuration;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId)
            .IsRequired();

        builder.Property(o => o.OrderDate)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.Total)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(o => o.Notes)
            .HasMaxLength(500);

        builder.Property(o => o.CreatedDate)
            .IsRequired();

        builder.Property(o => o.ModifiedDate)
            .IsRequired();

        builder.Property(o => o.ModifiedBy)
            .HasMaxLength(100);

        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.OrderDate);
        builder.HasIndex(o => o.Status);

        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.OrderNotes)
            .WithOne(n => n.Order)
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
