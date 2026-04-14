using AcmeCoffeeOnline.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcmeCoffeeOnline.Api.Data.Configuration;

public class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.OrderId)
            .IsRequired();

        builder.Property(n => n.NoteText)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.NoteDate)
            .IsRequired();

        builder.Property(n => n.Author)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(n => n.OrderId);
    }
}
