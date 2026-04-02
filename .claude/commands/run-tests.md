# /run-tests

Execute the generated request bodies via the C# automation framework.

## Usage
```
/run-tests {STATE_CODE}
```
Example: `/run-tests IL`

## Prerequisites
- Request bodies must exist in `output/request-bodies/{STATE}/`
- Sample request must have been provided: `sample-requests/SampleRequestHOA{STATE}.json`
- C# framework must be set up in `framework/Automation/Automation/`

If request bodies don't exist, run `/generate-requests {STATE}` first.

## What this does
1. Clears old request bodies from `framework/Automation/Automation/TestProject/RequestBodies/HOA/{STATE}/`
2. Copies all JSON files from `output/request-bodies/{STATE}/` to `framework/Automation/Automation/TestProject/RequestBodies/HOA/{STATE}/`
3. Updates `framework/Automation/Automation/TestProject/config.json` for HOA carrier and target state
4. Executes: `dotnet test "framework/Automation/Automation/MappingVerification.sln"`
5. Waits for all tests to complete
6. Copies results from `framework/Automation/Automation/TestProject/Output/HOA/{STATE}/` to `output/results/{STATE}/`
7. Reports summary: how many sent, succeeded, failed

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
- `output/results/{STATE}/` — folder with carrier request/response pairs per scenario
- Framework also generates ExtentReport HTML in `framework/Automation/Automation/TestProject/Execution Reports/{STATE}/`
