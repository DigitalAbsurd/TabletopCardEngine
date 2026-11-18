using FluentAssertions;
using System.Text.RegularExpressions;

namespace TabletopCardEngine.Demo.Tests;

[TestClass]
public sealed partial class AwaitedEvents_Tests
{
    /// <summary>
    /// bUnit's synchronous event helpers do not wait for the dispatcher, and a test that uses one is
    /// flaky only under load (see <see cref="AwaitedEvents"/>), so nothing else would catch it coming back.
    /// </summary>
    [TestMethod]
    public void TestSources_RaiseEventsOnlyThroughTheAwaitedHelpers()
    {
        // Arrange
        var directory = Path.Combine(FindRepositoryRoot(), "tests", "TabletopCardEngine.Demo.Tests");

        // Act
        var offenders = Directory.EnumerateFiles(directory, "*.cs")
            .Where(path => Path.GetFileName(path) is not (nameof(AwaitedEvents) + ".cs"))
            .SelectMany(path => File.ReadLines(path).Select((line, index) => (path, line, number: index + 1)))
            .Where(l => SynchronousEvent().IsMatch(l.line))
            .Select(l => $"{Path.GetFileName(l.path)}:{l.number}: {l.line.Trim()}")
            .ToList();

        // Assert
        offenders.Should().BeEmpty("events go through ClickAndWait, ChangeAndWait, InputAndWait and TriggerEventAndWait");
    }

    [GeneratedRegex(@"\.(Click|DoubleClick|Change|Input|TriggerEvent|KeyDown|KeyUp|KeyPress|Submit|Focus|Blur)\(")]
    private static partial Regex SynchronousEvent();

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "TabletopCardEngine.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"Could not find TabletopCardEngine.sln above {AppContext.BaseDirectory}.");
    }
}
