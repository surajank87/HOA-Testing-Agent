# HOA Testing Agent — Project & Domain Knowledge

## Purpose
This document captures the business logic, decisions, learnings, and requirements for the HOA Testing Agent project.

---

## 1. COMPANY & PLATFORM

- **Platform:** Bolt
- **Business:** U.S.-based insurance platform. Agents enter customer data on Bolt to retrieve quotes from multiple carriers.
- **Workflow:** Agents complete the Bolt interview -> Bolt submits JSON/XML to carrier -> carrier responds -> Bolt displays results.
- **Support Site:** Has an API. Used to retrieve actual request sent to carrier and response received, using Friendly ID (F#).
- **Environments:** QA (`developer-qa.boltqa.com`), UAT (`developer.bolttest.com`)

---

## 2. CARRIER: HOA (Homeowners of America)

- **Carrier:** HOA (BriteCore)
- **LOB:** Personal Home (HO3)
- **Request Format:** XML (XPaths start with `/root/...`)
- **Requirement Document:** `HOA (BriteCore) - HO3 - Standardization - V1.xlsx`

### HOA Document Structure (Analyzed)

**9 sheets total, only 2 primary for test data:**

**Interview sheet (170 rows):**
- Columns: No. | Blind | Section | Field Name | Textual Description | Relevancy Condition | State | Mandatory | Type | Input length | List | Carrier format | Mappings Details | XPath | Comment | Column1 | Column2
- 6 Blinds: Start (9), Home (27), Structure (23), Features (39), Policy (26), Applicant (45)
- State filtering via `State` column
- Relevancy conditions are code-like: `a.TypeOfFoundation == TypeOfFoundationType.Basement`
- Fields with `List` column reference a list name in the Lists sheet

**Lists sheet (1516 rows):**
- Columns: No. | Blind | List Name | Bolt Condition | Bolt States | Carrier States | Interview Value | Enum | Mapping | Mapping Type | Comment
- 56 unique list types
- State filtering via `Carrier States` column
- Bolt Condition is mainly for occupation sublists (filtered by EmploymentIndustry)

**State Filtering Patterns:**
- Interview `State` column: "state AZ, VA", "NOT USED FOR VA", "FL", "Not in MO", "Not in FL", "Not in CA", blank = all states
- Lists `Carrier States` column: "AZ, VA", "Not in TX", "CA, FL", "TX", "IN,NY", blank = all states

**Supporting sheets:**
- Defaults and Extras (20 rows) -- hardcoded values for every request
- UUD (2 rules) -- ineligibility conditions
- Result Page Coverages (12 items) -- what to display from response
- Result Page Details (7 items) -- premium, success/fail, errors
- API Setup -- environment configuration
- Scope -- states in scope (column C has check if applicable)
- Updates Log -- version history

---

## 3. THE GOAL — END-TO-END AGENT

**User provides:** Requirement document + target state
**Agent produces:**
1. **Test data in Excel** -- state-filtered, with smart reuse (skip if unchanged, incremental update if minor changes)
2. **JSON request bodies** -- combinatorially optimized (packed scenarios, not one-field-per-request)
3. **Test execution results** -- tabbed HTML dashboard with bug evidence links

**The agent supports 4 testing types:**
1. **Relevancy** -- remove a field → check API validation error + compare available values with requirement doc
2. **Mapping** -- verify field values map correctly from Bolt Enum to carrier XML
3. **Result Page** -- verify coverages display correctly in API response
4. **UUD & Defaults** -- verify ineligibility rules trigger decline; verify all 20 defaults are present

**Two testing modes:**
- **Focused** -- user describes affected areas → agent tests ONLY those fields exhaustively
- **Overall** -- full regression covering ALL fields with combinatorial packing

---

## 4. TEST DATA GENERATION — CRITICAL RULES

### Smart Reuse (Check Before Generating)
- If test data exists AND requirement doc timestamp hasn't changed → SKIP entirely
- If test data exists BUT requirement doc has changes → diff and incrementally update only changed items, report changes to user
- If test data doesn't exist → generate from scratch

### State Filtering
- Include rows where State column is blank/empty/None/"All States"
- Include rows where target state is listed
- Exclude rows where target state is NOT listed
- Handle "Not in [STATE]" and "NOT USED FOR" patterns

### List Sheet Handling (CRITICAL)
- **Field relevancy drives list inclusion:** If a field is excluded for the state, its list values are also excluded
- **Bolt values are DATA, not filters:** Interview Value column contains dropdown options, NEVER filter based on these
- **Filter using Carrier States column only**
- **Bolt Condition column:** Used mainly for occupation sublists

### Test Data Output Format
- Keep ALL columns from the requirement document
- Add a LAST column: `Reference` -- format: `(Sheet: [SheetName], Row: [RowNumber])`
- Preserve formatting, hyperlinks, freeze panes, auto-filter

---

## 5. REQUEST BODY GENERATION — CRITICAL RULES

### Combinatorial Optimization (CRITICAL)
- **Do NOT create one request body per field.** Pack multiple independent fields from ALL blinds into each request body.
- Independent fields (no relevancy condition linking them) vary TOGETHER. Scenarios = MAX(value count), not SUM.
- Dependent fields (parent-child) get their combinations tested explicitly and overlaid onto the packed matrix.
- Example: 3 booleans + 5-value dropdown + 4-value dropdown = 5 scenarios (not 14).

### Low-Priority Fields
- **Sample, don't exhaust:** CurrentPersonalHomeownerCarrierList, CurrentPersonalAutoCarrierList, PLLossDescriptionList, EmploymentIndustryList, OccupationStrList — sample 2-3 values only
- **Start and Applicant blinds:** minimal variation (address/contact/name fields not critical for carrier mapping)
- **15+ value threshold:** ask user before exhaustive testing; default to 3-5 sampled values

### Request Body Reuse
- Before generating, check if previous run exists for the state
- Ask user: reuse existing or generate fresh?

### The Sample Request Body
- Is a valid, complete request for the carrier
- Fields in request but NOT in test data = other-carrier fields, KEEP AS-IS
- Fields with "Do not send" / blank XPath = Bolt display-only, KEEP for other carriers

### None / NoCoverage / Reject
- These are REAL Enum values you SEND in the request
- NOT the same as commenting out or removing the field

### File Prefixes by Testing Type
| Type | Prefix |
|---|---|
| Relevancy (field) | `rel_` |
| Relevancy (conditional) | `crel_` |
| Mapping | `map_` |
| Result Page | `res_` |
| UUD | `uud_` |
| Defaults | `def_` |

### Output per Run
1. JSON scenario files in timestamped run folder
2. TEST_CASE_GUIDE.md — plain-English descriptions grouped by testing area with coverage matrix
3. Test Scenario Document (Excel)
4. run-metadata.json — mode, types, timestamp, scenario count

---

## 6. VALIDATION RULES

### Validation by Testing Type (auto-detected by file prefix)

**Relevancy Validation (`rel_`, `crel_`):**
- Check API returned validation error when field removed
- Parse available values from error message
- Compare with requirement doc's list values → mismatch = bug
- For conditional: verify parent-child triggering/non-triggering behavior

**Mapping Validation (`map_`):**
- For each HOA field with XPath: extract value from carrier request XML, compare with expected
- Check Defaults and Extras are present
- "Do not send" + value not found in XML = PASS

**Result Page Validation (`res_`):**
- Check response contains coverages with correct values
- Verify premium = annualTotalUsd + annualFeesUsd

**UUD/Default Validation (`uud_`, `def_`):**
- UUD: verify API declined with correct error
- Defaults: verify all 20 defaults at correct XPaths

### Bug Evidence Collection (ALL testing types)
When any check FAILs, collect 3 evidence files from:
1. **Requirement document** (ORIGINAL client-provided doc, NOT test data)
2. **Carrier request** (what was sent)
3. **Carrier response** (what was received)

Evidence formats vary by type. All stored in `output/runs/{RUN}/evidence/`. Linked from HTML dashboard FAIL rows.

### Tabbed HTML Report
```
[Overall Dashboard] [Relevancy Check] [Mapping Verification] [Result Page] [UUD & Defaults]
```
- Relevancy has sub-tabs: Field Relevancy | Conditional Relevancy
- UUD has sub-tabs: UUD Ineligibility | Defaults Verification
- CSS-only tabs (no JavaScript)
- Every FAIL row includes downloadable evidence links

### Progress Display
- During test execution: `5/25 scenarios completed` (monitor framework output folder)
- During validation: `5/25 validated` (per-scenario progress)

---

## 7. C# FRAMEWORK — ARCHITECTURE

### Solution: MappingVerification.sln (5 projects)

**DO NOT MODIFY any framework code.** Agent only interacts via file I/O and `dotnet test`.

**API Collection:** Endpoints, environment configs (QA.json, UAT.json), request templates
**CoreLogic:** `SendAndRetrieve()` sends request, retrieves from PolicyViewer, saves results
**PolicyViewer:** Selenium: navigates support site, searches by F#, retrieves Request/Response text
**Utility:** JSON/XML readers, path resolution, ExtentReport, JsonCommentStripper
**TestProject:** `Test.cs` reads .json files from RequestBodies/ folder, each file = one NUnit test case

### Framework Flow
```
RequestBodies/ folder (JSON files)
    → Test.cs reads each .json → strips comments → creates test case
    → RestSharp: CreateQuote → SubmitQuote → GetResults (poll until Completed)
    → Extract FriendlyId
    → PolicyViewer (Selenium): search F# → retrieve Request/Response text
    → Save to Output/{Carrier}/{State}/:
        {name}_carrier_request.txt, {name}_carrier_response.txt,
        {name}_details.json, {name}_request.json
    → ExtentReport HTML generated
```

### Key Technical Details
- .NET 8.0, NUnit 4.3.2, RestSharp 112.1.0, Selenium.WebDriver 4.27.0
- JSON requests always sent to Bolt API (Bolt converts to carrier format)
- Carrier request format (XML) determines validation extraction method

---

## 8. DECISIONS MADE

| Decision | Choice | Reason |
|---|---|---|
| Single agent | Claude Code for HOA | Everything in one place, leverages Claude Code features |
| Test data output format | Keep all columns + Reference column | User wants nothing removed |
| Test data reuse | Smart skip/incremental/fresh | Avoid unnecessary regeneration |
| Combinatorial packing | Pack independent fields across all blinds | Drastically reduces scenarios while maintaining coverage |
| Low-priority sampling | 2-3 values for occupation/industry/carrier lists | Hundreds of values but not mapping-critical |
| 15+ value threshold | Ask user first | Prevents scenario explosion |
| File prefixes by type | `rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_` | Auto-detection during validation |
| Bug evidence | 3 files per FAIL from requirement doc/request/response | Clear documentation for all test types |
| Timestamped run folders | `{STATE}_{YYYY-MM-DD}_{HH-MM-SS}/` | Preserve history, no overwrites |
| Formatted XML | Pretty-print alongside raw files | Human-readable without losing originals |
| Tabbed HTML report | CSS-only tabs, no JavaScript | Portable, no dependencies |
| Testing modes | Focused (specific) + Overall (regression) | Most testing is about specific changes |
| Testing types | 4 types with distinct validation | Relevancy, Mapping, Result Page, UUD/Defaults |
| None vs Comment-out | Distinct behaviors | None = real value sent; Comment-out = field absent |
| Multi-carrier rule | Only modify HOA fields (with XPath) | Other carriers need their fields intact |
| Framework | DO NOT MODIFY | Team uses it; agent only does file I/O |
