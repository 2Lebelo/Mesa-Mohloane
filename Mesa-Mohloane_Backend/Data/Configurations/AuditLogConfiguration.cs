using Mesa_Mohloane_Backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mesa_Mohloane_Backend.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ActionType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.OldValuesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewValuesJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(100);

        builder.Property(a => a.Notes)
            .HasMaxLength(2000);

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.IsDeleted)
            .HasDefaultValue(false);

        builder.HasIndex(a => a.ActionAt);

        builder.HasOne(a => a.ActorUser)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
