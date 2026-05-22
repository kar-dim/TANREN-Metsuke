using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;
using TANREN_Metsuke.ViewModels;

namespace TANREN_Metsuke.Views;

public partial class SummaryView : BodyMapView
{
    private static readonly Color HeatGreen = Color.Parse("#4CAF50");
    private static readonly Color HeatYellowGreen = Color.Parse("#8BC34A");
    private static readonly Color HeatAmber = Color.Parse("#FFC107");
    private static readonly Color HeatOrange = Color.Parse("#FF5722");
    private static readonly Color HeatRed = Color.Parse("#F44336");

    private SummaryViewModel? ViewModel => DataContext as SummaryViewModel;

    public SummaryView()
    {
        InitializeComponent();
        AttachBodyMap();
    }

    protected override void OnViewModelChanged()
    {
        if (ViewModel == null)
            return;
        ViewModel.VolumesChanged += OnVolumesChanged;
        RefreshStats();
    }

    // refresh stats and colors when volumes change, also refresh detail if it's open to reflect new data
    private void OnVolumesChanged()
    {
        RefreshStats();
        ApplyColors();
        RefreshOpenDetail();
    }

    private void RefreshStats()
    {
        if (ViewModel != null)
            StatsText.Text = ViewModel.StatsDisplay;
    }

    protected override Color ColorForGroup(MuscleGroup group)
    {
        if (ViewModel == null)
            return BodyMapHelper.MuscleInactive;
        var max = ViewModel.MuscleVolumes.Values.DefaultIfEmpty(0).Max();
        if (max == 0)
            return BodyMapHelper.MuscleInactive;
        return HeatColor(ViewModel.MuscleVolumes.GetValueOrDefault(group) / max);
    }

    protected override string HoverTextFor(MuscleGroup group)
    {
        var vol = ViewModel?.MuscleVolumes.GetValueOrDefault(group) ?? 0;
        var imperial = ViewModel?.IsImperial ?? false;
        var volDisplay = WeightHelper.ToDisplay(vol, imperial);
        return $"{group}  |  {volDisplay:N0} {WeightHelper.Unit(imperial)} total volume  (click for history)";
    }

    protected override string DefaultHoverText => "Hover over a muscle group for details (click to see history)";

    protected override bool CanOpenDetail(MuscleGroup group) => ViewModel != null;

    protected override void PopulateDetail(MuscleGroup group)
    {
        if (ViewModel == null)
            return;
        var detailVm = ViewModel.CreateDetailViewModel(group);
        DetailTitle.Text = detailVm.Title;
        DetailVolume.Text = detailVm.TotalVolumeDisplay;
        DetailDaysList.ItemsSource = detailVm.Days;
    }

    // heat color mapping based on percentage of max volume (simple linear interpolation between defined colors)
    private static Color HeatColor(double t)
    {
        if (t < 0.01)
            return BodyMapHelper.MuscleInactive;
        if (t < 0.25)
            return Lerp(HeatGreen, HeatYellowGreen, t / 0.25);
        if (t < 0.50)
            return Lerp(HeatYellowGreen, HeatAmber, (t - 0.25) / 0.25);
        if (t < 0.75)
            return Lerp(HeatAmber, HeatOrange, (t - 0.50) / 0.25);
        return Lerp(HeatOrange, HeatRed, (t - 0.75) / 0.25);
    }

    // color interpolation helper
    private static Color Lerp(Color a, Color b, double t)
    {
        t = Math.Clamp(t, 0, 1);
        return Color.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }
}
