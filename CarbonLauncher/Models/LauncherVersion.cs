namespace CarbonLauncher.Models
{
    public sealed class LauncherVersion
    {
        public LauncherVersion(string minecraftVersion, string status)
        {
            MinecraftVersion = minecraftVersion;
            Status = status;
        }

        public string MinecraftVersion { get; }

        public string Status { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(Status)
            ? MinecraftVersion
            : $"{MinecraftVersion} {Status}";
    }
}
