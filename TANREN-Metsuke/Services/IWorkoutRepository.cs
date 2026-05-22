using System.Collections.Generic;
using TANREN_Metsuke.Models;

namespace TANREN_Metsuke.Services;

public interface IWorkoutRepository
{
    List<WorkoutSession> LoadAll();
}
