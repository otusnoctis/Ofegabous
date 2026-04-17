namespace App.Services;

public sealed record DocumentationNavItem(
    string Slug,
    string Title,
    string RelativePath,
    string? Summary,
    int SortOrder);
