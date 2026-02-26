using Microsoft.EntityFrameworkCore;
using OneIncTask.Domain.Entities;

namespace OneIncTask.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<Job> Jobs { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
