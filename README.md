# NetAppStack

**Стек для настольных программ Windows на C# одной командой.** Фоновый сбор данных, таблица со статусами, логи, настройки, Excel и выпуск одним exe — уже внутри. Остаётся написать свои источники данных и экраны.

```bash
dotnet new install NetAppStack
```

```bash
dotnet new netapp -n MyApp
```

До публикации в NuGet — из папки с репозиторием: `dotnet new install ./template`.

## Как начать

1. `dotnet new netapp -n MyApp` — проект с демо-программой: фоновый сбор по расписанию и по кнопке, таблица «новые сверху» со статусами и заметками, поиск и фильтры, график по дням, логи, настройки, выгрузка в Excel/CSV.
2. `dotnet run --project MyApp/src/MyApp` — запуск. Открыть в Visual Studio / Rider — `MyApp/MyApp.slnx`.
3. Свой источник данных — класс `ICollector` + одна строка регистрации; правила для ИИ-ассистента — `MyApp/AGENTS.md`.
4. `git tag v1.0.0 && git push --tags` — GitHub Actions прогоняет тесты и выкладывает `MyApp.exe` в Releases.

Нужен [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0).

## Опции

| Опция | Значения | По умолчанию | Что даёт |
| --- | --- | --- | --- |
| `--db` | `sqlite` · `postgres` | `sqlite` | база файлом рядом с программой или общая PostgreSQL |
| `--charts` | `true` · `false` | `true` | вкладка «График» (ScottPlot) |
| `--tray` | `true` · `false` | `false` | значок в трее, работа при закрытом окне, уведомления о новом |
| `--updates` | `true` · `false` | `false` | Setup.exe и автообновление из GitHub Releases (Velopack) |
| `--repo` | адрес GitHub | — | ссылки в программе и источник обновлений |
| `--company` | текст | — | автор в свойствах exe |

```bash
dotnet new netapp -n LeadCollector --tray true --updates true --repo https://github.com/me/LeadCollector
```

## Стек

| Задача | Библиотека |
| --- | --- |
| Платформа | [.NET 10](https://dotnet.microsoft.com) · C# · WPF |
| Интерфейс | [WPF-UI](https://wpfui.lepo.co) — Fluent-дизайн Windows 11, Mica, светлая и тёмная тема |
| MVVM | [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — `[ObservableProperty]`, `[RelayCommand]` |
| Каркас | [Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host) — DI, конфигурация, фоновые службы |
| Логи | [Serilog](https://serilog.net) — файлы за 30 дней + вкладка «Логи» |
| База | [EF Core](https://learn.microsoft.com/ef/core/) — SQLite или [PostgreSQL](https://www.npgsql.org) |
| Сеть | HttpClient + [Microsoft.Extensions.Http.Resilience](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) (Polly: повторы, таймауты) |
| Разбор HTML | [AngleSharp](https://anglesharp.github.io) — CSS-селекторы |
| Графики | [ScottPlot](https://scottplot.net) |
| Экспорт | [ClosedXML](https://github.com/ClosedXML/ClosedXML) (Excel) · CSV для русского Excel |
| Трей | [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon) |
| Обновления | [Velopack](https://velopack.io) |
| Тесты | [xUnit v3](https://xunit.net) · [Shouldly](https://docs.shouldly.org) · «золотые» снимки |
| Выпуск | GitHub Actions → один exe без установки .NET |

Все библиотеки бесплатные (MIT / Apache). Сознательно не взяты: FluentAssertions (платный с v8), Verify (требует лицензионное свойство сборки), CsvHelper (для выгрузки хватает 10 строк).

## Сначала ТЗ

`TechnicalSpecification/` — ТЗ-заполнялка в Obsidian (паспорт, источники данных, таблица, функции, выпуск, этапы); есть в каждом новом проекте. Коротко — [PROMT.md](PROMT.md): заполнить и отдать ИИ-ассистенту.

## Разработка стартера

`template/` — обычное рабочее решение со всеми опциями сразу:

```bash
cd template
dotnet test --solution StarterApp.slnx
dotnet run --project src/StarterApp
```

Проверка всех вариантов и пакет — правила в [AGENTS.md](AGENTS.md).

MIT
