# Relevancy Tester Skill

## Purpose
Generates request bodies designed to validate field relevancy — verifying that mandatory fields are correctly required by the carrier API and that their available values match the requirement document. Also tests conditional (parent-child) field relevancy.

## Input
- Test data Excel: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- Sample request body: `sample-requests/SampleRequestHOA{STATE}.json`
- HOA requirement doc: `carrier-docs/HOA (BriteCore) - HO3 - Standardization - V1.xlsx` (for full Lists sheet)
- **Testing mode:** Focused (specific fields only) or Overall (all testable fields)

## Output
- `rel_{NNN}_{field_name}___Relevancy.json` — field relevancy test cases
- `crel_{NNN}_{parent}_{child}___ConditionalRelevancy.json` — conditional relevancy test cases
- All saved to `output/runs/{RUN}/request-bodies/`

---

## Concept: How Relevancy Testing Works

The Bolt API validates incoming requests. When a mandatory field is missing from the request body, the API returns a validation error that:
1. States the field is required
2. Lists the available/valid values for that field

**We exploit this behavior** to verify two things:
- **Field is required:** If we remove a mandatory field and the API does NOT error → the field may have been misconfigured as optional → BUG
- **Available values match:** If the API's list of available values differs from what the requirement document specifies → values are out of sync → BUG

---

## Process

### Step 1: Identify Testable Fields for Field Relevancy

From the test data Interview sheet, select fields that meet ALL criteria:
- **Mandatory** = Yes/True (Column H)
- **Has valid XPath** (Column N starts with `/root/...`)
- **Has a List reference** (Column K is not blank) — so we can compare available values
- **Applies to the target state** (state filtering already done in test data)

These are the fields where removing them should trigger a validation error AND we can verify the available values.

**In Focused Mode:** Only select fields that match the user's specified focus areas.

### Step 2: Generate Field Relevancy Test Cases

For each testable field:

1. **Start with the sample request body** (full valid request)
2. **Remove the target field entirely** from the JSON:
   - Do NOT set to null or empty string — actually remove the key
   - The API should detect it as missing and return a validation error
3. **Keep ALL other fields** at their valid sample values (the rest of the request must be valid so only the targeted field triggers the error)
4. **Do NOT remove other-carrier fields** — keep them as-is per multi-carrier rule

**File naming:** `rel_{NNN}_{field_name}___Relevancy.json`

**JSON header:**
```json
// ================================================================
// Scenario: Field Relevancy — {FieldName}
// Type: Relevancy
// State: {STATE} | Carrier: HOA | LOB: HO3
// Focus: Validates that {FieldName} is required by the API.
//        Removes {FieldName} from request and expects validation error
//        with available values matching the requirement doc.
// Field Removed: {FieldName}
// Expected: Validation error listing required field + available values
// Expected Values (from doc): {comma-separated list from Lists sheet}
// ================================================================
```

**Expected results from this test:**
- API returns validation error (NOT a successful quote)
- Error message mentions the removed field
- Error includes list of available values
- Those values should match the Lists sheet values for this field (state-filtered)

### Step 3: Identify Conditional Relationships

From the test data's Relevancy Condition column, extract parent-child pairs:

| Parent Field | Triggering Value | Child Field(s) |
|---|---|---|
| TypeOfFoundation | Basement, PartialBasement | BasementType |
| PL_AdditionalStructures_Garage | true | TypeGarageCarport, PL_NumberCarSpace |
| PL_AdditionalStructures_Pool | true | PL_PoolType |
| CurrentPersonalHomeownerCarrier | NoPriorInsurance | SelectReasonNoPriorHomeInsurance |
| HeatingUpdateYN | true | PLHeatingUpdate, HeatingUpdateYear |
| PlumbingUpdateYN | true | PLPlumbingUpdated, PlumbingUpdatedYear |
| ElectricalUpdateYN | true | PLElectricalUpdated, ElectricalUpdatedYear |
| IsMailAddress | true | MailingAddress fields |
| AnyAdditionalInsured | true | Co-applicant fields |
| FireDetection | true | FireDetectionType |
| BurglarAlarm | true | BurglarAlarmType |
| SmokeDetector | true | SmokeDetectorType |
| SprinklerSystem | true | SprinklerSystemType |
| PLHaveAnyLosses | true | PersonalLosses array |

**In Focused Mode:** Only include conditional relationships involving the focused fields.

### Step 4: Generate Conditional Relevancy Test Cases

For each parent-child relationship, generate TWO test cases:

#### Test A: Parent triggers → child should be required
1. Start with sample request body
2. Set parent field to the TRIGGERING value (e.g., `TypeOfFoundation = "Basement"`)
3. **Remove the child field** entirely (e.g., remove `BasementType`)
4. Keep everything else valid
5. **Expected:** API returns validation error for the child field (because parent triggers it, child becomes required)

**File naming:** `crel_{NNN}_{parent}_{child}_present___ConditionalRelevancy.json`

**JSON header:**
```json
// ================================================================
// Scenario: Conditional Relevancy — {Parent} triggers {Child}
// Type: ConditionalRelevancy
// State: {STATE} | Carrier: HOA | LOB: HO3
// Focus: When {Parent}={TriggeringValue}, {Child} should be required.
//        Sets {Parent}={TriggeringValue} and removes {Child}.
//        Expects validation error for {Child}.
// Parent: {Parent} = {TriggeringValue}
// Child Removed: {Child}
// Expected: Validation error — {Child} is required when {Parent}={TriggeringValue}
// ================================================================
```

#### Test B: Parent doesn't trigger → child should NOT be required
1. Start with sample request body
2. Set parent field to a NON-TRIGGERING value (e.g., `TypeOfFoundation = "SlabOnGrade"`)
3. **Remove the child field** entirely (e.g., remove `BasementType`)
4. Keep everything else valid
5. **Expected:** API accepts the request WITHOUT erroring for the child field (because parent doesn't trigger it, child is optional/irrelevant)

**File naming:** `crel_{NNN}_{parent}_{child}_absent___ConditionalRelevancy.json`

**JSON header:**
```json
// ================================================================
// Scenario: Conditional Relevancy — {Parent} does NOT trigger {Child}
// Type: ConditionalRelevancy
// State: {STATE} | Carrier: HOA | LOB: HO3
// Focus: When {Parent}={NonTriggeringValue}, {Child} should NOT be required.
//        Sets {Parent}={NonTriggeringValue} and removes {Child}.
//        Expects NO validation error for {Child}.
// Parent: {Parent} = {NonTriggeringValue}
// Child Removed: {Child}
// Expected: No error for {Child} — it should be optional when {Parent}={NonTriggeringValue}
// ================================================================
```

### Step 5: Value Comparison Reference

For each field relevancy test, document the expected values from the requirement doc:

```
Field: RoofType
List Name: PLRoofType
State: TX
Expected Values (from Lists sheet, state-filtered):
  - AsphaltShingle (Enum: AsphaltShingle, Mapping: 1)
  - Metal (Enum: Metal, Mapping: 3)
  - Tile (Enum: Tile, Mapping: 4)
  - Wood (Enum: Wood, Mapping: 5)
  - Slate (Enum: Slate, Mapping: 6)
  ...
```

This reference is used during validation (in `skills/result-validator/SKILL.md`) to compare against the API's returned available values.

---

## Important Notes

- **Remove the field, don't null it:** Setting a field to `null` or `""` may behave differently than removing it entirely. For relevancy testing, we need the field to be ABSENT from the JSON.
- **One field per test case:** Each `rel_` file removes exactly ONE field. This isolates which field triggered the validation error.
- **Multi-carrier rule still applies:** Only remove HOA-specific fields (those with valid XPaths). Never remove other-carrier fields.
- **State filtering:** Only test fields that are relevant for the target state.
- **Focused mode scope:** When focused, only generate relevancy tests for the user's specified fields and their immediate conditional relatives. Skip all other fields.
- **The validation of these results happens in `skills/result-validator/SKILL.md`** — this skill only generates the test case request bodies.
