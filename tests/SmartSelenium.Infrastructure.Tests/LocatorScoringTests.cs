using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SmartSelenium.Domain;
using SmartSelenium.Infrastructure;

namespace SmartSelenium.Infrastructure.Tests;

public class LocatorScoringTests
{
    [Fact]
    public void DefaultCandidateScorer_ShouldRecognizeFirstNameCandidates()
    {
        var scorer = new DefaultCandidateScorer();
        var field = new FieldDefinition(
            "firstName",
            "First Name",
            FieldType.Text,
            new[] { "first name", "given name" },
            Placeholder: "First Name",
            AriaLabel: "First Name",
            Keywords: new[] { "first", "name" });

        var candidate = new LocatorCandidate(
            "firstName",
            "firstName",
            "input",
            "text",
            null,
            "First Name",
            "First Name",
            "First Name",
            null,
            "input",
            null,
            null,
            true,
            true,
            new Dictionary<string, string>());

        var score = scorer.Score(field, candidate);

        score.Confidence.Should().BeGreaterThanOrEqualTo(0.75d);
        score.Evidence.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SeleniumLocatorEngine_ShouldChooseHighestConfidenceCandidate()
    {
        var scorer = new DefaultCandidateScorer();
        var engine = new SeleniumLocatorEngine(scorer, NullLogger<SeleniumLocatorEngine>.Instance);
        var field = new FieldDefinition(
            "state",
            "State",
            FieldType.Select,
            new[] { "state", "province" },
            AriaLabel: "State",
            Keywords: new[] { "state" });

        var candidates = new List<LocatorCandidate>
        {
            new("country", "country", "select", "text", null, "Country", "Country", "Country", null, null, null, null, true, true, new Dictionary<string, string>()),
            new("state", "state", "select", "text", null, "State", "State", "State", null, null, null, null, true, true, new Dictionary<string, string>())
        };

        var selected = await engine.SelectBestCandidateAsync(field, candidates, 0.75d);

        selected.Id.Should().Be("state");
    }
}
