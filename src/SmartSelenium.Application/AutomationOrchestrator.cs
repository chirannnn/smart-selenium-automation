namespace SmartSelenium.Application;

public sealed class AutomationOrchestrator : IAutomationOrchestrator
{
    private readonly IFormAutomationService _formAutomationService;

    public AutomationOrchestrator(IFormAutomationService formAutomationService)
    {
        _formAutomationService = formAutomationService;
    }

    public async Task<AutomationRunSummary> RunAsync(AutomationRequest request, CancellationToken cancellationToken = default)
    {
        var summary = await _formAutomationService.ExecuteAsync(request, cancellationToken);
        return summary;
    }
}
