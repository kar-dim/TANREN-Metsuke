using System.Collections.Generic;
using System.IO;
using ReactiveUI;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// order must match the TabItem order in MainView.axaml
public enum AppTab { Home, Graphs, Records, Settings, Sync, Info }

// The main view model for the application, responsible for managing the state of the main window and coordinating between different sub view models
public class MainViewModel : ViewModelBase
{
    public SettingsViewModel Settings { get; }
    public SyncViewModel Sync { get; }

    private List<WorkoutSession> sessions = [];

    private int selectedTabIndex;
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => this.RaiseAndSetIfChanged(ref selectedTabIndex, value);
    }

    private RecordsViewModel records = null!;
    public RecordsViewModel Records
    {
        get => records;
        private set => this.RaiseAndSetIfChanged(ref records, value);
    }

    private SummaryViewModel summary = null!;
    public SummaryViewModel Summary
    {
        get => summary;
        private set => this.RaiseAndSetIfChanged(ref summary, value);
    }

    private GraphsViewModel graphs = null!;
    public GraphsViewModel Graphs
    {
        get => graphs;
        private set => this.RaiseAndSetIfChanged(ref graphs, value);
    }

    // initialize the main model: load settings, create sync view model and load workout sessions
    public MainViewModel(List<WorkoutSession> sessions, AppSettings settings)
    {
        Settings = new SettingsViewModel(settings,
            onSecondaryChanged: weight => { Summary?.Recompute(weight); Graphs?.UpdateSecondaryWeight(weight); },
            onUnitChanged: () => Load(this.sessions));

        Sync = new SyncViewModel(
            getFolder: () => SettingsService.WorkoutsFolder,
            onFileSaved: () => Reload(LoadSessions()));

        Load(sessions);
    }

    public void Reload(List<WorkoutSession> sessions) => Load(sessions);

    private void Load(List<WorkoutSession> sessions)
    {
        ExerciseCatalog.LoadCustomExercises();
        this.sessions = sessions;
        var imperial = Settings.UseImperial;
        Summary = new SummaryViewModel(sessions, Settings.CurrentSecondaryWeight, imperial);
        Graphs = new GraphsViewModel(sessions, imperial, Settings.CurrentSecondaryWeight);
        Records = new RecordsViewModel(sessions, imperial);
    }

    public static List<WorkoutSession> LoadSessions()
    {
        var folder = SettingsService.WorkoutsFolder;
        if (!Directory.Exists(folder))
            return [];
        return new JsonWorkoutRepository(folder).LoadAll();
    }
}
