#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

# Get current date/time for the commit message
DATETIME=$(date '+%Y-%m-%d %H:%M:%S')

# Check if there are any changes
if git diff --quiet && git diff --cached --quiet && [ -z "$(git ls-files --others --exclude-standard)" ]; then
    echo "✅ No changes to commit. Everything is up to date."
    exit 0
fi

# Stage all changes
git add -A

# Show what's being committed
echo "📦 Changes to commit:"
git status --short
echo ""

# Commit with timestamp
git commit -m "chore: auto-update all packages [${DATETIME}]"

# Push to origin
git push origin

echo "✅ Pushed to origin successfully."
