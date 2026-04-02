# HOA Testing Agent — Project Architecture & Plan

---

## PROJECT OVERVIEW

A Claude Code agent running in VS Code that handles end-to-end testing for HOA (Homeowners of America) HO3 carrier on Bolt platform.

**User provides:** Requirement document + target state
**Agent delivers:** Test data Excel + graphical test results

**Internally the agent:** creates test data → generates JSON request bodies → runs C# framework (send/retrieve) → validates request/response mapping → produces visual results

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
│   └── result-validator/
│       └── SKILL.md                  # How to validate request/response
│
├── carrier-docs/                     # Requirement documents (input)
│   └── HOA (BriteCore) - HO3 - Standardization - V1.xlsx
│
├── sample-requests/                  # Sample request bodies from Postman (input, one per state)
│   └── SampleRequestHOAIL.json      # IL sample — user provides per state
│
├── framework/                        # C# automation framework (DO NOT MODIFY)
│   └── Automation/Automation/
│       ├── MappingVerification.sln
│       ├── API Collection/           # API endpoints, env configs, request templates
│       ├── CoreLogic/                # Send & retrieve logic
│       ├── PolicyViewer/             # Selenium: PolicyViewer page objects
│       ├── Utility/                  # JSON/XML readers, helpers, reporting
│       └── TestProject/
│           ├── config.json           # ← Agent updates this per state
│           ├── RequestBodies/HOA/{STATE}/  # ← Agent puts scenario JSONs here
│           ├── Output/HOA/{STATE}/         # ← Framework saves results here
│           └── Execution Reports/{STATE}/  # ← Framework generates HTML reports
│
├── output/                           # Agent's output (delivered to user)
│   ├── test-data/
│   │   └── HOA_HO3_{STATE}_TestData.xlsx
│   ├── request-bodies/{STATE}/       # Generated JSON scenarios per state
│   │   ├── req_001_base___Start.json
│   │   ├── req_002_...___Home.json
│   │   └── ...
│   ├── scenarios/
│   │   └── HOA_HO3_{STATE}_TestScenarios.xlsx
│   ├── results/{STATE}/              # Copied from framework output
│   │   ├── {scenario}_carrier_request.txt
│   │   ├── {scenario}_carrier_response.txt
│   │   ├── {scenario}_details.json
│   │   └── {scenario}_request.json
│   └── reports/
│       ├── HOA_HO3_{STATE}_ValidationReport.html
│       └── HOA_HO3_{STATE}_ValidationSummary.xlsx
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

This is the master instruction file. Claude Code reads this automatically. It contains:
- Agent identity and purpose
- HOA carrier domain knowledge
- The complete pipeline logic
- Rules for test data generation, request body creation, and validation
- How to interact with the C# framework
- Output format specifications

### .claude/commands/ (Slash Commands)

Each command triggers a specific phase of the pipeline:

| Command | What It Does |
|---|---|
| `/generate-testdata {STATE}` | Reads HOA requirement doc → generates state-filtered test data Excel |
| `/generate-requests {STATE}` | Reads test data + sample request → generates JSON scenario files |
| `/run-tests {STATE}` | Copies JSONs to framework → updates config → triggers dotnet test → collects results |
| `/validate {STATE}` | Reads results (request/response pairs) → validates mappings → generates report |
| `/full-pipeline {STATE}` | Runs all 4 steps in sequence |

### skills/ (Skill Instructions)

Each skill has a SKILL.md with detailed step-by-step instructions the agent follows:

- **test-data-generator/SKILL.md:** State filtering, Interview/Lists sheet parsing, output format
- **request-body-generator/SKILL.md:** Field classification, conditional rules, scenario design, JSON generation
- **result-validator/SKILL.md:** XPath extraction, mapping comparison, report generation

---

## THE PIPELINE — STEP BY STEP

### STEP 1: Generate Test Data (`/generate-testdata AZ`)

**Input:** HOA requirement doc + target state
**Output:** `output/test-data/HOA_HO3_AZ_TestData.xlsx`

Process:
1. Read Scope sheet → verify state is in scope
2. Read Interview sheet → filter by State column for target state
3. Read Lists sheet → filter by Carrier States column for target state
4. Apply field-to-list relevancy (if field excluded, its list values excluded)
5. Read Defaults and Extras → note state-conditional defaults
6. Generate Excel with ALL original columns + Reference column
7. Separate sheets in output: one per Blind (Start, Home, Structure, Features, Policy, Applicant) + Lists + Defaults + ResultPage

**Key HOA-Specific Logic:**
- 52 fields have XPaths (HOA sends these to carrier)
- 117 fields have no XPath (Bolt display only or other carriers — include in test data but mark XPath as blank)
- State patterns: "state AZ, VA" → only AZ, VA. "NOT USED FOR VA" → all except VA. "Not in MO" → all except MO. Blank → all states.
- Lists Carrier States: "AZ, VA" → only. "Not in TX" → all except TX. Blank → all states.

### STEP 2: Generate Request Bodies (`/generate-requests AZ`)

**Input:** Test data Excel + sample request body (`sample-requests/SampleRequestHOAAZ.json`)
**Output:** `output/request-bodies/AZ/` folder + `output/scenarios/HOA_HO3_AZ_TestScenarios.xlsx`

Process:
1. Parse sample request body
2. Cross-reference with test data:
   - Fields in request WITH XPath in test data → HOA-specific, we modify these
   - Fields in request WITHOUT XPath or NOT in test data → OTHER CARRIER fields, KEEP AS-IS
   - Fields in test data with XPath but NOT in request → may need to add (check conditions)
3. Identify conditional rules (add/remove) from Relevancy Condition column
4. Design scenarios by blind
5. Generate JSON files + scenario document

**CRITICAL MULTI-CARRIER RULE:**
The request body serves ALL carriers on Bolt. The agent ONLY modifies fields that appear in HOA's Interview sheet with a valid XPath. All other fields remain exactly as they are in the sample request. This ensures other carriers still get valid data.

**HOA-Specific Scenario Examples:**
- PLTypeOfDwelling variations (only for AZ, VA per state filter)
- PLConstructionType variations (watch UUD: Log, Asbestos, EFIS = ineligible)
- PLNumberOfStories variations (watch UUD: 3.5, 4 = ineligible)
- PLPersonalLiability value cycling
- PLAllPerilsDeductible value cycling (TX has special percentage values)
- WindstormDeductible (not in FL) vs AnnualHurricaneDed (FL only)
- Mitigation fields (FL only: MitCreditForm, MitWindowOpening, etc.)
- ViciousExoticAnimals (not used for VA)
- PLNumberOfUnits (only AZ & IL)
- CreditCheckPermission (not in CA vs special CA version)
- Mailing address different (triggers child fields)
- Basement type (triggered by TypeOfFoundation = Basement)

### STEP 3: Run Tests (`/run-tests AZ`)

**Input:** JSON files in `output/request-bodies/AZ/`
**Output:** `output/results/AZ/` with carrier request/response pairs

Process:
1. Clear old files from `framework/Automation/Automation/TestProject/RequestBodies/HOA/AZ/`
2. Copy JSONs from `output/request-bodies/AZ/` to `framework/Automation/Automation/TestProject/RequestBodies/HOA/AZ/`
3. Update `framework/Automation/Automation/TestProject/config.json` for HOA:
   ```json
   {
     "Tenant": "Unify",
     "LOB": "PersonalHome",
     "Carrier": "HOA",
     "PersonalLineOrCommercialLine": "PersonalLine",
     "Environment": "QA",
     "SubTenant": "MarketsLib",
     "CarrierRequestFormat": "xml",
     "State": "AZ"
   }
   ```
4. Execute: `dotnet test "framework/Automation/Automation/MappingVerification.sln"`
5. Copy results from `framework/Automation/Automation/TestProject/Output/HOA/AZ/` to `output/results/AZ/`
6. Report: X requests sent, Y succeeded, Z failed

### STEP 4: Validate Results (`/validate AZ`)

**Input:** Test data Excel + results folder `output/results/AZ/` (carrier request/response pairs)
**Output:** `output/reports/HOA_HO3_AZ_ValidationReport.html` + summary Excel

Process:
1. Load test data (expected values, XPaths)
2. Load Defaults and Extras (hardcoded values to verify)
3. For each request/response pair in `output/results/AZ/`:

   **Request Validation:**
   - For each HOA field with XPath: extract value from carrier request XML using XPath
   - Compare with expected mapping from test data
   - Check Defaults and Extras are present
   - "Do not send" fields → verify they're absent from XML
   - Track: PASS / FAIL / WARNING per field

   **Response Validation:**
   - Check Result Page Coverages: dwelling, other structures, personal property, liability, medical payments, deductibles
   - Check Result Page Details: premium (annualTotalUsd + annualFeesUsd), success/fail indication
   - Track: PASS / FAIL / WARNING per coverage

4. Generate graphical HTML report:
   - Dashboard with pass/fail pie chart
   - Per-request breakdown
   - Per-field heatmap
   - Failed items highlighted with expected vs actual
   - Trend across scenarios (which blinds have most failures)

5. Generate summary Excel:
   - Sheet 1: Overall summary (pass/fail counts per scenario)
   - Sheet 2: Detailed field-by-field results
   - Sheet 3: Failed items only (for quick action)

---

## HOA-SPECIFIC KNOWLEDGE (Embedded in CLAUDE.md)

### Carrier Details
- **Name:** Homeowners of America (HOA)
- **Platform:** BriteCore
- **LOB:** HO3 (Homeowners)
- **Request Format:** XML
- **XPath root:** `/root/...`
- **Total fields HOA cares about:** 52 (those with XPaths)
- **Defaults sent every request:** 20 (from Defaults and Extras sheet)

### State Filtering Rules
Interview `State` column:
| Pattern | Meaning |
|---|---|
| Blank/None | All states |
| "state AZ, VA" | Only AZ and VA |
| "FL" | Only FL |
| "NOT USED FOR VA" | All except VA |
| "Not in MO" | All except MO |
| "Not in FL" | All except FL |
| "Not in CA" | All except CA |
| "state AZ" | Only AZ |
| "state AZ & IL" | Only AZ and IL |

Lists `Carrier States` column:
| Pattern | Meaning |
|---|---|
| Blank/None | All states |
| "AZ, VA" | Only AZ and VA |
| "Not in TX" / "not in TX" | All except TX |
| "CA, FL" | Only CA and FL |
| "TX" | Only TX |
| "IN,NY" | Only IN and NY |
| "VA" | Only VA |
| "CA,TX" | Only CA and TX |

### Ineligibility Rules (UUD)
1. PLConstructionType in {Log, Asbestos, EFIS} → ineligible
2. PLNumberOfStories in {3.5, 4} → ineligible

### Defaults and Extras (Always Sent)
20 hardcoded values including:
- carrier = "HOMEOWNERS_OF_AMERICA"
- protectionClass = 1
- replacementCostOnPersonalProperty = true
- waterDamage = true
- seasonalDwelling = false (not for VA)
- roofImpactRating = None (not for AZ)
- Flooring = "Hardwood" (VA, AZ only)
- roofInstallationDate = derived from PLRoofUpdated/RoofUpdatedYear
- mineSubsidence = conditional (IL specific counties)
- Minimum deductible rules based on PersonalLineReplacementCost

### Multi-Carrier Request Body Rule
The JSON request body contains fields for ALL carriers on Bolt. The agent ONLY modifies:
- Fields that appear in HOA's Interview sheet WITH a valid XPath
- Defaults and Extras values

All other fields in the request body are for other carriers and must remain EXACTLY as-is.

---

## WHAT THE USER SEES

### Simple Interaction
```
User: /full-pipeline AZ

Agent: 
📋 Phase 1: Generating test data for HOA HO3, state AZ...
   ✅ 142 fields extracted (48 relevant for AZ)
   ✅ 38 list tables with 890 state-filtered values
   ✅ Test data saved: output/test-data/HOA_HO3_AZ_TestData.xlsx

🔧 Phase 2: Generating request bodies...
   ✅ 35 test scenarios created across 6 blinds
   ✅ 35 JSON files saved to output/request-bodies/AZ/
   ✅ Scenario doc saved: output/scenarios/HOA_HO3_AZ_TestScenarios.xlsx

🚀 Phase 3: Running tests via C# framework...
   ✅ 35 requests sent to HOA via Bolt QA
   ✅ 30 Success, 3 Declined, 2 Failed
   ✅ Results saved to output/results/AZ/

📊 Phase 4: Validating results...
   ✅ 1,820 field checks performed
   ✅ 1,756 PASSED (96.5%)
   ✅ 42 FAILED (2.3%)
   ✅ 22 WARNINGS (1.2%)
   
   📈 [Interactive validation dashboard displayed]
   📁 Report saved: output/reports/HOA_HO3_AZ_ValidationReport.html
```

### Output Delivered to User
1. **Test Data Excel** — all columns preserved + Reference column
2. **Graphical Test Results** — HTML dashboard with:
   - Overall pass/fail rate (pie chart)
   - Per-blind breakdown (bar chart)
   - Per-scenario results (table with color coding)
   - Failed items detail (expandable sections)
   - Trend analysis across scenarios

---

## STATUS

All agent components are built and ready:
- CLAUDE.md with full HOA domain knowledge
- 3 skill definitions (test-data-generator, request-body-generator, result-validator)
- 5 slash commands (generate-testdata, generate-requests, run-tests, validate, full-pipeline)
- C# framework uploaded and integrated (DO NOT MODIFY)
- Sample request for IL uploaded

**Ready to run:** `/generate-testdata IL` or `/full-pipeline IL`
