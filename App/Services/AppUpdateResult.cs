namespace App.Services;

public sealed record AppUpdateResult(AppUpdateSnapshot Snapshot, string ProgressMessage);
