namespace App.Services;

public sealed record UpdateLogEntry(
    DateTimeOffset TimestampUtc,
    string EventType,
    string Message,
    string? Version = null,
    string? Details = null);
