namespace App.Services;

public sealed class PersistenceDocument
{
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset? LastSavedAtUtc { get; init; }
}
