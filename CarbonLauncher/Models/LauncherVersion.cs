using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CarbonLauncher.Models
{
    public sealed class LauncherVersion : INotifyPropertyChanged
    {
        private bool _isSelected;

        public LauncherVersion()
        {
        }

        public LauncherVersion(string minecraftVersion, string status)
        {
            Id = $"carbon-{minecraftVersion}";
            DisplayName = $"Carbon {minecraftVersion}";
            MinecraftVersion = minecraftVersion;
            LoaderType = "Forge";
            Status = status;
            IsAvailable = status != "Coming Soon";
            IsComingSoon = status == "Coming Soon";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string MinecraftVersion { get; set; } = string.Empty;

        public string LoaderType { get; set; } = "Forge";

        public string Status { get; set; } = string.Empty;

        public bool IsAvailable { get; set; }

        public bool IsComingSoon { get; set; }

        public string Description { get; set; } = string.Empty;

        public string LocalJarPath { get; set; } = string.Empty;

        public string RemoteManifestUrl { get; set; } = string.Empty;

        public string ReleaseChannel { get; set; } = string.Empty;

        public DateTime? ReleaseDate { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
