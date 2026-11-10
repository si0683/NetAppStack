# AGENTS.md — правила для ИИ-агентов

Настольная программа для Windows на стеке NetAppStack: **.NET 10 · WPF · WPF-UI (Fluent) · CommunityToolkit.Mvvm · Microsoft.Extensions.Hosting (DI, фоновые службы) · Serilog · EF Core · AngleSharp · ClosedXML · xUnit v3 + Shouldly**.

## Перед работой

1. `README.md` — что есть и как устроено.
2. `TechnicalSpecification/` — ТЗ (начни с `README.md` и `00 Паспорт.md`). Изменилось решение — обнови ТЗ и `09 Решения и вопросы.md`.
3. Этот файл.

## Команды

```bash
dotnet build StarterApp.slnx
dotnet test --solution StarterApp.slnx
dotnet run --project src/StarterApp
dotnet publish src/StarterApp -c Release -o publish   # один exe
```

Перед сборкой закрой запущенную программу (`taskkill /F /IM StarterApp.exe`) — иначе exe занят.

## Слои

- `StarterApp.Core` — вся логика, без ссылок на WPF. Всё, что можно проверить тестом, пиши здесь.
- `StarterApp` — только окно: XAML, ViewModels, сервисы оболочки (тема, трей, обновления).
- `tests` — тестируют Core. Окно не тестируется — проверяй запуском.

## Как добавлять

- **Источник данных**: класс `ICollector` в `Core/Collecting/` (`Source` — латиница, не меняется; `CollectAsync` отдаёт `ItemDraft` через `yield return`, отчитывается `context.Report(...)`, уважает `ct`). Регистрация — `services.AddSingleton<ICollector, …>()` в `CoreServices.cs`. HTTP — только через `IHttpClientFactory` (именованный клиент с `AddStandardResilienceHandler`: повторы, таймауты). Разбор HTML/JSON — отдельным статическим методом + тест на сохранённой странице в `tests/…/Fixtures`.
- **Поле записи**: `Item` + `ItemDraft` (+ сравнение в `ApplyTo`) → колонка в `ItemsView.xaml` (`SortMemberPath` = ключ) → `ItemsViewModel.ExportColumns`.
- **Настройка**: свойство в `AppSettings` со значением по умолчанию → `SettingsViewModel` (поле, `Load`, `Save`) → строка в `SettingsWindow.xaml`.
- **Вкладка**: `UserControl` в `Views/` + `ViewModel` в `ViewModels/` (регистрация в `AppHost.cs`) + `TabItem` в `MainWindow.xaml`.
- **Сервис**: регистрация в `AppHost.cs` (окно) или `CoreServices.cs` (ядро). Зависимости — через конструктор.

## Правила

- MVVM: свойства — `[ObservableProperty]`, команды — `[RelayCommand]`. Код в code-behind — только то, что нельзя привязкой (колонки таблицы, фокус, диалоги).
- События сборщика приходят из фонового потока → в интерфейс через `Application.Current.Dispatcher.InvokeAsync`.
- Стили WPF-UI: свой стиль для стандартного элемента — только `BasedOn="{StaticResource Default…Style}"` (например `DefaultDataGridRowStyle`, `DefaultListBoxItemStyle`), иначе элемент теряет тему (белый фон в тёмной теме). Цвета — `DynamicResource` из темы (`TextFillColorSecondaryBrush` и т. п.), не жёсткие.
- Время в базе — UTC (`TimeProvider`, не `DateTime.Now`), на экране — `ToLocalTime()`.
- Логи: `ILogger<T>` с шаблонами `{Name}`, по-русски, понятно пользователю: что случилось и что делать. Ошибка источника — `LogError` с исключением.
- Тексты интерфейса и комментарии — по-русски, имена в коде — по-английски.
- Версии пакетов — только в `Directory.Packages.props`. Пакеты с платной лицензией или «спонсорскими» проверками сборки не добавлять.
- Минимальные изменения; сохраняй стиль файла.

## База

Сейчас `EnsureCreated`: таблицы создаются при первом запуске, но **новые поля в существующую базу не добавляются**. Пока данные можно собрать заново — удали `data/app.db` (или «⋯ → Собрать всё заново»). Когда появятся ценные ручные данные — перейди на миграции:

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
dotnet ef migrations add Initial --project src/StarterApp.Core --startup-project src/StarterApp
```

и замени `EnsureCreatedAsync` на `Database.MigrateAsync` в `ItemStore`.

## Тесты

- Новая логика в Core — с тестом. База в тестах — SQLite в памяти (`TestDb`), часы — `TestClock`.
- «Золотые» снимки: `Snapshot.Match(result)` → `tests/…/Snapshots/*.verified.json`. Изменение намеренное — проверь `*.received.json` и переименуй в `.verified.json`.

## Чек-лист перед ответом

- [ ] `dotnet build StarterApp.slnx` без ошибок.
- [ ] `dotnet test --solution StarterApp.slnx` — всё зелёное.
- [ ] Изменения окна проверены запуском (`dotnet run --project src/StarterApp`).
- [ ] README / ТЗ обновлены, если поменялось поведение.
- [ ] git commit / push — только с разрешения пользователя.
