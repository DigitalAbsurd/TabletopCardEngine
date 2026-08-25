using System.IO.Compression;
using System.Reflection;

namespace OpenHoldemEngine.Tests.Phh;

/// <summary>
/// The vendored PHH corpus embedded in this assembly. See
/// <c>Resources/phh/README.md</c> for provenance, licensing and how to regenerate it.
/// </summary>
internal static class PhhFixture
{
    /// <summary>
    /// Caps how many hands the corpus-wide replay covers, so <c>dotnet test</c> stays quick.
    /// Set to <c>0</c> to replay all of them.
    /// </summary>
    internal const string HandLimitVariable = "OHE_TEST_PHH_HAND_LIMIT";

    private const int DefaultHandLimit = 500;
    private const string PluribusResourceSuffix = "Resources.phh.pluribus.phhs.gz";

    private static readonly Lazy<IReadOnlyList<PhhHand>> _pluribus =
        new(() => PhhReader.ReadAll(ReadGZippedResource(PluribusResourceSuffix)));

    /// <summary>All 10 000 Pluribus hands, in upstream order. Parsed once per test run.</summary>
    internal static IReadOnlyList<PhhHand> Pluribus => _pluribus.Value;

    /// <summary>
    /// The Pluribus hands to replay, honouring <see cref="HandLimitVariable"/>. A capped run takes an
    /// evenly spaced sample rather than the first N hands, so it still reaches every table in the
    /// corpus — the rarer shapes (multi-way showdowns, split pots) are not spread evenly across it.
    /// </summary>
    internal static IReadOnlyList<PhhHand> PluribusUpToLimit
    {
        get
        {
            if (HandLimit is not { } limit || limit >= Pluribus.Count)
                return Pluribus;

            var stride = (int)Math.Ceiling(Pluribus.Count / (double)limit);
            return [.. Pluribus.Where((_, ix) => ix % stride == 0)];
        }
    }

    /// <summary><c>null</c> when the whole corpus should be replayed.</summary>
    internal static int? HandLimit
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable(HandLimitVariable);
            if (string.IsNullOrWhiteSpace(configured))
                return DefaultHandLimit;

            if (!int.TryParse(configured, out var limit) || limit < 0)
                throw new InvalidOperationException($"{HandLimitVariable}='{configured}' is not a non-negative integer");

            return limit == 0 ? null : limit;
        }
    }

    /// <summary>Finds one hand by its upstream file name, e.g. <c>pluribus/30/0.phh</c>.</summary>
    internal static PhhHand Single(string source)
        => Pluribus.SingleOrDefault(hand => hand.Source == source)
            ?? throw new InvalidOperationException($"'{source}' is not in the embedded PHH corpus");

    /// <summary>Looks the resource up by suffix so a change of folder layout does not break loading.</summary>
    private static string ReadGZippedResource(string resourceNameSuffix)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(resourceNameSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No embedded resource ends with '{resourceNameSuffix}'. Found: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var resourceStream = assembly.GetManifestResourceStream(resourceName)!;
        using var gzipStream = new GZipStream(resourceStream, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        return reader.ReadToEnd();
    }
}
