using System;

namespace CarbonLauncher.Models
{
    public sealed class LaunchSession
    {
        public bool IsAuthenticated { get; set; }

        public bool IsGuestMode { get; set; } = true;

        public string Username { get; set; } = string.Empty;

        public string Uuid { get; set; } = string.Empty;

        public string AccessToken { get; set; } = "0";

        public string UserType { get; set; } = "legacy";

        public string SessionType { get; set; } = "offline";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
