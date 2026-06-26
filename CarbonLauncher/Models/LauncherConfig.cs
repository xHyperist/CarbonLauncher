namespace CarbonLauncher.Models
{
    public sealed class LauncherConfig
    {
        public string SelectedVersion { get; set; } = "1.8.9";

        public string GuestUsername { get; set; } = "CarbonPlayer";

        public string JavaPath { get; set; } = string.Empty;

        public string MinecraftDirectory { get; set; } = string.Empty;

        public int AllocatedMemoryMb { get; set; } = 2048;

        public string LastSelectedPage { get; set; } = "Home";

        public double WindowWidth { get; set; } = 1180;

        public double WindowHeight { get; set; } = 720;
    }
}
