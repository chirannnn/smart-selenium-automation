# Implementation Status

This document records the current repository scope. It distinguishes
implemented behavior from known gaps and roadmap items.

## Current implementation

- **CLI:** Loads optional JSON configuration and environment variables, builds
  the dependency-injection container, starts one automation request, prints
  field results, and returns a success/failure exit code.
- **Browser:** Creates a ChromeDriver session with configurable headless mode,
  page-load timeout, and an implicit wait derived from the element-wait
  setting.
- **Locator:** Discovers native `input`, `select`, and `textarea` elements;
  scores candidates using control type, token overlap, selected attributes,
  visibility, and enabled state; and applies per-operation thresholds.
- **Field actions:** Enters and verifies First Name, selects a Gender radio
  choice, and selects and verifies a native State dropdown option.
- **Diagnostics:** Writes browser startup, candidate-selection, ambiguity,
  and browser-quit warning messages through console logging.

## Automated tests

The test projects currently cover:

- Domain label/keyword normalization and interaction-result factories.
- Orchestrator delegation using a hand-written service stub.
- A First Name scoring example and State-versus-Country candidate ranking.

The CloudQA integration test is skipped and does not execute browser
automation. The other `UnitTest1.cs` files are template tests without
meaningful assertions. The CI workflow restores, builds, and runs tests on
.NET 8; it does not install a browser or run an end-to-end test.

## Known gaps

- Radio selection and verification are not tightly bound to the scored
  candidate; verification can accept a matching value without confirming the
  option is selected.
- Candidate discovery does not inspect associated visible labels or DOM
  context and does not support custom controls.
- Explicit condition-based waits, stale-element recovery, screenshots, and
  browser log capture are not implemented.
- `ConfidenceThreshold` and `BrowserType` are read as settings but do not
  control the effective candidate threshold or browser implementation.
- CLI flags are parsed as fallbacks; existing configuration values take
  precedence.
- The form service is a singleton with mutable session state, and navigation
  occurs before the cleanup `finally` block.

See the [Engineering Roadmap](PLAN.md) for prioritized improvement proposals
and [Architecture](docs/architecture.md) for component boundaries and runtime
flow.

## Updating this status

Update this page when behavior changes, and keep each status statement tied to
the implementation and tests. Do not mark an item complete based only on a
design proposal or a skipped test.
