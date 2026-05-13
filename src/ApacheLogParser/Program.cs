using ApacheLogParser;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ApacheLogParser <log-file-path>");
    return 1;
}

var filePath = args[0];

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"Error: file not found — {filePath}");
    return 1;
}

var lines = File.ReadAllLines(filePath);
var entries = LogParser.ParseLines(lines).ToList();
var skipped = lines.Length - entries.Count;

Console.WriteLine("=== Apache Log Analysis Report ===");
Console.WriteLine();
Console.WriteLine($"Lines parsed:        {entries.Count}");
Console.WriteLine($"Lines skipped:       {skipped}");
Console.WriteLine();

if (entries.Count == 0)
{
    Console.WriteLine("No valid log entries found.");
    return 0;
}

var uniqueIpCount = LogAnalyzer.CountUniqueIps(entries);
var topUrls       = LogAnalyzer.TopUrls(entries);
var topIps        = LogAnalyzer.TopIps(entries);

Console.WriteLine($"Unique IP Addresses: {uniqueIpCount}");
Console.WriteLine();

Console.WriteLine("Top 3 Most Visited URLs:");
if (topUrls.Count == 0)
    Console.WriteLine("  (none)");
else
    foreach (var (url, count) in topUrls)
        Console.WriteLine($"  {url,-45} {count,5} request(s)");

Console.WriteLine();

Console.WriteLine("Top 3 Most Active IP Addresses:");
if (topIps.Count == 0)
    Console.WriteLine("  (none)");
else
    foreach (var (ip, count) in topIps)
        Console.WriteLine($"  {ip,-20} {count,5} request(s)");

return 0;
