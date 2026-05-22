using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

public class WorkoutSetRow(int index, WorkoutSet set, bool imperial = false)
{
    public string SetLabel { get; } = $"Set {index}";
    public string Reps { get; } = set.Reps.ToString();
    public string Kg { get; } = WeightHelper.Format(set.Kg, imperial);
}

public class WorkoutEntryRow
{
    public string ExerciseName { get; }
    public List<WorkoutSetRow> Sets { get; }

    public WorkoutEntryRow(WorkoutEntry entry, bool imperial)
    {
        var def = ExerciseCatalog.Get(entry.ExerciseId);
        ExerciseName = def?.Name ?? entry.ExerciseId;
        Sets = [.. entry.Sets.Select((s, i) => new WorkoutSetRow(i + 1, s, imperial))];
    }
}

// ViewModel for displaying details of a workout session, including date, total volume, and individual entries
public class WorkoutDetailViewModel
{
    public string DateString { get; }
    public string TotalVolume { get; }
    public List<WorkoutEntryRow> Entries { get; }

    public WorkoutDetailViewModel(WorkoutSession session, bool imperial = false)
    {
        DateString = session.Date.ToString("dddd, d MMMM yyyy");
        var display = WeightHelper.ToDisplay(session.TotalVolume, imperial);
        TotalVolume = $"Total volume: {display:N0} {WeightHelper.Unit(imperial)}";
        Entries = [.. session.Entries.Select(e => new WorkoutEntryRow(e, imperial))];
    }
}
