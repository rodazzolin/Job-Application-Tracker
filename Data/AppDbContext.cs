using JobTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data;
public class AppDbContext : DbContext
{
    private const int MAX_NAME_LENGTH = 200;
    private const int MAX_STATUS_LENGTH = 20;

    // Constructor.
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    // Sets.
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    // Settings.
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(MAX_NAME_LENGTH);
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(MAX_NAME_LENGTH);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(MAX_STATUS_LENGTH);
        });
    }
}