#!/usr/bin/env bash
set -euo pipefail

# Note: This assumes the script is placed inside a subfolder like `scripts/`
# If you place it at the root of your repo instead, change to: REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PACKAGES_DIR="$REPO_ROOT/Packages"
DATETIME=$(date '+%Y-%m-%d %H:%M:%S')

# ── Helpers ──

info()  { echo "  $*"; }
ok()    { echo "  ✅ $*"; }
warn()  { echo "  ⚠  $*"; }
header(){ echo ""; echo "── $* ──"; }

# ── Phase 1: Packages ──

header "Phase 1: Packages"

ANY_PACKAGE_FAILED=false

# Dynamically find ALL nested directories under PACKAGES_DIR that contain a .git
# This completely bypasses .gitmodules and ensures nothing is left behind
PACKAGE_PATHS=()
if [ -d "$PACKAGES_DIR" ]; then
    # Using -print0 and -z to safely handle folder names with spaces
    while IFS= read -r -d '' git_path; do
        if [ -n "$git_path" ]; then
            PACKAGE_PATHS+=("$(dirname "$git_path")")
        fi
    done < <(find "$PACKAGES_DIR" -name ".git" -print0 2>/dev/null | sort -z)
fi

if [ ${#PACKAGE_PATHS[@]} -gt 0 ]; then
    for pkg in "${PACKAGE_PATHS[@]}"; do
        label="$(basename "$pkg")"
        cd "$pkg"
        
        # Stage all local changes (new, modified, deleted)
        git add -A 2>/dev/null || true
        
        # If there are staged changes, commit them unconditionally
        if ! git diff --cached --quiet 2>/dev/null; then
            info "Changes in $label:"
            git status --short
            git commit -m "chore: auto-update [${DATETIME}]" >/dev/null
        fi
        
        # Determine if we need to push (handles unpushed changes you forgot about)
        NEEDS_PUSH=false
        PUSH_ARGS=""
        upstream=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null || echo "")
        
        if [ -z "$upstream" ]; then
            # Detached HEAD or no upstream branch configured
            BRANCH=$(git symbolic-ref -q --short HEAD || echo "")
            if [ -z "$BRANCH" ]; then
                # Detached HEAD
                DEFAULT_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@' || echo "main")
                if ! git merge-base --is-ancestor HEAD "origin/$DEFAULT_BRANCH" 2>/dev/null; then
                    NEEDS_PUSH=true
                    PUSH_ARGS="origin HEAD:$DEFAULT_BRANCH"
                fi
            else
                # On a branch, no upstream
                NEEDS_PUSH=true
                PUSH_ARGS="-u origin $BRANCH"
            fi
        else
            # Check if there are any unpushed commits
            if [ -n "$(git rev-list "${upstream}..HEAD" 2>/dev/null)" ]; then
                NEEDS_PUSH=true
                BRANCH=$(git symbolic-ref -q --short HEAD)
                PUSH_ARGS="-u origin $BRANCH"
            fi
        fi
        
        if [ "$NEEDS_PUSH" = true ]; then
            if git push $PUSH_ARGS >/dev/null 2>&1; then
                ok "$label — pushed"
            else
                warn "$label — push failed"
                ANY_PACKAGE_FAILED=true
            fi
        else
            ok "$label — clean & up to date"
        fi
    done
else
    info "No packages found in $PACKAGES_DIR"
fi

# Stop execution to protect the parent repo if any package fails to push
if [ "$ANY_PACKAGE_FAILED" = true ]; then
    echo ""
    warn "Some packages failed to push. Parent repo will NOT be committed."
    exit 1
fi

# ── Phase 2: Parent repo ──

header "Phase 2: Parent repo"
cd "$REPO_ROOT"

# Stage all changes in the parent repo (including the updated package pointers)
git add -A 2>/dev/null || true

PARENT_HAS_CHANGES=false
if ! git diff --cached --quiet 2>/dev/null; then
    PARENT_HAS_CHANGES=true
fi

PARENT_NEEDS_PUSH=false
upstream=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null || echo "")

if [ -z "$upstream" ]; then
    PARENT_NEEDS_PUSH=true
else
    if [ -n "$(git rev-list "${upstream}..HEAD" 2>/dev/null)" ]; then
        PARENT_NEEDS_PUSH=true
    fi
fi

if [ "$PARENT_HAS_CHANGES" = false ] && [ "$PARENT_NEEDS_PUSH" = false ]; then
    ok "Parent repo — clean & up to date, nothing to do."
    echo ""
    ok "Done!"
    exit 0
fi

if [ "$PARENT_HAS_CHANGES" = true ]; then
    echo "📦 Changes to commit in Parent Repo:"
    git status --short
    echo ""
    git commit -m "chore: auto-update all packages [${DATETIME}]" >/dev/null
    PARENT_NEEDS_PUSH=true
fi

if [ "$PARENT_NEEDS_PUSH" = true ]; then
    if git push -u origin HEAD >/dev/null 2>&1; then
        ok "Parent repo — pushed"
    else
        warn "Parent repo — push failed"
        exit 1
    fi
fi

echo ""
ok "Done!"