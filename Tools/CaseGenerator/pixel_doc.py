#!/usr/bin/env python3
"""
Pixel Art Document Generator — Papers Please style.

Draws evidence documents at low native resolution (~120x168)
using a custom bitmap pixel font, then upscales with nearest-neighbor.

Usage:
    python pixel_doc.py                  # Generate sample access log
    python pixel_doc.py --scale 5        # 5x upscale (default 4x)
"""

import argparse
from pathlib import Path
from PIL import Image, ImageDraw

OUTPUT_DIR = Path(__file__).parent / "output"

# ==============================================================================
# COLOR PALETTE — Papers Please inspired, limited to ~8 colors
# ==============================================================================

PALETTE = {
    "bg":          (184, 172, 156),   # card background (warm gray-tan)
    "bg_light":    (196, 184, 168),   # lighter section background
    "bg_dark":     (160, 148, 132),   # darker section/header bar
    "bg_accent":   (172, 148, 132),   # accent bar (slightly warm)
    "border":      (100, 88, 76),     # borders and lines
    "text":        (40, 36, 32),      # primary text (near-black)
    "text_light":  (120, 108, 96),    # secondary/faded text
    "red":         (160, 60, 60),     # red accent (stamps, highlights)
    "red_bg":      (172, 140, 128),   # reddish background for dates
    "stamp_red":   (140, 80, 72),     # faded stamp color
    "white":       (216, 208, 196),   # highlight / white-ish
}

# ==============================================================================
# PIXEL FONT — 4x6 bitmap font (uppercase, digits, punctuation)
# Each char is a list of 6 rows, each row is 4 bits wide (0/1)
# ==============================================================================

FONT_4x6 = {
    'A': [
        [0,1,1,0],
        [1,0,0,1],
        [1,1,1,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'B': [
        [1,1,1,0],
        [1,0,0,1],
        [1,1,1,0],
        [1,0,0,1],
        [1,1,1,0],
        [0,0,0,0],
    ],
    'C': [
        [0,1,1,1],
        [1,0,0,0],
        [1,0,0,0],
        [1,0,0,0],
        [0,1,1,1],
        [0,0,0,0],
    ],
    'D': [
        [1,1,1,0],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [1,1,1,0],
        [0,0,0,0],
    ],
    'E': [
        [1,1,1,1],
        [1,0,0,0],
        [1,1,1,0],
        [1,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
    ],
    'F': [
        [1,1,1,1],
        [1,0,0,0],
        [1,1,1,0],
        [1,0,0,0],
        [1,0,0,0],
        [0,0,0,0],
    ],
    'G': [
        [0,1,1,1],
        [1,0,0,0],
        [1,0,1,1],
        [1,0,0,1],
        [0,1,1,1],
        [0,0,0,0],
    ],
    'H': [
        [1,0,0,1],
        [1,0,0,1],
        [1,1,1,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'I': [
        [1,1,1,0],
        [0,1,0,0],
        [0,1,0,0],
        [0,1,0,0],
        [1,1,1,0],
        [0,0,0,0],
    ],
    'J': [
        [0,0,1,1],
        [0,0,0,1],
        [0,0,0,1],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    'K': [
        [1,0,0,1],
        [1,0,1,0],
        [1,1,0,0],
        [1,0,1,0],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'L': [
        [1,0,0,0],
        [1,0,0,0],
        [1,0,0,0],
        [1,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
    ],
    'M': [
        [1,0,0,1],
        [1,1,1,1],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'N': [
        [1,0,0,1],
        [1,1,0,1],
        [1,0,1,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'O': [
        [0,1,1,0],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    'P': [
        [1,1,1,0],
        [1,0,0,1],
        [1,1,1,0],
        [1,0,0,0],
        [1,0,0,0],
        [0,0,0,0],
    ],
    'Q': [
        [0,1,1,0],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,1,0],
        [0,1,0,1],
        [0,0,0,0],
    ],
    'R': [
        [1,1,1,0],
        [1,0,0,1],
        [1,1,1,0],
        [1,0,1,0],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'S': [
        [0,1,1,1],
        [1,0,0,0],
        [0,1,1,0],
        [0,0,0,1],
        [1,1,1,0],
        [0,0,0,0],
    ],
    'T': [
        [1,1,1,1],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,0,0],
    ],
    'U': [
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    'V': [
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,1,1,0],
        [0,1,1,0],
        [0,0,0,0],
    ],
    'W': [
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [1,1,1,1],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'X': [
        [1,0,0,1],
        [0,1,1,0],
        [0,1,1,0],
        [0,1,1,0],
        [1,0,0,1],
        [0,0,0,0],
    ],
    'Y': [
        [1,0,0,1],
        [0,1,1,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,0,0],
    ],
    'Z': [
        [1,1,1,1],
        [0,0,1,0],
        [0,1,0,0],
        [1,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
    ],
    '0': [
        [0,1,1,0],
        [1,0,0,1],
        [1,0,0,1],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    '1': [
        [0,1,0,0],
        [1,1,0,0],
        [0,1,0,0],
        [0,1,0,0],
        [1,1,1,0],
        [0,0,0,0],
    ],
    '2': [
        [0,1,1,0],
        [1,0,0,1],
        [0,0,1,0],
        [0,1,0,0],
        [1,1,1,1],
        [0,0,0,0],
    ],
    '3': [
        [1,1,1,0],
        [0,0,0,1],
        [0,1,1,0],
        [0,0,0,1],
        [1,1,1,0],
        [0,0,0,0],
    ],
    '4': [
        [1,0,0,1],
        [1,0,0,1],
        [1,1,1,1],
        [0,0,0,1],
        [0,0,0,1],
        [0,0,0,0],
    ],
    '5': [
        [1,1,1,1],
        [1,0,0,0],
        [1,1,1,0],
        [0,0,0,1],
        [1,1,1,0],
        [0,0,0,0],
    ],
    '6': [
        [0,1,1,0],
        [1,0,0,0],
        [1,1,1,0],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    '7': [
        [1,1,1,1],
        [0,0,0,1],
        [0,0,1,0],
        [0,1,0,0],
        [0,1,0,0],
        [0,0,0,0],
    ],
    '8': [
        [0,1,1,0],
        [1,0,0,1],
        [0,1,1,0],
        [1,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    '9': [
        [0,1,1,0],
        [1,0,0,1],
        [0,1,1,1],
        [0,0,0,1],
        [0,1,1,0],
        [0,0,0,0],
    ],
    ' ': [
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '.': [
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,1,0,0],
        [0,0,0,0],
    ],
    ',': [
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,1,0,0],
        [1,0,0,0],
        [0,0,0,0],
    ],
    ':': [
        [0,0,0,0],
        [0,1,0,0],
        [0,0,0,0],
        [0,1,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '-': [
        [0,0,0,0],
        [0,0,0,0],
        [1,1,1,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '/': [
        [0,0,0,1],
        [0,0,1,0],
        [0,1,0,0],
        [1,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '(': [
        [0,0,1,0],
        [0,1,0,0],
        [0,1,0,0],
        [0,1,0,0],
        [0,0,1,0],
        [0,0,0,0],
    ],
    ')': [
        [0,1,0,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,0,1,0],
        [0,1,0,0],
        [0,0,0,0],
    ],
    '#': [
        [0,1,0,1],
        [1,1,1,1],
        [0,1,0,1],
        [1,1,1,1],
        [0,1,0,1],
        [0,0,0,0],
    ],
    '!': [
        [0,1,0,0],
        [0,1,0,0],
        [0,1,0,0],
        [0,0,0,0],
        [0,1,0,0],
        [0,0,0,0],
    ],
    '?': [
        [0,1,1,0],
        [1,0,0,1],
        [0,0,1,0],
        [0,0,0,0],
        [0,0,1,0],
        [0,0,0,0],
    ],
    '_': [
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
    ],
    '=': [
        [0,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
        [1,1,1,1],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '+': [
        [0,0,0,0],
        [0,1,0,0],
        [1,1,1,0],
        [0,1,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '*': [
        [1,0,1,0],
        [0,1,0,0],
        [1,0,1,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    "'": [
        [0,1,0,0],
        [0,1,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
    '"': [
        [1,0,1,0],
        [1,0,1,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
        [0,0,0,0],
    ],
}

# Lowercase maps to uppercase for simplicity
for c in 'abcdefghijklmnopqrstuvwxyz':
    FONT_4x6[c] = FONT_4x6[c.upper()]


# Character width (4) + 1px spacing
CHAR_W = 5
CHAR_H = 6


def draw_text(img, x, y, text, color, draw=None):
    """Draw pixel text at (x, y) using the 4x6 bitmap font."""
    if draw is None:
        draw = ImageDraw.Draw(img)

    cx = x
    for ch in text:
        glyph = FONT_4x6.get(ch)
        if glyph is None:
            cx += CHAR_W  # skip unknown chars
            continue
        for row_i, row in enumerate(glyph):
            for col_i, pixel in enumerate(row):
                if pixel:
                    draw.point((cx + col_i, y + row_i), fill=color)
        cx += CHAR_W
    return cx  # return end x position


def text_width(text):
    """Calculate pixel width of text string."""
    return len(text) * CHAR_W


def draw_text_centered(img, y, text, color, canvas_width, draw=None):
    """Draw text centered horizontally."""
    w = text_width(text)
    x = (canvas_width - w) // 2
    draw_text(img, x, y, text, color, draw)


def draw_text_right(img, x_right, y, text, color, draw=None):
    """Draw text right-aligned to x_right."""
    w = text_width(text)
    draw_text(img, x_right - w, y, text, color, draw)


def draw_hline(draw, x1, x2, y, color):
    """Draw a horizontal pixel line."""
    draw.line([(x1, y), (x2, y)], fill=color)


def draw_rect_outline(draw, x, y, w, h, color):
    """Draw a 1px rectangle outline."""
    draw.rectangle([x, y, x + w - 1, y + h - 1], outline=color)


def draw_filled_rect(draw, x, y, w, h, color):
    """Draw a filled rectangle."""
    draw.rectangle([x, y, x + w - 1, y + h - 1], fill=color)


# ==============================================================================
# STAMP — pixel art rubber stamp (dithered circle with text)
# ==============================================================================

def draw_stamp(img, cx, cy, radius, draw=None):
    """Draw a pixelated circular rubber stamp."""
    if draw is None:
        draw = ImageDraw.Draw(img)
    color = PALETTE["stamp_red"]

    # Draw dithered circle
    for dy in range(-radius, radius + 1):
        for dx in range(-radius, radius + 1):
            dist = (dx * dx + dy * dy) ** 0.5
            # Ring at the edge (outline)
            if abs(dist - radius) < 1.2:
                # Dither: checkerboard pattern for faded look
                if (dx + dy) % 2 == 0:
                    draw.point((cx + dx, cy + dy), fill=color)
            # Inner ring
            elif abs(dist - (radius - 3)) < 0.8:
                if (dx + dy) % 3 == 0:
                    draw.point((cx + dx, cy + dy), fill=color)

    # Stamp text (tiny, use direct pixel placement)
    draw_text_centered(img, cy - 4, "SECTOR 3", color, cx * 2, draw)
    draw_text_centered(img, cy + 2, "VERIFIED", color, cx * 2, draw)


# ==============================================================================
# PATTERN EMBLEM — small 7x7 pixel icon
# ==============================================================================

EMBLEM_7x7 = [
    [0,0,1,1,1,0,0],
    [0,1,0,0,0,1,0],
    [1,0,1,1,0,0,1],
    [1,0,1,0,0,0,1],
    [1,0,1,0,0,0,1],
    [0,1,0,0,0,1,0],
    [0,0,1,1,1,0,0],
]

def draw_emblem(draw, x, y, color):
    """Draw the Pattern emblem (circle with P)."""
    for row_i, row in enumerate(EMBLEM_7x7):
        for col_i, pixel in enumerate(row):
            if pixel:
                draw.point((x + col_i, y + row_i), fill=color)


# ==============================================================================
# DOCUMENT: Depot Access Log
# ==============================================================================

def generate_access_log(scale=4):
    """Generate the Depot Access Log as pixel art."""

    # Native resolution — small, like Papers Please
    W, H = 160, 200

    img = Image.new("RGB", (W, H), PALETTE["bg"])
    draw = ImageDraw.Draw(img)

    # --- BORDER ---
    draw_rect_outline(draw, 0, 0, W, H, PALETTE["border"])

    # --- CLASSIFICATION STRIP ---
    strip_text = "BUREAU USE ONLY"
    strip_w = text_width(strip_text) + 6
    strip_x = (W - strip_w) // 2
    draw_filled_rect(draw, strip_x, 2, strip_w, 8, PALETTE["bg_dark"])
    draw_rect_outline(draw, strip_x, 2, strip_w, 8, PALETTE["border"])
    draw_text(img, strip_x + 3, 3, strip_text, PALETTE["red"], draw)

    # --- HEADER BORDER (top) ---
    draw_hline(draw, 4, W - 5, 13, PALETTE["border"])
    draw_hline(draw, 4, W - 5, 14, PALETTE["text_light"])

    # --- EMBLEM ---
    emblem_x = (W - 7) // 2
    draw_emblem(draw, emblem_x, 17, PALETTE["text"])

    # --- STATE NAME ---
    draw_text_centered(img, 26, "REPUBLIC OF", PALETTE["text"], W, draw)
    draw_text_centered(img, 33, "DRAZHOVIA", PALETTE["text"], W, draw)

    # --- HEADER BORDER (bottom) ---
    draw_hline(draw, 4, W - 5, 41, PALETTE["text_light"])
    draw_hline(draw, 4, W - 5, 42, PALETTE["border"])

    # --- DOCUMENT TITLE ---
    draw_text_centered(img, 46, "NIGHTLY ACCESS", PALETTE["text"], W, draw)
    draw_text_centered(img, 53, "LOG", PALETTE["text"], W, draw)

    # --- SUBTITLE BAR ---
    draw_filled_rect(draw, 4, 60, W - 8, 8, PALETTE["bg_dark"])
    draw_text(img, 6, 61, "FORM PA-7  SECURITY DIV.", PALETTE["text_light"], draw)

    # --- FACILITY INFO ---
    y = 72
    draw_text(img, 6, y, "FACILITY:", PALETTE["text_light"], draw)
    draw_text(img, 56, y, "BLOCK C DEPOT", PALETTE["text"], draw)

    y += 8
    draw_text(img, 6, y, "DATE:", PALETTE["text_light"], draw)
    draw_text(img, 36, y, "14/03/47", PALETTE["text"], draw)

    draw_text(img, 90, y, "SHIFT:", PALETTE["text_light"], draw)
    draw_text(img, 120, y, "NIGHT", PALETTE["text"], draw)

    draw_hline(draw, 4, W - 5, y + 8, PALETTE["text_light"])

    # --- TABLE ---
    table_y = 92
    table_x = 4
    table_w = W - 8

    # Table header bar
    draw_filled_rect(draw, table_x, table_y, table_w, 8, PALETTE["bg_dark"])
    draw_rect_outline(draw, table_x, table_y, table_w, 8, PALETTE["border"])

    # Column positions — wider spacing
    col_no = table_x + 2
    col_name = table_x + 12
    col_in = table_x + 70
    col_out = table_x + 100
    col_auth = table_x + 136

    draw_text(img, col_no, table_y + 1, "N", PALETTE["text"], draw)
    draw_text(img, col_name, table_y + 1, "NAME", PALETTE["text"], draw)
    draw_text(img, col_in, table_y + 1, "IN", PALETTE["text"], draw)
    draw_text(img, col_out, table_y + 1, "OUT", PALETTE["text"], draw)
    draw_text(img, col_auth, table_y + 1, "A", PALETTE["text"], draw)

    # Table rows
    rows = [
        ("1", "BABIC D",   "22:02", "06:05", "S", False),
        ("2", "HORVAT M",  "22:00", "06:01", "S", False),
        ("3", "KOVAC P",   "23:15", "05:48", "S", False),
        ("4", "ZELNIK M",  "02:17", "--:--", "G", True),  # SUSPICIOUS
    ]

    for i, (no, name, time_in, time_out, auth, suspicious) in enumerate(rows):
        row_y = table_y + 9 + (i * 10)

        # Highlight suspicious row
        if suspicious:
            draw_filled_rect(draw, table_x + 1, row_y + 1, table_w - 2, 8, PALETTE["bg_accent"])

        # Row separator
        draw_hline(draw, table_x, table_x + table_w - 1, row_y, PALETTE["text_light"])

        text_color = PALETTE["text"]
        draw_text(img, col_no, row_y + 2, no, text_color, draw)
        draw_text(img, col_name, row_y + 2, name, text_color, draw)
        draw_text(img, col_in, row_y + 2, time_in, text_color, draw)
        draw_text(img, col_out, row_y + 2, time_out,
                  PALETTE["red"] if suspicious else text_color, draw)
        draw_text(img, col_auth, row_y + 2, auth, text_color, draw)

        # Red exclamation on suspicious row
        if suspicious:
            draw_text(img, table_x + table_w + 2, row_y + 2, "?!", PALETTE["red"], draw)

    # Table bottom border
    table_bottom = table_y + 9 + len(rows) * 10
    draw_hline(draw, table_x, table_x + table_w - 1, table_bottom, PALETTE["border"])
    draw_rect_outline(draw, table_x, table_y, table_w, table_bottom - table_y, PALETTE["border"])

    # Vertical divider lines (drawn after rows so we know height)
    for vx in [col_name - 2, col_in - 2, col_out - 2, col_auth - 2]:
        for vy_off in range(table_bottom - table_y + 1):
            if vy_off % 2 == 0:
                draw.point((vx, table_y + vy_off), fill=PALETTE["text_light"])

    # --- EMPTY ROWS (show the form has more space) ---
    for i in range(2):
        empty_y = table_bottom + 1 + (i * 10)
        draw_hline(draw, table_x, table_x + table_w - 1, empty_y + 9, PALETTE["text_light"])

    # --- AUTH LEGEND ---
    legend_y = table_bottom + 24
    draw_text(img, 6, legend_y, "S=SCHED  G=GUARD", PALETTE["text_light"], draw)
    draw_text(img, 6, legend_y + 8, "M=MAINT  E=EMERG", PALETTE["text_light"], draw)

    # --- STAMP (dithered, lower right) ---
    draw_stamp(img, 125, legend_y + 6, 14, draw)

    # --- FOOTER ---
    footer_y = H - 12
    draw_hline(draw, 4, W - 5, footer_y, PALETTE["text_light"])
    draw_text_centered(img, footer_y + 2, "THE PATTERN PROVIDES", PALETTE["text_light"], W, draw)

    # --- DOG EAR (top-right corner) ---
    for i in range(6):
        for j in range(6 - i):
            px = W - 1 - j
            py = i
            draw.point((px, py), fill=PALETTE["bg_dark"])
    # Fold line
    for i in range(6):
        draw.point((W - 6 + i, 5 - i), fill=PALETTE["border"])

    # --- Upscale with nearest-neighbor ---
    final = img.resize((W * scale, H * scale), Image.NEAREST)

    return final, img


def main():
    parser = argparse.ArgumentParser(description="Generate pixel art evidence documents")
    parser.add_argument("--scale", type=int, default=4, help="Upscale factor (default 4)")
    parser.add_argument("--output", "-o", help="Output path")
    parser.add_argument("--native", action="store_true", help="Also save 1x native resolution")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    final, native = generate_access_log(scale=args.scale)

    out_path = args.output or str(OUTPUT_DIR / "ev_access_log_pixel.png")
    final.save(out_path)
    print(f"Generated: {out_path}  ({final.width}x{final.height}, {args.scale}x upscale)")

    if args.native:
        native_path = str(Path(out_path).with_suffix("")) + "_1x.png"
        native.save(native_path)
        print(f"Native:    {native_path}  ({native.width}x{native.height})")


if __name__ == "__main__":
    main()
