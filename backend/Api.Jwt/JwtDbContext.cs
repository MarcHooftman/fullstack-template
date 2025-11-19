using Microsoft.EntityFrameworkCore;
using Api.Jwt;

namespace Api.Jwt;

public class JwtDbContext : DbContext
{
    public JwtDbContext(DbContextOptions<JwtDbContext> options) : base(options) { }

    // DbSets for all entities
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });
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