using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class CarbonClientLaunchInfo
    {
        public bool IsJarFound { get; set; }

        public bool IsCustomLaunchSupported { get; set; }

        public string JarPath { get; set; } = string.Empty;

        public string DetectedLaunchMode { get; set; } = "Unknown";

        public string ManifestMainClass { get; set; } = string.Empty;

        public string ManifestTweakClass { get; set; } = string.Empty;

        public string FmlCorePlugin { get; set; } = string.Empty;

        public bool HasMcmodInfo { get; set; }

        public bool HasForgeModMetadata { get; set; }

        public List<string> DetectedEntries { get; set; } = new List<string>();

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> Warnings { get; set; } = new List<string>();
    }
}
