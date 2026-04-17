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
    private string _lastUpdateMessage = "Updates have not been checked yet.";

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
            _lastUpdateMessage = "Development mode or invalid installation: updates are disabled.";
            var disabledSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return disabledSnapshot;
        }

        try
        {
            reportProgress?.Invoke("Checking for updates...");
            var updates = await _updateManager.CheckForUpdatesAsync();
            _lastCheckedAt = DateTimeOffset.Now;

            if (updates is null)
            {
                _availableUpdate = null;
                _lastUpdateMessage = "The application is up to date.";
                var upToDateSnapshot = GetSnapshot();
                SnapshotChanged?.Invoke();
                return upToDateSnapshot;
            }

            _availableUpdate = updates;
            _lastUpdateMessage = $"An update is available: {_availableUpdate.TargetFullRelease.Version}.";
            var availableSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return availableSnapshot;
        }
        catch (Exception exception)
        {
            _availableUpdate = null;
            _lastCheckedAt = DateTimeOffset.Now;
            _lastUpdateMessage = $"Could not check for updates: {exception.Message}";
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
            _lastUpdateMessage = "Development mode or invalid installation: updates are disabled.";
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
                reportProgress($"Downloading {targetVersion}... {progress}%");
            });

            reportProgress($"Update downloaded. Restarting into {targetVersion}...");

            _updateManager.ApplyUpdatesAndRestart(
                _availableUpdate.TargetFullRelease,
                [
                    "--updated-from", currentVersion,
                    "--updated-to", targetVersion,
                    "--updated-package", _availableUpdate.TargetFullRelease.FileName
                ]);

            _lastUpdateMessage = $"The {targetVersion} update is ready to restart.";
            var preparedSnapshot = GetSnapshot();
            SnapshotChanged?.Invoke();
            return new AppUpdateResult(preparedSnapshot, string.Empty);
        }
        catch (Exception exception)
        {
            _lastUpdateMessage = $"Could not prepare the update: {exception.Message}";
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

        if (!string.IsNullOrWhiteSpace(_startupState.FirstRunVersion))
        {
            return $"First launch after installation. Installed version: {_startupState.FirstRunVersion}.";
        }

        if (!string.IsNullOrWhiteSpace(_startupState.RestartedVersion))
        {
            return $"The application restarted after an update. Current version: {_startupState.RestartedVersion}.";
        }

        return $"Active installation. Current version: {appVersion}.";
    }
}
