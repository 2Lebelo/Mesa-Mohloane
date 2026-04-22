using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("Incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.IncidentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(i => i.LocationName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Latitude)
            .HasPrecision(9, 6);

        builder.Property(i => i.Longitude)
            .HasPrecision(9, 6);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(i => i.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(i => i.IncidentNumber)
            .IsUnique();

        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.CreatedAt);

        builder.HasOne(i => i.Citizen)
            .WithMany(u => u.IncidentsReported)
            .HasForeignKey(i => i.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.VerifiedByAdmin)
            .WithMany(u => u.IncidentsVerified)
            .HasForeignKey(i => i.VerifiedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Photos)
            .WithOne(p => p.Incident)
            .HasForeignKey(p => p.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.TenderApplications)
            .WithOne(t => t.Incident)
            .HasForeignKey(t => t.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Assignment)
            .WithOne(a => a.Incident)
            .HasForeignKey<Assignment>(a => a.IncidentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
