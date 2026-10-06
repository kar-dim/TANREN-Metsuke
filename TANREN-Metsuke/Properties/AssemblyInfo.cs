using System.Runtime.CompilerServices;

// Let regression tests exercise the real HTTP handler without making it part of the public API.
[assembly: InternalsVisibleTo("TANREN-Metsuke.Tests")]
