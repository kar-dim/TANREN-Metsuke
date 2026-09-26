using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TANREN_Metsuke.Views;

public partial class NoWorkoutsDialog : Window
{
    public NoWorkoutsDialog() => InitializeComponent();

    private void SyncButton_Click(object? sender, RoutedEventArgs e) => Close(true);
    private void LaterButton_Click(object? sender, RoutedEventArgs e) => Close(false);
}
