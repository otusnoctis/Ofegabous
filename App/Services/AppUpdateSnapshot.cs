namespace App.Services;

public sealed record AppUpdateSnapshot(
    string AppVersion,
    string VelopackVersion,
    string RepositoryUrl,
    bool IsDevMode,
    bool IsInstalled,
    bool CanCheckUpdates,
    string StartupMessage,
    string UpdateMessage,
    bool IsUpdateAvailable,
    string? AvailableVersion,
    DateTimeOffset? LastCheckedAt);
