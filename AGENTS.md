# AGENTS.md — rules for AI coding agents

NetAppStack is a **project starter** for Windows desktop apps: a `dotnet new` template package (`NetAppStack.csproj`, `PackageType=Template`) that ships `template/` — a WPF solution on .NET 10 (WPF-UI, CommunityToolkit.Mvvm, Generic Host, Serilog, EF Core, AngleSharp, ClosedXML, ScottPlot, H.NotifyIcon, Velopack, xUnit v3 + Shouldly). Same family as LightWebsiteUi, GameMonsterJS2D/3D and AppJS.

- `template/.template.config/template.json` — the template: `shortName: netapp`, `sourceName: StarterApp` (every `StarterApp` in names, paths and text becomes the project name — never write `StarterApp` where the literal must survive), options `db` (`sqlite`|`postgres` → computed `sqlite`/`postgres`), `charts`, `tray`, `updates` (bool), `repo` (replaces `https://github.com/OWNER/REPO`), `company` (replaces `COMPANY_NAME`), `appGuid` (replaces the single-instance GUID).
- Conditional code: C# `#if (charts)`, XML/XAML/csproj `<!--#if (charts) -->` … `<!--#endif -->`. Option-only files are excluded by `sources.modifiers` in template.json; `release-velopack.yml` is renamed to `release.yml` when `updates`.
- `template/` is a **working app with every option on**: `Directory.Build.props` defines `sqlite;charts;tray;updates` inside `<!--#if (false) -->` (dropped from generated projects). The `postgres` branch is not compiled in place — it is checked by generating a project.
- Work on the template in place: `cd template && dotnet test --solution StarterApp.slnx && dotnet run --project src/StarterApp`. `template/AGENTS.md` (Russian) is the guide for agents in the user's project — keep it in sync with the code. UI texts and code comments in the template are Russian; English here.
- Versions: only in `template/Directory.Packages.props`. Stay on the .NET 10 line (`Microsoft.*` 10.0.x) until the template moves to a new SDK. No packages with paid licenses or build-time license checks (FluentAssertions ≥ 8, Verify ≥ 33 were rejected). Tests run on Microsoft.Testing.Platform (`template/global.json`), so no `Microsoft.NET.Test.Sdk`.
- WPF-UI pitfall: custom styles for stock controls must be `BasedOn="{StaticResource Default…Style}"` (e.g. `DefaultDataGridRowStyle`, `DefaultListBoxItemStyle`, `DefaultDataGridTextElementStyle`), otherwise the control falls back to the Aero style (white rows in the dark theme). Plain `ListView` is not themed — use `ListBox`.
- `TechnicalSpecification/` (Obsidian vault) and `PROMT.md` live in the root and in `template/` — keep both copies identical (`diff -r TechnicalSpecification template/TechnicalSpecification`).
- `index.html` in the root is the GitHub Pages page: a short install guide and the stack table, one screen without scrolling, Inter font.

## Release check

1. `cd template && dotnet test --solution StarterApp.slnx` — green; run the app and look at every tab.
2. Install into an isolated hive and generate every variant into a temp folder, then `dotnet build` each and `dotnet test` the default one:

   ```bash
   dotnet new install ./template --debug:custom-hive "$TMP/hive"
   dotnet new netapp -n DefaultApp -o "$TMP/v1" --debug:custom-hive "$TMP/hive"
   dotnet new netapp -n PgApp -o "$TMP/v2" --db postgres --charts false --debug:custom-hive "$TMP/hive"
   dotnet new netapp -n FullApp -o "$TMP/v3" --tray true --updates true --repo https://github.com/me/FullApp --debug:custom-hive "$TMP/hive"
   ```

   Also grep the generated projects for leftovers: `#if`, `StarterApp`, `OWNER/REPO`, `COMPANY_NAME`.
3. `dotnet pack NetAppStack.csproj -c Release -o nupkg` and check the `.nupkg` contains `content/.template.config/template.json` and no `bin/`/`obj/`.
4. Version: `<Version>` in `NetAppStack.csproj` + `CHANGELOG.md` + tag `vX.Y.Z`.
