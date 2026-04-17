using System.Text.Json;

namespace App.Services;

public sealed class PersistenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly TemplateEnvironment _environment;

    public PersistenceStore(TemplateEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<PersistenceDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_environment.PersistenceFilePath))
        {
            return await LoadBundledDocumentAsync(cancellationToken);
        }

        try
        {
            await using var stream = File.OpenRead(_environment.PersistenceFilePath);
            var document = await JsonSerializer.DeserializeAsync<PersistenceDocument>(stream, cancellationToken: cancellationToken);
            return document ?? new PersistenceDocument();
        }
        catch (JsonException)
        {
            return await LoadBundledDocumentAsync(cancellationToken);
        }
    }

    public async Task SaveAsync(PersistenceDocument document, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_environment.DataDirectory);

        await using var stream = File.Create(_environment.PersistenceFilePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions, cancellationToken);
    }

    private async Task<PersistenceDocument> LoadBundledDocumentAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_environment.BundledPersistenceFilePath))
        {
            return new PersistenceDocument();
        }

        try
        {
            await using var stream = File.OpenRead(_environment.BundledPersistenceFilePath);
            var document = await JsonSerializer.DeserializeAsync<PersistenceDocument>(stream, cancellationToken: cancellationToken);
            return document ?? new PersistenceDocument();
        }
        catch (JsonException)
        {
            return new PersistenceDocument();
        }
    }
}
