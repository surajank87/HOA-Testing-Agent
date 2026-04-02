# /full-pipeline

Run the complete end-to-end testing pipeline for a state.

## Usage
```
/full-pipeline {STATE_CODE}
```
Example: `/full-pipeline AZ`

## What this does
Executes all 4 phases in sequence:

### Phase 1: Generate Test Data
- Reads HOA requirement doc → filters for state → produces test data Excel
- Output: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`

### Phase 2: Generate Request Bodies
- Reads test data + sample request (`sample-requests/SampleRequestHOA{STATE}.json`) → produces JSON scenario files
- Output: `output/request-bodies/{STATE}/` folder + scenario document

### Phase 3: Run Tests
- Copies JSONs to `framework/Automation/Automation/TestProject/RequestBodies/HOA/{STATE}/` → updates config → executes dotnet test → collects results
- Output: `output/results/{STATE}/` with carrier request/response pairs

### Phase 4: Validate & Report
- Reads results → validates against test data → produces graphical report
- Output: `output/reports/HOA_HO3_{STATE}_ValidationReport.html`

## Progress Display
Show progress at each phase:
```
📋 Phase 1: Generating test data for HOA HO3, state {STATE}...
   ✅ {N} fields extracted ({M} relevant for {STATE})
   ✅ {P} list tables with {Q} state-filtered values

🔧 Phase 2: Generating request bodies...
   ✅ {N} test scenarios created across 6 blinds
   ✅ Scenario doc saved

🚀 Phase 3: Running tests via C# framework...
   ✅ {N} requests sent
   ✅ {S} Success, {D} Declined, {F} Failed

📊 Phase 4: Validating results...
   ✅ {N} field checks performed
   ✅ {P}% pass rate
   📈 Report saved
```

## Error Handling
- If any phase fails, stop and report the error
- Save partial results from completed phases
- Don't proceed to the next phase if the current one produced no output
