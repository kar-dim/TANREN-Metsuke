using System;
using System.IO;

namespace TANREN_Metsuke.Services;

internal static class AppPaths
{
    internal static readonly string BaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TANREN");
}
