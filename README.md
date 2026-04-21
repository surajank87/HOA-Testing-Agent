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
   - Name as `SampleRequestHOA{STATE}.json` (e.g., `SampleRequestHOATX.json`)
   - Export from Postman for each state you want to test
4. **C# framework** is in `framework/Automation/Automation/`:
   - Solution: `MappingVerification.sln`
   - Ensure `dotnet test` works from command line
5. **Start Claude Code** in VS Code

## Usage

### Full Pipeline (Recommended)
```
/full-pipeline TX
```
Runs everything: user configuration → test data (smart reuse) → request bodies (packed scenarios) → send tests → validate → tabbed report with bug evidence.

### Individual Steps
```
/generate-testdata TX     # Step 1: Generate/reuse test data Excel
/generate-requests TX     # Step 2: Generate packed JSON request bodies
/run-tests TX             # Step 3: Send via framework, collect & format results
/validate TX              # Step 4: Validate per type, collect evidence, produce report
```

## Testing Modes

- **Focused Testing** — Provide specific areas/changes (e.g., "roof type mapping changed") → agent tests ONLY those fields exhaustively
- **Overall Testing** — Full regression with combinatorially packed scenarios covering all HOA fields

## Testing Types

| Type | What It Validates |
|---|---|
| **Relevancy** | Required fields and their available values match requirement doc |
| **Mapping** | Field values map correctly from Bolt Enum to carrier XML |
| **Result Page** | Coverages display correctly in API response |
| **UUD & Defaults** | Ineligibility rules trigger decline; all 20 defaults present |

## Output

All output is in timestamped run folders: `output/runs/{STATE}_{YYYY-MM-DD}_{HH-MM-SS}/`

| What | Where |
|---|---|
| Test data Excel | `output/test-data/HOA_HO3_{STATE}_TestData.xlsx` |
| JSON request bodies | `output/runs/{RUN}/request-bodies/` |
| Test case guide | `output/runs/{RUN}/scenarios/TEST_CASE_GUIDE.md` |
| Scenario documentation | `output/runs/{RUN}/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` |
| Results (raw + formatted) | `output/runs/{RUN}/results/` |
| Bug evidence (FAIL items) | `output/runs/{RUN}/evidence/` |
| Tabbed HTML report | `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html` |
| Summary Excel | `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx` |
| Run metadata | `output/runs/{RUN}/run-metadata.json` |

## Project Structure

```
hoa-agent/
├── CLAUDE.md                    # Agent brain (auto-read by Claude Code)
├── .claude/                     # Agent configuration
│   ├── settings.json
│   └── commands/                # 5 slash commands
├── skills/                      # Skill instructions (SKILL.md per phase)
│   ├── test-data-generator/     # Smart reuse, state filtering, Excel generation
│   ├── request-body-generator/  # Combinatorial packing, 4 types, TEST_CASE_GUIDE
│   ├── relevancy-tester/        # Field & conditional relevancy test generation
│   └── result-validator/        # 4-type validation, evidence collection, tabbed report
├── carrier-docs/                # HOA requirement document (client-provided)
├── sample-requests/             # Sample JSON request bodies (from Postman)
├── framework/                   # C# automation framework (DO NOT MODIFY)
│   └── Automation/Automation/
├── output/                      # All generated output
│   ├── test-data/               # Persists across runs (smart reuse)
│   └── runs/                    # Timestamped run folders
├── scripts/                     # Utility scripts
└── docs/                        # Context and planning documents
```
