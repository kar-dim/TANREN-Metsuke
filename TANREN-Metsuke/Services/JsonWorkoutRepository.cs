using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// Repository class to load workout sessions from JSON files in a specified folder,
// each file is expected to contain a single WorkoutSession object serialized as JSON
public class JsonWorkoutRepository(string folder) : IWorkoutRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public List<WorkoutSession> LoadAll()
    {
        List<WorkoutSession> sessions = [];
        if (!Directory.Exists(folder))
            return sessions;

        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try
            {
                string json = File.ReadAllText(file);
                var session = JsonSerializer.Deserialize<WorkoutSession>(json, Options);
                if (session != null)
                    sessions.Add(session);
            }
            catch
            {
                // skip malformed files (silently)
            }
        }
        sessions.Sort((a, b) => a.Date.CompareTo(b.Date));
        return sessions;
    }
}
