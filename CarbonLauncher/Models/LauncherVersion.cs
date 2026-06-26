using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CarbonLauncher.Models
{
    public sealed class LauncherVersion : INotifyPropertyChanged
    {
        private bool _isSelected;

        public LauncherVersion(string minecraftVersion, string status)
        {
            MinecraftVersion = minecraftVersion;
            Status = status;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string MinecraftVersion { get; }

        public string Status { get; }

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

        public string DisplayName => string.IsNullOrWhiteSpace(Status)
            ? MinecraftVersion
            : $"{MinecraftVersion} {Status}";

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
