#!/bin/bash
# Post-test hook: Collect results from framework output folder
# Usage: ./hooks/post-test-run.sh {STATE}

set -e

STATE="${1:?Usage: $0 STATE_CODE (e.g., IL, AZ, FL)}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

FRAMEWORK_OUTPUT="$PROJECT_ROOT/framework/Automation/Automation/TestProject/Output/HOA/$STATE"
OUTPUT_RESULTS="$PROJECT_ROOT/output/results/$STATE"

# Check source exists
if [ ! -d "$FRAMEWORK_OUTPUT" ]; then
    echo "Error: No results found in framework output for $STATE"
    echo "   Expected: $FRAMEWORK_OUTPUT"
    echo "   The framework may not have run successfully"
    exit 1
fi

# Create output folder
mkdir -p "$OUTPUT_RESULTS"

# Copy results
cp -r "$FRAMEWORK_OUTPUT"/* "$OUTPUT_RESULTS/"

# Count result files
RESULT_COUNT=$(ls "$OUTPUT_RESULTS"/*_details.json 2>/dev/null | wc -l)

echo "Results collected: $RESULT_COUNT scenarios"
echo "   From: $FRAMEWORK_OUTPUT"
echo "   To:   $OUTPUT_RESULTS"
