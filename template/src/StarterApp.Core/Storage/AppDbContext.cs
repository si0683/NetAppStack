using Microsoft.EntityFrameworkCore;

namespace StarterApp.Core.Storage;

/// <summary>
/// База программы. Таблицы создаются при первом запуске (EnsureCreated).
/// Когда в базе появятся ценные ручные данные и схема начнёт меняться — переходите
/// на миграции (см. AGENTS.md → «База»).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Item>(e =>
        {
            e.HasIndex(x => new { x.Source, x.ExternalId }).IsUnique();
            e.HasIndex(x => x.FirstSeenAt);
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.ExternalId).HasMaxLength(256);
            e.Property(x => x.Title).HasMaxLength(1000);
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });
    }
}
