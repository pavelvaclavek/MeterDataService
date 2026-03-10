using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MeterDataService.Net48.Models;

namespace MeterDataService.Net48.Workers
{
    public static class DlmsMessageConverter
    {
        private static readonly Regex ObisRegex = new Regex(@"2:([^[]+)\[OBIS\]", RegexOptions.Compiled);
        private static readonly Regex CellRegex = new Regex(@"(\d+):([^[]+)\[", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> ObisMapping = new Dictionary<string, string>
        {
            { "1.1.1.8.0.255", "1.8.0" },
            { "1.1.1.8.1.255", "1.8.1" },
            { "1.1.1.8.2.255", "1.8.2" },
            { "1.1.2.8.0.255", "2.8.0" },
        };

        public static Dictionary<int, string> ParseColumnsMapping(string columnsValue)
        {
            var result = new Dictionary<int, string>();
            var lines = columnsValue.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                var match = ObisRegex.Match(lines[i]);
                string shortObis;
                if (match.Success && ObisMapping.TryGetValue(match.Groups[1].Value, out shortObis))
                {
                    result[i + 1] = shortObis;
                }
            }

            return result;
        }

        public static Dictionary<int, string> ParseValuesRow(string rowLine)
        {
            var result = new Dictionary<int, string>();
            var matches = CellRegex.Matches(rowLine);

            foreach (Match m in matches)
            {
                int pos;
                if (int.TryParse(m.Groups[1].Value, out pos))
                {
                    result[pos] = m.Groups[2].Value;
                }
            }

            return result;
        }

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

                string tsRaw;
                long dummy;
                if (!row.TryGetValue(1, out tsRaw) || !long.TryParse(tsRaw, out dummy))
                {
                    continue;
                }

                var sb = new StringBuilder();
                foreach (var kv in columnsMapping.OrderBy(kv => kv.Key))
                {
                    string val;
                    if (row.TryGetValue(kv.Key, out val))
                    {
                        sb.AppendFormat("{0}({1})\r\n", kv.Value, val);
                    }
                }

                yield return new MeterMessage
                {
                    Utc = tsRaw,
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
    }
}
