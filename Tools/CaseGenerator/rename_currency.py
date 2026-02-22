#!/usr/bin/env python3
"""
Rename currency across all case JSON files and design documents.

Usage:
    python rename_currency.py "knots" "credits"
    python rename_currency.py "Knotmark" "Credit"

This does a case-sensitive find-and-replace across:
  - All case JSON files in StreamingAssets/content/cases/
  - game_design.md
  - evidence_references.json
  - All brief files in briefs/
"""

import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
CASES_DIR = PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"

SCAN_PATHS = [
    CASES_DIR,
    SCRIPT_DIR / "briefs",
]

SCAN_FILES = [
    SCRIPT_DIR / "game_design.md",
    SCRIPT_DIR / "evidence_references.json",
    SCRIPT_DIR / "story_bible.md",
    PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "world_config.json",
]


def rename(old: str, new: str):
    count = 0
    files_changed = []

    # Scan directories
    for scan_dir in SCAN_PATHS:
        if not scan_dir.exists():
            continue
        for f in scan_dir.rglob("*"):
            if f.suffix in (".json", ".md") and f.is_file():
                text = f.read_text(encoding="utf-8")
                if old in text:
                    f.write_text(text.replace(old, new), encoding="utf-8")
                    n = text.count(old)
                    count += n
                    files_changed.append((f.name, n))

    # Scan individual files
    for f in SCAN_FILES:
        if not f.exists():
            continue
        text = f.read_text(encoding="utf-8")
        if old in text:
            f.write_text(text.replace(old, new), encoding="utf-8")
            n = text.count(old)
            count += n
            files_changed.append((f.name, n))

    return count, files_changed


def main():
    if len(sys.argv) != 3:
        print("Usage: python rename_currency.py <old_name> <new_name>")
        print('Example: python rename_currency.py "knots" "credits"')
        sys.exit(1)

    old, new = sys.argv[1], sys.argv[2]
    total, changed = rename(old, new)

    if total == 0:
        print(f'No occurrences of "{old}" found.')
    else:
        print(f'Replaced {total} occurrences of "{old}" → "{new}" in {len(changed)} file(s):')
        for fname, n in changed:
            print(f"  {fname}: {n} replacement(s)")


if __name__ == "__main__":
    main()
