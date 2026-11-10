namespace Shop.Tests.Guardrails;

/// <summary>
/// Compares the SQL a use case sent with an approved file. The file holds command text only,
/// without parameter values. The text can still depend on the values (with EF Core 10.0.14 and
/// SQLite, Skip and Take with equal values shared one parameter), so each test uses fixed inputs.
/// </summary>
public static class SqlSnapshot
{
    private const string Separator = "\n-- next command\n";

    public static string Normalize(IEnumerable<string> commands) =>
        string.Join(Separator, commands.Select(NormalizeOne)) + "\n";

    /// <summary>
    /// Returns null when the SQL matches. Otherwise writes name.received.sql next to the
    /// approved file and returns the reason. It never overwrites the approved file.
    /// </summary>
    public static string? Compare(string directory, string name, IEnumerable<string> commands)
    {
        var actual = Normalize(commands);
        var approvedPath = Path.Combine(directory, name + ".sql");
        var receivedPath = Path.Combine(directory, name + ".received.sql");

        if (!File.Exists(approvedPath))
        {
            File.WriteAllText(receivedPath, actual);
            return $"No approved snapshot {name}.sql. Review {name}.received.sql and rename it if it is right.";
        }

        var approved = NormalizeOne(File.ReadAllText(approvedPath)) + "\n";
        if (approved == actual)
        {
            File.Delete(receivedPath);
            return null;
        }

        File.WriteAllText(receivedPath, actual);
        return $"SQL for {name} changed. Approved {CountCommands(approved)} command(s), received {CountCommands(actual)}. "
            + $"{FirstDifference(approved, actual)} Review {name}.received.sql against {name}.sql.";
    }

    private static int CountCommands(string snapshot) =>
        string.IsNullOrWhiteSpace(snapshot) ? 0 : snapshot.Split(Separator).Length;

    private static string NormalizeOne(string sql) =>
        string.Join("\n", sql.Replace("\r\n", "\n").Split('\n').Select(line => line.TrimEnd())).Trim();

    private static string FirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i] : "<end>";
            var a = i < actualLines.Length ? actualLines[i] : "<end>";
            if (e != a)
            {
                return $"First difference at line {i + 1}: approved \"{e}\", received \"{a}\".";
            }
        }

        return "No line differs.";
    }
}
