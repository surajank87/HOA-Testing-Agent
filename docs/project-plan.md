# HOA Testing Agent — Project Architecture & Plan

---

## PROJECT OVERVIEW

A Claude Code agent running in VS Code that handles end-to-end testing for HOA (Homeowners of America) HO3 carrier on Bolt platform.

**User provides:** Requirement document + target state
**Agent delivers:** Test data Excel + graphical test results with tabbed dashboard

**Internally the agent:** creates test data (with smart reuse) → asks user for testing mode & types → generates optimized JSON request bodies (combinatorial packing) → runs C# framework (send/retrieve) → validates per testing type → collects bug evidence → produces tabbed visual report

---

## PROJECT STRUCTURE

```
hoa-agent/
│
├── .claude/                          # Claude Code agent configuration
│   ├── settings.json                 # Agent settings, permissions
│   └── commands/                     # Custom slash commands
│       ├── generate-testdata.md      # /generate-testdata {STATE}
│       ├── generate-requests.md      # /generate-requests {STATE}
│       ├── run-tests.md              # /run-tests {STATE}
│       ├── validate.md               # /validate {STATE}
│       └── full-pipeline.md          # /full-pipeline {STATE} (end-to-end)
│
├── CLAUDE.md                         # Master agent instructions (the brain)
│
├── skills/                           # Skill definitions (instructions for each phase)
│   ├── test-data-generator/
│   │   └── SKILL.md                  # How to generate state-specific test data
│   ├── request-body-generator/
│   │   └── SKILL.md                  # How to generate JSON request bodies
│   ├── relevancy-tester/
│   │   └── SKILL.md                  # How to generate relevancy test cases
│   └── result-validator/
│       └── SKILL.md                  # How to validate request/response
│
├── carrier-docs/                     # Requirement documents (input)
│   └── HOA (BriteCore) - HO3 - Standardization - V1.xlsx
│
├── sample-requests/                  # Sample request bodies from Postman (input, one per state)
│   └── SampleRequestHOA{STATE}.json
│
├── framework/                        # C# automation framework (DO NOT MODIFY)
│   └── Automation/Automation/
│       ├── MappingVerification.sln
│       ├── API Collection/           # API endpoints, env configs, request templates
│       ├── CoreLogic/                # Send & retrieve logic
│       ├── PolicyViewer/             # Selenium: PolicyViewer page objects
│       ├── Utility/                  # JSON/XML readers, helpers, reporting
│       └── TestProject/
│           ├── config.json           # Agent updates this per state
│           ├── RequestBodies/HOA/{STATE}/  # Agent puts scenario JSONs here
│           ├── Output/HOA/{STATE}/         # Framework saves results here
│           └── Execution Reports/{STATE}/  # Framework generates HTML reports
│
├── output/                           # Agent's output (delivered to user)
│   ├── test-data/                    # Persists across runs (smart reuse)
│   │   └── HOA_HO3_{STATE}_TestData.xlsx
│   └── runs/                         # Timestamped run folders
│       └── {STATE}_{YYYY-MM-DD}_{HH-MM-SS}/
│           ├── request-bodies/       # JSON scenarios for this run
│           │   ├── rel_001_...___Relevancy.json
│           │   ├── crel_001_...___ConditionalRelevancy.json
│           │   ├── map_001_...___Packed.json
│           │   ├── res_001_...___ResultPage.json
│           │   ├── uud_001_...___UUD.json
│           │   └── def_001_...___Defaults.json
│           ├── scenarios/
│           │   ├── HOA_HO3_{STATE}_TestScenarios.xlsx
│           │   └── TEST_CASE_GUIDE.md
│           ├── results/              # Carrier request/response pairs
│           │   ├── *_carrier_request.txt
│           │   ├── *_carrier_request_formatted.xml
│           │   ├── *_carrier_response.txt
│           │   ├── *_carrier_response_formatted.xml
│           │   └── *_details.json
│           ├── evidence/             # Bug evidence (FAIL items only)
│           │   ├── *_requirement.txt
│           │   ├── *_request_actual.txt
│           │   └── *_response_actual.txt
│           ├── reports/
│           │   ├── HOA_HO3_{STATE}_ValidationReport.html
│           │   └── HOA_HO3_{STATE}_ValidationSummary.xlsx
│           └── run-metadata.json
│
├── docs/                             # Project documentation
│   ├── project-context.md            # Domain knowledge & decisions
│   └── project-plan.md              # Architecture & plan (this file)
│
└── scripts/
    └── run-framework.sh              # Triggers dotnet test
```

---

## CORE FILES EXPLAINED

### CLAUDE.md (The Brain)

Master instruction file. Contains:
- Agent identity and purpose
- **Testing modes** (Focused vs Overall) and **testing types** (Relevancy, Mapping, Result Page, UUD & Defaults)
- **Combinatorial optimization** rules for packing multiple fields per request body
- **Smart test data reuse** (skip/incremental update when requirement doc unchanged)
- **Low-priority fields** and 15+ value threshold rules
- **Request body reuse** check
- **Bug evidence collection** rules for all 5 testing types
- **Run folder convention** (timestamped folders)
- **Validation rules** per testing type
- Multi-carrier request body rules, state filtering, relevancy conditions
- C# framework integration

### .claude/commands/ (Slash Commands)

| Command | What It Does |
|---|---|
| `/generate-testdata {STATE}` | Smart reuse check → generate/update/skip test data |
| `/generate-requests {STATE}` | Ask mode + types → reuse check → generate packed JSON scenarios |
| `/run-tests {STATE}` | Copy to framework → run with progress monitoring → format carrier XML |
| `/validate {STATE}` | Auto-detect types by prefix → validate → collect evidence → tabbed report |
| `/full-pipeline {STATE}` | All 4 steps with user configuration phase at the start |

### skills/ (4 Skill Instructions)

- **test-data-generator/SKILL.md:** Smart reuse (skip/incremental/fresh), state filtering, output format
- **request-body-generator/SKILL.md:** Combinatorial optimization, focused/overall mode, 4 testing types, TEST_CASE_GUIDE, low-priority field sampling
- **relevancy-tester/SKILL.md:** Field relevancy (remove field → check API error + available values) and conditional relevancy (parent-child validation)
- **result-validator/SKILL.md:** 4-type validation, XML formatting, bug evidence collection for all types, tabbed HTML report with evidence links, progress display

---

## THE PIPELINE — STEP BY STEP

### STEP 0: User Configuration (in `/full-pipeline` or `/generate-requests`)

Before generating request bodies, agent asks:

1. **Testing Mode:**
   - **Focused Testing** — user describes affected areas → exhaustive edge-case scenarios for ONLY those fields + related conditional fields
   - **Overall Testing** — packed scenarios covering ALL HOA fields across ALL blinds using combinatorial optimization (NOT blind-by-blind)

2. **Testing Types:**
   - (1) Relevancy — verify required fields and available values
   - (2) Mapping — verify field value mappings to carrier XML
   - (3) Result Page — verify coverage display in response
   - (4) UUD & Defaults — verify ineligibility rules and default values
   - (5) All of the above

### STEP 1: Generate Test Data (`/generate-testdata AZ`)

**Input:** HOA requirement doc + target state
**Output:** `output/test-data/HOA_HO3_AZ_TestData.xlsx`

**Smart Reuse:**
- If test data exists AND requirement doc unchanged → **SKIP** (reuse existing)
- If test data exists BUT requirement doc changed → **incremental update** (only apply changes, report diff)
- If test data doesn't exist → generate from scratch

**Generation Process:**
1. Read Scope sheet → verify state is in scope
2. Read Interview sheet → filter: only fields with valid XPath (`/root/...`) AND relevant for state
3. Read Lists sheet → filter by Carrier States + only values for included fields
4. Read Defaults and Extras → filter state-conditional
5. Copy UUD as-is
6. Filter Result Page Coverages by state, copy Details as-is
7. Generate Excel with ALL original columns + Reference column
8. Add hyperlinks from Interview List column → Lists sheet
9. Preserve formatting, freeze panes, auto-filter

### STEP 2: Generate Request Bodies (`/generate-requests AZ`)

**Input:** Test data Excel + sample request + user's mode/type selections
**Output:** `output/runs/{RUN}/request-bodies/` + `output/runs/{RUN}/scenarios/`

**Reuse Check:** If previous run exists for state, ask user: reuse or generate fresh?

**Combinatorial Optimization (CRITICAL):**
- Independent fields are packed together — one scenario varies fields from ALL blinds simultaneously
- Number of scenarios = MAX(value count across independent fields), not the sum
- Dependent fields (parent-child) get their combinations overlaid onto the packed matrix
- Low-priority fields (OccupationStr, EmploymentIndustry, etc.) sampled 2-3 values
- Fields with 15+ values: ask user before exhaustive testing, default to 3-5 samples

**File Prefixes by Type:**
| Type | Prefix | Example |
|---|---|---|
| Relevancy (field) | `rel_` | `rel_001_RoofType___Relevancy.json` |
| Relevancy (conditional) | `crel_` | `crel_001_Foundation_BasementType___ConditionalRelevancy.json` |
| Mapping | `map_` | `map_002_combo_01___Packed.json` |
| Result Page | `res_` | `res_001_base_coverages___ResultPage.json` |
| UUD | `uud_` | `uud_001_construction_log___UUD.json` |
| Defaults | `def_` | `def_001_base_defaults___Defaults.json` |

**Also generates:**
- `TEST_CASE_GUIDE.md` — plain-English descriptions grouped by testing area with coverage matrix
- `HOA_HO3_{STATE}_TestScenarios.xlsx` — scenario documentation

### STEP 3: Run Tests (`/run-tests AZ`)

**Input:** JSON files from run folder
**Output:** `output/runs/{RUN}/results/`

Process:
1. Identify run folder (latest or user-selected)
2. Clear old files from framework RequestBodies
3. Copy JSONs from run folder to framework
4. Update config.json for HOA state
5. Execute `dotnet test` with **progress monitoring** (count *_details.json files, display `5/25 completed`)
6. Copy results back to run folder
7. **Format carrier XML** — parse raw carrier_request.txt and carrier_response.txt, save properly indented formatted versions alongside originals

### STEP 4: Validate Results (`/validate AZ`)

**Input:** Test data + results from run folder + requirement doc (for evidence)
**Output:** `output/runs/{RUN}/reports/` + `output/runs/{RUN}/evidence/`

Process:
1. Load reference data (test data, defaults, lists, result page coverages)
2. **Format carrier XML** if not done in Step 3
3. **Auto-detect testing types** from file prefixes (`rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_`)
4. Validate each scenario per type with **progress display** (`5/25 validated`)
5. **Collect bug evidence** for every FAIL — 3 files per bug:
   - Requirement doc reference (from ORIGINAL client-provided doc, not test data)
   - What was sent (carrier request)
   - What was received (carrier response)
   - Evidence formats vary by type (relevancy, mapping, result page, UUD, defaults)
6. Generate **tabbed HTML report**:
   ```
   [Overall Dashboard] [Relevancy Check] [Mapping Verification] [Result Page] [UUD & Defaults]
   ```
   - Relevancy tab has sub-tabs: Field Relevancy | Conditional Relevancy
   - UUD tab has sub-tabs: UUD Ineligibility | Defaults Verification
   - Every FAIL row has downloadable evidence links
   - CSS-only tabs (no JavaScript)
7. Generate summary Excel (7 sheets)

---

## WHAT THE USER SEES

### Full Pipeline Interaction
```
User: /full-pipeline AZ

Agent: What testing mode do you want?
  1. Focused Testing — provide specific areas/changes
  2. Overall Testing — full regression with packed scenarios

User: 1 — roof type mapping changed, number of stories values updated

Agent: Which testing types?
  1. Relevancy  2. Mapping  3. Result Page  4. UUD & Defaults  5. All

User: 2

Agent:
📋 Phase 1: Test data for HOA HO3, state AZ...
   ✅ Test data already exists and is up-to-date. Skipping generation.

🔧 Phase 2: Generating request bodies...
   Mode: Focused (RoofType, PLNumberOfStories) | Types: Mapping
   ✅ 6 packed scenarios created (covers all values for focused fields)
   ✅ TEST_CASE_GUIDE.md saved
   ✅ Scenario doc saved

🚀 Phase 3: Running tests via C# framework...
   ⏳ 2/6 scenarios completed...
   ⏳ 4/6 scenarios completed...
   ✅ 6/6 scenarios completed
   ✅ 5 Success, 1 Declined, 0 Failed
   ✅ Carrier XML formatted

📊 Phase 4: Validating results...
   ⏳ 3/6 scenarios validated...
   ✅ 6/6 scenarios validated
   ✅ 98.2% overall pass rate
   📁 Evidence collected for 2 FAIL items

🏁 Pipeline complete for AZ
   📁 Run folder: output/runs/AZ_2026-04-03_10-30-00/
   📊 6 scenarios | 98.2% pass rate
   🧪 Types tested: Mapping
   📈 Report: output/runs/AZ_2026-04-03_10-30-00/reports/HOA_HO3_AZ_ValidationReport.html
```

### Output Delivered to User
1. **Test Data Excel** — all columns preserved + Reference column (reused when unchanged)
2. **TEST_CASE_GUIDE.md** — plain-English descriptions with coverage matrix
3. **Formatted Carrier XML** — properly indented request/response (not raw)
4. **Tabbed HTML Dashboard** — Overall, Relevancy, Mapping, Result Page, UUD & Defaults tabs with evidence links
5. **Bug Evidence Files** — requirement doc reference + request + response for each FAIL
6. **Summary Excel** — 7 sheets covering all testing types
7. **Run metadata** — JSON with mode, types, timestamp, scenario count

---

## KEY OPTIMIZATIONS

| Optimization | Description |
|---|---|
| **Smart test data reuse** | Skip generation if requirement doc unchanged; incremental update if only a few changes |
| **Request body reuse check** | Ask user before regenerating if previous run exists |
| **Combinatorial packing** | Pack independent fields across ALL blinds into each scenario. Scenarios = MAX(values), not SUM |
| **Low-priority field sampling** | OccupationStr, EmploymentIndustry, carrier lists: sample 2-3 values only |
| **15+ value threshold** | Ask user before exhaustively testing fields with many values |
| **Focused testing mode** | Test only affected fields with exhaustive edge cases |
| **4 testing types** | Relevancy, Mapping, Result Page, UUD & Defaults — each with distinct validation logic |
| **Bug evidence per type** | 3 evidence files per FAIL from requirement doc, request, and response |
| **Timestamped run folders** | Preserve history across runs |
| **Progress monitoring** | Granular `5/25 completed` display during execution and validation |
| **Formatted carrier XML** | Pretty-printed alongside raw files |

---

## STATUS

All agent components are built and ready:
- CLAUDE.md with full HOA domain knowledge + all optimizations
- 4 skill definitions (test-data-generator, request-body-generator, relevancy-tester, result-validator)
- 5 slash commands (generate-testdata, generate-requests, run-tests, validate, full-pipeline)
- C# framework uploaded and integrated (DO NOT MODIFY)

**Ready to run:** `/generate-testdata TX` or `/full-pipeline TX`
