using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class LaunchValidationResult
    {
        public bool IsValid { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> Warnings { get; set; } = new List<string>();

        public string Summary { get; set; } = string.Empty;
    }
}
