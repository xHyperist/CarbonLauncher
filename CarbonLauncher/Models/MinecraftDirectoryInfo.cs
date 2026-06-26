namespace CarbonLauncher.Models
{
    public sealed class MinecraftDirectoryInfo
    {
        public bool IsDetected { get; set; }

        public bool IsValid { get; set; }

        public string DirectoryPath { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public bool HasVersionsFolder { get; set; }

        public bool HasAssetsFolder { get; set; }

        public bool HasLibrariesFolder { get; set; }
    }
}
