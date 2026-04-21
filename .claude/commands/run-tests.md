# /run-tests

Execute the generated request bodies via the C# automation framework.

## Usage
```
/run-tests {STATE_CODE}
```
Example: `/run-tests IL`

## Prerequisites
- A run folder must exist with request bodies in `output/runs/{STATE}_{timestamp}/request-bodies/`
- If no run folder exists, run `/generate-requests {STATE}` first
- C# framework must be set up in `framework/Automation/Automation/`

**Finding the run folder:** Use the most recent run folder for the given state in `output/runs/`. If multiple exist, ask the user which one to use.

## What this does

### Step 1: Identify Run Folder
- Find the latest run folder for the state: `output/runs/{STATE}_*/`
- If multiple exist, show the list and ask the user which one
- Confirm the run folder with the user

### Step 2: Prepare Framework
1. Clears old request bodies from `framework/Automation/Automation/TestProject/RequestBodies/HOA/{STATE}/`
2. Copies all JSON files from `output/runs/{RUN}/request-bodies/` to `framework/Automation/Automation/TestProject/RequestBodies/HOA/{STATE}/`
3. Updates `framework/Automation/Automation/TestProject/config.json` for HOA carrier and target state

### Step 3: Execute Tests with Progress Monitoring
1. Count total scenarios (JSON files copied)
2. Execute: `dotnet test "framework/Automation/Automation/MappingVerification.sln"`
3. **While tests are running**, monitor the framework output folder by periodically counting `*_details.json` files in `framework/Automation/Automation/TestProject/Output/HOA/{STATE}/`
4. Display granular progress:
```
🚀 Running tests via C# framework...
   ⏳ 5/25 scenarios completed...
   ⏳ 12/25 scenarios completed...
   ⏳ 20/25 scenarios completed...
   ✅ 25/25 scenarios completed
```
5. Check every 10-15 seconds until dotnet test completes

### Step 4: Collect Results
1. Copy results from `framework/Automation/Automation/TestProject/Output/HOA/{STATE}/` to `output/runs/{RUN}/results/`
2. Report summary: how many sent, succeeded, declined, failed

### Step 5: Format Carrier XML
For each result file in `output/runs/{RUN}/results/`:
1. Read `{scenario}_carrier_request.txt`
   - Strip outer `<Request>` wrapper tag if present
   - If inner content is JSON → parse and pretty-print with indentation
   - If inner content is XML → parse and indent with 2-space indentation
   - Save as `{scenario}_carrier_request_formatted.xml` (or `.json` based on content type)
2. Read `{scenario}_carrier_response.txt`
   - Parse XML content and indent with 2-space indentation
   - Save as `{scenario}_carrier_response_formatted.xml`
3. Keep original raw files unchanged

### Step 6: Summary
Report final summary:
```
✅ Test execution complete
   📁 Run folder: output/runs/{RUN}/
   📊 {N} scenarios: {S} Success, {D} Declined, {F} Failed
   📄 Formatted carrier XML saved alongside originals
```

## Framework Config for HOA
```json
{
  "Tenant": "Unify",
  "LOB": "PersonalHome",
  "Carrier": "HOA",
  "PersonalLineOrCommercialLine": "PersonalLine",
  "Environment": "QA",
  "SubTenant": "MarketsLib",
  "CarrierRequestFormat": "xml",
  "State": "{STATE}"
}
```

## Output
- `output/runs/{RUN}/results/` — carrier request/response pairs per scenario (raw + formatted)
- Framework also generates ExtentReport HTML in `framework/Automation/Automation/TestProject/Execution Reports/{STATE}/`
