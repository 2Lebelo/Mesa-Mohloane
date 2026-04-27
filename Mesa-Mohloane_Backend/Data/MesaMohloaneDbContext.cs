using Microsoft.EntityFrameworkCore;
using Mesa_Mohloane_Backend.Models.Entities;

namespace Mesa_Mohloane_Backend.Data;

public class MesaMohloaneDbContext(DbContextOptions<MesaMohloaneDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<ContractorProfile> ContractorProfiles { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<IncidentPhoto> IncidentPhotos { get; set; }
    public DbSet<TenderApplication> TenderApplications { get; set; }
    public DbSet<TenderLineItem> TenderLineItems { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<WorkCompletion> WorkCompletions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ContractorRating> ContractorRatings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesaMohloaneDbContext).Assembly);
    }
}