using System.IO;
using System.Linq;
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
        var sessions = MainViewModel.LoadSessions();
        var mainVm = new MainViewModel(sessions, settings);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow { DataContext = mainVm };
            desktop.MainWindow = window;
            desktop.Exit += (_, _) => { if (settings.IsDirty) SettingsService.Save(settings); }; // save settings on exit if they were changed (dirty)
            window.Opened += async (_, _) => await CheckForDataAsync(window, mainVm);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task CheckForDataAsync(Window window, MainViewModel vm)
    {
        var workoutFolder = SettingsService.WorkoutsFolder;
        var hasData = Directory.Exists(workoutFolder) && Directory.EnumerateFiles(workoutFolder, "*.json").Any();
        if (hasData)
            return;
        var dialog = new NoWorkoutsDialog();
        var goToSync = await dialog.ShowDialog<bool>(window);
        // if there is no workout data, prompt the user to go to the sync tab to import some, if they choose yes, we switch to the sync tab
        if (goToSync)
            vm.SelectedTabIndex = (int)AppTab.Sync;
    }
}
