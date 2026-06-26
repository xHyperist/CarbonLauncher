using System.Collections.ObjectModel;
using System.Windows.Input;
using CarbonLauncher.Models;
using CarbonLauncher.Services;

namespace CarbonLauncher.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly LauncherConfigService _configService;
        private readonly LauncherStateService _launcherStateService;
        private readonly LauncherConfig _config;
        private LauncherVersion _selectedVersion;
        private string _currentPage;
        private string _guestUsername;
        private string _javaPath;
        private string _minecraftDirectory;
        private int _allocatedMemoryMb;
        private string _statusText;

        public MainViewModel()
        {
            _configService = new LauncherConfigService();
            _config = _configService.Load();
            _launcherStateService = new LauncherStateService();
            Versions = new ObservableCollection<LauncherVersion>(_launcherStateService.GetAvailableVersions());
            NewsItems = new ObservableCollection<NewsItem>(_launcherStateService.GetNewsItems());
            NavigationItems = new ObservableCollection<NavigationItemViewModel>
            {
                new NavigationItemViewModel("Home", "Home / Play"),
                new NavigationItemViewModel("Versions", "Versions"),
                new NavigationItemViewModel("Account", "Account"),
                new NavigationItemViewModel("Settings", "Settings"),
                new NavigationItemViewModel("News", "News")
            };
            _selectedVersion = FindVersion(_config.SelectedVersion);
            _currentPage = IsKnownPage(_config.LastSelectedPage) ? _config.LastSelectedPage : "Home";
            _guestUsername = _config.GuestUsername;
            _javaPath = _config.JavaPath;
            _minecraftDirectory = _config.MinecraftDirectory;
            _allocatedMemoryMb = _config.AllocatedMemoryMb;
            _statusText = "UI Skeleton Ready";
            UpdateStatus = "No downloads running";
            UpdateProgress = 0;
            LaunchCommand = new RelayCommand(_ => StatusText = "Launch flow placeholder");
            NavigateCommand = new RelayCommand(page => Navigate(page as string));
            UpdateActiveNavigation();
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public ObservableCollection<LauncherVersion> Versions { get; }

        public ObservableCollection<NewsItem> NewsItems { get; }

        public ICommand LaunchCommand { get; }

        public ICommand NavigateCommand { get; }

        public string AccountName => $"{(string.IsNullOrWhiteSpace(GuestUsername) ? "Guest" : GuestUsername)} Mode";

        public double InitialWindowWidth => _config.WindowWidth;

        public double InitialWindowHeight => _config.WindowHeight;

        public string UpdateStatus { get; }

        public double UpdateProgress { get; }

        public string UpdateProgressText => $"{UpdateProgress:0}%";

        public string CurrentPage
        {
            get => _currentPage;
            private set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CurrentPageTitle));
                    OnPropertyChanged(nameof(CurrentPageSubtitle));
                    UpdateActiveNavigation();
                }
            }
        }

        public string CurrentPageTitle
        {
            get
            {
                switch (CurrentPage)
                {
                    case "Versions":
                        return "Versions";
                    case "Account":
                        return "Account";
                    case "Settings":
                        return "Settings";
                    case "News":
                        return "News";
                    default:
                        return "Play Carbon Client";
                }
            }
        }

        public string CurrentPageSubtitle
        {
            get
            {
                switch (CurrentPage)
                {
                    case "Versions":
                        return "Manage available and upcoming Carbon Client game versions.";
                    case "Account":
                        return "Account state placeholder for future premium authentication.";
                    case "Settings":
                        return "Local launcher configuration placeholders.";
                    case "News":
                        return "Carbon Launcher changelog and news placeholders.";
                    default:
                        return "Premium Windows launcher foundation for Carbon Client.";
                }
            }
        }

        public LauncherVersion SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                if (value != null && _selectedVersion != value)
                {
                    _selectedVersion = value;
                    _config.SelectedVersion = value.MinecraftVersion;
                    SaveConfig();
                    OnPropertyChanged();
                }
            }
        }

        public string GuestUsername
        {
            get => _guestUsername;
            set
            {
                string normalizedValue = value ?? string.Empty;
                if (_guestUsername != normalizedValue)
                {
                    _guestUsername = normalizedValue;
                    _config.GuestUsername = string.IsNullOrWhiteSpace(normalizedValue) ? "Guest" : normalizedValue;
                    SaveConfig();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(AccountName));
                }
            }
        }

        public string JavaPath
        {
            get => _javaPath;
            set
            {
                string normalizedValue = value ?? string.Empty;
                if (_javaPath != normalizedValue)
                {
                    _javaPath = normalizedValue;
                    _config.JavaPath = normalizedValue;
                    SaveConfig();
                    OnPropertyChanged();
                }
            }
        }

        public string MinecraftDirectory
        {
            get => _minecraftDirectory;
            set
            {
                string normalizedValue = value ?? string.Empty;
                if (_minecraftDirectory != normalizedValue)
                {
                    _minecraftDirectory = normalizedValue;
                    _config.MinecraftDirectory = normalizedValue;
                    SaveConfig();
                    OnPropertyChanged();
                }
            }
        }

        public int AllocatedMemoryMb
        {
            get => _allocatedMemoryMb;
            set
            {
                int normalizedValue = value < 512 ? 512 : value;
                if (_allocatedMemoryMb != normalizedValue)
                {
                    _allocatedMemoryMb = normalizedValue;
                    _config.AllocatedMemoryMb = normalizedValue;
                    SaveConfig();
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

        private void Navigate(string? page)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return;
            }

            CurrentPage = page;
            SaveConfig();
            StatusText = $"{CurrentPageTitle} selected";
        }

        public void SaveWindowState(double width, double height)
        {
            if (width > 0)
            {
                _config.WindowWidth = width;
            }

            if (height > 0)
            {
                _config.WindowHeight = height;
            }

            SaveConfig();
        }

        private void UpdateActiveNavigation()
        {
            foreach (NavigationItemViewModel item in NavigationItems)
            {
                item.IsActive = item.Key == CurrentPage;
            }
        }

        private LauncherVersion FindVersion(string minecraftVersion)
        {
            foreach (LauncherVersion version in Versions)
            {
                if (version.MinecraftVersion == minecraftVersion)
                {
                    return version;
                }
            }

            return Versions[0];
        }

        private bool IsKnownPage(string page)
        {
            foreach (NavigationItemViewModel item in NavigationItems)
            {
                if (item.Key == page)
                {
                    return true;
                }
            }

            return false;
        }

        private void SaveConfig()
        {
            _config.LastSelectedPage = CurrentPage;
            _configService.Save(_config);
        }
    }
}
