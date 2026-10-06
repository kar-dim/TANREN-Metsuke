using System;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.Models;

public record SetRecord(double Kg, int Reps, DateOnly Date)
{
    public double Volume => Kg * Reps;
    public string DateDisplay => Date.ToString("d MMM yyyy");

    public string FormatDisplay(bool imperial)
    {
        if (Kg == 0)
            return $"{Reps} reps (no added weight)";
        return $"{WeightHelper.FormatValue(Kg, imperial)} {WeightHelper.Unit(imperial)} × {Reps}";
    }
}

public record SessionVolumeRecord(double Volume, DateOnly Date)
{
    public string DateDisplay => Date.ToString("d MMM yyyy");
}

public record ExercisePersonalRecord(string ExerciseId, string ExerciseName, SetRecord? BestWeight, SetRecord? BestSet,
    SetRecord BestReps, SessionVolumeRecord? BestSession, bool Imperial = false)
{
    public bool HasLoadRecords => BestWeight != null;
    public string BestWeightDisplay => BestWeight?.FormatDisplay(Imperial) ?? "—";
    public string BestSetDisplay => BestSet?.FormatDisplay(Imperial) ?? "—";
    public string BestRepsDisplay => BestReps.FormatDisplay(Imperial);
    public string BestSessionDisplay => BestSession == null ? "—" : $"{WeightHelper.ToDisplay(BestSession.Volume, Imperial):N0} {WeightHelper.Unit(Imperial)}";
}
