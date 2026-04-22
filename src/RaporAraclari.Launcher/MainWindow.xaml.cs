using System.Windows;
using RaporAraclari.Launcher.Services;
using RaporAraclari.Launcher.ViewModels;
using RaporAraclari.Launcher.Views;

namespace RaporAraclari.Launcher;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(
            new AppManifestProvider(),
            new LauncherStateStore(),
            new GitHubReleaseService(),
            new AppInstallationService(),
            new DialogService());
        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        await _viewModel.InitializeAsync();
    }

    private void BtnInfo_Click(object sender, RoutedEventArgs e)
    {
        var window = new InfoWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }
}
