using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Property(p => p.PaymentReference)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Method)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(p => p.PaymentReference).IsUnique();
        builder.HasIndex(p => p.CreatedAt);

        builder.HasOne(p => p.Invoice)
            .WithOne(i => i.Payment)
            .HasForeignKey<Payment>(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ApprovedByAdmin)
            .WithMany(u => u.PaymentsApproved)
            .HasForeignKey(p => p.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
