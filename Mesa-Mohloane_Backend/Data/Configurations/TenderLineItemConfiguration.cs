using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class TenderLineItemConfiguration : IEntityTypeConfiguration<TenderLineItem>
{
    public void Configure(EntityTypeBuilder<TenderLineItem> builder)
    {
        builder.ToTable("TenderLineItems");
        builder.HasKey(li => li.Id);

        builder.Property(li => li.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(li => li.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(li => li.Quantity)
            .HasPrecision(18, 2);

        builder.Property(li => li.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(li => li.LineTotal)
            .HasPrecision(18, 2);

        builder.Property(li => li.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(li => li.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(li => li.IsDeleted)
            .HasDefaultValue(false);
    }
}
