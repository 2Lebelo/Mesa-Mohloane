using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.OriginalQuotedAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.FinalInvoiceAmount)
            .HasPrecision(18, 2);

        builder.Property(i => i.VariancePercentage)
            .HasPrecision(6, 2);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.ValidationRemarks)
            .HasMaxLength(2000);

        builder.Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
        builder.HasIndex(i => i.CreatedAt);

        builder.HasOne(i => i.Assignment)
            .WithOne(a => a.Invoice)
            .HasForeignKey<Invoice>(i => i.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TenderApplication)
            .WithOne(t => t.Invoice)
            .HasForeignKey<Invoice>(i => i.TenderApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Contractor)
            .WithMany()
            .HasForeignKey(i => i.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ApprovedByAdmin)
            .WithMany(u => u.InvoicesApproved)
            .HasForeignKey(i => i.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.LineItems)
            .WithOne(li => li.Invoice)
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Payment)
            .WithOne(p => p.Invoice)
            .HasForeignKey<Payment>(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
