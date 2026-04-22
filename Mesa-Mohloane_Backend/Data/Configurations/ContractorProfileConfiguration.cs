using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class ContractorProfileConfiguration : IEntityTypeConfiguration<ContractorProfile>
{
    public void Configure(EntityTypeBuilder<ContractorProfile> builder)
    {
        builder.ToTable("ContractorProfiles");
        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cp => cp.RegistrationNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(cp => cp.TaxNumber)
            .HasMaxLength(100);

        builder.Property(cp => cp.CoverageArea)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cp => cp.AverageRating)
            .HasPrecision(5, 2);

        builder.Property(cp => cp.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(cp => cp.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne(cp => cp.ApprovedByAdmin)
            .WithMany()
            .HasForeignKey(cp => cp.ApprovedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
