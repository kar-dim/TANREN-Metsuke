using System.Collections.Generic;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// ViewModel for displaying personal records across different muscle groups
public class RecordsViewModel : ViewModelBase
{
    private readonly Dictionary<MuscleGroup, List<ExercisePersonalRecord>> records;

    public int TotalExercisesTracked { get; }

    public RecordsViewModel(List<WorkoutSession> sessions, bool imperial = false)
    {
        records = PersonalRecordCalculator.Compute(sessions, imperial);
        foreach (var list in records.Values)
            TotalExercisesTracked += list.Count;
    }

    public List<ExercisePersonalRecord> GetRecords(MuscleGroup muscle) => records.GetValueOrDefault(muscle) ?? [];

    public bool HasRecords(MuscleGroup muscle) => records.TryGetValue(muscle, out var list) && list.Count > 0;

    public int RecordCount(MuscleGroup muscle) => records.GetValueOrDefault(muscle)?.Count ?? 0;
}
