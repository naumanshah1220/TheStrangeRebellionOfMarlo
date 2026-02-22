#!/usr/bin/env python3
"""
Overseer Letter Generator — Terzic's Day 4 Commendation.

Generates a high-quality Bureau letterhead document with:
- Smooth paper background (high quality stock)
- Bureau letterhead in bureau-brown + Pattern emblem
- Typewriter-style monospace body text
- Cursive signature "V. Terzic" (subtle class tell)
- Faint "SILENCE IS ORDER" watermark
- Halftone overlay (fine — high quality printing)

Usage:
    python halftone_overseer.py
    python halftone_overseer.py --no-halftone
    python halftone_overseer.py -o overseer.png
"""

import argparse
import random
from pathlib import Path

from PIL import Image, ImageDraw

from halftone_common import (
    PALETTE, OUTPUT_DIR,
    load_font, make_paper_texture,
    draw_bureau_header, draw_pattern_emblem, draw_footer,
    apply_halftone, add_grain_overlay,
)

LETTER_W = 600
LETTER_H = 800

# Letter content — typewriter style, personal but institutional
BODY_LINES = [
    "DATE:  Day 4, Year 47",
    "TO:    Analyst Dashev, M.",
    "FROM:  Overseer Terzic, V.",
    "RE:    Performance Commendation",
    "",
    "---",
    "",
    "Four for four, Analyst Dashev.",
    "",
    "Your clearance rate this week has not gone",
    "unnoticed. The Bureau values precision, and",
    "you have delivered it consistently.",
    "",
    "I have noted your handling of the Depot case",
    "with particular interest. The access log",
    "analysis was thorough. The indictment was",
    "clean. No loose threads.",
    "",
    "Keep this standard. There are opportunities",
    "ahead for analysts who demonstrate this kind",
    "of reliable pattern recognition.",
    "",
    "I trust you understand what reliability means",
    "in our line of work.",
    "",
    "",
    "",
]


def generate_overseer_letter(dot_spacing=14, skip_halftone=False):
    """Generate Terzic's Day 4 commendation letter."""

    # --- Background: high quality paper (smooth, warm) ---
    # Slightly warmer than standard paper — implies quality stock
    quality_paper = (244, 236, 218)
    img = make_paper_texture(LETTER_W, LETTER_H, quality_paper, grain_amount=4,
                              fiber_density=0)  # smooth — no fibers
    draw = ImageDraw.Draw(img)

    ink = PALETTE["ink"]
    ink_mid = PALETTE["ink_mid"]
    ink_light = PALETTE["ink_light"]
    brown = PALETTE["bureau_brown"]

    # --- Faint watermark: "SILENCE IS ORDER" ---
    # Drawn first so it's behind everything
    watermark_font = load_font("serif_bold", 60)
    watermark_text = "SILENCE IS ORDER"
    bbox = watermark_font.getbbox(watermark_text)
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]

    # Very faint — barely visible
    watermark_color = (
        quality_paper[0] - 8,
        quality_paper[1] - 8,
        quality_paper[2] - 6,
    )
    draw.text(((LETTER_W - tw) // 2, LETTER_H // 2 - th // 2),
              watermark_text, fill=watermark_color, font=watermark_font)

    # --- Bureau letterhead ---
    y = draw_bureau_header(draw, img, LETTER_W, 30, font_size=16)

    # Pattern emblem in header
    draw_pattern_emblem(draw, LETTER_W // 2, y + 18, 14, brown)
    y += 40

    # Sub-header
    sub_font = load_font("serif", 10)
    sub_text = "OFFICE OF THE OVERSEER  —  INTERNAL CORRESPONDENCE"
    bbox = sub_font.getbbox(sub_text)
    tw = bbox[2] - bbox[0]
    draw.text(((LETTER_W - tw) // 2, y), sub_text, fill=ink_light, font=sub_font)
    y += 20

    draw.line([(40, y), (LETTER_W - 40, y)], fill=ink_light, width=1)
    y += 20

    # --- Body text (typewriter monospace) ---
    mono_font = load_font("mono", 14)
    line_h = 22

    for line in BODY_LINES:
        if line == "---":
            # Horizontal rule
            draw.line([(50, y + 8), (LETTER_W - 50, y + 8)], fill=ink_light,
                      width=1)
            y += line_h
            continue

        # Slight typewriter imperfection — occasional darker/lighter chars
        # simulated by drawing the whole line with slight position jitter
        x_jitter = random.randint(-1, 1)

        # Typewriter ink: not perfectly black, warm dark
        type_color = (
            ink[0] + random.randint(0, 15),
            ink[1] + random.randint(0, 15),
            ink[2] + random.randint(0, 12),
        )

        draw.text((50 + x_jitter, y), line, fill=type_color, font=mono_font)
        y += line_h

    # --- Signature: "V. Terzic" in CURSIVE ---
    # This is the subtle class tell — the only cursive on a Bureau document.
    # Terzic writes in script because he IS Council, not Bureau.
    sig_font = load_font("cursive", 28)
    signature = "V. Terzic"

    # Signature color: rich dark ink (quality pen, not typewriter)
    sig_color = (30, 25, 20)
    draw.text((350, y), signature, fill=sig_color, font=sig_font)
    y += 40

    # Title under signature
    title_font = load_font("mono", 10)
    draw.text((350, y), "OVERSEER, BUREAU OF", fill=ink_mid, font=title_font)
    y += 14
    draw.text((350, y), "PATTERN COMPLIANCE", fill=ink_mid, font=title_font)

    # --- Footer ---
    draw_footer(draw, img, LETTER_W, LETTER_H, color=ink_light)

    # --- Halftone overlay (fine — this is high quality printing) ---
    if not skip_halftone:
        img = apply_halftone(img, dot_spacing=dot_spacing,
                              dot_color=ink, bg_color=quality_paper)

    # --- Very light grain (quality paper) ---
    img = add_grain_overlay(img, amount=3)

    return img


def main():
    parser = argparse.ArgumentParser(
        description="Generate halftone Overseer letter"
    )
    parser.add_argument("-o", "--output", help="Output file path")
    parser.add_argument("--dot-spacing", type=int, default=14,
                        help="Halftone dot spacing (default: 14, fine)")
    parser.add_argument("--no-halftone", action="store_true",
                        help="Skip halftone overlay")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    img = generate_overseer_letter(
        dot_spacing=args.dot_spacing,
        skip_halftone=args.no_halftone,
    )

    out_path = args.output or str(OUTPUT_DIR / "letter_overseer_day4.png")
    img.save(out_path)
    print(f"Generated: {out_path}  ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
