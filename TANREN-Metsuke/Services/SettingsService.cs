using System.IO;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// Main settings service for loading and saving application settings to an INI file.
public static class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(AppPaths.BaseDir, "settings.ini");
    public static readonly string WorkoutsFolder = Path.Combine(AppPaths.BaseDir, "workouts");

    // loads from disk if exists, else it constructs with default values
    public static AppSettings Load()
    {
        var settings = new AppSettings();
        if (!File.Exists(SettingsPath))
            return settings;

        // TODO: we must use a proper INI parser in the future, not this custom one!!
        foreach (var line in File.ReadAllLines(SettingsPath))
        {
            // parse ini manually (for now), ignore comments and sections
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('[') || line.StartsWith(';'))
                continue;
            // split on first '='
            var eq = line.IndexOf('=');
            if (eq < 0)
                continue;
            //get the KEY and VALUE, trim whitespace
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            //parse only known keys, ignore unknown ones and invalid values
            switch (key)
            {
                case "SecondaryContribution":
                    if (int.TryParse(value, out int sc) && (sc == 0 || sc == 50 || sc == 100))
                        settings.SecondaryContribution = sc;
                    break;
                case "UseImperial":
                    if (bool.TryParse(value, out bool ui))
                        settings.UseImperial = ui;
                    break;
            }
        }

        return settings;
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllLines(SettingsPath,
        [
            "[General]",
            $"SecondaryContribution={settings.SecondaryContribution}",
            $"UseImperial={settings.UseImperial}"
        ]);
    }
}
