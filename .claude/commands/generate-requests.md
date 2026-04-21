# /generate-requests

Generate multiple JSON request body scenarios from test data and sample request.

## Usage
```
/generate-requests {STATE_CODE}
```
Example: `/generate-requests AZ`

## Prerequisites
- Test data must exist: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- Sample request must exist: `sample-requests/SampleRequestHOA{STATE}.json`

If test data doesn't exist, run `/generate-testdata {STATE}` first.
If sample request doesn't exist, ask the user to provide one from Postman.

## What this does

### Step 0: Determine Testing Mode

Before generating any scenarios, ASK the user:

> **What testing mode do you want?**
> 1. **Focused Testing** — You provide specific areas/changes (e.g., "roof type mapping changed, number of stories values updated") and I generate exhaustive edge-case scenarios for ONLY those fields and their related/conditional fields.
> 2. **Overall Testing** — I generate packed scenarios covering all HOA fields across all blinds (full regression with combinatorial optimization).

If the user chooses **Focused**: ask them to describe what changed or which fields/areas they want to test. Map their description to HOA fields from the test data, identify related parent/child conditional fields, and generate exhaustive scenarios only for those.

If the user chooses **Overall**: proceed with packed scenarios covering all HOA fields across all blinds.

Note: In all these cases scoped or relevant fileds and its all the possible values must be tested throughly i.e No value should miss.

### Step 0.5: Select Testing Types

Then ASK the user:

> **Which testing types do you want to run?**
> 1. **Relevancy Testing** — Verify required fields and available values
> 2. **Mapping Testing** — Verify field value mappings to carrier XML
> 3. **Result Page Testing** — Verify coverage display in response
> 4. **UUD & Defaults** — Verify ineligibility rules and default values
> 5. **All of the above**
>
> You can select multiple (e.g., "1, 2" or "all").

### Step 0.7: Check for Existing Request Bodies

Check if request bodies already exist for this state in a previous run folder (`output/runs/{STATE}_*/request-bodies/`). If found, ask the user:
> "I found existing request bodies for {STATE} from run {previous_timestamp}. Do you want to reuse those, or generate fresh ones for this session?"

If reuse → copy to new run folder, skip Steps 2-7. If fresh → proceed normally.

### Step 1: Create Run Folder

Create a timestamped run folder:
```
output/runs/{STATE}_{YYYY-MM-DD}_{HH-MM-SS}/
```
Create subdirectories: `request-bodies/`, `scenarios/`, `results/`, `reports/`

Save `run-metadata.json` with: state, timestamp, mode (focused/overall), focus areas (if focused), testing types selected, environment.

### Step 2-7: Generate Scenarios

1. Reads the generated test data Excel
2. Reads the sample request body from `sample-requests/`
3. Cross-references: identifies HOA-specific fields (have XPath) vs other-carrier fields (keep as-is)
4. Identifies all conditional rules (add/remove fields based on relevancy conditions)
5. Based on selected testing types, generates appropriate scenario files:
   - Relevancy → delegate to `skills/relevancy-tester/SKILL.md`
   - Mapping → use `skills/request-body-generator/SKILL.md`
   - Result Page → use `skills/request-body-generator/SKILL.md` (result page section)
   - UUD & Defaults → use `skills/request-body-generator/SKILL.md` (UUD/defaults section)
6. Generates scenario documentation Excel
7. Generates `TEST_CASE_GUIDE.md` — plain-English test case descriptions grouped by testing area

## Rules
- Read the relevant skill files before starting
- ONLY modify fields that have XPaths in HOA's Interview sheet
- Keep ALL other fields exactly as-is (they serve other carriers)
- None/NoCoverage = real values to SEND, not the same as removing
- Comment out fields ONLY when a specific Relevancy Condition disables them
- Add fields ONLY when a specific Relevancy Condition enables them
- Use file prefixes by type: `rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_`

## Output
- `output/runs/{RUN}/request-bodies/` — all JSON scenario files
- `output/runs/{RUN}/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` — scenario documentation
- `output/runs/{RUN}/scenarios/TEST_CASE_GUIDE.md` — plain-English test case guide
- `output/runs/{RUN}/run-metadata.json` — run configuration
