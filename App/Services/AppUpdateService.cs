using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace App.Services;

public sealed class AppUpdateService
{
    private readonly UpdateStartupState _startupState;
    private readonly string _repositoryUrl;
    private readonly UpdateManager? _updateManager;
    private Task? _startupCheckTask;
    private UpdateInfo? _availableUpdate;
    private DateTimeOffset? _lastCheckedAt;
    private string _lastUpdateMessage = "Aun no se han consultado actualizaciones.";

    public AppUpdateService(UpdateStartupState startupState, TemplateMetadata metadata, TemplateEnvironment environment)
    {
        _startupState = startupState;
        _repositoryUrl = metadata.RepositoryUrl;
        IsDevMode = environment.IsDevelopmentMode;

        if (!IsDevMode)
        {
            var source = new GithubSource(_repositoryUrl, "", false, null!);
            _updateManager = new UpdateManager(source);
        }
    }

    public bool IsDevMode { get; }
    public event Action? SnapshotChanged;

    public AppUpdateSnapshot GetSnapshot(string? overrideStatus = null)
    {
        var appVersion = IsDevMode
            ? "x.x.x-dev"
            : _updateManager?.CurrentVersion?.ToString() ?? GetAssemblyVersion();

        return new AppUpdateSnapshot(
            appVersion,
            VelopackRuntimeInfo.VelopackNugetVersion.ToString(),
            _repositoryUrl,
            IsDevMode,
            _updateManager?.IsInstalled == true,
            !IsDevMode && _updateManager?.IsInstalled == true,
            BuildStartupStatus(appVersion),
            overrideStatus ?? _lastUpdateMessage,
            _availableUpdate is not null,
            _availableUpdate?.TargetFullRelease.Version.ToString(),
            _lastCheckedAt);
    }

    public Task EnsureStartupCheckAsync()
    {
        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            return Task.CompletedTask;
        }

        return _startupCheckTask ??= CheckForUpdatesAsync();
    }

    public async Task<AppUpdateSnapshot> CheckForUpdatesAsync(Action<string>? reportProgress = null)
    {
        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            _availableUpdate = null;
            _lastCheckedAt = null;
            _lastUpdateMessage = "Development mode o instalacion no valida: actualizaciones deshabilitadas.";
            var disabledSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return disabledSnapshot;
        }

        try
        {
            reportProgress?.Invoke("Comprobando actualizaciones...");
            var updates = await _updateManager.CheckForUpdatesAsync();
            _lastCheckedAt = DateTimeOffset.Now;

            if (updates is null)
            {
                _availableUpdate = null;
                _lastUpdateMessage = "La aplicacion esta al dia.";
                var upToDateSnapshot = GetSnapshot();
                SnapshotChanged?.Invoke();
                return upToDateSnapshot;
            }

            _availableUpdate = updates;
            _lastUpdateMessage = $"Hay una actualizacion disponible a {_availableUpdate.TargetFullRelease.Version}.";
            var availableSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return availableSnapshot;
        }
        catch (Exception exception)
        {
            _availableUpdate = null;
            _lastCheckedAt = DateTimeOffset.Now;
            _lastUpdateMessage = $"No se pudieron comprobar las actualizaciones: {exception.Message}";
            var failedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return failedSnapshot;
        }
    }

    public async Task<AppUpdateResult> DownloadAndApplyAsync(Action<string> reportProgress)
    {
        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            _availableUpdate = null;
            _lastUpdateMessage = "Development mode o instalacion no valida: actualizaciones deshabilitadas.";
            var disabledSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(disabledSnapshot, string.Empty);
        }

        if (_availableUpdate is null)
        {
            var checkedSnapshot = await CheckForUpdatesAsync(reportProgress);
            if (_availableUpdate is null)
            {
                return new AppUpdateResult(checkedSnapshot, string.Empty);
            }
        }

        try
        {
            var currentVersion = _updateManager.CurrentVersion?.ToString() ?? GetAssemblyVersion();
            var targetVersion = _availableUpdate.TargetFullRelease.Version.ToString();

            await _updateManager.DownloadUpdatesAsync(_availableUpdate, progress =>
            {
                reportProgress($"Descargando {targetVersion}... {progress}%");
            });

            reportProgress($"Actualizacion descargada. Reiniciando hacia {targetVersion}...");

            _updateManager.ApplyUpdatesAndRestart(
                _availableUpdate.TargetFullRelease,
                [
                    "--updated-from", currentVersion,
                    "--updated-to", targetVersion,
                    "--updated-package", _availableUpdate.TargetFullRelease.FileName
                ]);

            _lastUpdateMessage = $"La actualizacion a {targetVersion} se ha preparado para reinicio.";
            var preparedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(preparedSnapshot, string.Empty);
        }
        catch (Exception exception)
        {
            _lastUpdateMessage = $"No se pudo preparar la actualizacion: {exception.Message}";
            var failedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(failedSnapshot, exception.Message);
        }
    }

    private static string GetAssemblyVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
    }

    private string BuildStartupStatus(string appVersion)
    {
        if (IsDevMode)
        {
            return "Development mode: esta build local no comprobara actualizaciones reales.";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.UpdatedFromVersion) &&
            !string.IsNullOrWhiteSpace(_startupState.UpdatedToVersion))
        {
            var packageText = string.IsNullOrWhiteSpace(_startupState.UpdatedPackage)
                ? string.Empty
                : $" Paquete aplicado: {_startupState.UpdatedPackage}.";
            return $"Actualizada correctamente desde {_startupState.UpdatedFromVersion} hasta {_startupState.UpdatedToVersion}.{packageText}";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.FirstRunVersion))
        {
            return $"Primer arranque tras instalacion. Version instalada: {_startupState.FirstRunVersion}.";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.RestartedVersion))
        {
            return $"La aplicacion se ha reiniciado tras una actualizacion. Version actual: {_startupState.RestartedVersion}.";
        }

        return $"Instalacion activa. Version actual: {appVersion}.";
    }
}
