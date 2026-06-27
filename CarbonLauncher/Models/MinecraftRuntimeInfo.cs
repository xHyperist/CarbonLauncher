using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class MinecraftRuntimeInfo
    {
        public bool IsResolved { get; set; }

        public string MinecraftVersion { get; set; } = string.Empty;

        public string VersionDirectory { get; set; } = string.Empty;

        public string VersionJsonPath { get; set; } = string.Empty;

        public string ClientJarPath { get; set; } = string.Empty;

        public string AssetsDirectory { get; set; } = string.Empty;

        public string AssetIndex { get; set; } = string.Empty;

        public string AssetIndexPath { get; set; } = string.Empty;

        public string LibrariesDirectory { get; set; } = string.Empty;

        public string NativesDirectory { get; set; } = string.Empty;

        public string MainClass { get; set; } = string.Empty;

        public List<string> LibraryPaths { get; set; } = new List<string>();

        public List<string> MissingLibraries { get; set; } = new List<string>();

        public List<string> NativeLibraryJarPaths { get; set; } = new List<string>();

        public List<string> MissingNativeLibraryJarPaths { get; set; } = new List<string>();

        public List<string> ExtractedNativeFiles { get; set; } = new List<string>();

        public bool AreNativesPrepared { get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> Warnings { get; set; } = new List<string>();
    }
}
