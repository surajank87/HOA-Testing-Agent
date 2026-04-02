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
1. **Test data in Excel** -- all columns from requirement doc preserved, last column = reference (sheet, row where data came from). State-specific filtering applied.
2. **JSON request bodies** -- multiple scenarios covering different blinds/coverages/conditions
3. **Test execution results** -- graphical format showing pass/fail after validation

**Internally, the agent:**
- Creates test data for the state
- Creates JSON request bodies for different scenarios
- Uses the reusable C# framework to send requests and retrieve request/response from support site
- Validates request mapping and response mapping
- Produces final results

---

## 4. TEST DATA GENERATION — CRITICAL RULES

### State Filtering
- Include rows where State column is blank/empty/None/"All States"
- Include rows where target state is listed
- Exclude rows where target state is NOT listed
- Handle "Not in [STATE]" and "<>" (not equal) patterns
- "All States" or blank = applies everywhere

### List Sheet Handling (CRITICAL)
- **Field relevancy drives list inclusion:** If a field is NOT relevant for the target state (excluded during state filtering), its corresponding list values should also be excluded
- **Bolt values are DATA, not filters:** The Interview Value column contains selectable dropdown options. NEVER apply state filtering on these values themselves. Even if values look like state codes, they are dropdown options.
- **Filter using Carrier States column only:** In the Lists sheet, only the `Carrier States` column determines which values apply to which states
- **Bolt Condition column:** Used mainly for occupation sublists (e.g., "Agriculture/Forestry/Fishing" conditions OccupationStrList values)
- **Include ALL values for an included list** unless Carrier States restricts specific values

### Coverage Rules
- HOA coverages are fields in the Policy blind with list values (PLPersonalLiabilityList, DwellingMedicalPaymentsList, PLAllPerilsDeductibleList, etc.)
- Include only coverages applicable to the target state
- Add new coverages if they exist for the state but not in a template
- Completely omit coverages not applicable -- no "Not applicable" messages

### Test Data Output Format
- Keep ALL columns from the requirement document -- do NOT remove any
- Add a LAST column: `Reference` -- format: `(Sheet: [SheetName], Row: [RowNumber])`
- This is the only addition to the original column structure

---

## 5. REQUEST BODY GENERATION — CRITICAL RULES

### The Sample Request Body
- Is a valid, complete request for the carrier
- Fields in request but NOT in test data = other-carrier fields, KEEP AS-IS (multi-carrier request body)
- Fields with "Do not send" / blank XPath in test data = Bolt display-only fields, KEEP in request for other carriers but don't modify for HOA

### None / NoCoverage / Reject
- These are REAL Enum values you SEND in the request
- NOT the same as commenting out or removing the field
- The carrier processes "received with None" differently from "field not present"

### Commenting Out Fields
- ONLY when a specific condition in the test data says "disable" / "do not display" / "gray out" for a specific scenario
- Every comment-out must cite the specific rule and source row
- NEVER comment out based on assumptions

### Adding Fields
- ONLY when a specific condition in the test data says "display" / "send" and the condition is triggered
- The sample request represents one scenario -- other scenarios may need additional fields
- Every addition must cite the condition and source

### Scenario Organization
- Organized by Blind (Start, Home, Structure, Features, Policy, Applicant, CrossBlind)
- File naming: `req_[NNN]_[description]___[BlindName].json`
- Triple underscore before blind name for easy parsing

### Output
1. Folder with all JSON files
2. Test Scenario Document (Excel) with: Scenario Summary, Field Value Matrix, Conditional Rules, Coverage Value Reference, Fields Removed/Added

---

## 6. VALIDATION RULES

### Request Mapping Validation
- For each field with an XPath in test data:
  - Was the value sent to the correct path?
  - Does the sent value match the expected Enum/Mapping?
  - Were "Do not send" fields correctly omitted?
  - Were conditional rules respected?
- "Do not send" + "Unable to extract value" = PASS (correctly omitted)

### Response Mapping Validation
- For each Result Page entry:
  - Does the response contain the expected coverage/field?
  - Does the display format match?

### End-to-end trace
- Test Data Enum -> Request Value Sent -> Response Display Value -> Expected Mapping

---

## 7. C# FRAMEWORK — ARCHITECTURE

### Solution: MappingVerification.sln (5 projects)

**API Collection:**
- `APIConstants.cs` -- endpoints: CREATE_QUOTE, SUBMIT_QUOTE, GET_RESULT
- Environment configs: QA.json, UAT.json (base URLs, API keys)
- Request templates: PersonalAuto.json, PersonalHome.json, Renters.json

**CoreLogic:**
- `CommonBaseTest.cs` -- `SendAndRetrieve()` sends request, retrieves from PolicyViewer, saves results to output folder.
- `APIManager.cs` -- RestSharp: CreateQuote -> SubmitQuote -> GetResults. Gets FriendlyId from response.
- `EnvironmentConfigurationManager.cs` -- resolves environment config file paths
- `PolicyViewerUrlManager.cs` -- builds PolicyViewer URLs per environment
- `RequestManager.cs` -- resolves request template file paths
- `Credentials.cs` -- PolicyViewer login credentials

**PolicyViewer:**
- `PolicyViewerPage.cs` -- Selenium: navigates to support site, selects tenant, searches by F#, clicks "Show Stage Details", retrieves Request/Response/ResultMessages text from new windows
- `SubmissionHandler.cs` -- orchestrates PolicyViewer retrieval, handles different statuses (Success, Declined, Failed, SubmissionReferral, TechnicalError)
- `DriverManager.cs` -- thread-safe ChromeDriver management
- `BasePage.cs` -- Selenium page object base

**Utility:**
- `JsonReader.cs` -- reads/updates JSON, adds key-value pairs at paths
- `DataReader.cs` -- extracts values from XML (XPath) and JSON (JSONPath)
- `Helper.cs` -- path resolution, test result comparison, filename sanitization
- `PropertyAddress.cs` -- updates address in request body
- `Reporter.cs` -- ExtentReport logging
- `ExtentReportBase.cs` -- report initialization and flushing
- `JsonCommentStripper.cs` -- strips // comments from Claude's JSON

**TestProject:**
- `Test.cs` -- reads .json files from RequestBodies/ folder, each file = one NUnit test case
- `config.json` -- carrier, LOB, environment, tenant, request format configuration

### Framework Flow
```
RequestBodies/ folder (JSON files)
    |
Test.cs reads each .json file -> strips comments -> creates test case
    |
CommonBaseTest.SendAndRetrieve():
    -> Parse JSON request body
    -> RestSharp: POST /getquote/v0/api/applications (CreateQuote)
    -> Extract applicationId
    -> POST /getquote/v0/api/applications/{id}/submission (SubmitQuote)
    -> GET /getquote/v0/api/applications/{id}/submission (GetResults, poll until Completed)
    -> Extract FriendlyId from CreateQuote response
    |
PolicyViewer (Selenium):
    -> Navigate to support site URL
    -> Select tenant from dropdown
    -> Search by FriendlyId
    -> Click "Show Stage Details"
    -> Click "Request" button -> new window -> extract text from textarea
    -> Click "Response" button -> new window -> extract text from textarea
    |
Save to Output/{Carrier}/{State}/ folder:
    -> Per request: {name}_carrier_request.txt, {name}_carrier_response.txt, {name}_details.json, {name}_request.json
    |
ExtentReport HTML generated
```

### Key Technical Details
- NuGet packages: RestSharp 112.1.0, Selenium.WebDriver 4.27.0, EPPlus 7.5.2, Newtonsoft.Json 13.0.3, ExtentReports 5.0.4, NUnit 4.3.2
- .NET 8.0
- JSON requests always sent to Bolt API (even for XML-format carriers -- Bolt converts)
- Carrier request format (XML/JSON) determines how validation extracts values from the actual carrier request

---

## 8. DECISIONS MADE

| Decision | Choice | Reason |
|---|---|---|
| Single agent vs separate projects | Single agent (Claude Code) for HOA | More professional, covers more Claude features, everything in one place |
| Test data output format | Keep all columns + add Reference column | User doesn't want any columns removed |
| List filtering | Field relevancy drives list inclusion | If field is excluded for state, its list values are also excluded |
| Bolt values | NEVER filter as state conditions | They are dropdown DATA, not filters |
| None vs Comment-out | Distinct behaviors | None = real value sent; Comment-out = field absent due to condition |
| Request body fields not in test data | Keep as-is | They're carrier-essential |
| Condition-driven add/remove | Both directions from test data Rules | Conditions can add child fields AND disable/hide fields |
| Framework approach | Simplified send-and-retrieve | Claude handles intelligence; framework handles API/browser |
| Output format | Test data Excel + JSON bodies + graphical results | User wants all three |

---

## 9. IMPORTANT REMINDERS

- **HOA's structure is simpler** -- 2 primary sheets (Interview + Lists), no complex MappingData bands
- **State filtering is predictable** -- Interview `State` column + Lists `Carrier States` column
- **Relevancy conditions are code-like** -- parseable expressions like `a.FieldName == Type.Value`
- **Defaults and Extras** -- 20 hardcoded values that go in every request, some state-conditional
- **Request format is XML** -- XPaths like `/root/propertyAddress/line1`
- **Model recommendation:** Opus 4.6 + Extended Thinking ON
