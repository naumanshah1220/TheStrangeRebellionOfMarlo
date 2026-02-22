#!/usr/bin/env python3
"""
Case Validator for The Strange Rebellion of Marlo.

Validates case JSON files against:
1. JSON Schema compliance (structure, types, enums)
2. ID uniqueness (no duplicate clue/evidence/citizen IDs)
3. Cross-reference integrity (all IDs resolve correctly)
4. Reachability (can the player reach the correct verdict?)
5. Tool consistency (spectrograph/disc/fingerprint constraints)
6. Interrogation logic (clickable text exists in response sequences)

Usage:
    python validate.py path/to/case.json
    python validate.py path/to/cases/*.json           # Batch mode
    python validate.py --all                           # Validate all existing cases
    python validate.py path/to/case.json --verbose     # Detailed output
"""

import argparse
import json
import sys
from pathlib import Path
from dataclasses import dataclass, field

try:
    import jsonschema
except ImportError:
    print("Error: 'jsonschema' package not installed. Run: pip install jsonschema")
    sys.exit(1)


SCRIPT_DIR = Path(__file__).parent
SCHEMA_PATH = SCRIPT_DIR / "schema" / "case_schema.json"
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # Tools/CaseGenerator -> Tools -> project root
CASES_DIR = PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"


@dataclass
class ValidationResult:
    """Holds validation results for a single case file."""
    file_path: str
    case_id: str = ""
    title: str = ""
    errors: list = field(default_factory=list)
    warnings: list = field(default_factory=list)
    info: list = field(default_factory=list)

    @property
    def is_valid(self) -> bool:
        return len(self.errors) == 0

    def error(self, category: str, message: str):
        self.errors.append(f"[{category}] {message}")

    def warn(self, category: str, message: str):
        self.warnings.append(f"[{category}] {message}")

    def note(self, category: str, message: str):
        self.info.append(f"[{category}] {message}")


def load_schema() -> dict:
    """Load the JSON Schema for case validation."""
    if not SCHEMA_PATH.exists():
        print(f"Error: Schema file not found: {SCHEMA_PATH}")
        sys.exit(1)
    with open(SCHEMA_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def validate_schema(case: dict, schema: dict, result: ValidationResult):
    """Phase 1: JSON Schema compliance."""
    validator = jsonschema.Draft202012Validator(schema)
    errors = list(validator.iter_errors(case))

    for err in errors:
        path = " → ".join(str(p) for p in err.absolute_path) or "root"
        result.error("SCHEMA", f"{path}: {err.message}")

    if not errors:
        result.note("SCHEMA", "JSON Schema validation passed")


def collect_all_clue_ids(case: dict) -> set:
    """Collect every clue ID defined in the case (evidence hotspots + interrogation clickables)."""
    clue_ids = set()

    # From evidence hotspots
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        for hotspot in ev.get("hotspots", []):
            if hotspot.get("clueId"):
                clue_ids.add(hotspot["clueId"])

    # From interrogation clickable clues
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for resp_source in _all_responses(tag):
                for clue in resp_source.get("clickableClues", []):
                    if clue.get("clueId"):
                        clue_ids.add(clue["clueId"])

    return clue_ids


def collect_all_tag_ids(case: dict) -> set:
    """Collect every tagId defined in tag interactions."""
    tag_ids = set()
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            if tag.get("tagId"):
                tag_ids.add(tag["tagId"])
    return tag_ids


def _all_responses(tag: dict) -> list:
    """Get all response objects from a tag interaction (default, contradiction, variants, unlocked)."""
    responses = []
    for r in tag.get("responses", []):
        responses.append(r)
    if tag.get("contradictionResponse"):
        responses.append(tag["contradictionResponse"])
    if tag.get("unlockedInitialResponseIfPreviouslyDenied"):
        responses.append(tag["unlockedInitialResponseIfPreviouslyDenied"])
    if tag.get("unlockedInitialResponseIfNotDenied"):
        responses.append(tag["unlockedInitialResponseIfNotDenied"])
    for r in tag.get("unlockedFollowupResponses", []):
        responses.append(r)
    for variant in tag.get("responseVariants", []):
        for r in variant.get("responses", []):
            responses.append(r)
    return responses


def validate_id_uniqueness(case: dict, result: ValidationResult):
    """Phase 2: No duplicate IDs within the case."""
    # Evidence IDs
    ev_ids = []
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        ev_ids.append(ev.get("id", ""))
    duplicates = [x for x in set(ev_ids) if ev_ids.count(x) > 1]
    for dup in duplicates:
        result.error("UNIQUE", f"Duplicate evidence ID: {dup}")

    # Citizen IDs
    citizen_ids = [s.get("citizenID", "") for s in case.get("suspects", [])]
    duplicates = [x for x in set(citizen_ids) if citizen_ids.count(x) > 1]
    for dup in duplicates:
        result.error("UNIQUE", f"Duplicate citizen ID: {dup}")

    # Clue IDs (within evidence hotspots — same clue from two sources is OK)
    # Just note if found
    all_clues = collect_all_clue_ids(case)
    result.note("UNIQUE", f"Found {len(all_clues)} unique clue IDs")

    if not duplicates:
        result.note("UNIQUE", "ID uniqueness check passed")


def validate_cross_references(case: dict, result: ValidationResult):
    """Phase 3: All ID references resolve correctly."""
    all_clue_ids = collect_all_clue_ids(case)
    all_tag_ids = collect_all_tag_ids(case)
    citizen_ids = {s.get("citizenID") for s in case.get("suspects", [])}

    # culpritCitizenID must reference a suspect
    culprit = case.get("culpritCitizenID", "")
    if culprit and culprit not in citizen_ids:
        result.error("XREF", f"culpritCitizenID '{culprit}' not found in suspects")
    elif culprit:
        result.note("XREF", f"Culprit '{culprit}' exists in suspects")

    # Every contradictedByEvidenceTagIds must reference an existing clue
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for contra_id in tag.get("contradictedByEvidenceTagIds", []):
                if contra_id not in all_clue_ids and contra_id not in all_tag_ids:
                    result.error(
                        "XREF",
                        f"Suspect '{suspect.get('citizenID')}', tag '{tag.get('tagId')}': "
                        f"contradictedByEvidenceTagIds references '{contra_id}' "
                        f"which is not a known clue or tag"
                    )

    # Every unlocksTruthForTagIds must reference an existing tag
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for unlock_id in tag.get("unlocksTruthForTagIds", []):
                if unlock_id not in all_tag_ids:
                    result.error(
                        "XREF",
                        f"Suspect '{suspect.get('citizenID')}', tag '{tag.get('tagId')}': "
                        f"unlocksTruthForTagIds references '{unlock_id}' "
                        f"which is not a known tag"
                    )

    # Every clueVerdictMapping.clueId must reference a discoverable clue
    for mapping in case.get("clueVerdictMappings", []):
        clue_id = mapping.get("clueId", "")
        if clue_id not in all_clue_ids:
            result.error(
                "XREF",
                f"clueVerdictMapping references clue '{clue_id}' "
                f"which is not discoverable"
            )

    # Every solution slot answer must reference valid IDs
    for i, solution in enumerate(case.get("solutions", [])):
        for answer in solution.get("answers", []):
            slot_id = answer.get("slotId", "")
            # Check if slotId exists in verdictSchema
            schema_slots = {
                s.get("slotId") for s in
                case.get("verdictSchema", {}).get("slots", [])
            }
            if slot_id not in schema_slots:
                result.error(
                    "XREF",
                    f"Solution {i} references slot '{slot_id}' "
                    f"not defined in verdictSchema"
                )

    # Every evidence hotspot clueId should be used as a tagId by at least one suspect
    hotspot_clue_ids = set()
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        for hotspot in ev.get("hotspots", []):
            hotspot_clue_ids.add(hotspot.get("clueId", ""))

    for clue_id in hotspot_clue_ids:
        if clue_id not in all_tag_ids:
            result.warn(
                "XREF",
                f"Evidence hotspot clue '{clue_id}' is not used as a tagId "
                f"in any suspect's tagInteractions (may be unused)"
            )


def validate_clickable_text(case: dict, result: ValidationResult):
    """Phase 4: Every clickableText must be a substring of a responseSequence line."""
    for suspect in case.get("suspects", []):
        citizen_id = suspect.get("citizenID", "unknown")
        for tag in suspect.get("tagInteractions", []):
            tag_id = tag.get("tagId", "unknown")
            for resp in _all_responses(tag):
                sequence = resp.get("responseSequence", [])
                full_text = " ".join(sequence)
                for clue in resp.get("clickableClues", []):
                    clickable = clue.get("clickableText", "")
                    if clickable and clickable not in full_text:
                        # Check each line individually too
                        found_in_line = any(clickable in line for line in sequence)
                        if not found_in_line:
                            result.error(
                                "CLICK",
                                f"Suspect '{citizen_id}', tag '{tag_id}': "
                                f"clickableText \"{clickable}\" not found in "
                                f"responseSequence"
                            )


def validate_reachability(case: dict, result: ValidationResult):
    """Phase 5: Can the player reach the correct verdict?"""
    # Check that culprit is guilty
    culprit_id = case.get("culpritCitizenID", "")
    for suspect in case.get("suspects", []):
        if suspect.get("citizenID") == culprit_id:
            if not suspect.get("isGuilty", False):
                result.warn(
                    "REACH",
                    f"Culprit '{culprit_id}' is not marked as isGuilty=true"
                )
            break

    # Check that at least one solution exists
    solutions = case.get("solutions", [])
    if not solutions:
        result.error("REACH", "No solutions defined — verdict is unreachable")
        return

    # Check that enough clues exist to meet minDiscoveredCluesToAllowCommit
    all_clues = collect_all_clue_ids(case)
    min_clues = case.get("minDiscoveredCluesToAllowCommit", 3)
    if len(all_clues) < min_clues:
        result.error(
            "REACH",
            f"Only {len(all_clues)} discoverable clues but "
            f"minDiscoveredCluesToAllowCommit is {min_clues}"
        )

    # Check that verdict options can be populated
    for mapping in case.get("clueVerdictMappings", []):
        if mapping.get("clueId") in all_clues:
            result.note("REACH", f"Verdict option '{mapping.get('label')}' is reachable via clue '{mapping.get('clueId')}'")

    # Check that the suspect slot has at least one valid option
    suspect_slot_exists = False
    for slot in case.get("verdictSchema", {}).get("slots", []):
        if slot.get("type") == "Suspect":
            suspect_slot_exists = True
            if slot.get("optionSource") == "CaseOnly":
                result.note("REACH", "Suspect slot uses CaseOnly — suspects are always available")
            break

    if not suspect_slot_exists:
        result.warn("REACH", "No Suspect-type slot in verdict schema")


def validate_tool_consistency(case: dict, result: ValidationResult):
    """Phase 6: Tool-specific evidence constraints."""
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        ev_id = ev.get("id", "unknown")
        ev_type = ev.get("type", "Document")
        substance = ev.get("foreignSubstance", "None")

        # Spectrograph: foreignSubstance != None should have a related clue
        if substance != "None":
            has_substance_clue = False
            for hotspot in ev.get("hotspots", []):
                # Look for clues that seem related to substance analysis
                note = hotspot.get("noteText", "").lower()
                clue_id = hotspot.get("clueId", "").lower()
                if any(kw in note or kw in clue_id for kw in
                       ["substance", "chemical", "ink", "residue", "analysis",
                        "spectrograph", "compound", "paint", "blood", "soil",
                        "gunpowder", "pharmaceutical", "cosmetic", "industrial"]):
                    has_substance_clue = True
                    break
            if not has_substance_clue:
                result.warn(
                    "TOOL",
                    f"Evidence '{ev_id}' has foreignSubstance='{substance}' "
                    f"but no hotspot seems related to substance analysis"
                )

        # Disc evidence should have associatedAppId
        if ev_type == "Disc" and not ev.get("associatedAppId"):
            result.error(
                "TOOL",
                f"Evidence '{ev_id}' is type Disc but has no associatedAppId"
            )

        # Item/Document can use fingerprint — just note it
        if ev_type == "Item":
            result.note("TOOL", f"Evidence '{ev_id}' is type Item — fingerprint analysis possible")


def validate_stress_feasibility(case: dict, result: ValidationResult):
    """Phase 7: Stress levels are within workable ranges."""
    for suspect in case.get("suspects", []):
        citizen_id = suspect.get("citizenID", "unknown")
        initial_stress = suspect.get("initialStress", -1)
        nervousness = suspect.get("nervousnessLevel", 0.3)

        if initial_stress > 0.8:
            result.warn(
                "STRESS",
                f"Suspect '{citizen_id}' starts at stress {initial_stress} — "
                f"very close to breakdown zone"
            )

        if nervousness < 0.05:
            result.warn(
                "STRESS",
                f"Suspect '{citizen_id}' nervousness {nervousness} is very low — "
                f"may be unresponsive to pressure"
            )

        # Check that stress variants have reasonable thresholds
        for tag in suspect.get("tagInteractions", []):
            for variant in tag.get("responseVariants", []):
                for cond in variant.get("conditions", []):
                    if cond.get("type") == "StressAbove":
                        threshold = cond.get("threshold", 0)
                        if threshold <= initial_stress and initial_stress >= 0:
                            result.warn(
                                "STRESS",
                                f"Suspect '{citizen_id}', tag '{tag.get('tagId')}': "
                                f"stress variant triggers at {threshold} but "
                                f"initial stress is {initial_stress} — "
                                f"variant may trigger immediately"
                            )


def validate_case(case_path: Path, schema: dict, verbose: bool = False) -> ValidationResult:
    """Run all validation phases on a case file."""
    result = ValidationResult(file_path=str(case_path))

    # Load the case
    try:
        with open(case_path, "r", encoding="utf-8") as f:
            case = json.load(f)
    except json.JSONDecodeError as e:
        result.error("PARSE", f"Invalid JSON: {e}")
        return result

    result.case_id = case.get("caseID", "unknown")
    result.title = case.get("title", "untitled")

    # Run all phases
    validate_schema(case, schema, result)
    validate_id_uniqueness(case, result)
    validate_cross_references(case, result)
    validate_clickable_text(case, result)
    validate_reachability(case, result)
    validate_tool_consistency(case, result)
    validate_stress_feasibility(case, result)

    return result


def print_result(result: ValidationResult, verbose: bool = False):
    """Print validation results."""
    status = "PASS" if result.is_valid else "FAIL"
    icon = "+" if result.is_valid else "X"

    print(f"\n[{icon}] {result.case_id} — \"{result.title}\" ({status})")
    print(f"    File: {result.file_path}")

    if result.errors:
        print(f"    Errors ({len(result.errors)}):")
        for err in result.errors:
            print(f"      - {err}")

    if result.warnings:
        print(f"    Warnings ({len(result.warnings)}):")
        for warn in result.warnings:
            print(f"      ! {warn}")

    if verbose and result.info:
        print(f"    Info ({len(result.info)}):")
        for info in result.info:
            print(f"      . {info}")


def main():
    parser = argparse.ArgumentParser(
        description="Validate Marlo case JSON files"
    )
    parser.add_argument(
        "files",
        nargs="*",
        help="Case JSON file(s) to validate",
    )
    parser.add_argument(
        "--all", "-a",
        action="store_true",
        help="Validate all cases in StreamingAssets/content/cases/",
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        help="Show detailed info messages",
    )
    parser.add_argument(
        "--json-output",
        action="store_true",
        help="Output results as JSON",
    )

    args = parser.parse_args()

    if not args.files and not args.all:
        parser.print_help()
        sys.exit(1)

    schema = load_schema()

    # Collect files to validate
    files = []
    if args.all:
        if CASES_DIR.exists():
            files = sorted(CASES_DIR.glob("*.json"))
        else:
            print(f"Error: Cases directory not found: {CASES_DIR}")
            sys.exit(1)
    else:
        files = [Path(f) for f in args.files]

    # Also check output directory
    output_dir = SCRIPT_DIR / "output"
    if args.all and output_dir.exists():
        files.extend(sorted(output_dir.glob("*.json")))

    results = []
    for file_path in files:
        if not file_path.exists():
            print(f"Warning: File not found: {file_path}")
            continue
        result = validate_case(file_path, schema, args.verbose)
        results.append(result)

    if args.json_output:
        output = []
        for r in results:
            output.append({
                "file": r.file_path,
                "caseID": r.case_id,
                "title": r.title,
                "valid": r.is_valid,
                "errors": r.errors,
                "warnings": r.warnings,
            })
        print(json.dumps(output, indent=2))
    else:
        print(f"\nValidating {len(results)} case file(s)...")
        for result in results:
            print_result(result, args.verbose)

        # Summary
        passed = sum(1 for r in results if r.is_valid)
        failed = sum(1 for r in results if not r.is_valid)
        total_errors = sum(len(r.errors) for r in results)
        total_warnings = sum(len(r.warnings) for r in results)

        print(f"\n{'='*60}")
        print(f"Results: {passed} passed, {failed} failed")
        print(f"Total: {total_errors} errors, {total_warnings} warnings")
        print(f"{'='*60}")

    sys.exit(0 if all(r.is_valid for r in results) else 1)


if __name__ == "__main__":
    main()
