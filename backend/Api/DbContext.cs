using Microsoft.EntityFrameworkCore;
using Api.Models;
using Api.Models.Enums;
using Api.DTOs;

namespace Api;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base(options) { }

    // DbSets for all entities
    public DbSet<UserDto> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=postgres;Port=5432;Database=roosterdb;Username=postgres;Password=postgres");
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not null &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = entry.Entity;
            var entityType = entity.GetType();

            var updatedAtProperty = entityType.GetProperty("UpdatedAt");
            if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(DateTime))
            {
                updatedAtProperty.SetValue(entity, DateTime.UtcNow);
            }

            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entityType.GetProperty("CreatedAt");
                if (createdAtProperty != null && createdAtProperty.PropertyType == typeof(DateTime))
                {
                    var currentValue = createdAtProperty.GetValue(entity);
                    if (currentValue == null || (DateTime)currentValue == default)
                    {
                        createdAtProperty.SetValue(entity, DateTime.UtcNow);
                    }
                }
            }
        }
    }
}