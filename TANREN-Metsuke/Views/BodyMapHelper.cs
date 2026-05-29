using System;
using Avalonia.Media;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Theme;

namespace TANREN_Metsuke.Views;

internal enum ShapeKind { Shoulder, Chest, Arm, Forearm, Core, Quad, Shin, Traps, Lat, Glute, Hamstring, Calf }

internal record RegionDef(MuscleGroup Group, double X, double Y, double W, double H, ShapeKind Shape, bool Mirror = false, double Rotation = 0);

internal static class BodyMapHelper
{
    internal static readonly Color MuscleInactive = Color.Parse(Palette.MuscleInactive);
    internal static readonly Color BodyLabelGray = Color.Parse(Palette.BodyLabelGray);

    // define all muscle regions with their position, size, shape, and other properties
    internal static RegionDef[] AllRegions() =>
    [
        // FRONT
        new(MuscleGroup.Shoulders, 120, 172, 78, 50, ShapeKind.Shoulder, Rotation: -45),
        new(MuscleGroup.Shoulders, 314, 172, 78, 50, ShapeKind.Shoulder, Mirror: true, Rotation: -45),
        new(MuscleGroup.Chest, 160, 192, 92, 90, ShapeKind.Chest),
        new(MuscleGroup.Chest, 252, 192, 92, 90, ShapeKind.Chest, Mirror: true),
        new(MuscleGroup.Biceps, 136, 248, 36, 82, ShapeKind.Arm),
        new(MuscleGroup.Biceps, 352, 248, 36, 82, ShapeKind.Arm, Mirror: true),
        new(MuscleGroup.Forearms, 122, 350, 28, 80, ShapeKind.Forearm, Rotation: 20),
        new(MuscleGroup.Forearms, 362, 350, 28, 80, ShapeKind.Forearm, Mirror: true, Rotation: 20),
        new(MuscleGroup.Core, 211, 278, 84,  230, ShapeKind.Core, Rotation: -2),
        new(MuscleGroup.Quads, 173, 482, 74, 180, ShapeKind.Quad),
        new(MuscleGroup.Quads, 268, 482, 74, 180, ShapeKind.Quad, Mirror: true),
        new(MuscleGroup.Shins, 184, 790, 34, 105, ShapeKind.Shin, Rotation: -8),
        new(MuscleGroup.Shins, 278, 790, 34, 105, ShapeKind.Shin, Mirror: true, Rotation: -8),
        // BACK
        new(MuscleGroup.Traps, 633, 138, 150, 165, ShapeKind.Traps),
        new(MuscleGroup.Back, 626, 218, 60, 175, ShapeKind.Lat, Rotation: -10),
        new(MuscleGroup.Back, 730, 218, 60, 175, ShapeKind.Lat, Mirror: true, Rotation: -10),
        new(MuscleGroup.Triceps, 582, 232, 36, 82, ShapeKind.Arm),
        new(MuscleGroup.Triceps, 806, 232, 36, 82, ShapeKind.Arm, Mirror: true),
        new(MuscleGroup.Glutes, 624, 440, 76, 90, ShapeKind.Glute),
        new(MuscleGroup.Glutes, 704, 440, 76, 90, ShapeKind.Glute, Mirror: true),
        new(MuscleGroup.Hamstrings,628, 534, 72, 170, ShapeKind.Hamstring),
        new(MuscleGroup.Hamstrings,718, 534, 72, 170, ShapeKind.Hamstring, Mirror: true),
        new(MuscleGroup.Calves, 638, 710, 56, 188, ShapeKind.Calf, Rotation: -8),
        new(MuscleGroup.Calves, 729, 710, 56, 188, ShapeKind.Calf, Mirror: true, Rotation: -4),
    ];

    // generate a Geometry object for a given shape kind and size, this IS vibe coded to perfection... It also uses SVG path data to define the shapes
    internal static Geometry MakeGeometry(ShapeKind kind, double w, double h)
    {
        var d = kind switch
        {
            ShapeKind.Shoulder => Fmt($"M 0,{h} C 0,{h * 0.15} {w * 0.15},0 {w / 2},0 C {w * 0.85},0 {w},{h * 0.15} {w},{h} C {w * 0.7},{h * 0.82} {w / 2},{h * 0.75} {w * 0.3},{h * 0.82} Z"),
            ShapeKind.Chest => Fmt($"M {w * 0.05},{h * 0.28} C 0,{h * 0.12} {w * 0.42},{h * -0.04} {w * 0.5},{h * -0.06} C {w * 0.58},{h * -0.04} {w},{h * 0.12} {w * 0.95},{h * 0.28} C {w},{h * 0.6} {w * 0.82},{h * 0.95} {w * 0.5},{h} C {w * 0.18},{h * 0.95} 0,{h * 0.6} {w * 0.05},{h * 0.28} Z"),
            ShapeKind.Arm => Fmt($"M {w / 2},0 C {w * 0.95},{h * 0.05} {w},{h * 0.3} {w * 0.92},{h / 2} C {w * 0.85},{h * 0.7} {w * 0.7},{h * 0.95} {w / 2},{h} C {w * 0.3},{h * 0.95} {w * 0.15},{h * 0.7} {w * 0.08},{h / 2} C 0,{h * 0.3} {w * 0.05},{h * 0.05} {w / 2},0 Z"),
            ShapeKind.Forearm => Fmt($"M {w * 0.55},0 C {w * 0.95},{h * 0.05} {w},{h * 0.25} {w * 0.88},{h * 0.5} C {w * 0.75},{h * 0.78} {w * 0.58},{h} {w * 0.38},{h} C {w * 0.18},{h} {w * 0.05},{h * 0.75} 0,{h * 0.5} C 0,{h * 0.22} {w * 0.15},{h * 0.03} {w * 0.55},0 Z"),
            ShapeKind.Core => Fmt($"M {w * 0.08},{h * 0.02} C {w * 0.35},0 {w * 0.65},0 {w * 0.92},{h * 0.02} C {w},{h * 0.15} {w},{h * 0.38} {w * 0.88},{h * 0.58} C {w * 0.72},{h * 0.78} {w * 0.62},{h * 0.9} {w * 0.5},{h} C {w * 0.38},{h * 0.9} {w * 0.28},{h * 0.78} {w * 0.12},{h * 0.58} C 0,{h * 0.38} 0,{h * 0.15} {w * 0.08},{h * 0.02} Z"),
            ShapeKind.Quad => Fmt($"M {w * 0.42},0 C {w * 0.88},0 {w},{h * 0.18} {w},{h * 0.42} C {w},{h * 0.68} {w * 0.78},{h * 0.95} {w * 0.48},{h} C {w * 0.18},{h * 0.95} 0,{h * 0.68} 0,{h * 0.42} C 0,{h * 0.18} {w * 0.12},0 {w * 0.42},0 Z"),
            ShapeKind.Shin => Fmt($"M {w / 2},0 C {w * 0.88},{h * 0.03} {w},{h * 0.18} {w * 0.95},{h * 0.42} C {w * 0.88},{h * 0.7} {w * 0.7},{h * 0.95} {w / 2},{h} C {w * 0.3},{h * 0.95} {w * 0.12},{h * 0.7} {w * 0.05},{h * 0.42} C 0,{h * 0.18} {w * 0.12},{h * 0.03} {w / 2},0 Z"),
            ShapeKind.Traps => Fmt($"M 0,{h * 0.27} C {w * 0.1},{h * 0.06} {w * 0.44},0 {w * 0.5},0 C {w * 0.56},0 {w * 0.9},{h * 0.06} {w},{h * 0.27} C {w * 0.72},{h * 0.82} {w * 0.52},{h * 0.97} {w * 0.5},{h} C {w * 0.48},{h * 0.97} {w * 0.28},{h * 0.82} 0,{h * 0.27} Z"),
            ShapeKind.Lat => Fmt($"M {w * 0.25},0 C {w * 0.75},0 {w},{h * 0.22} {w},{h * 0.48} C {w},{h * 0.75} {w * 0.72},{h} {w * 0.42},{h} C {w * 0.12},{h} 0,{h * 0.68} 0,{h * 0.35} C 0,{h * 0.12} {w * 0.08},0 {w * 0.25},0 Z"),
            ShapeKind.Glute => Fmt($"M {w * 0.18},0 C {w * 0.55},0 {w},{h * 0.12} {w},{h * 0.48} C {w},{h * 0.82} {w * 0.72},{h} {w * 0.42},{h} C {w * 0.12},{h} 0,{h * 0.78} 0,{h * 0.42} C 0,{h * 0.08} {w * 0.05},0 {w * 0.18},0 Z"),
            ShapeKind.Hamstring => Fmt($"M {w * 0.42},0 C {w * 0.85},0 {w},{h * 0.18} {w},{h * 0.42} C {w},{h * 0.7} {w * 0.8},{h * 0.95} {w * 0.48},{h} C {w * 0.18},{h * 0.95} 0,{h * 0.7} 0,{h * 0.42} C 0,{h * 0.18} {w * 0.12},0 {w * 0.42},0 Z"),
            ShapeKind.Calf => Fmt($"M {w / 2},0 C {w * 0.92},{h * 0.05} {w},{h * 0.2} {w},{h * 0.36} C {w},{h * 0.55} {w * 0.72},{h * 0.85} {w / 2},{h} C {w * 0.28},{h * 0.85} 0,{h * 0.55} 0,{h * 0.36} C 0,{h * 0.2} {w * 0.08},{h * 0.05} {w / 2},0 Z"),
            _ => $"M 0,0 L {w},0 L {w},{h} L 0,{h} Z"
        };
        return Geometry.Parse(d);
    }

    // this is used to always force decimals to be "." in the SVG path data, regardless of the user's locale
    private static string Fmt(FormattableString s) => s.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
