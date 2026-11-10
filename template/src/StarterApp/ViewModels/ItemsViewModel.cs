using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using StarterApp.Core.Collecting;
using StarterApp.Core.Export;
using StarterApp.Core.Settings;
using StarterApp.Core.Storage;

namespace StarterApp.ViewModels;

/// <summary>Вкладка «Данные»: строки, поиск, фильтр по статусу, строка-итог, выгрузка.</summary>
public sealed partial class ItemsViewModel : ObservableObject
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private readonly ItemStore _store;
    private readonly SettingsStore _settings;
    private readonly ILogger<ItemsViewModel> _log;
    private readonly Dictionary<long, ItemRow> _byId = [];

    public ItemsViewModel(ItemStore store, SettingsStore settings, CollectorService collector, ILogger<ItemsViewModel> log)
    {
        _store = store;
        _settings = settings;
        _log = log;

        View = CollectionViewSource.GetDefaultView(Rows);
        View.Filter = Matches;

        StatusFilters = [new StatusFilter("", "Все статусы"), .. ItemStatusNames.All.Select(s => new StatusFilter(s.Value.ToString(), s.Title))];
        _searchText = settings.Current.Grid.Search;
        _statusFilter = StatusFilters.FirstOrDefault(f => f.Key == settings.Current.Grid.Status) ?? StatusFilters[0];

        collector.ItemsChanged += r => Application.Current?.Dispatcher.InvokeAsync(() => MergeAsync(r));
    }

    public ObservableCollection<ItemRow> Rows { get; } = [];

    public ICollectionView View { get; }

    public IReadOnlyList<StatusFilter> StatusFilters { get; }

    /// <summary>Данные изменились (загрузка, новые записи) — для графика.</summary>
    public event Action? DataChanged;

    /// <summary>Ключи видимых колонок в порядке на экране. Задаёт таблица (ItemsView).</summary>
    public Func<IReadOnlyList<string>>? VisibleColumns { get; set; }

    [ObservableProperty]
    private string _searchText;

    [ObservableProperty]
    private StatusFilter _statusFilter;

    [ObservableProperty]
    private string _summary = "";

    partial void OnSearchTextChanged(string value)
    {
        _settings.Current.Grid.Search = value;
        Refresh();
    }

    partial void OnStatusFilterChanged(StatusFilter value)
    {
        _settings.Current.Grid.Status = value.Key;
        Refresh();
    }

    public async Task LoadAsync()
    {
        var items = await _store.GetAllAsync();
        Rows.Clear();
        _byId.Clear();
        foreach (var item in items)
            AddRow(item);
        Refresh();
        DataChanged?.Invoke();
    }

    /// <summary>Сборщик записал пачку: добавить новые строки и обновить изменённые, не сбивая выделение.</summary>
    private async Task MergeAsync(UpsertResult change)
    {
        var ids = change.AddedIds.Concat(change.UpdatedIds).ToList();
        foreach (var item in await _store.GetByIdsAsync(ids))
        {
            if (_byId.TryGetValue(item.Id, out var row))
                row.Update(item);
            else
                AddRow(item);
        }
        Refresh();
        DataChanged?.Invoke();
    }

    private void AddRow(Item item)
    {
        var row = new ItemRow(item, SaveWork);
        _byId[item.Id] = row;
        Rows.Add(row);
    }

    private async void SaveWork(ItemRow row)
    {
        try
        {
            await _store.SaveWorkAsync(row.Id, row.Status, row.Note);
            UpdateSummary();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Не сохранён статус записи {Id}", row.Id);
        }
    }

    private bool Matches(object o)
    {
        var row = (ItemRow)o;
        if (StatusFilter.Key.Length > 0 && row.Status.ToString() != StatusFilter.Key)
            return false;
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        string q = SearchText.Trim();
        return Has(row.Title) || Has(row.Category) || Has(row.Location) || Has(row.Phone) || Has(row.Note);

        bool Has(string? s) => s?.Contains(q, StringComparison.CurrentCultureIgnoreCase) == true;
    }

    public void Refresh()
    {
        View.Refresh();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var shown = View.Cast<ItemRow>().ToList();
        int fresh = shown.Count(r => r.Status == ItemStatus.New);
        decimal sum = shown.Sum(r => r.Amount ?? 0);
        Summary = $"Показано {shown.Count:N0} из {Rows.Count:N0} · новых {fresh:N0}"
            + (sum > 0 ? $" · сумма {sum.ToString("N0", Ru)}" : "");
    }

    /// <summary>Все колонки, которые можно выгрузить: ключ (SortMemberPath колонки) → заголовок и значение.</summary>
    public static IReadOnlyDictionary<string, ExportColumn> ExportColumns { get; } = new Dictionary<string, ExportColumn>
    {
        ["Date"] = new("Дата", r => ((ItemRow)r).Date),
        ["Title"] = new("Название", r => ((ItemRow)r).Title),
        ["Category"] = new("Категория", r => ((ItemRow)r).Category),
        ["Location"] = new("Место", r => ((ItemRow)r).Location),
        ["Amount"] = new("Сумма", r => ((ItemRow)r).Amount),
        ["Phone"] = new("Телефон", r => ((ItemRow)r).Phone),
        ["Status"] = new("Статус", r => ((ItemRow)r).StatusTitle),
        ["Note"] = new("Заметка", r => ((ItemRow)r).Note),
        ["FirstSeen"] = new("Найдено", r => ((ItemRow)r).FirstSeen),
        ["Url"] = new("Ссылка", r => Uri.TryCreate(((ItemRow)r).Url, UriKind.Absolute, out var u) ? u : null),
    };

    /// <summary>Выгрузка строк текущего фильтра в текущем порядке, видимые колонки.</summary>
    public void Export(string path)
    {
        var keys = VisibleColumns?.Invoke() ?? ExportColumns.Keys.ToList();
        var columns = keys.Where(ExportColumns.ContainsKey).Select(k => ExportColumns[k]).ToList();
        var rows = View.Cast<object>().ToList();

        if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            TableExport.WriteCsv(path, columns, rows);
        else
            TableExport.WriteXlsx(path, columns, rows);
        _log.LogInformation("Выгружено {Count} строк: {Path}", rows.Count, path);
    }
}

public sealed record StatusFilter(string Key, string Title);
