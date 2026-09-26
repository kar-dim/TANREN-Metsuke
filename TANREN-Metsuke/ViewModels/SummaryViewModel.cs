using System;
using System.Collections.Generic;
using System.Linq;
using ReactiveUI;
using TANREN_Metsuke.Models;
using TANREN_Metsuke.Services;

namespace TANREN_Metsuke.ViewModels;

// ViewModel for summarizing workout sessions, showing total volume and muscle group breakdowns
public class SummaryViewModel : ViewModelBase
{
    private readonly List<WorkoutSession> sessions;
    private readonly bool imperial;
    private double secondaryWeight;

    public int TotalSessions { get; }
    public bool IsImperial => imperial;
    public Dictionary<MuscleGroup, double> MuscleVolumes { get; private set; } = [];
    public double TotalVolume { get; }
    public double MaxMuscleVolume { get; private set; }

    public string StatsDisplay =>
        $"{TotalSessions} sessions  |  {WeightHelper.ToDisplay(TotalVolume, imperial):N0} {WeightHelper.Unit(imperial)} total volume";

    public event Action? VolumesChanged;

    public SummaryViewModel(List<WorkoutSession> sessions, double secondaryWeight, bool imperial = false)
    {
        this.sessions = sessions;
        this.secondaryWeight = secondaryWeight;
        this.imperial = imperial;
        TotalSessions = sessions.Count;
        TotalVolume = sessions.Sum(s => s.TotalVolume);
        UpdateVolumes(secondaryWeight);
    }

    public void Recompute(double secondaryWeight)
    {
        this.secondaryWeight = secondaryWeight;
        UpdateVolumes(secondaryWeight);
        this.RaisePropertyChanged(nameof(StatsDisplay));
        VolumesChanged?.Invoke();
    }

    private void UpdateVolumes(double weight)
    {
        MuscleVolumes = VolumeCalculator.ComputeVolumes(sessions, weight);
        MaxMuscleVolume = MuscleVolumes.Values.DefaultIfEmpty(0).Max();
    }

    public MuscleDetailViewModel CreateDetailViewModel(MuscleGroup muscle)
    {
        var includeSecondary = secondaryWeight > 0;
        var history = VolumeCalculator.GetHistoryForMuscle(sessions, muscle, includeSecondary);
        var volume = MuscleVolumes.GetValueOrDefault(muscle);
        return new MuscleDetailViewModel(muscle, volume, history, imperial);
    }
}
