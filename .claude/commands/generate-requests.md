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
1. Reads the generated test data Excel
2. Reads the sample request body from `sample-requests/`
3. Cross-references: identifies HOA-specific fields (have XPath) vs other-carrier fields (keep as-is)
4. Identifies all conditional rules (add/remove fields based on relevancy conditions)
5. Designs test scenarios organized by blind
6. Generates JSON files with proper naming convention
7. Generates scenario documentation Excel

## Rules
- Read the skill at `skills/request-body-generator/SKILL.md` before starting
- ONLY modify fields that have XPaths in HOA's Interview sheet
- Keep ALL other fields exactly as-is (they serve other carriers)
- None/NoCoverage = real values to SEND, not the same as removing
- Comment out fields ONLY when a specific Relevancy Condition disables them
- Add fields ONLY when a specific Relevancy Condition enables them
- File naming: `req_{NNN}_{description}___{BlindName}.json`

## Output
- `output/request-bodies/{STATE}/` — folder with all JSON scenario files for the state
- `output/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` — scenario documentation
