using System.Reflection;

namespace OpenHoldemEngine.Demo.Services;

/// <summary>
/// The commit the running demo was built from. The .NET 8 SDK's built-in Source Link appends it to
/// the informational version as <c>1.0.0+&lt;sha&gt;</c> whenever the build runs inside a git
/// checkout — locally and on CI alike — so no build step of our own is needed to stamp it.
/// </summary>
public static class BuildInfo
{
    private const int ShortShaLength = 7;

    /// <summary>The full commit SHA, or <see langword="null"/> when the build had no git metadata.</summary>
    public static string? CommitSha { get; } = ReadCommitSha(
        typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    /// <summary>The first seven characters of <see cref="CommitSha"/>, the way git abbreviates it.</summary>
    public static string? ShortSha => CommitSha is { Length: > ShortShaLength } sha ? sha[..ShortShaLength] : CommitSha;

    /// <summary>
    /// The part of an informational version after the <c>+</c>, if it looks like a commit SHA.
    /// </summary>
    /// <param name="informationalVersion">The assembly's informational version.</param>
    /// <returns>The SHA, or <see langword="null"/>.</returns>
    public static string? ReadCommitSha(string? informationalVersion)
    {
        var plus = informationalVersion?.IndexOf('+', StringComparison.Ordinal) ?? -1;
        if (plus < 0)
        {
            return null;
        }

        var sha = informationalVersion![(plus + 1)..];
        return sha.Length >= ShortShaLength && sha.All(Uri.IsHexDigit) ? sha : null;
    }
}
