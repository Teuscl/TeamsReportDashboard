using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyXunit;

namespace TeamsReportDashboard.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Disable config file watching — prevents inotify exhaustion on Linux when running many tests
        Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");

        // Store Verify snapshots in Snapshots/ folder relative to the test project
        Verifier.UseProjectRelativeDirectory("Snapshots");

        // In CI, fail instead of opening a diff tool
        if (Environment.GetEnvironmentVariable("CI") == "true")
            DiffRunner.Disabled = true;

        // Scrub unstable members globally so snapshots stay stable across runs
        VerifierSettings.ScrubMembersWithType<Guid>();
        VerifierSettings.ScrubMembersWithType<DateTime>();
        VerifierSettings.ScrubMembersWithType<DateTimeOffset>();
    }
}
