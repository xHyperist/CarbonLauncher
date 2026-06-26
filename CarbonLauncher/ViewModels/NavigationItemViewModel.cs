namespace CarbonLauncher.ViewModels
{
    public sealed class NavigationItemViewModel : ViewModelBase
    {
        private bool _isActive;

        public NavigationItemViewModel(string key, string title, string icon)
        {
            Key = key;
            Title = title;
            Icon = icon;
        }

        public string Key { get; }

        public string Title { get; }

        public string Icon { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
