# Apache Log Parser

A C# console application that parses Apache HTTP access logs and reports:

- Total count of unique IP addresses
- Top 3 most visited URLs (by request count)
- Top 3 most active IP addresses (by request count)

---

## Assumptions & Design Decisions

### URL Normalization

The same resource can appear in a log file in several forms:

| Raw value in log | Normalized to |
|---|---|
| `/path/` | `/path` |
| `/path?q=1&r=2` | `/path` |
| `/path#section` | `/path` |
| `http://example.net/path?q=1` | `/path` |
| `/PATH` | `/path` |
| `/` | `/` (root is preserved) |

**Rules applied in order:**

1. If the URL is an absolute `http://` or `https://` URL, extract only the `AbsolutePath` via `Uri.TryCreate`. This handles the "proxy-style" entries that Apache logs when the client sends a full URL in the request line.
2. Strip everything from `?` onward (query string).
3. Strip everything from `#` onward (fragment — rare in server logs but handled).
4. Strip trailing `/` unless the path is exactly `/`.
5. Lowercase the result.

**Why strip query strings?** Query parameters parameterize the same resource; `/search?q=cats` and `/search?q=dogs` are both visits to `/search`. Grouping by path matches the spirit of "most visited URL."

**Why not group by resource type** (e.g., all `/api/*` together)? The problem asks for top URLs, not top route prefixes. Exact-path grouping is simpler and reversible; bucketing would be a lossy design choice that the caller can't undo.

### Malformed Line Handling

A line is considered malformed and **silently skipped** if any of the following is true:

- It is empty or whitespace-only.
- The outer Apache log pattern doesn't match (the regex requires at minimum: IP, identd placeholder, auth-user, `[timestamp]`, `"request-line"`, status code, bytes).
- The `status-code` field is not a valid integer.
- The request-line field (the quoted string) cannot be split into `METHOD URL [HTTP/version]`.

The final report shows `Lines skipped: N` so the caller knows how many lines were rejected without crashing or hiding the information.

**Extra fields are tolerated**: lines with additional trailing tokens beyond the standard referer and user-agent fields (e.g., load balancer IDs, custom log tags) parse successfully because the regex does not anchor to end-of-line after the mandatory fields.

**`-` request lines**: If Apache couldn't record the request (e.g., a connection that was dropped before a request was sent), it may log `"-"` for the request field. This fails the `METHOD URL` parse and is counted as skipped — intentional, since there's no URL to analyze.

### Auth User Field

Apache logs either `-` (unauthenticated) or a username in the third field. The parser captures whatever is there as `AuthUser` but does not use it for any aggregation — it's available on `LogEntry` for callers that want it.

### Edge Cases

| Scenario | Behavior |
|---|---|
| Empty file | `Lines parsed: 0`, no report sections crash |
| All lines malformed | `Lines parsed: 0`, `Lines skipped: N`, reports "(none)" |
| Fewer than 3 unique URLs/IPs | Reports however many exist (`Take(n)` returns ≤ n) |
| Exact tie in counts | Secondary sort is alphabetical, making output deterministic |
| Full absolute URLs | Path extracted via `Uri.TryCreate`; query stripped separately |

---

## Project Structure

```
ApacheLogParser/
├── ApacheLogParser.sln
├── sample.log                         # 23-line test dataset
├── README.md
├── src/
│   └── ApacheLogParser/
│       ├── LogEntry.cs                # immutable record for one parsed line
│       ├── LogParser.cs               # regex parsing + URL normalization
│       ├── LogAnalyzer.cs             # LINQ aggregations
│       └── Program.cs                 # CLI entry point
└── tests/
    └── ApacheLogParser.Tests/
        ├── SampleLogs.cs              # 23-line dataset as a string array
        ├── LogParserTests.cs          # parser + NormalizeUrl unit tests
        └── LogAnalyzerTests.cs        # analyzer unit + full-dataset integration test
```

---

## Setup & Build

Requires the [.NET SDK](https://dotnet.microsoft.com/download) (10.0 or later; also works with 8.0+ if you update `<TargetFramework>` in both `.csproj` files).

```bash
git clone <repo>
cd ApacheLogParser
dotnet build
```

---

## Running the Parser

```bash
dotnet run --project src/ApacheLogParser -- <path-to-log-file>
```

**Example with a real-world log file:**

```bash
dotnet run --project src/ApacheLogParser -- /Users/lizcourp/programming-task/programming-task-example-data.log
```

**Sample output:**

```
=== Apache Log Analysis Report ===

Lines parsed:        23
Lines skipped:       0

Unique IP Addresses: 11

Top 3 Most Visited URLs:
  /docs/manage-websites                             2 request(s)
  /faq                                              2 request(s)
  /                                                 1 request(s)

Top 3 Most Active IP Addresses:
  168.41.191.40            4 request(s)
  177.71.128.21            3 request(s)
  50.112.00.11             3 request(s)
```

All 23 lines parsed successfully with no skipped entries.

---

## Running Tests

```bash
dotnet test
```

All 40 tests should pass in under a second. The test suite covers:

- Field extraction from standard log lines
- `admin` auth-user variant
- Full absolute URL extraction (`http://example.net/path` → `/path`)
- Query string stripping
- Trailing slash normalization
- Uppercase path lowercasing
- Extra junk fields after user-agent
- Null / empty / whitespace / malformed input → `null`
- `TopUrls` and `TopIps` ordering, tie-breaking, and `count` parameter
- Fewer-than-3-results edge cases
- Full 23-line dataset integration test with exact expected counts

---

## Notable Design Choices

**`LogParser.NormalizeUrl` is `public`** so it can be tested in isolation via `[Theory]` without needing a full log line. Keeping it internal and using `[InternalsVisibleTo]` would work equally well; `public` was simpler here.

**`LogAnalyzer` is stateless** (all static methods over `IEnumerable<LogEntry>`). There's no mutable object to construct, no dependencies to inject, and the callers are free to filter or transform entries before passing them in.

**`ParseLines` is an iterator** (`yield return`) rather than materializing a `List<>` immediately. The caller in `Program.cs` calls `.ToList()` once, which is fine for a CLI. A streaming use case (e.g., tailing a live log) could consume the iterator directly without loading the whole file.
