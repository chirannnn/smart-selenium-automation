using FluentAssertions;
using SmartSelenium.Domain;

namespace SmartSelenium.Domain.Tests;

public class FieldDefinitionTests
{
    [Fact]
    public void FieldDefinition_ShouldNormalizeAlternativeLabelsAndKeywords()
    {
        var field = new FieldDefinition(
            "firstName",
            "First Name",
            FieldType.Text,
            new[] { "first name", "First Name", "given name" },
            "First Name",
            "First Name",
            new[] { "first", "name" });

        field.AlternativeLabels.Should().BeEquivalentTo(new[] { "first name", "given name" });
        field.Keywords.Should().BeEquivalentTo(new[] { "first", "name" });
        field.NormalizedName.Should().Be("firstName");
    }

    [Fact]
    public void FieldInteractionResult_ShouldRepresentSuccessAndFailure()
    {
        var success = FieldInteractionResult.SuccessResult("FirstName", "Applied", "John");
        var failure = FieldInteractionResult.FailureResult("Gender", "Missing field");

        success.Success.Should().BeTrue();
        success.Value.Should().Be("John");
        failure.Success.Should().BeFalse();
        failure.Message.Should().Be("Missing field");
    }
}
