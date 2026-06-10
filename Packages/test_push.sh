        NEEDS_PUSH=false
        PUSH_ARGS=""
        upstream=$(git rev-parse --abbrev-ref '@{upstream}' 2>/dev/null || echo "")
        
        if [ -z "$upstream" ]; then
            BRANCH=$(git symbolic-ref -q --short HEAD || echo "")
            if [ -z "$BRANCH" ]; then
                DEFAULT_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@' || echo "main")
                if ! git merge-base --is-ancestor HEAD "origin/$DEFAULT_BRANCH" 2>/dev/null; then
                    NEEDS_PUSH=true
                    PUSH_ARGS="origin HEAD:$DEFAULT_BRANCH"
                fi
            else
                NEEDS_PUSH=true
                PUSH_ARGS="-u origin $BRANCH"
            fi
        else
            if [ -n "$(git rev-list "${upstream}..HEAD" 2>/dev/null)" ]; then
                NEEDS_PUSH=true
                BRANCH=$(git symbolic-ref -q --short HEAD)
                PUSH_ARGS="-u origin $BRANCH"
            fi
        fi
        
        if [ "$NEEDS_PUSH" = true ]; then
            if git push $PUSH_ARGS >/dev/null 2>&1; then
                ok "pushed"
            else
                warn "push failed"
            fi
        else
            ok "clean & up to date"
        fi
