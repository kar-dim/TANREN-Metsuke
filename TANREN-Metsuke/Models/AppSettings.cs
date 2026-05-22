namespace TANREN_Metsuke.Models;

public class AppSettings
{
    // Valid values: 0, 50, 100, anything else defaults to 50 on load
    public int SecondaryContribution { get; set; } = 50;
    public bool UseImperial { get; set; } = false;

    // If a user changed a setting -> settings file will be overriden on exit
    public bool IsDirty { get; set; }
}
