using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using CarbonLauncher.ViewModels;

namespace CarbonLauncher.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel viewModel = new MainViewModel();
            DataContext = viewModel;
            Width = viewModel.InitialWindowWidth;
            Height = viewModel.InitialWindowHeight;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel viewModel && WindowState == WindowState.Normal)
            {
                viewModel.SaveWindowState(Width, Height);
            }

            base.OnClosing(e);
        }
    }
}
