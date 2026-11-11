using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SmartSelenium.Application;
using SmartSelenium.Domain;

namespace SmartSelenium.Infrastructure;

public sealed class SeleniumBrowserFactory : IBrowserFactory
{
    private readonly ILogger<SeleniumBrowserFactory> _logger;

    public SeleniumBrowserFactory(ILogger<SeleniumBrowserFactory> logger)
    {
        _logger = logger;
    }

    public Task<IBrowserSession> CreateAsync(BrowserSettings settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new ChromeOptions();

        if (settings.Headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--window-size=1440,1200");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        var driver = new ChromeDriver(options);
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(settings.PageLoadTimeoutSeconds);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(Math.Max(1, settings.ElementWaitSeconds / 2));

        _logger.LogInformation("Created Selenium Chrome session. Headless={Headless}, pageLoadTimeout={PageLoadTimeoutSeconds}s", settings.Headless, settings.PageLoadTimeoutSeconds);

        return Task.FromResult<IBrowserSession>(new SeleniumBrowserSession(driver, _logger));
    }
}

public sealed class SeleniumBrowserSession : IBrowserSession
{
    private readonly IWebDriver _driver;
    private readonly ILogger _logger;

    public IWebDriver Driver => _driver;

    public SeleniumBrowserSession(IWebDriver driver, ILogger logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public Task NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _driver.Navigate().GoToUrl(url);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<LocatorCandidate>> FindCandidatesAsync(string selector, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var elements = _driver.FindElements(By.CssSelector(selector));
        var candidates = new List<LocatorCandidate>();

        foreach (var element in elements)
        {
            var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var attributeName in new[] { "id", "name", "type", "value", "placeholder", "aria-label", "for", "class", "role" })
            {
                var value = element.GetAttribute(attributeName);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    attributes[attributeName] = value;
                }
            }

            candidates.Add(new LocatorCandidate(
                Id: element.GetAttribute("id"),
                Name: element.GetAttribute("name"),
                TagName: element.TagName,
                Type: element.GetAttribute("type"),
                Value: element.GetAttribute("value"),
                Placeholder: element.GetAttribute("placeholder"),
                AriaLabel: element.GetAttribute("aria-label"),
                LabelText: element.GetAttribute("aria-label") ?? element.GetAttribute("value"),
                Text: element.Text,
                CssSelector: selector,
                XPath: null,
                Path: null,
                IsVisible: element.Displayed,
                IsEnabled: element.Enabled,
                Attributes: new ReadOnlyDictionary<string, string>(attributes)));
        }

        return Task.FromResult<IReadOnlyCollection<LocatorCandidate>>(candidates);
    }

    public Task<string?> GetCurrentUrlAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(_driver.Url);
    }

    public Task<string> GetPageSourceAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_driver.PageSource);
    }

    public Task SetTextValueAsync(string selector, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var element = _driver.FindElement(By.CssSelector(selector));
        element.Clear();
        element.SendKeys(value);
        return Task.CompletedTask;
    }

    public Task SelectOptionAsync(string selector, string optionValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var element = _driver.FindElement(By.CssSelector(selector));
        var select = new SelectElement(element);

        try
        {
            select.SelectByValue(optionValue);
        }
        catch (NoSuchElementException)
        {
            try
            {
                select.SelectByText(optionValue);
            }
            catch (NoSuchElementException)
            {
                var option = element.FindElements(By.TagName("option")).FirstOrDefault(candidate =>
                    string.Equals(candidate.Text, optionValue, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidate.GetAttribute("value"), optionValue, StringComparison.OrdinalIgnoreCase));

                if (option is null)
                {
                    throw;
                }

                option.Click();
            }
        }

        return Task.CompletedTask;
    }

    public Task SelectRadioAsync(string selector, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var radios = _driver.FindElements(By.CssSelector(selector));
        var matched = radios.FirstOrDefault(element =>
            string.Equals(element.GetAttribute("value"), value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(element.GetAttribute("id"), value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(element.GetAttribute("name"), value, StringComparison.OrdinalIgnoreCase) ||
            element.GetAttribute("aria-label") is string ariaLabel && ariaLabel.Contains(value, StringComparison.OrdinalIgnoreCase));

        if (matched is null && radios.Count == 1)
        {
            radios[0].Click();
            return Task.CompletedTask;
        }

        if (matched is null)
        {
            throw new NoSuchElementException($"No radio button matching '{value}' was found using selector '{selector}'.");
        }

        matched.Click();
        return Task.CompletedTask;
    }

    public Task<bool> VerifyValueAsync(string selector, string expectedValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var elements = _driver.FindElements(By.CssSelector(selector));
        if (elements.Count == 0)
        {
            return Task.FromResult(false);
        }

        var element = elements.Count == 1 ? elements[0] : elements.FirstOrDefault(candidate => candidate.Selected || candidate.GetAttribute("value") == expectedValue || candidate.GetAttribute("aria-checked") == "true") ?? elements[0];

        if (string.Equals(element.TagName, "select", StringComparison.OrdinalIgnoreCase))
        {
            var select = new SelectElement(element);
            var selected = select.SelectedOption;
            var optionValue = selected?.GetAttribute("value") ?? selected?.Text ?? element.GetAttribute("value");
            return Task.FromResult(string.Equals(optionValue, expectedValue, StringComparison.OrdinalIgnoreCase));
        }

        var elementValue = element.GetAttribute("value") ?? element.Text;
        return Task.FromResult(string.Equals(elementValue, expectedValue, StringComparison.OrdinalIgnoreCase) || element.Selected);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            _driver.Quit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to quit Selenium driver cleanly.");
        }

        await Task.CompletedTask;
    }
}

public sealed class DefaultCandidateScorer : ICandidateScorer
{
    public CandidateScore Score(FieldDefinition field, LocatorCandidate candidate)
    {
        double total = 0d;
        var evidence = new List<string>();

        if (field.Type == FieldType.Text)
        {
            if (candidate.TagName is "input" or "textarea")
            {
                total += 0.22d;
                evidence.Add("Matches text input semantics.");
            }
            if (candidate.Type is "text" or "email" or "search")
            {
                total += 0.18d;
                evidence.Add("Input type is compatible with text entry.");
            }
        }

        if (field.Type == FieldType.RadioGroup)
        {
            if (candidate.Type == "radio")
            {
                total += 0.28d;
                evidence.Add("Candidate is a radio button.");
            }
        }

        if (field.Type == FieldType.Select)
        {
            if (candidate.TagName == "select")
            {
                total += 0.32d;
                evidence.Add("Candidate is a select element.");
            }
        }

        var fieldTokens = GetNormalizedTokens(
            field.Name,
            field.Label,
            string.Join(' ', field.AlternativeLabels),
            string.Join(' ', field.Keywords),
            field.Placeholder,
            field.AriaLabel);
        var candidateText = string.Join(' ',
            candidate.Id ?? string.Empty,
            candidate.Name ?? string.Empty,
            candidate.Value ?? string.Empty,
            candidate.Placeholder ?? string.Empty,
            candidate.AriaLabel ?? string.Empty,
            candidate.LabelText ?? string.Empty,
            candidate.Text ?? string.Empty,
            candidate.Type ?? string.Empty);

        var matchTokens = GetNormalizedTokens(candidateText);
        var overlapCount = 0;
        foreach (var token in fieldTokens)
        {
            if (matchTokens.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                overlapCount++;
            }
        }

        if (overlapCount > 0)
        {
            total += Math.Min(overlapCount * 0.15d, 0.45d);
            evidence.Add($"Shared semantic tokens found ({overlapCount}).");

            if (candidate.Id is not null || candidate.Name is not null || candidate.Placeholder is not null || candidate.AriaLabel is not null)
            {
                total += 0.10d;
                evidence.Add("Strong semantic match on the field identity attributes.");
            }
        }

        if (field.Placeholder is not null && !string.IsNullOrWhiteSpace(candidate.Placeholder) &&
            candidate.Placeholder.Contains(field.Placeholder, StringComparison.OrdinalIgnoreCase))
        {
            total += 0.15d;
            evidence.Add("Placeholder matches the field label.");
        }

        if (field.AriaLabel is not null && !string.IsNullOrWhiteSpace(candidate.AriaLabel) &&
            candidate.AriaLabel.Contains(field.AriaLabel, StringComparison.OrdinalIgnoreCase))
        {
            total += 0.12d;
            evidence.Add("ARIA label matches the field label.");
        }

        if (!string.IsNullOrWhiteSpace(candidate.Id) && field.Name.Contains("first", StringComparison.OrdinalIgnoreCase) && candidate.Id.Contains("first", StringComparison.OrdinalIgnoreCase))
        {
            total += 0.12d;
            evidence.Add("ID contains a first-name label.");
        }

        if (candidate.IsVisible)
        {
            total += 0.08d;
            evidence.Add("Element is visible.");
        }

        if (candidate.IsEnabled)
        {
            total += 0.05d;
            evidence.Add("Element is enabled.");
        }

        total = Math.Clamp(total, 0d, 1d);

        var confidence = Math.Round(total, 4);
        return new CandidateScore(field.Name, total, confidence, "Candidate scored based on semantic, type, and accessibility signals.", evidence.Distinct().ToArray());
    }

    private static HashSet<string> GetNormalizedTokens(params string?[] values)
    {
        var output = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var segments = value
                .Replace("-", " ")
                .Replace("_", " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var segment in segments)
            {
                var normalized = segment.Trim();
                if (!string.IsNullOrWhiteSpace(normalized) && normalized.Length > 1)
                {
                    output.Add(normalized.ToLowerInvariant());
                }
            }
        }

        return output;
    }
}

public sealed class SeleniumLocatorEngine : IElementLocator
{
    private readonly ICandidateScorer _scorer;
    private readonly ILogger<SeleniumLocatorEngine> _logger;

    public SeleniumLocatorEngine(ICandidateScorer scorer, ILogger<SeleniumLocatorEngine> logger)
    {
        _scorer = scorer;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<LocatorCandidate>> DiscoverCandidatesAsync(FieldDefinition field, IBrowserSession session, CancellationToken cancellationToken = default)
    {
        var sources = await session.FindCandidatesAsync("input, select, textarea", cancellationToken);

        var discovered = new List<LocatorCandidate>();

        foreach (var candidate in sources)
        {
            if (candidate.TagName is null)
            {
                continue;
            }

            var score = _scorer.Score(field, candidate);
            if (score.Confidence > 0.1d || candidate.TagName.Equals("select", StringComparison.OrdinalIgnoreCase) || candidate.TagName.Equals("input", StringComparison.OrdinalIgnoreCase))
            {
                discovered.Add(candidate);
            }
        }

        return discovered;
    }

    public Task<LocatorCandidate> SelectBestCandidateAsync(FieldDefinition field, IReadOnlyCollection<LocatorCandidate> candidates, double minimumConfidence, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (candidates.Count == 0)
        {
            throw new ElementNotFoundException(DomainErrors.ElementNotFound);
        }

        var ranked = candidates
            .Select(candidate => new { candidate, score = _scorer.Score(field, candidate) })
            .OrderByDescending(x => x.score.Confidence)
            .ToList();

        var best = ranked[0];
        if (best.score.Confidence < minimumConfidence)
        {
            throw new ElementNotFoundException($"Best match score {best.score.Confidence:0.00} below the configured threshold {minimumConfidence:0.00}.");
        }

        var sameNameGroup = ranked
            .GroupBy(candidate => candidate.candidate.Name ?? candidate.candidate.Id ?? candidate.candidate.Attributes.GetValueOrDefault("name"))
            .Any(group => group.Count() > 1 && Math.Abs(group.Max(x => x.score.Confidence) - group.Min(x => x.score.Confidence)) < 0.08d);

        var runnerUp = ranked.Count > 1 ? ranked[1] : null;
        if (!sameNameGroup && runnerUp is not null && Math.Abs(best.score.Confidence - runnerUp.score.Confidence) < 0.08d && best.score.Confidence >= minimumConfidence)
        {
            var message = $"Ambiguous selection for '{field.Name}': {best.score.Confidence:0.00} vs {runnerUp.score.Confidence:0.00} and within 0.08 threshold.";
            _logger.LogWarning(message);
            throw new AmbiguousElementException(message);
        }

        _logger.LogInformation("Selected candidate for '{FieldName}' with confidence {Confidence:0.00}. Reasons: {Reasons}", field.Name, best.score.Confidence, string.Join("; ", best.score.Evidence));
        return Task.FromResult(best.candidate);
    }
}

public sealed class SeleniumFormAutomationService : IFormAutomationService
{
    private readonly IBrowserFactory _browserFactory;
    private readonly IElementLocator _locator;
    private readonly ICandidateScorer _candidateScorer;
    private readonly ILogger<SeleniumFormAutomationService> _logger;
    private IBrowserSession? _session;

    public SeleniumFormAutomationService(IBrowserFactory browserFactory, IElementLocator locator, ICandidateScorer candidateScorer, ILogger<SeleniumFormAutomationService> logger)
    {
        _browserFactory = browserFactory;
        _locator = locator;
        _candidateScorer = candidateScorer;
        _logger = logger;
    }

    public async Task<FieldInteractionResult> FillFirstNameAsync(string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return FieldInteractionResult.FailureResult("FirstName", "First name value is required.");
        }

        var field = new FieldDefinition("firstName", "First Name", FieldType.Text,
            new[] { "first name", "given name", "name" },
            Placeholder: "First Name",
            AriaLabel: "First Name",
            Keywords: new[] { "first", "name" });

        return await ExecuteTextFieldAsync(field, value, cancellationToken);
    }

    public async Task<FieldInteractionResult> SelectGenderAsync(string selection, CancellationToken cancellationToken = default)
    {
        var selectedValue = selection?.Trim();
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            return FieldInteractionResult.FailureResult("Gender", "Gender selection is required.");
        }

        var field = new FieldDefinition("gender", "Gender", FieldType.RadioGroup,
            new[] { "gender", "sex" },
            AriaLabel: "Gender",
            Keywords: new[] { "gender", "male", "female", "other" });

        var session = await EnsureSessionAsync(cancellationToken);
        var candidates = await _locator.DiscoverCandidatesAsync(field, session, cancellationToken);
        var desiredCandidate = candidates
            .OrderByDescending(candidate => _candidateScorer.Score(field, candidate).Confidence)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Value, selectedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Id, selectedValue, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Name, selectedValue, StringComparison.OrdinalIgnoreCase))
            ?? await _locator.SelectBestCandidateAsync(field, candidates, 0.60d, cancellationToken);

        var selector = "input[name='gender'], input[type='radio']";
        await session.SelectRadioAsync(selector, selectedValue);
        var verified = await session.VerifyValueAsync(selector, selectedValue);

        if (!verified)
        {
            return FieldInteractionResult.FailureResult("Gender", $"Gender selection '{selectedValue}' could not be verified.");
        }

        return FieldInteractionResult.SuccessResult("Gender", $"Gender '{selectedValue}' selected and verified.", selectedValue);
    }

    public async Task<FieldInteractionResult> SelectStateAsync(string selection, CancellationToken cancellationToken = default)
    {
        var selectedValue = selection?.Trim();
        if (string.IsNullOrWhiteSpace(selectedValue))
        {
            return FieldInteractionResult.FailureResult("State", "State selection is required.");
        }

        var field = new FieldDefinition("state", "State", FieldType.Select,
            new[] { "state", "province", "region" },
            AriaLabel: "State",
            Keywords: new[] { "state", "province" });

        var session = await EnsureSessionAsync(cancellationToken);
        var candidates = await _locator.DiscoverCandidatesAsync(field, session, cancellationToken);
        var chosen = await _locator.SelectBestCandidateAsync(field, candidates, 0.65d, cancellationToken);
        var selector = BuildSelector(chosen);

        if (!string.Equals(chosen.TagName, "select", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidFieldTypeException("The matched state element is not a select element.");
        }

        await session.SelectOptionAsync(selector, selectedValue);
        var verified = await session.VerifyValueAsync(selector, selectedValue);

        if (!verified)
        {
            return FieldInteractionResult.FailureResult("State", $"State selection '{selectedValue}' could not be verified.");
        }

        return FieldInteractionResult.SuccessResult("State", $"State '{selectedValue}' selected and verified.", selectedValue);
    }

    public async Task<AutomationRunSummary> ExecuteAsync(AutomationRequest request, CancellationToken cancellationToken = default)
    {
        var settings = request.Browser ?? new BrowserSettings();
        _session = await _browserFactory.CreateAsync(settings, cancellationToken);
        await _session.NavigateAsync(request.TargetUrl, cancellationToken);

        try
        {
            var results = new List<FieldInteractionResult>
            {
                await FillFirstNameAsync(request.FirstName, cancellationToken),
                await SelectGenderAsync(request.Gender, cancellationToken),
                await SelectStateAsync(request.State, cancellationToken)
            };

            var success = results.All(x => x.Success);
            return new AutomationRunSummary(success,
                success ? "Automation completed successfully." : "One or more automation steps failed.",
                results,
                request.TargetUrl);
        }
        finally
        {
            if (_session is not null)
            {
                await _session.DisposeAsync();
            }
        }
    }

    private async Task<IBrowserSession> EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_session is null)
        {
            _session = await _browserFactory.CreateAsync(new BrowserSettings(), cancellationToken);
        }

        return _session;
    }

    private async Task<FieldInteractionResult> ExecuteTextFieldAsync(FieldDefinition field, string value, CancellationToken cancellationToken)
    {
        var session = await EnsureSessionAsync(cancellationToken);
        var candidates = await _locator.DiscoverCandidatesAsync(field, session, cancellationToken);
        var chosen = await _locator.SelectBestCandidateAsync(field, candidates, 0.75d, cancellationToken);

        if (!string.Equals(chosen.TagName, "input", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(chosen.TagName, "textarea", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidFieldTypeException("The matched element was not a text input or textarea.");
        }

        var selector = BuildSelector(chosen);
        await session.SetTextValueAsync(selector, value, cancellationToken);
        var verified = await session.VerifyValueAsync(selector, value, cancellationToken);

        if (!verified)
        {
            return FieldInteractionResult.FailureResult(field.Name, $"'{field.Label}' value '{value}' could not be verified.");
        }

        return FieldInteractionResult.SuccessResult(field.Name, $"'{field.Label}' value '{value}' applied and verified.", value);
    }

    private static string BuildSelector(LocatorCandidate candidate)
    {
        if (!string.IsNullOrWhiteSpace(candidate.Id))
        {
            return $"#{EscapeSelector(candidate.Id)}";
        }

        if (!string.IsNullOrWhiteSpace(candidate.Name))
        {
            return $"[name='{EscapeSelector(candidate.Name)}']";
        }

        if (!string.IsNullOrWhiteSpace(candidate.TagName))
        {
            return candidate.TagName;
        }

        return "input";
    }

    private static string EscapeSelector(string value)
    {
        return value.Replace("\"", "\\\"");
    }
}
