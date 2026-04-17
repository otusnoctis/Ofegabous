using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace App.Services;

public sealed class AppUpdateService
{
    private readonly UpdateStartupState _startupState;
    private readonly string _repositoryUrl;
    private readonly UpdateManager? _updateManager;
    private readonly UpdateLogStore _updateLogStore;
    private Task? _startupCheckTask;
    private UpdateInfo? _availableUpdate;
    private DateTimeOffset? _lastCheckedAt;
    private string _lastUpdateMessage = "Updates have not been checked yet.";

    public AppUpdateService(
        UpdateStartupState startupState,
        TemplateMetadata metadata,
        TemplateEnvironment environment,
        UpdateLogStore updateLogStore)
    {
        _startupState = startupState;
        _repositoryUrl = metadata.RepositoryUrl;
        _updateLogStore = updateLogStore;
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
        return _startupCheckTask ??= EnsureStartupCheckInternalAsync();
    }

    public async Task<AppUpdateSnapshot> CheckForUpdatesAsync(Action<string>? reportProgress = null)
    {
        return await CheckForUpdatesAsync(reportProgress, isStartupCheck: false);
    }

    public async Task<AppUpdateResult> DownloadAndApplyAsync(Action<string> reportProgress)
    {
        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            _availableUpdate = null;
            _lastUpdateMessage = "Development mode or invalid installation: updates are disabled.";
            await LogEventAsync("download_skipped", "Update download skipped because updates are disabled.");
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

            await LogEventAsync("download_started", $"Download started for update {targetVersion}.", targetVersion);
            await _updateManager.DownloadUpdatesAsync(_availableUpdate, _ =>
            {
                reportProgress($"Downloading {targetVersion}...");
            });

            await LogEventAsync("download_completed", $"Download completed for update {targetVersion}.", targetVersion);
            reportProgress($"Update downloaded. Restarting into {targetVersion}...");
            await LogEventAsync("restart_requested", $"Restart requested to apply update {targetVersion}.", targetVersion);

            _updateManager.ApplyUpdatesAndRestart(
                _availableUpdate.TargetFullRelease,
                [
                    "--updated-from", currentVersion,
                    "--updated-to", targetVersion,
                    "--updated-package", _availableUpdate.TargetFullRelease.FileName
                ]);

            _lastUpdateMessage = $"Restart requested to apply update {targetVersion}.";
            var preparedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(preparedSnapshot, string.Empty);
        }
        catch (Exception exception)
        {
            _lastUpdateMessage = $"Could not prepare the update: {exception.Message}";
            await LogEventAsync(
                "download_failed",
                $"Could not apply update: {exception.Message}",
                details: exception.ToString());
            var failedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(failedSnapshot, exception.Message);
        }
    }

    private async Task EnsureStartupCheckInternalAsync()
    {
        await LogStartupStateAsync();

        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            return;
        }

        await CheckForUpdatesAsync(reportProgress: null, isStartupCheck: true);
    }

    private async Task<AppUpdateSnapshot> CheckForUpdatesAsync(Action<string>? reportProgress, bool isStartupCheck)
    {
        if (IsDevMode || _updateManager is null || !_updateManager.IsInstalled)
        {
            _availableUpdate = null;
            _lastCheckedAt = null;
            _lastUpdateMessage = "Development mode or invalid installation: updates are disabled.";
            await LogEventAsync(
                "check_skipped",
                isStartupCheck
                    ? "Automatic startup update check skipped because updates are disabled."
                    : "Manual update check skipped because updates are disabled.");
            var disabledSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return disabledSnapshot;
        }

        try
        {
            await LogEventAsync(
                "check_started",
                isStartupCheck
                    ? "Automatic startup update check started."
                    : "Manual update check started.");
            reportProgress?.Invoke("Checking for updates...");
            var updates = await _updateManager.CheckForUpdatesAsync();
            _lastCheckedAt = DateTimeOffset.Now;

            if (updates is null)
            {
                _availableUpdate = null;
                _lastUpdateMessage = "The application is up to date.";
                await LogEventAsync(
                    "check_completed",
                    isStartupCheck
                        ? "Automatic startup update check completed. No updates are available."
                        : "Update check completed. No updates are available.");
                var upToDateSnapshot = GetSnapshot();
                SnapshotChanged?.Invoke();
                return upToDateSnapshot;
            }

            _availableUpdate = updates;
            _lastUpdateMessage = $"An update is available: {_availableUpdate.TargetFullRelease.Version}.";
            await LogEventAsync(
                "update_available",
                $"Update available: {_availableUpdate.TargetFullRelease.Version}.",
                _availableUpdate.TargetFullRelease.Version.ToString());
            var availableSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return availableSnapshot;
        }
        catch (Exception exception)
        {
            _availableUpdate = null;
            _lastCheckedAt = DateTimeOffset.Now;
            _lastUpdateMessage = $"Could not check for updates: {exception.Message}";
            await LogEventAsync(
                "check_failed",
                $"Update check failed: {exception.Message}",
                details: exception.ToString());
            var failedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return failedSnapshot;
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
            return "Development mode: this local build will not check for real updates.";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.UpdatedFromVersion) &&
            !string.IsNullOrWhiteSpace(_startupState.UpdatedToVersion))
        {
            var packageText = string.IsNullOrWhiteSpace(_startupState.UpdatedPackage)
                ? string.Empty
                : $" Applied package: {_startupState.UpdatedPackage}.";
            return $"Updated successfully from {_startupState.UpdatedFromVersion} to {_startupState.UpdatedToVersion}.{packageText}";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.RestartedVersion))
        {
            return $"The application restarted after an update. Current version: {_startupState.RestartedVersion}.";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.FirstRunVersion))
        {
            return $"First launch after installation. Installed version: {_startupState.FirstRunVersion}.";
        }

        return $"Active installation. Current version: {appVersion}.";
    }

    private async Task LogStartupStateAsync()
    {
        var appVersion = GetSnapshot().AppVersion;

        if (IsDevMode)
        {
            await LogEventAsync("startup", "Application started in development mode.", appVersion);
            return;
        }

        if (_updateManager?.IsInstalled != true)
        {
            await LogEventAsync("startup", "Application started without a valid Velopack installation. Updates are disabled.", appVersion);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_startupState.UpdatedFromVersion) &&
            !string.IsNullOrWhiteSpace(_startupState.UpdatedToVersion))
        {
            await LogEventAsync(
                "startup_after_update",
                $"Application started after updating from {_startupState.UpdatedFromVersion} to {_startupState.UpdatedToVersion}.",
                _startupState.UpdatedToVersion,
                _startupState.UpdatedPackage);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_startupState.RestartedVersion))
        {
            await LogEventAsync(
                "startup",
                $"Application restarted. Current version: {_startupState.RestartedVersion}.",
                _startupState.RestartedVersion);
            return;
        }

        await LogEventAsync("startup", $"Application started. Current version: {appVersion}.", appVersion);
    }

    private Task LogEventAsync(string eventType, string message, string? version = null, string? details = null)
    {
        return _updateLogStore.AppendAsync(new UpdateLogEntry(
            DateTimeOffset.UtcNow,
            eventType,
            message,
            version,
            details));
    }
}
