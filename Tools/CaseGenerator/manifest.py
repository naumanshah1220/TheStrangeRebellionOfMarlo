#!/usr/bin/env python3
"""
Art Manifest Generator for The Strange Rebellion of Marlo.

Takes a case JSON and outputs a detailed art manifest listing every
visual asset needed: portraits, evidence cards, case cards, tool overlays.
Each entry includes description, text content, visual style, and an
AI art prompt ready for Midjourney/DALL-E/Stable Diffusion.

Usage:
    python manifest.py path/to/case.json
    python manifest.py path/to/case.json --output manifest.json
    python manifest.py --all
"""

import argparse
import json
import sys
from pathlib import Path

SCRIPT_DIR = Path(__file__).parent
PROJECT_ROOT = SCRIPT_DIR.parent.parent
CASES_DIR = PROJECT_ROOT / "Assets" / "StreamingAssets" / "content" / "cases"
OUTPUT_DIR = SCRIPT_DIR / "output"

# ==============================================================================
# STYLE GUIDE — shared across all assets
# ==============================================================================

STYLE_GUIDE = {
    "world": "Republic of Drazhovia — fictional 1970s-80s Eastern European authoritarian state",
    "aesthetic": "Pixel art, Papers Please style. Limited color palette (max 16-24 colors per asset). "
                 "Chunky readable pixels. Dithering for gradients and shadows. "
                 "Deliberate low resolution (think 320x240 upscaled). "
                 "Muted, desaturated tones — institutional greens, grays, browns. "
                 "No anti-aliasing. Crisp pixel edges. Readable text rendered as pixel font.",
    "color_palette": {
        "paper": "#d4c8a0 (warm tan) to #b8a878 (aged, darker tan)",
        "ink_official": "#2a2a2a (near-black pixel text)",
        "ink_handwritten": "#2a3a5a (dark blue-gray, pixel handwriting)",
        "ink_stamp": "#7a2020 (dark red, dithered for fade effect)",
        "highlight": "#cc3333 (analyst's red marks — solid pixels, no blur)",
        "state_green": "#2d4a3e (institutional green, used for headers/borders)",
        "state_brown": "#5c4a3a (wood, desk surfaces)",
        "bg_dark": "#1a1a2e (dark UI background, like Papers Please desk)",
        "bg_mid": "#2a2a3e (panel/window backgrounds)",
        "skin_light": "#d4a878 (lighter skin, pixel portrait)",
        "skin_medium": "#b88860 (medium skin, pixel portrait)",
        "skin_dark": "#8a6040 (darker skin tones)",
    },
    "document_elements": [
        "Pixel-font headers in ALL CAPS (think 8x8 or 6x8 pixel fonts)",
        "Single or double pixel-wide borders",
        "State emblem: small pixel icon — circle with 'P' (for Pattern)",
        "'REPUBLIC OF DRAZHOVIA' header in pixel capitals",
        "Form numbers (e.g. 'Form PA-7')",
        "Pixelated circular rubber stamp (dithered red, slightly offset)",
        "Optional: coffee stain as a few dithered brown pixels",
        "'THE PATTERN PROVIDES' footer in small pixel text",
        "Classification strips: solid colored bar with pixel text",
        "Table grids: 1px lines, clear cell separation",
    ],
    "photo_style": "Pixel art surveillance photo. Very low resolution, 2-3 color grayscale. "
                   "Visible scanlines or interlace pattern. Security camera aesthetic.",
    "portrait_style": "Pixel art mugshot, Papers Please style. ~64x80 pixel base resolution upscaled. "
                      "Front-facing, head and shoulders. Flat institutional gray background. "
                      "Limited palette (8-12 colors). Visible individual pixels. "
                      "No smooth gradients — use dithering. Distinctive features readable at low res.",
    "ai_prompt_prefix": "pixel art, Papers Please game style, low resolution pixelated, "
                        "limited color palette, 1970s Eastern European authoritarian state, "
                        "institutional, cold war era, muted desaturated tones, dithered shading, "
                        "no anti-aliasing, crisp pixel edges",
}

# ==============================================================================
# Evidence type → visual description mappings
# ==============================================================================

EVIDENCE_TYPE_STYLES = {
    "Document": {
        "card_style": "Pixel art document card. Tan/cream rectangle with dark pixel border. "
                      "Tiny pixel text lines suggesting content. Official header pixels at top. "
                      "Small red pixel stamp mark. Dog-eared corner (2-3 pixels folded).",
        "full_style": "Full-page pixel art bureaucratic document. Pixel font text on tan background. "
                      "State header in dark pixel capitals. Form grid with 1px lines. "
                      "Dithered red rubber stamp. Papers Please-style readable pixel text.",
    },
    "Photo": {
        "card_style": "Pixel art polaroid. White pixel border, grainy 2-3 color interior. "
                      "Surveillance camera aesthetic. Visible scanlines.",
        "full_style": "Full pixel art photograph. Very low resolution look even at full size. "
                      "2-4 grayscale tones, dithered shadows. Evidence tag pixel label in corner.",
    },
    "Item": {
        "card_style": "Pixel art evidence bag. Clear plastic look (light gray dithered). "
                      "Item visible inside. Bureau pixel label attached.",
        "full_style": "Pixel art close-up of physical item on dark surface. "
                      "Pixel ruler for scale. Evidence tag with pixel text.",
    },
    "Disc": {
        "card_style": "Pixel art disc in plastic sleeve. Circular pixel shape with "
                      "light reflection (few white pixels). Bureau label.",
        "full_style": "Pixel art computer screen. Green-on-black CRT terminal. "
                      "Chunky pixel GUI, retro OS aesthetic. Scanline effect.",
    },
}

FOREIGN_SUBSTANCE_PROMPTS = {
    "None": "",
    "Ink": "with ink smudges, traces of printing ink visible under examination",
    "Paint": "with paint residue, dried paint flecks on surface",
    "Chemical": "with chemical residue marks, slight discoloration from chemical contact",
    "Blood": "with dark reddish-brown stains, dried biological material",
    "Soil": "with earth/dirt traces, soil particles embedded in surface",
    "Gunpowder": "with dark powder residue, burn marks, chemical smell implied",
    "Adhesive": "with sticky residue marks, tape remnants",
    "Food": "with food stains, grease marks",
    "Cosmetic": "with cosmetic residue, fragrant oils, soap film",
    "Industrial": "with industrial chemical residue, machine oil traces",
    "Pharmaceutical": "with pharmaceutical compound traces, pill dust, medical-grade chemical residue",
}


# ==============================================================================
# Speech class → portrait clothing/style mappings
# ==============================================================================

def infer_class_from_occupation(occupation: str) -> str:
    """Infer social class from occupation for portrait styling."""
    occupation_lower = occupation.lower()
    elite_keywords = ["councilor", "council", "director", "colonel", "captain", "aide"]
    professional_keywords = ["analyst", "clerk", "accountant", "inspector", "supervisor",
                             "administrator", "physician", "doctor", "auditor", "secretary",
                             "lieutenant", "sergeant", "nurse", "teacher", "librarian",
                             "chemist", "pharmacist"]
    worker_keywords = ["worker", "guard", "janitor", "foreman", "driver", "courier",
                       "vendor", "orderly", "mechanic", "repairman", "maintenance",
                       "factory", "brickyard", "cannery", "laborer"]

    for kw in elite_keywords:
        if kw in occupation_lower:
            return "elite"
    for kw in professional_keywords:
        if kw in occupation_lower:
            return "professional"
    for kw in worker_keywords:
        if kw in occupation_lower:
            return "worker"
    return "worker"  # default


CLASS_PORTRAIT_STYLES = {
    "elite": "Pixel art portrait. Dark suit or military uniform pixels. "
             "Precise pixel grooming details. Stern, angular face. "
             "Darker color palette suggesting wealth.",
    "professional": "Pixel art portrait. Button-up shirt pixels, possibly tie. "
                    "Clean, neat pixel arrangement. Bureau-standard look. "
                    "Medium-tone clothing palette.",
    "worker": "Pixel art portrait. Coveralls or simple shirt pixels. "
              "Rougher pixel features, weathered face. "
              "Warm brown and gray clothing tones. Slightly worn look.",
}


# ==============================================================================
# Manifest generation
# ==============================================================================

def generate_manifest(case: dict) -> dict:
    """Generate a complete art manifest from a case JSON."""
    case_id = case.get("caseID", "unknown")
    title = case.get("title", "Untitled")

    manifest = {
        "caseID": case_id,
        "title": title,
        "styleGuide": STYLE_GUIDE,
        "assets": {
            "caseCard": generate_case_card_entry(case),
            "portraits": [],
            "evidenceCards": [],
            "toolAssets": [],
        },
        "summary": {
            "totalAssets": 0,
            "portraits": 0,
            "evidenceSmallCards": 0,
            "evidenceFullPages": 0,
            "toolOverlays": 0,
        },
    }

    # Portraits
    for suspect in case.get("suspects", []):
        portrait = generate_portrait_entry(suspect, case_id)
        manifest["assets"]["portraits"].append(portrait)

    # Evidence cards (each evidence needs a small card + full page view)
    for ev in case.get("evidences", []) + case.get("extraEvidences", []):
        card_entry = generate_evidence_entry(ev, case_id)
        manifest["assets"]["evidenceCards"].append(card_entry)

        # Check if this evidence needs tool-specific assets
        tool_assets = generate_tool_assets(ev, case_id)
        manifest["assets"]["toolAssets"].extend(tool_assets)

    # Summary counts
    manifest["summary"]["portraits"] = len(manifest["assets"]["portraits"])
    manifest["summary"]["evidenceSmallCards"] = len(manifest["assets"]["evidenceCards"])
    manifest["summary"]["evidenceFullPages"] = len(manifest["assets"]["evidenceCards"])
    manifest["summary"]["toolOverlays"] = len(manifest["assets"]["toolAssets"])
    manifest["summary"]["totalAssets"] = (
        1  # case card
        + manifest["summary"]["portraits"]
        + manifest["summary"]["evidenceSmallCards"]
        + manifest["summary"]["evidenceFullPages"]
        + manifest["summary"]["toolOverlays"]
    )

    return manifest


def generate_case_card_entry(case: dict) -> dict:
    """Generate the case card art spec."""
    case_id = case.get("caseID", "unknown")
    title = case.get("title", "Untitled")
    description = case.get("description", "")

    return {
        "assetPath": f"Cases/{case_id}/card",
        "assetType": "CaseCard",
        "title": title,
        "dimensions": "400x560 (card ratio, pixel art upscaled from ~100x140 base)",
        "description": f"Case card for '{title}'. Pixel art case file folder. "
                       f"Dark moody desk background. Bureau aesthetic.",
        "textOnCard": title.upper(),
        "visualNotes": f"Based on: {description[:120]}",
        "aiPrompt": (
            f"{STYLE_GUIDE['ai_prompt_prefix']}, "
            f"case file folder, pixel art manila envelope, "
            f"'{title.upper()}' in pixel font on front, "
            f"pixel art evidence tag, red 'ACTIVE' pixel stamp, "
            f"dark desk background, top-down view"
        ),
    }


def generate_portrait_entry(suspect: dict, case_id: str) -> dict:
    """Generate a portrait art spec for a suspect."""
    first = suspect.get("firstName", "Unknown")
    last = suspect.get("lastName", "Unknown")
    full_name = f"{first} {last}"
    citizen_id = suspect.get("citizenID", "unknown")
    occupation = suspect.get("occupation", "Unknown")
    gender = suspect.get("gender", "Male")
    guilty = suspect.get("isGuilty", False)
    dob = suspect.get("dateOfBirth", "01/01/1980")
    social_class = infer_class_from_occupation(occupation)

    # Estimate age from DOB (game year ~2047 in Drazhovia calendar, but let's use
    # the year portion for relative aging)
    try:
        birth_year = int(dob.split("/")[-1])
        approx_age = 2047 - birth_year
    except (ValueError, IndexError):
        approx_age = 35

    gender_word = "man" if gender == "Male" else "woman"
    clothing = CLASS_PORTRAIT_STYLES.get(social_class, CLASS_PORTRAIT_STYLES["worker"])

    # Mood from guilt and nervousness
    nervousness = suspect.get("nervousnessLevel", 0.3)
    if guilty and nervousness > 0.5:
        mood = "nervous, guarded, avoiding eye contact"
    elif guilty:
        mood = "composed but tense, controlled expression"
    elif nervousness > 0.5:
        mood = "anxious, confused, slightly frightened"
    else:
        mood = "calm, cooperative, steady gaze"

    return {
        "assetPath": suspect.get("picturePath", f"Portraits/{citizen_id}"),
        "assetType": "Portrait",
        "characterName": full_name,
        "citizenID": citizen_id,
        "dimensions": "256x320 (pixel art upscaled from ~64x80 base)",
        "description": (
            f"{gender.capitalize()}, approximately {approx_age} years old. "
            f"Occupation: {occupation}. Social class: {social_class}. "
            f"{'GUILTY' if guilty else 'INNOCENT'}. {clothing}"
        ),
        "mood": mood,
        "visualNotes": (
            f"Pixel art portrait, Papers Please style. {clothing} "
            f"Flat gray pixel background. Distinctive pixel features."
        ),
        "aiPrompt": (
            f"{STYLE_GUIDE['ai_prompt_prefix']}, "
            f"pixel art ID card mugshot portrait, {gender_word}, approximately {approx_age} years old, "
            f"Eastern European, {occupation.lower()}, "
            f"{clothing.lower()}, "
            f"{mood}, "
            f"flat gray pixel background, "
            f"head and shoulders crop, front-facing, no smile, "
            f"Papers Please character style, visible pixels, limited palette"
        ),
    }


def generate_evidence_entry(ev: dict, case_id: str) -> dict:
    """Generate evidence card art specs (both small card and full page)."""
    ev_id = ev.get("id", "unknown")
    ev_type = ev.get("type", "Document")
    title = ev.get("title", "Untitled Evidence")
    description = ev.get("description", "")
    substance = ev.get("foreignSubstance", "None")
    hotspots = ev.get("hotspots", [])

    type_style = EVIDENCE_TYPE_STYLES.get(ev_type, EVIDENCE_TYPE_STYLES["Document"])
    substance_note = FOREIGN_SUBSTANCE_PROMPTS.get(substance, "")

    # Build hotspot descriptions for the artist
    hotspot_specs = []
    for hs in hotspots:
        # Clean markup tags from noteText for art description
        note = hs.get("noteText", "")
        note = note.replace("<person>", "").replace("</person>", "")
        note = note.replace("<location>", "").replace("</location>", "")
        note = note.replace("<item>", "").replace("</item>", "")

        hotspot_specs.append({
            "clueId": hs.get("clueId", ""),
            "description": note,
            "page": hs.get("pageIndex", 0),
            "region": f"x:{hs.get('positionX', 0.5):.0%} y:{hs.get('positionY', 0.5):.0%} "
                      f"w:{hs.get('width', 0.3):.0%} h:{hs.get('height', 0.1):.0%}",
            "interactionNote": "Player clicks this region to discover the clue.",
        })

    # Determine what text should appear on the document
    text_content = build_evidence_text_content(ev, ev_type)

    return {
        "assetPath": ev.get("cardImagePath", f"Evidence/{case_id}/{ev_id}_card"),
        "assetType": "EvidenceCard",
        "evidenceID": ev_id,
        "evidenceType": ev_type,
        "title": title,
        "foreignSubstance": substance if substance != "None" else None,
        "dimensions": {
            "smallCard": "400x560 (card ratio, for hand view)",
            "fullPage": "800x1120 (expanded, for mat/inspection view)",
        },
        "description": description,
        "smallCardVisual": type_style["card_style"],
        "fullPageVisual": type_style["full_style"],
        "textContent": text_content,
        "hotspots": hotspot_specs,
        "substanceNote": substance_note if substance_note else None,
        "aiPrompt": {
            "smallCard": (
                f"{STYLE_GUIDE['ai_prompt_prefix']}, "
                f"pixel art evidence card, {ev_type.lower()}, "
                f"'{title}', "
                f"{type_style['card_style'].lower()}, "
                f"{'pixel ' + substance.lower() + ' residue marks, ' if substance != 'None' else ''}"
                f"dark desk background, top-down view"
            ),
            "fullPage": (
                f"{STYLE_GUIDE['ai_prompt_prefix']}, "
                f"pixel art {ev_type.lower()}, full page view, "
                f"'{title}', "
                f"{type_style['full_style'].lower()}, "
                f"{'with pixel ' + substance_note + ', ' if substance_note else ''}"
                f"readable pixel font text, detailed pixel art"
            ),
        },
    }


def build_evidence_text_content(ev: dict, ev_type: str) -> dict:
    """Build the actual text content that should appear on the evidence."""
    title = ev.get("title", "")
    description = ev.get("description", "")

    content = {
        "header": f"REPUBLIC OF DRAZHOVIA — {title.upper()}",
        "body": description,
        "footer": "THE PATTERN PROVIDES — Form [auto-assign]",
    }

    # For documents, extract clue-relevant text from hotspots
    if ev_type == "Document":
        key_text = []
        for hs in ev.get("hotspots", []):
            note = hs.get("noteText", "")
            note = note.replace("<person>", "").replace("</person>", "")
            note = note.replace("<location>", "").replace("</location>", "")
            note = note.replace("<item>", "").replace("</item>", "")
            key_text.append(note)
        content["keyPassages"] = key_text

    return content


def generate_tool_assets(ev: dict, case_id: str) -> list:
    """Generate tool-specific overlay assets if needed."""
    assets = []
    ev_id = ev.get("id", "unknown")
    substance = ev.get("foreignSubstance", "None")
    app_id = ev.get("associatedAppId")

    # Spectrograph reading (if foreignSubstance is not None)
    if substance and substance != "None":
        assets.append({
            "assetPath": f"Evidence/{case_id}/{ev_id}_spectrum",
            "assetType": "SpectrographReading",
            "title": f"Spectrograph: {ev.get('title', '')}",
            "dimensions": "600x200 (pixel art spectrum bar)",
            "description": (
                f"Pixel art ROYGBIV spectrum analysis showing {substance} compound. "
                f"Sharp pixel peaks at characteristic wavelengths."
            ),
            "visualNotes": (
                "Pixel art horizontal spectrum bar (red to violet left-to-right). "
                "Sharp pixel peaks on compound-specific wavelengths. "
                "Green pixel font readout overlay. "
                "Green-on-black CRT aesthetic, pixel scanlines."
            ),
            "substance": substance,
            "aiPrompt": (
                "pixel art, Papers Please style, scientific spectrograph reading, "
                f"ROYGBIV spectrum, {substance.lower()} compound detected, "
                "sharp pixel peaks, green phosphor CRT pixel display, "
                "retro pixel instrument, dark background, "
                "pixel font digital readout, limited color palette"
            ),
        })

    # Disc app screen (if associatedAppId exists)
    if app_id:
        assets.append({
            "assetPath": f"Evidence/{case_id}/{ev_id}_app_screen",
            "assetType": "ComputerAppScreen",
            "title": f"Computer: {ev.get('title', '')}",
            "dimensions": "800x600 (pixel art monitor ratio)",
            "description": f"Pixel art computer application screen for {app_id}.",
            "visualNotes": (
                "Pixel art early GUI — chunky pixel window borders, very limited palette. "
                "Drazhovia state computer system. Papers Please monitor aesthetic."
            ),
            "appId": app_id,
            "aiPrompt": (
                "pixel art, Papers Please style, retro computer screen, "
                f"early GUI, chunky pixel window borders, "
                f"{app_id.replace('_', ' ')} application, "
                "very limited color palette, CRT pixel glow, "
                "Eastern European government computer, pixel font text"
            ),
        })

    return assets


# ==============================================================================
# Main
# ==============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Generate art manifests for Marlo case files"
    )
    parser.add_argument(
        "files",
        nargs="*",
        help="Case JSON file(s) to generate manifests for",
    )
    parser.add_argument(
        "--all", "-a",
        action="store_true",
        help="Generate manifests for all cases",
    )
    parser.add_argument(
        "--output", "-o",
        help="Output path (single file only)",
    )
    parser.add_argument(
        "--prompts-only",
        action="store_true",
        help="Output only AI prompts (for batch feeding to art generators)",
    )

    args = parser.parse_args()

    if not args.files and not args.all:
        parser.print_help()
        sys.exit(1)

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    files = []
    if args.all:
        if CASES_DIR.exists():
            files = sorted(CASES_DIR.glob("*.json"))
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

        # Skip placeholders
        if not case.get("suspects"):
            print(f"Skipping {case.get('caseID', 'unknown')} — no suspects (placeholder)")
            continue

        manifest = generate_manifest(case)

        if args.prompts_only:
            output = extract_prompts_only(manifest)
            output_path = args.output or str(OUTPUT_DIR / f"{case['caseID']}_prompts.txt")
            Path(output_path).write_text(output, encoding="utf-8")
        else:
            output_path = args.output or str(OUTPUT_DIR / f"{case['caseID']}_art_manifest.json")
            with open(output_path, "w", encoding="utf-8") as f:
                json.dump(manifest, f, indent=2, ensure_ascii=False)

        print(f"Generated: {output_path}")
        print(f"  Assets needed: {manifest['summary']['totalAssets']} total")
        print(f"    Portraits: {manifest['summary']['portraits']}")
        print(f"    Evidence cards: {manifest['summary']['evidenceSmallCards']} "
              f"(x2 for small+full = {manifest['summary']['evidenceSmallCards'] * 2})")
        print(f"    Tool overlays: {manifest['summary']['toolOverlays']}")
        generated += 1

    print(f"\nGenerated {generated} manifest(s)")


def extract_prompts_only(manifest: dict) -> str:
    """Extract just the AI prompts into a text file for batch processing."""
    lines = [f"# Art Prompts for: {manifest['title']} ({manifest['caseID']})\n"]

    # Case card
    cc = manifest["assets"]["caseCard"]
    lines.append(f"## Case Card: {cc['title']}")
    lines.append(f"Path: {cc['assetPath']}")
    lines.append(f"Prompt: {cc['aiPrompt']}\n")

    # Portraits
    for p in manifest["assets"]["portraits"]:
        lines.append(f"## Portrait: {p['characterName']}")
        lines.append(f"Path: {p['assetPath']}")
        lines.append(f"Description: {p['description']}")
        lines.append(f"Prompt: {p['aiPrompt']}\n")

    # Evidence
    for ev in manifest["assets"]["evidenceCards"]:
        lines.append(f"## Evidence: {ev['title']} [{ev['evidenceType']}]")
        lines.append(f"Path: {ev['assetPath']}")
        lines.append(f"Description: {ev['description']}")
        lines.append(f"Small Card Prompt: {ev['aiPrompt']['smallCard']}")
        lines.append(f"Full Page Prompt: {ev['aiPrompt']['fullPage']}\n")

    # Tool assets
    for ta in manifest["assets"]["toolAssets"]:
        lines.append(f"## Tool Asset: {ta['title']}")
        lines.append(f"Path: {ta['assetPath']}")
        lines.append(f"Type: {ta['assetType']}")
        lines.append(f"Description: {ta['description']}")
        lines.append(f"Prompt: {ta['aiPrompt']}\n")

    return "\n".join(lines)


if __name__ == "__main__":
    main()
