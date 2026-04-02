using System.IO.Compression;
using System.Xml;

namespace LegacyMigrationPrep;

internal static class XlsxReader
{
    public static List<Dictionary<string, string>> ReadSheet(string path, string sheetName)
    {
        using var stream = File.OpenRead(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, false);

        var sharedStrings = LoadSharedStrings(archive);
        var target = FindSheetTarget(archive, sheetName)
                     ?? throw new InvalidOperationException($"Sheet '{sheetName}' not found in '{path}'.");

        var entry = archive.GetEntry(target)
                   ?? throw new InvalidOperationException($"Worksheet XML '{target}' not found.");

        using var reader = XmlReader.Create(entry.Open(), new XmlReaderSettings { IgnoreWhitespace = true });
        var rows = new List<Dictionary<string, string>>();
        List<string>? headers = null;

        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "row")
            {
                continue;
            }

            using var rowReader = reader.ReadSubtree();
            var values = ReadRow(rowReader, sharedStrings);

            if (headers is null)
            {
                headers = values;
                continue;
            }

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var key = headers[i];
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                row[key.Trim()] = i < values.Count ? values[i].Trim() : string.Empty;
            }

            if (row.Count > 0)
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static List<string> LoadSharedStrings(ZipArchive archive)
    {
        var result = new List<string>();
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return result;
        }

        using var reader = XmlReader.Create(entry.Open(), new XmlReaderSettings { IgnoreWhitespace = true });
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "si")
            {
                using var subtree = reader.ReadSubtree();
                var text = new System.Text.StringBuilder();
                while (subtree.Read())
                {
                    if (subtree.NodeType == XmlNodeType.Text ||
                        subtree.NodeType == XmlNodeType.Whitespace ||
                        subtree.NodeType == XmlNodeType.SignificantWhitespace)
                    {
                        text.Append(subtree.Value);
                    }
                }

                result.Add(text.ToString());
            }
        }

        return result;
    }

    private static string? FindSheetTarget(ZipArchive archive, string sheetName)
    {
        var relMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var relEntry = archive.GetEntry("xl/_rels/workbook.xml.rels")
                      ?? throw new InvalidOperationException("Workbook relationships not found.");

        using (var relReader = XmlReader.Create(relEntry.Open(), new XmlReaderSettings { IgnoreWhitespace = true }))
        {
            while (relReader.Read())
            {
                if (relReader.NodeType == XmlNodeType.Element && relReader.LocalName == "Relationship")
                {
                    var id = relReader.GetAttribute("Id");
                    var target = relReader.GetAttribute("Target");
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(target))
                    {
                        relMap[id] = target;
                    }
                }
            }
        }

        var workbookEntry = archive.GetEntry("xl/workbook.xml")
                           ?? throw new InvalidOperationException("Workbook XML not found.");

        using var workbookReader = XmlReader.Create(workbookEntry.Open(), new XmlReaderSettings { IgnoreWhitespace = true });
        while (workbookReader.Read())
        {
            if (workbookReader.NodeType == XmlNodeType.Element && workbookReader.LocalName == "sheet")
            {
                var name = workbookReader.GetAttribute("name");
                var relationId = workbookReader.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

                if (string.Equals(name, sheetName, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(relationId) &&
                    relMap.TryGetValue(relationId, out var target))
                {
                    return $"xl/{target.Replace('\\', '/')}";
                }
            }
        }

        return null;
    }

    private static List<string> ReadRow(XmlReader rowReader, List<string> sharedStrings)
    {
        var values = new SortedDictionary<int, string>();

        while (rowReader.Read())
        {
            if (rowReader.NodeType != XmlNodeType.Element || rowReader.LocalName != "c")
            {
                continue;
            }

            var reference = rowReader.GetAttribute("r");
            var type = rowReader.GetAttribute("t");
            var columnIndex = GetColumnIndex(reference);
            var value = string.Empty;

            using var cellReader = rowReader.ReadSubtree();
            while (cellReader.Read())
            {
                if (cellReader.NodeType == XmlNodeType.Element && (cellReader.LocalName == "v" || cellReader.LocalName == "t"))
                {
                    var raw = cellReader.ReadElementContentAsString();
                    if (type == "s" && int.TryParse(raw, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
                    {
                        value = sharedStrings[sharedIndex];
                    }
                    else
                    {
                        value = raw;
                    }

                    break;
                }
            }

            values[columnIndex] = value;
        }

        if (values.Count == 0)
        {
            return new List<string>();
        }

        var maxIndex = values.Keys.Max();
        var result = new List<string>(Enumerable.Repeat(string.Empty, maxIndex + 1));
        foreach (var kvp in values)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return 0;
        }

        var index = 0;
        foreach (var ch in cellReference.ToUpperInvariant())
        {
            if (!char.IsLetter(ch))
            {
                break;
            }

            index = index * 26 + (ch - 'A' + 1);
        }

        return Math.Max(0, index - 1);
    }
}
