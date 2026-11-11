using FluentAssertions;
using SmartSelenium.Application;
using SmartSelenium.Domain;

namespace SmartSelenium.Application.Tests;

public class AutomationOrchestratorTests
{
    [Fact]
    public async Task RunAsync_ShouldDelegateToTheFormService()
    {
        var service = new StubFormAutomationService();
        var orchestrator = new AutomationOrchestrator(service);

        var request = new AutomationRequest(
            "https://app.cloudqa.io/home/AutomationPracticeForm",
            "John",
            "Male",
            "California");

        var result = await orchestrator.RunAsync(request);

        result.Success.Should().BeTrue();
        result.FieldResults.Should().HaveCount(3);
        service.Invocations.Should().Be(1);
    }

    private sealed class StubFormAutomationService : IFormAutomationService
    {
        public int Invocations { get; private set; }

        public Task<FieldInteractionResult> FillFirstNameAsync(string value, CancellationToken cancellationToken = default)
            => Task.FromResult(FieldInteractionResult.SuccessResult("FirstName", "Filled", value));

        public Task<FieldInteractionResult> SelectGenderAsync(string selection, CancellationToken cancellationToken = default)
            => Task.FromResult(FieldInteractionResult.SuccessResult("Gender", "Selected", selection));

        public Task<FieldInteractionResult> SelectStateAsync(string selection, CancellationToken cancellationToken = default)
            => Task.FromResult(FieldInteractionResult.SuccessResult("State", "Selected", selection));

        public Task<AutomationRunSummary> ExecuteAsync(AutomationRequest request, CancellationToken cancellationToken = default)
        {
            Invocations++;
            return Task.FromResult(new AutomationRunSummary(
                true,
                "Automation completed.",
                new[]
                {
                    FieldInteractionResult.SuccessResult("FirstName", "Filled", request.FirstName),
                    FieldInteractionResult.SuccessResult("Gender", "Selected", request.Gender),
                    FieldInteractionResult.SuccessResult("State", "Selected", request.State)
                },
                request.TargetUrl));
        }
    }
}
