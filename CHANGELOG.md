# Changelog

## 1.0.0 — 2026-11-10

Первая версия: шаблон `dotnet new netapp`.

- Программа WPF на .NET 10 с WPF-UI (Fluent, Mica, светлая/тёмная тема как в Windows).
- Фоновый сбор (`CollectorService`): по расписанию и по кнопке, отмена, прогресс, запись пачками, ошибка источника не останавливает остальные; демо-источник и источник «список на веб-странице» (HttpClient + повторы Polly + AngleSharp).
- Таблица: новые сверху, свежие жирным, поиск, фильтр по статусу, строка-итог, статус и заметка в таблице, «Колонки», запоминание порядка/ширины/сортировки.
- Вкладки «График» (ScottPlot) и «Логи» (Serilog: файлы за 30 дней + экран).
- Настройки в `data/settings.json` с атомарной записью; положение окна; одна копия программы.
- Выгрузка в Excel (ClosedXML) и CSV для русского Excel.
- Опции: `--db sqlite|postgres`, `--charts`, `--tray` (H.NotifyIcon), `--updates` (Velopack), `--repo`, `--company`.
- Тесты xUnit v3 + Shouldly + свои «золотые» снимки; GitHub Actions: проверка и выпуск exe по тегу.
- ТЗ-заполнялка `TechnicalSpecification/` (Obsidian) и `PROMT.md`, правила для ИИ `AGENTS.md`.
