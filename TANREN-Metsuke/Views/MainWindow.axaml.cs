using System;
using Avalonia.Controls;

namespace TANREN_Metsuke.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    // resize the window to 75% of the working area,
    // must be done here rather than in XAML (else DPI scaling breaks...)
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var screen = Screens.Primary;
        if (screen != null)
        {
            var scaling = screen.Scaling;
            Width = screen.WorkingArea.Width * 0.75 / scaling;
            Height = screen.WorkingArea.Height * 0.75 / scaling;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
