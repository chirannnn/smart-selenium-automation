# Engineering Roadmap

This roadmap describes the next improvements for SmartSelenium based on the
current implementation. The existing CLI workflow is implemented; the items
below are proposed work, not claims about current capabilities.

## Priorities

### 1. Make candidate selection and interaction refer to the same element

**Why:** The locator engine ranks `LocatorCandidate` values, but interaction
later reconstructs a CSS selector from ID, name, or tag. In Gender automation,
the scored candidate is not used for the click. A broad or duplicate selector
can therefore act on an element other than the one the scoring logic favored.

**Outcome:**

- Represent a selected target in a way that uniquely identifies the element
  for the interaction.
- Match radio options to the requested choice within the correct group.
- Fail with useful diagnostics when a unique target cannot be established.
- Add regression tests for duplicate names, duplicate labels, and ambiguous
  matches.

### 2. Verify control state precisely

**Why:** A field operation should report success only when the intended
element's state is confirmed. Current radio verification can accept a matching
value without confirming that the radio is selected.

**Outcome:**

- Verify `Selected`/checked state for the exact radio option.
- Verify the selected dropdown option by both its value and expected display
  text where appropriate.
- Test incorrect, unchanged, missing, and duplicate option cases.

### 3. Synchronize on browser conditions

**Why:** The current implementation configures an implicit wait but does not
wait explicitly for visibility, enablement, or a dynamically loaded option.

**Outcome:**

- Introduce bounded waits for the specific state needed before each
  interaction.
- Cover delayed controls and delayed dropdown options with deterministic tests.
- Keep wait behavior consistent and avoid arbitrary sleeps.

### 4. Improve browser-session ownership and cleanup

**Why:** The form service is registered as a singleton and stores a mutable
session field. Navigation also occurs before its `try/finally` block.

**Outcome:**

- Make a browser session local to a single execution rather than shared
  mutable service state.
- Ensure session disposal covers browser creation, navigation, and every
  interaction failure path.
- Add tests for cancellation, navigation failure, repeated execution, and
  concurrent calls where supported.

### 5. Complete locator evidence and settings behavior

**Why:** Discovery currently queries native `input`, `select`, and `textarea`
elements and reads selected attributes. It does not resolve visible label
associations or nearby context. `BrowserSettings.ConfidenceThreshold` is
loaded but field thresholds are hard-coded.

**Outcome:**

- Add only evidence sources that improve field identification for supported
  pages, such as correctly associated labels and accessible names.
- Use the configured confidence threshold or remove the unused setting.
- Make ambiguity handling scoped to the candidates being compared and expose
  enough evidence to diagnose rejected candidates.
- Test resilience against documented DOM variations without claiming
  robustness to arbitrary redesigns.

### 6. Strengthen configuration and command-line behavior

**Why:** CLI values are currently fallbacks to JSON/environment configuration,
and the command-line configuration package is referenced without a command-line
provider. The browser type setting also does not select a driver.

**Outcome:**

- Define and document an explicit precedence order for defaults, JSON,
  environment variables, and CLI flags.
- Validate malformed URLs, timeouts, thresholds, and field values before
  starting Chrome.
- Either implement browser selection or clearly constrain the setting to
  Chrome.

### 7. Add browser integration coverage and diagnostics

**Why:** The current CloudQA integration test is skipped, and existing tests
do not interact with Selenium or a test page.

**Outcome:**

- Create a local deterministic HTML fixture for browser adapter tests.
- Exercise text entry, radio selection, dropdown selection, waits, and
  failure cases against the fixture.
- Keep external CloudQA smoke coverage optional and environment-gated.
- Capture useful failure diagnostics (for example, URL, candidate evidence,
  and optionally screenshots) without leaking sensitive page data.

## Completion criteria for a release-quality workflow

- Documented configuration precedence matches executable behavior.
- Each requested interaction is uniquely targeted and state-verified.
- Ambiguity and missing elements fail with actionable diagnostics.
- Browser sessions are reliably cleaned up after all failure paths.
- Unit and local browser integration tests cover success and representative
  failure cases.
- CI runs the deterministic browser tests in a known browser environment.
- README and architecture documentation reflect implemented behavior.

## Scope principle

Prioritize correctness and diagnosability over adding more locator signals.
Each new strategy should have a defined evidence source, a test showing when it
helps, and a safe failure mode when it cannot identify a unique control.
