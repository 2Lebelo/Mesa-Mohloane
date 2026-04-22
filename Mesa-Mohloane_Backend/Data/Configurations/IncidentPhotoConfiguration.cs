using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class IncidentPhotoConfiguration : IEntityTypeConfiguration<IncidentPhoto>
{
    public void Configure(EntityTypeBuilder<IncidentPhoto> builder)
    {
        builder.ToTable("IncidentPhotos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ImageUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.PublicId)
            .HasMaxLength(200);

        builder.Property(p => p.Caption)
            .HasMaxLength(300);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);
    }
}
