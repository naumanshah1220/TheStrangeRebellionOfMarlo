#!/usr/bin/env python3
"""
Halftone Newspaper Generator — THE PATTERN TIMES.

Generates a Day 2 newspaper with:
- newspaper-yellow (#D4C878) background with newsprint texture
- Bold condensed masthead
- Column layout with halftone body text
- Halftone photograph placeholder
- Propaganda sidebar
- Fold crease effect

Usage:
    python halftone_newspaper.py
    python halftone_newspaper.py --no-halftone
    python halftone_newspaper.py -o newspaper.png
"""

import argparse
import random
import textwrap
from pathlib import Path

from PIL import Image, ImageDraw

from halftone_common import (
    PALETTE, OUTPUT_DIR,
    load_font, make_paper_texture,
    draw_pattern_emblem, apply_halftone, apply_halftone_tint,
    add_grain_overlay, add_fold_crease,
)

NEWS_W = 600
NEWS_H = 800


# Filler body text — regime propaganda style
BODY_TEXT = (
    "Bureau officials confirmed yesterday that the suspect, identified as "
    "Miroslav Zelnik, a night security guard assigned to the Block C Central "
    "Ration Depot, has confessed to the theft of regulated supplies. "
    "According to Bureau spokesperson Henrik Petrov, the Pattern Analysis "
    "conducted by the Bureau of Pattern Compliance was instrumental in "
    "identifying the irregularities in the depot access log that led to "
    "Zelnik's apprehension. "
    "\"The Pattern reveals all deviation,\" Petrov stated during the press "
    "briefing. \"Citizens should rest assured that the Bureau maintains "
    "constant vigilance over the supply chain.\" "
    "Zelnik, a resident of Block D, had served as night guard for the "
    "depot for three years. Records indicate unauthorized access at 02:17 "
    "with no corresponding exit time logged. The missing supplies — "
    "consisting of grain rations allocated for Block F distribution — "
    "were recovered from a secondary location. "
    "The Council has commended the Bureau's swift resolution. Zelnik "
    "faces relocation to Block G pending tribunal review."
)


def generate_newspaper(dot_spacing=10, skip_halftone=False):
    """Generate THE PATTERN TIMES newspaper."""

    # --- Background: newsprint yellow with grain ---
    img = make_paper_texture(NEWS_W, NEWS_H, PALETTE["newspaper_yellow"],
                              grain_amount=10, fiber_density=2)
    draw = ImageDraw.Draw(img)

    ink = PALETTE["ink"]
    ink_mid = PALETTE["ink_mid"]
    ink_light = PALETTE["ink_light"]

    y = 15

    # --- Top rule ---
    draw.line([(15, y), (NEWS_W - 15, y)], fill=ink, width=3)
    y += 8

    # --- Date line ---
    date_font = load_font("serif", 10)
    draw.text((20, y), "VOL. XLVII  No. 287", fill=ink_mid, font=date_font)
    date_text = "Day 2 — Republic of Drazhovia — For the Collective"
    bbox = date_font.getbbox(date_text)
    tw = bbox[2] - bbox[0]
    draw.text((NEWS_W - 20 - tw, y), date_text, fill=ink_mid, font=date_font)
    y += 18

    # --- Masthead ---
    draw.line([(15, y), (NEWS_W - 15, y)], fill=ink, width=1)
    y += 4

    mast_font = load_font("serif_bold", 48)
    masthead = "THE PATTERN TIMES"
    bbox = mast_font.getbbox(masthead)
    tw = bbox[2] - bbox[0]
    draw.text(((NEWS_W - tw) // 2, y), masthead, fill=ink, font=mast_font)
    y += 56

    # Pattern emblem flanking the masthead
    draw_pattern_emblem(draw, 45, y - 28, 14, ink_mid)
    draw_pattern_emblem(draw, NEWS_W - 45, y - 28, 14, ink_mid)

    # --- Below masthead rule ---
    draw.line([(15, y), (NEWS_W - 15, y)], fill=ink, width=2)
    y += 5
    draw.line([(15, y), (NEWS_W - 15, y)], fill=ink, width=1)
    y += 10

    # --- Main headline ---
    headline_font = load_font("serif_bold", 32)
    headline = "RATION DEPOT THIEF"
    bbox = headline_font.getbbox(headline)
    tw = bbox[2] - bbox[0]
    draw.text(((NEWS_W - tw) // 2, y), headline, fill=ink, font=headline_font)
    y += 38

    headline2 = "APPREHENDED"
    bbox = headline_font.getbbox(headline2)
    tw = bbox[2] - bbox[0]
    draw.text(((NEWS_W - tw) // 2, y), headline2, fill=ink, font=headline_font)
    y += 42

    # --- Subheadline ---
    sub_font = load_font("serif", 14)
    sub = "Bureau Analysis Praised — Security Guard Zelnik Confesses"
    bbox = sub_font.getbbox(sub)
    tw = bbox[2] - bbox[0]
    draw.text(((NEWS_W - tw) // 2, y), sub, fill=ink_mid, font=sub_font)
    y += 22

    # --- Rule below headline ---
    draw.line([(15, y), (NEWS_W - 15, y)], fill=ink, width=1)
    y += 10

    # --- Photo placeholder (halftone rectangle) ---
    photo_x = 20
    photo_w = 260
    photo_h = 140
    photo_y = y

    # Gray halftone rectangle with dot pattern
    for py in range(photo_y, photo_y + photo_h):
        for px in range(photo_x, photo_x + photo_w):
            # Simulate a coarse halftone photo — checkerboard with noise
            cell = 6
            gx = (px - photo_x) // cell
            gy = (py - photo_y) // cell
            base_gray = 120 + int(40 * ((gx + gy) % 3 == 0))
            noise = random.randint(-20, 20)
            v = max(60, min(200, base_gray + noise))
            img.putpixel((px, py), (v, v - 5, v - 10))

    # Photo border
    draw.rectangle([photo_x, photo_y, photo_x + photo_w, photo_y + photo_h],
                    outline=ink, width=1)

    # Photo caption
    caption_font = load_font("serif", 9)
    draw.text((photo_x + 4, photo_y + photo_h + 3),
              "Block C Depot — file photograph", fill=ink_light,
              font=caption_font)

    # --- Body text (two columns) ---
    body_font = load_font("serif", 11)
    line_h = 15

    # Column 1: next to photo, then full width below
    col1_x = photo_x + photo_w + 15
    col1_w = NEWS_W - col1_x - 20
    col1_y = photo_y

    # Wrap text for narrow column next to photo
    chars_per_line_narrow = col1_w // 6  # approximate
    chars_per_line_wide = (NEWS_W - 55) // 2 // 6

    wrapped = textwrap.wrap(BODY_TEXT, width=chars_per_line_narrow)

    # Draw lines next to photo
    ty = col1_y
    line_idx = 0
    while ty < photo_y + photo_h + 15 and line_idx < len(wrapped):
        draw.text((col1_x, ty), wrapped[line_idx], fill=ink_mid, font=body_font)
        ty += line_h
        line_idx += 1

    # Continue in two columns below photo
    col_top = max(ty, photo_y + photo_h + 20)
    col_gap = 20
    col_w = (NEWS_W - 40 - col_gap) // 2

    remaining = " ".join(wrapped[line_idx:])
    full_wrapped = textwrap.wrap(remaining, width=chars_per_line_wide)

    # Split roughly in half for two columns
    half = len(full_wrapped) // 2

    # Left column
    ty = col_top
    for line in full_wrapped[:half]:
        if ty > NEWS_H - 80:
            break
        draw.text((20, ty), line, fill=ink_mid, font=body_font)
        ty += line_h

    # Column divider
    div_x = 20 + col_w + col_gap // 2
    draw.line([(div_x, col_top), (div_x, ty)], fill=ink_light, width=1)

    # Right column
    ty2 = col_top
    for line in full_wrapped[half:]:
        if ty2 > NEWS_H - 80:
            break
        draw.text((div_x + col_gap // 2 + 5, ty2), line, fill=ink_mid,
                  font=body_font)
        ty2 += line_h

    # --- Propaganda sidebar (bottom right) ---
    sidebar_y = max(ty, ty2) + 10
    sidebar_x = NEWS_W - 200
    sidebar_w = 180
    sidebar_h = 60

    draw.rectangle([sidebar_x, sidebar_y, sidebar_x + sidebar_w,
                     sidebar_y + sidebar_h], outline=ink, width=2)

    sidebar_font = load_font("sans_bold", 11)
    sidebar_title = "COUNCIL REMINDS:"
    bbox = sidebar_font.getbbox(sidebar_title)
    stw = bbox[2] - bbox[0]
    draw.text((sidebar_x + (sidebar_w - stw) // 2, sidebar_y + 8),
              sidebar_title, fill=ink, font=sidebar_font)

    motto_font = load_font("serif_bold", 16)
    motto = "SILENCE"
    bbox = motto_font.getbbox(motto)
    mtw = bbox[2] - bbox[0]
    draw.text((sidebar_x + (sidebar_w - mtw) // 2, sidebar_y + 22),
              motto, fill=ink, font=motto_font)
    motto2 = "IS ORDER"
    bbox = motto_font.getbbox(motto2)
    mtw = bbox[2] - bbox[0]
    draw.text((sidebar_x + (sidebar_w - mtw) // 2, sidebar_y + 40),
              motto2, fill=ink, font=motto_font)

    # --- Bottom rule ---
    draw.line([(15, NEWS_H - 20), (NEWS_W - 15, NEWS_H - 20)], fill=ink,
              width=2)

    # Price and edition
    price_font = load_font("serif", 9)
    draw.text((20, NEWS_H - 16), "PRICE: 2 MARKS", fill=ink_mid,
              font=price_font)
    draw.text((NEWS_W - 140, NEWS_H - 16), "THE PATTERN PROVIDES",
              fill=ink_mid, font=price_font)

    # --- Fold crease across middle ---
    img = add_fold_crease(img, y_pos=NEWS_H // 2)

    # --- Halftone overlay ---
    if not skip_halftone:
        img = apply_halftone(img, dot_spacing=dot_spacing,
                              dot_color=ink, bg_color=PALETTE["newspaper_yellow"])
        img = apply_halftone_tint(img, dot_spacing=dot_spacing + 6,
                                   tint_color=PALETTE["bureau_brown"],
                                   intensity=0.08, angle=12)

    # --- Final grain ---
    img = add_grain_overlay(img, amount=8)

    return img


def main():
    parser = argparse.ArgumentParser(
        description="Generate halftone newspaper — THE PATTERN TIMES"
    )
    parser.add_argument("-o", "--output", help="Output file path")
    parser.add_argument("--dot-spacing", type=int, default=10,
                        help="Halftone dot spacing (default: 10)")
    parser.add_argument("--no-halftone", action="store_true",
                        help="Skip halftone overlay")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    img = generate_newspaper(
        dot_spacing=args.dot_spacing,
        skip_halftone=args.no_halftone,
    )

    out_path = args.output or str(OUTPUT_DIR / "newspaper_day2_halftone.png")
    img.save(out_path)
    print(f"Generated: {out_path}  ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
