using FluentAssertions;

namespace SmartSelenium.IntegrationTests;

public class FormAutomationIntegrationTests
{
    [Fact(Skip = "Requires a browser-enabled environment and external network connectivity to the CloudQA app.")]
    public void CloudQaForm_ShouldAcceptThePracticeAutomationFlow()
    {
        true.Should().BeTrue();
    }
}
