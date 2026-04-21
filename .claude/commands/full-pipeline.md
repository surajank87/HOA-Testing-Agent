# /full-pipeline

Run the complete end-to-end testing pipeline for a state.

## Usage
```
/full-pipeline {STATE_CODE}
```
Example: `/full-pipeline AZ`

## What this does
Executes all 4 phases in sequence with a single timestamped run folder.

### Phase 0: User Configuration

**Step 0.1 — Testing Mode:**
Ask the user:
> **What testing mode do you want?**
> 1. **Focused Testing** — Provide specific areas/changes and I generate exhaustive edge-case scenarios for ONLY those fields
> 2. **Overall Testing** — Full regression with packed scenarios covering all HOA fields across all blinds

If Focused: ask the user to describe what changed or which fields/areas to test.

**Step 0.2 — Testing Types:**
Ask the user:
> **Which testing types do you want to run?**
> 1. **Relevancy Testing** — Verify required fields and available values
> 2. **Mapping Testing** — Verify field value mappings
> 3. **Result Page Testing** — Verify coverage display in response
> 4. **UUD & Defaults** — Verify ineligibility rules and default values
> 5. **All of the above**

**Step 0.3 — Create Run Folder:**
Create `output/runs/{STATE}_{YYYY-MM-DD}_{HH-MM-SS}/` with subdirectories and `run-metadata.json`.

---

### Phase 1: Generate Test Data (Smart Reuse)
- **First check:** Does `output/test-data/HOA_HO3_{STATE}_TestData.xlsx` already exist?
  - If YES and requirement doc unchanged → **SKIP** (reuse existing)
  - If YES but requirement doc changed → **incremental update** (only apply changes, report diff to user)
  - If NO → generate from scratch
- Output: `output/test-data/HOA_HO3_{STATE}_TestData.xlsx`

```
📋 Phase 1: Test data for HOA HO3, state {STATE}...
   ✅ Test data already exists and is up-to-date. Skipping generation.
   — OR —
   ✅ Requirement doc changed. Updated: +2 fields, ~1 modified list, -0 removed.
   — OR —
   ✅ {N} fields extracted ({M} relevant for {STATE})
   ✅ {P} list tables with {Q} state-filtered values
```

### Phase 2: Generate Request Bodies (with Reuse Check)
- **First check:** Do request bodies exist for this state from a previous run?
  - If YES → ask user: "Reuse existing request bodies from {previous run}, or generate fresh?"
  - If user says reuse → copy to new run folder, skip generation
  - If user says fresh or none exist → generate new scenarios
- Reads test data + sample request → produces JSON scenario files based on selected mode and types
- Generates TEST_CASE_GUIDE.md with plain-English descriptions
- Output: `output/runs/{RUN}/request-bodies/` + `output/runs/{RUN}/scenarios/`

```
🔧 Phase 2: Generating request bodies...
   Mode: {Focused|Overall} | Types: {Relevancy, Mapping, ...}
   ✅ {N} test scenarios created
   ✅ TEST_CASE_GUIDE.md saved
   ✅ Scenario doc saved
```

### Phase 3: Run Tests
- Copies JSONs to framework → updates config → executes dotnet test → collects results
- **Shows per-scenario progress during execution**
- Formats carrier XML after collection
- Output: `output/runs/{RUN}/results/`

```
🚀 Phase 3: Running tests via C# framework...
   ⏳ 5/{N} scenarios completed...
   ⏳ 12/{N} scenarios completed...
   ✅ {N}/{N} scenarios completed
   ✅ {S} Success, {D} Declined, {F} Failed
   ✅ Carrier XML formatted
```

### Phase 4: Validate & Report
- Reads results → validates against test data per testing type → produces tabbed HTML report
- **Shows per-scenario progress during validation**
- Output: `output/runs/{RUN}/reports/`

```
📊 Phase 4: Validating results...
   ⏳ 5/{N} scenarios validated...
   ⏳ 12/{N} scenarios validated...
   ✅ {N}/{N} scenarios validated
   ✅ {P}% overall pass rate
   📈 Report saved: output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html
```

### Final Summary
```
🏁 Pipeline complete for {STATE}
   📁 Run folder: output/runs/{RUN}/
   📊 {N} scenarios | {P}% pass rate
   🧪 Types tested: {list}
   📈 Report: output/runs/{RUN}/reports/HOA_HO3_{STATE}_ValidationReport.html
```

## Error Handling
- If any phase fails, stop and report the error
- Save partial results from completed phases
- Don't proceed to the next phase if the current one produced no output
