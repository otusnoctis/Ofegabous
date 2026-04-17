# Ofegabous Template

`Ofegabous` is a base template for local Windows tools built with `.NET MAUI` + `BlazorWebView` and distributed with `Velopack` + `GitHub Releases`.

## What it includes

- Windows-only application shell
- local JSON-based persistence
- an in-app markdown documentation system
- Velopack updates
- tag-driven release automation
- derivable configuration centralized in `Directory.Build.props`

## Documentation

The main documentation no longer lives only in this `README`. The canonical project content is in the [documentation](./documentation) folder, and the app renders it in the `Documentation` section.

Recommended entry points:

- [Overview](./documentation/01-overview.md)
- [Project Structure](./documentation/02-project-structure.md)
- [Runtime Model](./documentation/03-runtime-model.md)
- [Configuration](./documentation/04-configuration.md)
- [Persistence](./documentation/05-persistence.md)
- [Documentation System](./documentation/06-documentation-system.md)
- [Update System](./documentation/07-update-system.md)
- [Release Process](./documentation/08-release-process.md)
- [Deriving An App](./documentation/09-deriving-an-app.md)

## Local development

```powershell
dotnet build .\App\App.csproj
```

The app reads:

- persistent data from `data`
- documentation from `documentation`

In development those folders are used directly from the repo root. In a published runtime, the template distinguishes between bundled content and persistent storage.
