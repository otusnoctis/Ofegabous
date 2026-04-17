using System.Text;
using System.Text.RegularExpressions;

namespace App.Services;

public sealed partial class DocumentationService
{
    private readonly TemplateEnvironment _environment;

    public DocumentationService(TemplateEnvironment environment)
    {
        _environment = environment;
    }

    public IReadOnlyList<DocumentationNavItem> GetNavigation()
    {
        if (!Directory.Exists(_environment.DocumentationDirectory))
        {
            return [];
        }

        var documents = Directory.GetFiles(_environment.DocumentationDirectory, "*.md", SearchOption.AllDirectories)
            .Select(BuildNavigationItem)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return documents;
    }

    public async Task<DocumentationPage?> LoadPageAsync(string slug, CancellationToken cancellationToken = default)
    {
        var navigationItem = GetNavigation().FirstOrDefault(item => string.Equals(item.Slug, NormalizeSlug(slug), StringComparison.OrdinalIgnoreCase));
        if (navigationItem is null)
        {
            return null;
        }

        var markdown = await File.ReadAllTextAsync(GetAbsolutePath(navigationItem.RelativePath), Encoding.UTF8, cancellationToken);
        var html = SimpleMarkdownRenderer.Render(markdown, target => ResolveLink(navigationItem.RelativePath, target));
        return new DocumentationPage(navigationItem, markdown, html);
    }

    private DocumentationNavItem BuildNavigationItem(string absolutePath)
    {
        var relativePath = Path.GetRelativePath(_environment.DocumentationDirectory, absolutePath).Replace('\\', '/');
        var markdown = File.ReadAllText(absolutePath, Encoding.UTF8);
        var title = ExtractTitle(markdown, Path.GetFileNameWithoutExtension(relativePath));
        var summary = ExtractSummary(markdown);
        var sortOrder = ExtractSortOrder(Path.GetFileName(relativePath));
        var slug = NormalizeSlug(Path.ChangeExtension(relativePath, null)?.Replace('\\', '/') ?? relativePath);

        return new DocumentationNavItem(slug, title, relativePath, summary, sortOrder);
    }

    private string ResolveLink(string currentRelativePath, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return "#";
        }

        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("#", StringComparison.Ordinal))
        {
            return target;
        }

        var anchor = string.Empty;
        var pathPart = target;
        var anchorIndex = target.IndexOf('#');
        if (anchorIndex >= 0)
        {
            pathPart = target[..anchorIndex];
            anchor = target[anchorIndex..];
        }

        if (!pathPart.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return target;
        }

        var currentDirectory = Path.GetDirectoryName(currentRelativePath.Replace('/', Path.DirectorySeparatorChar)) ?? string.Empty;
        var resolvedAbsolutePath = Path.GetFullPath(Path.Combine(_environment.DocumentationDirectory, currentDirectory, pathPart.Replace('/', Path.DirectorySeparatorChar)));
        if (!resolvedAbsolutePath.StartsWith(_environment.DocumentationDirectory, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(resolvedAbsolutePath))
        {
            return "#";
        }

        var resolvedRelativePath = Path.GetRelativePath(_environment.DocumentationDirectory, resolvedAbsolutePath).Replace('\\', '/');
        var slug = NormalizeSlug(Path.ChangeExtension(resolvedRelativePath, null) ?? resolvedRelativePath);
        return $"/documentation?doc={Uri.EscapeDataString(slug)}{anchor}";
    }

    private string GetAbsolutePath(string relativePath)
    {
        return Path.Combine(_environment.DocumentationDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string NormalizeSlug(string slug)
    {
        return slug.Trim().Replace('\\', '/');
    }

    private static string ExtractTitle(string markdown, string fallback)
    {
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return fallback;
    }

    private static string? ExtractSummary(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n");
        var paragraphs = normalized.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.StartsWith("```", StringComparison.Ordinal) ||
                trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                continue;
            }

            return BuildIntro(trimmed);
        }

        return null;
    }

    private static string BuildIntro(string paragraph)
    {
        var singleLine = Regex.Replace(paragraph.Replace('\n', ' '), @"\s+", " ").Trim();
        singleLine = Regex.Replace(singleLine, @"\[(.*?)\]\((.*?)\)", "$1");
        singleLine = singleLine.Replace("`", string.Empty);

        const int maxLength = 120;
        if (singleLine.Length <= maxLength)
        {
            return singleLine;
        }

        var shortened = singleLine[..maxLength].TrimEnd();
        var lastSpace = shortened.LastIndexOf(' ');
        if (lastSpace > 70)
        {
            shortened = shortened[..lastSpace];
        }

        return $"{shortened}...";
    }

    private static int ExtractSortOrder(string fileName)
    {
        var match = SortOrderRegex().Match(fileName);
        return match.Success && int.TryParse(match.Groups[1].Value, out var order)
            ? order
            : int.MaxValue;
    }

    [GeneratedRegex(@"^(\d+)[-_ ]", RegexOptions.Compiled)]
    private static partial Regex SortOrderRegex();
}
