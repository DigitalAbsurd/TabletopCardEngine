using FluentAssertions;
using System.Diagnostics;
using System.Reflection;

namespace TabletopCardEngine.Tests;

/// <summary>
/// <c>docs/api/</c> and the <c>nav</c> block of <c>mkdocs.yml</c> are committed output of
/// <c>scripts/generate-api-reference.ps1</c> (ADR-0009). A change to a public member or to its XML
/// doc comment makes them stale, and this test is what notices, the way
/// <see cref="ObservabilityDocsTests"/> does for <c>docs/observability.md</c>. It runs the script
/// without <c>-Update</c>, so it compares everything the script compares and writes nothing.
/// <para>
/// To fix a failure run <c>pwsh ./scripts/generate-api-reference.ps1 -Update</c>, then commit
/// <c>docs/api/</c> and <c>mkdocs.yml</c>.
/// </para>
/// </summary>
[TestClass]
public class ApiReferenceDocsTests
{
    [TestMethod]
    public async Task ApiReference_MatchesTheBuiltAssemblies()
    {
        // The test run built the packages in its own configuration, so -NoBuild must look there.
        var configuration = typeof(ApiReferenceDocsTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        if (configuration is not ("Debug" or "Release"))
        {
            configuration = "Debug";
        }

        var psi = new ProcessStartInfo
        {
            FileName = "pwsh",
            WorkingDirectory = RepositoryRoot.FullPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-File");
        psi.ArgumentList.Add(Path.Combine(RepositoryRoot.FullPath, "scripts", "generate-api-reference.ps1"));
        psi.ArgumentList.Add("-NoBuild");
        psi.ArgumentList.Add("-Configuration");
        psi.ArgumentList.Add(configuration);

        Process? process;
        try
        {
            process = Process.Start(psi);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Assert.Fail($"PowerShell 7 (pwsh) is required to check the API reference and could not be started: {ex.Message}");
            return;
        }

        using (process)
        {
            if (process is null)
            {
                Assert.Fail("PowerShell 7 (pwsh) is required to check the API reference and could not be started.");
                return;
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            process.ExitCode.Should().Be(
                0,
                "docs/api/ and the nav block of mkdocs.yml must match the built assemblies; run `pwsh ./scripts/generate-api-reference.ps1 -Update` and commit them. Script output: {0} {1}",
                await stdout,
                await stderr);
        }
    }
}
