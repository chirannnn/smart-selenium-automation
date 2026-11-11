using SmartSelenium.Domain;

namespace SmartSelenium.Application;

public sealed record BrowserSettings(
    string BrowserType = "Chrome",
    bool Headless = true,
    int PageLoadTimeoutSeconds = 30,
    int ElementWaitSeconds = 15,
    double ConfidenceThreshold = 0.75d);

public sealed record AutomationRequest(
    string TargetUrl,
    string FirstName,
    string Gender,
    string State,
    BrowserSettings? Browser = null);

public sealed record AutomationRunSummary(
    bool Success,
    string Message,
    IReadOnlyCollection<FieldInteractionResult> FieldResults,
    string? Url = null)
{
    public static AutomationRunSummary FromResults(string message, IEnumerable<FieldInteractionResult> results, string? url = null) =>
        new(results.All(x => x.Success), message, results.ToArray(), url);
}

public interface IAutomationOrchestrator
{
    Task<AutomationRunSummary> RunAsync(AutomationRequest request, CancellationToken cancellationToken = default);
}

public interface IBrowserFactory
{
    Task<IBrowserSession> CreateAsync(BrowserSettings settings, CancellationToken cancellationToken = default);
}

public interface IBrowserSession : IAsyncDisposable
{
    Task NavigateAsync(string url, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LocatorCandidate>> FindCandidatesAsync(string selector, CancellationToken cancellationToken = default);
    Task<string?> GetCurrentUrlAsync(CancellationToken cancellationToken = default);
    Task<string> GetPageSourceAsync(CancellationToken cancellationToken = default);
    Task SetTextValueAsync(string selector, string value, CancellationToken cancellationToken = default);
    Task SelectOptionAsync(string selector, string optionValue, CancellationToken cancellationToken = default);
    Task SelectRadioAsync(string selector, string value, CancellationToken cancellationToken = default);
    Task<bool> VerifyValueAsync(string selector, string expectedValue, CancellationToken cancellationToken = default);
}

public interface IElementLocator
{
    Task<IReadOnlyCollection<LocatorCandidate>> DiscoverCandidatesAsync(FieldDefinition field, IBrowserSession session, CancellationToken cancellationToken = default);
    Task<LocatorCandidate> SelectBestCandidateAsync(FieldDefinition field, IReadOnlyCollection<LocatorCandidate> candidates, double minimumConfidence, CancellationToken cancellationToken = default);
}

public interface IFormAutomationService
{
    Task<FieldInteractionResult> FillFirstNameAsync(string value, CancellationToken cancellationToken = default);
    Task<FieldInteractionResult> SelectGenderAsync(string selection, CancellationToken cancellationToken = default);
    Task<FieldInteractionResult> SelectStateAsync(string selection, CancellationToken cancellationToken = default);
    Task<AutomationRunSummary> ExecuteAsync(AutomationRequest request, CancellationToken cancellationToken = default);
}

public interface ILocatorStrategy
{
    Task<IReadOnlyCollection<LocatorCandidate>> DiscoverAsync(FieldDefinition field, IBrowserSession session, CancellationToken cancellationToken = default);
}

public interface ICandidateScorer
{
    CandidateScore Score(FieldDefinition field, LocatorCandidate candidate);
}
