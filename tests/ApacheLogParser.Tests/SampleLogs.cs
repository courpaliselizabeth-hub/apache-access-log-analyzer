namespace ApacheLogParser.Tests;

/// <summary>
/// 23-line dataset that exercises all edge cases:
///   - Full absolute URLs (http://example.net/...)
///   - Query strings (?page=2, ?ref=home)
///   - Trailing slashes (/about/, /contact/)
///   - Non-dash auth user (admin on line 4)
///   - Extra junk fields after the standard fields (lines 15 and 23)
///   - Two fully malformed lines (lines 11 and 19)
///
/// Expected counts after normalization:
///   Valid entries : 21   (2 malformed skipped)
///   Unique IPs    : 10
///   /index.html   : 7   (lines 1,3,5,8,10,15,23)
///   /about        : 5   (lines 2,7,12,16,21)
///   /contact      : 4   (lines 6,13,18,22)
///   Top IP        : 192.168.1.1 × 5
/// </summary>
public static class SampleLogs
{
    public static readonly string[] Lines =
    [
        // --- standard lines ---
        @"192.168.1.1 - - [10/Oct/2023:13:55:36 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
        @"192.168.1.2 - - [10/Oct/2023:13:55:37 -0700] ""GET /about HTTP/1.1"" 200 567 ""-"" ""Mozilla/5.0""",
        @"192.168.1.1 - - [10/Oct/2023:13:55:38 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
        // auth user = "admin" instead of "-"
        @"10.0.0.5 - admin [10/Oct/2023:13:55:39 -0700] ""POST /admin/login HTTP/1.1"" 302 0 ""-"" ""Mozilla/5.0""",
        // full absolute URL
        @"192.168.1.3 - - [10/Oct/2023:13:55:40 -0700] ""GET http://example.net/index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
        @"192.168.1.1 - - [10/Oct/2023:13:55:41 -0700] ""GET /contact HTTP/1.1"" 200 890 ""-"" ""Mozilla/5.0""",
        // trailing slash on path
        @"192.168.1.4 - - [10/Oct/2023:13:55:42 -0700] ""GET /about/ HTTP/1.1"" 301 0 ""-"" ""Mozilla/5.0""",
        // query string
        @"10.0.0.5 - - [10/Oct/2023:13:55:43 -0700] ""GET /index.html?page=2 HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
        @"192.168.1.2 - - [10/Oct/2023:13:55:44 -0700] ""GET /images/logo.png HTTP/1.1"" 200 4567 ""-"" ""Mozilla/5.0""",
        @"192.168.1.5 - - [10/Oct/2023:13:55:45 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0""",
        // line 11: MALFORMED
        "MALFORMED LINE WITHOUT PROPER FORMAT",
        @"192.168.1.1 - - [10/Oct/2023:13:55:47 -0700] ""GET /about HTTP/1.1"" 200 567 ""-"" ""Mozilla/5.0""",
        // full URL with query string
        @"192.168.1.3 - - [10/Oct/2023:13:55:48 -0700] ""GET http://example.net/contact?ref=home HTTP/1.1"" 200 890 ""-"" ""Mozilla/5.0""",
        @"192.168.1.6 - - [10/Oct/2023:13:55:49 -0700] ""DELETE /api/item/123 HTTP/1.1"" 204 0 ""-"" ""curl/7.68.0""",
        // extra junk field at end
        @"10.0.0.5 - - [10/Oct/2023:13:55:50 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0"" extra_junk_field",
        @"192.168.1.7 - - [10/Oct/2023:13:55:51 -0700] ""GET /about HTTP/1.1"" 404 234 ""-"" ""Mozilla/5.0""",
        @"192.168.1.4 - - [10/Oct/2023:13:55:52 -0700] ""PUT /api/data HTTP/1.1"" 201 0 ""-"" ""PostmanRuntime/7.26""",
        // trailing slash on contact
        @"192.168.1.8 - - [10/Oct/2023:13:55:53 -0700] ""GET /contact/ HTTP/1.1"" 200 890 ""-"" ""Mozilla/5.0""",
        // line 19: MALFORMED
        "ANOTHER BAD LINE !!!",
        @"192.168.1.1 - - [10/Oct/2023:13:55:55 -0700] ""GET /images/logo.png HTTP/1.1"" 200 4567 ""-"" ""Mozilla/5.0""",
        // full absolute URL — normalizes to /about
        @"192.168.1.9 - - [10/Oct/2023:13:55:56 -0700] ""GET http://example.net/about HTTP/1.1"" 200 567 ""-"" ""Mozilla/5.0""",
        @"10.0.0.5 - - [10/Oct/2023:13:55:57 -0700] ""POST /contact HTTP/1.1"" 200 0 ""-"" ""Mozilla/5.0""",
        // multiple extra junk fields at end
        @"192.168.1.2 - - [10/Oct/2023:13:55:58 -0700] ""GET /index.html HTTP/1.1"" 200 1234 ""-"" ""Mozilla/5.0"" extra1 extra2",
    ];
}
