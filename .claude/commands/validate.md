# /validate

Validate carrier request/response pairs against test data and produce a graphical report.

## Usage
```
/validate {STATE_CODE}
```
Example: `/validate AZ`

## Prerequisites
- Test data must exist: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- Results must exist in `output/results/{STATE}/`

If results don't exist, run `/run-tests {STATE}` first.

## What this does
1. Reads the test data Excel (expected values, XPaths, mappings)
2. Reads Defaults and Extras from the requirement doc
3. Reads each carrier request/response pair from `output/results/{STATE}/`
4. For each pair:
   - **Request validation:** Extracts values from carrier request XML using XPaths, compares with expected mappings
   - **Defaults validation:** Checks all 20 default values are present in the request
   - **Response validation:** Checks Result Page Coverages and Details against response XML
5. Generates graphical HTML report with dashboard
6. Generates summary Excel with detailed pass/fail per field

## Rules
- Read the skill at `skills/result-validator/SKILL.md` before starting
- Compare values exactly — string matching
- "Do not send" + value not found in XML = PASS (correctly omitted)
- Mark uncertain results as WARNING, not PASS
- Include trace references for every check

## Output
- `output/reports/HOA_HO3_{STATE}_ValidationReport.html` — graphical dashboard
- `output/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx` — detailed Excel report
