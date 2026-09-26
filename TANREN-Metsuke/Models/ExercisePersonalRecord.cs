using System;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.Models;

public record SetRecord(double Kg, int Reps, DateOnly Date)
{
    public double Volume => Kg * Reps;
    public string DateDisplay => Date.ToString("d MMM yyyy");

    public string FormatDisplay(bool imperial)
    {
        return $"{WeightHelper.FormatValue(Kg, imperial)} {WeightHelper.Unit(imperial)} × {Reps}";
    }
}

public record ExercisePersonalRecord(string ExerciseName, SetRecord BestWeight, SetRecord BestSet, bool Imperial = false)
{
    public string BestWeightDisplay => BestWeight.FormatDisplay(Imperial);
    public string BestSetDisplay => BestSet.FormatDisplay(Imperial);
}
