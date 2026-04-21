# /generate-testdata

Generate state-specific test data from the HOA requirement document.

## Usage
```
/generate-testdata {STATE_CODE}
```
Example: `/generate-testdata TX`

## What this does

### Step 0: Check for Existing Test Data

Before generating, check if `output/test-data/HOA_HO3_{STATE}_TestData.xlsx` already exists:

**Case 1 — Exists AND requirement doc unchanged:**
- Compare the requirement doc's last-modified timestamp vs the test data file's timestamp
- If requirement doc has NOT been modified since test data was generated → **SKIP generation**
- Inform user: "Test data for {STATE} already exists and requirement doc hasn't changed. Reusing existing test data."

**Case 2 — Exists BUT requirement doc has changes:**
- Read existing test data AND current requirement doc
- Diff field-by-field: identify added, removed, or modified Interview fields, List values, Defaults, Result Page items
- **Incrementally update** only the changed items in the existing test data
- Inform user with a summary:
```
📋 Requirement doc has changed since last test data generation.
   Changes detected:
   - Interview: {N} fields added, {M} fields modified, {K} fields removed
   - Lists: {changes summary}
   - Defaults: {changes summary}
   Updated existing test data with these changes. All other data unchanged.
```

**Case 3 — Does NOT exist:**
- Generate from scratch (normal flow below)

### Steps 1-9: Full Generation (when needed)
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
