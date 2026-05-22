using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace TANREN_Metsuke.Models;

public class WorkoutSet
{
    public int Reps { get; set; }
    public double Kg { get; set; }

    [JsonIgnore]
    public double Volume => Reps * Kg;
}

public class WorkoutEntry
{
    public string ExerciseId { get; set; } = "";
    public List<WorkoutSet> Sets { get; set; } = [];

    [JsonIgnore]
    public double Volume => Sets.Sum(s => s.Volume);
}

public class WorkoutSession
{
    public DateOnly Date { get; set; }
    public List<WorkoutEntry> Entries { get; set; } = [];

    [JsonIgnore]
    public double TotalVolume => Entries.Sum(e => e.Volume);
}
