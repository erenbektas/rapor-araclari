using System.Windows;
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
            TitleText = candidate.Definition.DisplayName;
            VersionText = $"Yeni sürüm: {candidate.Release.TagName}";
            SummaryText = $"{candidate.CurrentVersion} sürümünden {candidate.Release.TagName} sürümüne geçebilirsiniz. Güncelleme launcher içinden kurulacak ve tarayıcı açılmayacak.";
            AssetText = candidate.AssetKind == ReleaseAssetKind.Setup
                ? "Setup EXE ile sessiz kurulum yapılacak."
                : "Portable paket launcher klasörüne yerleştirilecek.";
        }

        public string TitleText { get; }
        public string VersionText { get; }
        public string SummaryText { get; }
        public string AssetText { get; }
    }
}
