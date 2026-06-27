namespace CarbonLauncher.Models
{
    public sealed class LauncherStorageInfo
    {
        public string RootDirectory { get; set; } = string.Empty;

        public string ConfigFilePath { get; set; } = string.Empty;

        public string VersionManifestPath { get; set; } = string.Empty;

        public string ClientDirectory { get; set; } = string.Empty;

        public string LogsDirectory { get; set; } = string.Empty;

        public string CacheDirectory { get; set; } = string.Empty;

        public string RuntimeDirectory { get; set; } = string.Empty;

        public string TempDirectory { get; set; } = string.Empty;

        public string CrashReportsDirectory { get; set; } = string.Empty;

        public bool IsReady { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
