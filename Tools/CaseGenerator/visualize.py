#!/usr/bin/env python3
"""
Case Flow Visualizer for The Strange Rebellion of Marlo.

Generates HTML pages with Mermaid.js flow diagrams showing the investigation
flow for each case: Evidence -> Clues -> Tags -> Interrogation -> Verdict.

Usage:
    python visualize.py path/to/case.json
    python visualize.py path/to/cases/*.json     # Batch mode
    python visualize.py --all                     # All cases
"""

import argparse
import json
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent  # Tools/CaseGenerator -> Tools -> project root
CASES_DIR = PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"
OUTPUT_DIR = SCRIPT_DIR / "output"

HTML_TEMPLATE = """<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{title} — Case Flow</title>
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Courier New', monospace;
            background: #1a1a2e;
            color: #e0e0e0;
            padding: 20px;
        }}
        h1 {{
            color: #e94560;
            border-bottom: 2px solid #e94560;
            padding-bottom: 10px;
            margin-bottom: 20px;
        }}
        h2 {{
            color: #0f3460;
            background: #16213e;
            padding: 8px 16px;
            margin: 20px 0 10px;
            border-left: 4px solid #e94560;
        }}
        h3 {{
            color: #53d8fb;
            margin: 15px 0 8px;
        }}
        .header {{
            background: #16213e;
            padding: 20px;
            border-radius: 8px;
            margin-bottom: 20px;
        }}
        .header .meta {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: 10px;
            margin-top: 10px;
        }}
        .header .meta span {{
            background: #0f3460;
            padding: 6px 12px;
            border-radius: 4px;
            font-size: 0.9em;
        }}
        .flow-diagram {{
            background: #16213e;
            padding: 20px;
            border-radius: 8px;
            margin: 20px 0;
            overflow-x: auto;
        }}
        .mermaid {{
            display: flex;
            justify-content: center;
        }}
        .panel {{
            background: #16213e;
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0;
        }}
        .suspect {{
            border-left: 4px solid #e94560;
            padding: 10px 15px;
            margin: 10px 0;
            background: #0f3460;
            border-radius: 0 8px 8px 0;
        }}
        .suspect.guilty {{ border-left-color: #e94560; }}
        .suspect.innocent {{ border-left-color: #4ecca3; }}
        .evidence {{
            border-left: 4px solid #53d8fb;
            padding: 10px 15px;
            margin: 10px 0;
            background: #0f3460;
            border-radius: 0 8px 8px 0;
        }}
        .clue {{
            display: inline-block;
            background: #0f3460;
            padding: 4px 10px;
            border-radius: 12px;
            margin: 3px;
            font-size: 0.85em;
            border: 1px solid #53d8fb;
        }}
        .verdict-path {{
            border: 2px solid #4ecca3;
            padding: 15px;
            border-radius: 8px;
            margin: 10px 0;
        }}
        .warning {{
            background: #5c3d1e;
            border-left: 4px solid #e9a645;
            padding: 10px 15px;
            margin: 10px 0;
            border-radius: 0 8px 8px 0;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 10px 0;
        }}
        th, td {{
            padding: 8px 12px;
            text-align: left;
            border-bottom: 1px solid #0f3460;
        }}
        th {{ background: #0f3460; color: #53d8fb; }}
        .tag {{ color: #e9a645; }}
        .lie {{ color: #e94560; }}
        .truth {{ color: #4ecca3; }}
    </style>
</head>
<body>
    <div class="header">
        <h1>{title}</h1>
        <p>{description}</p>
        <div class="meta">
            <span>ID: {case_id}</span>
            <span>Type: {case_type}</span>
            <span>Day: {day}</span>
            <span>Reward: {reward} marks</span>
            <span>Law: {law_broken}</span>
            <span>Min Clues: {min_clues}</span>
        </div>
    </div>

    <h2>Investigation Flow</h2>
    <div class="flow-diagram">
        <div class="mermaid">
{mermaid_diagram}
        </div>
    </div>

    <h2>Suspects</h2>
    {suspects_html}

    <h2>Evidence</h2>
    {evidence_html}

    <h2>Clue Map</h2>
    <div class="panel">
        {clues_html}
    </div>

    <h2>Interrogation Paths</h2>
    {interrogation_html}

    <h2>Verdict</h2>
    {verdict_html}

    {warnings_html}

    <script>
        mermaid.initialize({{
            startOnLoad: true,
            theme: 'dark',
            themeVariables: {{
                primaryColor: '#16213e',
                primaryBorderColor: '#53d8fb',
                primaryTextColor: '#e0e0e0',
                lineColor: '#53d8fb',
                secondaryColor: '#0f3460',
                tertiaryColor: '#1a1a2e'
            }}
        }});
    </script>
</body>
</html>
"""


def sanitize_mermaid(text: str) -> str:
    """Sanitize text for use in Mermaid diagram labels."""
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
    """Build a Mermaid.js flowchart from case data."""
    lines = ["graph TD"]

    # Evidence nodes
    for ev in case.get("evidences", []):
        ev_id = ev["id"].replace("-", "_")
        label = sanitize_mermaid(ev["title"])
        lines.append(f'    {ev_id}["{label}"]')
        lines.append(f'    style {ev_id} fill:#0f3460,stroke:#53d8fb')

        # Hotspots to clues
        for hotspot in ev.get("hotspots", []):
            clue_id = hotspot["clueId"].replace("-", "_")
            clue_label = sanitize_mermaid(hotspot["clueId"])
            lines.append(f'    {clue_id}("{clue_label}")')
            lines.append(f'    {ev_id} -->|hotspot| {clue_id}')
            lines.append(f'    style {clue_id} fill:#1a3a5c,stroke:#e9a645')

    # Interrogation connections
    for suspect in case.get("suspects", []):
        s_id = suspect["citizenID"].replace("-", "_")
        s_label = f"{suspect['firstName']} {suspect['lastName']}"
        guilty_style = "#5c1e1e" if suspect.get("isGuilty") else "#1e5c3d"
        lines.append(f'    {s_id}{{{{{s_label}}}}}')
        lines.append(f'    style {s_id} fill:{guilty_style},stroke:#e94560')

        for tag in suspect.get("tagInteractions", []):
            tag_id = tag["tagId"].replace("-", "_")

            # Connect clue tag to suspect interrogation
            lines.append(f'    {tag_id} -->|interrogate| {s_id}')

            # Contradiction paths
            if tag.get("contradictionResponse"):
                contra_node = f'{s_id}_{tag_id}_contra'
                lines.append(f'    {contra_node}["Contradiction"]')
                lines.append(f'    {s_id} -.->|contradicted| {contra_node}')
                lines.append(f'    style {contra_node} fill:#5c3d1e,stroke:#e9a645')

                # Clickable clues from contradiction
                for clue in tag["contradictionResponse"].get("clickableClues", []):
                    new_clue = clue["clueId"].replace("-", "_")
                    new_label = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {new_clue}("{new_label}")')
                    lines.append(f'    {contra_node} -->|reveals| {new_clue}')
                    lines.append(f'    style {new_clue} fill:#1a3a5c,stroke:#4ecca3')

            # Truth unlock paths
            if tag.get("unlockedInitialResponseIfPreviouslyDenied"):
                unlock = tag["unlockedInitialResponseIfPreviouslyDenied"]
                for clue in unlock.get("clickableClues", []):
                    new_clue = clue["clueId"].replace("-", "_")
                    new_label = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {new_clue}("{new_label}")')
                    lines.append(f'    {s_id} -.->|truth unlocked| {new_clue}')
                    lines.append(f'    style {new_clue} fill:#1a3a5c,stroke:#4ecca3')

            # Default response clickable clues
            for resp in tag.get("responses", []):
                for clue in resp.get("clickableClues", []):
                    new_clue = clue["clueId"].replace("-", "_")
                    new_label = sanitize_mermaid(clue["clueId"])
                    lines.append(f'    {new_clue}("{new_label}")')
                    is_lie = "lie" if resp.get("isLie") else "truth"
                    lines.append(f'    {s_id} -->|{is_lie}| {new_clue}')
                    color = "#4ecca3" if not resp.get("isLie") else "#e94560"
                    lines.append(f'    style {new_clue} fill:#1a3a5c,stroke:{color}')

    # Verdict node
    lines.append('    VERDICT["VERDICT FORM"]')
    lines.append('    style VERDICT fill:#1e5c3d,stroke:#4ecca3,stroke-width:3px')

    # Connect clue-verdict mappings
    for mapping in case.get("clueVerdictMappings", []):
        clue_id = mapping["clueId"].replace("-", "_")
        label = sanitize_mermaid(mapping.get("label", mapping["optionId"]))
        lines.append(f'    {clue_id} -->|unlocks: {label}| VERDICT')

    return "\n".join(lines)


def build_suspects_html(case: dict) -> str:
    """Build HTML for the suspects panel."""
    html = []
    for suspect in case.get("suspects", []):
        guilty_class = "guilty" if suspect.get("isGuilty") else "innocent"
        guilty_label = "GUILTY" if suspect.get("isGuilty") else "INNOCENT"
        html.append(f'<div class="suspect {guilty_class}">')
        html.append(f'<h3>{suspect["firstName"]} {suspect["lastName"]} '
                    f'<span class="{"lie" if suspect.get("isGuilty") else "truth"}">[{guilty_label}]</span></h3>')
        html.append(f'<p>ID: {suspect["citizenID"]} | '
                    f'Occupation: {suspect.get("occupation", "N/A")} | '
                    f'Stress: {suspect.get("initialStress", -1)} | '
                    f'Nervousness: {suspect.get("nervousnessLevel", 0.3)}</p>')
        html.append(f'<p>Tags: {len(suspect.get("tagInteractions", []))} interactions</p>')
        html.append('</div>')
    return "\n".join(html)


def build_evidence_html(case: dict) -> str:
    """Build HTML for the evidence panel."""
    html = []
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        html.append(f'<div class="evidence">')
        html.append(f'<h3>{ev["title"]} <span class="tag">[{ev.get("type", "Document")}]</span></h3>')
        html.append(f'<p>{ev["description"]}</p>')
        substance = ev.get("foreignSubstance", "None")
        if substance != "None":
            html.append(f'<p>Foreign Substance: <span class="tag">{substance}</span> (Spectrograph)</p>')
        if ev.get("associatedAppId"):
            html.append(f'<p>Disc App: <span class="tag">{ev["associatedAppId"]}</span></p>')
        html.append(f'<p>Hotspots: {len(ev.get("hotspots", []))}</p>')
        for hs in ev.get("hotspots", []):
            html.append(f'<span class="clue">{hs["clueId"]}</span>')
        html.append('</div>')
    return "\n".join(html)


def build_clues_html(case: dict) -> str:
    """Build HTML showing all discoverable clues."""
    clues = {}

    # From evidence
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        for hs in ev.get("hotspots", []):
            clues[hs["clueId"]] = f'Evidence: {ev["title"]} (page {hs.get("pageIndex", 0)})'

    # From interrogation
    for suspect in case.get("suspects", []):
        for tag in suspect.get("tagInteractions", []):
            for resp in _all_responses_flat(tag):
                for clue in resp.get("clickableClues", []):
                    source_type = "lie" if resp.get("isLie") else "truth"
                    clues[clue["clueId"]] = (
                        f'Interrogation: {suspect["firstName"]} {suspect["lastName"]} '
                        f'(tag: {tag["tagId"]}, {source_type})'
                    )

    html = ['<table>', '<tr><th>Clue ID</th><th>Source</th></tr>']
    for clue_id, source in sorted(clues.items()):
        html.append(f'<tr><td><span class="clue">{clue_id}</span></td><td>{source}</td></tr>')
    html.append('</table>')
    return "\n".join(html)


def _all_responses_flat(tag: dict) -> list:
    """Get all response objects from a tag interaction."""
    responses = list(tag.get("responses", []))
    if tag.get("contradictionResponse"):
        responses.append(tag["contradictionResponse"])
    if tag.get("unlockedInitialResponseIfPreviouslyDenied"):
        responses.append(tag["unlockedInitialResponseIfPreviouslyDenied"])
    if tag.get("unlockedInitialResponseIfNotDenied"):
        responses.append(tag["unlockedInitialResponseIfNotDenied"])
    responses.extend(tag.get("unlockedFollowupResponses", []))
    for variant in tag.get("responseVariants", []):
        responses.extend(variant.get("responses", []))
    return responses


def build_interrogation_html(case: dict) -> str:
    """Build HTML for interrogation paths."""
    html = []
    for suspect in case.get("suspects", []):
        html.append(f'<div class="panel">')
        html.append(f'<h3>{suspect["firstName"]} {suspect["lastName"]}</h3>')
        html.append('<table>')
        html.append('<tr><th>Tag</th><th>Default</th><th>Contradiction</th><th>Clues Revealed</th></tr>')

        for tag in suspect.get("tagInteractions", []):
            default_type = "unknown"
            if tag.get("responses"):
                default_type = "lie" if tag["responses"][0].get("isLie") else "truth"

            contra = "—"
            if tag.get("contradictedByEvidenceTagIds"):
                contra_ids = ", ".join(tag["contradictedByEvidenceTagIds"])
                contra = f'Present: {contra_ids}'

            clues_revealed = []
            for resp in _all_responses_flat(tag):
                for clue in resp.get("clickableClues", []):
                    clues_revealed.append(clue["clueId"])

            clues_str = ", ".join(f'<span class="clue">{c}</span>' for c in clues_revealed) or "—"

            html.append(
                f'<tr>'
                f'<td><span class="tag">{tag["tagId"]}</span></td>'
                f'<td class="{default_type}">{default_type.upper()}</td>'
                f'<td>{contra}</td>'
                f'<td>{clues_str}</td>'
                f'</tr>'
            )

        html.append('</table>')
        html.append('</div>')
    return "\n".join(html)


def build_verdict_html(case: dict) -> str:
    """Build HTML for verdict information."""
    html = ['<div class="verdict-path">']

    schema = case.get("verdictSchema", {})
    html.append(f'<h3>Template: {schema.get("sentenceTemplate", "N/A")}</h3>')

    html.append('<table>')
    html.append('<tr><th>Slot</th><th>Label</th><th>Type</th><th>Source</th><th>Required</th></tr>')
    for slot in schema.get("slots", []):
        html.append(
            f'<tr>'
            f'<td>{slot["slotId"]}</td>'
            f'<td>{slot["displayLabel"]}</td>'
            f'<td>{slot["type"]}</td>'
            f'<td>{slot.get("optionSource", "CaseAndGlobal")}</td>'
            f'<td>{"Yes" if slot.get("required") else "No"}</td>'
            f'</tr>'
        )
    html.append('</table>')

    html.append('<h3>Accepted Solutions</h3>')
    for i, solution in enumerate(case.get("solutions", [])):
        html.append(f'<p>Solution {i+1} (min confidence: {solution.get("minConfidenceToApprove", 100)}%):</p>')
        html.append('<ul>')
        for answer in solution.get("answers", []):
            options = ", ".join(answer.get("acceptedOptionIds", []))
            html.append(f'<li>{answer["slotId"]}: {options}</li>')
        html.append('</ul>')

    # Clue-verdict mappings
    mappings = case.get("clueVerdictMappings", [])
    if mappings:
        html.append('<h3>Clue → Verdict Mappings</h3>')
        html.append('<table>')
        html.append('<tr><th>Clue</th><th>Slot</th><th>Option</th><th>Label</th></tr>')
        for m in mappings:
            html.append(
                f'<tr>'
                f'<td><span class="clue">{m["clueId"]}</span></td>'
                f'<td>{m["slotId"]}</td>'
                f'<td>{m["optionId"]}</td>'
                f'<td>{m["label"]}</td>'
                f'</tr>'
            )
        html.append('</table>')

    html.append('</div>')
    return "\n".join(html)


def generate_html(case: dict) -> str:
    """Generate the complete HTML page for a case."""
    mermaid = build_mermaid_diagram(case)
    suspects = build_suspects_html(case)
    evidence = build_evidence_html(case)
    clues = build_clues_html(case)
    interrogation = build_interrogation_html(case)
    verdict = build_verdict_html(case)

    warnings = ""
    resistance = case.get("involvesResistance", False)
    if resistance:
        warnings = (
            '<h2>Narrative Flags</h2>'
            '<div class="warning">'
            f'<p>Resistance Choice: {case.get("resistanceChoice", "N/A")}</p>'
            f'<p>State Choice: {case.get("stateChoice", "N/A")}</p>'
            '</div>'
        )

    return HTML_TEMPLATE.format(
        title=case.get("title", "Unknown Case"),
        description=case.get("description", ""),
        case_id=case.get("caseID", "unknown"),
        case_type=case.get("caseType", "Unknown"),
        day=case.get("firstAvailableDay", "?"),
        reward=case.get("reward", 0),
        law_broken=case.get("lawBroken", "N/A"),
        min_clues=case.get("minDiscoveredCluesToAllowCommit", 3),
        mermaid_diagram=mermaid,
        suspects_html=suspects,
        evidence_html=evidence,
        clues_html=clues,
        interrogation_html=interrogation,
        verdict_html=verdict,
        warnings_html=warnings,
    )


def main():
    parser = argparse.ArgumentParser(
        description="Generate HTML flow diagrams for Marlo case files"
    )
    parser.add_argument(
        "files",
        nargs="*",
        help="Case JSON file(s) to visualize",
    )
    parser.add_argument(
        "--all", "-a",
        action="store_true",
        help="Visualize all cases in StreamingAssets/content/cases/",
    )
    parser.add_argument(
        "--output-dir", "-o",
        default=str(OUTPUT_DIR),
        help=f"Output directory for HTML files (default: {OUTPUT_DIR})",
    )

    args = parser.parse_args()

    if not args.files and not args.all:
        parser.print_help()
        sys.exit(1)

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    files = []
    if args.all:
        if CASES_DIR.exists():
            files = sorted(CASES_DIR.glob("*.json"))
        # Also include generated cases
        if OUTPUT_DIR.exists():
            files.extend(sorted(OUTPUT_DIR.glob("*_generated.json")))
    else:
        files = [Path(f) for f in args.files]

    generated = 0
    for file_path in files:
        if not file_path.exists():
            print(f"Warning: File not found: {file_path}")
            continue

        try:
            with open(file_path, "r", encoding="utf-8") as f:
                case = json.load(f)
        except json.JSONDecodeError as e:
            print(f"Error: Invalid JSON in {file_path}: {e}")
            continue

        case_id = case.get("caseID", file_path.stem)

        # Skip placeholder cases with no suspects
        if not case.get("suspects"):
            print(f"Skipping {case_id} — no suspects (placeholder)")
            continue

        html = generate_html(case)
        output_path = output_dir / f"{case_id}_flow.html"
        output_path.write_text(html, encoding="utf-8")
        print(f"Generated: {output_path}")
        generated += 1

    print(f"\nGenerated {generated} flow diagram(s) in {output_dir}")


if __name__ == "__main__":
    main()
