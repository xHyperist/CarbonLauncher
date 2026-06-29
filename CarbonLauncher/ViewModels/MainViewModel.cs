using System;
using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CarbonLauncher.Models;
using CarbonLauncher.Services;

namespace CarbonLauncher.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly LauncherConfigService _configService;
        private readonly LauncherStorageService _storageService;
        private readonly StartupLogService _startupLogService;
        private readonly JavaDetectionService _javaDetectionService;
        private readonly MinecraftDirectoryService _minecraftDirectoryService;
        private readonly MinecraftRuntimeResolverService _minecraftRuntimeResolverService;
        private readonly NativeLibraryExtractorService _nativeLibraryExtractorService;
        private readonly LaunchProfileService _launchProfileService;
        private readonly LaunchSessionService _launchSessionService;
        private readonly LaunchCommandBuilderService _launchCommandBuilderService;
        private readonly LaunchProcessService _launchProcessService;
        private readonly VersionManifestService _versionManifestService;
        private readonly ClientJarResolverService _clientJarResolverService;
        private readonly CarbonTweakerManifestService _carbonTweakerManifestService;
        private readonly LaunchWrapperResolverService _launchWrapperResolverService;
        private readonly LauncherConfig _config;
        private readonly VersionManifest _versionManifest;
        private LauncherStorageInfo _currentStorage;
        private LauncherVersion _selectedVersion;
        private JavaInfo _currentJava;
        private MinecraftDirectoryInfo _currentMinecraftDirectory;
        private MinecraftRuntimeInfo _currentMinecraftRuntime;
        private ClientJarInfo _currentClientJar;
        private CarbonTweakerInfo _currentCarbonTweaker;
        private LaunchWrapperInfo _currentLaunchWrapper;
        private LaunchProfile _currentLaunchProfile;
        private LaunchValidationResult _currentLaunchValidation;
        private LaunchSession _currentLaunchSession;
        private CarbonLauncher.Models.LaunchCommand _currentLaunchCommand;
        private LaunchProcessInfo _currentLaunchProcess;
        private string _currentPage;
        private string _guestUsername;
        private string _playerIgnInput;
        private string _playerIgnErrorText;
        private string _javaPath;
        private string _minecraftDirectory;
        private int _allocatedMemoryMb;
        private bool _isModalVisible;
        private bool _startupDiagnosticsScheduled;
        private string _modalTitle;
        private string _modalMessage;

        public MainViewModel()
        {
            _storageService = new LauncherStorageService();
            _currentStorage = _storageService.EnsureStorage();
            _startupLogService = new StartupLogService(_storageService);
            _startupLogService.Write("app started");
            _startupLogService.Write("storage ensured");
            _configService = new LauncherConfigService(_storageService);
            _javaDetectionService = new JavaDetectionService();
            _minecraftDirectoryService = new MinecraftDirectoryService();
            _minecraftRuntimeResolverService = new MinecraftRuntimeResolverService(_storageService);
            _nativeLibraryExtractorService = new NativeLibraryExtractorService();
            _launchProfileService = new LaunchProfileService();
            _launchSessionService = new LaunchSessionService();
            _launchCommandBuilderService = new LaunchCommandBuilderService();
            _launchProcessService = new LaunchProcessService(_storageService);
            _launchProcessService.ProcessExited += OnLaunchProcessExited;
            _versionManifestService = new VersionManifestService(_storageService);
            _clientJarResolverService = new ClientJarResolverService(_storageService);
            _carbonTweakerManifestService = new CarbonTweakerManifestService();
            _launchWrapperResolverService = new LaunchWrapperResolverService();
            _config = _configService.Load();
            _startupLogService.Write("config loaded");
            _versionManifest = _versionManifestService.Load();
            _startupLogService.Write("manifest loaded");

            Versions = new ObservableCollection<LauncherVersion>(_versionManifest.Versions);

            NavigationItems = new ObservableCollection<NavigationItemViewModel>
            {
                new NavigationItemViewModel("Home", "Play"),
                new NavigationItemViewModel("Versions", "Versions"),
                new NavigationItemViewModel("Account", "Account"),
                new NavigationItemViewModel("Settings", "Settings")
            };

            _selectedVersion = FindVersion(_config.SelectedVersion);
            _config.SelectedVersion = _selectedVersion.MinecraftVersion;
            SaveConfig();
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
            _currentMinecraftRuntime = new MinecraftRuntimeInfo
            {
                IsResolved = false,
                MinecraftVersion = _selectedVersion.MinecraftVersion
            };
            _currentClientJar = new ClientJarInfo
            {
                Status = "Missing",
                ErrorMessage = "Client jar has not been checked yet."
            };
            _currentCarbonTweaker = new CarbonTweakerInfo
            {
                Status = "MissingJar",
                ErrorMessage = "Carbon tweaker manifest has not been checked yet."
            };
            _currentLaunchWrapper = new LaunchWrapperInfo();
            _currentLaunchProfile = new LaunchProfile();
            _currentLaunchValidation = new LaunchValidationResult
            {
                IsValid = false,
                Summary = "Launch profile has not been checked yet."
            };
            _currentLaunchSession = new LaunchSession
            {
                IsGuestMode = true,
                SessionType = "offline",
                AccessToken = "0",
                UserType = "legacy",
                ErrorMessage = "Launch session has not been created yet."
            };
            _currentLaunchCommand = new CarbonLauncher.Models.LaunchCommand
            {
                IsBuildable = false
            };
            _currentLaunchProcess = new LaunchProcessInfo();
            _modalTitle = "Carbon Launcher";
            _modalMessage = string.Empty;

            StartLaunchCommand = new RelayCommand(_ => _ = StartLaunchAsync());
            LaunchCommand = StartLaunchCommand;
            NavigateCommand = new RelayCommand(page => Navigate(page as string));
            SelectVersionCommand = new RelayCommand(version => SelectVersion(version as LauncherVersion));
            SaveAccountCommand = new RelayCommand(_ => SaveAccount());
            DetectJavaCommand = new RelayCommand(_ => DetectJava());
            DetectMinecraftDirectoryCommand = new RelayCommand(_ => DetectMinecraftDirectory());
            CheckClientJarCommand = new RelayCommand(_ =>
            {
                RefreshClientJar();
                RefreshCarbonTweaker();
                RefreshLaunchWrapper();
                RefreshLaunchProfile();
            });
            RefreshLaunchProfileCommand = new RelayCommand(_ => RefreshLaunchProfile());
            RefreshMinecraftRuntimeCommand = new RelayCommand(_ =>
            {
                RefreshMinecraftRuntime();
                RefreshLaunchCommand();
            });
            RefreshLaunchSessionCommand = new RelayCommand(_ =>
            {
                RefreshLaunchSession();
                RefreshLaunchCommand();
            });
            RefreshLaunchCommandCommand = new RelayCommand(_ =>
            {
                RefreshClientJar();
                RefreshCarbonTweaker();
                RefreshLaunchWrapper();
                RefreshLaunchProfile();
            });
            RefreshCarbonTweakerCommand = new RelayCommand(_ =>
            {
                RefreshCarbonTweaker();
                RefreshLaunchWrapper();
                RefreshLaunchCommand();
            });
            RefreshLaunchWrapperCommand = new RelayCommand(_ =>
            {
                RefreshLaunchWrapper();
                RefreshLaunchCommand();
            });
            OpenStorageFolderCommand = new RelayCommand(_ => OpenStorageFolder());
            ShowComingSoonCommand = new RelayCommand(message => ShowModal("Coming Soon", message as string ?? "This feature is coming soon."));
            CloseModalCommand = new RelayCommand(_ => IsModalVisible = false);

            UpdateActiveNavigation();
            UpdateSelectedVersionState();
        }

        public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

        public ObservableCollection<LauncherVersion> Versions { get; }

        public ICommand LaunchCommand { get; }

        public ICommand StartLaunchCommand { get; }

        public ICommand NavigateCommand { get; }

        public ICommand SelectVersionCommand { get; }

        public ICommand SaveAccountCommand { get; }

        public ICommand DetectJavaCommand { get; }

        public ICommand DetectMinecraftDirectoryCommand { get; }

        public ICommand CheckClientJarCommand { get; }

        public ICommand RefreshLaunchProfileCommand { get; }

        public ICommand RefreshMinecraftRuntimeCommand { get; }

        public ICommand RefreshLaunchSessionCommand { get; }

        public ICommand RefreshLaunchCommandCommand { get; }

        public ICommand RefreshCarbonTweakerCommand { get; }

        public ICommand RefreshLaunchWrapperCommand { get; }

        public ICommand OpenStorageFolderCommand { get; }

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
                    RefreshClientJar();
                    RefreshCarbonTweaker();
                    RefreshLaunchWrapper();
                    RefreshLaunchProfile();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedVersionText));
                    OnPropertyChanged(nameof(SelectedVersionDisplayText));
                }
            }
        }

        public string SelectedVersionText => SelectedVersion.MinecraftVersion;

        public string SelectedVersionDisplayText => string.IsNullOrWhiteSpace(SelectedVersion.DisplayName)
            ? SelectedVersion.MinecraftVersion
            : SelectedVersion.DisplayName;

        public LauncherStorageInfo CurrentStorage
        {
            get => _currentStorage;
            private set
            {
                _currentStorage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StorageStatusText));
                OnPropertyChanged(nameof(StorageRootText));
            }
        }

        public string StorageStatusText => CurrentStorage.IsReady ? "Ready" : "Error";

        public string StorageRootText => string.IsNullOrWhiteSpace(CurrentStorage.RootDirectory)
            ? "-"
            : CurrentStorage.RootDirectory;

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

        public MinecraftRuntimeInfo CurrentMinecraftRuntime
        {
            get => _currentMinecraftRuntime;
            private set
            {
                _currentMinecraftRuntime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinecraftRuntimeStatusText));
                OnPropertyChanged(nameof(MinecraftRuntimeSummaryText));
            }
        }

        public string MinecraftRuntimeStatusText => CurrentMinecraftRuntime.IsResolved ? "Resolved" : "Not Resolved";

        public string MinecraftRuntimeSummaryText
        {
            get
            {
                string versionJson = File.Exists(CurrentMinecraftRuntime.VersionJsonPath) ? "Found" : "Missing";
                string assets = File.Exists(CurrentMinecraftRuntime.AssetIndexPath) ? "Found" : "Missing";
                string natives = CurrentMinecraftRuntime.AreNativesPrepared
                    ? $"Prepared ({CurrentMinecraftRuntime.ExtractedNativeFiles.Count})"
                    : "Missing";
                return $"Version JSON: {versionJson}, Libraries: {CurrentMinecraftRuntime.LibraryPaths.Count} found / {CurrentMinecraftRuntime.MissingLibraries.Count} missing, Assets: {assets}, Natives: {natives}";
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

        public ClientJarInfo CurrentClientJar
        {
            get => _currentClientJar;
            private set
            {
                _currentClientJar = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClientJarStatusText));
                OnPropertyChanged(nameof(ClientJarPathText));
                OnPropertyChanged(nameof(LaunchClientJarReadinessText));
            }
        }

        public string ClientJarStatusText
        {
            get
            {
                return CurrentClientJar.Status == "NotAvailable"
                    ? "Not Available"
                    : CurrentClientJar.Status;
            }
        }

        public string ClientJarPathText => string.IsNullOrWhiteSpace(CurrentClientJar.JarPath)
            ? "-"
            : CurrentClientJar.JarPath;

        public CarbonTweakerInfo CurrentCarbonTweaker
        {
            get => _currentCarbonTweaker;
            private set
            {
                _currentCarbonTweaker = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CarbonTweakerStatusText));
                OnPropertyChanged(nameof(CarbonTweakerClassText));
            }
        }

        public string CarbonTweakerStatusText
        {
            get
            {
                switch (CurrentCarbonTweaker.Status)
                {
                    case "Ready":
                        return "Ready";
                    case "Error":
                        return "Error";
                    case "MissingTweakClass":
                        return "Missing";
                    default:
                        return "Missing";
                }
            }
        }

        public string CarbonTweakerClassText => string.IsNullOrWhiteSpace(CurrentCarbonTweaker.TweakClass)
            ? "-"
            : CurrentCarbonTweaker.TweakClass;

        public LaunchWrapperInfo CurrentLaunchWrapper
        {
            get => _currentLaunchWrapper;
            private set
            {
                _currentLaunchWrapper = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchWrapperStatusText));
                OnPropertyChanged(nameof(LaunchWrapperPathText));
            }
        }

        public string LaunchWrapperStatusText
        {
            get
            {
                switch (CurrentLaunchWrapper.Status)
                {
                    case "Ready":
                        return "Ready";
                    case "Missing":
                        return "Missing";
                    case "Error":
                        return "Error";
                    default:
                        return "Not Required";
                }
            }
        }

        public string LaunchWrapperPathText => string.IsNullOrWhiteSpace(CurrentLaunchWrapper.ExpectedPath)
            ? "-"
            : CurrentLaunchWrapper.ExpectedPath;

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

        public LaunchSession CurrentLaunchSession
        {
            get => _currentLaunchSession;
            private set
            {
                _currentLaunchSession = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchSessionStatusText));
                OnPropertyChanged(nameof(LaunchSessionUsernameText));
                OnPropertyChanged(nameof(LaunchSessionUuidText));
                OnPropertyChanged(nameof(LaunchSessionAuthText));
            }
        }

        public CarbonLauncher.Models.LaunchCommand CurrentLaunchCommand
        {
            get => _currentLaunchCommand;
            private set
            {
                _currentLaunchCommand = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchCommandStatusText));
                OnPropertyChanged(nameof(LaunchCommandPreviewText));
            }
        }

        public LaunchProcessInfo CurrentLaunchProcess
        {
            get => _currentLaunchProcess;
            private set
            {
                _currentLaunchProcess = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LaunchProcessStatusText));
                OnPropertyChanged(nameof(LaunchLogPathText));
            }
        }

        public string LaunchProfileNameText => CurrentLaunchProfile.ProfileName;

        public string LaunchJavaReadinessText => CurrentJava.IsDetected ? "Ready" : "Missing";

        public string LaunchMinecraftDirectoryReadinessText => CurrentMinecraftDirectory.IsValid ? "Ready" : "Missing";

        public string LaunchClientJarReadinessText => ClientJarStatusText;

        public string LaunchStatusText => CurrentLaunchValidation.IsValid ? "Ready" : "Not Ready";

        public string LaunchValidationText => CurrentLaunchValidation.Summary;

        public string LaunchSessionStatusText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(CurrentLaunchSession.ErrorMessage))
                {
                    return "Invalid";
                }

                return CurrentLaunchSession.IsGuestMode ? "Guest / Offline" : "Authenticated";
            }
        }

        public string LaunchSessionUsernameText => string.IsNullOrWhiteSpace(CurrentLaunchSession.ErrorMessage)
            ? CurrentLaunchSession.Username
            : "Needs attention";

        public string LaunchSessionUuidText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentLaunchSession.Uuid))
                {
                    return "-";
                }

                return CurrentLaunchSession.Uuid.Length <= 13
                    ? CurrentLaunchSession.Uuid
                    : CurrentLaunchSession.Uuid.Substring(0, 13) + "...";
            }
        }

        public string LaunchSessionAuthText => CurrentLaunchSession.IsAuthenticated ? "Authenticated" : "Offline Mode";

        public string LaunchCommandStatusText => CurrentLaunchCommand.IsBuildable ? "Buildable" : "Not Buildable";

        public string LaunchProcessStatusText
        {
            get
            {
                if (CurrentLaunchProcess.ProcessId.HasValue &&
                    (CurrentLaunchProcess.IsRunning || CurrentLaunchProcess.HasStarted))
                {
                    return $"{CurrentLaunchProcess.Status} (PID {CurrentLaunchProcess.ProcessId.Value})";
                }

                return string.IsNullOrWhiteSpace(CurrentLaunchProcess.Status)
                    ? "Not Running"
                    : CurrentLaunchProcess.Status;
            }
        }

        public string LaunchLogPathText => string.IsNullOrWhiteSpace(CurrentLaunchProcess.LogFilePath)
            ? "Log: -"
            : $"Log: {CurrentLaunchProcess.LogFilePath}";

        public string LaunchCommandPreviewText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CurrentLaunchCommand.FullCommandPreview))
                {
                    return "-";
                }

                string preview = CurrentLaunchCommand.FullCommandPreview.Length <= 260
                    ? CurrentLaunchCommand.FullCommandPreview
                    : CurrentLaunchCommand.FullCommandPreview.Substring(0, 260) + "...";

                if (!string.IsNullOrWhiteSpace(CurrentLaunchCommand.LaunchWrapperPath) &&
                    preview.IndexOf("launchwrapper-1.12.jar", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    preview += $" LaunchWrapper: {CurrentLaunchCommand.LaunchWrapperPath}";
                }

                return preview;
            }
        }

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

        public void StartStartupDiagnostics()
        {
            if (_startupDiagnosticsScheduled)
            {
                return;
            }

            _startupDiagnosticsScheduled = true;

            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.BeginInvoke(
                    new Action(() => _ = RunStartupDiagnosticsAsync()),
                    System.Windows.Threading.DispatcherPriority.Background);
                return;
            }

            _ = RunStartupDiagnosticsAsync();
        }

        private async Task RunStartupDiagnosticsAsync()
        {
            StartupDiagnosticsResult result = await Task.Run(() => BuildStartupDiagnostics());

            if (Application.Current?.Dispatcher != null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ApplyStartupDiagnostics(result));
                return;
            }

            ApplyStartupDiagnostics(result);
        }

        private StartupDiagnosticsResult BuildStartupDiagnostics()
        {
            StartupDiagnosticsResult result = new StartupDiagnosticsResult();

            try
            {
                _startupLogService.Write("startup diagnostics started");
                _storageService.CleanTempDirectory();

                result.JavaInfo = _javaDetectionService.Detect(JavaPath);
                _startupLogService.Write("java detection completed");

                result.MinecraftDirectoryInfo = _minecraftDirectoryService.Detect(MinecraftDirectory);
                _startupLogService.Write("minecraft directory detection completed");

                result.ClientJarInfo = _clientJarResolverService.Resolve(SelectedVersion);
                _startupLogService.Write("client jar resolved");

                result.CarbonTweakerInfo = _carbonTweakerManifestService.Read(result.ClientJarInfo);
                _startupLogService.Write("carbon tweaker manifest checked");

                result.LaunchWrapperInfo = _launchWrapperResolverService.Resolve(
                    result.CarbonTweakerInfo,
                    result.MinecraftDirectoryInfo);
                _startupLogService.Write("launchwrapper checked");

                MinecraftRuntimeInfo runtimeInfo = _minecraftRuntimeResolverService.Resolve(
                    result.MinecraftDirectoryInfo,
                    SelectedVersion);
                result.MinecraftRuntimeInfo = _nativeLibraryExtractorService.PrepareNatives(runtimeInfo);
                _startupLogService.Write("minecraft runtime resolved");

                LauncherConfig profileConfig = new LauncherConfig
                {
                    SelectedVersion = SelectedVersion.MinecraftVersion,
                    GuestUsername = _config.GuestUsername,
                    JavaPath = result.JavaInfo.IsDetected ? result.JavaInfo.JavaPath : JavaPath,
                    MinecraftDirectory = result.MinecraftDirectoryInfo.IsValid
                        ? result.MinecraftDirectoryInfo.DirectoryPath
                        : MinecraftDirectory,
                    AllocatedMemoryMb = AllocatedMemoryMb,
                    LastSelectedPage = CurrentPage,
                    WindowWidth = _config.WindowWidth,
                    WindowHeight = _config.WindowHeight
                };

                result.LaunchProfile = _launchProfileService.CreateDefaultProfile(
                    profileConfig,
                    result.JavaInfo,
                    result.MinecraftDirectoryInfo,
                    SelectedVersion,
                    result.ClientJarInfo);

                result.LaunchValidation = _launchProfileService.Validate(
                    result.LaunchProfile,
                    result.JavaInfo,
                    result.MinecraftDirectoryInfo,
                    SelectedVersion,
                    result.ClientJarInfo);

                if (HasPlayerIgnError)
                {
                    result.LaunchValidation.Errors.Add(PlayerIgnErrorText);
                    result.LaunchValidation.IsValid = false;
                    result.LaunchValidation.Summary = $"{result.LaunchValidation.Errors.Count} issue(s) need attention.";
                }

                result.LaunchSession = HasPlayerIgnError
                    ? _launchSessionService.CreateInvalidGuestSession(PlayerIgnInput, PlayerIgnErrorText)
                    : _launchSessionService.CreateGuestSession(result.LaunchProfile.Username);

                result.LaunchCommand = _launchCommandBuilderService.Build(
                    result.LaunchProfile,
                    result.JavaInfo,
                    result.MinecraftDirectoryInfo,
                    result.ClientJarInfo,
                    SelectedVersion,
                    result.LaunchSession,
                    result.MinecraftRuntimeInfo,
                    result.CarbonTweakerInfo,
                    result.LaunchWrapperInfo);

                _startupLogService.Write("startup diagnostics completed");
            }
            catch (Exception ex)
            {
                _startupLogService.Write($"startup diagnostics error: {ex}");
            }

            return result;
        }

        private void ApplyStartupDiagnostics(StartupDiagnosticsResult result)
        {
            CurrentJava = result.JavaInfo;
            CurrentMinecraftDirectory = result.MinecraftDirectoryInfo;
            CurrentClientJar = result.ClientJarInfo;
            CurrentCarbonTweaker = result.CarbonTweakerInfo;
            CurrentLaunchWrapper = result.LaunchWrapperInfo;
            CurrentMinecraftRuntime = result.MinecraftRuntimeInfo;
            CurrentLaunchProfile = result.LaunchProfile;
            CurrentLaunchValidation = result.LaunchValidation;
            CurrentLaunchSession = result.LaunchSession;
            CurrentLaunchCommand = result.LaunchCommand;

            if (result.JavaInfo.IsDetected)
            {
                _javaPath = result.JavaInfo.JavaPath;
                _config.JavaPath = result.JavaInfo.JavaPath;
                OnPropertyChanged(nameof(JavaPath));
            }

            if (result.MinecraftDirectoryInfo.IsValid)
            {
                _minecraftDirectory = result.MinecraftDirectoryInfo.DirectoryPath;
                _config.MinecraftDirectory = result.MinecraftDirectoryInfo.DirectoryPath;
                OnPropertyChanged(nameof(MinecraftDirectory));
            }

            SaveConfig();
            OnPropertyChanged(nameof(PlayerIgnReadinessText));
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

            if (!version.IsAvailable || version.IsComingSoon)
            {
                ShowModal("Coming Soon", $"{version.DisplayName} is coming soon.");
                return;
            }

            SelectedVersion = version;
            ShowModal("Version Selected", $"{version.DisplayName} is selected.");
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

            RefreshLaunchWrapper();
            RefreshLaunchProfile();
        }

        private void RefreshClientJar()
        {
            CurrentClientJar = _clientJarResolverService.Resolve(SelectedVersion);
        }

        private void RefreshCarbonTweaker()
        {
            CurrentCarbonTweaker = _carbonTweakerManifestService.Read(CurrentClientJar);
        }

        private void RefreshLaunchWrapper()
        {
            CurrentLaunchWrapper = _launchWrapperResolverService.Resolve(CurrentCarbonTweaker, CurrentMinecraftDirectory);
        }

        private void RefreshMinecraftRuntime()
        {
            MinecraftRuntimeInfo runtimeInfo = _minecraftRuntimeResolverService.Resolve(CurrentMinecraftDirectory, SelectedVersion);
            CurrentMinecraftRuntime = _nativeLibraryExtractorService.PrepareNatives(runtimeInfo);
        }

        private void OpenStorageFolder()
        {
            CurrentStorage = _storageService.EnsureStorage();

            if (!CurrentStorage.IsReady)
            {
                ShowModal("Storage Error", CurrentStorage.ErrorMessage);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = CurrentStorage.RootDirectory,
                    UseShellExecute = true
                });
            }
            catch
            {
                ShowModal("Storage Error", "Storage folder could not be opened.");
            }
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
                CurrentMinecraftDirectory,
                SelectedVersion,
                CurrentClientJar);
            CurrentLaunchProfile = profile;
            LaunchValidationResult validation = _launchProfileService.Validate(
                profile,
                CurrentJava,
                CurrentMinecraftDirectory,
                SelectedVersion,
                CurrentClientJar);

            if (HasPlayerIgnError)
            {
                validation.Errors.Add(PlayerIgnErrorText);
                validation.IsValid = false;
                validation.Summary = $"{validation.Errors.Count} issue(s) need attention.";
            }

            CurrentLaunchValidation = validation;
            RefreshMinecraftRuntime();
            RefreshLaunchSession();
            RefreshLaunchCommand();
            OnPropertyChanged(nameof(PlayerIgnReadinessText));
        }

        private void RefreshLaunchSession()
        {
            CurrentLaunchSession = HasPlayerIgnError
                ? _launchSessionService.CreateInvalidGuestSession(PlayerIgnInput, PlayerIgnErrorText)
                : _launchSessionService.CreateGuestSession(CurrentLaunchProfile.Username);
        }

        private void RefreshLaunchCommand()
        {
            CarbonLauncher.Models.LaunchCommand command = _launchCommandBuilderService.Build(
                CurrentLaunchProfile,
                CurrentJava,
                CurrentMinecraftDirectory,
                CurrentClientJar,
                SelectedVersion,
                CurrentLaunchSession,
                CurrentMinecraftRuntime,
                CurrentCarbonTweaker,
                CurrentLaunchWrapper);

            CurrentLaunchCommand = command;
        }

        private async Task StartLaunchAsync()
        {
            try
            {
                if (CurrentLaunchProcess.IsRunning)
                {
                    ShowModal("Launch", "Minecraft is already running.");
                    return;
                }

                RefreshLaunchProfile();

                if (!CurrentLaunchCommand.IsBuildable)
                {
                    string errors = CurrentLaunchCommand.Errors.Count == 0
                        ? "Launch command is not buildable."
                        : string.Join("\n", CurrentLaunchCommand.Errors);
                    ShowModal("Launch Command Not Ready", errors);
                    return;
                }

                CurrentLaunchProcess = new LaunchProcessInfo
                {
                    Status = "Starting",
                    StartedAt = DateTime.Now
                };

                LaunchProcessInfo processInfo = await _launchProcessService.StartAsync(CurrentLaunchCommand, CurrentLaunchProfile);
                CurrentLaunchProcess = processInfo;

                if (processInfo.HasStarted)
                {
                    ShowModal("Launch", "Minecraft process started.");
                    return;
                }

                ShowModal("Launch Error", string.IsNullOrWhiteSpace(processInfo.ErrorMessage)
                    ? "Minecraft process could not be started."
                    : processInfo.ErrorMessage);
            }
            catch (Exception ex)
            {
                CurrentLaunchProcess = new LaunchProcessInfo
                {
                    Status = "Error",
                    ErrorMessage = ex.Message
                };
                ShowModal("Launch Error", ex.Message);
            }
        }

        private void OnLaunchProcessExited(LaunchProcessInfo processInfo)
        {
            if (Application.Current?.Dispatcher != null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => CurrentLaunchProcess = processInfo);
                return;
            }

            CurrentLaunchProcess = processInfo;
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
                version.IsSelected = version.Id == SelectedVersion.Id;
            }
        }

        private LauncherVersion FindVersion(string minecraftVersion)
        {
            foreach (LauncherVersion version in Versions)
            {
                if (version.MinecraftVersion == minecraftVersion && version.IsAvailable)
                {
                    return version;
                }
            }

            foreach (LauncherVersion version in Versions)
            {
                if (version.IsAvailable)
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

        private sealed class StartupDiagnosticsResult
        {
            public JavaInfo JavaInfo { get; set; } = new JavaInfo
            {
                IsDetected = false,
                Source = "Not Found",
                ErrorMessage = "Java has not been checked yet."
            };

            public MinecraftDirectoryInfo MinecraftDirectoryInfo { get; set; } = new MinecraftDirectoryInfo
            {
                IsDetected = false,
                IsValid = false,
                Source = "Not Found",
                ErrorMessage = "Minecraft directory has not been checked yet."
            };

            public ClientJarInfo ClientJarInfo { get; set; } = new ClientJarInfo
            {
                Status = "Missing",
                ErrorMessage = "Client jar has not been checked yet."
            };

            public CarbonTweakerInfo CarbonTweakerInfo { get; set; } = new CarbonTweakerInfo
            {
                Status = "MissingJar",
                ErrorMessage = "Carbon tweaker manifest has not been checked yet."
            };

            public LaunchWrapperInfo LaunchWrapperInfo { get; set; } = new LaunchWrapperInfo();

            public MinecraftRuntimeInfo MinecraftRuntimeInfo { get; set; } = new MinecraftRuntimeInfo();

            public LaunchProfile LaunchProfile { get; set; } = new LaunchProfile();

            public LaunchValidationResult LaunchValidation { get; set; } = new LaunchValidationResult
            {
                IsValid = false,
                Summary = "Launch profile has not been checked yet."
            };

            public LaunchSession LaunchSession { get; set; } = new LaunchSession
            {
                IsGuestMode = true,
                SessionType = "offline",
                AccessToken = "0",
                UserType = "legacy",
                ErrorMessage = "Launch session has not been created yet."
            };

            public CarbonLauncher.Models.LaunchCommand LaunchCommand { get; set; } = new CarbonLauncher.Models.LaunchCommand
            {
                IsBuildable = false
            };
        }
    }
}
