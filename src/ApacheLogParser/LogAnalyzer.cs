namespace ApacheLogParser;

public record UrlCount(string Url, int Count);
public record IpCount(string IpAddress, int Count);

public static class LogAnalyzer
{
    public static int CountUniqueIps(IEnumerable<LogEntry> entries) =>
        entries.Select(e => e.IpAddress).Distinct().Count();

    public static IReadOnlyList<UrlCount> TopUrls(IEnumerable<LogEntry> entries, int count = 3) =>
        entries
            .GroupBy(e => e.NormalizedUrl)
            .Select(g => new UrlCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Url)          // deterministic tie-breaking
            .Take(count)
            .ToList();

    public static IReadOnlyList<IpCount> TopIps(IEnumerable<LogEntry> entries, int count = 3) =>
        entries
            .GroupBy(e => e.IpAddress)
            .Select(g => new IpCount(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.IpAddress)    // deterministic tie-breaking
            .Take(count)
            .ToList();
}
