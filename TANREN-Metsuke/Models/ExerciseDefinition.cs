using System.Collections.Generic;

namespace TANREN_Metsuke.Models;

public class ExerciseDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<MuscleGroup> PrimaryMuscles { get; set; } = [];
    public List<MuscleGroup> SecondaryMuscles { get; set; } = [];
}
