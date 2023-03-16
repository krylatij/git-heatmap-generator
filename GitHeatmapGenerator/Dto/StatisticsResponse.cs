namespace GitHeatmapGenerator.Dto
{
    public class StatisticsResponse
    {
        public string Message { get; set; }

        public UserStatisticsDto[] Items { get; set; }

        public bool IsSuccessful => string.IsNullOrEmpty(Message);
    }
}
