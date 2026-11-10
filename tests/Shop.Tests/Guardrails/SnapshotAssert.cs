using System.Runtime.CompilerServices;
using Xunit;

namespace Shop.Tests.Guardrails;

public static class SnapshotAssert
{
    /// <summary>
    /// Fails when the SQL differs from Snapshots/name.sql, which sits next to the calling test file.
    /// The path comes from the compiler, so it needs the source tree that was built.
    /// </summary>
    public static void Matches(string name, IEnumerable<string> commands, [CallerFilePath] string callerFile = "")
    {
        var directory = Path.Combine(Path.GetDirectoryName(callerFile)!, "Snapshots");
        var reason = SqlSnapshot.Compare(directory, name, commands);
        if (reason is not null)
        {
            Assert.Fail(reason);
        }
    }
}
