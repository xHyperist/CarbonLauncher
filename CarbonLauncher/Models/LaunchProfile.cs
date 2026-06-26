using System;
using System.Collections.Generic;

namespace CarbonLauncher.Models
{
    public sealed class LaunchProfile
    {
        public string ProfileName { get; set; } = "Carbon 1.8.9";

        public string MinecraftVersion { get; set; } = "1.8.9";

        public string LoaderType { get; set; } = "Forge";

        public string JavaPath { get; set; } = string.Empty;

        public string MinecraftDirectory { get; set; } = string.Empty;

        public string GameDirectory { get; set; } = string.Empty;

        public string Username { get; set; } = "CarbonPlayer";

        public int AllocatedMemoryMb { get; set; } = 2048;

        public List<string> JvmArguments { get; set; } = new List<string>();

        public List<string> GameArguments { get; set; } = new List<string>();

        public bool IsGuestMode { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
