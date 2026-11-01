using JobTracker.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data;
public class AppDbContext : IdentityDbContext
{
    // Constructor.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    // Sets.
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    // Settings.
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(FieldLengths.CompanyTitle);
            entity.HasIndex(c => new { c.UserID, c.Name }).IsUnique();
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(FieldLengths.JobTitle);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(FieldLengths.Status);
            entity.Property(j => j.PostingURL).HasMaxLength(FieldLengths.URL);
            entity.Property(j => j.Location).HasMaxLength(FieldLengths.Location);
        });
    }
}