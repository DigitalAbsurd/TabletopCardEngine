using FluentAssertions;
using System.Text.RegularExpressions;

namespace TabletopCardEngine.Tests;

/// <summary>
/// Ensures that all consumer documentation and examples use only stable package versions and
/// do not include the <c>--prerelease</c> flag, keeping pre-release versions out of copy-paste
/// code that users may encounter on NuGet.org.
/// </summary>
[TestClass]
public sealed class StableExamplesTests
{
    [TestMethod]
    public void ConsumerDocs_UseOnlyStableVersions()
    {
        var errors = new List<string>();
        var prereleaseFlagPattern = new Regex(@"--prerelease", RegexOptions.None);
        var preReleaseVersionPattern = new Regex(@"\b\d+\.\d+\.\d+-(preview|rc)\b", RegexOptions.None);

        var filesToCheck = new List<string>();

        var rootPath = RepositoryRoot.FullPath;

        // Add top-level README and docs
        filesToCheck.Add(Path.Combine(rootPath, "README.md"));
        filesToCheck.Add(Path.Combine(rootPath, "docs", "getting-started.md"));
        filesToCheck.Add(Path.Combine(rootPath, "docs", "docker.md"));

        // Add package READMEs
        var srcPath = Path.Combine(rootPath, "src");
        if (Directory.Exists(srcPath))
        {
            foreach (var packageDir in Directory.GetDirectories(srcPath))
            {
                var readmePath = Path.Combine(packageDir, "README.md");
                if (File.Exists(readmePath))
                {
                    filesToCheck.Add(readmePath);
                }
            }
        }

        // Add sample documentation
        var samplesPath = Path.Combine(rootPath, "samples");
        if (Directory.Exists(samplesPath))
        {
            foreach (var mdFile in Directory.GetFiles(samplesPath, "*.md", SearchOption.AllDirectories))
            {
                filesToCheck.Add(mdFile);
            }
        }

        // Check each file
        foreach (var filePath in filesToCheck)
        {
            if (!File.Exists(filePath))
            {
                continue;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                var relativePath = Path.GetRelativePath(rootPath, filePath).Replace(Path.DirectorySeparatorChar, '/');

                for (var lineNo = 0; lineNo < lines.Length; lineNo++)
                {
                    var line = lines[lineNo];

                    if (prereleaseFlagPattern.IsMatch(line))
                    {
                        errors.Add($"{relativePath}:{lineNo + 1}: {line.Trim()}");
                    }
                    else if (preReleaseVersionPattern.IsMatch(line))
                    {
                        errors.Add($"{relativePath}:{lineNo + 1}: {line.Trim()}");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip files we cannot read
            }
        }

        errors.Should().BeEmpty("consumer documentation must use only stable versions and not include --prerelease");
    }
}
