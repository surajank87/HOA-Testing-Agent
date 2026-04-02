# Request Body Generator Skill

## Purpose
Takes the generated test data Excel and a sample request body, then produces multiple JSON request body files covering different test scenarios.

## Input
- Test data Excel: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`
- Sample request body: `sample-requests/SampleRequestHOA{STATE}.json` (e.g., `SampleRequestHOAIL.json` for IL)

## Output
- `output/request-bodies/{STATE}/` — folder with all JSON scenario files for the state
- `output/scenarios/HOA_HO3_{STATE}_TestScenarios.xlsx` — scenario documentation

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

### Step 4: Design Scenarios by Blind

**Start Blind:**
- req_001: Base default (state-corrected address)

**Home Blind:**
- req_002-N: PLTypeOfDwelling variations (if applicable for state)
- req_N: ArchitectureStyle variations
- req_N: OccupancyType variations
- req_N: Dog count variations (0, 1, 2+)
- req_N: ViciousExoticAnimals true/false (if applicable for state)
- req_N: HomeUnderConstruction true/false
- req_N: ShortTermRental true/false

**Structure Blind:**
- req_N: PLConstructionType variations (AVOID Log, Asbestos, EFIS — those are UUD/ineligible)
- req_N: TypeOfFoundation variations → test BasementType child field add/remove
- req_N: PLNumberOfStories variations (AVOID 3.5, 4 — those are UUD/ineligible)
- req_N: RoofType variations
- req_N: ExteriorWallsConstruction variations
- req_N: MitRoofShape (if applicable — AZ only)
- req_N: Mitigation fields (if FL)

**Features Blind:**
- req_N: Garage true/false → TypeGarageCarport add/remove
- req_N: Pool true/false → PL_PoolType add/remove
- req_N: Heating type variations + update fields
- req_N: Plumbing type variations + update fields
- req_N: Electrical update variations
- req_N: Security features (FireDetection, BurglarAlarm → type fields add/remove)

**Policy Blind:**
- req_N: PLPersonalLiability value cycling (all valid Enums for state)
- req_N: DwellingMedicalPayments value cycling
- req_N: PLAllPerilsDeductible value cycling (state-specific values)
- req_N: WindstormDeductible / AnnualHurricaneDed (depending on state)
- req_N: CurrentPersonalHomeownerCarrier = NoPriorInsurance → triggers reason field
- req_N: PLHaveAnyLosses = true → triggers PersonalLosses array
- req_N: PersonalLineReplacementCost variations (affects minimum deductible rules in Defaults)

**Applicant Blind:**
- req_N: IsMailAddress true → mailing address fields added
- req_N: IsMailAddress false → mailing address fields as sample
- req_N: AnyAdditionalInsured true → co-applicant fields added
- req_N: AnyAdditionalInsured false → co-applicant fields removed
- req_N: CreditCheckPermission (state-specific behavior)
- req_N: MaritalStatus variations
- req_N: Gender variations

**CrossBlind:**
- req_N: Scenarios testing multiple conditions simultaneously

### Step 5: Generate JSON Files

For each scenario:

1. Start with the sample request body
2. ONLY modify HOA-specific fields per the scenario
3. Keep ALL other fields exactly as-is
4. Apply conditional add/remove based on relevancy conditions
5. Add header comment block:
```json
// ================================================================
// Scenario: {Scenario Name}
// Blind: {BlindName}
// State: {STATE} | Carrier: HOA | LOB: HO3
// Description: {What this tests}
// HOA Fields Changed: {list}
// Commented Out: {list or "None"}
// Added: {list or "None"}
// ================================================================
```
6. Save to `output/request-bodies/{STATE}/req_{NNN}_{description}___{BlindName}.json`

### Step 6: Generate Scenario Document

Excel with sheets:
1. **Scenario Summary** — #, name, file, blind, description, fields changed, expected outcome
2. **Field Value Matrix** — every HOA field × every scenario, showing value/commented-out/added
3. **Conditional Rules Tested** — which rules, which scenarios test them
4. **Coverage Values Reference** — all valid Enums per coverage field for this state

### Important Reminders
- NEVER modify fields not in HOA's test data — they belong to other carriers
- Use Enum values from the Lists sheet, NOT Interview Values
- None/NoCoverage = real values to send
- Comment out ONLY when Relevancy Condition explicitly says to
- Add fields ONLY when Relevancy Condition triggers them
- AVOID UUD-triggering values (Log/Asbestos/EFIS for construction, 3.5/4 for stories) — unless specifically testing UUD behavior
