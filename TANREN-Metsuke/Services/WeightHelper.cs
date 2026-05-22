using System;

namespace TANREN_Metsuke.Services;

// Weight helper class, used mostly to convert weight from kg to lb for display purposes, and to format the display string
public static class WeightHelper
{
    private const double LbPerKg = 2.20462;

    public static double ToDisplay(double kg, bool imperial) => imperial ? Math.Round(kg * LbPerKg, 2) : Math.Round(kg, 2);

    public static string Unit(bool imperial) => imperial ? "lb" : "kg";

    public static string Format(double kg, bool imperial)
    {
        double v = ToDisplay(kg, imperial);
        return v % 1 == 0 ? $"{v:F0} {Unit(imperial)}" : $"{v:F2} {Unit(imperial)}";
    }
}
