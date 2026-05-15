using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeCoffeeOnline.Api.Data.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t => t.IsTemporal(tt =>
        {
            tt.HasPeriodStart("SysStart");
            tt.HasPeriodEnd("SysEnd");
            tt.UseHistoryTable("ProductHistory");
        }));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.RoastLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.CaffeineContent)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.GrindType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(200);

        builder.Property(p => p.CreatedDate)
            .IsRequired();

        builder.Property(p => p.ModifiedDate)
            .IsRequired();
    }
}
