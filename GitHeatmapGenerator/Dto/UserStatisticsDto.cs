using System.Diagnostics;

namespace GitHeatmapGenerator.Dto;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public record UserStatisticsDto(string User, Dictionary<DateTime, int> CommitsByDate)
{
    public int LinesAdded { get; set; }

    public int LinesDeleted { get; set; }

    public int LinesCount => LinesAdded + LinesDeleted;

    public HashSet<string> FilesAdded { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public HashSet<string> FilesModified { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public HashSet<string> FilesDeleted { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    public int FilesCount => FilesAdded.Count + FilesModified.Count + FilesDeleted.Count;

    public int FilesUniqueCount => new[] {FilesAdded, FilesModified, FilesDeleted}.SelectMany(x => x).Distinct().Count();

    public int CommitsCount { get; set; }

    private string DebuggerDisplay => $"{User} - files: {FilesCount}, commits: {CommitsCount}, lines (added/deleted): {LinesAdded}/{LinesDeleted}";

    public void Merge(UserStatisticsDto patch)
    {
        LinesAdded += patch.LinesAdded;
        LinesDeleted += patch.LinesDeleted;

        FilesAdded.UnionWith(patch.FilesAdded);
        FilesModified.UnionWith(patch.FilesModified);
        FilesDeleted.UnionWith(patch.FilesDeleted);

        CommitsCount += patch.CommitsCount;

        foreach (var (commitDate, count) in patch.CommitsByDate)
        {
            if (CommitsByDate.ContainsKey(commitDate))
            {
                CommitsByDate[commitDate] += count;
            }
            else
            {
                CommitsByDate.Add(commitDate, count);
            }
        }
    }
}