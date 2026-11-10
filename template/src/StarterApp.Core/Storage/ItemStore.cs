using Microsoft.EntityFrameworkCore;
using StarterApp.Core.Collecting;

namespace StarterApp.Core.Storage;

/// <summary>Чтение и запись записей. Каждый метод открывает свой короткий контекст — безопасно из любого потока.</summary>
public sealed class ItemStore(IDbContextFactory<AppDbContext> factory, TimeProvider time)
{
    public async Task EnsureCreatedAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Database.EnsureCreatedAsync(ct);
    }

    public async Task<List<Item>> GetAllAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Items.AsNoTracking().ToListAsync(ct);
    }

    public async Task<List<Item>> GetByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        return await db.Items.AsNoTracking().Where(i => ids.Contains(i.Id)).ToListAsync(ct);
    }

    /// <summary>Сохраняет работу пользователя: статус и заметку.</summary>
    public async Task SaveWorkAsync(long id, ItemStatus status, string? note, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var item = await db.Items.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (item is null)
            return;

        if (item.Status != status)
        {
            item.Status = status;
            item.StatusChangedAt = time.GetUtcNow().UtcDateTime;
        }
        item.Note = string.IsNullOrWhiteSpace(note) ? null : note;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Добавляет новые записи и обновляет поля источника у известных.
    /// Статус и заметка не трогаются.
    /// </summary>
    public async Task<UpsertResult> UpsertAsync(string source, IReadOnlyList<ItemDraft> drafts, CancellationToken ct = default)
    {
        if (drafts.Count == 0)
            return new UpsertResult([], []);

        var now = time.GetUtcNow().UtcDateTime;
        var ids = drafts.Select(d => d.ExternalId).Distinct().ToList();

        await using var db = await factory.CreateDbContextAsync(ct);
        var known = await db.Items
            .Where(i => i.Source == source && ids.Contains(i.ExternalId))
            .ToDictionaryAsync(i => i.ExternalId, ct);

        var added = new List<Item>();
        var updated = new List<Item>();
        foreach (var d in drafts.DistinctBy(d => d.ExternalId))
        {
            if (known.TryGetValue(d.ExternalId, out var item))
            {
                if (d.ApplyTo(item))
                {
                    item.UpdatedAt = now;
                    updated.Add(item);
                }
            }
            else
            {
                item = new Item { Source = source, ExternalId = d.ExternalId, FirstSeenAt = now, UpdatedAt = now };
                d.ApplyTo(item);
                db.Items.Add(item);
                added.Add(item);
            }
        }

        await db.SaveChangesAsync(ct);
        return new UpsertResult(added.Select(i => i.Id).ToList(), updated.Select(i => i.Id).ToList());
    }

    /// <summary>Удаляет все собранные данные (кнопка «Собрать всё заново»).</summary>
    public async Task ClearAsync(CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await db.Items.ExecuteDeleteAsync(ct);
    }
}

/// <summary>Итог записи пачки: id новых и обновлённых записей.</summary>
public sealed record UpsertResult(IReadOnlyList<long> AddedIds, IReadOnlyList<long> UpdatedIds);
