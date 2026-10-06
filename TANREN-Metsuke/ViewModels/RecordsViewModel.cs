using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// ViewModel for displaying personal records across different muscle groups
public class RecordsViewModel : ViewModelBase
{
    private Dictionary<MuscleGroup, List<ExercisePersonalRecord>> records;
    public event System.Action? RecordsChanged;

    public int TotalExercisesTracked { get; }

    public RecordsViewModel(List<WorkoutSession> sessions, bool imperial = false)
    {
        records = PersonalRecordCalculator.Compute(sessions, imperial);
        // an exercise can list several primary muscles, so we count distinct exercises to not count duplicates
        TotalExercisesTracked = records.Values
            .SelectMany(list => list)
            .Select(pr => pr.ExerciseId)
            .Distinct()
            .Count();
    }

    public List<ExercisePersonalRecord> GetRecords(MuscleGroup muscle) => records.GetValueOrDefault(muscle) ?? [];

    public bool HasRecords(MuscleGroup muscle) => records.TryGetValue(muscle, out var list) && list.Count > 0;

    public int RecordCount(MuscleGroup muscle) => records.GetValueOrDefault(muscle)?.Count ?? 0;

    public void UpdateImperial(bool imperial)
    {
        records = records.ToDictionary(pair => pair.Key, pair => pair.Value.Select(pr => pr with { Imperial = imperial }).ToList());
        RecordsChanged?.Invoke();
    }
}
