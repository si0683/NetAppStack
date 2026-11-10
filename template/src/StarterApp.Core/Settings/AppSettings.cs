namespace StarterApp.Core.Settings;

/// <summary>
/// Все настройки программы. Хранятся в data/settings.json.
/// Новое поле — просто добавьте свойство со значением по умолчанию: старые файлы прочитаются.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Как часто собирать данные, минут.</summary>
    public int UpdateIntervalMinutes { get; set; } = 60;

    /// <summary>Тема: system / light / dark.</summary>
    public string Theme { get; set; } = "system";

    /// <summary>Демо-источник: придумывает записи, чтобы было что показать.</summary>
    public bool DemoEnabled { get; set; } = true;

    /// <summary>Источник «список на веб-странице» (пустой адрес — выключен).</summary>
    public WebListSource WebList { get; set; } = new();
#if (postgres)

    /// <summary>Подключение к PostgreSQL.</summary>
    public string PostgresConnection { get; set; } = "Host=localhost;Port=5432;Database=starterapp;Username=postgres;Password=postgres";
#endif
#if (tray)

    /// <summary>Крестик сворачивает в трей, а не закрывает программу.</summary>
    public bool MinimizeToTray { get; set; } = true;
#endif
#if (updates)

    /// <summary>Проверять обновления при запуске.</summary>
    public bool CheckUpdates { get; set; } = true;
#endif

    /// <summary>Вид таблицы: фильтры, сортировка, колонки. Сохраняется сам.</summary>
    public GridState Grid { get; set; } = new();

    /// <summary>Размер и положение окна.</summary>
    public WindowPlacement Window { get; set; } = new();
}

/// <summary>Настройки разбора веб-страницы со списком (CSS-селекторы).</summary>
public sealed class WebListSource
{
    public string Url { get; set; } = "";

    /// <summary>Один элемент списка, например «article» или «.item».</summary>
    public string ItemSelector { get; set; } = "article";

    /// <summary>Внутри элемента: ссылка, её текст — название.</summary>
    public string LinkSelector { get; set; } = "a";

    /// <summary>Внутри элемента: дата (атрибут datetime или текст). Пусто — не берём.</summary>
    public string DateSelector { get; set; } = "time";

    /// <summary>Внутри элемента: категория. Пусто — не берём.</summary>
    public string CategorySelector { get; set; } = "";
}

public sealed class GridState
{
    public string Search { get; set; } = "";

    /// <summary>Фильтр по статусу: пусто — все.</summary>
    public string Status { get; set; } = "";

    public string? SortColumn { get; set; }

    public bool SortDescending { get; set; } = true;

    public List<ColumnState> Columns { get; set; } = [];
}

public sealed class ColumnState
{
    public string Key { get; set; } = "";
    public int DisplayIndex { get; set; }
    public double Width { get; set; }
    public bool Visible { get; set; } = true;
}

public sealed class WindowPlacement
{
    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public double Width { get; set; } = 1280;
    public double Height { get; set; } = 780;
    public bool Maximized { get; set; }

    /// <summary>Открытая вкладка.</summary>
    public int Tab { get; set; }
}
