using CommunityToolkit.Mvvm.ComponentModel;
using StarterApp.Core.Storage;

namespace StarterApp.ViewModels;

/// <summary>Строка таблицы. Статус и заметка редактируются в таблице и сразу сохраняются в базу.</summary>
public sealed partial class ItemRow : ObservableObject
{
    private readonly Action<ItemRow> _save;
    private Item _item;

    public ItemRow(Item item, Action<ItemRow> save)
    {
        _item = item;
        _save = save;
        _status = item.Status;
        _note = item.Note;
    }

    public long Id => _item.Id;

    /// <summary>Дата для колонки и сортировки «новые сверху»: дата публикации, а если её нет — когда увидели.</summary>
    public DateTime Date => (_item.PublishedAt ?? _item.FirstSeenAt).ToLocalTime();

    public DateTime FirstSeen => _item.FirstSeenAt.ToLocalTime();

    /// <summary>Появилась за последние сутки — строка жирная.</summary>
    public bool IsFresh => DateTime.UtcNow - _item.FirstSeenAt < TimeSpan.FromDays(1);

    public string Source => _item.Source;
    public string Title => _item.Title;
    public string? Category => _item.Category;
    public string? Location => _item.Location;
    public decimal? Amount => _item.Amount;
    public string? Phone => _item.Phone;
    public string? Url => _item.Url;

    public string StatusTitle => ItemStatusNames.Title(Status);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusTitle))]
    private ItemStatus _status;

    [ObservableProperty]
    private string? _note;

    partial void OnStatusChanged(ItemStatus value) => _save(this);

    partial void OnNoteChanged(string? value) => _save(this);

    /// <summary>Новые данные от источника. Работа пользователя (статус, заметка) остаётся.</summary>
    public void Update(Item item)
    {
        _item = item;
        OnPropertyChanged(string.Empty);
    }
}
