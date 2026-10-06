using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using TANREN_Metsuke.Services;
using TANREN_Metsuke.ViewModels;
using TANREN_Metsuke.Views;

namespace TANREN_Metsuke;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        // initialize live charts lib
        LiveCharts.Configure(config => config.AddSkiaSharp().AddDefaultMappers());
        // load workout sessions, settings and create the main view model
        var settings = SettingsService.Load();
        var mainVm = new MainViewModel([], settings);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow { DataContext = mainVm };
            desktop.MainWindow = window;
            desktop.Exit += (_, _) =>
            {
                mainVm.Dispose();
                if (settings.IsDirty) SettingsService.Save(settings);
            };
            window.Opened += async (_, _) =>
            {
                await mainVm.ReloadAsync();
                await CheckForDataAsync(window, mainVm);
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = mainVm };
            _ = mainVm.ReloadAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task CheckForDataAsync(Window window, MainViewModel vm)
    {
        if (vm.HasWorkouts)
            return;
        var dialog = new NoWorkoutsDialog();
        var goToSync = await dialog.ShowDialog<bool>(window);
        // if there is no workout data, prompt the user to go to the sync tab to import some, if they choose yes, we switch to the sync tab
        if (goToSync)
            vm.SelectedTabIndex = (int)AppTab.Sync;
    }
}
