using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class TenderApplicationConfiguration : IEntityTypeConfiguration<TenderApplication>
{
    public void Configure(EntityTypeBuilder<TenderApplication> builder)
    {
        builder.ToTable("TenderApplications");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ProposalText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(t => t.QuotedTotalAmount)
            .HasPrecision(18, 2);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.WeightedScore)
            .HasPrecision(6, 2);

        builder.Property(t => t.CostScore)
            .HasPrecision(6, 2);

        builder.Property(t => t.RatingScore)
            .HasPrecision(6, 2);

        builder.Property(t => t.PerformanceScore)
            .HasPrecision(6, 2);

        builder.Property(t => t.EvaluationNotes)
            .HasMaxLength(2000);

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.CreatedAt);

        builder.HasOne(t => t.Contractor)
            .WithMany(u => u.TenderApplications)
            .HasForeignKey(t => t.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ReviewedByAdmin)
            .WithMany()
            .HasForeignKey(t => t.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.LineItems)
            .WithOne(li => li.TenderApplication)
            .HasForeignKey(li => li.TenderApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Assignment)
            .WithOne(a => a.TenderApplication)
            .HasForeignKey<Assignment>(a => a.TenderApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Invoice)
            .WithOne(i => i.TenderApplication)
            .HasForeignKey<Invoice>(i => i.TenderApplicationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
