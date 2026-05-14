#!/usr/bin/env python3
"""Remove all // and /// comments from .cs files under Packages/."""

import re
import sys
from pathlib import Path

ROOT = Path(__file__).parent
PATTERN = re.compile(r'^(\s*)//.*$')

stats = {"files": 0, "lines_removed": 0, "blank_lines_collapsed": 0}


def process_file(path: Path) -> None:
    original = path.read_text(encoding="utf-8")
    lines = original.split("\n")
    result = []
    removed = 0

    for line in lines:
        if PATTERN.match(line):
            removed += 1
            continue
        result.append(line)

    # Collapse runs of 3+ blank lines into 1
    collapsed = []
    blank_run = 0
    for line in result:
        if line.strip() == "":
            blank_run += 1
            if blank_run <= 2:
                collapsed.append(line)
        else:
            blank_run = 0
            collapsed.append(line)

    output = "\n".join(collapsed)
    if output.endswith("\n\n"):
        output = output[:-1]

    if output != original:
        path.write_text(output, encoding="utf-8")
        stats["files"] += 1
        stats["lines_removed"] += removed
        print(f"  {path.relative_to(ROOT)}: removed {removed} comment lines")


def main() -> None:
    dry_run = "--dry-run" in sys.argv

    cs_files = sorted(ROOT.rglob("*.cs"))
    print(f"Scanning {len(cs_files)} .cs files...\n")

    if dry_run:
        for f in cs_files:
            lines = f.read_text(encoding="utf-8").split("\n")
            removed = sum(1 for l in lines if PATTERN.match(l))
            if removed:
                stats["files"] += 1
                stats["lines_removed"] += removed
                print(f"  {f.relative_to(ROOT)}: would remove {removed} comment lines")
        print(f"\nDry run. {stats['files']} files would change, {stats['lines_removed']} comment lines would be removed.")
    else:
        for f in cs_files:
            process_file(f)
        print(f"\nDone. {stats['files']} files changed, {stats['lines_removed']} comment lines removed.")


if __name__ == "__main__":
    main()
