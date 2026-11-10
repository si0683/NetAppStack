using StarterApp.Core.Storage;

namespace StarterApp.Core.Collecting;

/// <summary>Запись в том виде, как её отдал источник. Сборщик возвращает черновики, база решает — новая или обновлённая.</summary>
public sealed record ItemDraft
{
    public required string ExternalId { get; init; }
    public required string Title { get; init; }
    public string? Category { get; init; }
    public string? Location { get; init; }
    public decimal? Amount { get; init; }
    public string? Phone { get; init; }
    public string? Url { get; init; }
    public DateTime? PublishedAt { get; init; }

    /// <summary>Переносит поля в запись. Возвращает true, если что-то изменилось.</summary>
    internal bool ApplyTo(Item item)
    {
        bool changed = item.Title != Title
            || item.Category != Category
            || item.Location != Location
            || item.Amount != Amount
            || item.Phone != Phone
            || item.Url != Url
            || item.PublishedAt != PublishedAt;

        item.Title = Title;
        item.Category = Category;
        item.Location = Location;
        item.Amount = Amount;
        item.Phone = Phone;
        item.Url = Url;
        item.PublishedAt = PublishedAt;
        return changed;
    }
}
