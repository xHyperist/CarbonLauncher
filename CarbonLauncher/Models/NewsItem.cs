namespace CarbonLauncher.Models
{
    public sealed class NewsItem
    {
        public NewsItem(string title, string summary)
        {
            Title = title;
            Summary = summary;
        }

        public string Title { get; }

        public string Summary { get; }
    }
}
