#!/usr/bin/env python3
"""
Case Browser & Validator for The Strange Rebellion of Marlo.

Generates a single self-contained HTML file that lets you browse all case
JSON files, inspect evidence/suspects/dialogue trees, see investigation
flow diagrams, and view validation results — all in one place.

Usage:
    python case_browser.py                  # Generate output/case_browser.html
    python case_browser.py --open           # Generate and open in browser
    python case_browser.py -o my_output.html  # Custom output path
"""

import argparse
import json
import sys
import webbrowser
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
CASES_DIR = PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"
OUTPUT_DIR = SCRIPT_DIR / "output"
SCHEMA_PATH = SCRIPT_DIR / "schema" / "case_schema.json"

# ---------------------------------------------------------------------------
# Validation (inline, mirrors validate.py phases)
# ---------------------------------------------------------------------------

try:
    import jsonschema
    HAS_JSONSCHEMA = True
except ImportError:
    HAS_JSONSCHEMA = False


def _all_responses(tag: dict) -> list:
    """Get all response objects from a tag interaction."""
    responses = list(tag.get("responses", []))
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


def collect_all_clue_ids(case: dict) -> set:
    clue_ids = set()
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        for hs in ev.get("hotspots", []):
            if hs.get("clueId"):
                clue_ids.add(hs["clueId"])
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for resp in _all_responses(tag):
                for clue in resp.get("clickableClues", []):
                    if clue.get("clueId"):
                        clue_ids.add(clue["clueId"])
    return clue_ids


def collect_all_tag_ids(case: dict) -> set:
    tag_ids = set()
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            if tag.get("tagId"):
                tag_ids.add(tag["tagId"])
    return tag_ids


def validate_case_inline(case: dict, schema: dict | None) -> dict:
    """Run all 7 validation phases. Returns {errors, warnings, info} by phase."""
    phases = {}

    # Phase 1: SCHEMA
    phase_errors, phase_warnings, phase_info = [], [], []
    if schema and HAS_JSONSCHEMA:
        validator = jsonschema.Draft202012Validator(schema)
        for err in validator.iter_errors(case):
            path = " -> ".join(str(p) for p in err.absolute_path) or "root"
            phase_errors.append(f"{path}: {err.message}")
        if not phase_errors:
            phase_info.append("JSON Schema validation passed")
    elif not HAS_JSONSCHEMA:
        phase_info.append("jsonschema not installed — schema check skipped")
    phases["SCHEMA"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 2: UNIQUE
    phase_errors, phase_warnings, phase_info = [], [], []
    ev_ids = [ev.get("id", "") for ev in case.get("evidences", []) + case.get("extraEvidences", [])]
    for eid in set(ev_ids):
        if ev_ids.count(eid) > 1:
            phase_errors.append(f"Duplicate evidence ID: {eid}")
    citizen_ids = [s.get("citizenID", "") for s in case.get("suspects", [])]
    for cid in set(citizen_ids):
        if citizen_ids.count(cid) > 1:
            phase_errors.append(f"Duplicate citizen ID: {cid}")
    all_clues = collect_all_clue_ids(case)
    phase_info.append(f"Found {len(all_clues)} unique clue IDs")
    if not phase_errors:
        phase_info.append("ID uniqueness check passed")
    phases["UNIQUE"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 3: XREF
    phase_errors, phase_warnings, phase_info = [], [], []
    all_tag_ids = collect_all_tag_ids(case)
    citizen_id_set = {s.get("citizenID") for s in case.get("suspects", [])}

    culprit = case.get("culpritCitizenID", "")
    if culprit and culprit not in citizen_id_set:
        phase_errors.append(f"culpritCitizenID '{culprit}' not found in suspects")
    elif culprit:
        phase_info.append(f"Culprit '{culprit}' exists in suspects")

    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for contra_id in tag.get("contradictedByEvidenceTagIds", []):
                if contra_id not in all_clues and contra_id not in all_tag_ids:
                    phase_errors.append(
                        f"Suspect '{suspect.get('citizenID')}', tag '{tag.get('tagId')}': "
                        f"contradictedByEvidenceTagIds references '{contra_id}' — not a known clue or tag"
                    )
            for unlock_id in tag.get("unlocksTruthForTagIds", []):
                if unlock_id not in all_tag_ids:
                    phase_errors.append(
                        f"Suspect '{suspect.get('citizenID')}', tag '{tag.get('tagId')}': "
                        f"unlocksTruthForTagIds references '{unlock_id}' — not a known tag"
                    )

    for mapping in case.get("clueVerdictMappings", []):
        if mapping.get("clueId", "") not in all_clues:
            phase_errors.append(f"clueVerdictMapping references clue '{mapping.get('clueId')}' — not discoverable")

    for i, solution in enumerate(case.get("solutions", [])):
        schema_slots = {s.get("slotId") for s in case.get("verdictSchema", {}).get("slots", [])}
        for answer in solution.get("answers", []):
            if answer.get("slotId", "") not in schema_slots:
                phase_errors.append(f"Solution {i} references slot '{answer.get('slotId')}' not in verdictSchema")

    hotspot_clue_ids = set()
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        for hs in ev.get("hotspots", []):
            hotspot_clue_ids.add(hs.get("clueId", ""))
    for cid in hotspot_clue_ids:
        if cid not in all_tag_ids:
            phase_warnings.append(f"Evidence hotspot clue '{cid}' not used as a tagId in any suspect's tagInteractions")

    phases["XREF"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 4: CLICK
    phase_errors, phase_warnings, phase_info = [], [], []
    for suspect in case.get("suspects", []):
        cid = suspect.get("citizenID", "unknown")
        for tag in suspect.get("tagInteractions", []):
            tid = tag.get("tagId", "unknown")
            for resp in _all_responses(tag):
                seq = resp.get("responseSequence", [])
                full_text = " ".join(seq)
                for clue in resp.get("clickableClues", []):
                    ct = clue.get("clickableText", "")
                    if ct and ct not in full_text:
                        if not any(ct in line for line in seq):
                            phase_errors.append(
                                f"Suspect '{cid}', tag '{tid}': clickableText \"{ct}\" "
                                f"not found in responseSequence"
                            )
    if not phase_errors:
        phase_info.append("All clickable text matches response sequences")
    phases["CLICK"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 5: REACH
    phase_errors, phase_warnings, phase_info = [], [], []
    culprit_id = case.get("culpritCitizenID", "")
    for suspect in case.get("suspects", []):
        if suspect.get("citizenID") == culprit_id:
            if not suspect.get("isGuilty", False):
                phase_warnings.append(f"Culprit '{culprit_id}' is not marked isGuilty=true")
            break

    solutions = case.get("solutions", [])
    if not solutions:
        phase_errors.append("No solutions defined — verdict is unreachable")
    else:
        min_clues = case.get("minDiscoveredCluesToAllowCommit", 3)
        if len(all_clues) < min_clues:
            phase_errors.append(
                f"Only {len(all_clues)} discoverable clues but minDiscoveredCluesToAllowCommit is {min_clues}"
            )
        for mapping in case.get("clueVerdictMappings", []):
            if mapping.get("clueId") in all_clues:
                phase_info.append(f"Verdict option '{mapping.get('label')}' reachable via clue '{mapping.get('clueId')}'")

        suspect_slot = False
        for slot in case.get("verdictSchema", {}).get("slots", []):
            if slot.get("type") == "Suspect":
                suspect_slot = True
                break
        if not suspect_slot:
            phase_warnings.append("No Suspect-type slot in verdict schema")

    phases["REACH"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 6: TOOL
    phase_errors, phase_warnings, phase_info = [], [], []
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        ev_id = ev.get("id", "unknown")
        ev_type = ev.get("type", "Document")
        substance = ev.get("foreignSubstance", "None")
        if substance != "None":
            has_substance_clue = False
            for hs in ev.get("hotspots", []):
                note = hs.get("noteText", "").lower()
                cid = hs.get("clueId", "").lower()
                keywords = ["substance", "chemical", "ink", "residue", "analysis",
                            "spectrograph", "compound", "paint", "blood", "soil",
                            "gunpowder", "pharmaceutical", "cosmetic", "industrial"]
                if any(kw in note or kw in cid for kw in keywords):
                    has_substance_clue = True
                    break
            if not has_substance_clue:
                phase_warnings.append(
                    f"Evidence '{ev_id}' has foreignSubstance='{substance}' "
                    f"but no hotspot relates to substance analysis"
                )
        if ev_type == "Disc" and not ev.get("associatedAppId"):
            phase_errors.append(f"Evidence '{ev_id}' is type Disc but has no associatedAppId")
        if ev_type == "Item":
            phase_info.append(f"Evidence '{ev_id}' is type Item — fingerprint analysis possible")
    phases["TOOL"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    # Phase 7: STRESS
    phase_errors, phase_warnings, phase_info = [], [], []
    for suspect in case.get("suspects", []):
        cid = suspect.get("citizenID", "unknown")
        initial_stress = suspect.get("initialStress", -1)
        nervousness = suspect.get("nervousnessLevel", 0.3)
        if initial_stress > 0.8:
            phase_warnings.append(f"Suspect '{cid}' starts at stress {initial_stress} — close to breakdown")
        if nervousness < 0.05:
            phase_warnings.append(f"Suspect '{cid}' nervousness {nervousness} is very low")
        for tag in suspect.get("tagInteractions", []):
            for variant in tag.get("responseVariants", []):
                for cond in variant.get("conditions", []):
                    if cond.get("type") == "StressAbove":
                        threshold = cond.get("threshold", 0)
                        if threshold <= initial_stress and initial_stress >= 0:
                            phase_warnings.append(
                                f"Suspect '{cid}', tag '{tag.get('tagId')}': "
                                f"stress variant triggers at {threshold} but initial stress is {initial_stress}"
                            )
    phases["STRESS"] = {"errors": phase_errors, "warnings": phase_warnings, "info": phase_info}

    return phases


def compute_overall_status(phases: dict) -> str:
    """Return PASS / WARN / FAIL based on validation phases."""
    total_errors = sum(len(p["errors"]) for p in phases.values())
    total_warnings = sum(len(p["warnings"]) for p in phases.values())
    if total_errors > 0:
        return "FAIL"
    if total_warnings > 0:
        return "WARN"
    return "PASS"


# ---------------------------------------------------------------------------
# Mermaid diagram builder (adapted from visualize.py)
# ---------------------------------------------------------------------------

def sanitize_mermaid(text: str) -> str:
    return (
        text.replace('"', "'")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace("\n", " ")
        .replace("(", "[")
        .replace(")", "]")
        [:60]
    )


def build_mermaid_diagram(case: dict) -> str:
    lines = ["graph TD"]
    for ev in case.get("evidences", []):
        ev_id = ev["id"].replace("-", "_")
        label = sanitize_mermaid(ev["title"])
        lines.append(f'    {ev_id}["{label}"]')
        lines.append(f'    style {ev_id} fill:#0f3460,stroke:#53d8fb')
        for hotspot in ev.get("hotspots", []):
            clue_id = hotspot["clueId"].replace("-", "_")
            clue_label = sanitize_mermaid(hotspot["clueId"])
            lines.append(f'    {clue_id}("{clue_label}")')
            lines.append(f'    {ev_id} -->|hotspot| {clue_id}')
            lines.append(f'    style {clue_id} fill:#1a3a5c,stroke:#e9a645')

    for suspect in case.get("suspects", []):
        s_id = suspect["citizenID"].replace("-", "_")
        s_label = f"{suspect['firstName']} {suspect['lastName']}"
        guilty_style = "#5c1e1e" if suspect.get("isGuilty") else "#1e5c3d"
        lines.append(f'    {s_id}{{{{{s_label}}}}}')
        lines.append(f'    style {s_id} fill:{guilty_style},stroke:#e94560')

        for tag in suspect.get("tagInteractions", []):
            tag_id = tag["tagId"].replace("-", "_")
            lines.append(f'    {tag_id} -->|interrogate| {s_id}')

            if tag.get("contradictionResponse"):
                contra_node = f'{s_id}_{tag_id}_contra'
                lines.append(f'    {contra_node}["Contradiction"]')
                lines.append(f'    {s_id} -.->|contradicted| {contra_node}')
                lines.append(f'    style {contra_node} fill:#5c3d1e,stroke:#e9a645')
                for clue in tag["contradictionResponse"].get("clickableClues", []):
                    nc = clue["clueId"].replace("-", "_")
                    nl = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {nc}("{nl}")')
                    lines.append(f'    {contra_node} -->|reveals| {nc}')
                    lines.append(f'    style {nc} fill:#1a3a5c,stroke:#4ecca3')

            if tag.get("unlockedInitialResponseIfPreviouslyDenied"):
                unlock = tag["unlockedInitialResponseIfPreviouslyDenied"]
                for clue in unlock.get("clickableClues", []):
                    nc = clue["clueId"].replace("-", "_")
                    nl = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {nc}("{nl}")')
                    lines.append(f'    {s_id} -.->|truth unlocked| {nc}')
                    lines.append(f'    style {nc} fill:#1a3a5c,stroke:#4ecca3')

            for resp in tag.get("responses", []):
                for clue in resp.get("clickableClues", []):
                    nc = clue["clueId"].replace("-", "_")
                    nl = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {nc}("{nl}")')
                    is_lie = "lie" if resp.get("isLie") else "truth"
                    lines.append(f'    {s_id} -->|{is_lie}| {nc}')
                    color = "#4ecca3" if not resp.get("isLie") else "#e94560"
                    lines.append(f'    style {nc} fill:#1a3a5c,stroke:{color}')

    lines.append('    VERDICT["VERDICT FORM"]')
    lines.append('    style VERDICT fill:#1e5c3d,stroke:#4ecca3,stroke-width:3px')

    for mapping in case.get("clueVerdictMappings", []):
        clue_id = mapping["clueId"].replace("-", "_")
        label = sanitize_mermaid(mapping.get("label", mapping["optionId"]))
        lines.append(f'    {clue_id} -->|unlocks: {label}| VERDICT')

    return "\n".join(lines)


# ---------------------------------------------------------------------------
# Art assets manifest builder
# ---------------------------------------------------------------------------

def compute_art_assets(case: dict) -> list:
    """Return list of {asset, type, notes} for required art."""
    assets = []

    # Case card
    assets.append({
        "asset": case.get("cardImagePath", f"Cases/{case.get('caseID', '?')}/card"),
        "type": "Case Card",
        "notes": "Small card sprite for case hand"
    })

    # Suspect portraits
    for s in case.get("suspects", []):
        assets.append({
            "asset": s.get("picturePath", f"Portraits/{s.get('citizenID', '?')}"),
            "type": "Portrait",
            "notes": f"{s.get('firstName', '')} {s.get('lastName', '')} — ID card photo"
        })

    # Evidence cards + full pages
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        assets.append({
            "asset": ev.get("cardImagePath", f"Evidence/{case.get('caseID','?')}/{ev.get('id','?')}_card"),
            "type": f"Evidence Card ({ev.get('type', 'Document')})",
            "notes": ev.get("title", "")
        })
        # Full-page evidence view (implied)
        pages = set(hs.get("pageIndex", 0) for hs in ev.get("hotspots", []))
        for p in sorted(pages):
            assets.append({
                "asset": f"Evidence/{case.get('caseID','?')}/{ev.get('id','?')}_page{p}",
                "type": "Evidence Full Page",
                "notes": f"{ev.get('title', '')} — page {p}"
            })

        # Spectrograph readout
        substance = ev.get("foreignSubstance", "None")
        if substance != "None":
            assets.append({
                "asset": f"Spectrograph/{case.get('caseID','?')}/{ev.get('id','?')}_readout",
                "type": "Spectrograph Readout",
                "notes": f"Substance: {substance}"
            })

        # Computer app screens
        if ev.get("associatedAppId"):
            assets.append({
                "asset": f"ComputerApps/{ev.get('associatedAppId', '?')}",
                "type": "Computer App Screen",
                "notes": f"Disc evidence app for {ev.get('title', '')}"
            })

    return assets


# ---------------------------------------------------------------------------
# Detect whether a case is a placeholder
# ---------------------------------------------------------------------------

def is_placeholder(case: dict) -> bool:
    return (
        not case.get("suspects")
        or "placeholder" in case.get("title", "").lower()
        or not case.get("evidences")
    )


# ---------------------------------------------------------------------------
# HTML generation
# ---------------------------------------------------------------------------

def generate_browser_html(all_cases: list, all_validation: dict) -> str:
    """Generate the complete single-page HTML browser."""

    # Build per-case data for embedding
    cases_data = []
    for entry in all_cases:
        case = entry["data"]
        case_id = case.get("caseID", entry["filename"])
        v = all_validation.get(case_id, {})
        status = "EMPTY" if is_placeholder(case) else compute_overall_status(v) if v else "PASS"

        mermaid = build_mermaid_diagram(case) if not is_placeholder(case) else ""
        art = compute_art_assets(case) if not is_placeholder(case) else []

        cases_data.append({
            "caseID": case_id,
            "title": case.get("title", "Untitled"),
            "caseType": case.get("caseType", "Unknown"),
            "source": entry["source"],
            "status": status,
            "placeholder": is_placeholder(case),
            "data": case,
            "validation": v,
            "mermaid": mermaid,
            "art": art,
        })

    cases_json = json.dumps(cases_data, ensure_ascii=False, indent=None)

    # Load evidence reference images if available
    refs_path = SCRIPT_DIR / "evidence_references.json"
    refs_data = {}
    if refs_path.exists():
        try:
            refs_data = json.loads(refs_path.read_text(encoding="utf-8")).get("references", {})
        except Exception:
            pass
    refs_json = json.dumps(refs_data, ensure_ascii=False, indent=None)

    return (_HTML_TEMPLATE
            .replace("__CASES_DATA__", cases_json)
            .replace("__EVIDENCE_REFS__", refs_json))


_HTML_TEMPLATE = r"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>Marlo Case Browser</title>
<script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
<style>
* { margin: 0; padding: 0; box-sizing: border-box; }
:root {
    --bg: #1a1a2e;
    --panel: #16213e;
    --deep: #0f3460;
    --accent: #e94560;
    --cyan: #53d8fb;
    --green: #4ecca3;
    --gold: #e9a645;
    --text: #e0e0e0;
    --muted: #8888a0;
    --sidebar-w: 260px;
}
body {
    font-family: 'Courier New', monospace;
    background: var(--bg);
    color: var(--text);
    display: flex;
    height: 100vh;
    overflow: hidden;
}

/* ── Sidebar ───────────────────────────────── */
#sidebar {
    width: var(--sidebar-w);
    min-width: var(--sidebar-w);
    background: var(--panel);
    border-right: 1px solid var(--deep);
    display: flex;
    flex-direction: column;
    overflow: hidden;
}
#sidebar-header {
    padding: 16px;
    border-bottom: 1px solid var(--deep);
}
#sidebar-header h2 {
    color: var(--accent);
    font-size: 14px;
    margin-bottom: 10px;
}
#search-box {
    width: 100%;
    padding: 6px 10px;
    background: var(--deep);
    border: 1px solid var(--muted);
    border-radius: 4px;
    color: var(--text);
    font-family: inherit;
    font-size: 12px;
    outline: none;
}
#search-box:focus { border-color: var(--cyan); }
#case-list {
    flex: 1;
    overflow-y: auto;
    padding: 8px 0;
}
.case-group-label {
    font-size: 10px;
    color: var(--muted);
    text-transform: uppercase;
    letter-spacing: 2px;
    padding: 12px 16px 4px;
}
.case-item {
    padding: 8px 16px;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: space-between;
    font-size: 12px;
    border-left: 3px solid transparent;
    transition: all 0.15s;
}
.case-item:hover { background: var(--deep); }
.case-item.active {
    background: var(--deep);
    border-left-color: var(--accent);
}
.case-item .name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    margin-right: 8px;
}
.badge {
    font-size: 9px;
    font-weight: bold;
    padding: 2px 6px;
    border-radius: 3px;
    flex-shrink: 0;
}
.badge.PASS { background: #1e5c3d; color: var(--green); }
.badge.WARN { background: #5c4b1e; color: var(--gold); }
.badge.FAIL { background: #5c1e1e; color: var(--accent); }
.badge.EMPTY { background: #333; color: var(--muted); }

/* ── Main ──────────────────────────────────── */
#main {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}
#case-header {
    padding: 16px 24px;
    background: var(--panel);
    border-bottom: 1px solid var(--deep);
    display: flex;
    align-items: center;
    gap: 12px;
    flex-wrap: wrap;
}
#case-header h1 {
    color: var(--accent);
    font-size: 18px;
}
#case-header .type-badge {
    font-size: 11px;
    padding: 2px 8px;
    border-radius: 3px;
    background: var(--deep);
    color: var(--cyan);
}

/* ── Tabs ──────────────────────────────────── */
#tabs {
    display: flex;
    background: var(--panel);
    border-bottom: 1px solid var(--deep);
    padding: 0 24px;
    gap: 0;
    overflow-x: auto;
}
.tab {
    padding: 10px 16px;
    font-size: 12px;
    cursor: pointer;
    border-bottom: 2px solid transparent;
    color: var(--muted);
    white-space: nowrap;
    transition: all 0.15s;
    font-family: inherit;
    background: none;
    border-top: none;
    border-left: none;
    border-right: none;
}
.tab:hover { color: var(--text); }
.tab.active {
    color: var(--cyan);
    border-bottom-color: var(--cyan);
}

/* ── Tab Content ───────────────────────────── */
#tab-content {
    flex: 1;
    overflow-y: auto;
    padding: 24px;
}
.tab-pane { display: none; }
.tab-pane.active { display: block; }

.section-title {
    color: var(--cyan);
    font-size: 14px;
    margin: 20px 0 8px;
    padding-bottom: 4px;
    border-bottom: 1px solid var(--deep);
}
.section-title:first-child { margin-top: 0; }

.meta-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
    gap: 8px;
    margin: 10px 0;
}
.meta-chip {
    background: var(--deep);
    padding: 6px 12px;
    border-radius: 4px;
    font-size: 12px;
}
.meta-chip .label { color: var(--muted); margin-right: 4px; }

.card-panel {
    background: var(--deep);
    border-radius: 6px;
    padding: 14px;
    margin: 8px 0;
}
.card-panel.evidence { border-left: 3px solid var(--cyan); }
.card-panel.suspect-guilty { border-left: 3px solid var(--accent); }
.card-panel.suspect-innocent { border-left: 3px solid var(--green); }
.card-panel h3 {
    font-size: 13px;
    margin-bottom: 6px;
}

.tag-badge {
    display: inline-block;
    background: var(--bg);
    border: 1px solid var(--cyan);
    padding: 2px 8px;
    border-radius: 10px;
    font-size: 11px;
    margin: 2px;
}
.tag-badge.lie { border-color: var(--accent); color: var(--accent); }
.tag-badge.truth { border-color: var(--green); color: var(--green); }
.tag-badge.gold { border-color: var(--gold); color: var(--gold); }

/* ── Dialogue tree ─────────────────────────── */
.dialogue-tag {
    background: var(--panel);
    border-radius: 6px;
    margin: 8px 0;
    overflow: hidden;
}
.dialogue-tag-header {
    padding: 10px 14px;
    cursor: pointer;
    display: flex;
    align-items: center;
    gap: 8px;
    font-size: 12px;
    background: var(--deep);
    user-select: none;
}
.dialogue-tag-header:hover { background: #12305a; }
.dialogue-tag-header .arrow {
    transition: transform 0.2s;
    font-size: 10px;
}
.dialogue-tag-header .arrow.open { transform: rotate(90deg); }
.dialogue-tag-body {
    display: none;
    padding: 12px 14px;
}
.dialogue-tag-body.open { display: block; }
.response-block {
    background: var(--bg);
    border-radius: 4px;
    padding: 10px;
    margin: 6px 0;
    font-size: 12px;
}
.response-block .resp-label {
    font-size: 10px;
    text-transform: uppercase;
    letter-spacing: 1px;
    margin-bottom: 4px;
}
.response-block .resp-label.lie { color: var(--accent); }
.response-block .resp-label.truth { color: var(--green); }
.response-line {
    padding: 2px 0;
    line-height: 1.5;
}
.clickable-highlight {
    background: rgba(233, 166, 69, 0.25);
    border-bottom: 1px dashed var(--gold);
    padding: 0 2px;
}
.rich-text person { color: var(--accent); font-weight: bold; }
.rich-text location { color: var(--cyan); }
.rich-text item { color: var(--gold); }
.condition-chip {
    display: inline-block;
    font-size: 10px;
    padding: 1px 6px;
    border-radius: 3px;
    background: var(--panel);
    border: 1px solid var(--muted);
    margin: 1px;
}

/* ── Tables ────────────────────────────────── */
table {
    width: 100%;
    border-collapse: collapse;
    margin: 8px 0;
    font-size: 12px;
}
th, td {
    padding: 6px 10px;
    text-align: left;
    border-bottom: 1px solid var(--deep);
}
th { background: var(--deep); color: var(--cyan); font-size: 11px; }

/* ── Validation ────────────────────────────── */
.validation-phase {
    margin: 8px 0;
    border-radius: 6px;
    overflow: hidden;
}
.validation-phase-header {
    padding: 8px 14px;
    display: flex;
    align-items: center;
    gap: 10px;
    cursor: pointer;
    background: var(--deep);
    font-size: 12px;
    user-select: none;
}
.validation-phase-header:hover { background: #12305a; }
.validation-phase-body {
    display: none;
    padding: 10px 14px;
    background: var(--panel);
}
.validation-phase-body.open { display: block; }
.val-item {
    padding: 3px 0;
    font-size: 11px;
}
.val-item.error { color: var(--accent); }
.val-item.error::before { content: "ERR "; font-weight: bold; }
.val-item.warning { color: var(--gold); }
.val-item.warning::before { content: "WARN "; font-weight: bold; }
.val-item.info { color: var(--muted); }
.val-item.info::before { content: "INFO "; }

/* ── Flow diagram ──────────────────────────── */
.flow-container {
    background: var(--panel);
    padding: 20px;
    border-radius: 8px;
    overflow-x: auto;
}
.mermaid { display: flex; justify-content: center; }

/* ── Empty state ───────────────────────────── */
.empty-state {
    text-align: center;
    color: var(--muted);
    padding: 60px 20px;
    font-size: 14px;
}
.empty-state .icon { font-size: 40px; margin-bottom: 12px; }

/* ── Scrollbar ─────────────────────────────── */
::-webkit-scrollbar { width: 6px; height: 6px; }
::-webkit-scrollbar-track { background: var(--bg); }
::-webkit-scrollbar-thumb { background: var(--deep); border-radius: 3px; }
::-webkit-scrollbar-thumb:hover { background: var(--muted); }
</style>
</head>
<body>

<div id="sidebar">
    <div id="sidebar-header">
        <h2>MARLO CASE BROWSER</h2>
        <input type="text" id="search-box" placeholder="Search cases...">
    </div>
    <div id="case-list"></div>
</div>

<div id="main">
    <div id="case-header">
        <h1 id="header-title">Select a case</h1>
        <span class="type-badge" id="header-type" style="display:none"></span>
        <span class="badge" id="header-status" style="display:none"></span>
    </div>
    <div id="tabs">
        <button class="tab active" data-tab="overview">Overview</button>
        <button class="tab" data-tab="evidence">Evidence</button>
        <button class="tab" data-tab="flow">Flow</button>
        <button class="tab" data-tab="suspects">Suspects</button>
        <button class="tab" data-tab="verdict">Verdict</button>
        <button class="tab" data-tab="validation">Validation</button>
        <button class="tab" data-tab="assets">Assets</button>
    </div>
    <div id="tab-content">
        <div id="pane-overview" class="tab-pane active">
            <div class="empty-state"><div class="icon">&#128270;</div>Select a case from the sidebar to begin.</div>
        </div>
        <div id="pane-evidence" class="tab-pane"></div>
        <div id="pane-flow" class="tab-pane"></div>
        <div id="pane-suspects" class="tab-pane"></div>
        <div id="pane-verdict" class="tab-pane"></div>
        <div id="pane-validation" class="tab-pane"></div>
        <div id="pane-assets" class="tab-pane"></div>
    </div>
</div>

<script>
const ALL_CASES = __CASES_DATA__;
const EVIDENCE_REFS = __EVIDENCE_REFS__;

// ── State ────────────────────────────────────
let currentCaseIdx = -1;

// ── Init ─────────────────────────────────────
mermaid.initialize({
    startOnLoad: false,
    theme: 'dark',
    themeVariables: {
        primaryColor: '#16213e',
        primaryBorderColor: '#53d8fb',
        primaryTextColor: '#e0e0e0',
        lineColor: '#53d8fb',
        secondaryColor: '#0f3460',
        tertiaryColor: '#1a1a2e'
    }
});

buildSidebar();
setupTabs();
setupSearch();

// Select first non-placeholder case (or first case)
const firstReal = ALL_CASES.findIndex(c => !c.placeholder);
if (firstReal >= 0) selectCase(firstReal);
else if (ALL_CASES.length > 0) selectCase(0);

// ── Sidebar ──────────────────────────────────
function buildSidebar() {
    const list = document.getElementById('case-list');
    list.innerHTML = '';

    const groups = { Core: [], Secondary: [], Generated: [] };
    ALL_CASES.forEach((c, i) => {
        const g = c.source === 'generated' ? 'Generated'
                : c.caseType === 'Core' ? 'Core'
                : 'Secondary';
        groups[g].push({ ...c, idx: i });
    });

    for (const [label, items] of Object.entries(groups)) {
        if (items.length === 0) continue;
        const groupEl = document.createElement('div');
        groupEl.innerHTML = `<div class="case-group-label">${label} Cases (${items.length})</div>`;
        items.forEach(c => {
            const el = document.createElement('div');
            el.className = 'case-item';
            el.dataset.idx = c.idx;
            el.innerHTML = `<span class="name">${esc(c.caseID)}</span><span class="badge ${c.status}">${c.status}</span>`;
            el.addEventListener('click', () => selectCase(c.idx));
            groupEl.appendChild(el);
        });
        list.appendChild(groupEl);
    }
}

function setupSearch() {
    document.getElementById('search-box').addEventListener('input', function() {
        const q = this.value.toLowerCase();
        document.querySelectorAll('.case-item').forEach(el => {
            const idx = parseInt(el.dataset.idx);
            const c = ALL_CASES[idx];
            const match = c.caseID.toLowerCase().includes(q)
                || c.title.toLowerCase().includes(q);
            el.style.display = match ? '' : 'none';
        });
    });
}

// ── Tabs ─────────────────────────────────────
function setupTabs() {
    document.querySelectorAll('.tab').forEach(tab => {
        tab.addEventListener('click', function() {
            document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
            document.querySelectorAll('.tab-pane').forEach(p => p.classList.remove('active'));
            this.classList.add('active');
            document.getElementById('pane-' + this.dataset.tab).classList.add('active');

            // Re-render mermaid if Flow tab
            if (this.dataset.tab === 'flow' && currentCaseIdx >= 0) {
                renderMermaid();
            }
        });
    });
}

// ── Select case ──────────────────────────────
function selectCase(idx) {
    currentCaseIdx = idx;
    const c = ALL_CASES[idx];

    // Update sidebar active
    document.querySelectorAll('.case-item').forEach(el => {
        el.classList.toggle('active', parseInt(el.dataset.idx) === idx);
    });

    // Update header
    document.getElementById('header-title').textContent = c.title;
    const typeBadge = document.getElementById('header-type');
    typeBadge.textContent = c.caseType;
    typeBadge.style.display = '';
    const statusBadge = document.getElementById('header-status');
    statusBadge.textContent = c.status;
    statusBadge.className = 'badge ' + c.status;
    statusBadge.style.display = '';

    // Render all tabs
    renderOverview(c);
    renderEvidence(c);
    renderFlow(c);
    renderSuspects(c);
    renderVerdict(c);
    renderValidation(c);
    renderAssets(c);
}

// ── Overview tab ─────────────────────────────
function renderOverview(c) {
    const d = c.data;
    const pane = document.getElementById('pane-overview');

    if (c.placeholder) {
        pane.innerHTML = `<div class="empty-state"><div class="icon">&#128196;</div>
            <p>Placeholder case — no content authored yet.</p>
            <p style="margin-top:8px;font-size:12px;color:var(--muted)">ID: ${esc(c.caseID)} | Type: ${esc(c.caseType)}</p></div>`;
        return;
    }

    const allClues = new Set();
    (d.evidences || []).concat(d.extraEvidences || []).forEach(ev => {
        (ev.hotspots || []).forEach(h => allClues.add(h.clueId));
    });
    (d.suspects || []).forEach(s => {
        (s.tagInteractions || []).forEach(tag => {
            allResponses(tag).forEach(r => {
                (r.clickableClues || []).forEach(cl => allClues.add(cl.clueId));
            });
        });
    });

    const prereqs = (d.requiredPreviousCaseIds || []).join(', ') || 'None';
    const unlocks = (d.unlocksNextCaseIds || []).join(', ') || 'None';

    let html = `
        <div class="section-title">Case Metadata</div>
        <div class="meta-grid">
            <div class="meta-chip"><span class="label">ID:</span> ${esc(d.caseID)}</div>
            <div class="meta-chip"><span class="label">Type:</span> ${esc(d.caseType)}</div>
            <div class="meta-chip"><span class="label">Day:</span> ${d.firstAvailableDay}</div>
            <div class="meta-chip"><span class="label">Sequence:</span> #${d.coreSequenceNumber || 0}</div>
            <div class="meta-chip"><span class="label">Reward:</span> ${d.reward} knots</div>
            <div class="meta-chip"><span class="label">Law:</span> ${esc(d.lawBroken || 'N/A')}</div>
            <div class="meta-chip"><span class="label">Min Clues:</span> ${d.minDiscoveredCluesToAllowCommit}</div>
            <div class="meta-chip"><span class="label">Low Confidence:</span> ${d.allowCommitWithLowConfidence ? 'Yes' : 'No'}</div>
        </div>

        <div class="section-title">Description</div>
        <div class="card-panel">${esc(d.description)}</div>

        <div class="section-title">Case Chain</div>
        <div class="meta-grid">
            <div class="meta-chip"><span class="label">Requires:</span> ${esc(prereqs)}</div>
            <div class="meta-chip"><span class="label">Unlocks:</span> ${esc(unlocks)}</div>
        </div>`;

    if (d.involvesResistance) {
        html += `
        <div class="section-title">Resistance / State Choices</div>
        <div class="card-panel">
            <p><strong style="color:var(--green)">Resistance:</strong> ${esc(d.resistanceChoice || 'N/A')}</p>
            <p style="margin-top:6px"><strong style="color:var(--accent)">State:</strong> ${esc(d.stateChoice || 'N/A')}</p>
        </div>`;
    }

    html += `
        <div class="section-title">Quick Stats</div>
        <div class="meta-grid">
            <div class="meta-chip"><span class="label">Suspects:</span> ${(d.suspects || []).length}</div>
            <div class="meta-chip"><span class="label">Evidence:</span> ${(d.evidences || []).length + (d.extraEvidences || []).length}</div>
            <div class="meta-chip"><span class="label">Clues:</span> ${allClues.size}</div>
            <div class="meta-chip"><span class="label">Steps:</span> ${(d.steps || []).length}</div>
            <div class="meta-chip"><span class="label">Solutions:</span> ${(d.solutions || []).length}</div>
            <div class="meta-chip"><span class="label">Culprit:</span> ${esc(d.culpritCitizenID || 'N/A')}</div>
        </div>`;

    if ((d.steps || []).length > 0) {
        html += `<div class="section-title">Investigation Steps</div>`;
        d.steps.forEach(step => {
            html += `<div class="card-panel">
                <strong>Step ${step.stepNumber}:</strong> ${esc(step.description)}
                ${step.requiredClueIds && step.requiredClueIds.length ? '<br><span class="label">Requires:</span> ' + step.requiredClueIds.map(c => `<span class="tag-badge">${esc(c)}</span>`).join(' ') : ''}
                ${step.unlockedClueIds && step.unlockedClueIds.length ? '<br><span class="label">Unlocks:</span> ' + step.unlockedClueIds.map(c => `<span class="tag-badge gold">${esc(c)}</span>`).join(' ') : ''}
            </div>`;
        });
    }

    pane.innerHTML = html;
}

// ── Evidence tab ─────────────────────────────
function renderEvidence(c) {
    const d = c.data;
    const pane = document.getElementById('pane-evidence');

    if (c.placeholder) {
        pane.innerHTML = emptyState('No evidence data');
        return;
    }

    const allEv = (d.evidences || []).concat(d.extraEvidences || []);
    if (allEv.length === 0) {
        pane.innerHTML = emptyState('No evidence items defined');
        return;
    }

    let html = '';
    allEv.forEach(ev => {
        const substance = ev.foreignSubstance || 'None';
        html += `<div class="card-panel evidence">
            <h3>${esc(ev.title)} <span class="tag-badge gold">${esc(ev.type || 'Document')}</span></h3>
            <p style="margin:4px 0;color:var(--muted)">${esc(ev.description)}</p>
            <p style="font-size:11px"><span class="label">ID:</span> ${esc(ev.id)}
            ${substance !== 'None' ? ` | <span class="label">Substance:</span> <span style="color:var(--gold)">${esc(substance)}</span> (Spectrograph)` : ''}
            ${ev.associatedAppId ? ` | <span class="label">Disc App:</span> <span style="color:var(--cyan)">${esc(ev.associatedAppId)}</span>` : ''}</p>`;

        if ((ev.hotspots || []).length > 0) {
            html += `<table style="margin-top:8px">
                <tr><th>Clue ID</th><th>Note</th><th>Page</th><th>Position</th></tr>`;
            ev.hotspots.forEach(hs => {
                html += `<tr>
                    <td><span class="tag-badge">${esc(hs.clueId)}</span></td>
                    <td class="rich-text">${renderRichText(hs.noteText || '')}</td>
                    <td>${hs.pageIndex}</td>
                    <td style="font-size:10px">(${(hs.positionX||0.5).toFixed(2)}, ${(hs.positionY||0.5).toFixed(2)}) ${(hs.width||0.3).toFixed(2)}x${(hs.height||0.1).toFixed(2)}</td>
                </tr>`;
            });
            html += `</table>`;
        } else {
            html += `<p style="font-size:11px;color:var(--muted);margin-top:6px">No hotspots defined</p>`;
        }

        // Reference image section
        const ref = EVIDENCE_REFS[d.caseID + '/' + ev.id] || EVIDENCE_REFS[ev.id];
        if (ref) {
            html += `<div style="margin-top:10px;padding:10px;background:rgba(233,165,69,0.08);border:1px solid rgba(233,165,69,0.2);border-radius:6px">
                <p style="font-size:12px;font-weight:bold;color:var(--gold);margin-bottom:6px">Art Reference</p>
                <div style="display:flex;gap:12px;align-items:flex-start">`;
            if (ref.stockRef) {
                html += `<a href="${esc(ref.stockRef)}" target="_blank" style="flex-shrink:0">
                    <img src="${esc(ref.stockRef)}" style="width:120px;height:80px;object-fit:cover;border-radius:4px;border:1px solid var(--gold)" onerror="this.style.display='none'" />
                </a>`;
            }
            html += `<div style="flex:1;font-size:11px">`;
            if (ref.stockNote) html += `<p style="color:var(--muted);margin-bottom:4px"><span class="label">Ref:</span> ${esc(ref.stockNote)}</p>`;
            if (ref.aiPrompt) html += `<p style="margin-bottom:4px"><span class="label">AI Prompt:</span> <span style="color:var(--text);opacity:0.8">${esc(ref.aiPrompt)}</span></p>`;
            if (ref.artNotes) html += `<p style="color:var(--cyan)"><span class="label">Notes:</span> ${esc(ref.artNotes)}</p>`;
            html += `</div></div></div>`;
        }

        html += `</div>`;
    });

    pane.innerHTML = html;
}

// ── Flow tab (Mermaid) ───────────────────────
function renderFlow(c) {
    const pane = document.getElementById('pane-flow');
    if (c.placeholder || !c.mermaid) {
        pane.innerHTML = emptyState('No flow diagram — case has no content');
        return;
    }
    pane.innerHTML = `<div class="flow-container"><div class="mermaid" id="mermaid-graph">${esc(c.mermaid)}</div></div>`;

    // Render on next frame if visible
    const flowTab = document.querySelector('.tab[data-tab="flow"]');
    if (flowTab.classList.contains('active')) {
        renderMermaid();
    }
}

async function renderMermaid() {
    const el = document.getElementById('mermaid-graph');
    if (!el) return;
    const c = ALL_CASES[currentCaseIdx];
    if (!c || !c.mermaid) return;
    try {
        const { svg } = await mermaid.render('mermaid-svg-' + Date.now(), c.mermaid);
        el.innerHTML = svg;
    } catch(e) {
        el.innerHTML = `<pre style="color:var(--accent)">Mermaid render error: ${esc(e.message)}</pre>`;
    }
}

// ── Suspects tab ─────────────────────────────
function renderSuspects(c) {
    const d = c.data;
    const pane = document.getElementById('pane-suspects');

    if (c.placeholder || !(d.suspects || []).length) {
        pane.innerHTML = emptyState('No suspects');
        return;
    }

    let html = '';
    d.suspects.forEach(s => {
        const guilty = s.isGuilty;
        const panelClass = guilty ? 'suspect-guilty' : 'suspect-innocent';
        const label = guilty ? 'GUILTY' : 'INNOCENT';
        const labelColor = guilty ? 'var(--accent)' : 'var(--green)';

        html += `<div class="card-panel ${panelClass}">
            <h3>${esc(s.firstName)} ${esc(s.lastName)} <span style="color:${labelColor}">[${label}]</span></h3>
            <div class="meta-grid">
                <div class="meta-chip"><span class="label">ID:</span> ${esc(s.citizenID)}</div>
                <div class="meta-chip"><span class="label">Occupation:</span> ${esc(s.occupation || 'N/A')}</div>
                <div class="meta-chip"><span class="label">DOB:</span> ${esc(s.dateOfBirth || 'N/A')}</div>
                <div class="meta-chip"><span class="label">Address:</span> ${esc(s.address || 'N/A')}</div>
                <div class="meta-chip"><span class="label">Gender:</span> ${esc(s.gender || 'N/A')}</div>
                <div class="meta-chip"><span class="label">Marital:</span> ${esc(s.maritalStatus || 'N/A')}</div>
                <div class="meta-chip"><span class="label">Nervousness:</span> ${s.nervousnessLevel}</div>
                <div class="meta-chip"><span class="label">Initial Stress:</span> ${s.initialStress}</div>
            </div>`;

        // Criminal history
        if ((s.criminalHistory || []).length > 0) {
            html += `<div style="margin-top:8px"><strong style="font-size:12px;color:var(--gold)">Criminal History</strong>`;
            s.criminalHistory.forEach(cr => {
                html += `<div style="font-size:11px;margin:2px 0"> - ${esc(cr.offense)} (${esc(cr.date)}, ${esc(cr.severity)}): ${esc(cr.description)}</div>`;
            });
            html += `</div>`;
        }

        // Stress zone responses
        html += `<div style="margin-top:10px"><strong style="font-size:12px;color:var(--muted)">Stress Zone Fallbacks</strong>`;
        if ((s.lawyeredUpResponses || []).length)
            html += `<div style="font-size:11px;margin:4px 0"><span class="label">Lawyered Up:</span> "${esc(s.lawyeredUpResponses[0])}" <span style="color:var(--muted)">(+${s.lawyeredUpResponses.length - 1} more)</span></div>`;
        if ((s.rattledResponses || []).length)
            html += `<div style="font-size:11px;margin:4px 0"><span class="label">Rattled:</span> "${esc(s.rattledResponses[0])}" <span style="color:var(--muted)">(+${s.rattledResponses.length - 1} more)</span></div>`;
        if ((s.shutdownResponses || []).length)
            html += `<div style="font-size:11px;margin:4px 0"><span class="label">Shutdown:</span> "${esc(s.shutdownResponses[0])}" <span style="color:var(--muted)">(+${s.shutdownResponses.length - 1} more)</span></div>`;
        html += `</div>`;

        // Dialogue tree
        html += `<div style="margin-top:12px"><strong style="font-size:12px;color:var(--cyan)">Dialogue Tree (${(s.tagInteractions || []).length} tags)</strong></div>`;
        (s.tagInteractions || []).forEach((tag, ti) => {
            const tagIdx = `${s.citizenID}_${ti}`;
            html += buildDialogueTag(tag, tagIdx);
        });

        html += `</div>`; // close card-panel
    });

    pane.innerHTML = html;

    // Attach toggle listeners
    pane.querySelectorAll('.dialogue-tag-header').forEach(header => {
        header.addEventListener('click', function() {
            const body = this.nextElementSibling;
            const arrow = this.querySelector('.arrow');
            body.classList.toggle('open');
            arrow.classList.toggle('open');
        });
    });
}

function buildDialogueTag(tag, prefix) {
    let html = `<div class="dialogue-tag">
        <div class="dialogue-tag-header">
            <span class="arrow">&#9654;</span>
            <span class="tag-badge gold">${esc(tag.tagId)}</span>
            <span style="color:var(--muted);font-size:11px">${esc(tag.tagQuestion || '')}</span>
        </div>
        <div class="dialogue-tag-body">`;

    // Default responses
    (tag.responses || []).forEach((r, ri) => {
        html += buildResponseBlock(r, 'Default Response' + (tag.responses.length > 1 ? ` #${ri+1}` : ''));
    });

    // Contradiction info
    if ((tag.contradictedByEvidenceTagIds || []).length > 0) {
        html += `<div style="margin:8px 0;font-size:11px"><span class="label">Contradicted by:</span> ${tag.contradictedByEvidenceTagIds.map(id => `<span class="tag-badge lie">${esc(id)}</span>`).join(' ')}</div>`;
    }

    // Contradiction response
    if (tag.contradictionResponse) {
        html += buildResponseBlock(tag.contradictionResponse, 'Contradiction Response');
    }

    // Truth unlock info
    if ((tag.unlocksTruthForTagIds || []).length > 0) {
        html += `<div style="margin:8px 0;font-size:11px"><span class="label">Unlocks truth for:</span> ${tag.unlocksTruthForTagIds.map(id => `<span class="tag-badge truth">${esc(id)}</span>`).join(' ')}</div>`;
    }

    // Unlocked responses
    if (tag.unlockedInitialResponseIfPreviouslyDenied) {
        html += buildResponseBlock(tag.unlockedInitialResponseIfPreviouslyDenied, 'Truth (if previously denied)');
    }
    if (tag.unlockedInitialResponseIfNotDenied) {
        html += buildResponseBlock(tag.unlockedInitialResponseIfNotDenied, 'Truth (if not denied)');
    }
    (tag.unlockedFollowupResponses || []).forEach((r, ri) => {
        html += buildResponseBlock(r, `Unlocked Follow-up #${ri+1}`);
    });

    // Response variants
    (tag.responseVariants || []).forEach(variant => {
        const condStr = (variant.conditions || []).map(c => {
            if (c.type === 'StressAbove') return `Stress > ${c.threshold}`;
            if (c.type === 'StressBelow') return `Stress < ${c.threshold}`;
            if (c.type === 'TagAsked') return `Tag asked: ${c.targetId}`;
            if (c.type === 'TagNotAsked') return `Tag not asked: ${c.targetId}`;
            if (c.type === 'ClueDiscovered') return `Clue found: ${c.targetId}`;
            if (c.type === 'ClueNotDiscovered') return `Clue not found: ${c.targetId}`;
            return c.type + (c.targetId ? ': ' + c.targetId : '') + (c.threshold !== undefined ? ' @ ' + c.threshold : '');
        }).join(', ');

        html += `<div style="margin:6px 0;font-size:11px;color:var(--gold)">Variant: ${esc(variant.variantId)} <span style="color:var(--muted)">[${esc(condStr)}]</span> (weight: ${variant.weight || 1.0})</div>`;
        (variant.responses || []).forEach((r, ri) => {
            html += buildResponseBlock(r, `Variant Response #${ri+1}`);
        });
    });

    html += `</div></div>`; // close body + tag
    return html;
}

function buildResponseBlock(resp, label) {
    const lieClass = resp.isLie ? 'lie' : 'truth';
    const lieLabel = resp.isLie ? 'LIE' : 'TRUTH';

    let html = `<div class="response-block">
        <div class="resp-label ${lieClass}">${esc(label)} — ${lieLabel}
            ${resp.responseType ? ` | ${esc(resp.responseType)}` : ''}
            ${resp.stressImpact ? ` | Stress: +${resp.stressImpact}` : ''}</div>`;

    // Response lines with clickable highlighting
    const clickables = (resp.clickableClues || []).map(cl => cl.clickableText);
    (resp.responseSequence || []).forEach(line => {
        let rendered = esc(line);
        // Highlight clickable segments
        clickables.forEach(ct => {
            const escaped = esc(ct);
            if (rendered.includes(escaped)) {
                rendered = rendered.replace(escaped, `<span class="clickable-highlight">${escaped}</span>`);
            }
        });
        html += `<div class="response-line">"${rendered}"</div>`;
    });

    // Clickable clues detail
    if ((resp.clickableClues || []).length > 0) {
        html += `<div style="margin-top:6px">`;
        resp.clickableClues.forEach(cl => {
            html += `<div style="font-size:11px;margin:3px 0">
                <span class="tag-badge">${esc(cl.clueId)}</span>
                <span style="color:var(--muted)">click: "</span><span style="color:var(--gold)">${esc(cl.clickableText)}</span><span style="color:var(--muted)">"</span>
                <br><span class="rich-text" style="font-size:10px;margin-left:12px">${renderRichText(cl.noteText || '')}</span>
            </div>`;
        });
        html += `</div>`;
    }

    html += `</div>`;
    return html;
}

// ── Verdict tab ──────────────────────────────
function renderVerdict(c) {
    const d = c.data;
    const pane = document.getElementById('pane-verdict');

    if (c.placeholder) {
        pane.innerHTML = emptyState('No verdict data');
        return;
    }

    const schema = d.verdictSchema || {};
    let html = `
        <div class="section-title">Sentence Template</div>
        <div class="card-panel" style="font-size:16px;color:var(--green)">${esc(schema.sentenceTemplate || 'N/A').replace(/\{(\w+)\}/g, '<span style="color:var(--gold);border-bottom:2px dashed var(--gold)">{$1}</span>')}</div>

        <div class="section-title">Slot Definitions</div>
        <table>
            <tr><th>Slot ID</th><th>Label</th><th>Type</th><th>Source</th><th>Required</th></tr>`;
    (schema.slots || []).forEach(slot => {
        html += `<tr>
            <td>${esc(slot.slotId)}</td>
            <td>${esc(slot.displayLabel)}</td>
            <td><span class="tag-badge">${esc(slot.type)}</span></td>
            <td>${esc(slot.optionSource || 'CaseAndGlobal')}</td>
            <td>${slot.required ? '<span style="color:var(--accent)">Yes</span>' : 'No'}</td>
        </tr>`;
    });
    html += `</table>`;

    // Solutions
    html += `<div class="section-title">Accepted Solutions</div>`;
    (d.solutions || []).forEach((sol, i) => {
        html += `<div class="card-panel">
            <strong>Solution ${i+1}</strong> <span style="color:var(--muted)">(min confidence: ${sol.minConfidenceToApprove}%)</span>
            <ul style="margin:6px 0 0 16px;font-size:12px">`;
        (sol.answers || []).forEach(ans => {
            html += `<li>${esc(ans.slotId)}: ${ans.acceptedOptionIds.map(id => `<span class="tag-badge">${esc(id)}</span>`).join(' ')}</li>`;
        });
        html += `</ul></div>`;
    });

    // Clue-verdict mappings
    const mappings = d.clueVerdictMappings || [];
    if (mappings.length > 0) {
        html += `<div class="section-title">Clue &rarr; Verdict Mappings</div>
            <table>
                <tr><th>Clue</th><th>Slot</th><th>Option ID</th><th>Label</th></tr>`;
        mappings.forEach(m => {
            html += `<tr>
                <td><span class="tag-badge">${esc(m.clueId)}</span></td>
                <td>${esc(m.slotId)}</td>
                <td>${esc(m.optionId)}</td>
                <td>${esc(m.label)}</td>
            </tr>`;
        });
        html += `</table>`;
    }

    pane.innerHTML = html;
}

// ── Validation tab ───────────────────────────
function renderValidation(c) {
    const pane = document.getElementById('pane-validation');
    const v = c.validation || {};
    const phases = Object.keys(v);

    if (c.placeholder) {
        pane.innerHTML = emptyState('Placeholder case — validation skipped');
        return;
    }

    if (phases.length === 0) {
        pane.innerHTML = emptyState('No validation results');
        return;
    }

    let totalE = 0, totalW = 0;
    phases.forEach(p => { totalE += v[p].errors.length; totalW += v[p].warnings.length; });
    const overall = totalE > 0 ? 'FAIL' : totalW > 0 ? 'WARN' : 'PASS';

    let html = `<div class="meta-grid">
        <div class="meta-chip"><span class="label">Status:</span> <span class="badge ${overall}">${overall}</span></div>
        <div class="meta-chip"><span class="label">Errors:</span> <span style="color:var(--accent)">${totalE}</span></div>
        <div class="meta-chip"><span class="label">Warnings:</span> <span style="color:var(--gold)">${totalW}</span></div>
    </div>`;

    const phaseNames = {
        SCHEMA: 'Phase 1: Schema Compliance',
        UNIQUE: 'Phase 2: ID Uniqueness',
        XREF: 'Phase 3: Cross-References',
        CLICK: 'Phase 4: Clickable Text',
        REACH: 'Phase 5: Reachability',
        TOOL: 'Phase 6: Tool Consistency',
        STRESS: 'Phase 7: Stress Feasibility'
    };

    phases.forEach(p => {
        const data = v[p];
        const pe = data.errors.length, pw = data.warnings.length, pi = data.info.length;
        const pStatus = pe > 0 ? 'FAIL' : pw > 0 ? 'WARN' : 'PASS';
        const counts = [];
        if (pe) counts.push(`<span style="color:var(--accent)">${pe} errors</span>`);
        if (pw) counts.push(`<span style="color:var(--gold)">${pw} warnings</span>`);
        if (pi) counts.push(`<span style="color:var(--muted)">${pi} info</span>`);

        html += `<div class="validation-phase">
            <div class="validation-phase-header" onclick="this.nextElementSibling.classList.toggle('open')">
                <span class="badge ${pStatus}">${pStatus}</span>
                <span>${esc(phaseNames[p] || p)}</span>
                <span style="margin-left:auto;font-size:10px">${counts.join(' | ')}</span>
            </div>
            <div class="validation-phase-body">`;

        data.errors.forEach(e => { html += `<div class="val-item error">${esc(e)}</div>`; });
        data.warnings.forEach(w => { html += `<div class="val-item warning">${esc(w)}</div>`; });
        data.info.forEach(i => { html += `<div class="val-item info">${esc(i)}</div>`; });

        if (!pe && !pw && !pi) html += `<div class="val-item info">No issues</div>`;

        html += `</div></div>`;
    });

    pane.innerHTML = html;
}

// ── Assets tab ───────────────────────────────
function renderAssets(c) {
    const pane = document.getElementById('pane-assets');

    if (c.placeholder) {
        pane.innerHTML = emptyState('No asset requirements — placeholder case');
        return;
    }

    const arts = c.art || [];
    if (arts.length === 0) {
        pane.innerHTML = emptyState('No art assets computed');
        return;
    }

    // Group by type
    const byType = {};
    arts.forEach(a => {
        if (!byType[a.type]) byType[a.type] = [];
        byType[a.type].push(a);
    });

    let html = `<div class="meta-grid">
        <div class="meta-chip"><span class="label">Total Assets:</span> ${arts.length}</div>
        ${Object.entries(byType).map(([t,items]) => `<div class="meta-chip"><span class="label">${esc(t)}:</span> ${items.length}</div>`).join('')}
    </div>`;

    html += `<table>
        <tr><th>Type</th><th>Asset Path</th><th>Notes</th></tr>`;
    arts.forEach(a => {
        html += `<tr>
            <td><span class="tag-badge">${esc(a.type)}</span></td>
            <td style="font-size:11px;color:var(--cyan)">${esc(a.asset)}</td>
            <td style="font-size:11px">${esc(a.notes)}</td>
        </tr>`;
    });
    html += `</table>`;

    // Cross-case summary
    html += `<div class="section-title" style="margin-top:24px">Cross-Case Asset Summary</div>`;
    let totalAll = 0;
    const allByType = {};
    ALL_CASES.forEach(cc => {
        (cc.art || []).forEach(a => {
            if (!allByType[a.type]) allByType[a.type] = 0;
            allByType[a.type]++;
            totalAll++;
        });
    });
    html += `<div class="meta-grid">
        <div class="meta-chip"><span class="label">Grand Total:</span> ${totalAll} assets across ${ALL_CASES.filter(c => !c.placeholder).length} cases</div>
        ${Object.entries(allByType).map(([t,n]) => `<div class="meta-chip"><span class="label">${esc(t)}:</span> ${n}</div>`).join('')}
    </div>`;

    pane.innerHTML = html;
}

// ── Utilities ────────────────────────────────
function esc(s) {
    if (s == null) return '';
    return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function renderRichText(text) {
    return esc(text)
        .replace(/&lt;person&gt;/g, '<person>')
        .replace(/&lt;\/person&gt;/g, '</person>')
        .replace(/&lt;location&gt;/g, '<location>')
        .replace(/&lt;\/location&gt;/g, '</location>')
        .replace(/&lt;item&gt;/g, '<item>')
        .replace(/&lt;\/item&gt;/g, '</item>');
}

function emptyState(msg) {
    return `<div class="empty-state"><div class="icon">&#128196;</div>${esc(msg)}</div>`;
}

function allResponses(tag) {
    const r = [...(tag.responses || [])];
    if (tag.contradictionResponse) r.push(tag.contradictionResponse);
    if (tag.unlockedInitialResponseIfPreviouslyDenied) r.push(tag.unlockedInitialResponseIfPreviouslyDenied);
    if (tag.unlockedInitialResponseIfNotDenied) r.push(tag.unlockedInitialResponseIfNotDenied);
    (tag.unlockedFollowupResponses || []).forEach(x => r.push(x));
    (tag.responseVariants || []).forEach(v => (v.responses || []).forEach(x => r.push(x)));
    return r;
}
</script>
</body>
</html>"""


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(
        description="Generate an interactive case browser for Marlo case files"
    )
    parser.add_argument(
        "-o", "--output",
        default=str(OUTPUT_DIR / "case_browser.html"),
        help="Output HTML file path (default: output/case_browser.html)",
    )
    parser.add_argument(
        "--open",
        action="store_true",
        help="Open the generated HTML in the default browser",
    )
    args = parser.parse_args()

    # Load schema for validation
    schema = None
    if SCHEMA_PATH.exists() and HAS_JSONSCHEMA:
        with open(SCHEMA_PATH, "r", encoding="utf-8") as f:
            schema = json.load(f)

    # Discover case files
    case_files = []
    if CASES_DIR.exists():
        case_files.extend(sorted(CASES_DIR.glob("*.json")))
    if OUTPUT_DIR.exists():
        case_files.extend(sorted(OUTPUT_DIR.glob("*_generated.json")))

    if not case_files:
        print("No case files found.")
        sys.exit(1)

    print(f"Found {len(case_files)} case file(s)...")

    # Load all cases and run validation
    all_cases = []
    all_validation = {}

    for fp in case_files:
        try:
            with open(fp, "r", encoding="utf-8") as f:
                data = json.load(f)
        except json.JSONDecodeError as e:
            print(f"  Skipping {fp.name}: invalid JSON ({e})")
            continue

        case_id = data.get("caseID", fp.stem)
        source = "generated" if "_generated" in fp.name else "deployed"

        entry = {"data": data, "filename": fp.name, "source": source}
        all_cases.append(entry)

        # Validate non-placeholder cases
        if not is_placeholder(data):
            phases = validate_case_inline(data, schema)
            all_validation[case_id] = phases
            status = compute_overall_status(phases)
            total_e = sum(len(p["errors"]) for p in phases.values())
            total_w = sum(len(p["warnings"]) for p in phases.values())
            print(f"  {case_id}: {status} ({total_e} errors, {total_w} warnings)")
        else:
            print(f"  {case_id}: EMPTY (placeholder)")

    # Generate HTML
    html = generate_browser_html(all_cases, all_validation)

    output_path = Path(args.output)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(html, encoding="utf-8")
    print(f"\nGenerated: {output_path}")

    if args.open:
        webbrowser.open(str(output_path.resolve()))


if __name__ == "__main__":
    main()
