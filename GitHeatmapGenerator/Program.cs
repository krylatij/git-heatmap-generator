// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text.RegularExpressions;
using Alba.CsConsoleFormat;
using GitHeatmapGenerator.Dto;
using McMaster.Extensions.CommandLineUtils;
using ShellProgressBar;

namespace GitHeatmapGenerator;

class Program
{
    [Option("-r|--repository <REPOSITORY>", "The path to the folder with git repository. Multiple values allowed.", CommandOptionType.MultipleValue)]
    public string[] Repository { get; }

    [Option("-u|--user <USER>", "Optional. Name of the user to display aggregated statistics. Multiple values allowed.", CommandOptionType.MultipleValue)]
    public string[] User { get; }

    [Option("-e|--exclude <EXCLUDE>", "Optional. Regex for files to ignore during calculations. e.g. '*.md', 'package-lock.json'", CommandOptionType.SingleValue)]
    public string Exclude { get; }

    [Option("-s|--since <SINCE>", "Optional. Date to look commits from. e.g. '2022-12-21', '21-12-2022', 'last-2-month', 'last-10-years'.", CommandOptionType.SingleValue)]
    public string Since { get; }


    public static int Main(string[] args)
        => CommandLineApplication.Execute<Program>(args);

    private void OnExecute()
    {
        var st = Stopwatch.StartNew();
        try
        {
            var sinceDate = Utils.ParseDateArgument(Since);

            var progressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '─',
                ProgressBarOnBottom = true,
                ForegroundColor = ConsoleColor.DarkMagenta
            };

            using var progressBar = new ProgressBar(Repository.Length, $"Processing {Repository.Length} repositories.", progressBarOptions);

            var excludeFilesRegex = string.IsNullOrEmpty(Exclude) ? null : new Regex(Exclude);

            var statistics = new List<UserStatisticsDto>();
            var graphs = new List<Document>();

           
            var finalMessages = new List<string>();
            for (var i = 0; i < Repository.Length; i++)
            {
                var repositoryPath = Repository[i];
                var repoStat = GitStatisticsCalculator.GetAuthorStatistics(repositoryPath, sinceDate, excludeFilesRegex);
                if (repoStat.IsSuccessful)
                {
                    statistics.AddRange(repoStat.Items);

                    var doc = GitStatisticsVisualizer.BuildRepositoryStat(repoStat.Items, repositoryPath, User);
                    graphs.Add(doc);
                }
                else
                {
                    finalMessages.Add(repoStat.Message);
                }

                progressBar.Tick($"{i+1} of {Repository.Length} repositories processed.");
            }

            progressBar.Dispose();

            if (User.Length > 0)
            {
                var userStats = statistics.Where(x =>
                    User.Any(u => string.Equals(u, x.User, StringComparison.InvariantCultureIgnoreCase))).ToArray();

                if (userStats.Length == 0)
                {
                    Console.WriteLine($"No commits from user '{string.Join(',', User)}' were found in any repo for requested timeline.");
                }

                var aggregatedStat = new UserStatisticsDto(User[0], new());

                foreach (var userStatDto in userStats)
                {
                    aggregatedStat.Merge(userStatDto);
                }

                var doc = GitStatisticsVisualizer.BuildHeatmap(aggregatedStat, Repository, User, finalMessages.ToArray(), sinceDate);
                graphs.Add(doc);
            }

            foreach (var document in graphs)
            {
                ConsoleRenderer.RenderDocument(document);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            Console.WriteLine($"Completed in {st.Elapsed}");
        }
    }
}