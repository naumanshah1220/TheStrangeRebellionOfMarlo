#!/usr/bin/env python3
"""
Family Letter Generator — Lenka's Day 1 Letter.

Generates a warm, personal letter from Lenka Dashev to Marlo.
This is the ONE thing in the game that has NO halftone grain —
it feels real, human, and warm. The visual opposite of every
institutional document.

Features:
- Soft warm cream background — NO halftone grain
- Handwritten-style font
- Warm, slightly uneven text
- No borders, stamps, or institutional markings
- Small child's drawing (Eli) in the corner
- Paper texture: slightly wrinkled, soft

Usage:
    python halftone_letter.py
    python halftone_letter.py -o letter.png
"""

import argparse
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw

from halftone_common import (
    PALETTE, OUTPUT_DIR,
    load_font, make_paper_texture,
)

LETTER_W = 600
LETTER_H = 800

# Letter content
LETTER_LINES = [
    "Marlo,",
    "",
    "Eli told his friends his father catches",
    "criminals. You should have seen his face —",
    "so proud, so serious. He made a badge out",
    "of cardboard and pinned it to his shirt.",
    "",
    "I didn't correct him. Let him have this",
    "version of what you do. The real one can",
    "wait until he's older.",
    "",
    "The heating is working again. Mrs. Bradic",
    "from downstairs brought soup. I saved you",
    "some but Eli got to it first. I'll make",
    "more tomorrow.",
    "",
    "Come home when you can.",
    "",
    "     — Lenka",
]


def draw_child_drawing(draw, x, y):
    """
    Draw a simple child's stick-figure drawing by Eli.
    Crayon-like, wobbly, warm colors.
    """
    # Crayon colors
    red = (200, 80, 60)
    blue = (80, 100, 180)
    green = (70, 150, 80)
    yellow = (210, 190, 60)
    brown = (140, 100, 60)

    # Draw a wobbly stick figure (Marlo with "badge")
    # Head
    for angle in range(0, 360, 8):
        r = 12 + random.randint(-1, 1)
        px = x + int(r * math.cos(math.radians(angle)))
        py = y + int(r * math.sin(math.radians(angle)))
        draw.ellipse([px - 1, py - 1, px + 1, py + 1], fill=brown)

    # Smile
    for angle in range(200, 340, 10):
        r = 6
        px = x + int(r * math.cos(math.radians(angle)))
        py = y + int(r * math.sin(math.radians(angle)))
        draw.point((px, py), fill=brown)

    # Eyes
    draw.ellipse([x - 5, y - 4, x - 3, y - 2], fill=brown)
    draw.ellipse([x + 3, y - 4, x + 5, y - 2], fill=brown)

    # Body line
    for dy in range(0, 30):
        dx = random.randint(-1, 0)
        draw.point((x + dx, y + 12 + dy), fill=brown)

    # Arms
    for dx in range(-18, 19):
        wobble = random.randint(-1, 1)
        draw.point((x + dx, y + 22 + wobble), fill=brown)

    # Legs
    for dy in range(0, 20):
        draw.point((x - 8 + random.randint(-1, 0), y + 42 + dy), fill=brown)
        draw.point((x + 8 + random.randint(0, 1), y + 42 + dy), fill=brown)

    # Badge (small red rectangle on chest)
    for bx in range(-4, 5):
        for by in range(-3, 4):
            if random.random() > 0.15:
                draw.point((x + bx, y + 20 + by), fill=red)

    # Star on badge
    draw.point((x, y + 20), fill=yellow)
    draw.point((x - 1, y + 19), fill=yellow)
    draw.point((x + 1, y + 19), fill=yellow)

    # Ground (green wobbly line)
    for gx in range(-30, 31):
        wobble = random.randint(-1, 1)
        draw.point((x + gx, y + 62 + wobble), fill=green)

    # Sun (upper right)
    sun_x = x + 35
    sun_y = y - 25
    for angle in range(0, 360, 15):
        r = 8
        px = sun_x + int(r * math.cos(math.radians(angle)))
        py = sun_y + int(r * math.sin(math.radians(angle)))
        draw.ellipse([px - 1, py - 1, px + 1, py + 1], fill=yellow)
    # Sun rays
    for angle in range(0, 360, 45):
        for dr in range(10, 16):
            px = sun_x + int(dr * math.cos(math.radians(angle)))
            py = sun_y + int(dr * math.sin(math.radians(angle)))
            draw.point((px, py), fill=yellow)

    # "DAD" text in wobbly child handwriting
    label_y = y + 66
    # D
    for dy in range(0, 8):
        draw.point((x - 10, label_y + dy), fill=red)
    for dx in range(0, 4):
        draw.point((x - 10 + dx, label_y), fill=red)
        draw.point((x - 10 + dx, label_y + 7), fill=red)
    draw.point((x - 6, label_y + 2), fill=red)
    draw.point((x - 6, label_y + 5), fill=red)
    # A
    for dy in range(0, 8):
        draw.point((x - 2, label_y + dy), fill=red)
        draw.point((x + 2, label_y + dy), fill=red)
    draw.point((x - 1, label_y), fill=red)
    draw.point((x, label_y), fill=red)
    draw.point((x + 1, label_y), fill=red)
    draw.point((x - 1, label_y + 4), fill=red)
    draw.point((x, label_y + 4), fill=red)
    draw.point((x + 1, label_y + 4), fill=red)
    # D
    for dy in range(0, 8):
        draw.point((x + 6, label_y + dy), fill=red)
    for dx in range(0, 4):
        draw.point((x + 6 + dx, label_y), fill=red)
        draw.point((x + 6 + dx, label_y + 7), fill=red)
    draw.point((x + 10, label_y + 2), fill=red)
    draw.point((x + 10, label_y + 5), fill=red)


def generate_letter():
    """Generate Lenka's family letter — warm, human, NO halftone."""

    # --- Background: warm cream paper, soft texture ---
    # Deliberately warmer than institutional paper
    warm_cream = (248, 242, 228)
    img = make_paper_texture(LETTER_W, LETTER_H, warm_cream, grain_amount=6,
                              fiber_density=0)
    draw = ImageDraw.Draw(img)

    # --- Subtle paper wrinkles (very faint) ---
    for _ in range(8):
        wx = random.randint(50, LETTER_W - 50)
        wy = random.randint(50, LETTER_H - 50)
        length = random.randint(40, 120)
        angle = random.uniform(0, math.pi)
        for t in range(length):
            px = int(wx + t * math.cos(angle))
            py = int(wy + t * math.sin(angle))
            if 0 <= px < LETTER_W and 0 <= py < LETTER_H:
                r, g, b = img.getpixel((px, py))
                # Very subtle shadow
                img.putpixel((px, py), (max(0, r - 3), max(0, g - 3),
                                         max(0, b - 2)))

    # --- Letter text in handwritten font ---
    hand_font = load_font("handwritten", 20)
    ink_color = (60, 50, 42)  # warm dark brown-black, not harsh ink

    x_margin = 60
    y_start = 50
    line_height = 30

    for i, line in enumerate(LETTER_LINES):
        ty = y_start + i * line_height

        # Slight wobble per line (human handwriting isn't perfectly aligned)
        x_wobble = random.randint(-2, 2)
        y_wobble = random.randint(-1, 1)

        # Slight color variation (pen pressure changes)
        pressure = random.randint(-8, 8)
        line_color = (
            max(0, min(255, ink_color[0] + pressure)),
            max(0, min(255, ink_color[1] + pressure)),
            max(0, min(255, ink_color[2] + pressure)),
        )

        draw.text((x_margin + x_wobble, ty + y_wobble), line,
                  fill=line_color, font=hand_font)

    # --- Child's drawing (Eli) in bottom-right corner ---
    draw_child_drawing(draw, LETTER_W - 100, LETTER_H - 140)

    # --- NO halftone overlay (this is the one real thing) ---
    # --- NO stamps, borders, or institutional markings ---

    return img


def main():
    parser = argparse.ArgumentParser(
        description="Generate Lenka's family letter (no halftone)"
    )
    parser.add_argument("-o", "--output", help="Output file path")
    args = parser.parse_args()

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    img = generate_letter()

    out_path = args.output or str(OUTPUT_DIR / "letter_lenka_day1.png")
    img.save(out_path)
    print(f"Generated: {out_path}  ({img.width}x{img.height})")


if __name__ == "__main__":
    main()
