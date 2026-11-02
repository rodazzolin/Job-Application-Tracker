using JobTracker.Models;
using Microsoft.AspNetCore.Identity;
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
    public DbSet<JobApplicationStatusChange> StatusChanges => Set<JobApplicationStatusChange>();

    // Settings.
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(FieldLengths.CompanyTitle);
            entity.HasIndex(c => new { c.UserID, c.Name }).IsUnique();
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(c => c.UserID);
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(FieldLengths.JobTitle);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(FieldLengths.Status);
            entity.Property(j => j.PostingURL).HasMaxLength(FieldLengths.URL);
            entity.Property(j => j.Location).HasMaxLength(FieldLengths.Location);
            entity.Property(j => j.Notes).HasMaxLength(FieldLengths.Notes);
            entity.HasOne<IdentityUser>().WithMany().HasForeignKey(j => j.UserID).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<JobApplicationStatusChange>(entity =>
        {
           entity.Property(s => s.OldStatus).HasConversion<string>().HasMaxLength(FieldLengths.Status); 
           entity.Property(s => s.NewStatus).HasConversion<string>().HasMaxLength(FieldLengths.Status); 
        });
    }
}