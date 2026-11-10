using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StarterApp.Core.Storage;

namespace StarterApp.Tests;

/// <summary>База SQLite в памяти на один тест: живёт, пока открыто соединение.</summary>
public sealed class TestDb : IDbContextFactory<AppDbContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<AppDbContext> _options;

    public TestDb()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

/// <summary>Часы, которые двигает тест.</summary>
public sealed class TestClock(DateTime utc) : TimeProvider
{
    public DateTime Now { get; set; } = utc;

    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
}
