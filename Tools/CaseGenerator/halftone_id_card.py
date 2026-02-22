#!/usr/bin/env python3
"""
Worker ID Card Generator — Miroslav Zelnik.

Generates a Drazhovia Worker Identification Card with:
- Small card format (400x250)
- worker-orange stripe at top (Industrial sector)
- "REPUBLIC OF DRAZHOVIA — WORKER IDENTIFICATION" header
- Photo placeholder (gray halftone rectangle)
- Personal data fields
- stamp-dark "ACTIVE" stamp
- "THE PATTERN PROVIDES" footer micro-text
- Worn paper texture, slightly yellowed

Usage:
    python halftone_id_card.py
    python halftone_id_card.py --no-halftone
    python halftone_id_card.py -o id_card.png
"""

import argparse
import random
from pathlib import Path

from PIL import Image, ImageDraw

from halftone_common import (
    PALETTE, OUTPUT_DIR,
    load_font, make_yellowed_paper, draw_pattern_emblem,
    apply_halftone, apply_halftone_tint, add_grain_overlay,
)

CARD_W = 400
CARD_H = 250


def generate_id_card(dot_spacing=5, skip_halftone=False):
    """Generate Miroslav Zelnik's Worker ID card."""

    # --- Background: yellowed, worn paper ---
    img = make_yellowed_paper(CARD_W, CARD_H, amount=1)
    draw = ImageDraw.Draw(img)

    ink = PALETTE["ink"]
    ink_mid = PALETTE["ink_mid"]
    ink_light = PALETTE["ink_light"]
    orange = PALETTE["worker_orange"]
    stamp = PALETTE["stamp_dark"]

    # --- Card border ---
    draw.rectangle([1, 1, CARD_W - 2, CARD_H - 2], outline=ink_mid, width=2)

    # --- Occupation color stripe (top) ---
    stripe_h = 28
    draw.rectangle([2, 2, CARD_W - 3, stripe_h], fill=orange)

    # Header text on stripe
    header_font = load_font("sans_bold", 11)
    header = "REPUBLIC OF DRAZHOVIA — WORKER IDENTIFICATION"
    bbox = header_font.getbbox(header)
    tw = bbox[2] - bbox[0]
    draw.text(((CARD_W - tw) // 2, 7), header, fill=PALETTE["paper"],
              font=header_font)

    # --- Pattern emblem (small, left of stripe) ---
    draw_pattern_emblem(draw, 18, 15, 8, PALETTE["paper"])

    # --- Photo placeholder (left side) ---
    photo_x = 15
    photo_y = stripe_h + 12
    photo_w = 80
    photo_h = 100

    # Gray halftone rectangle simulating a photo
    for py in range(photo_y, photo_y + photo_h):
        for px in range(photo_x, photo_x + photo_w):
            # Coarse halftone pattern — worker quality printing
            cell = 5
            gx = (px - photo_x) // cell
            gy = (py - photo_y) // cell
            # Simulate a face silhouette (darker center, lighter edges)
            cx_rel = (px - photo_x - photo_w // 2) / (photo_w // 2)
            cy_rel = (py - photo_y - photo_h * 0.4) / (photo_h * 0.4)
            dist = (cx_rel ** 2 + cy_rel ** 2) ** 0.5
            base_gray = 100 + int(60 * min(dist, 1.0))
            # Halftone quantization
            if (gx + gy) % 2 == 0:
                base_gray += 15
            noise = random.randint(-10, 10)
            v = max(60, min(200, base_gray + noise))
            img.putpixel((px, py), (v, v - 3, v - 6))

    draw.rectangle([photo_x, photo_y, photo_x + photo_w, photo_y + photo_h],
                    outline=ink_mid, width=1)

    # --- Personal data fields (right side) ---
    fields_x = photo_x + photo_w + 18
    field_y = stripe_h + 14

    label_font = load_font("sans", 10)
    value_font = load_font("sans_bold", 12)

    fields = [
        ("NAME:", "ZELNIK, Miroslav"),
        ("BLOCK:", "D"),
        ("OCCUPATION:", "Night Guard"),
        ("SECTOR:", "Industrial / Technical"),
        ("BADGE NO.:", "M-1187"),
        ("RATION CARD:", "RC-44-D-1187"),
    ]

    for label, value in fields:
        draw.text((fields_x, field_y), label, fill=ink_light, font=label_font)
        field_y += 13
        draw.text((fields_x, field_y), value, fill=ink, font=value_font)
        field_y += 18

        # Underline for form feel
        draw.line([(fields_x, field_y - 3),
                    (CARD_W - 20, field_y - 3)], fill=ink_light, width=1)

    # --- "ACTIVE" stamp (lower right, slightly rotated feel via misalignment) ---
    active_font = load_font("sans_bold", 18)
    active_text = "ACTIVE"
    bbox = active_font.getbbox(active_text)
    atw = bbox[2] - bbox[0]

    # Stamp box
    sx = CARD_W - atw - 30
    sy = CARD_H - 50
    padding = 6
    draw.rectangle([sx - padding, sy - padding, sx + atw + padding,
                     sy + 22 + padding], outline=stamp, width=2)
    draw.text((sx + 1, sy), active_text, fill=stamp, font=active_font)

    # --- Footer micro-text ---
    footer_font = load_font("sans", 7)
    footer = "THE PATTERN PROVIDES"
    bbox = footer_font.getbbox(footer)
    ftw = bbox[2] - bbox[0]
    draw.text(((CARD_W - ftw) // 2, CARD_H - 14), footer, fill=ink_light,
              font=footer_font)

    # --- Halftone overlay (coarse — worker quality) ---
    # For small cards, blend halftone with original to preserve readability.
    # Full replacement works at 800x1120 but destroys text at 400x250.
    if not skip_halftone:
        halftoned = apply_halftone(img, dot_spacing=dot_spacing,
                                    dot_color=ink,
                                    bg_color=PALETTE["paper"])
        img = Image.blend(img, halftoned, 0.45)  # 45% halftone, 55% original
        # Orange tint pass
        img = apply_halftone_tint(img, dot_spacing=dot_spacing + 3,
                                   tint_color=orange, intensity=0.10, angle=18)

    # --- Grain (moderate — cheap printing) ---
    img = add_grain_overlay(img, amount=10)

    return img


def main():
    parser = argparse.ArgumentParser(
        description="Generate halftone Worker ID card"
    )
    parser.add_argument("-o", "--output", help="Output file path")
    parser.add_argument("--dot-spacing", type=int, default=5,
                        help="Halftone dot spacing (default: 5, coarse worker quality)")
    parser.add_argument("--no-halftone", action="store_true",
                        help="Skip halftone overlay")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    img = generate_id_card(
        dot_spacing=args.dot_spacing,
        skip_halftone=args.no_halftone,
    )

    out_path = args.output or str(OUTPUT_DIR / "id_card_zelnik_halftone.png")
    img.save(out_path)
    print(f"Generated: {out_path}  ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
