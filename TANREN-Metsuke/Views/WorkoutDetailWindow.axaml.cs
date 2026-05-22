using Avalonia.Controls;
using Avalonia.Interactivity;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.ViewModels;

namespace TANREN_Metsuke.Views;

public partial class WorkoutDetailWindow : Window
{
    public WorkoutDetailWindow() => InitializeComponent(); // unused, needed only to stop compiler warnings

    public WorkoutDetailWindow(WorkoutSession session, bool imperial = false)
    {
        InitializeComponent();
        DataContext = new WorkoutDetailViewModel(session, imperial);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
