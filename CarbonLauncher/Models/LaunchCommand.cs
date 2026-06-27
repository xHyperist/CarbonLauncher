using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class LaunchCommand
    {
        public bool IsBuildable { get; set; }

        public string JavaExecutablePath { get; set; } = string.Empty;

        public string WorkingDirectory { get; set; } = string.Empty;

        public string MainClass { get; set; } = string.Empty;

        public List<string> JvmArguments { get; set; } = new List<string>();

        public List<string> GameArguments { get; set; } = new List<string>();

        public List<string> ClasspathEntries { get; set; } = new List<string>();

        public string FullCommandPreview { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new List<string>();

        public List<string> Warnings { get; set; } = new List<string>();
    }
}
