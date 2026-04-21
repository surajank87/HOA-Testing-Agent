# Request Body Generator Skill

## Purpose
Takes the generated test data Excel and a sample request body, then produces multiple JSON request body files covering different test scenarios based on the selected testing mode and types.

## Input
- Test data Excel: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- Sample request body: `sample-requests/SampleRequestHOA{STATE}.json` (e.g., `SampleRequestHOAIL.json` for IL)
- **Testing mode:** Focused or Overall (from user)
- **Testing types:** Relevancy, Mapping, Result Page, UUD & Defaults (from user)

## Output
- `output/runs/{RUN}/request-bodies/` — folder with all JSON scenario files
- `output/runs/{RUN}/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` — scenario documentation
- `output/runs/{RUN}/scenarios/TEST_CASE_GUIDE.md` — plain-English test case descriptions

---

## Process

### Step 1: Parse Sample Request Body

Read the JSON and list every field with its path and current value:
```
Data.PropertyAddress.ZipCode = "60442"
Data.PLYearBuilt = 2015
Data.CreditCheckPermission = true
Data.PLPersonalLiability = "Fifty"
...
```

### Step 2: Classify Every Field

Cross-reference each request field against the test data:

| Classification | How to Identify | Action |
|---|---|---|
| **HOA-specific** | Field exists in test data with a valid XPath | We modify this in scenarios |
| **HOA display-only** | Field exists in test data but XPath is blank/None | Keep as-is (Bolt UI field, not sent to HOA) |
| **Other-carrier field** | Field NOT in test data at all | KEEP EXACTLY AS-IS |
| **Carrier-essential** | Fields like Products, IDs | Never touch |

**Only HOA-specific fields (52 of them) get modified in scenarios.** Everything else stays untouched.

### Step 3: Identify Conditional Rules

From the test data's Relevancy Condition column, extract parent-child relationships:

| Parent Field | Condition | Child Field(s) | Effect |
|---|---|---|---|
| TypeOfFoundation | == Basement or PartialBasement | BasementType | Show/hide |
| PL_AdditionalStructures_Garage | == true | TypeGarageCarport, PL_NumberCarSpace | Show/hide |
| PL_AdditionalStructures_Pool | == true | PL_PoolType, Pool features | Show/hide |
| CurrentPersonalHomeownerCarrier | == NoPriorInsurance | SelectReasonNoPriorHomeInsurance | Show/hide |
| HeatingUpdateYN | == true | PLHeatingUpdate, HeatingUpdateYear | Show/hide |
| PlumbingUpdateYN | == true | PLPlumbingUpdated, PlumbingUpdatedYear | Show/hide |
| ElectricalUpdateYN | == true | PLElectricalUpdated, ElectricalUpdatedYear | Show/hide |
| IsMailAddress | == true | MailingAddress fields | Show/hide |
| AnyAdditionalInsured | == true | Co-applicant fields | Show/hide |
| FireDetection | == true | FireDetectionType | Show/hide |
| BurglarAlarm | == true | BurglarAlarmType | Show/hide |
| SmokeDetector | == true | SmokeDetectorType | Show/hide |
| SprinklerSystem | == true | SprinklerSystemType | Show/hide |
| PLHaveAnyLosses | == true | PersonalLosses array | Show/hide |

When the parent condition IS met → child field should be present in the request.
When the parent condition IS NOT met → child field should be commented out.

---

## Step 3.5: Focused Mode Scenario Design

**Only when user selected Focused Testing mode.**

Given the user's description of affected areas (e.g., "roof type mapping changed"):

1. **Field Identification:** Map the description to HOA fields from the test data:
   - Match field names, List names, or descriptive keywords
   - Example: "roof type" → RoofType, PLRoofUpdated, MitRoofShape (if applicable for state)

2. **Related Fields Discovery:** For each identified field, also find:
   - Parent fields (fields whose Relevancy Condition references this field)
   - Child fields (fields that this field's condition triggers)
   - Fields sharing the same List group
   - Fields interacting via Defaults and Extras (e.g., roofInstallationDate derives from PLRoofUpdated)

3. **Exhaustive Edge-Case Strategy (with combinatorial packing):**
   - **Every single Enum value** from the Lists sheet for each affected field
   - **Conditional parent/child combinations:** parent enabled + each child value; parent disabled + child absent
   - **State-specific values:** if the field has state-filtered list values, test all of them
   - **Pack focused fields together:** If 3 focused fields have 4, 6, and 2 values → only 6 scenarios needed (not 12). Apply the same combinatorial optimization from Step 3.7.
   - **Default interactions:** if the field affects a Default/Extra value, test those interactions

4. **Focus Metadata:** Each JSON gets additional header lines:
   ```
   // Focus Area: {field_name(s)}
   // Focus Reason: {user's description}
   ```

5. **Non-affected fields stay at sample/default values.** Do NOT vary them.

---

## Step 3.7: Combinatorial Optimization — CRITICAL

**THIS IS THE MOST IMPORTANT OPTIMIZATION. Read carefully.**

Instead of changing one field per request body, **pack multiple independent field changes into each request body.** This drastically reduces the number of scenarios while maintaining full mapping coverage.

### The Principle

Fields are either **independent** or **dependent**:
- **Independent fields** have NO relevancy condition linking them to each other. Their values don't affect each other's presence or behavior. These can be varied TOGETHER in the same request body.
- **Dependent fields** have a parent-child relationship via Relevancy Conditions. These need their combinations tested explicitly.

### How to Build Optimized Scenarios

#### Phase A: Classify Fields by Independence

1. **List all HOA-specific fields** with valid XPaths (from Step 2)
2. **Build a dependency graph** from the Relevancy Conditions (from Step 3):
   - Mark pairs as DEPENDENT if one's Relevancy Condition references the other
   - All other pairs are INDEPENDENT
3. **Group dependent fields** into dependency clusters:
   - Example cluster: `{TypeOfFoundation, BasementType}` — because BasementType depends on TypeOfFoundation
   - Example cluster: `{PL_AdditionalStructures_Garage, TypeGarageCarport, PL_NumberCarSpace}`
   - Independent fields each form their own single-field cluster

#### Phase B: Determine Value Count Per Field

For each field, count how many distinct values need coverage:
- **Boolean fields:** 2 values (true, false)
- **Dropdown/List fields:** N values (from Lists sheet, state-filtered, excluding UUD-triggering values for mapping tests)
- **Free-text/numeric fields:** typically 1-2 representative values

#### Phase C: Pack Independent Fields Together

The number of scenarios needed = **MAX value count across all independent fields** (not the SUM).

**Example:**
| Field | Type | Values to Test | Count |
|---|---|---|---|
| IsFire | Boolean | true, false | 2 |
| IsSmoke | Boolean | true, false | 2 |
| ConstructionType | Dropdown | Frame, Masonry, Brick, Steel, Stucco | 5 |
| RoofType | Dropdown | Asphalt, Metal, Tile, Slate | 4 |
| NoOfStories | Dropdown | 1, 1.5, 2, 2.5, 3 | 5 |

**Naive approach:** 2+2+5+4+5 = 18 scenarios (one field per request) — WRONG
**Optimized approach:** MAX(2,2,5,4,5) = **5 scenarios** — CORRECT

Build a value matrix by rotating through each field's values:

| Scenario | IsFire | IsSmoke | ConstructionType | RoofType | NoOfStories |
|---|---|---|---|---|---|
| map_001 | true | true | Frame | Asphalt | 1 |
| map_002 | false | false | Masonry | Metal | 1.5 |
| map_003 | true | true | Brick | Tile | 2 |
| map_004 | false | false | Steel | Slate | 2.5 |
| map_005 | true | true | Stucco | Asphalt (wrap) | 3 |

- Booleans cycle: true, false, true, false, true — **fully covered in 2 rows**
- RoofType has 4 values, wraps on row 5 — **fully covered in 4 rows**
- ConstructionType and NoOfStories each have 5 values — **fully covered in 5 rows**
- Once a field's values are all covered, subsequent rows can use ANY value (wrap around or use defaults)

#### Phase D: Handle Dependent Field Clusters Separately

For dependency clusters, generate the **cartesian product** of meaningful combinations:

**Example:** TypeOfFoundation (3 values: Basement, PartialBasement, SlabOnGrade) → BasementType (2 values, only when Foundation=Basement or PartialBasement)

| Scenario | TypeOfFoundation | BasementType |
|---|---|---|
| 1 | Basement | Finished |
| 2 | Basement | Unfinished |
| 3 | PartialBasement | Finished |
| 4 | PartialBasement | Unfinished |
| 5 | SlabOnGrade | _(absent — commented out)_ |

These 5 dependency scenarios can be **merged into the independent field matrix** — just overlay them onto the same rows. If the independent matrix has 5+ rows, merge directly. If fewer, add rows as needed.

#### Phase E: Coverage Tracking

Maintain a coverage tracker as you build scenarios:

```
Field: RoofType — Values: [Asphalt ✓(map_001), Metal ✓(map_002), Tile ✓(map_003), Slate ✓(map_004)]
Field: IsFire — Values: [true ✓(map_001), false ✓(map_002)]
Field: ConstructionType — Values: [Frame ✓(map_001), Masonry ✓(map_002), ...]
```

**Once all values for a field are covered, that field does NOT need to be varied further.** In remaining scenarios, use any valid value — no need to keep cycling.

#### Phase F: Applying to Focused Mode

In focused mode with only a few fields:
- Same principle: pack all focused fields into the minimum number of scenarios
- Number of scenarios = MAX value count across the focused fields
- If 3 focused fields have 4, 6, and 2 values → only 6 scenarios needed (not 12)

### Summary Formula

```
Total mapping scenarios ≈ MAX(values per independent field) + SUM(combinations per dependency cluster)
```

This typically reduces 20-30 one-field-per-request scenarios down to 5-10 packed scenarios.

---

## Step 4: Design Scenarios by Testing Type

### Type 1: Mapping Testing (prefix: `map_`)

**Apply the combinatorial optimization from Step 3.7. All fields from ALL blinds are packed together — NOT blind-by-blind.**

#### Overall Mode:

1. **map_001: Base default** — state-corrected address, all fields at sample values. This is the control scenario.

2. **map_002 to map_N: Packed mapping scenarios** — use the combinatorial matrix:
   - Each scenario varies MULTIPLE independent fields from ANY blind simultaneously
   - A single scenario can change fields from Home, Structure, Features, Policy, and Applicant blinds at the same time — because they are independent
   - Dependent field clusters are overlaid onto the matrix
   - The JSON header MUST list ALL fields changed and their values (since multiple fields change per scenario)
   - The TEST_CASE_GUIDE.md documents which field values are covered in which scenario

3. **Conditional add/remove scenarios** — for parent-child relationships:
   - Scenarios where parent triggers child (child present with each valid value)
   - Scenarios where parent doesn't trigger (child absent/commented out)
   - These are merged into the packed matrix wherever possible

**The goal:** Cover every Enum value for every HOA-specific field across ALL blinds using the MINIMUM number of request bodies. Every value must appear in at least one scenario. No value should be left untested. Fields from different blinds are packed together because they are independent.

#### Focused Mode:

Same packing logic but only for the focused fields:
- Identify focused fields + their related conditional fields
- Build the value matrix for just these fields
- Number of scenarios = MAX(value count across focused fields) + dependency combinations
- Non-focused fields stay at sample/default values in every scenario

### Type 2: Result Page Testing (prefix: `res_`)

**Also apply combinatorial optimization** for coverage fields:

Pack multiple coverage field variations into each scenario:
- res_001: Base coverage values → verify base response
- res_002-N: Rotate through PLPersonalLiability, DwellingMedicalPayments, PLAllPerilsDeductible, deductible variations simultaneously
- Each scenario varies ALL coverage fields together (they are independent)
- Number of scenarios = MAX(value count across coverage fields)

Each scenario must document what response field/value is expected for EVERY varied field.

### Type 3: UUD & Default Testing (prefixes: `uud_`, `def_`)

**UUD Scenarios (no packing — each must be isolated):**
UUD tests are special: each tests ONE ineligible value to confirm it triggers decline. Do NOT combine multiple UUD values — we need to know which specific value caused the decline.

- uud_001: PLConstructionType = Log → expect INELIGIBLE/decline
- uud_002: PLConstructionType = Asbestos → expect INELIGIBLE/decline
- uud_003: PLConstructionType = EFIS → expect INELIGIBLE/decline
- uud_004: PLNumberOfStories = 3.5 → expect INELIGIBLE/decline
- uud_005: PLNumberOfStories = 4 → expect INELIGIBLE/decline

**Default Scenarios:**
- def_001: Normal base request → verify all 20 defaults present in carrier XML
- def_002: If state has conditional defaults (e.g., IL mineSubsidence, VA/AZ Flooring) → verify those specific defaults
- def_003: PersonalLineReplacementCost variations → verify minimum deductible default changes

### Type 4: Relevancy Testing (prefixes: `rel_`, `crel_`)

**No packing for field relevancy — each must remove exactly ONE field** to isolate which field triggered the error.

**Delegate to `skills/relevancy-tester/SKILL.md`** for detailed generation logic.

---

## Step 5: Generate JSON Files

For each scenario:

1. Start with the sample request body
2. ONLY modify HOA-specific fields per the scenario
3. Keep ALL other fields exactly as-is
4. Apply conditional add/remove based on relevancy conditions
5. Add header comment block:
```json
// ================================================================
// Scenario: {Scenario Name}
// Type: {Mapping|Relevancy|ResultPage|UUD|Defaults}
// Blind: {BlindName or "Packed"} | Category: {testing type}
// State: {STATE} | Carrier: HOA | LOB: HO3
// Focus: {plain English — what this scenario validates}
//
// Fields Changed (this scenario varies ALL of these simultaneously):
//   - {FieldName1} = {Value1} (Enum: {EnumValue}, Expected Mapping: {CarrierValue})
//   - {FieldName2} = {Value2} (Enum: {EnumValue}, Expected Mapping: {CarrierValue})
//   - {FieldName3} = {Value3} (first time tested in this scenario)
//   ... (list every field that differs from the base/sample)
//
// Commented Out: {list or "None"}
// Added: {list or "None"}
// New Values Covered: {fields getting a NEW value not tested in prior scenarios}
// Focus Area: {field names, if focused mode}
// ================================================================
```

The `New Values Covered` line is key — it highlights which field-value combinations are being tested for the FIRST TIME in this scenario. This makes it easy to see at a glance what new coverage this scenario adds.

6. Save to `output/runs/{RUN}/request-bodies/{prefix}_{NNN}_{description}___{Category}.json`

---

## Step 6: Generate Scenario Document

Excel with sheets:
1. **Scenario Summary** — #, name, file, type, blind/category, description, fields changed, expected outcome
2. **Field Value Matrix** — every HOA field × every scenario, showing value/commented-out/added
3. **Conditional Rules Tested** — which rules, which scenarios test them
4. **Coverage Values Reference** — all valid Enums per coverage field for this state

---

## Step 7: Generate TEST_CASE_GUIDE.md

Create a plain-English test case guide at `output/runs/{RUN}/scenarios/TEST_CASE_GUIDE.md`:

```markdown
# HOA HO3 {STATE} — Test Case Guide
Generated: {timestamp}
Mode: {Focused|Overall} | Testing Types: {list}
Run Folder: {RUN}

---

## Relevancy Testing

### Testing Area: Field Relevancy — {Field Name}
Validates that {field_name} is required by the API and its available values match the requirement doc.

| # | File | What It Tests |
|---|---|---|
| 001 | rel_001_RoofType___Relevancy.json | RoofType removed → API should error with required + list values |

### Testing Area: Conditional Relevancy — {Parent} → {Child}
Validates that {child} is required when {parent} condition is met.

| # | File | What It Tests |
|---|---|---|
| 001 | crel_001_Foundation_BasementType___ConditionalRelevancy.json | Foundation=Basement + BasementType removed → error expected |

---

## Mapping Testing

### Coverage Matrix
Shows which field values are tested in which scenario. Each cell shows the value used.
Fields are packed together — multiple independent fields change per scenario.

| Scenario | ConstructionType | RoofType | NoOfStories | IsFire | IsSmoke | Foundation | BasementType |
|---|---|---|---|---|---|---|---|
| map_001 | _(base default)_ | _(base)_ | _(base)_ | _(base)_ | _(base)_ | _(base)_ | _(base)_ |
| map_002 | **Frame** | **Asphalt** | **1** | **true** | **true** | Basement | **Finished** |
| map_003 | **Masonry** | **Metal** | **1.5** | **false** | **false** | **PartialBasement** | **Unfinished** |
| map_004 | **Brick** | **Tile** | **2** | true | true | **SlabOnGrade** | _(absent)_ |
| map_005 | **Steel** | **Slate** | **2.5** | false | false | Basement | Finished |
| map_006 | **Stucco** | Asphalt | **3** | true | true | PartialBasement | Unfinished |

**Bold** = first time this value is tested. Non-bold = already covered, using any valid value.

### Scenario Details

| # | File | Fields Changed (all at once) |
|---|---|---|
| 001 | map_001_base_default___Packed.json | Base default — control scenario, no changes |
| 002 | map_002_combo_01___Packed.json | ConstructionType=Frame, RoofType=Asphalt, Stories=1, IsFire=true, IsSmoke=true, Foundation=Basement+BasementType=Finished |
| 003 | map_003_combo_02___Packed.json | ConstructionType=Masonry, RoofType=Metal, Stories=1.5, IsFire=false, IsSmoke=false, Foundation=PartialBasement+BasementType=Unfinished |
| 004 | map_004_combo_03___Packed.json | ConstructionType=Brick, RoofType=Tile, Stories=2, Foundation=SlabOnGrade (BasementType absent) |

### Conditional Field Coverage
Tests parent-child relationships embedded within the packed scenarios.

| Parent | Triggering Value | Child | Tested In |
|---|---|---|---|
| TypeOfFoundation | Basement | BasementType=Finished | map_002 |
| TypeOfFoundation | PartialBasement | BasementType=Unfinished | map_003 |
| TypeOfFoundation | SlabOnGrade | BasementType _(absent)_ | map_004 |

---

## Result Page Testing

### Testing Area: Coverage Verification
Tests that coverage values in the response match what was sent.

| # | File | What It Tests |
|---|---|---|
| 001 | res_001_base_coverages___ResultPage.json | Default coverage values → verify all 12 coverages in response |
| 002 | res_002_high_liability___ResultPage.json | PLPersonalLiability=FiveHundred → response shows $500,000 |

---

## UUD & Default Testing

### Testing Area: UUD Ineligibility
Tests that ineligible values cause the API to decline the quote.

| # | File | What It Tests |
|---|---|---|
| 001 | uud_001_construction_log___UUD.json | PLConstructionType=Log → API should decline (INELIGIBLE) |
| 002 | uud_002_stories_four___UUD.json | PLNumberOfStories=4 → API should decline (INELIGIBLE) |

### Testing Area: Default Values
Tests that all 20 hardcoded defaults are present in the carrier request XML.

| # | File | What It Tests |
|---|---|---|
| 001 | def_001_base_defaults___Defaults.json | Normal request → all 20 defaults present at correct XPaths |
```

**Grouping rules:**
- Group scenarios by "testing area" (a field or group of related fields being varied together)
- Each testing area gets a plain-English summary sentence explaining what it validates
- Each scenario within the area gets a one-line description of exactly what changed and what is expected
- Organized under main sections matching the 4 testing types

---

## Low-Priority Fields and Value Threshold Rules

### Low-priority lists — sample, don't exhaust
These fields have many values but are NOT critical for carrier mapping testing. **Randomly sample 2-3 unique values** and rotate them across packed scenarios:

- `CurrentPersonalHomeownerCarrierList` — prior carrier selection
- `CurrentPersonalAutoCarrierList` — prior auto carrier
- `PLLossDescriptionList` — loss descriptions
- `EmploymentIndustryList` — employment industries
- `OccupationStrList` — occupations (often hundreds of values)

### Low-priority blinds — minimal variation
- **Start blind** — address/agent fields, generally not carrier-mapping critical. One base scenario is sufficient.
- **Applicant blind** — name, DOB, contact fields. Vary MaritalStatus and Gender but don't exhaustively test every occupation/industry combination.

### High-value-count threshold (15+ values)
When building the combinatorial matrix (Step 3.7), if ANY field's list has **more than 15 values**, do NOT automatically include all values. Instead:

1. **Ask the user:** "Field {FieldName} has {N} possible values. Test all {N} (adds ~{X} scenarios) or sample a representative subset?"
2. **Default if not asked / no response:** Sample 3-5 representative values:
   - First value in the list
   - Last value in the list
   - Middle value
   - Most commonly used value (if known)
   - One state-specific value (if applicable)
3. Rotate these sampled values across packed scenarios
4. Only test ALL values when the user explicitly requests it

This significantly reduces scenario count for fields like OccupationStr (200+ values) while still validating the mapping mechanism works.

---

## Request Body Reuse Check

Before generating, check if request bodies already exist for this state in a previous run folder (`output/runs/{STATE}_*/request-bodies/`). If found:

> "I found existing request bodies for {STATE} from run {previous_timestamp}. Do you want to reuse those, or generate fresh ones?"

- If reuse → copy existing request bodies to the new run folder, skip generation
- If fresh → proceed with normal generation

---

## Important Reminders
- NEVER modify fields not in HOA's test data — they belong to other carriers
- Use Enum values from the Lists sheet, NOT Interview Values
- None/NoCoverage = real values to send
- Comment out ONLY when Relevancy Condition explicitly says to
- Add fields ONLY when Relevancy Condition triggers them
- AVOID UUD-triggering values (Log/Asbestos/EFIS for construction, 3.5/4 for stories) in mapping scenarios — use them ONLY in UUD scenarios
- Use correct file prefixes: `rel_`, `crel_`, `map_`, `res_`, `uud_`, `def_`
- Low-priority fields: sample 2-3 values, don't exhaust all values
- Fields with 15+ values: ask user before exhaustive testing
