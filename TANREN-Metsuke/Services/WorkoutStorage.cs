using System;
using System.IO;

namespace TANREN_Metsuke.Services;

// Keep folder swaps and recovery coordinated with workout readers.
public static class WorkoutStorage
{
    // Readers see either the old directory or the complete replacement, never a half-written batch.
    public static object Gate { get; } = new();

    // Place staging beside the live folder so directory moves stay on the same filesystem.
    internal static string TransactionFolder(string folder) => Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(folder))!, "." + Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) + "-sync");

    public static void Recover(string folder)
    {
        lock (Gate)
        {
            var transaction = TransactionFolder(folder);
            var previous = Path.Combine(transaction, "previous");
            if (!Directory.Exists(previous))
                return;
            // A missing live folder means the swap stopped early, otherwise keep the old folder as a backup.
            if (!Directory.Exists(folder))
                Directory.Move(previous, folder);
            else
                ArchivePrevious(folder, previous);
        }
    }

    internal static void ArchivePrevious(string folder, string previous)
    {
        var backups = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(folder))!, "sync-backups");
        Directory.CreateDirectory(backups);
        // Include a unique suffix so transfers in the same second cannot overwrite a backup.
        Directory.Move(previous, Path.Combine(backups, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")));
    }
}
