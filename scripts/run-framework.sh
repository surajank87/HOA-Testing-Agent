#!/bin/bash
# Run the C# NUnit framework
# Usage: ./scripts/run-framework.sh [optional: specific test filter]

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

FRAMEWORK_SLN="$PROJECT_ROOT/framework/Automation/Automation/MappingVerification.sln"

if [ ! -f "$FRAMEWORK_SLN" ]; then
    echo "Error: Framework solution not found at: $FRAMEWORK_SLN"
    echo "   Please ensure the C# framework is set up in the framework/ folder"
    exit 1
fi

echo "Running HOA test framework..."
echo "   Solution: $FRAMEWORK_SLN"
echo ""

# Run the tests
if [ -n "$1" ]; then
    # Run with filter
    echo "   Filter: $1"
    dotnet test "$FRAMEWORK_SLN" --filter "$1" --logger "console;verbosity=normal"
else
    # Run all tests
    dotnet test "$FRAMEWORK_SLN" --logger "console;verbosity=normal"
fi

EXIT_CODE=$?

if [ $EXIT_CODE -eq 0 ]; then
    echo ""
    echo "Framework execution completed successfully"
else
    echo ""
    echo "Framework execution completed with exit code: $EXIT_CODE"
    echo "   Some tests may have failed -- check the results"
fi

exit $EXIT_CODE
