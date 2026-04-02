# HOA Testing Agent

End-to-end testing agent for **HOA (Homeowners of America)** HO3 carrier on the **Bolt** platform.

## Setup

### Prerequisites
- VS Code with Claude Code extension
- .NET 8.0 SDK (for the C# framework)
- Chrome browser (for Selenium PolicyViewer automation)

### First-Time Setup

1. **Open this folder in VS Code**
2. **HOA requirement document** is in `carrier-docs/`:
   - `HOA (BriteCore) - HO3 - Standardization - V1.xlsx`
3. **Place sample request bodies** in `sample-requests/`:
   - Name as `SampleRequestHOA{STATE}.json` (e.g., `SampleRequestHOAIL.json`)
   - Export from Postman for each state you want to test
4. **C# framework** is in `framework/Automation/Automation/`:
   - Solution: `MappingVerification.sln`
   - Ensure `dotnet test` works from command line
5. **Start Claude Code** in VS Code

## Usage

### Full Pipeline (Recommended)
```
/full-pipeline IL
```
Runs everything: test data → request bodies → send tests → validate → report.

### Individual Steps
```
/generate-testdata IL     # Step 1: Generate test data Excel
/generate-requests IL     # Step 2: Generate JSON request bodies
/run-tests IL             # Step 3: Send via framework, collect results
/validate IL              # Step 4: Validate and produce report
```

## Output

| What | Where |
|---|---|
| Test data Excel | `output/test-data/HOA_HO3_{STATE}_TestData.xlsx` |
| JSON request bodies | `output/request-bodies/{STATE}/` |
| Scenario documentation | `output/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` |
| Framework results | `output/results/{STATE}/` |
| Validation reports | `output/reports/HOA_HO3_{STATE}_ValidationReport.html` |

## Project Structure

```
hoa-agent/
├── CLAUDE.md                    # Agent brain (auto-read by Claude Code)
├── .claude/                     # Agent configuration
│   ├── settings.json
│   └── commands/                # Slash commands
├── skills/                      # Skill instructions (SKILL.md per phase)
│   ├── test-data-generator/
│   ├── request-body-generator/
│   └── result-validator/
├── carrier-docs/                # HOA requirement document
├── sample-requests/             # Sample JSON request bodies (from Postman)
├── framework/                   # C# automation framework (DO NOT MODIFY)
│   └── Automation/Automation/
│       ├── MappingVerification.sln
│       └── TestProject/
│           ├── config.json
│           ├── RequestBodies/HOA/{STATE}/  # Agent puts JSONs here
│           └── Output/HOA/{STATE}/        # Framework saves results here
├── output/                      # All generated output
├── scripts/                     # Utility scripts
└── docs/                        # Context and planning documents
```
