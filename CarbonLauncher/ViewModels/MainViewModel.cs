using System.Collections.ObjectModel;
using System.Windows.Input;
using CarbonLauncher.Models;
using CarbonLauncher.Services;

namespace CarbonLauncher.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly LauncherConfigService _configService;
        private readonly LauncherConfig _config;
        private LauncherVersion _selectedVersion;
        private string _currentPage;
        private string _guestUsername;
        private string _javaPath;
        private string _minecraftDirectory;
        private int _allocatedMemoryMb;
        private bool _isModalVisible;
        private string _modalTitle;
        private string _modalMessage;

        public MainViewModel()
        {
            _configService = new LauncherConfigService();
            _config = _configService.Load();

            Versions = new ObservableCollection<LauncherVersion>
            {
                new LauncherVersion("1.8.9", "Ready"),
                new LauncherVersion("1.7.10", "Coming Soon")
            };

            NavigationItems = new ObservableCollection<NavigationItemViewModel>
            {
                new NavigationItemViewModel("Home", "Play"),
                new NavigationItemViewModel("Versions", "Versions"),
                new NavigationItemViewModel("Account", "Account"),
                new NavigationItemViewModel("Settings", "Settings")
            };

            _selectedVersion = FindVersion(_config.SelectedVersion);
            _currentPage = IsKnownPage(_config.LastSelectedPage) ? _config.LastSelectedPage : "Home";
            _guestUsername = _config.GuestUsername;
            _javaPath = _config.JavaPath;
            _minecraftDirectory = _config.MinecraftDirectory;
            _allocatedMemoryMb = _config.AllocatedMemoryMb;
            _modalTitle = "Carbon Launcher";
            _modalMessage = string.Empty;

            LaunchCommand = new RelayCommand(_ => ShowModal("Launch", "Launch system is not implemented yet."));
            NavigateCommand = new RelayCommand(page => Navigate(page as string));
            SelectVersionCommand = new RelayCommand(version => SelectVersion(version as LauncherVersion));
            SaveAccountCommand = new RelayCommand(_ => SaveAccount());
            ShowComingSoonCommand = new RelayCommand(message => ShowModal("Coming Soon", message as string ?? "This feature is coming soon."));
            CloseModalCommand = new RelayCommand(_ => IsModalVisible = false);

            UpdateActiveNavigation();
            UpdateSelectedVersionState();
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public ObservableCollection<LauncherVersion> Versions { get; }

        public ICommand LaunchCommand { get; }

        public ICommand NavigateCommand { get; }

        public ICommand SelectVersionCommand { get; }

        public ICommand SaveAccountCommand { get; }

        public ICommand ShowComingSoonCommand { get; }

        public ICommand CloseModalCommand { get; }

        public double InitialWindowWidth => _config.WindowWidth;

        public double InitialWindowHeight => _config.WindowHeight;

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
                    default:
                        return "Play";
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
                        return "Choose the local Carbon Client version.";
                    case "Account":
                        return "Manage the local guest profile.";
                    case "Settings":
                        return "Configure local launcher values.";
                    default:
                        return "Start from a clean local launcher shell.";
                }
            }
        }

        public LauncherVersion SelectedVersion
        {
            get => _selectedVersion;
            private set
            {
                if (_selectedVersion != value)
                {
                    _selectedVersion = value;
                    _config.SelectedVersion = value.MinecraftVersion;
                    SaveConfig();
                    UpdateSelectedVersionState();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedVersionText));
                }
            }
        }

        public string SelectedVersionText => SelectedVersion.MinecraftVersion;

        public string GuestUsername
        {
            get => _guestUsername;
            set
            {
                string normalizedValue = string.IsNullOrWhiteSpace(value) ? "Guest" : value;
                if (_guestUsername != normalizedValue)
                {
                    _guestUsername = normalizedValue;
                    _config.GuestUsername = normalizedValue;
                    SaveConfig();
                    OnPropertyChanged();
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

        public bool IsModalVisible
        {
            get => _isModalVisible;
            private set
            {
                if (_isModalVisible != value)
                {
                    _isModalVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ModalTitle
        {
            get => _modalTitle;
            private set
            {
                if (_modalTitle != value)
                {
                    _modalTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ModalMessage
        {
            get => _modalMessage;
            private set
            {
                if (_modalMessage != value)
                {
                    _modalMessage = value;
                    OnPropertyChanged();
                }
            }
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

        private void Navigate(string? page)
        {
            if (string.IsNullOrWhiteSpace(page) || !IsKnownPage(page))
            {
                return;
            }

            CurrentPage = page;
            SaveConfig();
        }

        private void SelectVersion(LauncherVersion? version)
        {
            if (version == null)
            {
                return;
            }

            if (version.MinecraftVersion != "1.8.9")
            {
                ShowModal("Coming Soon", $"{version.MinecraftVersion} is coming soon.");
                return;
            }

            SelectedVersion = version;
            ShowModal("Version Selected", $"{version.MinecraftVersion} is selected.");
        }

        private void SaveAccount()
        {
            _config.GuestUsername = GuestUsername;
            SaveConfig();
            ShowModal("Account", "Guest username saved.");
        }

        private void ShowModal(string title, string message)
        {
            ModalTitle = title;
            ModalMessage = message;
            IsModalVisible = true;
        }

        private void UpdateActiveNavigation()
        {
            foreach (NavigationItemViewModel item in NavigationItems)
            {
                item.IsActive = item.Key == CurrentPage;
            }
        }

        private void UpdateSelectedVersionState()
        {
            foreach (LauncherVersion version in Versions)
            {
                version.IsSelected = version.MinecraftVersion == SelectedVersion.MinecraftVersion;
            }
        }

        private LauncherVersion FindVersion(string minecraftVersion)
        {
            foreach (LauncherVersion version in Versions)
            {
                if (version.MinecraftVersion == minecraftVersion && version.MinecraftVersion == "1.8.9")
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
