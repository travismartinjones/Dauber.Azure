using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dauber.Azure.DocumentDb
{
    public class MostCostlyParametersLogger : ITelemetryLogger
    {
        private class ParameterLoggerValue
        {
            public string Content { get; set; }
            public double RequestCharge { get; set; }
        }

        private static readonly ConcurrentDictionary<string,ParameterLoggerValue> MostCostly = new ConcurrentDictionary<string, ParameterLoggerValue>();

        public async Task Log(string type, double requestCharge, double duration, string methodName, string file, int lineNumber,  string context)
        {
            var key = $"{file}.{methodName}{lineNumber}";

            if (MostCostly.ContainsKey(key) && MostCostly[key].RequestCharge < requestCharge) return;

            var formattedContent = context;
            var match = Regex.Match(context, $"WHERE(.+)", RegexOptions.Singleline);
            if (match.Success)
                formattedContent = match.Groups[1].Value;

            MostCostly.AddOrUpdate(key, s => new ParameterLoggerValue
            {
                Content = formattedContent,
                RequestCharge = requestCharge
            }, (s, value) =>
            {
                value.Content = formattedContent;
                value.RequestCharge = requestCharge;
                return value;
            });
        }

        private static string CleanupContentForCsv(string content)
        {
            var stringBuilder = new StringBuilder(content);
            stringBuilder.Replace("\r", "");
            stringBuilder.Replace("\n", "");
            stringBuilder.Replace("\\\"", "'");
            stringBuilder.Replace("root", "c");
            var value = stringBuilder.ToString();
            return value.EndsWith(" }\"") ? value.Substring(0, value.Length - 3) : content;
        }

        public static string ToCsv()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Source,Request Units,Content");
            var keys = MostCostly.Keys.ToList();
            foreach (var key in keys)
            {
                var value = MostCostly[key];
                stringBuilder.AppendLine($"{key},{value.RequestCharge},\"{CleanupContentForCsv(value.Content)}\"");
            }
            return stringBuilder.ToString();
        }
    }
}