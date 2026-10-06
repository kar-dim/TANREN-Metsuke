using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

// The same validation applies to files received over sync and files already on disk.
public static class WorkoutJson
{
    public const string CustomExercisesFilename = "custom_exercises.json";

    // Accept only plain dated filenames so an upload cannot supply a directory or relative path.
    public static bool IsWorkoutFilename(string? name) =>
        name is { Length: 15 } && name.EndsWith(".json", StringComparison.Ordinal) &&
        DateOnly.TryParseExact(name[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
        date != DateOnly.MinValue;

    public static bool IsSyncableFilename(string? name) =>
        IsWorkoutFilename(name) || name == CustomExercisesFilename;

    public static WorkoutSession ReadWorkout(string filename, string json)
    {
        if (!IsWorkoutFilename(filename))
            throw new InvalidDataException("Expected a dated workout filename.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || !HasProperty(root, "date") || !HasProperty(root, "entries"))
            throw new InvalidDataException("A workout must contain a date and entries.");
        var session = JsonSerializer.Deserialize<WorkoutSession>(json, JsonDefaults.CaseInsensitive)
            ?? throw new InvalidDataException("The workout cannot be null.");
        if (session.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) != filename[..10])
            throw new InvalidDataException("The workout date must match its filename.");
        if (session.Entries == null)
            throw new InvalidDataException("Workout entries cannot be null.");
        // Check required fields explicitly, a missing kg value must not become zero through model defaults.
        var entriesJson = root.EnumerateObject().Last(p => p.Name.Equals("entries", StringComparison.OrdinalIgnoreCase)).Value;
        foreach (var entryJson in entriesJson.EnumerateArray())
        {
            if (entryJson.ValueKind != JsonValueKind.Object || !HasProperty(entryJson, "exerciseId") || !HasProperty(entryJson, "sets"))
                throw new InvalidDataException("Every entry must contain exerciseId and sets.");
            var setsJson = entryJson.EnumerateObject().Last(p => p.Name.Equals("sets", StringComparison.OrdinalIgnoreCase)).Value;
            if (setsJson.ValueKind != JsonValueKind.Array)
                throw new InvalidDataException("Sets must be an array.");
            foreach (var setJson in setsJson.EnumerateArray())
                if (setJson.ValueKind != JsonValueKind.Object || !HasProperty(setJson, "reps") || !HasProperty(setJson, "kg"))
                    throw new InvalidDataException("Every set must contain reps and kg.");
        }
        foreach (var entry in session.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.ExerciseId) || entry.Sets == null)
                throw new InvalidDataException("Every entry needs an exercise ID and a set list.");
            foreach (var set in entry.Sets)
                if (set == null || set.Reps <= 0 || !double.IsFinite(set.Kg) || set.Kg < 0 || !double.IsFinite(set.Volume))
                    throw new InvalidDataException("Sets require positive reps and a finite, nonnegative weight.");
        }
        // Finite individual sets can still overflow when their volumes are added together.
        if (!double.IsFinite(session.TotalVolume))
            throw new InvalidDataException("Workout volume is too large.");
        // Empty entries from older files are harmless, but are not workouts by themselves.
        session.Entries.RemoveAll(e => e.Sets.Count == 0);
        return session;
    }

    public static List<ExerciseDefinition> ReadCustomExercises(string json)
    {
        var dtos = JsonSerializer.Deserialize<List<CustomExerciseDto>>(json, JsonDefaults.CaseInsensitive)
            ?? throw new InvalidDataException("The custom exercise list cannot be null.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var definitions = new List<ExerciseDefinition>();
        foreach (var dto in dtos)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Id) || string.IsNullOrWhiteSpace(dto.Name) || !ids.Add(dto.Id) ||
                dto.PrimaryMuscles == null || dto.SecondaryMuscles == null || dto.PrimaryMuscles.Count == 0)
                throw new InvalidDataException("Custom exercises require unique IDs, names and primary muscles.");
            // Remove repeated muscle names so one exercise cannot count the same muscle twice.
            var primary = dto.PrimaryMuscles.Select(ParseMuscle).Distinct().ToList();
            var secondary = dto.SecondaryMuscles.Select(ParseMuscle).Distinct().ToList();
            if (primary.Intersect(secondary).Any())
                throw new InvalidDataException("A muscle cannot be both primary and secondary.");
            definitions.Add(new ExerciseDefinition { Id = dto.Id, Name = dto.Name, PrimaryMuscles = primary, SecondaryMuscles = secondary });
        }
        return definitions;
    }

    public static void Validate(string filename, string json)
    {
        if (filename == CustomExercisesFilename)
            _ = ReadCustomExercises(json);
        else
            _ = ReadWorkout(filename, json);
    }

    private static MuscleGroup ParseMuscle(string name) =>
        Enum.TryParse<MuscleGroup>(name, true, out var muscle) && Enum.IsDefined(muscle)
            ? muscle : throw new InvalidDataException($"Unknown muscle: {name}");

    private static bool HasProperty(JsonElement element, string name) =>
        element.EnumerateObject().Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}

// Read muscle names as text before checking that each name belongs to the desktop muscle enum.
file sealed class CustomExerciseDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> PrimaryMuscles { get; set; } = [];
    public List<string> SecondaryMuscles { get; set; } = [];
}
