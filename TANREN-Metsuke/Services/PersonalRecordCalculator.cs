using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// Helper class to compute personal records for exercises based on workout sessions
public static class PersonalRecordCalculator
{
    public static Dictionary<MuscleGroup, List<ExercisePersonalRecord>> Compute(List<WorkoutSession> sessions, bool imperial = false)
    {
        var best = new Dictionary<string, (SetRecord? wt, SetRecord? sv, SetRecord? reps, SessionVolumeRecord? session)>();
        foreach (var session in sessions)
            foreach (var group in session.Entries.GroupBy(e => e.ExerciseId))
            {
                best.TryGetValue(group.Key, out var b);
                double volume = 0;
                foreach (var set in group.SelectMany(e => e.Sets))
                {
                    if (set.Kg < 0 || !double.IsFinite(set.Kg) || set.Reps <= 0)
                        continue;
                    var setRecord = new SetRecord(set.Kg, set.Reps, session.Date);
                    if (b.reps == null || set.Reps > b.reps.Reps || (set.Reps == b.reps.Reps && set.Kg > b.reps.Kg))
                        b = b with { reps = setRecord };
                    if (set.Kg == 0)
                        continue;
                    volume += setRecord.Volume;
                    if (b.wt == null || set.Kg > b.wt.Kg || (set.Kg == b.wt.Kg && set.Reps > b.wt.Reps))
                        b = b with { wt = setRecord };
                    if (b.sv == null || setRecord.Volume > b.sv.Volume)
                        b = b with { sv = setRecord };
                }
                if (volume > 0 && (b.session == null || volume > b.session.Volume))
                    b = b with { session = new SessionVolumeRecord(volume, session.Date) };
                best[group.Key] = b;
            }

        var result = new Dictionary<MuscleGroup, List<ExercisePersonalRecord>>();
        foreach (var (id, (wt, sv, reps, session)) in best)
        {
            if (reps == null)
                continue;
            var def = ExerciseCatalog.Get(id);
            if (def == null)
                continue;

            var pr = new ExercisePersonalRecord(id, def.Name, wt, sv, reps, session, imperial);
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
