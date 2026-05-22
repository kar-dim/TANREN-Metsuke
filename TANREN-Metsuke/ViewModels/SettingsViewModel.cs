using System;
using ReactiveUI;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.ViewModels;

public enum SecondaryContribution { Full, Half, Off }

// ViewModel for the settings page, which allows the user to change the application's settings
public class SettingsViewModel(AppSettings settings, Action<double> onSecondaryChanged, Action onUnitChanged) : ViewModelBase
{
    private SecondaryContribution secondary = settings.SecondaryContribution switch
    {
        100 => SecondaryContribution.Full,
        0 => SecondaryContribution.Off,
        _ => SecondaryContribution.Half
    };

    public bool IsSecondaryFull { get => secondary == SecondaryContribution.Full; set { if (value) SetSecondary(SecondaryContribution.Full); } }
    public bool IsSecondaryHalf { get => secondary == SecondaryContribution.Half; set { if (value) SetSecondary(SecondaryContribution.Half); } }
    public bool IsSecondaryOff { get => secondary == SecondaryContribution.Off; set { if (value) SetSecondary(SecondaryContribution.Off); } }

    public bool IsKg { get => !settings.UseImperial; set { if (value) SetUnit(false); } }
    public bool IsLb { get => settings.UseImperial; set { if (value) SetUnit(true); } }

    public bool UseImperial => settings.UseImperial;

    public double CurrentSecondaryWeight => SecondaryWeight();

    private void SetSecondary(SecondaryContribution s)
    {
        secondary = s;
        settings.SecondaryContribution = s switch
        {
            SecondaryContribution.Full => 100,
            SecondaryContribution.Off => 0,
            _ => 50
        };
        settings.IsDirty = true; //user changed a value, so we need to save the settings when the app closes
        this.RaisePropertyChanged(nameof(IsSecondaryFull));
        this.RaisePropertyChanged(nameof(IsSecondaryHalf));
        this.RaisePropertyChanged(nameof(IsSecondaryOff));
        onSecondaryChanged(SecondaryWeight());
    }

    private void SetUnit(bool imperial)
    {
        settings.UseImperial = imperial;
        settings.IsDirty = true; //user changed a value, so we need to save the settings when the app closes
        this.RaisePropertyChanged(nameof(IsKg));
        this.RaisePropertyChanged(nameof(IsLb));
        onUnitChanged();
    }

    private double SecondaryWeight() => secondary switch
    {
        SecondaryContribution.Full => 1.0,
        SecondaryContribution.Half => 0.5,
        _ => 0.0
    };
}
