using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class ContractorRatingConfiguration : IEntityTypeConfiguration<ContractorRating>
{
    public void Configure(EntityTypeBuilder<ContractorRating> builder)
    {
        builder.ToTable("ContractorRatings");
        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.Comment)
            .HasMaxLength(1000);

        builder.Property(cr => cr.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(cr => cr.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(cr => cr.CreatedAt);

        builder.HasOne(cr => cr.Incident)
            .WithMany(i => i.Ratings)
            .HasForeignKey(cr => cr.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.Assignment)
            .WithOne(a => a.ContractorRating)
            .HasForeignKey<ContractorRating>(cr => cr.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.Citizen)
            .WithMany(u => u.RatingsGiven)
            .HasForeignKey(cr => cr.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cr => cr.Contractor)
            .WithMany()
            .HasForeignKey(cr => cr.ContractorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
