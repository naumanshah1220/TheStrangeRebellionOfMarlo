#!/usr/bin/env python3
"""
Halftone Bureau Evidence Document Generator.

Generates the Block C Depot Access Log (Form PA-7) in the halftone
duotone print style defined in the Art Style Bible.

Features:
- paper (#F0E6D0) textured background with subtle halftone grain
- "BUREAU OF PATTERN COMPLIANCE" header in bureau-brown serif
- Form number "Form PA-7 / CASE: BPC-2024-0001"
- Access log table with suspect highlight
- stamp-dark circular Bureau seal (slightly misaligned)
- "THE PATTERN PROVIDES" footer
- Halftone dot texture overlay

Usage:
    python halftone_doc.py                    # Generate with defaults
    python halftone_doc.py --no-halftone      # Skip halftone overlay (debug)
    python halftone_doc.py --dot-spacing 10   # Adjust halftone coarseness
    python halftone_doc.py -o myfile.png      # Custom output path
"""

import argparse
from pathlib import Path

from PIL import Image, ImageDraw

from halftone_common import (
    PALETTE, OUTPUT_DIR,
    load_font, make_paper_texture,
    draw_bureau_header, draw_stamp_circle, draw_footer, draw_pattern_emblem,
    apply_halftone_blend, add_grain_overlay,
)

# Document dimensions (source resolution)
DOC_W = 800
DOC_H = 1120


def generate_access_log(dot_spacing=12, skip_halftone=False):
    """Generate the Bureau Depot Access Log document."""

    # --- Background: aged paper with grain ---
    img = make_paper_texture(DOC_W, DOC_H, PALETTE["paper"], grain_amount=8,
                              fiber_density=1)
    draw = ImageDraw.Draw(img)

    ink = PALETTE["ink"]
    ink_mid = PALETTE["ink_mid"]
    ink_light = PALETTE["ink_light"]
    brown = PALETTE["bureau_brown"]
    stamp = PALETTE["stamp_dark"]

    # --- Outer border (single line, Bureau standard) ---
    draw.rectangle([15, 15, DOC_W - 16, DOC_H - 16], outline=brown, width=2)

    # --- Classification strip ---
    strip_text = "BUREAU USE ONLY"
    strip_font = load_font("sans_bold", 11)
    bbox = strip_font.getbbox(strip_text)
    tw = bbox[2] - bbox[0]
    strip_w = tw + 20
    strip_x = (DOC_W - strip_w) // 2
    draw.rectangle([strip_x, 8, strip_x + strip_w, 28], fill=brown)
    draw.text((strip_x + 10, 10), strip_text, fill=PALETTE["paper"],
              font=strip_font)

    # --- Bureau header ---
    y = draw_bureau_header(draw, img, DOC_W, 40, font_size=20)

    # --- Pattern emblem ---
    draw_pattern_emblem(draw, DOC_W // 2, y + 25, 18, brown)
    y += 55

    # --- State name ---
    state_font = load_font("serif", 14)
    for line in ["REPUBLIC OF DRAZHOVIA", "SECURITY DIVISION"]:
        bbox = state_font.getbbox(line)
        tw = bbox[2] - bbox[0]
        draw.text(((DOC_W - tw) // 2, y), line, fill=ink_mid, font=state_font)
        y += 20

    # --- Separator ---
    y += 5
    draw.line([(40, y), (DOC_W - 40, y)], fill=ink_light, width=1)
    y += 10

    # --- Document title ---
    title_font = load_font("serif_bold", 28)
    title = "NIGHTLY ACCESS LOG"
    bbox = title_font.getbbox(title)
    tw = bbox[2] - bbox[0]
    draw.text(((DOC_W - tw) // 2, y), title, fill=ink, font=title_font)
    y += 40

    # --- Form number bar ---
    draw.rectangle([30, y, DOC_W - 30, y + 28], fill=brown)
    form_font = load_font("mono", 12)
    draw.text((40, y + 6), "FORM PA-7  /  CASE: BPC-2024-0001",
              fill=PALETTE["paper"], font=form_font)
    y += 40

    # --- Facility info ---
    label_font = load_font("sans", 14)
    value_font = load_font("sans_bold", 14)

    fields = [
        ("FACILITY:", "BLOCK C — CENTRAL RATION DEPOT"),
        ("DATE:", "14 / 03 / 47"),
        ("SHIFT:", "NIGHT  (22:00 — 06:00)"),
        ("SUPERVISOR:", "SGT. HORVAT, M."),
    ]

    for label, value in fields:
        draw.text((40, y), label, fill=ink_light, font=label_font)
        draw.text((170, y), value, fill=ink, font=value_font)
        y += 24

    y += 8
    draw.line([(30, y), (DOC_W - 30, y)], fill=ink_light, width=1)
    y += 15

    # --- Access log table ---
    table_x = 30
    table_w = DOC_W - 60
    col_widths = [50, 240, 100, 100, 80, 100]  # NO, NAME, BADGE, IN, OUT, AUTH
    col_headers = ["NO.", "NAME", "BADGE", "TIME IN", "TIME OUT", "AUTH"]

    # Header row
    header_h = 32
    draw.rectangle([table_x, y, table_x + table_w, y + header_h], fill=brown)

    header_font = load_font("sans_bold", 13)
    cx = table_x
    for i, (header, cw) in enumerate(zip(col_headers, col_widths)):
        draw.text((cx + 8, y + 8), header, fill=PALETTE["paper"],
                  font=header_font)
        cx += cw
    y += header_h

    # Data rows
    rows = [
        ("1", "BABIC, Dragan",    "D-4418", "22:02", "06:05", "SCHED",  False),
        ("2", "HORVAT, Marko",    "M-2201", "22:00", "06:01", "SCHED",  False),
        ("3", "KOVAC, Petar",     "P-3305", "23:15", "05:48", "SCHED",  False),
        ("4", "ZELNIK, Miroslav", "M-1187", "02:17", "--:--", "GUARD",  True),
    ]

    row_font = load_font("mono", 13)
    row_h = 30

    for no, name, badge, time_in, time_out, auth, suspicious in rows:
        # Highlight suspicious row
        if suspicious:
            highlight = (
                min(255, PALETTE["paper"][0] - 15),
                min(255, PALETTE["paper"][1] - 20),
                min(255, PALETTE["paper"][2] - 30),
            )
            draw.rectangle([table_x + 1, y + 1, table_x + table_w - 1,
                            y + row_h - 1], fill=highlight)

        # Row border
        draw.line([(table_x, y), (table_x + table_w, y)], fill=ink_light,
                  width=1)

        # Cell values
        values = [no, name, badge, time_in, time_out, auth]
        cx = table_x
        for i, (val, cw) in enumerate(zip(values, col_widths)):
            color = ink
            if suspicious and i == 4:  # TIME OUT missing
                color = PALETTE["resistance_red"]
            draw.text((cx + 8, y + 7), val, fill=color, font=row_font)
            cx += cw

        # Suspicious marker
        if suspicious:
            marker_font = load_font("sans_bold", 16)
            draw.text((table_x + table_w + 8, y + 5), "?!",
                      fill=PALETTE["resistance_red"], font=marker_font)

        y += row_h

    # Empty rows (form has more space)
    for _ in range(3):
        draw.line([(table_x, y), (table_x + table_w, y)], fill=ink_light,
                  width=1)
        y += row_h

    # Table bottom border
    draw.line([(table_x, y), (table_x + table_w, y)], fill=ink_mid, width=2)

    # Vertical column dividers
    cx = table_x
    for cw in col_widths[:-1]:
        cx += cw
        for dy in range(0, y - (y - len(rows) * row_h - 3 * row_h - header_h), 2):
            yy = y - len(rows) * row_h - 3 * row_h - header_h + dy
            draw.point((cx, yy), fill=ink_light)

    y += 20

    # --- Authorization legend ---
    legend_font = load_font("sans", 11)
    draw.text((40, y), "AUTHORIZATION CODES:", fill=ink_mid, font=legend_font)
    y += 18
    codes = [
        "SCHED = Scheduled shift    GUARD = Guard override",
        "MAINT = Maintenance        EMERG = Emergency access",
    ]
    for line in codes:
        draw.text((40, y), line, fill=ink_light, font=legend_font)
        y += 16

    # --- Bureau stamp (lower right, slightly misaligned) ---
    draw_stamp_circle(
        draw,
        cx=DOC_W - 140, cy=y + 10,
        radius=55,
        text_top="SECTOR 3",
        text_bottom="VERIFIED",
        color=stamp,
        misalign=(3, -2),  # slight registration error
    )

    # --- Footer ---
    draw_footer(draw, img, DOC_W, DOC_H, color=ink_light)

    # --- Apply halftone overlay ---
    if not skip_halftone:
        img = apply_halftone(img, dot_spacing=dot_spacing,
                              dot_color=ink, bg_color=PALETTE["paper"])
        # Second pass: brown tint at offset angle
        from halftone_common import apply_halftone_tint
        img = apply_halftone_tint(img, dot_spacing=dot_spacing + 4,
                                   tint_color=brown, intensity=0.15, angle=15)

    # --- Final grain ---
    img = add_grain_overlay(img, amount=6)

    return img


def main():
    parser = argparse.ArgumentParser(
        description="Generate halftone Bureau evidence document"
    )
    parser.add_argument("-o", "--output", help="Output file path")
    parser.add_argument("--dot-spacing", type=int, default=12,
                        help="Halftone dot spacing (default: 12, lower=finer)")
    parser.add_argument("--no-halftone", action="store_true",
                        help="Skip halftone overlay (for debugging)")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    img = generate_access_log(
        dot_spacing=args.dot_spacing,
        skip_halftone=args.no_halftone,
    )

    out_path = args.output or str(OUTPUT_DIR / "ev_access_log_halftone.png")
    img.save(out_path)
    print(f"Generated: {out_path}  ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
