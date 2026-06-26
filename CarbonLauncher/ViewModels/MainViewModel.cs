using System.Collections.ObjectModel;
using System.Windows.Input;
using CarbonLauncher.Models;
using CarbonLauncher.Services;

namespace CarbonLauncher.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly LauncherStateService _launcherStateService;
        private LauncherVersion _selectedVersion;
        private string _statusText;

        public MainViewModel()
        {
            _launcherStateService = new LauncherStateService();
            Versions = new ObservableCollection<LauncherVersion>(_launcherStateService.GetAvailableVersions());
            NewsItems = new ObservableCollection<NewsItem>(_launcherStateService.GetNewsItems());
            _selectedVersion = Versions[0];
            _statusText = "UI Skeleton Ready";
            AccountName = "Guest Mode";
            UpdateStatus = "No downloads running";
            UpdateProgress = 0;
            LaunchCommand = new RelayCommand(_ => StatusText = "Launch flow placeholder");
        }

        public ObservableCollection<LauncherVersion> Versions { get; }

        public ObservableCollection<NewsItem> NewsItems { get; }

        public ICommand LaunchCommand { get; }

        public string AccountName { get; }

        public string UpdateStatus { get; }

        public double UpdateProgress { get; }

        public string UpdateProgressText => $"{UpdateProgress:0}%";

        public LauncherVersion SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                if (_selectedVersion != value)
                {
                    _selectedVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
