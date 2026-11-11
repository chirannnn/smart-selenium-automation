# Architecture Notes

The framework separates domain models from browser-specific concerns.

- Domain owns field metadata, candidate representations, and domain-level validation.
- Application owns orchestration and browser contracts.
- Infrastructure implements Selenium sessions, candidate discovery, and scoring.
- CLI binds configuration and runs the automation flow.

The locator engine evaluates multiple signals: semantic labels, aria metadata, tag type, input type, visible state, and candidate context.

This allows the runner to remain resilient when IDs, names, or CSS structure change as long as the field meaning remains present in the DOM.
