using System.Text;
using System.Text.RegularExpressions;
using MeterDataService.Models;

namespace MeterDataService.Workers;

public static partial class DlmsMessageConverter
{
    // DLMS full OBIS → short register name (only the four tracked registers)
    private static readonly Dictionary<string, string> ObisMapping = new()
    {
        { "1.1.1.8.0.255", "1.8.0" },
        { "1.1.1.8.1.255", "1.8.1" },
        { "1.1.1.8.2.255", "1.8.2" },
        { "1.1.2.8.0.255", "2.8.0" },
    };

    /// <summary>
    /// Parses the "columns" entry value to build a mapping from column position (1-based)
    /// to short OBIS code (e.g., position 3 → "1.8.0"). Only positions with recognized
    /// OBIS codes are included.
    /// </summary>
    public static Dictionary<int, string> ParseColumnsMapping(string columnsValue)
    {
        var result = new Dictionary<int, string>();
        var lines = columnsValue.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < lines.Length; i++)
        {
            var match = ObisRegex().Match(lines[i]);
            if (match.Success && ObisMapping.TryGetValue(match.Groups[1].Value, out var shortObis))
            {
                result[i + 1] = shortObis; // 1-based position
            }
        }

        return result;
    }

    /// <summary>
    /// Parses a single row line from the "values" entry.
    /// Returns a dictionary mapping column position to the raw value string.
    /// Example: "1:1771313400[TIMESTAMP],2:0[UINT8],3:85646360[UINT64]" →
    /// { 1: "1771313400", 2: "0", 3: "85646360" }
    /// </summary>
    public static Dictionary<int, string> ParseValuesRow(string rowLine)
    {
        var result = new Dictionary<int, string>();
        var matches = CellRegex().Matches(rowLine);

        foreach (Match m in matches)
        {
            if (int.TryParse(m.Groups[1].Value, out var pos))
            {
                result[pos] = m.Groups[2].Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a DlmsMessage to one MeterMessage per timestamped row in the "values" data entry.
    /// Each resulting MeterMessage has a synthesized result.data in IEC 62056-21 format
    /// so that all existing providers can parse and store the data unchanged.
    /// </summary>
    public static IEnumerable<MeterMessage> ToMeterMessages(DlmsMessage dlms)
    {
        var columnsEntry = dlms.Result.Data.FirstOrDefault(e => e.Attr == "columns");
        var valuesEntry = dlms.Result.Data.FirstOrDefault(e => e.Attr == "values");

        if (columnsEntry == null || valuesEntry == null)
        {
            yield break;
        }

        var columnsMapping = ParseColumnsMapping(columnsEntry.Value);
        if (columnsMapping.Count == 0)
        {
            yield break;
        }

        var valueLines = valuesEntry.Value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in valueLines)
        {
            var row = ParseValuesRow(line);

            // Position 1 is always the TIMESTAMP (Unix epoch as integer string)
            if (!row.TryGetValue(1, out var tsRaw) || !long.TryParse(tsRaw, out _))
            {
                continue; // Skip invalid rows
            }

            // Build IEC 62056-21 synthetic data string: "1.8.0(85646360)\r\n1.8.1(...)\r\n"
            var sb = new StringBuilder();
            foreach (var (colPos, shortObis) in columnsMapping.OrderBy(kv => kv.Key))
            {
                if (row.TryGetValue(colPos, out var val))
                {
                    sb.Append($"{shortObis}({val})\r\n");
                }
            }

            yield return new MeterMessage
            {
                Utc = tsRaw,                       // Unix epoch string
                Id = dlms.Id.ToString(),
                M = dlms.M,
                N = dlms.N.ToString(),
                Network = dlms.Network,
                System = dlms.System,
                Cname = dlms.Cname,
                Cdesc = dlms.Cdesc,
                Model = dlms.Model,
                Sn = dlms.Sn,
                Fid = dlms.Fid,
                Result = new MeterResult
                {
                    Enc = dlms.Result.Enc,
                    Data = sb.ToString()
                }
            };
        }
    }

    [GeneratedRegex(@"2:([^[]+)\[OBIS\]")]
    private static partial Regex ObisRegex();

    [GeneratedRegex(@"(\d+):([^[]+)\[")]
    private static partial Regex CellRegex();
}

