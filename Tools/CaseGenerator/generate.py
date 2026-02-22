#!/usr/bin/env python3
"""
Case Generator for The Strange Rebellion of Marlo.

Reads a case brief (markdown), the story bible, and a system prompt,
then calls the Claude API to generate a valid case JSON file.

Usage:
    python generate.py briefs/core_01.md
    python generate.py briefs/core_01.md --output output/core_01.json
    python generate.py briefs/*.md              # Batch mode
    python generate.py briefs/core_02.md --validate  # Generate + validate
"""

import argparse
import json
import os
import sys
from pathlib import Path

try:
    import anthropic
except ImportError:
    print("Error: 'anthropic' package not installed. Run: pip install anthropic")
    sys.exit(1)


SCRIPT_DIR = Path(__file__).parent
STORY_BIBLE = SCRIPT_DIR / "story_bible.md"
SYSTEM_PROMPT = SCRIPT_DIR / "prompts" / "system_prompt.md"
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # Tools/CaseGenerator -> Tools -> project root
EXAMPLE_CASE = (
    PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"
    / "core_01_missing_ledger.json"
)
OUTPUT_DIR = SCRIPT_DIR / "output"
MODEL = "claude-sonnet-4-5-20250929"
MAX_TOKENS = 16000


def load_text(path: Path) -> str:
    """Load a text file, raising a clear error if missing."""
    if not path.exists():
        print(f"Error: Required file not found: {path}")
        sys.exit(1)
    return path.read_text(encoding="utf-8")


def build_system_prompt() -> str:
    """Build the full system prompt from template + story bible + example case."""
    system = load_text(SYSTEM_PROMPT)
    bible = load_text(STORY_BIBLE)

    # Include the gold-standard example case if available
    example = ""
    if EXAMPLE_CASE.exists():
        example_json = EXAMPLE_CASE.read_text(encoding="utf-8")
        example = f"\n\n## Gold Standard Example\n\nHere is a complete, valid case JSON (core_01) as a reference:\n\n```json\n{example_json}\n```\n"

    return f"{system}\n\n## Story Bible (condensed world context)\n\n{bible}{example}"


def generate_case(brief_path: Path, client: anthropic.Anthropic) -> dict:
    """Generate a case JSON from a brief file."""
    brief = load_text(brief_path)
    system_prompt = build_system_prompt()

    print(f"  Generating case from: {brief_path.name}")
    print(f"  Using model: {MODEL}")
    print(f"  System prompt length: {len(system_prompt):,} chars")
    print(f"  Brief length: {len(brief):,} chars")

    message = client.messages.create(
        model=MODEL,
        max_tokens=MAX_TOKENS,
        system=system_prompt,
        messages=[
            {
                "role": "user",
                "content": (
                    f"Generate a complete, valid case JSON from this brief. "
                    f"Output ONLY the raw JSON — no markdown fences, no explanations.\n\n"
                    f"{brief}"
                ),
            }
        ],
    )

    # Extract the text content
    response_text = ""
    for block in message.content:
        if block.type == "text":
            response_text += block.text

    # Clean up: strip any markdown fences if the model added them
    response_text = response_text.strip()
    if response_text.startswith("```json"):
        response_text = response_text[7:]
    if response_text.startswith("```"):
        response_text = response_text[3:]
    if response_text.endswith("```"):
        response_text = response_text[:-3]
    response_text = response_text.strip()

    # Parse JSON
    try:
        case_data = json.loads(response_text)
    except json.JSONDecodeError as e:
        print(f"  ERROR: Failed to parse JSON response: {e}")
        print(f"  Raw response (first 500 chars): {response_text[:500]}")
        # Save raw response for debugging
        debug_path = OUTPUT_DIR / f"{brief_path.stem}_raw.txt"
        debug_path.write_text(response_text, encoding="utf-8")
        print(f"  Raw response saved to: {debug_path}")
        return None

    print(f"  Generated case: {case_data.get('caseID', 'unknown')} — \"{case_data.get('title', 'untitled')}\"")
    print(f"  Suspects: {len(case_data.get('suspects', []))}")
    print(f"  Evidence: {len(case_data.get('evidences', []))}")
    print(f"  Token usage: {message.usage.input_tokens:,} in / {message.usage.output_tokens:,} out")

    return case_data


def save_case(case_data: dict, output_path: Path):
    """Save case JSON with pretty formatting."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(case_data, f, indent=2, ensure_ascii=False)
    print(f"  Saved to: {output_path}")


def run_validator(case_path: Path) -> bool:
    """Run the validator on a generated case."""
    validate_script = SCRIPT_DIR / "validate.py"
    if not validate_script.exists():
        print("  Validator not found, skipping validation.")
        return True

    import subprocess

    result = subprocess.run(
        [sys.executable, str(validate_script), str(case_path)],
        capture_output=True,
        text=True,
    )
    print(result.stdout)
    if result.returncode != 0:
        print(result.stderr)
        return False
    return True


def determine_output_path(brief_path: Path, explicit_output: str | None) -> Path:
    """Determine the output path for a brief file."""
    if explicit_output:
        return Path(explicit_output)

    # Derive from brief filename: core_01.md -> core_01_generated.json
    stem = brief_path.stem
    # Remove category prefix for secondary briefs (secondary_domestic_01 -> secondary_01)
    return OUTPUT_DIR / f"{stem}_generated.json"


def main():
    parser = argparse.ArgumentParser(
        description="Generate Marlo case JSON from case briefs using Claude API"
    )
    parser.add_argument(
        "briefs",
        nargs="+",
        help="Path(s) to case brief markdown files",
    )
    parser.add_argument(
        "--output", "-o",
        help="Output path (only valid with single brief)",
    )
    parser.add_argument(
        "--validate", "-v",
        action="store_true",
        help="Run validator on generated output",
    )
    parser.add_argument(
        "--model", "-m",
        default=MODEL,
        help=f"Claude model to use (default: {MODEL})",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be generated without calling the API",
    )

    args = parser.parse_args()

    if args.output and len(args.briefs) > 1:
        print("Error: --output can only be used with a single brief file.")
        sys.exit(1)

    # Check for API key
    api_key = os.environ.get("ANTHROPIC_API_KEY")
    if not api_key and not args.dry_run:
        print("Error: ANTHROPIC_API_KEY environment variable not set.")
        print("Set it with: export ANTHROPIC_API_KEY=your-key-here")
        sys.exit(1)

    # Override model if specified
    global MODEL
    MODEL = args.model

    # Create output directory
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    # Initialize client
    client = None if args.dry_run else anthropic.Anthropic(api_key=api_key)

    results = {"generated": 0, "failed": 0, "validated": 0, "invalid": 0}

    for brief_file in args.briefs:
        brief_path = Path(brief_file)
        if not brief_path.exists():
            print(f"Warning: Brief file not found: {brief_path}")
            results["failed"] += 1
            continue

        output_path = determine_output_path(brief_path, args.output)

        print(f"\n{'='*60}")
        print(f"Processing: {brief_path.name}")
        print(f"{'='*60}")

        if args.dry_run:
            print(f"  Would generate: {output_path}")
            print(f"  Brief: {brief_path}")
            print(f"  Model: {MODEL}")
            continue

        case_data = generate_case(brief_path, client)

        if case_data is None:
            results["failed"] += 1
            continue

        save_case(case_data, output_path)
        results["generated"] += 1

        if args.validate:
            if run_validator(output_path):
                results["validated"] += 1
            else:
                results["invalid"] += 1

    # Summary
    print(f"\n{'='*60}")
    print("Summary:")
    print(f"  Generated: {results['generated']}")
    print(f"  Failed: {results['failed']}")
    if args.validate:
        print(f"  Validated: {results['validated']}")
        print(f"  Invalid: {results['invalid']}")
    print(f"{'='*60}")


if __name__ == "__main__":
    main()
