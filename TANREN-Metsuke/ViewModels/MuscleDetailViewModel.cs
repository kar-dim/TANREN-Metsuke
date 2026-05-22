using System;
using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// ViewModel for displaying workout history for a specific muscle group
public class MuscleDetailViewModel : ViewModelBase
{
    public string Title { get; }
    public string TotalVolumeDisplay { get; }
    public List<WorkoutDayGroup> Days { get; }

    public MuscleDetailViewModel(
        MuscleGroup muscle,
        double totalVolume,
        List<(DateOnly Date, string ExerciseName, List<WorkoutSet> Sets, bool IsPrimary)> history,
        bool imperial = false)
    {
        Title = $"{muscle} | Workout History";
        TotalVolumeDisplay = $"Total volume: {WeightHelper.ToDisplay(totalVolume, imperial):N0} {WeightHelper.Unit(imperial)}";

        Days = [.. history
            .GroupBy(h => h.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new WorkoutDayGroup
            {
                DateText = g.Key.ToString("dd/MM/yyyy (dddd)", System.Globalization.CultureInfo.InvariantCulture), // Format date as "dd/MM/yyyy always
                Exercises = [.. g.Select(e => new ExerciseRow
                {
                    Name = e.ExerciseName,
                    Tag = e.IsPrimary ? "" : " (secondary)", // highlight if it is secondary
                    IsPrimary = e.IsPrimary,
                    Sets = [.. e.Sets.Select((s, i) => new WorkoutSetRow(i + 1, s, imperial))]
                })]
            })];
    }
}

public class WorkoutDayGroup
{
    public string DateText { get; set; } = "";
    public List<ExerciseRow> Exercises { get; set; } = [];
}

public class ExerciseRow
{
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    public bool IsPrimary { get; set; } = true;
    public List<WorkoutSetRow> Sets { get; set; } = [];
    public string Display => $"{Name}{Tag}";
}
