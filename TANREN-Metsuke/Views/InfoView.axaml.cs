using System.Reflection;
using Avalonia.Controls;

namespace TANREN_Metsuke.Views;

public partial class InfoView : UserControl
{
    public InfoView()
    {
        InitializeComponent();
        var version = typeof(InfoView).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?";
        CopyrightText.Text = $"© 2026 Dimitris Karatzas | TANREN Metsuke v{version}";
    }
}
