# SmartSelenium

SmartSelenium is a resilient Selenium automation framework for the CloudQA practice form.

## Goals

- Fill the First Name field
- Select a Gender radio option
- Select a State from the dropdown
- Use resilient, strategy-based candidate selection instead of brittle fixed selectors

## Architecture

- SmartSelenium.Domain: field models, candidate models, and domain exceptions
- SmartSelenium.Application: orchestration, browser abstraction, and locator contracts
- SmartSelenium.Infrastructure: Selenium implementation and candidate scoring
- SmartSelenium.Cli: command-line runner and configuration binding

## Run

1. Install .NET 8 SDK.
2. Restore packages:
   
   dotnet restore

3. Run the CLI with defaults:

   dotnet run --project src/SmartSelenium.Cli -- --url "https://app.cloudqa.io/home/AutomationPracticeForm" --first-name "John" --gender "Male" --state "Argentina"

4. Or update appsettings example and copy it to the output path before execution.

## Example configuration

See [config/appsettings.example.json](config/appsettings.example.json).
