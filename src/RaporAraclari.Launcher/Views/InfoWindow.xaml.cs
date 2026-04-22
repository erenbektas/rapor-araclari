using System.Reflection;
using System.Windows;

namespace RaporAraclari.Launcher.Views;

public partial class InfoWindow : Window
{
    public InfoWindow()
    {
        InitializeComponent();
        VersionText.Text = $"Launcher v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.1"}";
    }
}
