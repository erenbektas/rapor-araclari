using System.Windows;
using System.Windows.Media;
using RaporAraclari.Launcher.Models;

namespace RaporAraclari.Launcher.Views;

public partial class UpdatePromptWindow : Window
{
    public UpdatePromptWindow(UpdateCandidate candidate)
    {
        InitializeComponent();
        DataContext = new UpdatePromptViewModel(candidate);
    }

    private void LaterButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private sealed class UpdatePromptViewModel
    {
        public UpdatePromptViewModel(UpdateCandidate candidate)
        {
            TitleText = $"{candidate.Definition.DisplayName} icin yeni surum hazir";
            SummaryText = $"{candidate.CurrentVersion} surumunden {candidate.Release.TagName} surumune gecis yapabilirsiniz.";
            DetailText = "Guncelleme launcher icinden indirilecek ve tarayici acilmayacak.";
            AssetText = candidate.AssetKind == ReleaseAssetKind.Setup
                ? "Kurulum tipi: Setup EXE (sessiz kurulum)"
                : "Kurulum tipi: Portable EXE (launcher klasorune yerlestirilir)";
            AccentBrush = (Brush)new BrushConverter().ConvertFromString(candidate.Definition.AccentColor)!;
            AccentSoftBrush = (Brush)new BrushConverter().ConvertFromString(candidate.Definition.AccentLightColor)!;
        }

        public string TitleText { get; }
        public string SummaryText { get; }
        public string DetailText { get; }
        public string AssetText { get; }
        public Brush AccentBrush { get; }
        public Brush AccentSoftBrush { get; }
    }
}

