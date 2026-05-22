using System;
using System.Collections.Generic;
using System.Linq;
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
                    totals.TryAdd(muscle, 0);
                    totals[muscle] += value;
                }
                // don't calculate if user has selected "0" wight for secondary!
                if (secondaryWeight > 0)
                {
                    foreach (var muscle in exercise.SecondaryMuscles)
                    {
                        totals.TryAdd(muscle, 0);
                        totals[muscle] += value * secondaryWeight;
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
        foreach (var session in sessions.OrderByDescending(s => s.Date))
        {
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
