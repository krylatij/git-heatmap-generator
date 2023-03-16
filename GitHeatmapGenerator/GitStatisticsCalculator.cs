using System.Text.RegularExpressions;
using GitHeatmapGenerator.Dto;
using LibGit2Sharp;

namespace GitHeatmapGenerator;

public static class GitStatisticsCalculator
{
    public static StatisticsResponse GetAuthorStatistics(string repositoryPath, DateTimeOffset? sinceDate, Regex? excludeFilesRegex)
    {
        Repository repo;
        try
        {
            repo = new Repository(repositoryPath);
        }
        catch (Exception e)
        {
            return new StatisticsResponse
            {
                Message = $"Failed to analyze '{repositoryPath}' with error: '{e.Message}'"
            };
        }

        var result = new List<UserStatisticsDto>();

        foreach (var authorGroup in repo.Head.Commits.GroupBy(x => x.Author.Name))
        {
            var commits = authorGroup.ToArray();

            var entry = new UserStatisticsDto(authorGroup.Key, 
                commits
                    .Where(x => sinceDate == null || x.Committer.When >= sinceDate)
                    .GroupBy(x => x.Author.When.Date)
                    .ToDictionary(x => 
                        x.Key.Date, 
                        x => x.Count()));

            foreach (var commit in commits)
            {
                UpdateFilesStats(commit, ref entry);
                UpdateCodeStats(commit, ref entry);
            }
            result.Add(entry);
        }

        return new StatisticsResponse()
        {
            Items = result.ToArray()
        };

        void UpdateCodeStats(Commit commit, ref UserStatisticsDto userStat)
        {
            var parent = GetParent(commit);

            var changes = repo.Diff.Compare<Patch>(parent?.Tree, commit.Tree);

            foreach (var change in changes)
            {
                var fileName = Path.GetFileName(change.Path);

                if (excludeFilesRegex == null || !excludeFilesRegex.IsMatch(fileName))
                {
                    userStat.LinesAdded += change.LinesAdded;
                    userStat.LinesDeleted += change.LinesDeleted;
                }
            }
        }

        void UpdateFilesStats(Commit commit, ref UserStatisticsDto userStat)
        {
            var parent = GetParent(commit);

            var changes = repo.Diff.Compare<TreeChanges>(parent?.Tree, commit.Tree);

            userStat.CommitsCount++;

            UpdateFileList(userStat.FilesAdded, changes.Added);
            UpdateFileList(userStat.FilesModified, changes.Modified);
            UpdateFileList(userStat.FilesDeleted, changes.Deleted);

            void UpdateFileList(HashSet<string> resultList, IEnumerable<TreeEntryChanges> listChanges)
            {
                foreach (var listChange in listChanges)
                {
                    var fileName = Path.GetFileName(listChange.Path);

                    if (excludeFilesRegex == null || !excludeFilesRegex.IsMatch(fileName))
                    {
                        resultList.Add(listChange.Path);
                    }
                }
            }
        }

        Commit? GetParent(Commit commit)
        {
            return commit.Parents.FirstOrDefault();
        }
    }
}