#!/bin/bash

# Unity Test Runner Script
# This script runs Unity tests in batch mode
# Make sure Unity Editor is closed before running this script

UNITY_EDITOR="/home/l/Unity/Hub/Editor/6000.5.0a8/Editor/Unity"
PROJECT_PATH="/home/l/Github/BovineLabs"
TEST_RESULTS="TestResults.xml"

echo "=========================================="
echo "Unity Test Runner"
echo "=========================================="
echo "Project: $PROJECT_PATH"
echo "Results: $TEST_RESULTS"
echo ""

# Check if Unity Editor is already running with this project
if pgrep -f "Unity.*$PROJECT_PATH" > /dev/null; then
    echo "ERROR: Unity Editor is currently running with this project!"
    echo "Please close Unity Editor before running tests."
    echo ""
    echo "Running Unity processes:"
    ps aux | grep -i "Unity.*$PROJECT_PATH" | grep -v grep
    exit 1
fi

echo "Running EditMode tests..."
"$UNITY_EDITOR" \
    -runTests \
    -batchmode \
    -projectPath "$PROJECT_PATH" \
    -testResults "$TEST_RESULTS" \
    -testPlatform EditMode \
    -logFile - 2>&1

EXIT_CODE=$?

echo ""
echo "=========================================="
if [ $EXIT_CODE -eq 0 ]; then
    echo "Tests completed successfully!"
    
    # Display test results summary
    if [ -f "$PROJECT_PATH/$TEST_RESULTS" ]; then
        echo ""
        echo "Test Results Summary:"
        grep -E 'result=|passed=|failed=' "$PROJECT_PATH/$TEST_RESULTS" | head -1 | sed 's/.*<test-run/Test Run:/'
        
        # Check for failures
        FAILED=$(grep -oP 'failed="\K[0-9]+' "$PROJECT_PATH/$TEST_RESULTS" | head -1)
        PASSED=$(grep -oP 'passed="\K[0-9]+' "$PROJECT_PATH/$TEST_RESULTS" | head -1)
        
        echo "Passed: $PASSED"
        echo "Failed: $FAILED"
        
        if [ "$FAILED" != "0" ]; then
            echo ""
            echo "Failed tests details:"
            grep -A 3 'result="Failed"' "$PROJECT_PATH/$TEST_RESULTS" || echo "Check $TEST_RESULTS for details"
        fi
    fi
else
    echo "Tests failed with exit code: $EXIT_CODE"
    echo "Check the log output above for details."
fi
echo "=========================================="

exit $EXIT_CODE
