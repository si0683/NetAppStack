namespace StarterApp.Core.Storage;

/// <summary>
/// Одна запись списка: объект, заявка, товар, игра — что собирает программа.
/// Поля из источника обновляются при каждом сборе, а <see cref="Status"/> и <see cref="Note"/>
/// — работа пользователя, сборщик их не трогает.
/// </summary>
public sealed class Item
{
    public long Id { get; set; }

    /// <summary>Код источника (сборщика), например «demo».</summary>
    public string Source { get; set; } = "";

    /// <summary>Номер записи у источника. Пара Source + ExternalId уникальна.</summary>
    public string ExternalId { get; set; } = "";

    public string Title { get; set; } = "";

    public string? Category { get; set; }

    public string? Location { get; set; }

    public decimal? Amount { get; set; }

    public string? Phone { get; set; }

    public string? Url { get; set; }

    /// <summary>Когда опубликовано у источника (UTC).</summary>
    public DateTime? PublishedAt { get; set; }

    /// <summary>Когда программа увидела запись впервые (UTC).</summary>
    public DateTime FirstSeenAt { get; set; }

    /// <summary>Когда запись последний раз менялась у источника (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    public ItemStatus Status { get; set; } = ItemStatus.New;

    public string? Note { get; set; }

    public DateTime? StatusChangedAt { get; set; }
}

/// <summary>Статус работы с записью. Порядок важен — по нему сортируется колонка.</summary>
public enum ItemStatus
{
    New = 0,
    InWork = 1,
    Later = 2,
    Done = 3,
    Rejected = 4,
}

public static class ItemStatusNames
{
    public static string Title(ItemStatus status) => status switch
    {
        ItemStatus.New => "Новое",
        ItemStatus.InWork => "В работе",
        ItemStatus.Later => "Отложено",
        ItemStatus.Done => "Готово",
        ItemStatus.Rejected => "Отказ",
        _ => status.ToString(),
    };

    /// <summary>Для выпадающих списков: значение + подпись.</summary>
    public static IReadOnlyList<StatusOption> All { get; } =
        Enum.GetValues<ItemStatus>().Select(s => new StatusOption(s, Title(s))).ToList();
}

public sealed record StatusOption(ItemStatus Value, string Title);
