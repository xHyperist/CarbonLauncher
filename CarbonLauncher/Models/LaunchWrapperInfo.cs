namespace CarbonLauncher.Models
{
    public sealed class LaunchWrapperInfo
    {
        public bool IsRequired { get; set; }

        public bool IsFound { get; set; }

        public string ExpectedPath { get; set; } = string.Empty;

        public string Version { get; set; } = "1.12";

        public string Status { get; set; } = "NotRequired";

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
