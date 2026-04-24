using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var task = modelBuilder.Entity<TaskItem>();

        task.ToTable("Tasks");
        task.HasKey(x => x.Id);

        task.Property(x => x.Title).IsRequired().HasMaxLength(200);
        task.Property(x => x.Description).HasMaxLength(2000);
        task.Property(x => x.AssignedTo).HasMaxLength(150);
        task.Property(x => x.TenantId).IsRequired().HasMaxLength(100);
        task.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        task.Property(x => x.Priority).HasConversion<string>().HasMaxLength(30);
        task.Property(x => x.CreatedAt).IsRequired();
        task.Property(x => x.UpdatedAt).IsRequired();

        task.HasIndex(x => new { x.TenantId, x.Status });
        task.HasIndex(x => x.DueDate);

        task.HasQueryFilter(x => !x.IsDeleted && x.TenantId == _tenantProvider.TenantId);
    }
}
