using System.Reflection;

namespace App.Services;

public sealed class TemplateMetadata
{
    private const string DisplayNameKey = "TemplateAppDisplayName";
    private const string ShortNameKey = "TemplateAppShortName";
    private const string DescriptionKey = "TemplateAppDescription";
    private const string RepositoryUrlKey = "TemplateRepositoryUrl";
    private const string DataDirectoryNameKey = "TemplateDataDirectoryName";
    private const string PersistenceFileNameKey = "TemplatePersistenceFileName";
    private const string DocumentationDirectoryNameKey = "TemplateDocumentationDirectoryName";

    public string DisplayName { get; } = ReadMetadata(DisplayNameKey, "Template App");
    public string ShortName { get; } = ReadMetadata(ShortNameKey, "Template");
    public string Description { get; } = ReadMetadata(DescriptionKey, "Reusable template for local tools.");
    public string RepositoryUrl { get; } = ReadMetadata(RepositoryUrlKey, "https://github.com/your-org/your-app");
    public string DataDirectoryName { get; } = ReadMetadata(DataDirectoryNameKey, "data");
    public string PersistenceFileName { get; } = ReadMetadata(PersistenceFileNameKey, "persistence.json");
    public string DocumentationDirectoryName { get; } = ReadMetadata(DocumentationDirectoryNameKey, "documentation");

    private static string ReadMetadata(string key, string fallback)
    {
        var value = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)
            ?.Value;

        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
