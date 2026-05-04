using Microsoft.Playwright;

namespace WysiMd.Blazor.IntegrationTests;

/// <summary>
/// NUnit SetUpFixture that installs Playwright browsers once per test run.
/// The sample app URL is configured via the WYSIMD_BASE_URL environment variable
/// (default: http://localhost:5100). Start the sample before running tests:
///   dotnet run --project samples/WysiMd.Blazor.Sample --urls http://localhost:5100
/// </summary>
[SetUpFixture]
public class PlaywrightFixture
{
    public static string BaseUrl { get; private set; } =
        Environment.GetEnvironmentVariable("WYSIMD_BASE_URL") ?? "http://localhost:5100";

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        // Only install if the browser executable is missing; skip --with-deps on repeat runs.
        var exitCode = IsBrowserInstalled()
            ? 0
            : Microsoft.Playwright.Program.Main(["install", "chromium"]);

        if (exitCode != 0)
            throw new InvalidOperationException($"Playwright install failed with exit code {exitCode}");

        await Task.CompletedTask;
    }

    private static bool IsBrowserInstalled()
    {
        // Playwright stores browsers under %LOCALAPPDATA%\ms-playwright on Windows,
        // ~/.cache/ms-playwright on Linux/macOS.
        var home = Environment.GetEnvironmentVariable("LOCALAPPDATA")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
        var browsersRoot = Path.Combine(home, "ms-playwright");
        return Directory.Exists(browsersRoot)
            && Directory.EnumerateFiles(browsersRoot, "chrome.exe", SearchOption.AllDirectories).Any();
    }
}
