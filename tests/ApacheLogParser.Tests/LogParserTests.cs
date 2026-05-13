namespace ApacheLogParser.Tests;

public class LogParserTests
{
    // ── ParseLine: happy-path field extraction ────────────────────────────────

    [Fact]
    public void ParseLine_StandardLine_ExtractsAllFields()
    {
        const string line = @"192.168.1.1 - - [10/Oct/2023:13:55:36 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("192.168.1.1", entry.IpAddress);
        Assert.Equal("-", entry.AuthUser);
        Assert.Equal("GET", entry.Method);
        Assert.Equal("/index.html", entry.RawUrl);
        Assert.Equal("/index.html", entry.NormalizedUrl);
        Assert.Equal(200, entry.StatusCode);
        Assert.Equal("1234", entry.BytesTransferred);
    }

    [Fact]
    public void ParseLine_AdminAuthUser_CapturesUsername()
    {
        const string line = @"10.0.0.5 - admin [10/Oct/2023:13:55:39 -0700] ""POST /admin/login HTTP/1.1"" 302 0 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("10.0.0.5", entry.IpAddress);
        Assert.Equal("admin", entry.AuthUser);
        Assert.Equal("/admin/login", entry.NormalizedUrl);
    }

    // ── ParseLine: URL normalization applied during parsing ───────────────────

    [Fact]
    public void ParseLine_FullHttpUrl_ExtractsPathOnly()
    {
        const string line = @"192.168.1.3 - - [10/Oct/2023:13:55:40 -0700] ""GET http://example.net/index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("http://example.net/index.html", entry.RawUrl);
        Assert.Equal("/index.html", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_UrlWithQueryString_StripsQuery()
    {
        const string line = @"10.0.0.5 - - [10/Oct/2023:13:55:43 -0700] ""GET /index.html?page=2 HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/index.html", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_TrailingSlash_IsStripped()
    {
        const string line = @"192.168.1.4 - - [10/Oct/2023:13:55:42 -0700] ""GET /about/ HTTP/1.1"" 301 0 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/about", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_RootPath_TrailingSlashKept()
    {
        const string line = @"192.168.1.1 - - [10/Oct/2023:13:55:36 -0700] ""GET / HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_UpperCasePath_IsLowercased()
    {
        const string line = @"192.168.1.1 - - [10/Oct/2023:13:55:36 -0700] ""GET /Images/Logo.PNG HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/images/logo.png", entry.NormalizedUrl);
    }

    // ── ParseLine: tolerance of non-standard lines ────────────────────────────

    [Fact]
    public void ParseLine_ExtraFieldsAfterUserAgent_ParsesSuccessfully()
    {
        const string line = @"10.0.0.5 - - [10/Oct/2023:13:55:50 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0"" extra_junk_field";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/index.html", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_MultipleExtraFields_ParsesSuccessfully()
    {
        const string line = @"192.168.1.2 - - [10/Oct/2023:13:55:58 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0"" extra1 extra2";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("/index.html", entry.NormalizedUrl);
    }

    [Fact]
    public void ParseLine_DeleteMethod_ParsesSuccessfully()
    {
        const string line = @"192.168.1.6 - - [10/Oct/2023:13:55:49 -0700] ""DELETE /api/item/123 HTTP/1.1"" 204 0 ""-"" ""curl/7.68.0""";

        var entry = LogParser.ParseLine(line);

        Assert.NotNull(entry);
        Assert.Equal("DELETE", entry.Method);
        Assert.Equal("/api/item/123", entry.NormalizedUrl);
    }

    // ── ParseLine: malformed / missing input → returns null ───────────────────

    [Fact]
    public void ParseLine_MalformedLine_ReturnsNull()
    {
        Assert.Null(LogParser.ParseLine("MALFORMED LINE WITHOUT PROPER FORMAT"));
    }

    [Fact]
    public void ParseLine_AnotherJunkLine_ReturnsNull()
    {
        Assert.Null(LogParser.ParseLine("ANOTHER BAD LINE !!!"));
    }

    [Fact]
    public void ParseLine_EmptyString_ReturnsNull()
    {
        Assert.Null(LogParser.ParseLine(""));
    }

    [Fact]
    public void ParseLine_WhitespaceOnly_ReturnsNull()
    {
        Assert.Null(LogParser.ParseLine("   "));
    }

    [Fact]
    public void ParseLine_NullInput_ReturnsNull()
    {
        Assert.Null(LogParser.ParseLine(null));
    }

    // ── ParseLines: batch skipping ────────────────────────────────────────────

    [Fact]
    public void ParseLines_MixedInput_SkipsMalformedLines()
    {
        var lines = new[]
        {
            @"192.168.1.1 - - [10/Oct/2023:13:55:36 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
            "MALFORMED",
            @"192.168.1.2 - - [10/Oct/2023:13:55:37 -0700] ""GET /about HTTP/1.1"" 200 567 ""-"" ""Mozilla/5.0""",
            "",
        };

        var entries = LogParser.ParseLines(lines).ToList();

        Assert.Equal(2, entries.Count);
    }

    // ── NormalizeUrl: standalone unit tests ───────────────────────────────────

    [Theory]
    [InlineData("/path/",                          "/path")]
    [InlineData("/path",                           "/path")]
    [InlineData("/",                               "/")]
    [InlineData("/PATH",                           "/path")]
    [InlineData("/path?q=1",                       "/path")]
    [InlineData("/path?q=1&r=2",                   "/path")]
    [InlineData("/path#section",                   "/path")]
    [InlineData("/path?q=1#frag",                  "/path")]
    [InlineData("http://example.net/path",         "/path")]
    [InlineData("http://example.net/path?q=1",     "/path")]
    [InlineData("https://example.net/path/",       "/path")]
    [InlineData("http://example.net/contact?ref=home", "/contact")]
    public void NormalizeUrl_Various_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, LogParser.NormalizeUrl(input));
    }
}
