using System.Windows;
using RaporAraclari.Launcher.Models;
using RaporAraclari.Launcher.Views;

namespace RaporAraclari.Launcher.Services;

public sealed class DialogService
{
    public bool ShowUpdatePrompt(UpdateCandidate candidate)
    {
        var window = new UpdatePromptWindow(candidate)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true;
    }

    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowInfo(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

