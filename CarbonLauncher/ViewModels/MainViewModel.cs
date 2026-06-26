using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CarbonLauncher.Models;
using CarbonLauncher.Services;

namespace CarbonLauncher.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly LauncherConfigService _configService;
        private readonly JavaDetectionService _javaDetectionService;
        private readonly MinecraftDirectoryService _minecraftDirectoryService;
        private readonly LaunchProfileService _launchProfileService;
        private readonly LauncherConfig _config;
        private LauncherVersion _selectedVersion;
        private JavaInfo _currentJava;
        private MinecraftDirectoryInfo _currentMinecraftDirectory;
        private LaunchProfile _currentLaunchProfile;
        private LaunchValidationResult _currentLaunchValidation;
        private string _currentPage;
        private string _guestUsername;
        private string _playerIgnInput;
        private string _playerIgnErrorText;
        private string _javaPath;
        private string _minecraftDirectory;
        private int _allocatedMemoryMb;
        private bool _isModalVisible;
        private string _modalTitle;
        private string _modalMessage;

        public MainViewModel()
        {
            _configService = new LauncherConfigService();
            _javaDetectionService = new JavaDetectionService();
            _minecraftDirectoryService = new MinecraftDirectoryService();
            _launchProfileService = new LaunchProfileService();
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
            _playerIgnInput = _guestUsername;
            _playerIgnErrorText = ValidateOfflineIgn(_playerIgnInput);
            _javaPath = _config.JavaPath;
            _minecraftDirectory = _config.MinecraftDirectory;
            _allocatedMemoryMb = _config.AllocatedMemoryMb;
            _currentJava = new JavaInfo
            {
                IsDetected = false,
                Source = "Not Found",
                ErrorMessage = "Java has not been checked yet."
            };
            _currentMinecraftDirectory = new MinecraftDirectoryInfo
            {
                IsDetected = false,
                IsValid = false,
                Source = "Not Found",
                ErrorMessage = "Minecraft directory has not been checked yet."
            };
            _currentLaunchProfile = new LaunchProfile();
            _currentLaunchValidation = new LaunchValidationResult
            {
                IsValid = false,
                Summary = "Launch profile has not been checked yet."
            };
            _modalTitle = "Carbon Launcher";
            _modalMessage = string.Empty;

            LaunchCommand = new RelayCommand(_ => ShowLaunchValidationModal());
            NavigateCommand = new RelayCommand(page => Navigate(page as string));
            SelectVersionCommand = new RelayCommand(version => SelectVersion(version as LauncherVersion));
            SaveAccountCommand = new RelayCommand(_ => SaveAccount());
            DetectJavaCommand = new RelayCommand(_ => DetectJava());
            DetectMinecraftDirectoryCommand = new RelayCommand(_ => DetectMinecraftDirectory());
            RefreshLaunchProfileCommand = new RelayCommand(_ => RefreshLaunchProfile());
            ShowComingSoonCommand = new RelayCommand(message => ShowModal("Coming Soon", message as string ?? "This feature is coming soon."));
            CloseModalCommand = new RelayCommand(_ => IsModalVisible = false);

            DetectJava();
            DetectMinecraftDirectory();
            RefreshLaunchProfile();
            UpdateActiveNavigation();
            UpdateSelectedVersionState();
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public ObservableCollection<LauncherVersion> Versions { get; }

        public ICommand LaunchCommand { get; }

        public ICommand NavigateCommand { get; }

        public ICommand SelectVersionCommand { get; }

        public ICommand SaveAccountCommand { get; }

        public ICommand DetectJavaCommand { get; }

        public ICommand DetectMinecraftDirectoryCommand { get; }

        public ICommand RefreshLaunchProfileCommand { get; }

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
                        return "Manage the offline player name.";
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
                    RefreshLaunchProfile();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedVersionText));
                }
            }
        }

        public string SelectedVersionText => SelectedVersion.MinecraftVersion;

        public JavaInfo CurrentJava
        {
            get => _currentJava;
            private set
            {
                _currentJava = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(JavaStatusText));
                OnPropertyChanged(nameof(JavaVersionText));
                OnPropertyChanged(nameof(JavaSourceText));
                OnPropertyChanged(nameof(LaunchJavaReadinessText));
            }
        }

        public string JavaStatusText
        {
            get
            {
                if (CurrentJava.IsDetected)
                {
                    return "Detected";
                }

                return CurrentJava.ErrorMessage == "Invalid Java path." ||
                       CurrentJava.ErrorMessage == "Java path must point to java.exe."
                    ? "Invalid"
                    : "Not Found";
            }
        }

        public string JavaVersionText => CurrentJava.IsDetected ? CurrentJava.VersionText : "-";

        public string JavaSourceText => string.IsNullOrWhiteSpace(CurrentJava.Source) ? "-" : CurrentJava.Source;

        public MinecraftDirectoryInfo CurrentMinecraftDirectory
        {
            get => _currentMinecraftDirectory;
            private set
            {
                _currentMinecraftDirectory = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinecraftDirectoryStatusText));
                OnPropertyChanged(nameof(MinecraftDirectorySourceText));
                OnPropertyChanged(nameof(MinecraftDirectoryDetailsText));
                OnPropertyChanged(nameof(LaunchMinecraftDirectoryReadinessText));
            }
        }

        public string MinecraftDirectoryStatusText
        {
            get
            {
                if (CurrentMinecraftDirectory.IsValid)
                {
                    return "Detected";
                }

                return CurrentMinecraftDirectory.ErrorMessage == "Minecraft directory does not exist." ||
                       CurrentMinecraftDirectory.ErrorMessage == "Directory must contain versions, assets, or libraries."
                    ? "Invalid"
                    : "Not Found";
            }
        }

        public string MinecraftDirectorySourceText => string.IsNullOrWhiteSpace(CurrentMinecraftDirectory.Source)
            ? "-"
            : CurrentMinecraftDirectory.Source;

        public string MinecraftDirectoryDetailsText
        {
            get
            {
                if (!CurrentMinecraftDirectory.IsValid)
                {
                    return string.IsNullOrWhiteSpace(CurrentMinecraftDirectory.ErrorMessage)
                        ? "-"
                        : CurrentMinecraftDirectory.ErrorMessage;
                }

                string versions = CurrentMinecraftDirectory.HasVersionsFolder ? "versions: yes" : "versions: no";
                string assets = CurrentMinecraftDirectory.HasAssetsFolder ? "assets: yes" : "assets: no";
                string libraries = CurrentMinecraftDirectory.HasLibrariesFolder ? "libraries: yes" : "libraries: no";
                return $"{versions}, {assets}, {libraries}";
            }
        }

        public LaunchProfile CurrentLaunchProfile
        {
            get => _currentLaunchProfile;
            private set
            {
                _currentLaunchProfile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchProfileNameText));
                OnPropertyChanged(nameof(PlayerIgnReadinessText));
            }
        }

        public LaunchValidationResult CurrentLaunchValidation
        {
            get => _currentLaunchValidation;
            private set
            {
                _currentLaunchValidation = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchStatusText));
                OnPropertyChanged(nameof(LaunchValidationText));
            }
        }

        public string LaunchProfileNameText => CurrentLaunchProfile.ProfileName;

        public string LaunchJavaReadinessText => CurrentJava.IsDetected ? "Ready" : "Missing";

        public string LaunchMinecraftDirectoryReadinessText => CurrentMinecraftDirectory.IsValid ? "Ready" : "Missing";

        public string LaunchStatusText => CurrentLaunchValidation.IsValid ? "Ready" : "Not Ready";

        public string LaunchValidationText => CurrentLaunchValidation.Summary;

        public string PlayerIgnReadinessText => HasPlayerIgnError ? "Needs attention" : CurrentLaunchProfile.Username;

        public string PlayerIgnInput
        {
            get => _playerIgnInput;
            set
            {
                string normalizedValue = value ?? string.Empty;
                if (_playerIgnInput != normalizedValue)
                {
                    _playerIgnInput = normalizedValue;
                    PlayerIgnErrorText = ValidateOfflineIgn(normalizedValue);
                    if (IsPlayerIgnValid)
                    {
                        GuestUsername = normalizedValue;
                    }

                    RefreshLaunchProfile();
                    OnPropertyChanged();
                }
            }
        }

        public bool IsPlayerIgnValid => string.IsNullOrWhiteSpace(PlayerIgnErrorText);

        public bool HasPlayerIgnError => !IsPlayerIgnValid;

        public string PlayerIgnErrorText
        {
            get => _playerIgnErrorText;
            private set
            {
                if (_playerIgnErrorText != value)
                {
                    _playerIgnErrorText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsPlayerIgnValid));
                    OnPropertyChanged(nameof(HasPlayerIgnError));
                    OnPropertyChanged(nameof(PlayerIgnReadinessText));
                }
            }
        }

        public string GuestUsername
        {
            get => _guestUsername;
            set
            {
                string normalizedValue = string.IsNullOrWhiteSpace(value) ? "CarbonPlayer" : value;
                if (_guestUsername != normalizedValue)
                {
                    _guestUsername = normalizedValue;
                    _config.GuestUsername = normalizedValue;
                    SaveConfig();
                    RefreshLaunchProfile();
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
                    ValidateManualJavaPath(normalizedValue);
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
                    ValidateManualMinecraftDirectory(normalizedValue);
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
                    RefreshLaunchProfile();
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
            RefreshLaunchProfile();
            if (HasPlayerIgnError)
            {
                ShowModal("Account", "Player IGN is invalid.");
                return;
            }

            GuestUsername = PlayerIgnInput;
            _config.GuestUsername = GuestUsername;
            SaveConfig();
            ShowModal("Account", "Player IGN saved.");
        }

        private void DetectJava()
        {
            JavaInfo javaInfo = _javaDetectionService.Detect(JavaPath);
            ApplyJavaInfo(javaInfo, saveDetectedPath: javaInfo.IsDetected);
        }

        private void ValidateManualJavaPath(string javaPath)
        {
            if (string.IsNullOrWhiteSpace(javaPath))
            {
                _config.JavaPath = string.Empty;
                CurrentJava = new JavaInfo
                {
                    IsDetected = false,
                    Source = "Manual",
                    ErrorMessage = "Java path is empty."
                };
                SaveConfig();
                RefreshLaunchProfile();
                return;
            }

            JavaInfo javaInfo = _javaDetectionService.ValidateJavaPath(javaPath, "Manual");
            ApplyJavaInfo(javaInfo, saveDetectedPath: javaInfo.IsDetected);
        }

        private void ApplyJavaInfo(JavaInfo javaInfo, bool saveDetectedPath)
        {
            CurrentJava = javaInfo;

            if (javaInfo.IsDetected && saveDetectedPath)
            {
                _javaPath = javaInfo.JavaPath;
                _config.JavaPath = javaInfo.JavaPath;
                SaveConfig();
                OnPropertyChanged(nameof(JavaPath));
            }

            RefreshLaunchProfile();
        }

        private void DetectMinecraftDirectory()
        {
            MinecraftDirectoryInfo directoryInfo = _minecraftDirectoryService.Detect(MinecraftDirectory);
            ApplyMinecraftDirectoryInfo(directoryInfo, saveDetectedPath: directoryInfo.IsValid);
        }

        private void ValidateManualMinecraftDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                _config.MinecraftDirectory = string.Empty;
                CurrentMinecraftDirectory = new MinecraftDirectoryInfo
                {
                    IsDetected = false,
                    IsValid = false,
                    Source = "Manual",
                    ErrorMessage = "Minecraft directory is empty."
                };
                SaveConfig();
                RefreshLaunchProfile();
                return;
            }

            MinecraftDirectoryInfo directoryInfo = _minecraftDirectoryService.ValidateDirectory(directoryPath, "Manual");
            ApplyMinecraftDirectoryInfo(directoryInfo, saveDetectedPath: directoryInfo.IsValid);
        }

        private void ApplyMinecraftDirectoryInfo(MinecraftDirectoryInfo directoryInfo, bool saveDetectedPath)
        {
            CurrentMinecraftDirectory = directoryInfo;

            if (directoryInfo.IsValid && saveDetectedPath)
            {
                _minecraftDirectory = directoryInfo.DirectoryPath;
                _config.MinecraftDirectory = directoryInfo.DirectoryPath;
                SaveConfig();
                OnPropertyChanged(nameof(MinecraftDirectory));
            }

            RefreshLaunchProfile();
        }

        private void RefreshLaunchProfile()
        {
            LauncherConfig profileConfig = new LauncherConfig
            {
                SelectedVersion = SelectedVersion.MinecraftVersion,
                GuestUsername = _config.GuestUsername,
                JavaPath = JavaPath,
                MinecraftDirectory = MinecraftDirectory,
                AllocatedMemoryMb = AllocatedMemoryMb,
                LastSelectedPage = CurrentPage,
                WindowWidth = _config.WindowWidth,
                WindowHeight = _config.WindowHeight
            };
            LaunchProfile profile = _launchProfileService.CreateDefaultProfile(
                profileConfig,
                CurrentJava,
                CurrentMinecraftDirectory);
            CurrentLaunchProfile = profile;
            LaunchValidationResult validation = _launchProfileService.Validate(
                profile,
                CurrentJava,
                CurrentMinecraftDirectory);

            if (HasPlayerIgnError)
            {
                validation.Errors.Add(PlayerIgnErrorText);
                validation.IsValid = false;
                validation.Summary = $"{validation.Errors.Count} issue(s) need attention.";
            }

            CurrentLaunchValidation = validation;
            OnPropertyChanged(nameof(PlayerIgnReadinessText));
        }

        private void ShowLaunchValidationModal()
        {
            if (CurrentLaunchValidation.IsValid)
            {
                ShowModal("Launch Profile", "Launch profile is ready. Real launch pipeline is coming next.");
                return;
            }

            string message = CurrentLaunchValidation.Errors.Count == 0
                ? CurrentLaunchValidation.Summary
                : string.Join("\n", CurrentLaunchValidation.Errors);
            ShowModal("Launch Profile Not Ready", message);
        }

        private static string ValidateOfflineIgn(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return "Player IGN is required.";
            }

            if (username.Length < 3 || username.Length > 16)
            {
                return "Player IGN must be 3-16 characters.";
            }

            return Regex.IsMatch(username, "^[A-Za-z0-9_]+$")
                ? string.Empty
                : "Only A-Z, a-z, 0-9 and underscore are allowed. Turkish characters, spaces and special characters are not allowed.";
        }

        private static bool IsValidOfflineIgn(string username)
        {
            return string.IsNullOrWhiteSpace(ValidateOfflineIgn(username));
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
