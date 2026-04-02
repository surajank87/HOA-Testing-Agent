# /generate-testdata

Generate state-specific test data from the HOA requirement document.

## Usage
```
/generate-testdata {STATE_CODE}
```
Example: `/generate-testdata TX`

## What this does
1. Reads the HOA requirement document: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx`
2. Verifies the state is in scope (Scope sheet)
3. Filters Interview sheet: ONLY fields with valid XPath (`/root/...`) AND relevant for target state
4. Filters Lists sheet: only values for included fields + Carrier States filter
5. Filters Defaults and Extras for state-conditional items
6. Copies UUD sheet as-is
7. Filters Result Page Coverages by Bolt States
8. Copies Result Page Details as-is
9. Generates Excel workbook: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`

## Rules
- Read the skill at `skills/test-data-generator/SKILL.md` before starting
- **Only include Interview fields with valid XPath** — blank/NA/"Do not send" XPath = exclude entirely
- **Keep the SAME sheet structure as the requirement doc** — do NOT split into per-blind sheets
- Sheets: Interview, Lists, Defaults and Extras, UUD, Result Page Coverages, Result Page Details
- Keep ALL original columns from each sheet + add `Reference` column at the end
- **Preserve formatting** from the requirement doc (colors, fonts, borders, column widths)
- **Add hyperlinks** in the Interview List column → corresponding group in Lists sheet
- Never fabricate data — everything traceable to the requirement document

## Output
- `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
