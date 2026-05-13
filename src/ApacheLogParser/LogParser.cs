using System.Text.RegularExpressions;

namespace ApacheLogParser;

public static class LogParser
{
    // Matches the mandatory fields of Apache Combined Log Format.
    // Groups: (1) IP, (2) auth-user, (3) timestamp, (4) request-line, (5) status-code, (6) bytes.
    // Trailing content (referer, user-agent, extra junk) is intentionally uncaptured so the
    // regex succeeds on lines with extra or missing optional fields.
    private static readonly Regex LineRegex = new(
        @"^(\S+)\s+\S+\s+(\S+)\s+\[([^\]]+)\]\s+""([^""]*)""\s+(\d{3})\s+(\S+)",
        RegexOptions.Compiled);

    // Matches: METHOD URL [HTTP/version]
    // The protocol token is optional so we handle unusual/truncated log lines.
    private static readonly Regex RequestRegex = new(
        @"^([A-Z-]+)\s+(\S+)(?:\s+HTTP/\S+)?$",
        RegexOptions.Compiled);

    public static LogEntry? ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var lineMatch = LineRegex.Match(line);
        if (!lineMatch.Success)
            return null;

        if (!int.TryParse(lineMatch.Groups[5].Value, out var status))
            return null;

        var requestLine = lineMatch.Groups[4].Value;
        var requestMatch = RequestRegex.Match(requestLine);
        if (!requestMatch.Success)
            return null;

        var rawUrl = requestMatch.Groups[2].Value;

        return new LogEntry(
            IpAddress: lineMatch.Groups[1].Value,
            AuthUser: lineMatch.Groups[2].Value,
            Timestamp: lineMatch.Groups[3].Value,
            Method: requestMatch.Groups[1].Value,
            RawUrl: rawUrl,
            NormalizedUrl: NormalizeUrl(rawUrl),
            StatusCode: status,
            BytesTransferred: lineMatch.Groups[6].Value
        );
    }

    public static IEnumerable<LogEntry> ParseLines(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            var entry = ParseLine(line);
            if (entry is not null)
                yield return entry;
        }
    }

    /// <summary>
    /// Normalizes a raw URL from a log request field:
    ///   - Full URLs (http://host/path?q) → /path
    ///   - Strips query strings and fragments
    ///   - Strips trailing slashes (except root "/")
    ///   - Lowercases the result
    /// </summary>
    public static string NormalizeUrl(string rawUrl)
    {
        // Extract just the path from absolute URLs like http://example.net/path?q=1
        if (rawUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
                rawUrl = uri.AbsolutePath;
            // If the URI is somehow unparseable, fall through and treat as-is
        }

        // Strip query string
        var queryStart = rawUrl.IndexOf('?');
        if (queryStart >= 0)
            rawUrl = rawUrl[..queryStart];

        // Strip fragment
        var fragStart = rawUrl.IndexOf('#');
        if (fragStart >= 0)
            rawUrl = rawUrl[..fragStart];

        // Strip trailing slash from anything longer than "/"
        if (rawUrl.Length > 1 && rawUrl.EndsWith('/'))
            rawUrl = rawUrl.TrimEnd('/');

        return rawUrl.ToLowerInvariant();
    }
}
