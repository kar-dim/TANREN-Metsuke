using System;
using Avalonia.Controls;

namespace TANREN_Metsuke.Views;

public partial class MuscleDetailWindow : Window
{
    public MuscleDetailWindow() => InitializeComponent();

    // resize the window to 30% (width) and 65% (height) of the working area,
    // must be done here rather than in XAML (else DPI scaling breaks...)
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var screen = Screens.Primary;
        if (screen != null)
        {
            var scaling = screen.Scaling;
            Width = screen.WorkingArea.Width * 0.30 / scaling;
            Height = screen.WorkingArea.Height * 0.65 / scaling;
        }
    }
}
