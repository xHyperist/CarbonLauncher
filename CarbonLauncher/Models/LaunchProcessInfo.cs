using System;

namespace CarbonLauncher.Models
{
    public sealed class LaunchProcessInfo
    {
        public bool IsRunning { get; set; }

        public bool HasStarted { get; set; }

        public bool HasExited { get; set; }

        public int? ProcessId { get; set; }

        public int? ExitCode { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? ExitedAt { get; set; }

        public string Status { get; set; } = "Not Running";

        public string ErrorMessage { get; set; } = string.Empty;

        public string LogFilePath { get; set; } = string.Empty;
    }
}
