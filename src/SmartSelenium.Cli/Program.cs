using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartSelenium.Application;
using SmartSelenium.Infrastructure;

namespace SmartSelenium.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var argumentMap = ParseArguments(args);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var browserSettings = configuration.GetSection("Browser").Get<BrowserSettings>() ?? new BrowserSettings();
        var targetUrl = GetValue(configuration, "TargetUrl", GetArgument(argumentMap, "url", "https://app.cloudqa.io/home/AutomationPracticeForm"));
        var firstName = GetValue(configuration, "FirstName", GetArgument(argumentMap, "first-name", "John"));
        var gender = GetValue(configuration, "Gender", GetArgument(argumentMap, "gender", "Male"));
        var state = GetValue(configuration, "State", GetArgument(argumentMap, "state", "Argentina"));

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        services.AddSingleton<IBrowserFactory, SeleniumBrowserFactory>();
        services.AddSingleton<ICandidateScorer, DefaultCandidateScorer>();
        services.AddSingleton<IElementLocator, SeleniumLocatorEngine>();
        services.AddSingleton<IFormAutomationService, SeleniumFormAutomationService>();
        services.AddSingleton<IAutomationOrchestrator, AutomationOrchestrator>();

        using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IAutomationOrchestrator>();

        var request = new AutomationRequest(
            TargetUrl: targetUrl,
            FirstName: firstName,
            Gender: gender,
            State: state,
            Browser: browserSettings with
            {
                BrowserType = GetValue(configuration, "Browser:BrowserType", browserSettings.BrowserType),
                Headless = bool.TryParse(GetValue(configuration, "Browser:Headless", browserSettings.Headless.ToString()), out var headless) && headless,
                PageLoadTimeoutSeconds = int.TryParse(GetValue(configuration, "Browser:PageLoadTimeoutSeconds", browserSettings.PageLoadTimeoutSeconds.ToString()), out var loadTimeout) ? loadTimeout : browserSettings.PageLoadTimeoutSeconds,
                ElementWaitSeconds = int.TryParse(GetValue(configuration, "Browser:ElementWaitSeconds", browserSettings.ElementWaitSeconds.ToString()), out var waitSeconds) ? waitSeconds : browserSettings.ElementWaitSeconds,
                ConfidenceThreshold = double.TryParse(GetValue(configuration, "Browser:ConfidenceThreshold", browserSettings.ConfidenceThreshold.ToString()), out var threshold) ? threshold : browserSettings.ConfidenceThreshold
            });

        var result = await orchestrator.RunAsync(request);

        Console.WriteLine($"Result: {result.Message}");
        foreach (var fieldResult in result.FieldResults)
        {
            Console.WriteLine($"- {fieldResult.FieldName}: {(fieldResult.Success ? "SUCCESS" : "FAILED")} -> {fieldResult.Message}");
        }

        return result.Success ? 0 : 1;
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var key = arg[2..];
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var normalizedKey = key.Trim();
            var next = index + 1 < args.Length ? args[index + 1] : null;
            if (next is not null && !next.StartsWith("--", StringComparison.Ordinal))
            {
                map[normalizedKey] = next;
                index++;
            }
            else
            {
                map[normalizedKey] = "true";
            }
        }

        return map;
    }

    private static string GetArgument(Dictionary<string, string> values, string key, string fallback)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string GetValue(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
