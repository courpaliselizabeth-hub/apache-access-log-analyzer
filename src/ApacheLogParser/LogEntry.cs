namespace ApacheLogParser;

public record LogEntry(
    string IpAddress,
    string AuthUser,
    string Timestamp,
    string Method,
    string RawUrl,
    string NormalizedUrl,
    int StatusCode,
    string BytesTransferred
);
