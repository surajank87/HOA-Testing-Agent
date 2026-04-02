#!/bin/bash
# Pre-test hook: Copy generated JSON request files to framework input folder
# Usage: ./hooks/pre-test-run.sh {STATE}

set -e

STATE="${1:?Usage: $0 STATE_CODE (e.g., IL, AZ, FL)}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

REQUEST_BODIES="$PROJECT_ROOT/output/request-bodies/$STATE"
FRAMEWORK_INPUT="$PROJECT_ROOT/framework/Automation/Automation/TestProject/RequestBodies/HOA/$STATE"

# Check source exists
if [ ! -d "$REQUEST_BODIES" ]; then
    echo "Error: output/request-bodies/$STATE/ folder not found"
    echo "   Run /generate-requests $STATE first"
    exit 1
fi

# Count JSON files
FILE_COUNT=$(ls "$REQUEST_BODIES"/*.json 2>/dev/null | wc -l)
if [ "$FILE_COUNT" -eq 0 ]; then
    echo "Error: No .json files in output/request-bodies/$STATE/"
    exit 1
fi

# Create framework input folder if needed
mkdir -p "$FRAMEWORK_INPUT"

# Clear existing files
rm -f "$FRAMEWORK_INPUT"/*.json

# Copy
cp "$REQUEST_BODIES"/*.json "$FRAMEWORK_INPUT/"

echo "Copied $FILE_COUNT request files to framework"
echo "   From: $REQUEST_BODIES"
echo "   To:   $FRAMEWORK_INPUT"
