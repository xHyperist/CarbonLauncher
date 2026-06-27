using System;

namespace CarbonLauncher.Models
{
    public sealed class ClientJarInfo
    {
        public bool Exists { get; set; }

        public string JarPath { get; set; } = string.Empty;

        public string ExpectedFileName { get; set; } = string.Empty;

        public string VersionId { get; set; } = string.Empty;

        public string Status { get; set; } = "Missing";

        public string ErrorMessage { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public DateTime? LastModifiedAt { get; set; }
    }
}
