using System;
using System.Collections.Generic;
using System.Linq;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

public class DummyWorkoutRepository : IWorkoutRepository
{
    public List<WorkoutSession> LoadAll() => GenerateSessions();

    private static readonly string[] PushExercises = ["bench_press", "overhead_press", "lateral_raise", "tricep_pushdown", "dip"];

    private static readonly string[] PullExercises = ["barbell_row", "lat_pulldown", "bicep_curl", "hammer_curl", "face_pull"];

    private static readonly string[] LegExercises = ["squat", "leg_press", "leg_extension", "leg_curl", "calf_raise", "glute_bridge"];

    private static readonly string[][] Splits = [PushExercises, PullExercises, LegExercises];

    private static List<WorkoutSession> GenerateSessions()
    {
        var rng = new Random(42);
        List<WorkoutSession> sessions = [];
        var splitIndex = 0;
        var start = new DateOnly(2026, 1, 6);
        var end = new DateOnly(2026, 5, 15);

        for (var date = start; date <= end; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Sunday)
                continue;
            if (rng.NextDouble() < 0.3)
                continue;

            var exercises = Splits[splitIndex % 3];
            splitIndex++;

            // Progressive overload: 2% per week
            var weekNumber = (date.DayNumber - start.DayNumber) / 7;
            var progress = 1.0 + weekNumber * 0.02;

            List<WorkoutEntry> entries = [];
            foreach (var id in exercises)
            {
                if (rng.NextDouble() < 0.1)
                    continue;

                var numSets = rng.Next(3, 5);
                var baseKg = BaseWeight(id) * progress;
                List<WorkoutSet> sets = [];

                for (int s = 0; s < numSets; s++)
                {
                    // Round to nearest 2.5 kg for realism
                    var raw = baseKg + rng.Next(-3, 4);
                    var kg = Math.Round(raw / 2.5) * 2.5;
                    var reps = rng.Next(6, 13);
                    sets.Add(new WorkoutSet { Reps = reps, Kg = kg });
                }

                entries.Add(new WorkoutEntry { ExerciseId = id, Sets = sets });
            }

            if (entries.Count > 0)
                sessions.Add(new WorkoutSession { Date = date, Entries = entries });
        }

        return [.. sessions.OrderBy(s => s.Date)];
    }

    private static double BaseWeight(string id) => id switch
    {
        "bench_press" => 80,
        "overhead_press" => 50,
        "squat" => 100,
        "barbell_row" => 70,
        "lat_pulldown" => 60,
        "leg_press" => 150,
        "bicep_curl" => 15,
        "hammer_curl" => 14,
        "tricep_pushdown" => 25,
        "lateral_raise" => 10,
        "leg_extension" => 50,
        "leg_curl" => 40,
        "calf_raise" => 60,
        "glute_bridge" => 80,
        "face_pull" => 20,
        "dip" => 10,
        _ => 20
    };
}
