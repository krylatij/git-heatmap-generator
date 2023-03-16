using System.Globalization;
using System.Text.RegularExpressions;

namespace GitHeatmapGenerator
{
    internal class Utils
    {
        public static DateTimeOffset ParseDateArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DateTimeOffset.Now.AddYears(-1);
            }

            if (DateTimeOffset.TryParseExact(value, new[] { "yyyy-MM-dd", "dd-MM-yyyy" }, null, DateTimeStyles.None, out var result))
            {
                return result;
            }

            var regex = new Regex("last-(?<count>\\d+)-(?<unit>(day|week|month|year))s?");
            var match = regex.Match(value);

            if (!match.Success)
            {
                throw new InvalidOperationException($"Unsupported datetime format '{value}'");
            }

            var unit = match.Groups["unit"].Value;
            var count = Convert.ToInt32(match.Groups["count"].Value);

            return unit switch
            {
                "day" => DateTimeOffset.Now.AddDays(-count),
                "week" => DateTimeOffset.Now.AddDays(-count * 7),
                "month" => DateTimeOffset.Now.AddMonths(-count),
                "year" => DateTimeOffset.Now.AddYears(-count),
                _ => throw new InvalidOperationException($"Unsupported unit type {unit}")
            };
        }
    }
}
