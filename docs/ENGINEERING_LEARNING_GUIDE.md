# SmartSelenium: Engineering Learning Guide

This guide explains the engineering ideas behind the repository and distinguishes
general Selenium/.NET practice from features that are actually present here. It
is a learning resource, not a claim that the current implementation handles
every production scenario.

## How to read the repository

The principal implementation files are:

- [`src/SmartSelenium.Cli/Program.cs`](../src/SmartSelenium.Cli/Program.cs)
- [`src/SmartSelenium.Application/AutomationModels.cs`](../src/SmartSelenium.Application/AutomationModels.cs)
- [`src/SmartSelenium.Application/AutomationOrchestrator.cs`](../src/SmartSelenium.Application/AutomationOrchestrator.cs)
- [`src/SmartSelenium.Domain/FieldModels.cs`](../src/SmartSelenium.Domain/FieldModels.cs)
- [`src/SmartSelenium.Infrastructure/SeleniumAutomationServices.cs`](../src/SmartSelenium.Infrastructure/SeleniumAutomationServices.cs)

The solution is [`SmartSelenium.sln`](../SmartSelenium.sln). The project files
declare the target framework and packages. Tests are under `tests/`; the CI
workflow is [`.github/workflows/ci.yml`](../.github/workflows/ci.yml).

## 1. Technology fundamentals

### Selenium

1. **Simple meaning:** Selenium is a set of tools for controlling a real web
   browser through code.
2. **Why it exists:** Websites run in browsers, and many important behaviors
   depend on JavaScript, the DOM, browser rendering, and user interactions.
3. **Problem it solves:** It lets engineers repeat browser tasks and verify
   browser-visible behavior instead of manually repeating every action.
4. **Where it is used:** UI regression tests, cross-browser checks, and
   controlled browser workflows.
5. **In SmartSelenium:** `Selenium.WebDriver` is used by
   `SeleniumBrowserFactory` and `SeleniumBrowserSession`.
   `Selenium.Support` provides `SelectElement` for native HTML selects.
6. **Practical example:** Open the CloudQA form, enter a name, select a radio
   option, and confirm the resulting field state.

Selenium automates a browser; it does not automatically understand what a form
field *means*. SmartSelenium adds its own field descriptions and candidate
scoring for that purpose.

### WebDriver and ChromeDriver

1. **Simple meaning:** WebDriver is the command interface; ChromeDriver is the
   browser-specific service that carries out commands in Chrome.
2. **Why they exist:** Test code needs a stable way to ask different browsers
   to navigate, inspect elements, and interact.
3. **Problem they solve:** They separate a test's commands from each browser's
   internal implementation.
4. **Where they are used:** Selenium test suites, browser grids, and local or
   remote browser automation.
5. **In SmartSelenium:** `new ChromeDriver(options)` starts the Chrome session.
   The .NET Selenium client sends WebDriver commands to ChromeDriver; the
   driver translates those commands into browser operations. Selenium 4 uses
   the W3C WebDriver protocol for standard commands. This is not the same as
   the application calling Chrome's internal APIs directly.
6. **Practical example:** `NavigateAsync` ultimately asks the driver to
   navigate to the configured URL.

ChromeDriver must be compatible with the installed browser. Selenium Manager
can help locate or manage drivers in supported Selenium versions, but that
doesn't remove the need for a working Chrome installation and a compatible
runtime environment.

### Browser automation and browser sessions

1. **Simple meaning:** Browser automation is software acting through the same
   browser controls a user would use. A browser session is one running
   browser-and-driver instance with its own current page and state.
2. **Why it exists:** It provides repeatable control over a browser and a
   defined boundary for cookies, tabs, navigation, and cleanup.
3. **Problem it solves:** It makes browser state explicit and allows a test to
   start, use, and close a browser predictably.
4. **Where it is used:** UI tests, test farms, and browser-based acceptance
   checks.
5. **In SmartSelenium:** `IBrowserFactory` creates an `IBrowserSession`;
   `SeleniumBrowserSession` wraps `IWebDriver`. `ExecuteAsync` disposes the
   session in a `finally` block after the automation steps.
6. **Practical example:** One run creates a Chrome session, loads the form,
   interacts with it, then calls `Quit` through `DisposeAsync`.

The wrapper gives the rest of the application an interface instead of direct
access to `IWebDriver`. It still represents a real, stateful browser session;
it is not a stateless HTTP request.

### DOM, HTML elements, and attributes

1. **Simple meaning:** HTML describes a page. The browser turns it into a live
   document tree called the DOM (Document Object Model). Elements are nodes in
   that tree; attributes provide metadata such as `id`, `name`, `type`, and
   `aria-label`.
2. **Why they exist:** HTML gives browsers a shared page structure and gives
   scripts and assistive technologies ways to inspect and interact with it.
3. **Problem they solve:** The DOM represents the page as structured, queryable
   content rather than as an unstructured picture.
4. **Where they are used:** Browsers, front-end frameworks, accessibility
   tools, and test automation.
5. **In SmartSelenium:** `FindCandidatesAsync` queries `input`, `select`, and
   `textarea` elements, then reads a defined set of attributes and properties
   to create `LocatorCandidate` objects.
6. **Practical example:** A text input with `name="First Name"` and
   `placeholder="Name"` gives the locator engine clues even if its ID changes.

An HTML attribute is not necessarily the same as the current DOM property. For
example, a text input's `value` attribute may describe its initial value while
the live `value` property changes as the user types. Selenium's `GetAttribute`
behavior exposes browser element information, but a production verifier should
be deliberate about which live property it checks.

### Locators: IDs, names, CSS, XPath, links, and accessibility

1. **Simple meaning:** A locator is a rule for identifying one or more elements
   in the DOM.
2. **Why it exists:** Automation needs a way to turn a human target such as
   “the State dropdown” into an element it can operate on.
3. **Problem it solves:** It creates a repeatable mapping between a test action
   and page structure.
4. **Where they are used:** Browser tests, assistive interfaces, scraping, and
   front-end diagnostics.
5. **In SmartSelenium:** The implementation uses CSS selectors for discovery
   and action. The discovery query is `input, select, textarea`. The model has
   an `XPath` property, but the live discovery code sets it to `null`; XPath is
   not an implemented discovery strategy.
6. **Practical example:** `#fname` selects an element by ID; `input[name='First
   Name']` selects by name; `select` selects every select element.

An ID or name selector can be concise, but the page owner can change it. CSS is
compact and good for attributes and relationships. XPath can express complex
tree relationships, but long absolute XPath expressions are often fragile.
Link text is useful for actual links, but SmartSelenium does not use it.
Accessibility-oriented locators target what a user of assistive technology
perceives; this repository reads the `aria-label` attribute as one signal but
does not calculate an accessible name or use a role/name locator API.

### Inputs, radio buttons, checkboxes, and dropdowns

1. **Simple meaning:** These are different HTML controls with different
   interaction rules. A text input accepts text; a radio group allows one
   choice; checkboxes are independently toggled; a select presents options.
2. **Why they exist:** Forms need controls that communicate the kind of input
   expected and provide appropriate browser behavior.
3. **Problem they solve:** Each control exposes built-in interaction semantics
   and validation behavior.
4. **Where they are used:** Registration forms, search pages, surveys, and
   settings.
5. **In SmartSelenium:** First Name uses `Clear` and `SendKeys`; radio selection
   uses `Click`; dropdown selection uses Selenium's `SelectElement` and
   fallback option matching.
6. **Practical example:** To select a state, the code tries an option's `value`
   first, then its visible text.

The repository does not automate checkboxes, despite having a generic
`FieldType` enum. It also does not submit the form or assert a server-side
outcome; its configured goal is to set and verify the three fields.

### CSS selectors and XPath

1. **Simple meaning:** CSS selectors and XPath are two query languages for
   selecting DOM elements.
2. **Why they exist:** They let a program describe element characteristics and
   relationships.
3. **Problem they solve:** They avoid manually walking the page tree one node
   at a time.
4. **Where they are used:** Selenium, browser developer tools, and web
   frameworks.
5. **In SmartSelenium:** `By.CssSelector` is used by every Selenium lookup.
   XPath is represented in the domain model but not populated or executed.
6. **Practical example:** CSS `input[type='radio']` narrows a query to radio
   inputs. XPath could express “the input associated with a label whose text
   is State,” but no such XPath is implemented here.

Prefer selectors that express stable meaning and have a clear expected result
count. Neither CSS nor XPath is inherently resilient: a brittle selector in
either language remains brittle.

### Waits and synchronization

1. **Simple meaning:** A wait gives the page time to reach a condition before
   the test continues.
2. **Why it exists:** Web pages load and update asynchronously; a command may
   run before the desired element is ready.
3. **Problem it solves:** It reduces timing races between page behavior and
   test actions.
4. **Where it is used:** Dynamic forms, single-page applications, and browser
   tests generally.
5. **In SmartSelenium:** `SeleniumBrowserFactory` sets page-load timeout and
   implicit wait. The implicit wait is
   `max(1, ElementWaitSeconds / 2)`, so the default of 15 seconds becomes 7
   seconds. There is no `WebDriverWait`, condition-based explicit wait, or
   arbitrary sleep in the implementation.
6. **Practical example:** A general explicit wait might wait until the State
   select becomes visible and enabled. This repository does not implement that
   condition-based wait.

An implicit wait applies to element lookup attempts; it does not mean “wait
until this control has the correct value.” An explicit wait repeatedly checks a
specific condition with a bounded timeout. Mixing implicit and explicit waits
can make timeout behavior confusing, so a mature suite usually chooses a
consistent wait policy.

### Headless Chrome

1. **Simple meaning:** A headless browser runs without a normal visible browser
   window.
2. **Why it exists:** Automated tests often need to run on servers without a
   desktop.
3. **Problem it solves:** It allows browser tests to run in CI and container
   environments.
4. **Where it is used:** CI pipelines, remote test runners, and automated
   smoke tests.
5. **In SmartSelenium:** When `BrowserSettings.Headless` is true, the factory
   adds `--headless=new`. The checked-in example configuration may be changed
   locally; `Program` also defaults to headless when no config setting
   overrides it.
6. **Practical example:** A developer can run the same form flow in CI without
   opening a Chrome window.

Headless mode is not guaranteed to behave identically to headed mode in every
environment. When diagnosing layout, overlays, or rendering problems, headed
execution and screenshots can be useful; screenshot capture is not implemented
in this repository.

### .NET and C#

1. **Simple meaning:** .NET is Microsoft's runtime and development platform;
   C# is a programming language commonly used on it.
2. **Why they exist:** The runtime provides libraries, memory management, and
   execution services; C# provides a strongly typed language for application
   code.
3. **Problem they solve:** They provide a productive environment for building
   maintainable command-line, web, desktop, and service applications.
4. **Where they are used:** Enterprise services, APIs, desktop software, cloud
   applications, and test tooling.
5. **In SmartSelenium:** The projects target `net8.0`, use nullable reference
   types and implicit usings, and compile with the C# language supported by
   the installed .NET 8 SDK.
6. **Practical example:** `AutomationRequest` is a typed record rather than a
   loosely structured collection of strings.

The .NET runtime is not the Selenium driver. The .NET Selenium package is a
client library that communicates with the browser driver.

### Dependency injection (DI)

1. **Simple meaning:** DI means supplying an object with the dependencies it
   needs rather than having it construct all of them itself.
2. **Why it exists:** It makes dependencies visible and lets an application
   replace implementations at the composition boundary.
3. **Problem it solves:** It reduces hidden construction logic and supports
   substitution in tests.
4. **Where it is used:** Web applications, worker services, command-line tools,
   and test frameworks.
5. **In SmartSelenium:** `Program.Main` registers concrete mappings such as
   `IElementLocator` to `SeleniumLocatorEngine`, then resolves
   `IAutomationOrchestrator`. Constructor parameters receive dependencies.
6. **Practical example:** A test can pass a stub `IFormAutomationService` to
   `AutomationOrchestrator` without launching Chrome.

DI is used in a relatively small application here. It is not automatically
valuable to put every value into a container; the benefit is clearest at
replaceable boundaries such as browser and automation services.

### Configuration

1. **Simple meaning:** Configuration is data that changes how a program runs
   without changing its source code.
2. **Why it exists:** Different environments need different URLs, values,
   timeouts, and browser modes.
3. **Problem it solves:** It avoids baking environment-specific values into
   every build.
4. **Where it is used:** Local development, CI, staging, and production.
5. **In SmartSelenium:** `Program` reads optional `appsettings.json` and
   `appsettings.Development.json` from `AppContext.BaseDirectory`, then
   environment variables. The CLI project links
   `config/appsettings.example.json` into output as `appsettings.json`.
6. **Practical example:** `Browser:Headless` controls whether the factory adds
   Chrome's headless argument.

The actual precedence deserves attention: JSON and environment configuration
are loaded, but the parsed CLI arguments are only used as fallback values.
Therefore a non-empty configuration value wins over `--url`, `--first-name`,
`--gender`, or `--state`. The `Microsoft.Extensions.Configuration.CommandLine`
package is referenced, but `AddCommandLine` is not called. This is a real
behavioral caveat, not merely a theoretical alternative.

### Logging

1. **Simple meaning:** Logging records events and useful context while a
   program runs.
2. **Why it exists:** Failures in automation can happen in another process,
   another machine, or only under CI timing.
3. **Problem it solves:** It gives engineers a timeline and diagnostic evidence
   without attaching a debugger to every run.
4. **Where it is used:** Services, test runners, browser infrastructure, and
   command-line tools.
5. **In SmartSelenium:** `Microsoft.Extensions.Logging.Console` is registered
   in `Program`. The factory logs browser startup; the locator engine logs
   candidate selection and ambiguity; disposal logs a warning if `Quit` fails.
6. **Practical example:** A selection log reports a field name, confidence, and
   the evidence strings used by the scorer.

The logs do not include a full candidate-by-candidate rejection report or
screenshots. A production system may also need run IDs and structured
correlation across retries and browser sessions.

### xUnit, FluentAssertions, and package dependencies

1. **Simple meaning:** xUnit runs .NET tests; FluentAssertions makes test
   expectations readable.
2. **Why they exist:** Automated tests provide repeatable checks that can be
   run locally and in CI.
3. **Problem they solve:** They help catch regressions and express expected
   outcomes in executable form.
4. **Where they are used:** Unit, integration, and end-to-end test projects.
5. **In SmartSelenium:** Test projects use xUnit 2.5.3 and FluentAssertions
   6.12.1. `Microsoft.NET.Test.Sdk` runs tests; `coverlet.collector` is
   referenced, but the CI workflow does not collect or publish coverage.
   There is no Moq or NSubstitute package; the application test uses a hand
   written stub.
6. **Practical example:** `selected.Id.Should().Be("state")` says directly
   what the locator-engine test expects.

The package list is evidence of available tooling, not proof that every
package feature is used. `TestFixtures` currently contains only a placeholder
class; it does not provide reusable fixtures.

## 2. Selenium engineering

### The browser automation path

A useful mental model is:

```text
SmartSelenium CLI
    -> Selenium .NET client (IWebDriver)
        -> WebDriver protocol commands
            -> ChromeDriver service
                -> Chrome browser
                    -> live DOM and rendered page
```

The client process issues commands such as navigate, find, click, and read
properties. ChromeDriver interprets those commands for Chrome. The browser
updates the page and DOM; Selenium returns observations to the C# process.
This request/response boundary means that an element lookup or click is not
just a local method call; it can incur process and browser communication.

### Locating elements in practice

A locator should identify the intended control, not just any control of the
right tag. Common approaches include:

- **ID:** `#firstName` or Selenium `By.Id(...)`; concise when stable and unique.
- **Name:** `[name='firstName']`; useful for form controls, but names can be
  duplicated in radio groups.
- **CSS:** attributes, tag types, and relationships; expressive and widely
  supported.
- **XPath:** text and tree relationships; powerful, but easy to overfit to
  nesting and position.
- **Link text:** visible text on links; useful for a stable navigation link.
- **Accessibility-oriented matching:** role and accessible name; closely
  reflects how users perceive controls when the page has good accessibility
  semantics.

In the repository, only CSS is actually used to find candidates and later
interact. The code queries the broad set `input, select, textarea`; it does not
try `By.Id`, `By.Name`, XPath, link text, or a browser accessibility tree.
`LocatorCandidate` has `XPath` and `Path` fields, but discovery currently
leaves both null.

### Labels, placeholders, ARIA, and semantic HTML

HTML semantics create useful evidence. A native `<select>` is more likely to be
a dropdown than a `<div>`. A label associated with an input can give an
accessible name. A placeholder can provide a hint, while `aria-label` can
provide an accessible label when a visible label is not present.

These are not interchangeable:

- A **visible label** is text users can see.
- A **placeholder** is an input hint, often shown only while the value is empty.
- An **ARIA label** is accessibility metadata and may not be visible.
- A **name** and **ID** are attributes commonly used for form submission and
  DOM targeting; they are not automatically user-facing labels.

SmartSelenium reads `aria-label`, placeholder, name, ID, value, type, tag name,
visibility, and enabled state. It does **not** locate an associated `<label>`
through its `for` attribute or inspect nearby text, parent/sibling context, or
an accessible-name computation. In `SeleniumBrowserSession`, `LabelText` is
filled from `aria-label` or `value`, not from a real `<label>` lookup. This is
important when explaining the implementation accurately.

### Interacting with controls

- **Typing:** a robust text flow checks that the chosen control is an input or
  textarea, clears it, types the configured value, and reads back the result.
  SmartSelenium implements this sequence through `SetTextValueAsync` and
  `VerifyValueAsync`.
- **Radio buttons:** choose one option within the correct group, then check the
  selected state. SmartSelenium matches a requested value against `value`,
  `id`, `name`, or `aria-label` among elements returned by its CSS query.
- **Dropdowns:** use `SelectElement` for a native `<select>`. It tries option
  value, then visible text, then a case-insensitive manual comparison. A
  custom JavaScript dropdown is not handled by `SelectElement`.
- **Checkboxes:** check or uncheck one control independently. This is a
  different behavior from radio groups and is not implemented by this
  assignment's field service.

A type check matters: a good semantic match is still unsafe to interact with
when the actual element cannot perform the requested operation.

### Visibility and enabled state

An element can exist in the DOM but be hidden, disabled, covered, or outside a
usable state. SmartSelenium records `Displayed` and `Enabled` and gives visible
and enabled candidates small score bonuses. It does not explicitly reject
hidden/disabled candidates before scoring. A click can still fail if an overlay
intercepts it, even when Selenium reports an element as displayed and enabled.

In production, check the exact precondition needed for the action (visible,
enabled, clickable, or selected) and include useful diagnostics for failures.

### Synchronization and dynamic pages

An explicit wait is a bounded polling loop for a meaningful condition, such as
“the option list is present” or “the button is clickable.” It is generally
more reliable than sleeping a fixed number of seconds because it continues as
soon as the condition is true and fails with a bounded timeout otherwise.

SmartSelenium configures a page-load timeout and an implicit wait. It does not
use explicit waits. The implicit wait's configured duration is half of
`ElementWaitSeconds` with a one-second minimum. This wait only helps Selenium
element lookups; it does not wait for a particular candidate score, value, or
business condition.

### Stale elements and dynamic DOM

A **stale element** is a Selenium element reference whose corresponding DOM
node has been removed or replaced since it was found. This is common in
single-page applications and after rerendering. A safe response is usually to
re-find the element using a stable semantic locator and retry a bounded,
idempotent operation when appropriate. Blind retries can duplicate actions or
hide real defects.

The current implementation builds a `LocatorCandidate` snapshot and later
re-finds using a selector generated from the candidate's ID, name, or tag. That
can avoid keeping one `IWebElement` reference across steps, but it does not
handle `StaleElementReferenceException` explicitly. If a candidate has no ID or
name, `BuildSelector` falls back to its tag name, which can match many elements.

### Missing and ambiguous elements

Missing means no candidate satisfies the threshold or the candidate list is
empty. Ambiguous means multiple plausible candidates are too close to
distinguish safely. SmartSelenium represents these cases with
`ElementNotFoundException` and `AmbiguousElementException`.

`SelectBestCandidateAsync` requires its caller's minimum score and considers
the top two candidates ambiguous when their confidence difference is less than
0.08, except when the `sameNameGroup` condition is true. That exception is
intended to accommodate same-name groups such as radio buttons, but it is a
coarse global condition: a duplicate-name group can suppress the top-two
ambiguity check for the entire candidate set. It does not prove that the
highest-ranked candidate is the correct field.

Rejecting uncertainty is preferable to silently clicking the first match. A
wrong click can create false test success, corrupt test data, or trigger an
unintended action. The trade-off is that an honest failure requires diagnostics
and a maintainer to decide what changed.

### Verification after interaction

An interaction succeeding at the WebDriver command level does not necessarily
mean the intended state changed. A robust flow reads the resulting value or
selected state and checks it against the request.

SmartSelenium verifies text input values and selected dropdown option values
or text. For multiple elements, `VerifyValueAsync` picks the first selected
element, expected-valued element, or `aria-checked=true`; otherwise it falls
back to the first result. For radio controls, this can produce a false positive:
if an element has the expected value but is not selected, the method can still
return true because the value matches. Therefore “the code runs a verification
method” is accurate; “the radio selection is always conclusively verified” is
not.

### Screenshots, page source, and debugging

Useful browser diagnostics include the current URL, page source, candidate
attributes and scores, screenshots, browser logs, and a trace of actions.
SmartSelenium exposes `GetCurrentUrlAsync` and `GetPageSourceAsync`, but the
automation flow does not call them. There is no screenshot capture, browser
console log collection, or failure artifact upload in the code.

When diagnosing a failure, first determine whether it is a browser startup,
navigation, lookup, interaction, timing, verification, or environment failure.
Then collect targeted evidence rather than adding arbitrary sleeps.

### Lifecycle and resource cleanup

A Chrome session consumes processes and memory. It should be closed even when a
step throws. SmartSelenium's `ExecuteAsync` uses `finally` to call
`DisposeAsync`; `SeleniumBrowserSession.DisposeAsync` calls `_driver.Quit()` and
logs a warning if quitting fails.

There are lifecycle caveats. Navigation occurs before the `try/finally` in
`ExecuteAsync`, so if `NavigateAsync` throws after driver creation, this
method's `finally` is not entered and the session may remain open. The
form-service object also stores `_session` in a mutable field and is registered
as a singleton. That is adequate for a one-run CLI, but concurrent or repeated
invocations through the same instance could share disposed or changing state.

## 3. Resilient locator engine

### What a locator is and why fixed selectors break

A locator is a rule for finding an element. A fixed selector such as
`#fname` is simple and fast, but it depends on the page continuing to use that
ID. It can fail after a harmless ID change. A positional selector such as “the
first text input” can silently target Last Name when a new field is added.

Resilience means surviving a defined set of likely changes, not surviving any
arbitrary redesign. When the site's meaning and accessible semantics disappear
or change, automation needs a human decision.

### Strategy-based discovery, candidates, and scores

An engineer can design a locator system in stages:

1. Define the target semantically (for example, field name, label, expected
   control type, synonyms).
2. Generate a bounded set of plausible DOM candidates.
3. Capture evidence for each candidate.
4. Score candidates using independent signals.
5. Require a minimum confidence.
6. Detect close competitors and fail with diagnostics rather than guessing.
7. Interact with the winning candidate and verify the resulting state.

That is the intended shape of this repository, but its actual implementation
has one discovery query: `input, select, textarea`. `ILocatorStrategy` exists
as an interface but is not registered or used by `SeleniumLocatorEngine`.
There are no separate label, ARIA, relationship, or text-context discovery
strategies.

### Actual scoring implementation

`DefaultCandidateScorer.Score` in
[`SeleniumAutomationServices.cs`](../src/SmartSelenium.Infrastructure/SeleniumAutomationServices.cs)
starts at zero and adds these weights:

| Evidence found | Added score |
|---|---:|
| Field is `Text` and candidate tag is `input` or `textarea` | 0.22 |
| Field is `Text` and input type is `text`, `email`, or `search` | 0.18 |
| Field is `RadioGroup` and candidate type is `radio` | 0.28 |
| Field is `Select` and candidate tag is `select` | 0.32 |
| Each distinct overlapping token, up to three tokens | 0.15 each, capped at 0.45 |
| Any token overlap plus candidate has ID, name, placeholder, or ARIA label | 0.10 |
| Candidate placeholder contains the configured field placeholder | 0.15 |
| Candidate ARIA label contains the configured field ARIA label | 0.12 |
| Candidate ID contains `first` for a field whose name contains `first` | 0.12 |
| Candidate is visible | 0.08 |
| Candidate is enabled | 0.05 |

The final value is clamped to 1.0 and rounded to four decimal places. Token
normalization replaces hyphens and underscores with spaces, splits on spaces,
lowercases, ignores one-character tokens, and uses distinct tokens. There is
no negative score for contradictory evidence; unrelated semantic tokens
simply do not add points.

These are heuristic weights, not statistically calibrated probabilities. A
confidence of 0.90 does not mean the system has measured a 90% probability of
being correct. It means the implemented rules produced a score of 0.90.

### Actual confidence thresholds and ambiguity

The score and threshold are related but separate:

- The text field method calls `SelectBestCandidateAsync` with **0.75**.
- The state method uses **0.65**.
- The gender fallback calls it with **0.60**.
- `CandidateScore.IsSelected` has a hard-coded **0.75** property, but the
  selection engine does not use that property.
- `BrowserSettings.ConfidenceThreshold` is read by `Program`, but no service
  uses it to select candidates. The effective thresholds are hard-coded at
  these call sites.

Two candidates are considered close when their scores differ by less than
**0.08**. The engine raises ambiguity only if `sameNameGroup` is false. That
flag becomes true if *any* candidate group keyed by name, ID, or `name`
attribute contains multiple candidates whose score range is less than 0.08.
This is not a precise per-option or per-field ambiguity policy.

### First Name example

The form service defines a `firstName` text field with label “First Name,”
alternative labels including “given name,” placeholder “First Name,” and
keywords “first” and “name.” The live target used during implementation had
an input with an ID resembling `fname`, name “First Name,” and placeholder
“Name.” The scorer can use the name tokens and text-input type even if an ID
changes.

But the implementation's discovery snapshot does not obtain the actual
associated visible label. Also, the model's `LabelText` is populated from
`aria-label` or `value`; an empty input value contributes no visible label.
Thus, if the ID, name, placeholder, and ARIA information all change or are
absent, the semantic evidence may be too weak. The threshold should make it
fail rather than claim arbitrary resilience.

After selection, `BuildSelector` prefers ID, then name, then tag. It does not
retain a unique reference to the exact candidate. If the ID is absent, a
duplicate name can still select the wrong matching control; if both are
absent, a tag fallback such as `input` can be very broad. This illustrates why
scoring and interaction-target construction must be designed together.

### Why reject uncertain matches?

The cost of a false positive is usually higher than the cost of a clear test
failure. If the wrong radio option is selected, the test may appear green while
the application behavior is wrong. A confidence threshold and ambiguity error
make uncertainty visible. They only help when candidate evidence is accurate,
the score is meaningful, and the later interaction refers to the same winning
candidate.

## 4. Software architecture

### Layered architecture and Clean Architecture

**Simple definition:** Layered architecture puts code with related
responsibilities together and defines which direction dependencies may flow.
Clean Architecture is a family of approaches that keeps core policy independent
from external mechanisms.

**Why engineers use it:** UI, browser, database, and vendor choices change at
different rates. Boundaries reduce the number of unrelated changes that must
be made together.

**Example:** An application use case can depend on an `IBrowserSession`
contract while Selenium-specific code implements the contract.

**SmartSelenium:** Domain models live in `SmartSelenium.Domain`; application
contracts and orchestration are in `SmartSelenium.Application`; Chrome and
Selenium implementation live in `SmartSelenium.Infrastructure`; `Program`
acts as the CLI/composition root. Application references Domain;
Infrastructure references Application and Domain; CLI references the layers to
wire them together.

**Honest assessment and trade-off:** This is a useful layered structure, but
the implementation is not a comprehensive Clean Architecture demonstration.
The application orchestrator simply delegates; the infrastructure groups
several classes in one source file; the `IElementLocator` methods take
infrastructure-agnostic models but browser operations remain shaped by CSS
selectors; and several models/contracts are currently unused. For a small
assignment, the extra projects and interfaces add ceremony. They become more
valuable when there are multiple use cases, browser implementations, or
independently testable policies.

### Domain, application, infrastructure, and CLI

| Layer | Plain-language responsibility | Repository example | Trade-off |
|---|---|---|---|
| Domain | Names the concepts and rules that matter to the task. | `FieldDefinition`, `LocatorCandidate`, `FieldInteractionResult`, and domain exceptions in `FieldModels.cs`. | A very thin domain can be useful, but a model layer without substantive rules can become mostly data containers. |
| Application | Defines the use-case boundary and coordinates work through contracts. | `AutomationOrchestrator.RunAsync` delegates to `IFormAutomationService.ExecuteAsync`; interfaces are in `AutomationModels.cs`. | A one-line delegator may not justify a separate layer yet; a larger use case could own validation and workflow policy. |
| Infrastructure | Connects the application to external technology. | `SeleniumBrowserFactory`, `SeleniumBrowserSession`, scorer, locator engine, and field automation service. | This layer necessarily knows Selenium and the target page's control conventions. |
| CLI/presentation | Accepts inputs, creates the object graph, starts work, and presents the result. | `Program.Main` loads configuration, registers services, resolves the orchestrator, prints results, and returns an exit code. | A compact CLI is simple; configuration parsing and DI composition in `Main` can grow and become harder to test. |

This project is not a web server, and its “presentation” layer is a console
entry point.

### Separation of concerns, coupling, and cohesion

- **Separation of concerns** means keeping configuration parsing, field
  selection policy, and browser mechanics distinct. SmartSelenium mostly does
  this through projects and interfaces.
- **Loose coupling** means one component relies on a contract rather than a
  specific class. `AutomationOrchestrator` accepts
  `IFormAutomationService`, not `SeleniumFormAutomationService`.
- **High cohesion** means the parts of a component contribute to one purpose.
  `SeleniumBrowserSession` focuses on browser operations. By contrast,
  `SeleniumAutomationServices.cs` contains several distinct types in one file;
  that is a file organization choice, not by itself a runtime defect.

These ideas help when they reduce the cost of change. They are not goals to
maximize by creating an interface for every method or splitting every class
into tiny files.

### Dependency inversion and interfaces

**Dependency inversion** says high-level policy should not directly depend on
low-level implementation details; both can depend on abstractions.
`IAutomationOrchestrator`, `IFormAutomationService`, `IBrowserFactory`,
`IBrowserSession`, and `IElementLocator` provide seams between those concerns.
The Infrastructure project implements several application interfaces, and
the CLI chooses the concrete registrations.

An interface is valuable when it represents a meaningful boundary or supports
replacement/testing. `ILocatorStrategy` is defined but unused, so the current
code does not yet gain extensibility from that contract. The browser session
interface also contains several operations; if a larger application had
clients needing only subsets, it could be split, but that would be premature
without a concrete need.

### Dependency injection

`Program.Main` is the composition root: it decides that the run will use
`SeleniumBrowserFactory`, `DefaultCandidateScorer`,
`SeleniumLocatorEngine`, `SeleniumFormAutomationService`, and
`AutomationOrchestrator`. Constructor injection makes these dependencies
visible and enables a test double such as the stub in
`AutomationOrchestratorTests`.

All services are registered as singletons. That is simple for a one-run
console application. However, the form service stores mutable session state;
singleton lifetime would be unsafe for concurrent runs or reusing the same
provider for multiple runs. A scoped or per-run service/session lifetime would
be a better fit if the application became a concurrent service.

### SOLID principles: where they apply and where they do not

- **Single Responsibility:** There are focused types for factory, session,
  scoring, locating, and form operations. The form service still combines
  session ownership and multiple field operations, and the CLI combines config
  loading, registrations, execution, and console output.
- **Open/Closed:** `ICandidateScorer` can be replaced through DI. The locator
  strategy interface suggests extension but is unused. Adding a new strategy
  currently means changing `SeleniumLocatorEngine`, not registering a set of
  active strategies.
- **Liskov Substitution:** There is little inheritance beyond domain
  exceptions inheriting `Exception`; the repository does not demonstrate a
  meaningful polymorphic hierarchy.
- **Interface Segregation:** Contracts let clients depend on abstractions, but
  `IBrowserSession` is a broad interface with navigation, lookup, interaction,
  and diagnostics methods. Whether to split it depends on actual consumers.
- **Dependency Inversion:** This is the clearest principle in the design:
  application-level contracts are implemented by infrastructure, selected
  from the CLI composition root.

It is more credible in an interview to say “the code applies dependency
inversion at the browser/service boundary, while other SOLID principles are
only partially demonstrated” than to claim perfect adherence.

## 5. Testing engineering

### Testing levels

- **Unit test:** Tests a small unit in isolation, usually with controlled
  inputs and test doubles. It is fast and deterministic, but cannot prove a
  real browser behaves correctly.
- **Integration test:** Exercises multiple components together, such as the
  Selenium adapter with a real local browser or a controlled test page.
- **End-to-end test:** Exercises the full user-visible path, often including a
  real application and external systems. It gives broad confidence but is
  slower, more environment-sensitive, and harder to diagnose.

Browser automation commonly needs a mix: unit-test scoring rules; test browser
adapter behavior against a stable local HTML fixture; and run a smaller,
controlled end-to-end suite in CI or a dedicated environment.

### Fixtures, mocks, and test doubles

A **fixture** is reusable test setup or data. A **test double** is a
replacement for a real dependency. A **stub** returns predetermined responses;
a **mock** typically also verifies expected interactions.

`AutomationOrchestratorTests` creates a hand-written
`StubFormAutomationService`, a deterministic test double. The repository
references a `TestFixtures` project, but it contains only an empty `Class1`
placeholder and is not used to provide a fixture. There is no Moq or
NSubstitute dependency.

### What the actual tests verify

| Test project / file | What is genuinely asserted | What it does not establish |
|---|---|---|
| `SmartSelenium.Domain.Tests/FieldDefinitionTests.cs` | Duplicate alternative labels/keywords are removed case-insensitively; name trimming behavior is represented; success/failure result factories populate fields. | It does not test locator accuracy, type rules, or invalid model input. |
| `SmartSelenium.Application.Tests/AutomationOrchestratorTests.cs` | The orchestrator calls the service once and returns the stub's three successful field results. | It does not test browser creation, failure propagation, cancellation, or real field automation. |
| `SmartSelenium.Infrastructure.Tests/LocatorScoringTests.cs` | One first-name candidate receives at least 0.75 confidence and evidence; a State candidate outranks a Country candidate for the supplied data. | It does not test real DOM discovery, ambiguous handling, visibility/type edge cases, radio behavior, dropdown operations, or browser failures. |
| `SmartSelenium.IntegrationTests/FormAutomationIntegrationTests.cs` | Nothing about CloudQA: the only purported flow test is skipped and its body is `true.Should().BeTrue()`. | It is not an end-to-end test and does not run Selenium. |
| `UnitTest1.cs` files in test projects | These are template tests with no meaningful assertions. | A passing result from them proves no application behavior. |

The tests are useful starter checks but do not validate the complete browser
flow. The CI workflow restores, builds, and runs tests on Ubuntu with .NET 8;
it does not install a browser, execute a real browser test, or publish coverage.
`coverlet.collector` is present in test project files, but no coverage
collection/report step appears in the workflow.

### Determinism, isolation, flakiness, and regression

A deterministic test produces the same outcome for the same inputs. Isolation
means one test does not depend on another test's state. A flaky test sometimes
passes and sometimes fails due to timing, environment, shared state, or an
external service.

Pure scoring tests can be deterministic because they operate on supplied
candidate objects. A live CloudQA test would depend on network availability,
target-site changes, browser installation, and timing, so it should be
isolated and treated as an environment-dependent test. A stable local fixture
page can test Selenium interactions without relying on the public target.

A regression test captures a bug or requirement so it cannot silently return.
Test coverage is a measure of which code ran, not whether the assertions were
strong or whether the requirements were correctly tested. Passing placeholder
tests and high line coverage would not prove the automation chose the right
radio option.

## 6. Engineering trade-offs

| Choice | Advantages | Costs / when another choice may fit |
|---|---|---|
| **Selenium vs Playwright** | Selenium has broad browser/platform history, W3C WebDriver support, and a large ecosystem. | Playwright has convenient auto-waiting, browser contexts, tracing, and modern test APIs. Choose Selenium for broad existing ecosystem/grid compatibility; choose Playwright when its built-in browser tooling and workflow fit better. SmartSelenium uses Selenium 4.30.0. |
| **Selenium vs Cypress** | Selenium automates browsers through a driver and supports several languages and browser setups. | Cypress offers an integrated JavaScript-oriented test runner and close app debugging, but has a different execution model and constraints. Choose based on language, browser needs, app architecture, and CI. This C# project uses Selenium. |
| **CSS vs XPath** | CSS is concise for tags and attributes and broadly familiar. | XPath can express text and ancestor/descendant relationships. It can become fragile when tied to DOM position. SmartSelenium currently uses CSS only; neither language makes a selector resilient by itself. |
| **Fixed vs resilient locator** | A stable, unique test ID is easy to understand and maintain when the app team controls it. | Semantic candidate scoring may survive some attribute changes but is more complex and can make false matches if evidence is weak. Prefer stable explicit test hooks in controlled applications; use resilient discovery where pages cannot provide them and require uncertainty safeguards. |
| **Headless vs headed** | Headless is suitable for server CI and uses no visible desktop. | Headed mode helps inspect layout, browser prompts, and overlays. Use headless for routine CI and headed reproduction for visual or interaction debugging. |
| **Explicit vs implicit wait** | Explicit waits target a specific state and make synchronization intent visible. | Implicit waits are globally simple but can make each lookup slow and interact confusingly with explicit waits. SmartSelenium uses an implicit wait only; it has no explicit wait condition. |
| **External vs hard-coded config** | External settings allow the same build to target different inputs/environments. | Configuration introduces precedence, validation, and deployment complexity. SmartSelenium uses JSON/environment config, but the CLI flags are only fallbacks and config wins when set. |
| **Unit vs live browser test** | Unit tests are fast, cheap, and controllable. | They cannot prove the browser's real behavior. Live tests verify more layers but are slower and depend on browser/network state. Use both, and keep public-site end-to-end tests small and non-blocking where possible. |

## 7. Real engineering scenarios

### 1. The website changes the First Name input ID

- **Problem:** `#fname` no longer identifies the input.
- **Possible causes:** a front-end rewrite, generated IDs, or a naming cleanup.
- **Engineering solution:** identify the field through stable visible/accessibility
  semantics or an app-provided test ID; score multiple candidate attributes;
  verify the resulting value.
- **Repository fit and trade-off:** SmartSelenium scores ID, name, placeholder,
  ARIA, type, and tokens; it can tolerate an ID change if other evidence
  remains. It does not truly read visible associated labels, and its resulting
  selector still prefers ID/name. If those signals disappear too, it may fail
  the threshold or fall back broadly. The current example is not a guarantee
  against redesign.

### 2. Two fields have similar labels

- **Problem:** “Name” could mean First Name, Last Name, or another input.
- **Possible causes:** generic placeholders, abbreviated labels, or duplicated
  forms.
- **Engineering solution:** combine precise label, field type, group/container
  context, and unique accessibility or test identifiers. If evidence remains
  close, fail with both candidates' evidence for a human to review.
- **Repository fit and trade-off:** The `firstName` field adds “given name” and
  first/name tokens, and ambiguity handling compares top scores. Candidate
  context is not actually collected, and token overlap can still favor a
  misleading candidate.

### 3. A dropdown loads dynamically

- **Problem:** The select exists before its desired options arrive.
- **Possible causes:** an API call, dependent-country selection, or client-side
  rendering.
- **Engineering solution:** explicitly wait for the requested option to be
  present and enabled, select it, then verify the selected option.
- **Repository fit and trade-off:** The code uses an implicit lookup wait, then
  selects by value/text and verifies. The implicit wait does not wait for an
  option inside an already found select, and no explicit option wait exists.
  The current operation may fail too early on a slow dynamic list.

### 4. The element exists but is hidden

- **Problem:** DOM lookup finds a control that is not usable to a user.
- **Possible causes:** hidden template fields, responsive duplicate controls,
  collapsed panels, or overlays.
- **Engineering solution:** require visible and enabled state and wait for the
  expected interaction condition. Use the visible label/control relationship
  and report hidden matches separately.
- **Repository fit and trade-off:** Visibility and enabled state add score
  points but are not hard filters. A hidden candidate can still be selected if
  its other score is high enough; interaction then may throw.

### 5. Chrome crashes during automation

- **Problem:** The WebDriver connection is lost or the browser process exits.
- **Possible causes:** memory pressure, incompatible binaries, CI sandbox
  configuration, browser bugs, or a killed process.
- **Engineering solution:** capture driver/browser logs and run context; ensure
  cleanup; retry only when the operation is safe and the failure is
  understood; isolate the run in a fresh session.
- **Repository fit and trade-off:** The factory logs startup and disposal logs
  a quit warning. There is no explicit crash classification, screenshot, retry,
  or structured browser-log capture. `--no-sandbox` and
  `--disable-dev-shm-usage` are passed, but they are not a complete crash
  strategy.

### 6. The browser is slower than expected

- **Problem:** A lookup or action times out or appears inconsistent.
- **Possible causes:** slow CI machines, network latency, API calls, page
  rendering, or an implicit wait on many lookups.
- **Engineering solution:** measure which phase is slow; wait on specific
  readiness conditions; bound navigation and interaction timeouts; retain
  diagnostics for the timeout.
- **Repository fit and trade-off:** Page-load timeout and implicit wait are
  configurable. The configured 15-second element wait is halved to 7 seconds
  and applied as an implicit wait. There is no explicit condition-based wait
  or per-step timing telemetry.

### 7. The automation selects the wrong radio option

- **Problem:** The group is found, but the requested value is not the selected
  radio.
- **Possible causes:** wrong group, ambiguous values, matching by an overly
  broad attribute, or an incorrect verification rule.
- **Engineering solution:** scope discovery to a semantic group, match the
  requested option exactly, click that option, then verify its selected state
  (not just its value).
- **Repository fit and trade-off:** `SelectRadioAsync` matches a requested
  value/ID/name/ARIA label among `input[name='gender'], input[type='radio']`.
  The scoring-selected `desiredCandidate` is calculated but not used for
  interaction. `VerifyValueAsync` can report success when the expected-valued
  radio exists even if it is not selected. This is a significant verification
  limitation.

### 8. The website changes its DOM structure

- **Problem:** A previously useful relationship or tag structure no longer
  exists.
- **Possible causes:** component-library change, nested wrappers, custom
  controls, or a redesigned form.
- **Engineering solution:** prefer stable semantic/accessibility contracts or
  app-owned test IDs; update candidate discovery when control types change;
  add a regression fixture representing the new DOM.
- **Repository fit and trade-off:** The current discovery searches native
  `input`, `select`, and `textarea` tags and can use some attributes. A custom
  `<div role="combobox">` is excluded. No parent/sibling or label-based
  strategy is implemented.

### 9. The test passes locally but fails in CI

- **Problem:** Different environments produce different browser behavior.
- **Possible causes:** Chrome/driver mismatch, missing browser dependencies,
  viewport differences, timing, network access, user permissions, or a headed
  browser requirement.
- **Engineering solution:** pin or manage browser versions; use a known CI
  image; run headless; capture artifacts; avoid public network dependencies
  for core tests; run a local fixture integration test.
- **Repository fit and trade-off:** CI runs Ubuntu and .NET 8 but does not
  install Chrome or execute the skipped integration test. The tests that run
  are predominantly pure logic; they cannot expose browser environment
  differences.

### 10. A locator matches multiple candidates

- **Problem:** More than one element looks plausible.
- **Possible causes:** duplicated controls, radio options sharing a name, hidden
  responsive copies, or weak labels.
- **Engineering solution:** use group-aware matching and exact option values;
  include enough context to rank candidates; throw with the top candidates and
  reasons when ambiguity remains.
- **Repository fit and trade-off:** The engine has a 0.08 close-score rule and
  same-name bypass. The bypass is broad and there is no candidate-by-candidate
  diagnostic rejection report. A safe system should not silently choose the
  first match.

## 8. Interview defence

### 25 technical interview questions and natural answers

1. **What is Selenium?**  
   It is a browser automation toolset. In this project the .NET Selenium client
   controls Chrome through ChromeDriver so we can exercise the actual rendered
   form.

2. **What role does ChromeDriver play?**  
   It is the browser-specific WebDriver service. The test code sends commands
   through Selenium, and ChromeDriver translates them into Chrome operations.

3. **What is the DOM?**  
   It is the browser's live object representation of HTML. Selenium queries
   that structure to locate controls and inspect their properties.

4. **What is a locator?**  
   It is a rule for finding an element. A locator should represent the intended
   control, not merely the first element with a certain tag.

5. **Why can an ID selector be brittle?**  
   The page owner may rename or regenerate IDs. A stable ID is still a good
   locator when the application team guarantees it; the problem is relying on
   an unstable implementation detail without verification.

6. **What does SmartSelenium do to improve locator resilience?**  
   It discovers native form controls, scores them using control type, semantic
   tokens, attributes, visibility, and enabled state, then applies thresholds.
   It is a heuristic, not arbitrary redesign-proof automation.

7. **Is a confidence score a probability?**  
   No. Here it is a weighted heuristic total. A score of 0.8 means the rules
   added up to 0.8, not that calibrated evidence proves an 80% chance.

8. **Why use a threshold?**  
   It prevents low-evidence candidates from being treated as certain. The
   trade-off is more explicit failures when the page lacks useful semantics.

9. **Why detect ambiguity?**  
   Choosing the first match can make a wrong test look successful. A clear
   failure is safer when candidates are close.

10. **What is the difference between implicit and explicit waits?**  
    An implicit wait affects element lookups globally. An explicit wait polls a
    particular condition such as visibility or option availability. This code
    uses an implicit wait, not explicit waits.

11. **Why is `Thread.Sleep` usually a poor wait?**  
    It always waits the full duration and still may be too short. A condition
    wait can proceed immediately when ready and fail with a useful timeout.

12. **What is a stale element?**  
    It is a reference to a DOM node that was removed or replaced. The usual
    response is to re-find it using stable semantics rather than repeatedly
    using the stale reference.

13. **How should you select a radio option?**  
    Identify the correct group, match the requested option, click it, and
    verify that the exact option is selected. In this project the interaction
    matches the requested value, but the current verifier can produce a false
    positive based on value alone.

14. **How should you select a native dropdown?**  
    Use `SelectElement`, select by value or visible text, then read the selected
    option back. For dynamic options, wait until the desired option exists.

15. **Why validate element type before interacting?**  
    A semantically plausible candidate might still be the wrong kind of
    control. Type validation prevents attempting text entry in a dropdown or
    applying a dropdown API to a custom element.

16. **Why verify after typing?**  
    The command may have targeted the wrong element or the page may have
    changed the value. Reading the value back checks the observable result.

17. **What does dependency injection buy this project?**  
    It lets the CLI select concrete Selenium services while application code
    accepts interfaces. A test can substitute a stub without launching Chrome.

18. **What is the composition root?**  
    It is the place that assembles the object graph. Here it is `Program.Main`,
    where concrete implementations are registered with the service collection.

19. **Why separate Domain, Application, and Infrastructure?**  
    It groups field concepts, workflow contracts, and Selenium details at
    different boundaries. That can reduce change impact, though for this small
    assignment the layering has some overhead.

20. **Does this project fully follow Clean Architecture?**  
    I would describe it as a layered design with dependency inversion at the
    service boundary. The application orchestrator is very thin, some
    contracts are unused, and the infrastructure implementation is still
    tightly tied to Chrome and native HTML controls.

21. **What is the biggest testing gap?**  
    There is no active end-to-end browser test. The CloudQA test is skipped,
    and the CI workflow does not install or run a browser.

22. **Why are test doubles useful?**  
    They isolate a unit from slower or less predictable dependencies. The
    orchestrator test uses a stub service to check delegation deterministically.

23. **What does the CI workflow verify?**  
    It restores dependencies, builds Release, and runs the test projects on
    Ubuntu with .NET 8. It does not validate the live browser flow.

24. **What is one important configuration issue?**  
    The CLI parses flags, but existing JSON/environment values take precedence
    because command-line values are used only as fallbacks. Also, the
    configurable confidence threshold is read but not used by candidate
    selection.

25. **What would you improve first for production reliability?**  
    I would fix exact radio-state verification and keep interaction tied to the
    actual selected candidate; add explicit waits and local browser integration
    tests; then improve label/context discovery and failure diagnostics.

### 10 challenging follow-up questions

1. **If a field's label and ID change, what evidence remains?**  
   In this code, possibly name, placeholder, ARIA, type, or other candidate
   tokens. If meaningful signals disappear, the responsible behavior is to
   fail rather than claim universal recovery.

2. **Why not simply lower the threshold until the test passes?**  
   Lowering a threshold trades false negatives for false positives. I would
   inspect the candidate evidence and add a reliable signal before changing
   the safety bar.

3. **What does a 0.75 score mean statistically?**  
   Nothing statistical by itself; it is a heuristic. To interpret it as
   probability would require labeled examples, calibration, and validation on
   representative pages.

4. **Could two candidates each score 0.9 and still be ambiguous?**  
   Yes. Absolute score is not enough; the difference between top candidates
   and the quality of their distinguishing evidence matter.

5. **Why not retry every Selenium exception?**  
   Some actions are not idempotent and retries can double-submit or hide a real
   defect. Classify transient failures and retry only safe operations with
   bounded attempts.

6. **How would you test the locator without CloudQA being online?**  
   Serve a deterministic local HTML fixture with known variations: renamed ID,
   duplicate labels, hidden field, reordered fields, and changed tag semantics.
   Assert both candidate selection and explicit failure cases.

7. **Does a passing text-value assertion prove the right field was selected?**  
   Not necessarily. Another field may accept the same value. The test should
   assert the intended control identity and relevant surrounding form state.

8. **Why can a singleton service be unsafe here?**  
   `SeleniumFormAutomationService` stores `_session` in a mutable field. Two
   concurrent runs could overwrite or dispose one another's session.

9. **What would you do if the site replaced `<select>` with a custom combobox?**  
   Add discovery and interaction semantics for that component, preferably via
   role and accessible name, and verify its selected value. The current
   `SelectElement` path only supports native selects.

10. **How do you prove a selector is resilient?**  
    Define the specific changes it should tolerate, create fixtures for those
    variants, and assert correct selection plus fail-safe behavior on
    ambiguity. No finite test proves resilience to arbitrary redesign.

### 5 architecture and design questions

1. **Where should a new browser implementation go?**  
   Infrastructure should implement `IBrowserFactory` and `IBrowserSession`;
   the CLI composition root would select it. This keeps browser choice out of
   the domain.

2. **How would you make confidence thresholds genuinely configurable?**  
   Validate settings at startup and pass them into the selection policy,
   replacing hard-coded per-field values with explicit configuration or
   field-specific policy. Add tests proving each threshold is honored.

3. **Would you create an interface for every class?**  
   No. I would add abstractions around replaceable or independently testable
   boundaries, such as browser sessions, and avoid interfaces that have only
   one consumer and no realistic substitution need.

4. **How would you support multiple browsers?**  
   Define validated browser options and register a factory that selects the
   requested driver. Add browser-specific tests and keep browser-specific
   capabilities out of the application workflow.

5. **How would you make repeated runs safe?**  
   Make session ownership per run rather than mutable singleton state, use a
   fresh session or explicit scope, dispose it even if navigation fails, and
   test concurrent calls.

### 5 Selenium troubleshooting questions

1. **`NoSuchElementException`: what do you check?**  
   Confirm the current URL and frame/window, inspect the live DOM, check
   selector uniqueness and timing, then wait for the correct condition. Do not
   immediately add a sleep.

2. **`StaleElementReferenceException`: what do you check?**  
   Find what rerendered the DOM, then re-locate the element after that change.
   Retry only a safe action and keep retries bounded.

3. **Element is present but click fails: why?**  
   It may be hidden, disabled, covered by an overlay, or outside the
   interaction state. Check displayed/enabled/clickability and inspect a
   screenshot or browser evidence.

4. **Works locally, fails in CI: what next?**  
   Compare browser/driver versions, headless mode, viewport, dependencies,
   permissions, and timing. Capture logs and run against a controlled local
   page before depending on an external site.

5. **The dropdown selection command succeeds but the page is wrong: why?**  
   Check whether it is a native or custom dropdown, whether the requested value
   matches option value or visible text, whether options loaded, and whether
   the correct select was targeted. Read back the selected option explicitly.

## Final review

### 20 concepts to remember

1. Selenium automates a browser; it does not infer a field's meaning by itself.
2. WebDriver commands pass through a browser-specific driver such as
   ChromeDriver.
3. The DOM is the browser's live, queryable representation of the page.
4. A locator identifies elements; a strong locator is semantic, unique, and
   testable.
5. IDs are useful when stable, not magically reliable.
6. CSS and XPath are query languages, not resilience strategies.
7. Visible labels, placeholders, ARIA labels, IDs, and names are different
   signals.
8. Semantic HTML makes both automation and accessibility easier.
9. Radio groups, checkboxes, text inputs, and selects need different
   interactions.
10. A successful WebDriver command does not prove the intended state changed.
11. Verify observable state after each important interaction.
12. Explicit waits target a condition; implicit waits affect element searches.
13. Fixed sleeps hide timing problems rather than solving them.
14. Dynamic DOM replacement can make element references stale.
15. Confidence scores are only as trustworthy as their evidence and calibration.
16. Ambiguity should be visible; silent “first match” behavior is dangerous.
17. Interfaces and DI create useful substitution points when they reflect real
   boundaries.
18. Layering has a maintenance benefit and an abstraction cost.
19. Unit tests cannot prove a real browser flow; integration and end-to-end
   tests cover different risks.
20. A green build is evidence of build/test success, not proof of correct
   application behavior.

### One-page interview explanation

> SmartSelenium is a .NET 8 console application that automates three controls
> on the CloudQA practice form using Selenium 4 and Chrome. I separated the
> code into Domain, Application, Infrastructure, and CLI projects. The Domain
> defines field and result models; Application exposes browser, locator, and
> orchestration contracts; Infrastructure implements Chrome sessions,
> candidate scoring, and field interactions; and the CLI loads configuration,
> wires the services with dependency injection, runs the use case, and prints
> the result.
>
> The central idea is to avoid depending only on a fixed ID. The locator engine
> considers native control type, normalized semantic tokens from field and
> candidate attributes, placeholder/ARIA matches, visibility, and enabled
> state. It applies minimum thresholds and has a close-score ambiguity check.
> Those scores are heuristics rather than calibrated probabilities, and the
> current discovery implementation is narrower than a full semantic locator
> engine: it queries `input`, `select`, and `textarea`, does not resolve visible
> label relationships or nearby context, and later builds a CSS selector from
> ID/name/tag.
>
> The code verifies text and select values after interaction and reports a
> result per field. I would be candid that radio verification has a false
> positive risk because it can accept a matching value without confirming the
> radio is selected. The settings also have gaps: command-line flags are
> fallbacks to JSON/environment values, the browser type setting is not used to
> choose a driver, the configured confidence threshold does not reach the
> locator, and synchronization uses an implicit wait rather than explicit
> condition waits.
>
> The tests cover model normalization, orchestrator delegation, and a couple of
> scoring/selection cases. The CloudQA integration test is skipped, and CI
> builds and runs tests without a live browser. My next production-hardening
> steps would be exact candidate-to-interaction binding, correct radio selected
> state verification, explicit waits, a local HTML integration fixture, and
> richer failure diagnostics. The design is intended to tolerate some
> attribute changes, not arbitrary redesigns.

### Honest assessment

**What the implementation does well**

- Separates domain models, application contracts, Selenium implementation,
  and CLI composition.
- Uses constructor injection and interfaces at useful browser/service
  boundaries.
- Uses multiple simple signals rather than relying exclusively on an ID.
- Checks text and dropdown results after interactions.
- Has domain-level errors for missing, ambiguous, and invalid-type candidates.
- Uses structured Microsoft logging and attempts browser cleanup in a
  `finally` path for field execution.
- Includes deterministic tests for a few model, delegation, and scoring cases.

**Limitations and potential bugs**

- Candidate discovery is a single CSS query for native `input`, `select`, and
  `textarea` controls. `ILocatorStrategy` is not used; labels, accessible-name
  computation, parent/sibling context, XPath, and custom controls are not
  implemented.
- `LabelText` is sourced from `aria-label` or `value`, not an associated
  visible label.
- The scored `desiredCandidate` in `SelectGenderAsync` is never used for the
  actual click. The click uses a fixed CSS selector and value matching.
- Radio verification can succeed from value equality without proving the
  radio is selected.
- `sameNameGroup` can suppress ambiguity checking across the entire candidate
  list when any same-key group has close scores.
- `BuildSelector` prefers ID and then name, and ultimately falls back to a
  tag; the chosen candidate and action target are not robustly bound together.
- Explicit waits, stale-element recovery, screenshots, retries, and detailed
  rejection diagnostics are absent.
- `BrowserType` is configurable but the factory always creates Chrome.
  `ConfidenceThreshold` is loaded but not used. `ElementWaitSeconds` becomes a
  halved implicit wait.
- CLI flags are parsed but are only used when configuration has no non-empty
  value; there is no `AddCommandLine` provider call.
- The form service holds a mutable browser session in a singleton registration.
  This is unsuitable for concurrent runs and can retain a disposed session.
- Navigation occurs before the `try/finally`; a navigation failure can skip
  cleanup.
- Exceptions from automation are not converted into a normal summary in
  `Program.Main`, so some failures can terminate the process with a stack
  trace.
- The active tests do not exercise Selenium against a real or fixture page.
  The named CloudQA integration test is skipped, and several template tests
  make no assertions.

In an interview, acknowledging these limits demonstrates sound engineering
judgment. The useful claim is “the implementation has a heuristic locator
prototype with type/semantic signals and fail thresholds,” not “the framework
is resilient to any website change.”

### Glossary

| Term | Meaning |
|---|---|
| **Accessible name** | The name assistive technology uses to identify a control, computed from labels and accessibility semantics. |
| **ARIA** | Accessibility metadata attributes used to expose roles, names, and states to assistive technology. |
| **Attribute** | Metadata on an HTML element, such as `id`, `name`, `type`, or `aria-label`. |
| **Browser session** | A running browser/driver context with current page state. |
| **Candidate** | A possible DOM element that may represent a requested field. |
| **Candidate scoring** | A heuristic process that assigns a numeric value based on observed evidence. |
| **CI/CD** | Automated continuous integration and delivery/deployment pipelines. |
| **Clean Architecture** | An architectural family that keeps core policy independent from external mechanisms. |
| **Confidence threshold** | The minimum heuristic score required before a candidate is accepted. |
| **Dependency injection** | Supplying an object's dependencies from outside rather than constructing them internally. |
| **Dependency inversion** | Depending on abstractions at boundaries rather than directly on concrete low-level implementations. |
| **DOM** | The browser's live object tree representing a web document. |
| **End-to-end test** | A test that exercises a complete user-visible flow across multiple system boundaries. |
| **Explicit wait** | A bounded wait for a particular condition, such as visibility or an option becoming available. |
| **Fixture** | Reusable test setup or controlled input data. |
| **Headless browser** | A browser running without a visible user interface window. |
| **Implicit wait** | A WebDriver timeout applied globally to element lookup attempts. |
| **Integration test** | A test of multiple components working together. |
| **Locator** | A rule used to find one or more elements in a page. |
| **Mock / stub / test double** | A replacement for a dependency in tests; stubs return controlled values, mocks also verify interactions, and test double is the general category. |
| **Native select** | A standard HTML `<select>` control that Selenium's `SelectElement` can operate on. |
| **Regression test** | A test that guards against a previously fixed or required behavior breaking later. |
| **Stale element** | A Selenium element reference whose corresponding DOM node is no longer attached/current. |
| **WebDriver protocol** | The standardized command protocol used by Selenium clients to control browser drivers. |

## Repository evidence inspected

This guide was written after inspecting the tracked solution and the repository's
current README, architecture notes, plan/progress notes, configuration example,
project files and package references, CI workflow, application/domain/
infrastructure/CLI source, all substantive test files, placeholder test files,
and `TestFixtures`. Generated `bin/` and `obj/` outputs were not treated as
source evidence.

The configuration example already had an uncommitted local modification when
this task began. It was read as repository evidence and left unchanged. No
application code or existing project file was modified for this guide.

## Topics not fully verified

- The guide describes Selenium/WebDriver protocol behavior at a general
  engineering level; no packet trace or protocol-level test was run.
- This guide inspects the implementation statically. It does not claim that a
  live CloudQA run was repeated for this task.
- The repository does not implement or test screenshots, XPath/label-based
  discovery, explicit waits, browser selection beyond Chrome, retries, or a
  live end-to-end flow. Those topics are explained as general engineering
  concepts and explicitly identified as absent where relevant.
