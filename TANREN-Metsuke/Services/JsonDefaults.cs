using System.Text.Json;

namespace TANREN_Metsuke.Services;

// Shared JSON options where all reads are case-insensitive, we allow property casing differences
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };
}
