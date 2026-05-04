using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WysiMd.Blazor.IntegrationTests;

/// <summary>
/// MSTest assembly initialiser that installs Playwright browsers once per test run.
/// The sample app URL is configured via the WYSIMD_BASE_URL environment variable
/// (default: http://localhost:5100). Start the sample before running tests:
///   dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
/// </summary>
[TestClass]
public class PlaywrightFixture
{
    public static string BaseUrl { get; private set; } =
        Environment.GetEnvironmentVariable("WYSIMD_BASE_URL") ?? "http://localhost:5100";

    [AssemblyInitialize]
    public static void GlobalSetup(TestContext _)
    {
        // --with-deps is for Linux CI only; on Windows it hangs waiting for system-level installs.
        // Skip entirely if chromium is already present.
        var browsersPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ms-playwright");
        bool chromiumInstalled = Directory.Exists(browsersPath) &&
            Directory.GetDirectories(browsersPath, "chromium*").Length > 0;

        if (!chromiumInstalled)
        {
            var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
            if (exitCode != 0)
                throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}");
        }
    }
}
