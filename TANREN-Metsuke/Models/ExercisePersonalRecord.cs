using System;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.Models;

public record SetRecord(double Kg, int Reps, DateOnly Date)
{
    public double Volume => Kg * Reps;
    public string DateDisplay => Date.ToString("d MMM yyyy");

    public string FormatDisplay(bool imperial)
    {
        var w = WeightHelper.ToDisplay(Kg, imperial);
        var wStr = w % 1 == 0 ? $"{w:F0}" : $"{w:F1}";
        return $"{wStr} {WeightHelper.Unit(imperial)} × {Reps}";
    }
}

public record ExercisePersonalRecord(string ExerciseName, SetRecord BestWeight, SetRecord BestSet, bool Imperial = false)
{
    public string BestWeightDisplay => BestWeight.FormatDisplay(Imperial);
    public string BestSetDisplay => BestSet.FormatDisplay(Imperial);
}
