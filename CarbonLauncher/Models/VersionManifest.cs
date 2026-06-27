using System;
using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class VersionManifest
    {
        public string ManifestVersion { get; set; } = "1";

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<LauncherVersion> Versions { get; set; } = new List<LauncherVersion>();
    }
}
