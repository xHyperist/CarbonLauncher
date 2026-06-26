namespace CarbonLauncher.Models
{
    public sealed class JavaInfo
    {
        public bool IsDetected { get; set; }

        public string JavaPath { get; set; } = string.Empty;

        public string VersionText { get; set; } = string.Empty;

        public int MajorVersion { get; set; }

        public string Source { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
