using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace App.Services;

public static partial class SimpleMarkdownRenderer
{
    public static string Render(string markdown, Func<string, string> linkResolver)
    {
        var normalized = markdown.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        var html = new StringBuilder();
        var paragraphLines = new List<string>();
        var codeBlockLines = new List<string>();
        var inUnorderedList = false;
        var inOrderedList = false;
        var inCodeBlock = false;
        var codeBlockLanguage = string.Empty;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');
            var trimmed = line.Trim();

            if (inCodeBlock)
            {
                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    var encodedCode = WebUtility.HtmlEncode(string.Join('\n', codeBlockLines));
                    var languageClass = string.IsNullOrWhiteSpace(codeBlockLanguage)
                        ? string.Empty
                        : $" class=\"language-{WebUtility.HtmlEncode(codeBlockLanguage)}\"";
                    html.Append($"<pre><code{languageClass}>{encodedCode}</code></pre>");
                    codeBlockLines.Clear();
                    inCodeBlock = false;
                    codeBlockLanguage = string.Empty;
                    continue;
                }

                codeBlockLines.Add(line);
                continue;
            }

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                CloseLists(html, ref inUnorderedList, ref inOrderedList);
                inCodeBlock = true;
                codeBlockLanguage = trimmed.Length > 3 ? trimmed[3..].Trim() : string.Empty;
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                CloseLists(html, ref inUnorderedList, ref inOrderedList);
                continue;
            }

            if (TryParseHeading(trimmed, out var level, out var headingText))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                CloseLists(html, ref inUnorderedList, ref inOrderedList);
                html.Append($"<h{level}>{RenderInline(headingText, linkResolver)}</h{level}>");
                continue;
            }

            if (IsHorizontalRule(trimmed))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                CloseLists(html, ref inUnorderedList, ref inOrderedList);
                html.Append("<hr />");
                continue;
            }

            if (TryParseBlockquote(trimmed, out var blockquoteText))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                CloseLists(html, ref inUnorderedList, ref inOrderedList);
                html.Append($"<blockquote><p>{RenderInline(blockquoteText, linkResolver)}</p></blockquote>");
                continue;
            }

            if (TryParseUnorderedListItem(trimmed, out var unorderedItem))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                if (inOrderedList)
                {
                    html.Append("</ol>");
                    inOrderedList = false;
                }

                if (!inUnorderedList)
                {
                    html.Append("<ul>");
                    inUnorderedList = true;
                }

                html.Append($"<li>{RenderInline(unorderedItem, linkResolver)}</li>");
                continue;
            }

            if (TryParseOrderedListItem(trimmed, out var orderedItem))
            {
                FlushParagraph(html, paragraphLines, linkResolver);
                if (inUnorderedList)
                {
                    html.Append("</ul>");
                    inUnorderedList = false;
                }

                if (!inOrderedList)
                {
                    html.Append("<ol>");
                    inOrderedList = true;
                }

                html.Append($"<li>{RenderInline(orderedItem, linkResolver)}</li>");
                continue;
            }

            paragraphLines.Add(trimmed);
        }

        if (inCodeBlock)
        {
            var encodedCode = WebUtility.HtmlEncode(string.Join('\n', codeBlockLines));
            html.Append($"<pre><code>{encodedCode}</code></pre>");
        }

        FlushParagraph(html, paragraphLines, linkResolver);
        CloseLists(html, ref inUnorderedList, ref inOrderedList);

        return html.ToString();
    }

    private static void FlushParagraph(StringBuilder html, List<string> paragraphLines, Func<string, string> linkResolver)
    {
        if (paragraphLines.Count == 0)
        {
            return;
        }

        html.Append($"<p>{RenderInline(string.Join(' ', paragraphLines), linkResolver)}</p>");
        paragraphLines.Clear();
    }

    private static void CloseLists(StringBuilder html, ref bool inUnorderedList, ref bool inOrderedList)
    {
        if (inUnorderedList)
        {
            html.Append("</ul>");
            inUnorderedList = false;
        }

        if (inOrderedList)
        {
            html.Append("</ol>");
            inOrderedList = false;
        }
    }

    private static bool TryParseHeading(string line, out int level, out string text)
    {
        var hashCount = 0;
        while (hashCount < line.Length && line[hashCount] == '#')
        {
            hashCount++;
        }

        if (hashCount is > 0 and <= 6 && hashCount < line.Length && line[hashCount] == ' ')
        {
            level = hashCount;
            text = line[(hashCount + 1)..].Trim();
            return true;
        }

        level = 0;
        text = string.Empty;
        return false;
    }

    private static bool IsHorizontalRule(string line)
    {
        return line.Length >= 3 &&
               line.All(character => character is '-' or '*' or '_');
    }

    private static bool TryParseBlockquote(string line, out string text)
    {
        if (line.StartsWith("> ", StringComparison.Ordinal))
        {
            text = line[2..].Trim();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryParseUnorderedListItem(string line, out string text)
    {
        if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
        {
            text = line[2..].Trim();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryParseOrderedListItem(string line, out string text)
    {
        var match = OrderedListRegex().Match(line);
        if (match.Success)
        {
            text = match.Groups[1].Value.Trim();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static string RenderInline(string text, Func<string, string> linkResolver)
    {
        var codeSegments = new List<string>();
        var encoded = text;

        encoded = CodeSpanRegex().Replace(encoded, match =>
        {
            codeSegments.Add($"<code>{WebUtility.HtmlEncode(match.Groups[1].Value)}</code>");
            return $"@@CODE{codeSegments.Count - 1}@@";
        });

        encoded = WebUtility.HtmlEncode(encoded);

        encoded = LinkRegex().Replace(encoded, match =>
        {
            var label = ApplyBasicFormatting(WebUtility.HtmlEncode(match.Groups[1].Value));
            var href = linkResolver(match.Groups[2].Value);
            var hrefEncoded = WebUtility.HtmlEncode(href);
            var externalAttributes = href.StartsWith("/", StringComparison.Ordinal) || href.StartsWith("#", StringComparison.Ordinal)
                ? string.Empty
                : " target=\"_blank\" rel=\"noreferrer\"";
            return $"<a href=\"{hrefEncoded}\"{externalAttributes}>{label}</a>";
        });

        encoded = ApplyBasicFormatting(encoded);

        for (var index = 0; index < codeSegments.Count; index++)
        {
            encoded = encoded.Replace($"@@CODE{index}@@", codeSegments[index], StringComparison.Ordinal);
        }

        return encoded;
    }

    private static string ApplyBasicFormatting(string encodedText)
    {
        var formatted = StrongRegex().Replace(encodedText, "<strong>$1</strong>");
        formatted = EmphasisRegex().Replace(formatted, "<em>$1</em>");
        return formatted;
    }

    [GeneratedRegex(@"^\d+\.\s+(.+)$", RegexOptions.Compiled)]
    private static partial Regex OrderedListRegex();

    [GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
    private static partial Regex CodeSpanRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)", RegexOptions.Compiled)]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*(.+?)\*\*", RegexOptions.Compiled)]
    private static partial Regex StrongRegex();

    [GeneratedRegex(@"(?<!\*)\*(?!\s)(.+?)(?<!\s)\*(?!\*)", RegexOptions.Compiled)]
    private static partial Regex EmphasisRegex();
}
