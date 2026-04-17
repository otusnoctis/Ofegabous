# Update System

The template uses `Velopack` to check, download, and apply updates distributed through `GitHub Releases`.

## Main components

- `UpdateStartupState`
- `AppUpdateService`
- `UpdateLogStore`
- the `Settings` page
- the notification badge in the menu

## Update source

The app takes the repository URL from assembly metadata, which in turn is fed from:

- `TemplateRepositoryUrl`

The Velopack dependency version is also intentionally centralized in `Directory.Build.props` through `VelopackVersion`. The project file contains a build guard so Velopack is not updated ad hoc in the `.csproj`.

## Update flow

1. The app starts.
2. If it is not in `dev mode` and the installation is valid, it checks for updates.
3. If there is a new version, `Settings` displays it and the menu highlights the section.
4. The user can download and apply the new version.
5. Velopack restarts the app with arguments that describe the applied update.

## Update log

The template stores update-related events in:

- `logs/update-log.json`

The log is intentionally:

- JSON-based
- file-backed
- easy to inspect directly
- separate from the sample persistence document

The `Settings` page can display the most recent entries without needing a database.

## Visible state

The `Settings` page shows:

- app version
- Velopack version
- update repository
- latest check state
- recent update log entries

## Important decision

The template does not mix this information into `Home`. Version and update diagnostics live in `Settings`, where they are more maintainable and more reusable.
