# Result Validator Skill

## Purpose
Validates carrier request/response XML pairs against the test data based on testing type, and produces a tabbed graphical validation report.

## Input
- Test data Excel: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- HOA requirement doc: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx` (for Defaults and Extras, Lists)
- Results folder: `output/runs/{RUN}/results/`
- Run metadata: `output/runs/{RUN}/run-metadata.json`

## Output
- `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html` — tabbed graphical dashboard
- `output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationSummary.xlsx` — detailed Excel report

---

## Process

### Step 1: Load Reference Data

**From test data Excel:**
- For each HOA field with XPath: field name, expected Enum value, expected Mapping, XPath
- Build lookup: `XPath → {field_name, expected_value, blind, mandatory}`

**From requirement doc (Defaults and Extras):**
- All 20 default values with their XPaths
- State-conditional defaults (filtered for target state)

**From requirement doc (Lists sheet):**
- For each List referenced by an Interview field: all valid Enum values and their Mappings
- State-filtered using Carrier States column
- This is needed for relevancy validation (comparing API values vs doc values)

**From requirement doc (Result Page Coverages):**
- Coverage name, XPath, state filter
- Filter for target state

**From requirement doc (Result Page Details):**
- Premium XPaths, success/fail indicators

### Step 1.5: Format Carrier XML

For each result file in the results folder:

1. **Read `{scenario}_carrier_request.txt`:**
   - Strip outer `<Request>` / `</Request>` wrapper tags if present
   - Detect content type:
     - If content starts with `{` → it's JSON: parse and pretty-print with 2-space indentation
     - If content starts with `<` → it's XML: parse and indent with 2-space indentation
   - Save formatted version as `{scenario}_carrier_request_formatted.xml` (or `.json` if JSON content)

2. **Read `{scenario}_carrier_response.txt`:**
   - Parse XML content and indent with 2-space indentation
   - Handle any namespace declarations gracefully
   - Save as `{scenario}_carrier_response_formatted.xml`

3. **Keep original raw files unchanged** — framework may reference them.

### Step 2: Load Results

For each scenario in the results folder (`output/runs/{RUN}/results/`):
- Read `{scenario}_details.json` → get F#, status, metadata
- Read `{scenario}_carrier_request.txt` → the actual XML request HOA received
- Read `{scenario}_carrier_response.txt` → HOA's XML response (if status is Success/Declined/Failed)
- Read `{scenario}_request.json` → what we sent to Bolt API
- **Auto-detect testing type** from file prefix: `rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_`

### Step 3: Validate Per Testing Type (with Progress Display)

After validating each scenario, output progress:
```
📊 Validating results...
   ⏳ 1/{TOTAL} validated — rel_001_RoofType (Relevancy)
   ⏳ 5/{TOTAL} validated — map_004_roof_metal___Structure (Mapping)
   ...
   ✅ {TOTAL}/{TOTAL} scenarios validated
```

#### 3A: Relevancy Validation (for `rel_` files — Field Relevancy)

For each field relevancy test case:

1. **Check API returned a validation error** (not a quote):
   - Status should indicate validation failure, not Success
   - The response should contain error/validation messages

2. **Extract error message** from the response:
   - Look for validation error text mentioning the omitted field name
   - Look for "available values", "valid options", or enumerated values in the error

3. **Parse available values** from the error message:
   - Extract the list of valid values the API reports for the missing field

4. **Compare with requirement doc:**
   - Get all valid Enum values from the Lists sheet for this field (state-filtered)
   - Compare API's available values against doc values
   - **PASS:** API identified field as required AND available values match the doc
   - **FAIL:** No validation error (field wasn't required), OR available values don't match
   - **WARNING:** API errored but available values couldn't be parsed from response

5. **Record:**
   - Field name, was error returned, expected values from doc, API values, match status

#### 3B: Conditional Relevancy Validation (for `crel_` files)

For each conditional relevancy test case:

1. **Check the scenario setup:**
   - What parent field value was set?
   - What child field was omitted?

2. **Validate:**
   - Parent triggers child → child omitted → API should error for child field
     - **PASS:** API returned validation error mentioning the child field
     - **FAIL:** API accepted request without child (child wasn't required when it should be)
   - Parent doesn't trigger child → child present → API should accept or ignore
     - **PASS:** API accepted without error about the child field
     - **FAIL:** API errored for child field when it shouldn't be required

3. **Record:**
   - Parent field, parent value, child field, child omitted?, error for child?, status

#### 3C: Mapping Validation (for `map_` files)

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

#### 3D: Result Page Validation (for `res_` files)

Only for scenarios where status is Success or Declined:

1. **Premium check:**
   - Extract `/root/quote/totals/annualTotalUsd` and `/root/quote/totals/annualFeesUsd`
   - Verify they exist and are numeric

2. **Coverage checks (Result Page Coverages):**
   - For each coverage applicable to this state, extract value from response XPath
   - Compare with expected display value based on what was sent in the request
   - Verify it exists and matches

3. **Success/Fail indication:**
   - If totals exist → success
   - If totals don't exist → check for error messages at `/root/messages[]`

#### 3E: UUD Validation (for `uud_` files)

1. **Check API returned decline/ineligibility:**
   - Status should be "Declined" or contain ineligibility indication
   - Error message should reference the ineligible value

2. **Record:**
   - UUD field, UUD value, was declined?, error message, status

#### 3F: Default Validation (for `def_` files)

1. **For each of the 20 defaults:**
   - Extract value from carrier request XML at the expected XPath
   - Compare with expected default value
   - State-conditional defaults: only check if applicable to this state

2. **Record:**
   - Default name, XPath, expected value, actual value, status

---

### Step 3.5: Collect Bug Evidence (for FAIL items only)

When any validation check results in **FAIL**, collect evidence from 3 sources:

#### Evidence 1: Requirement Document (client spec)
- Open the ORIGINAL requirement doc: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx`
- **NOT the test data** (test data is agent-generated; the requirement doc is client-provided)
- Find the row for the failed field in the Interview sheet
- Extract: Field Name, XPath, Mappings Details, List reference, expected Enum/Mapping values
- If the failure involves List values, also extract the relevant List entries from the Lists sheet
- Save as: `output/runs/{RUN}/evidence/{scenario}_{field}_requirement.txt`

Format:
```
BUG EVIDENCE — Requirement Document
=====================================
Field: {FieldName}
Source: carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx
Sheet: Interview, Row: {N}
XPath: {XPath}
Mappings Details: {MappingsDetails}
Expected Mapping: {from Lists sheet Mapping column}
List Values (state-filtered):
  - {InterviewValue1} → Enum: {Enum1} → Mapping: {Mapping1}
  - {InterviewValue2} → Enum: {Enum2} → Mapping: {Mapping2}
  ...
```

#### Evidence 2: What We Sent (carrier request)
- From the carrier request XML (formatted version if available)
- Extract the specific XPath node and its value
- Save as: `output/runs/{RUN}/evidence/{scenario}_{field}_request_actual.txt`

Format:
```
BUG EVIDENCE — Carrier Request (What Was Sent)
================================================
Scenario: {scenario_name}
File: {scenario_file}
XPath: {XPath}
Value Sent (Bolt Enum): {what we put in the JSON request}
Value at XPath (Carrier XML): {what actually appeared at XPath in carrier request}
Expected at XPath: {what should have appeared based on requirement doc mapping}

Relevant XML Fragment:
{indented XML snippet around the XPath node}
```

#### Evidence 3: What We Received (carrier response)
- From the carrier response XML (if applicable — mapping failures are request-side)
- For result page failures, extract the coverage/value from response
- Save as: `output/runs/{RUN}/evidence/{scenario}_{field}_response_actual.txt`

Format:
```
BUG EVIDENCE — Carrier Response (What Was Received)
=====================================================
Scenario: {scenario_name}
Coverage/Field: {coverage_name}
Response XPath: {XPath}
Expected Value: {what we expected}
Actual Value: {what the response contained}

Relevant XML Fragment:
{indented XML snippet}
```

#### Per-Testing-Type Evidence Details

Evidence is collected for ALL testing types — not just mapping. Here is what to capture for each type:

**Relevancy FAIL (field relevancy `rel_`):**
- Requirement: the field definition from Interview sheet + ALL expected list values from Lists sheet (state-filtered)
- Request: the JSON request body showing the field was removed
- Response: the API's validation error message — highlight whether it mentioned the field, and what available values it returned vs what the requirement doc says

```
BUG EVIDENCE — Relevancy FAIL
================================
Test Type: Field Relevancy
Field Removed: {FieldName}
Expected: API should error saying "{FieldName}" is required
Actual: {API did not error / API errored but wrong values}

Expected Values (from requirement doc Lists sheet):
  - {Value1}, {Value2}, {Value3}, ...

API Returned Values:
  - {Value1}, {Value2}, {ValueX_EXTRA}, ...

Mismatch: API has extra "{ValueX_EXTRA}" not in requirement doc
  — OR —
Mismatch: Requirement doc has "{Value3}" but API does not list it
```

**Conditional Relevancy FAIL (`crel_`):**
- Requirement: the Relevancy Condition from Interview sheet showing parent-child rule
- Request: the JSON showing parent value set and child removed
- Response: the API's error (or lack of error) for the child field

```
BUG EVIDENCE — Conditional Relevancy FAIL
============================================
Test Type: Conditional Relevancy
Parent: {ParentField} = {Value}
Child Removed: {ChildField}
Relevancy Condition (from requirement doc): {condition expression}
Expected: {API should/should not error for child}
Actual: {what happened}
```

**Mapping FAIL (`map_`):**
- Requirement: field row from Interview sheet + Mapping column from Lists sheet
- Request: carrier request XML showing actual value at XPath
- Response: not typically needed for mapping (it's a request-side check), but include if relevant

(Format already defined above in Evidence 1/2/3)

**Result Page FAIL (`res_`):**
- Requirement: the Result Page Coverages/Details row from requirement doc
- Request: what coverage values were sent in the JSON request
- Response: carrier response XML showing the actual coverage value vs expected

```
BUG EVIDENCE — Result Page FAIL
==================================
Test Type: Result Page Verification
Coverage: {CoverageName}
Sent Value (Bolt Enum): {what we sent}
Expected in Response: {what should appear based on mapping}
Actual in Response: {what the response XML shows}
Response XPath: {XPath}

Relevant Response XML Fragment:
{indented XML snippet}
```

**UUD FAIL (`uud_`):**
- Requirement: the UUD rule from UUD sheet (e.g., "PLConstructionType in {Log, Asbestos, EFIS} → INELIGIBLE")
- Request: the JSON request showing the ineligible value was sent
- Response: the API's response — did it decline or incorrectly accept?

```
BUG EVIDENCE — UUD FAIL
==========================
Test Type: UUD Ineligibility
UUD Rule (from requirement doc): {rule description}
Field: {FieldName} = {IneligibleValue}
Expected: API should DECLINE / return INELIGIBLE
Actual: {API accepted the quote / returned wrong error}
API Status: {Success/Declined/Failed}
Error Message: {if any}
```

**Defaults FAIL (`def_`):**
- Requirement: the Defaults and Extras row from requirement doc with expected XPath and value
- Request: carrier request XML showing the XPath where the default should be
- Response: not applicable (defaults are request-side)

```
BUG EVIDENCE — Defaults FAIL
===============================
Test Type: Defaults Verification
Default Name: {DefaultName}
XPath: {XPath}
Expected Value (from requirement doc): {ExpectedValue}
Actual Value (in carrier request XML): {ActualValue or "NOT FOUND"}
Condition: {state-conditional rule if any}

Relevant XML Fragment:
{indented XML snippet around the XPath, or note that node was missing}
```

#### Evidence Directory Structure
```
output/runs/{RUN}/evidence/
├── rel_001_RoofType_requirement.txt
├── rel_001_RoofType_request_actual.txt
├── rel_001_RoofType_response_actual.txt
├── crel_002_Foundation_BasementType_requirement.txt
├── crel_002_Foundation_BasementType_request_actual.txt
├── crel_002_Foundation_BasementType_response_actual.txt
├── map_003_RoofType_requirement.txt
├── map_003_RoofType_request_actual.txt
├── map_003_RoofType_response_actual.txt
├── res_002_PersonalLiability_requirement.txt
├── res_002_PersonalLiability_request_actual.txt
├── res_002_PersonalLiability_response_actual.txt
├── uud_001_ConstructionType_requirement.txt
├── uud_001_ConstructionType_request_actual.txt
├── uud_001_ConstructionType_response_actual.txt
├── def_001_protectionClass_requirement.txt
├── def_001_protectionClass_request_actual.txt
└── ...
```

Only create evidence files for FAIL items — do not create for PASS or WARNING.

---

### Step 4: Generate Tabbed HTML Report

Create a visually rich HTML report with **CSS-only tabs** (radio buttons + `:checked` selector, no JavaScript):

```html
<!DOCTYPE html>
<html>
<head>
    <title>HOA HO3 {STATE} - Validation Report</title>
    <style>
        /* DARK THEME — GitHub-inspired dark palette */
        body { background: #0d1117; color: #e6edf3; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; }
        .header { background: linear-gradient(135deg, #2c3e50, #3498db); color: white; }
        .container { max-width: 1400px; margin: 0 auto; }
        
        /* KPI cards */
        .kpi { background: #161b22; border-radius: 12px; color: #e6edf3; box-shadow: 0 2px 8px rgba(0,0,0,.3); }
        .kpi .label { color: #8b949e; }
        .kpi.pass-kpi .value { color: #3fb950; }
        .kpi.fail-kpi .value { color: #f85149; }
        .kpi.warn-kpi .value { color: #d29922; }
        
        /* Tab system using CSS radio buttons */
        .tab-input { display: none; }
        .tab-label {
            padding: 12px 24px;
            cursor: pointer;
            border-bottom: 3px solid transparent;
            font-weight: 600;
            color: #8b949e;
        }
        .tab-label:hover { color: #e6edf3; background: #1c2128; }
        .tab-input:checked + .tab-label {
            border-bottom-color: #58a6ff;
            color: #58a6ff;
            background: #1c2128;
        }
        .tab-bar { background: #161b22; }
        .tab-panel { display: none; }
        #tab-dashboard:checked ~ .panels > .panel-dashboard,
        #tab-relevancy:checked ~ .panels > .panel-relevancy,
        #tab-mapping:checked ~ .panels > .panel-mapping,
        #tab-resultpage:checked ~ .panels > .panel-resultpage,
        #tab-uud:checked ~ .panels > .panel-uud { display: block; }
        .panels { background: #161b22; }
        
        /* Sub-tab system (nested) */
        .subtab-input { display: none; }
        .subtab-input:checked + .subtab-label { border-bottom-color: #3fb950; color: #3fb950; }
        
        /* Tables */
        th { background: #1c2128; color: #58a6ff; border-bottom: 2px solid #30363d; }
        td { border-bottom: 1px solid #21262d; color: #e6edf3; }
        tr:hover td { background: #1c2128; }
        
        /* Status row backgrounds — dark with colored text for high contrast */
        .pass { background: #0d2818 !important; color: #3fb950 !important; }
        .fail { background: #3d1117 !important; color: #f85149 !important; }
        .warn { background: #2d2000 !important; color: #d29922 !important; }
        
        /* Status badges — solid colored pills */
        .badge { display: inline-block; padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; text-transform: uppercase; }
        .badge.pass { background: #238636 !important; color: #fff !important; }
        .badge.fail { background: #da3633 !important; color: #fff !important; }
        .badge.warn { background: #9e6a03 !important; color: #fff !important; }
        
        /* Misc dark elements */
        .section-title { color: #e6edf3; border-bottom: 2px solid #30363d; }
        .summary-box { background: #1c2128; border: 1px solid #30363d; border-radius: 8px; color: #8b949e; }
        .fail-detail { border-left: 4px solid #f85149; background: #3d1117; color: #f85149; }
        .warn-detail { border-left: 4px solid #d29922; background: #2d2000; color: #d29922; }
    </style>
</head>
<body>
    <!-- MAIN TABS -->
    <input type="radio" name="main-tab" id="tab-dashboard" class="tab-input" checked>
    <label for="tab-dashboard" class="tab-label">Overall Dashboard</label>
    
    <input type="radio" name="main-tab" id="tab-relevancy" class="tab-input">
    <label for="tab-relevancy" class="tab-label">Relevancy Check</label>
    
    <input type="radio" name="main-tab" id="tab-mapping" class="tab-input">
    <label for="tab-mapping" class="tab-label">Mapping Verification</label>
    
    <input type="radio" name="main-tab" id="tab-resultpage" class="tab-input">
    <label for="tab-resultpage" class="tab-label">Result Page</label>
    
    <input type="radio" name="main-tab" id="tab-uud" class="tab-input">
    <label for="tab-uud" class="tab-label">UUD & Defaults</label>
    
    <div class="panels">
        <!-- Panel: Overall Dashboard -->
        <!-- Panel: Relevancy Check (with sub-tabs) -->
        <!-- Panel: Mapping Verification -->
        <!-- Panel: Result Page -->
        <!-- Panel: UUD & Defaults (with sub-tabs) -->
    </div>
</body>
</html>
```

**Only show tabs for testing types that were actually run** (check file prefixes in results or `run-metadata.json`).

#### Tab 1: Overall Dashboard

- **Header:** Title, subtitle with carrier/LOB/state/timestamp/mode/testing types
- **KPI Cards (8):** Total Scenarios, Overall Pass Rate, per-type pass rates, API Success count, API Declined count
- **Per-Type Summary Table:**
  | Testing Type | Scenarios | Passed | Failed | Warnings | Pass Rate |
  |---|---|---|---|---|---|
  | Relevancy | 15 | 14 | 1 | 0 | 93.3% |
  | Mapping | 20 | 19 | 0 | 1 | 95.0% |
  | ... | ... | ... | ... | ... | ... |
- **Quick links** to each tab

#### Tab 2: Relevancy Check (with sub-tabs)

**Sub-tab: Field Relevancy**
| # | Field Name | Error Returned? | Expected Values (doc) | API Values | Values Match? | Status |
|---|---|---|---|---|---|---|
| 1 | RoofType | Yes | AsphaltShingle, Metal, Tile, ... | AsphaltShingle, Metal, Tile, ... | Yes | PASS |
| 2 | PLConstructionType | Yes | Frame, Masonry, ... | Frame, Masonry, Steel, ... | No — API has extra "Steel" | FAIL |

**Sub-tab: Conditional Relevancy**
| # | Parent Field | Parent Value | Child Field | Child Omitted? | Error for Child? | Status |
|---|---|---|---|---|---|---|
| 1 | TypeOfFoundation | Basement | BasementType | Yes | Yes — "BasementType is required" | PASS |
| 2 | PL_AdditionalStructures_Garage | true | TypeGarageCarport | Yes | No error returned | FAIL |

Summary stats at top: X/Y field relevancy checks passed, X/Y conditional checks passed.

#### Tab 3: Mapping Verification

Existing report structure, enhanced:
- **Per-Scenario Summary Table:**
  | # | Scenario | File | Blind | Status | Total Checks | Passed | Failed | Warnings | Pass Rate |
- **Per-Scenario Expandable Details:**
  - Table: Field Name | Expected | Actual | XPath | Status (color-coded)
  - Separate sections for Request Validation and Response Validation
- **Failed Items Summary:** grouped by field name and by blind
- **Per-Blind Pass Rates:** bar/progress visualization

#### Tab 4: Result Page Verification

| # | Scenario | Coverage Name | Sent Value | Response XPath | Response Value | Match? | Status |
|---|---|---|---|---|---|---|---|
| 1 | res_001 | Dwelling | 250000 | /root/.../dwelling | $250,000 | Yes | PASS |
| 2 | res_001 | Personal Liability | Fifty | /root/.../liability | $50,000 | Yes | PASS |

**Premium Verification:**
| # | Scenario | annualTotalUsd | annualFeesUsd | Total Premium | Status |
|---|---|---|---|---|---|

#### Tab 5: UUD & Defaults (with sub-tabs)

**Sub-tab: UUD Ineligibility**
| # | Scenario | UUD Field | UUD Value | API Declined? | Error Message | Status |
|---|---|---|---|---|---|---|
| 1 | uud_001 | PLConstructionType | Log | Yes — Declined | "Construction type not eligible" | PASS |
| 2 | uud_002 | PLNumberOfStories | 4 | Yes — Declined | "Number of stories exceeds limit" | PASS |

**Sub-tab: Defaults Verification**
Matrix table: Default Name × Scenario → Present/Absent with actual value

| Default | XPath | Expected | def_001 | def_002 | def_003 |
|---|---|---|---|---|---|
| carrier | /root/.../carrier | HOMEOWNERS_OF_AMERICA | PASS (HOMEOWNERS_OF_AMERICA) | PASS | PASS |
| protectionClass | /root/.../protectionClass | 1 | PASS (1) | PASS | PASS |
| waterDamage | /root/.../waterDamage | true | PASS (true) | PASS | PASS |

Color-coded cells: green = present with correct value, red = missing or wrong value.

#### Bug Evidence Links in All Tabs

For every FAIL row in ANY tab, add a **downloadable evidence link** column:

| ... | Status | Evidence |
|---|---|---|
| ... | FAIL | [View Evidence](../evidence/map_003_RoofType_requirement.txt) |

The evidence link points to the evidence files in the `evidence/` folder within the run directory. Since the HTML report and evidence folder are siblings under the same run folder, use relative paths like `../evidence/{filename}`.

For each FAIL, show 3 links:
- **Requirement** → `../evidence/{scenario}_{field}_requirement.txt`
- **Request** → `../evidence/{scenario}_{field}_request_actual.txt`
- **Response** → `../evidence/{scenario}_{field}_response_actual.txt` (if applicable)

This lets anyone reviewing the dashboard click directly to see:
- What the client spec says (requirement doc)
- What was actually sent (carrier request)
- What was actually received (carrier response)

---

### Step 5: Generate Summary Excel

**Sheet 1: Overall Summary**
| Scenario | File | Type | Status | Total Checks | Passed | Failed | Warnings | Pass Rate |
|---|---|---|---|---|---|---|---|---|

**Sheet 2: Relevancy Results**
| Field Name | Error Returned | Expected Values | API Values | Match | Status |
|---|---|---|---|---|---|

**Sheet 3: Mapping Field Detail**
| Scenario | Blind | Field Name | XPath | Expected | Actual | Status | Reference |
|---|---|---|---|---|---|---|---|

**Sheet 4: Result Page Detail**
| Scenario | Coverage | Sent Value | Response Value | Match | Status |
|---|---|---|---|---|---|

**Sheet 5: UUD Results**
| Scenario | UUD Field | UUD Value | Declined | Error Message | Status |
|---|---|---|---|---|---|

**Sheet 6: Defaults Validation**
| Default | XPath | Expected Value | Scenario 1 | Scenario 2 | ... |
|---|---|---|---|---|---|

**Sheet 7: Failed Items Only**
| Scenario | Type | Field Name | Expected | Actual | XPath | Possible Cause |
|---|---|---|---|---|---|---|

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
- Per testing type: separate pass rates for each type
