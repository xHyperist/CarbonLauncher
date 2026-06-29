namespace CarbonLauncher.Models
{
    public sealed class CarbonTweakerInfo
    {
        public bool IsJarFound { get; set; }

        public bool HasTweakClass { get; set; }

        public string JarPath { get; set; } = string.Empty;

        public string TweakClass { get; set; } = string.Empty;

        public string LaunchMode { get; set; } = string.Empty;

        public string Status { get; set; } = "MissingJar";

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
