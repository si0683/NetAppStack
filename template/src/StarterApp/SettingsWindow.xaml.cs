using System.Windows;
using StarterApp.ViewModels;

namespace StarterApp;

public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        _vm.Save();
        DialogResult = true;
    }
}
