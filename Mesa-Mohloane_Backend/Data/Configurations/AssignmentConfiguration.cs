using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(a => a.CreatedAt);

        builder.HasOne(a => a.Incident)
            .WithOne(i => i.Assignment)
            .HasForeignKey<Assignment>(a => a.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.TenderApplication)
            .WithOne(t => t.Assignment)
            .HasForeignKey<Assignment>(a => a.TenderApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Contractor)
            .WithMany(u => u.AssignmentsAsContractor)
            .HasForeignKey(a => a.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AssignedByAdmin)
            .WithMany(u => u.AssignmentsAssigned)
            .HasForeignKey(a => a.AssignedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.WorkCompletion)
            .WithOne(wc => wc.Assignment)
            .HasForeignKey<WorkCompletion>(wc => wc.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Invoice)
            .WithOne(i => i.Assignment)
            .HasForeignKey<Invoice>(i => i.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ContractorRating)
            .WithOne(cr => cr.Assignment)
            .HasForeignKey<ContractorRating>(cr => cr.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
