using Microsoft.EntityFrameworkCore;

namespace Discord.Net.Scheduler.EntityFrameworkCore;

public class SchedulerDbContext : DbContext
{
    public DbSet<JobEntity> ScheduledJobs => Set<JobEntity>();

    public SchedulerDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobEntity>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.HasIndex(e => e.NextExecution);
            entity.HasIndex(e => e.Status);
        });
    }
}
