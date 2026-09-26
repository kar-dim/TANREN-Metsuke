using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

public static class VolumeCalculator
{
    // primary muscles get the full value, secondaries get value * secondaryWeight (skipped when 0)
    public static Dictionary<MuscleGroup, double> AggregatePerMuscle(IEnumerable<WorkoutSession> sessions, Func<WorkoutEntry, double> metric, double secondaryWeight)
    {
        var totals = new Dictionary<MuscleGroup, double>();
        foreach (var session in sessions)
        {
            foreach (var entry in session.Entries)
            {
                var exercise = ExerciseCatalog.Get(entry.ExerciseId);
                if (exercise == null)
                    continue;

                double value = metric(entry);
                foreach (var muscle in exercise.PrimaryMuscles)
                {
                    ref var current = ref CollectionsMarshal.GetValueRefOrAddDefault(totals, muscle, out _);
                    current += value;
                }
                // don't calculate if user has selected "0" weight for secondary!
                if (secondaryWeight > 0)
                {
                    double secVal = value * secondaryWeight;
                    foreach (var muscle in exercise.SecondaryMuscles)
                    {
                        ref var current = ref CollectionsMarshal.GetValueRefOrAddDefault(totals, muscle, out _);
                        current += secVal;
                    }
                }
            }
        }

        return totals;
    }

    public static Dictionary<MuscleGroup, double> ComputeVolumes(List<WorkoutSession> sessions, double secondaryWeight = 0.5) =>
        AggregatePerMuscle(sessions, e => e.Volume, secondaryWeight);

    public static List<(DateOnly Date, string ExerciseName, List<WorkoutSet> Sets, bool IsPrimary)>
        GetHistoryForMuscle(List<WorkoutSession> sessions, MuscleGroup muscle, bool includeSecondary = true)
    {
        var results = new List<(DateOnly, string, List<WorkoutSet>, bool)>();
        // reverse loop because sessions are already ordered ascending by Date
        for (int i = sessions.Count - 1; i >= 0; i--)
        {
            var session = sessions[i];
            foreach (var entry in session.Entries)
            {
                var exercise = ExerciseCatalog.Get(entry.ExerciseId);
                if (exercise == null)
                    continue;
                bool isPrimary = exercise.PrimaryMuscles.Contains(muscle);
                bool isSecondary = exercise.SecondaryMuscles.Contains(muscle);
                if (!isPrimary && (!isSecondary || !includeSecondary))
                    continue;
                results.Add((session.Date, exercise.Name, entry.Sets, isPrimary));
            }
        }

        return results;
    }
}
