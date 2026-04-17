namespace App.Services;

public sealed record DocumentationPage(
    DocumentationNavItem Navigation,
    string Markdown,
    string Html);
