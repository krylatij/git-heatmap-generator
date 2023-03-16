using System.Globalization;
using Alba.CsConsoleFormat;
using GitHeatmapGenerator.Dto;

namespace GitHeatmapGenerator
{
    public static class GitStatisticsVisualizer
    {
        private static readonly LineThickness _headerThickness = new (LineWidth.Double, LineWidth.Single);

        public static Document BuildRepositoryStat(UserStatisticsDto[] statistics, string repository, string[] usersToHighlight)
        {
            var doc = new Document(
                new Span("Repository: ") { Color = ConsoleColor.Yellow }, repository, Environment.NewLine,
                new Grid
                {
                    Color = ConsoleColor.Gray,
                    Columns = { GridLength.Auto, GridLength.Auto, GridLength.Auto, GridLength.Auto },
                    Children = {
                        new Cell("Author") { Stroke = _headerThickness },
                        new Cell("Commits") { Stroke = _headerThickness },
                        new Cell("Files (add/upd/del)") { Stroke = _headerThickness },
                        new Cell("Lines of code (add/del)") { Stroke = _headerThickness },
                        statistics.OrderByDescending(x => x.CommitsCount).Select(x =>
                        {
                            var color = usersToHighlight.Contains(x.User, StringComparer.InvariantCultureIgnoreCase) 
                                ? ConsoleColor.Yellow
                                : default(ConsoleColor?);

                            return new[]
                            {
                                new Cell(x.User) {Color = color},
                                new Cell(x.CommitsCount) {Align = Align.Right, Color = color},
                                new Cell(
                                        $"{x.FilesUniqueCount}({x.FilesAdded.Count}/{x.FilesModified.Count}/{x.FilesDeleted.Count})")
                                    {Align = Align.Right, Color = color},
                                new Cell($"{x.LinesCount}({x.LinesAdded}/{x.LinesDeleted})") {Align = Align.Right, Color = color},
                            };
                        })
                    }
                }
            );

            return doc;
        }

        public static Document BuildHeatmap(UserStatisticsDto statistics, string[] repositories, string[] users, string[] warnings, DateTimeOffset sinceDate)
        {
            const int weeksInYear = 53;
            const int daysInWeek = 7;

            var start = sinceDate;
            if (start.DayOfWeek != DayOfWeek.Monday)
            {
                start = start.AddDays(DayOfWeek.Monday - start.DayOfWeek);
            }

            var grid = new Grid()
            {
                Color = ConsoleColor.Black,
                Background = ConsoleColor.White,
                Stroke = LineThickness.None,
                Columns = {Enumerable.Repeat(GridLength.Auto, weeksInYear)},
            };

            foreach (var header in GetHeader())
            {
                grid.Children.Add(header);
            }

            foreach (var day in GetBody())
            {
                grid.Children.Add(day);
            }

            return new Document(
                $"Aggregated heatmap against HEAD since {sinceDate:d} for user {string.Join(',', users.Select(x => $"'{x}'"))}", Environment.NewLine,
                new Span("Repositories: "), Environment.NewLine,

                new Span(string.Join(Environment.NewLine, repositories)), Environment.NewLine,

                new Span($"Total commits: {statistics.CommitsCount}"), Environment.NewLine,
                new Span($"Total files modified: {statistics.FilesModified.Count + statistics.FilesAdded.Count}"), Environment.NewLine,
                new Span($"Total lines of code added/deleted: {statistics.LinesAdded}/{statistics.LinesDeleted}"), Environment.NewLine,

                new Span( warnings.Length > 0 ? $"Warnings: {Environment.NewLine}" + string.Join(Environment.NewLine, warnings) : "no warnings")
                    { Color = warnings.Length > 0 ? ConsoleColor.Yellow : ConsoleColor.Green}, Environment.NewLine,

                new Span("xxxxxxx") { Color = ConsoleColor.White }, " - no commits", Environment.NewLine,
                new Span("xxxxxxx") { Color = ConsoleColor.DarkGray }, " - 1-2 commits", Environment.NewLine,
                new Span("xxxxxxx") { Color = ConsoleColor.DarkCyan }, " - 3-4 commits: ", Environment.NewLine,
                new Span("xxxxxxx") { Color = ConsoleColor.DarkMagenta }, " - >4 commits", Environment.NewLine,

                grid
            );

            ConsoleColor? GetDayColor(DateTimeOffset date)
            {
                if (!statistics.CommitsByDate.TryGetValue(date.Date, out var commitsCount))
                {
                    return ConsoleColor.White;
                }

                if (commitsCount < 3)
                {
                    return ConsoleColor.DarkGray;
                }

                if (commitsCount < 5)
                {
                    return ConsoleColor.DarkCyan;
                }

                return ConsoleColor.DarkMagenta;
            }

            IEnumerable<Cell> GetBody()
            {
                for (int i = 0; i < daysInWeek; i++)
                {
                    var cursor = start.AddDays(i);

                    for (int j = 0; j < weeksInYear; j++)
                    {
                        yield return new Cell(cursor.Day.ToString("00"))
                        {
                            Background = GetDayColor(cursor), 
                            Stroke = LineThickness.Single,
                        };

                        cursor = cursor.AddDays(daysInWeek);
                    }
                }
            }

            IEnumerable<Cell> GetHeader()
            {
                var cursor = start;
                var previousMonth = cursor.Month;
                var colSpan = 0;

                for (int i = 0; i < weeksInYear; i++)
                {
                    if (cursor.Month != previousMonth)
                    {
                        previousMonth = cursor.Month;

                        yield return new Cell(GetMonth(cursor.Month)){ColumnSpan = colSpan, Color = ConsoleColor.Black, Stroke = _headerThickness};
                        colSpan = 1;
                    }
                    else
                    {
                        colSpan++;
                    }

                    cursor = cursor.AddDays(daysInWeek);
                }

                if (colSpan > 1)
                {
                    yield return new Cell(GetMonth(cursor.Month + 1)) {ColumnSpan = colSpan, Color = ConsoleColor.Black, Stroke = _headerThickness };
                }

                string GetMonth(int month)
                {
                    return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month == 1
                        ? 12
                        : month - 1);
                }
            }
        }

    }
}
