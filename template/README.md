# StarterApp

Программа для Windows: собирает данные в фоне, показывает их таблицей (новые сверху), даёт отмечать статус и заметки, выгружает в Excel. Выпускается одним exe без установки .NET.

Создано из стартера [NetAppStack](https://github.com/si0683/NetAppStack).

## Запуск

Нужен [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet run --project src/StarterApp
```

```bash
dotnet test --solution StarterApp.slnx
```

Открыть в Visual Studio 2026 / Rider — файл `StarterApp.slnx`.

## Что уже есть

- **Фоновый сбор** — сразу после запуска и по расписанию; «Обновить сейчас» (F5) / «Отмена»; прогресс над таблицей; новые записи появляются по ходу сбора. Ошибка одного источника не останавливает остальные.
- **Таблица** — новые сверху, свежие за сутки жирным; поиск, фильтр по статусу, строка-итог; сортировка по колонкам (Shift — вторая колонка); «Колонки» — скрыть лишние; порядок, ширина и сортировка запоминаются.
- **Работа с записью** — статус (Новое / В работе / Отложено / Готово / Отказ) и заметка прямо в таблице; сборщик их не затирает.
- **Выгрузка** — «⋯ → В Excel» (Ctrl+E) и «В CSV»: строки фильтра в порядке на экране, видимые колонки.
- **Логи** — вкладка «Логи» (ошибки считаются в заголовке) + файлы `data/logs` за 30 дней.
- **Настройки** — `data/settings.json`, окно «Настройки»; тема как в Windows / светлая / тёмная.
- **Всё рядом с exe** — папка `data`: база, настройки, логи, выгрузки. Одна копия программы: повторный запуск показывает уже открытое окно.

## Устройство

```
src/StarterApp.Core/          логика без окна — тестируется
  Collecting/                 источники данных: ICollector, CollectorService (фон), Demo, WebList
  Storage/                    база: Item, AppDbContext (EF Core), ItemStore
  Settings/                   AppSettings + SettingsStore (JSON)
  Export/                     Excel (ClosedXML) и CSV
  CoreServices.cs             регистрация ядра (сюда — новые источники)
src/StarterApp/               окно WPF (WPF-UI, MVVM Toolkit)
  Program.cs, AppHost.cs      запуск, DI, логи Serilog
  ViewModels/  Views/         вкладки
tests/StarterApp.Tests/       xUnit v3 + Shouldly, снимки в Snapshots/
TechnicalSpecification/       ТЗ в Obsidian
```

## Как сделать свою программу

1. Заполните ТЗ в `TechnicalSpecification/` (или `PROMT.md`) и отдайте ИИ-ассистенту — правила для него в `AGENTS.md`.
2. Свой источник — класс `ICollector` в `src/StarterApp.Core/Collecting/` + строка в `CoreServices.cs`. Образец — `WebListCollector` (HTTP + разбор HTML), тест разбора — `ParsingAndExportTests`.
3. Свои поля — свойства в `Item` + `ItemDraft` + колонка в `Views/ItemsView.xaml` + ключ в `ItemsViewModel.ExportColumns`.
4. Демо выключается в «Настройках» или удалением `DemoCollector`.

## Выпуск

```bash
git tag v1.0.0
```

```bash
git push --tags
```

GitHub Actions (`.github/workflows/release.yml`) прогонит тесты и выложит `StarterApp.exe` в Releases. Локально: `dotnet publish src/StarterApp -c Release -o publish`.
