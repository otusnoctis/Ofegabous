using System.Text.Json;

namespace App.Services;

public sealed class UpdateLogStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly TemplateEnvironment _environment;

    public UpdateLogStore(TemplateEnvironment environment)
    {
        _environment = environment;
    }

    public string LogFilePath => _environment.UpdateLogFilePath;

    public async Task<IReadOnlyList<UpdateLogEntry>> LoadRecentAsync(int maxEntries = 20, CancellationToken cancellationToken = default)
    {
        var entries = await LoadAllAsync(cancellationToken);
        return entries
            .OrderByDescending(entry => entry.TimestampUtc)
            .Take(maxEntries)
            .ToList();
    }

    public async Task AppendAsync(UpdateLogEntry entry, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var entries = await ReadEntriesUnsafeAsync(cancellationToken);
            entries.Add(entry);

            Directory.CreateDirectory(_environment.LogsDirectory);
            await using var stream = File.Create(_environment.UpdateLogFilePath);
            await JsonSerializer.SerializeAsync(stream, entries, JsonOptions, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<IReadOnlyList<UpdateLogEntry>> LoadAllAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadEntriesUnsafeAsync(cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<List<UpdateLogEntry>> ReadEntriesUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_environment.UpdateLogFilePath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(_environment.UpdateLogFilePath);
            var entries = await JsonSerializer.DeserializeAsync<List<UpdateLogEntry>>(stream, cancellationToken: cancellationToken);
            return entries ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
