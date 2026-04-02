using System.Text;

namespace LegacyMigrationPrep;

internal static class CsvWriter
{
    public static void Write(string path, List<Dictionary<string, string>> rows, params string[] columns)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        writer.WriteLine(string.Join(",", columns.Select(Escape)));

        foreach (var row in rows)
        {
            writer.WriteLine(string.Join(",", columns.Select(column =>
                Escape(row.TryGetValue(column, out var value) ? value ?? string.Empty : string.Empty))));
        }
    }

    private static string Escape(string value)
    {
        if (value.Contains('"'))
        {
            value = value.Replace("\"", "\"\"");
        }

        return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? $"\"{value}\""
            : value;
    }
}
