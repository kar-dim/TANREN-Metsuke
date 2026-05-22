using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// Helper class to compute personal records for exercises based on workout sessions
public static class PersonalRecordCalculator
{
    public static Dictionary<MuscleGroup, List<ExercisePersonalRecord>> Compute(List<WorkoutSession> sessions, bool imperial = false)
    {
        var catalog = ExerciseCatalog.All.ToDictionary(e => e.Id);
        var best = new Dictionary<string, (SetRecord? wt, SetRecord? sv)>();
        // search for best weight and best volume for each exercise across all sessions
        foreach (var session in sessions)
            foreach (var entry in session.Entries)
                foreach (var set in entry.Sets)
                {
                    if (set.Kg <= 0 || set.Reps <= 0)
                        continue;
                    var setRecord = new SetRecord(set.Kg, set.Reps, session.Date);
                    best.TryGetValue(entry.ExerciseId, out var b);
                    if (b.wt == null || set.Kg > b.wt.Kg || (set.Kg == b.wt.Kg && set.Reps > b.wt.Reps))
                        b = b with { wt = setRecord };
                    if (b.sv == null || setRecord.Volume > b.sv.Volume)
                        b = b with { sv = setRecord };
                    best[entry.ExerciseId] = b;
                }

        var result = new Dictionary<MuscleGroup, List<ExercisePersonalRecord>>();
        foreach (var (id, (wt, sv)) in best)
        {
            if (wt == null || sv == null)
                continue;
            if (!catalog.TryGetValue(id, out var def))
                continue;

            var pr = new ExercisePersonalRecord(def.Name, wt, sv, imperial);
            foreach (var muscle in def.PrimaryMuscles)
            {
                if (!result.TryGetValue(muscle, out var list))
                    result[muscle] = list = [];
                list.Add(pr);
            }
        }

        foreach (var list in result.Values)
            list.Sort((a, b) => string.Compare(a.ExerciseName, b.ExerciseName, System.StringComparison.OrdinalIgnoreCase));

        return result;
    }
}
