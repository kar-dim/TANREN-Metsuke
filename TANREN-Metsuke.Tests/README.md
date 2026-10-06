# Regression checks

```powershell
dotnet build TANREN-Metsuke.sln
dotnet test TANREN-Metsuke.sln
```

Tests use synthetic workout directories beneath the test output, then remove those directories. UI checks use Avalonia's headless platform. No test reads or modifies `%APPDATA%/TANREN/workouts`.

The `TlsIntegration` category creates a temporary TLS certificate and uses localhost HTTPS. On Windows this requires access to the Windows credential/key services. Run these tests in a normal Windows terminal:

```powershell
dotnet test TANREN-Metsuke.sln --filter Category=TlsIntegration
```

For a restricted environment, the remaining tests exercise the production HTTP parser, authentication, endpoint handlers, keep-alive loop, transactions, crash recovery, analytics and UI:

```powershell
dotnet test TANREN-Metsuke.sln --filter Category!=TlsIntegration
```

If Avalonia's build-time telemetry task cannot write outside the workspace, append `-p:UsedAvaloniaProducts=` to the local build/test command. This command-line property skips the telemetry target: it is not set in the project configuration and does not change app behavior.
