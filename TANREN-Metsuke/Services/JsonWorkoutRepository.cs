using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// Repository class to load workout sessions from JSON files in a specified folder,
// each file is expected to contain a single WorkoutSession object serialized as JSON
public class JsonWorkoutRepository(string folder, Action<string>? onWarning = null) : IWorkoutRepository
{
    public List<WorkoutSession> LoadAll()
    {
        lock (WorkoutStorage.Gate)
        {
            WorkoutStorage.Recover(folder);
            return LoadSnapshot();
        }
    }

    private List<WorkoutSession> LoadSnapshot()
    {
        List<WorkoutSession> sessions = [];
        if (!Directory.Exists(folder))
            return sessions;

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            if (!WorkoutJson.IsWorkoutFilename(Path.GetFileName(file)))
                continue;
            try
            {
                string json = File.ReadAllText(file);
                var session = WorkoutJson.ReadWorkout(Path.GetFileName(file), json);
                if (session.Entries.Count > 0)
                    sessions.Add(session);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or IOException or UnauthorizedAccessException)
            {
                onWarning?.Invoke($"Skipped {Path.GetFileName(file)}: {ex.Message}");
            }
        }
        sessions.Sort((a, b) => a.Date.CompareTo(b.Date));
        return sessions;
    }
}
