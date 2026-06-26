using System.Collections.Generic;
using CarbonLauncher.Models;

namespace CarbonLauncher.Services
{
    public sealed class LauncherStateService
    {
        public IReadOnlyList<LauncherVersion> GetAvailableVersions()
        {
            return new[]
            {
                new LauncherVersion("1.8.9", "Available"),
                new LauncherVersion("1.7.10", "Coming Soon")
            };
        }

        public IReadOnlyList<NewsItem> GetNewsItems()
        {
            return new[]
            {
                new NewsItem("v0.1.0 Skeleton", "Premium WPF launcher shell, Carbon theme, and MVVM-friendly structure."),
                new NewsItem("Coming Next", "Authentication, update orchestration, and launch pipeline will be wired in later.")
            };
        }
    }
}
