using Microsoft.Extensions.Primitives;

namespace TabletopCardEngine.AspNetCore.Http;

/// <summary>
/// Converts between game versions and HTTP ETag and If-Match headers.
/// </summary>
internal static class HoldemETag
{
    /// <summary>
    /// Formats the given version as a strong entity tag.
    /// </summary>
    /// <param name="version">The version to format.</param>
    /// <returns>A quoted string like <c>"7"</c>, quotes included.</returns>
    internal static string Format(int version) => $"\"{version}\"";

    /// <summary>
    /// Parses the If-Match header and validates it according to RFC 9110.
    /// </summary>
    /// <param name="values">The values from HttpRequest.Headers["If-Match"]. Multiple header lines arrive as multiple values.</param>
    /// <returns>A record indicating whether the header was present, whether it is valid if present, and the expected version if present and valid.</returns>
    internal static HoldemIfMatch ParseIfMatch(StringValues values)
    {
        // Header absent
        if (values.Count == 0)
        {
            return new HoldemIfMatch(Present: false, Valid: true, ExpectedVersion: null);
        }

        // Wildcard: present but no version check
        if (values.Count == 1 && values[0] == "*")
        {
            return new HoldemIfMatch(Present: true, Valid: true, ExpectedVersion: null);
        }

        // Multiple header lines - invalid
        if (values.Count > 1)
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        var headerValue = values[0]!;

        // Empty or whitespace-only value
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Check for list (comma-separated values in single header line)
        if (headerValue.Contains(','))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Reject weak tags (starts with W/)
        if (headerValue.StartsWith("W/", StringComparison.Ordinal))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Must be quoted: starts and ends with "
        if (!headerValue.StartsWith('"') || !headerValue.EndsWith('"'))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Extract the content between quotes
        var tag = headerValue.Substring(1, headerValue.Length - 2);

        // Tag content must not contain quotes or be empty
        if (tag.Contains('"') || string.IsNullOrEmpty(tag))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Try to parse as an integer
        if (!int.TryParse(tag, out var version))
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        // Reject 0 or negative versions (Version starts at 1)
        if (version <= 0)
        {
            return new HoldemIfMatch(Present: true, Valid: false, ExpectedVersion: null);
        }

        return new HoldemIfMatch(Present: true, Valid: true, ExpectedVersion: version);
    }
}

/// <summary>
/// The result of parsing an If-Match header.
/// </summary>
/// <param name="Present">Whether the header was present.</param>
/// <param name="Valid">Whether the header, if present, is valid.</param>
/// <param name="ExpectedVersion">The version to expect, or null if no version check or invalid.</param>
internal readonly record struct HoldemIfMatch(bool Present, bool Valid, int? ExpectedVersion);
