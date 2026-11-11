namespace SmartSelenium.Domain;

public enum FieldType
{
    Text,
    RadioGroup,
    Select,
    Unknown
}

public enum AutomationStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed
}

public sealed record FieldDefinition(
    string Name,
    string Label,
    FieldType Type,
    IEnumerable<string>? AlternativeLabels = null,
    string? Placeholder = null,
    string? AriaLabel = null,
    IEnumerable<string>? Keywords = null)
{
    public IEnumerable<string> AlternativeLabels { get; } =
        (AlternativeLabels ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public IEnumerable<string> Keywords { get; } =
        (Keywords ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    public string NormalizedName => Name.Trim();
}

public sealed record LocatorCandidate(
    string? Id,
    string? Name,
    string? TagName,
    string? Type,
    string? Value,
    string? Placeholder,
    string? AriaLabel,
    string? LabelText,
    string? Text,
    string? CssSelector,
    string? XPath,
    string? Path,
    bool IsVisible,
    bool IsEnabled,
    IReadOnlyDictionary<string, string> Attributes)
{
    public bool HasValue => !string.IsNullOrWhiteSpace(Id)
        || !string.IsNullOrWhiteSpace(Name)
        || !string.IsNullOrWhiteSpace(TagName)
        || !string.IsNullOrWhiteSpace(Text)
        || !string.IsNullOrWhiteSpace(CssSelector)
        || !string.IsNullOrWhiteSpace(XPath);
}

public sealed record CandidateScore(
    string FieldName,
    double Score,
    double Confidence,
    string Reason,
    IReadOnlyCollection<string> Evidence)
{
    public bool IsSelected => Confidence >= 0.75d;
}

public sealed record FieldInteractionResult(
    string FieldName,
    bool Success,
    string Message,
    string? Value = null,
    AutomationStatus Status = AutomationStatus.Succeeded)
{
    public static FieldInteractionResult SuccessResult(string fieldName, string message, string? value = null) =>
        new(fieldName, true, message, value, AutomationStatus.Succeeded);

    public static FieldInteractionResult FailureResult(string fieldName, string message) =>
        new(fieldName, false, message, null, AutomationStatus.Failed);
}

public static class DomainErrors
{
    public const string ElementNotFound = "No suitable element was found for the requested field.";
    public const string AmbiguousElement = "Multiple candidate elements had similar confidence and required disambiguation.";
    public const string InvalidFieldType = "The matched element does not support the requested interaction.";
}

public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}

public sealed class ElementNotFoundException : DomainException
{
    public ElementNotFoundException(string message = DomainErrors.ElementNotFound) : base(message)
    {
    }
}

public sealed class AmbiguousElementException : DomainException
{
    public AmbiguousElementException(string message = DomainErrors.AmbiguousElement) : base(message)
    {
    }
}

public sealed class InvalidFieldTypeException : DomainException
{
    public InvalidFieldTypeException(string message = DomainErrors.InvalidFieldType) : base(message)
    {
    }
}
