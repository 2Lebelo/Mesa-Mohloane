using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class WorkCompletionConfiguration : IEntityTypeConfiguration<WorkCompletion>
{
    public void Configure(EntityTypeBuilder<WorkCompletion> builder)
    {
        builder.ToTable("WorkCompletions");
        builder.HasKey(wc => wc.Id);

        builder.Property(wc => wc.CompletionSummary)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(wc => wc.CompletionEvidenceUrl)
            .HasMaxLength(500);

        builder.Property(wc => wc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(wc => wc.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(wc => wc.CreatedAt);

        builder.HasOne(wc => wc.Assignment)
            .WithOne(a => a.WorkCompletion)
            .HasForeignKey<WorkCompletion>(wc => wc.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wc => wc.ReviewedByAdmin)
            .WithMany(u => u.WorkCompletionsReviewed)
            .HasForeignKey(wc => wc.ReviewedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
