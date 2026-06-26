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
        private string _currentPage;
        private string _statusText;

        public MainViewModel()
        {
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
            _selectedVersion = Versions[0];
            _currentPage = "Home";
            _statusText = "UI Skeleton Ready";
            AccountName = "Guest Mode";
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

        public string AccountName { get; }

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

        private void Navigate(string? page)
        {
            if (string.IsNullOrWhiteSpace(page))
            {
                return;
            }

            CurrentPage = page;
            StatusText = $"{CurrentPageTitle} selected";
        }

        private void UpdateActiveNavigation()
        {
            foreach (NavigationItemViewModel item in NavigationItems)
            {
                item.IsActive = item.Key == CurrentPage;
            }
        }
    }
}
