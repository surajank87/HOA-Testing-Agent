# /validate

Validate carrier request/response pairs against test data and produce a graphical report.

## Usage
```
/validate {STATE_CODE}
```
Example: `/validate AZ`

## Prerequisites
- Test data must exist: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- A run folder must exist with results in `output/runs/{STATE}_{timestamp}/results/`

If results don't exist, run `/run-tests {STATE}` first.

**Finding the run folder:** Use the most recent run folder for the given state in `output/runs/`. If multiple exist, ask the user which one to use.

## What this does

### Step 1: Identify Run Folder
- Find the latest run folder for the state: `output/runs/{STATE}_*/`
- If multiple exist, show the list and ask the user which one
- Read `run-metadata.json` to determine testing types and mode used

### Step 2: Load Reference Data
1. Reads the test data Excel (expected values, XPaths, mappings)
2. Reads Defaults and Extras from the requirement doc
3. Reads Result Page Coverages and Details from the requirement doc

### Step 3: Validate with Progress Display
For each result in `output/runs/{RUN}/results/`:
1. **Auto-detect testing type** by file prefix (`rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_`)
2. Run the appropriate validation logic per type:
   - **Relevancy (`rel_`, `crel_`):** Check for validation error, parse available values, compare with doc
   - **Mapping (`map_`):** Extract values from carrier request XML using XPaths, compare with expected mappings, check Defaults
   - **Result Page (`res_`):** Check response coverages and premium calculation
   - **UUD (`uud_`):** Verify decline/ineligibility status
   - **Defaults (`def_`):** Verify all 20 defaults in carrier request XML
3. **Display progress after each scenario:**
```
📊 Validating results...
   ⏳ 1/25 validated — rel_001_RoofType (Relevancy)
   ⏳ 5/25 validated — map_004_roof_metal___Structure (Mapping)
   ⏳ 12/25 validated — res_002_high_liability___ResultPage (Result Page)
   ...
   ✅ 25/25 scenarios validated
```

### Step 4: Generate HTML Report with Tabs
Create a tabbed HTML report at `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html`:

**Tab structure:**
```
[Overall Dashboard] [Relevancy Check] [Mapping Verification] [Result Page] [UUD & Defaults]
```

- **Overall Dashboard:** Aggregate stats, per-type pass rates, quick links to each tab
- **Relevancy Check** (sub-tabs: Field Relevancy | Conditional Relevancy):
  - Field table: Field Name | Error Returned? | Expected Values (doc) | API Values | Match? | Status
  - Conditional table: Parent | Value | Child | Child Omitted? | Error for Child? | Status
- **Mapping Verification:** Per-scenario expandable details, per-blind pass rates, failed items summary
- **Result Page:** Coverage Name | Sent Value | Response Value | Match? | Status
- **UUD & Defaults** (sub-tabs: UUD Ineligibility | Defaults Verification):
  - UUD table: Scenario | Field | Value | Declined? | Error Message | Status
  - Defaults matrix: Default Name x Scenario → Present/Absent with value

Only show tabs for testing types that were actually run (check file prefixes or run-metadata.json).

### Step 5: Generate Summary Excel
Generate detailed Excel at `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx`

### Step 6: Report Completion
```
✅ Validation complete
   📁 Run folder: output/runs/{RUN}/
   📊 Overall pass rate: {P}%
   📈 HTML Report: output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html
```

## Rules
- Read the skill at `skills/result-validator/SKILL.md` before starting
- Compare values exactly — string matching
- "Do not send" + value not found in XML = PASS (correctly omitted)
- Mark uncertain results as WARNING, not PASS
- Include trace references for every check
- Auto-detect testing types from file prefixes — do not require user to re-specify

## Output
- `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html` — tabbed graphical dashboard
- `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx` — detailed Excel report
