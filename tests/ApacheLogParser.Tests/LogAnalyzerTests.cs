namespace ApacheLogParser.Tests;

public class LogAnalyzerTests
{
    // Convenience factory — NormalizedUrl is what the analyzer uses
    private static LogEntry E(string ip, string url) =>
        new(ip, "-", "01/Jan/2023:00:00:00 +0000", "GET", url, url, 200, "100");

    // ── CountUniqueIps ────────────────────────────────────────────────────────

    [Fact]
    public void CountUniqueIps_Empty_ReturnsZero()
    {
        Assert.Equal(0, LogAnalyzer.CountUniqueIps([]));
    }

    [Fact]
    public void CountUniqueIps_AllSameIp_ReturnsOne()
    {
        var entries = new[] { E("1.1.1.1", "/a"), E("1.1.1.1", "/b"), E("1.1.1.1", "/c") };
        Assert.Equal(1, LogAnalyzer.CountUniqueIps(entries));
    }

    [Fact]
    public void CountUniqueIps_DistinctIps_CountsCorrectly()
    {
        var entries = new[]
        {
            E("1.1.1.1", "/a"), E("1.1.1.1", "/b"),
            E("2.2.2.2", "/a"),
            E("3.3.3.3", "/a"),
        };
        Assert.Equal(3, LogAnalyzer.CountUniqueIps(entries));
    }

    // ── TopUrls ───────────────────────────────────────────────────────────────

    [Fact]
    public void TopUrls_Empty_ReturnsEmptyList()
    {
        Assert.Empty(LogAnalyzer.TopUrls([]));
    }

    [Fact]
    public void TopUrls_FewerThan3Unique_ReturnsHoweverManyExist()
    {
        var entries = new[] { E("1.1.1.1", "/a"), E("2.2.2.2", "/b") };
        var top = LogAnalyzer.TopUrls(entries);
        Assert.Equal(2, top.Count);
    }

    [Fact]
    public void TopUrls_OrderedByDescendingCount()
    {
        var entries = new[]
        {
            E("1.1.1.1", "/a"), E("2.2.2.2", "/a"), E("3.3.3.3", "/a"),  // /a × 3
            E("1.1.1.1", "/b"), E("2.2.2.2", "/b"),                       // /b × 2
            E("1.1.1.1", "/c"),                                            // /c × 1
        };

        var top = LogAnalyzer.TopUrls(entries);

        Assert.Equal(3, top.Count);
        Assert.Equal("/a", top[0].Url);
        Assert.Equal(3, top[0].Count);
        Assert.Equal("/b", top[1].Url);
        Assert.Equal(2, top[1].Count);
        Assert.Equal("/c", top[2].Url);
        Assert.Equal(1, top[2].Count);
    }

    [Fact]
    public void TopUrls_TieBreaksByUrlAlphabetically()
    {
        // /alpha and /beta both appear once — /alpha should sort first
        var entries = new[] { E("1.1.1.1", "/beta"), E("1.1.1.1", "/alpha") };
        var top = LogAnalyzer.TopUrls(entries);
        Assert.Equal("/alpha", top[0].Url);
    }

    [Fact]
    public void TopUrls_RespectsCountParameter()
    {
        var entries = new[]
        {
            E("1.1.1.1", "/a"), E("1.1.1.1", "/a"),
            E("1.1.1.1", "/b"),
        };
        var top = LogAnalyzer.TopUrls(entries, count: 1);
        Assert.Single(top);
        Assert.Equal("/a", top[0].Url);
    }

    // ── TopIps ────────────────────────────────────────────────────────────────

    [Fact]
    public void TopIps_Empty_ReturnsEmptyList()
    {
        Assert.Empty(LogAnalyzer.TopIps([]));
    }

    [Fact]
    public void TopIps_FewerThan3Unique_ReturnsHoweverManyExist()
    {
        var entries = new[] { E("1.1.1.1", "/a"), E("2.2.2.2", "/a") };
        var top = LogAnalyzer.TopIps(entries);
        Assert.Equal(2, top.Count);
    }

    [Fact]
    public void TopIps_OrderedByDescendingCount()
    {
        var entries = new[]
        {
            E("1.1.1.1", "/a"), E("1.1.1.1", "/b"), E("1.1.1.1", "/c"),  // × 3
            E("2.2.2.2", "/a"), E("2.2.2.2", "/b"),                       // × 2
            E("3.3.3.3", "/a"),                                            // × 1
        };

        var top = LogAnalyzer.TopIps(entries);

        Assert.Equal("1.1.1.1", top[0].IpAddress);
        Assert.Equal(3, top[0].Count);
        Assert.Equal("2.2.2.2", top[1].IpAddress);
        Assert.Equal(2, top[1].Count);
        Assert.Equal("3.3.3.3", top[2].IpAddress);
        Assert.Equal(1, top[2].Count);
    }

    // ── Full dataset integration test ─────────────────────────────────────────

    [Fact]
    public void FullDataset_ParseAndAnalyze_ReturnsExpectedResults()
    {
        var entries = LogParser.ParseLines(SampleLogs.Lines).ToList();

        // 23 total lines, 2 malformed
        Assert.Equal(21, entries.Count);

        // 10 unique IPs: 192.168.1.{1–9} plus 10.0.0.5
        Assert.Equal(10, LogAnalyzer.CountUniqueIps(entries));

        // Top URLs
        var topUrls = LogAnalyzer.TopUrls(entries);
        Assert.Equal(3, topUrls.Count);

        Assert.Equal("/index.html", topUrls[0].Url);
        Assert.Equal(7, topUrls[0].Count);

        Assert.Equal("/about", topUrls[1].Url);
        Assert.Equal(5, topUrls[1].Count);

        Assert.Equal("/contact", topUrls[2].Url);
        Assert.Equal(4, topUrls[2].Count);

        // Top IPs
        var topIps = LogAnalyzer.TopIps(entries);
        Assert.Equal(3, topIps.Count);

        Assert.Equal("192.168.1.1", topIps[0].IpAddress);
        Assert.Equal(5, topIps[0].Count);

        Assert.Equal("10.0.0.5", topIps[1].IpAddress);
        Assert.Equal(4, topIps[1].Count);

        Assert.Equal("192.168.1.2", topIps[2].IpAddress);
        Assert.Equal(3, topIps[2].Count);
    }
}
