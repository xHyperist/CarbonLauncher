namespace CarbonLauncher.ViewModels
{
    public sealed class NavigationItemViewModel : ViewModelBase
    {
        private bool _isActive;

        public NavigationItemViewModel(string key, string title)
        {
            Key = key;
            Title = title;
        }

        public string Key { get; }

        public string Title { get; }

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
