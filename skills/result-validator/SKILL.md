# Result Validator Skill

## Purpose
Validates carrier request/response XML pairs against the test data and produces a graphical validation report.

## Input
- Test data Excel: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- HOA requirement doc: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx` (for Defaults and Extras)
- Results folder: `output/results/{STATE}/`

## Output
- `output/reports/HOA_HO3_{STATE}_ValidationReport.html` — graphical dashboard
- `output/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx` — detailed Excel report

---

## Process

### Step 1: Load Reference Data

**From test data Excel:**
- For each HOA field with XPath: field name, expected Enum value, expected Mapping, XPath
- Build lookup: `XPath → {field_name, expected_value, blind, mandatory}`

**From requirement doc (Defaults and Extras):**
- All 20 default values with their XPaths
- State-conditional defaults (filtered for target state)

**From requirement doc (Result Page Coverages):**
- Coverage name, XPath, state filter
- Filter for target state

**From requirement doc (Result Page Details):**
- Premium XPaths, success/fail indicators

### Step 2: Load Results

For each scenario in the results folder (`output/results/{STATE}/`):
- Read `{scenario}_details.json` → get F#, status, metadata
- Read `{scenario}_carrier_request.txt` → the actual XML request HOA received
- Read `{scenario}_carrier_response.txt` → HOA's XML response (if status is Success/Declined/Failed)
- Read `{scenario}_request.json` → what we sent to Bolt API

### Step 3: Request Validation (Per Scenario)

For each HOA field with an XPath in the test data:

1. **Extract actual value** from the carrier request XML:
```python
# Parse XML, strip namespaces if present
# Use XPath to find the value
actual_value = xml_tree.xpath(xpath_from_test_data)
```

2. **Determine expected value:**
   - If the scenario's sent_body has the field → the Enum value sent
   - Cross-reference with test data's Mappings Details for the expected carrier mapping
   - If Mappings Details is "None" or blank → the Enum value itself is expected

3. **Compare:**
   - actual == expected → **PASS**
   - actual != expected → **FAIL**
   - Field not found in XML + test data says "Do not send"/blank XPath → **PASS** (correctly omitted)
   - Field not found in XML + test data says it SHOULD be sent → **FAIL** (missing)
   - Can't determine → **WARNING**

4. **Check Defaults and Extras:**
   - For each of the 20 defaults, verify the value exists at the correct XPath in the carrier request
   - State-conditional defaults: only check if applicable to this state

### Step 4: Response Validation (Per Scenario)

Only for scenarios where status is Success or Declined:

1. **Premium check:**
   - Extract `/root/quote/totals/annualTotalUsd` and `/root/quote/totals/annualFeesUsd`
   - Verify they exist and are numeric

2. **Coverage checks (Result Page Coverages):**
   - For each coverage applicable to this state, extract value from response XPath
   - Verify it exists

3. **Success/Fail indication:**
   - If totals exist → success
   - If totals don't exist → check for error messages at `/root/messages[]`

### Step 5: Generate HTML Report

Create a visually rich HTML report with:

**Dashboard Section:**
- Overall pass rate (large number + pie chart)
- Per-blind pass rates (bar chart)
- Per-scenario status (color-coded table: green=all pass, yellow=warnings, red=failures)

**Per-Scenario Detail:**
- Expandable sections for each scenario
- Table: Field Name | Expected | Actual | XPath | Status (color-coded)
- Separate sections for Request Validation and Response Validation

**Failed Items Summary:**
- Consolidated list of all failures across all scenarios
- Grouped by field name (to identify systematic issues)
- Grouped by blind (to identify which area has most problems)

**Defaults Validation:**
- Table showing all 20 defaults with pass/fail per scenario

Use this HTML structure:
```html
<!DOCTYPE html>
<html>
<head>
    <title>HOA HO3 {STATE} - Validation Report</title>
    <style>
        /* Dashboard styles with charts */
        .pass { background: #d4edda; }
        .fail { background: #f8d7da; }
        .warn { background: #fff3cd; }
        /* Expandable sections */
        /* Responsive layout */
    </style>
</head>
<body>
    <!-- Dashboard with summary stats -->
    <!-- Per-scenario expandable details -->
    <!-- Failed items consolidated view -->
    <!-- Defaults validation table -->
</body>
</html>
```

### Step 6: Generate Summary Excel

**Sheet 1: Overall Summary**
| Scenario | File | F# | Status | Total Checks | Passed | Failed | Warnings | Pass Rate |
|---|---|---|---|---|---|---|---|---|

**Sheet 2: Field-by-Field Detail**
| Scenario | Blind | Field Name | XPath | Expected | Actual | Status | Reference |
|---|---|---|---|---|---|---|---|

**Sheet 3: Failed Items Only**
| Scenario | Blind | Field Name | Expected | Actual | XPath | Possible Cause |
|---|---|---|---|---|---|---|

**Sheet 4: Defaults Validation**
| Default | XPath | Expected Value | Scenario 1 | Scenario 2 | ... |
|---|---|---|---|---|---|

**Sheet 5: Response Validation**
| Scenario | Coverage | XPath | Found | Value | Status |
|---|---|---|---|---|---|

---

## Validation Logic Details

### XPath Extraction from XML
HOA uses XML format. XPaths from the test data look like:
- `/root/propertyAddress/line1`
- `/root/homeDetails/yearBuilt`
- `/root/coverages/dwelling/limitUsd`
- `/root/primaryInsured/emails[@id='1']/email`

Some XPaths have attributes like `[@id='1']` — handle these properly.

If the carrier request XML has namespaces, strip them before XPath evaluation (similar to what the C# framework does in DataReader.cs).

### Mapping Comparison
The test data has a `Mappings Details` column that describes how the Bolt value maps to the carrier value:
- `"format yyyy-mm-dd"` → date format conversion
- `"String, 1 to 1 from Bolt List"` → direct mapping from List's Mapping column
- Blank/None → Enum value is sent as-is
- Specific mapping descriptions → check the Lists sheet's Mapping column for the actual carrier value

### Handling Missing Responses
- If status is TechnicalError or SubmissionReferral → skip response validation
- If carrier_response.xml doesn't exist → only validate request
- Still report which scenarios had no response

### Pass/Fail Thresholds
- Individual field: exact match = PASS, mismatch = FAIL
- Scenario: all fields PASS = scenario PASS; any FAIL = scenario FAIL
- Overall: percentage of passed field checks across all scenarios
