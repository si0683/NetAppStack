using System.Windows;
using StarterApp.Services;

namespace StarterApp;

public partial class HelpWindow : Wpf.Ui.Controls.FluentWindow
{
    public HelpWindow()
    {
        InitializeComponent();
        Header.Text = $"{AppInfo.Title} {AppInfo.Version}";
    }

    private void OnRepo(object sender, RoutedEventArgs e) => Shell.Open(AppInfo.RepositoryUrl);
}
