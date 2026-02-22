#!/usr/bin/env python3
"""
Shared halftone utilities for the art style generators.

Provides:
- Master palette constants
- Halftone dot overlay generation
- Paper texture generation
- Common drawing helpers (borders, stamps, headers)
- Font loading with fallback chain
"""

import math
import random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageFilter

OUTPUT_DIR = Path(__file__).parent / "output"

# =============================================================================
# MASTER PALETTE — from art_style_bible.md
# =============================================================================

PALETTE = {
    # Base
    "paper":            (240, 230, 208),  # #F0E6D0
    "ink":              (26, 26, 26),      # #1A1A1A
    "ink_mid":          (74, 70, 64),      # #4A4640
    "ink_light":        (138, 132, 120),   # #8A8478

    # Power structure
    "council_blue":     (45, 75, 142),     # #2D4B8E
    "bureau_brown":     (107, 83, 64),     # #6B5340
    "resistance_red":   (160, 32, 40),     # #A02028

    # Worker occupation
    "worker_orange":    (200, 120, 48),    # #C87830
    "worker_green":     (58, 122, 64),     # #3A7A40
    "worker_amber":     (184, 144, 80),    # #B89050

    # Special
    "unreliable_gray":  (138, 138, 138),   # #8A8A8A
    "stamp_dark":       (58, 40, 32),      # #3A2820
    "newspaper_yellow": (212, 200, 120),   # #D4C878

    # Derived
    "desk":             (42, 38, 34),      # #2A2622
    "paper_warm":       (245, 238, 220),   # warmer variant for letters
}


# =============================================================================
# FONT LOADING — tries common system fonts with fallback
# =============================================================================

# Font search paths (Windows + macOS + Linux)
_FONT_DIRS = [
    Path("C:/Windows/Fonts"),
    Path("/usr/share/fonts"),
    Path("/usr/local/share/fonts"),
    Path.home() / ".fonts",
    Path("/System/Library/Fonts"),
    Path("/Library/Fonts"),
]

_FONT_CACHE = {}

def _find_font_file(names):
    """Search for font files by name across system font directories."""
    for name in names:
        # Try exact path first
        p = Path(name)
        if p.exists():
            return str(p)
        # Search font directories
        for d in _FONT_DIRS:
            if not d.exists():
                continue
            for f in d.rglob("*"):
                if f.name.lower() == name.lower():
                    return str(f)
    return None


def load_font(style, size):
    """
    Load a TrueType font by style name and size.

    Styles:
        "serif"      — Times New Roman / Georgia / DejaVu Serif
        "sans"       — Arial / Helvetica / DejaVu Sans
        "mono"       — Courier New / DejaVu Sans Mono
        "condensed"  — Arial Narrow / Impact / fallback to sans
        "cursive"    — Segoe Script / Comic Sans / fallback to serif italic
        "handwritten"— Segoe Print / fallback to cursive
    """
    key = (style, size)
    if key in _FONT_CACHE:
        return _FONT_CACHE[key]

    candidates = {
        "serif": [
            "times.ttf", "timesbd.ttf", "Times New Roman.ttf",
            "Georgia.ttf", "georgia.ttf",
            "DejaVuSerif.ttf", "LiberationSerif-Regular.ttf",
        ],
        "serif_bold": [
            "timesbd.ttf", "Times New Roman Bold.ttf",
            "georgiab.ttf", "Georgia Bold.ttf",
            "DejaVuSerif-Bold.ttf", "LiberationSerif-Bold.ttf",
        ],
        "sans": [
            "arial.ttf", "Arial.ttf",
            "Helvetica.ttf", "helvetica.ttf",
            "DejaVuSans.ttf", "LiberationSans-Regular.ttf",
        ],
        "sans_bold": [
            "arialbd.ttf", "Arial Bold.ttf",
            "Helvetica-Bold.ttf",
            "DejaVuSans-Bold.ttf", "LiberationSans-Bold.ttf",
        ],
        "mono": [
            "cour.ttf", "Courier New.ttf", "courbd.ttf",
            "DejaVuSansMono.ttf", "LiberationMono-Regular.ttf",
        ],
        "mono_bold": [
            "courbd.ttf", "Courier New Bold.ttf",
            "DejaVuSansMono-Bold.ttf", "LiberationMono-Bold.ttf",
        ],
        "condensed": [
            "arialn.ttf", "Arial Narrow.ttf",
            "impact.ttf", "Impact.ttf",
            "DejaVuSans-ExtraLight.ttf",
        ],
        "condensed_bold": [
            "arialnb.ttf", "Arial Narrow Bold.ttf",
            "impact.ttf", "Impact.ttf",
        ],
        "cursive": [
            "segoesc.ttf", "Segoe Script.ttf",
            "palai.ttf", "Palatino Linotype Italic.ttf",
            "ITCEDSCR.TTF", "Edwardian Script ITC.ttf",
        ],
        "handwritten": [
            "segoepr.ttf", "Segoe Print.ttf",
            "comic.ttf", "Comic Sans MS.ttf",
        ],
    }

    names = candidates.get(style, candidates["sans"])
    path = _find_font_file(names)

    if path:
        try:
            font = ImageFont.truetype(path, size)
            _FONT_CACHE[key] = font
            return font
        except Exception:
            pass

    # Ultimate fallback — Pillow default
    font = ImageFont.load_default()
    _FONT_CACHE[key] = font
    return font


# =============================================================================
# HALFTONE DOT OVERLAY
# =============================================================================

def apply_halftone(img, dot_spacing=8, dot_color=None, bg_color=None, angle=0):
    """
    Apply a halftone dot pattern overlay to an image.

    Converts the image to a halftone approximation:
    - Divides image into cells of `dot_spacing` pixels
    - Samples average brightness of each cell
    - Draws a filled circle whose radius is proportional to darkness

    Args:
        img: PIL Image (RGB)
        dot_spacing: Distance between dot centers (lower = finer halftone)
        dot_color: Tuple (R,G,B) for dots. Default: PALETTE["ink"]
        bg_color: Tuple (R,G,B) for background. Default: PALETTE["paper"]
        angle: Rotation angle in degrees for the dot grid

    Returns:
        New PIL Image with halftone effect applied.
    """
    if dot_color is None:
        dot_color = PALETTE["ink"]
    if bg_color is None:
        bg_color = PALETTE["paper"]

    src = img.convert("L")  # grayscale
    w, h = src.size
    out = Image.new("RGB", (w, h), bg_color)
    draw = ImageDraw.Draw(out)

    max_r = dot_spacing * 0.45  # maximum dot radius

    # Angle offset for grid
    cos_a = math.cos(math.radians(angle))
    sin_a = math.sin(math.radians(angle))

    for gy in range(-dot_spacing, h + dot_spacing, dot_spacing):
        for gx in range(-dot_spacing, w + dot_spacing, dot_spacing):
            # Rotated grid position
            cx = int(gx * cos_a - gy * sin_a + w * (1 - cos_a + sin_a) / 2)
            cy = int(gx * sin_a + gy * cos_a + h * (1 - cos_a - sin_a) / 2)

            if cx < 0 or cx >= w or cy < 0 or cy >= h:
                continue

            # Sample average brightness in cell
            x0 = max(0, cx - dot_spacing // 2)
            y0 = max(0, cy - dot_spacing // 2)
            x1 = min(w, cx + dot_spacing // 2)
            y1 = min(h, cy + dot_spacing // 2)

            region = src.crop((x0, y0, x1, y1))
            pixels = list(region.getdata())
            if not pixels:
                continue
            avg = sum(pixels) / len(pixels)

            # Darker = bigger dot (avg 0=black -> full radius, 255=white -> no dot)
            darkness = 1.0 - (avg / 255.0)
            r = max_r * darkness

            if r < 0.5:
                continue

            draw.ellipse(
                [cx - r, cy - r, cx + r, cy + r],
                fill=dot_color
            )

    return out


def apply_halftone_blend(img, dot_spacing=8, dot_color=None, bg_color=None,
                          angle=0, blend=0.5):
    """
    Apply halftone and blend with original for readable results.

    At full replacement (blend=1.0), halftone destroys fine detail like text.
    Blending preserves readability while adding visible halftone texture.

    Args:
        img: Source image
        dot_spacing: Dot grid spacing
        dot_color, bg_color: Colors (default: ink on paper)
        angle: Grid rotation
        blend: 0.0 = original only, 1.0 = full halftone. Recommended: 0.4-0.6

    Returns:
        Blended PIL Image.
    """
    halftoned = apply_halftone(img, dot_spacing, dot_color, bg_color, angle)
    return Image.blend(img, halftoned, blend)


def apply_halftone_tint(img, dot_spacing=8, tint_color=None, intensity=0.3, angle=15):
    """
    Apply a colored halftone tint layer on top of an existing image.

    Creates a second-pass halftone in a different color (e.g., bureau_brown)
    with slight angular offset to simulate color registration.

    Args:
        img: PIL Image (RGB) — the base image to tint
        dot_spacing: Dot grid spacing
        tint_color: Tuple (R,G,B) for tint dots
        intensity: 0.0-1.0, how visible the tint is
        angle: Grid rotation angle (offset from primary halftone)

    Returns:
        New PIL Image with tint applied.
    """
    if tint_color is None:
        tint_color = PALETTE["bureau_brown"]

    w, h = img.size
    tint_layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(tint_layer)

    max_r = dot_spacing * 0.35
    alpha = int(255 * intensity)
    fill = tint_color + (alpha,)

    cos_a = math.cos(math.radians(angle))
    sin_a = math.sin(math.radians(angle))

    for gy in range(-dot_spacing, h + dot_spacing, dot_spacing):
        for gx in range(-dot_spacing, w + dot_spacing, dot_spacing):
            cx = int(gx * cos_a - gy * sin_a + w * (1 - cos_a + sin_a) / 2)
            cy = int(gx * sin_a + gy * cos_a + h * (1 - cos_a - sin_a) / 2)

            if cx < 0 or cx >= w or cy < 0 or cy >= h:
                continue

            r = max_r * (0.5 + random.random() * 0.5)  # slight variation
            draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=fill)

    base = img.convert("RGBA")
    return Image.alpha_composite(base, tint_layer).convert("RGB")


# =============================================================================
# PAPER TEXTURE
# =============================================================================

def make_paper_texture(w, h, base_color=None, grain_amount=12, fiber_density=0):
    """
    Generate a paper texture background.

    Args:
        w, h: Dimensions
        base_color: Base RGB tuple (default: PALETTE["paper"])
        grain_amount: Noise amplitude (0-30)
        fiber_density: 0=clean, 1=slight fibers, 2=visible fibers

    Returns:
        PIL Image with paper texture.
    """
    if base_color is None:
        base_color = PALETTE["paper"]

    img = Image.new("RGB", (w, h), base_color)
    pixels = img.load()

    for y in range(h):
        for x in range(w):
            # Random grain
            noise = random.randint(-grain_amount, grain_amount)
            r = max(0, min(255, base_color[0] + noise))
            g = max(0, min(255, base_color[1] + noise))
            b = max(0, min(255, base_color[2] + noise))
            pixels[x, y] = (r, g, b)

    # Add subtle fiber streaks
    if fiber_density > 0:
        draw = ImageDraw.Draw(img)
        n_fibers = (w * h // 2000) * fiber_density
        for _ in range(n_fibers):
            fx = random.randint(0, w - 1)
            fy = random.randint(0, h - 1)
            length = random.randint(3, 15)
            angle = random.uniform(-0.3, 0.3)  # mostly horizontal
            ex = fx + int(length * math.cos(angle))
            ey = fy + int(length * math.sin(angle))
            shade = random.randint(-8, 8)
            fiber_color = (
                max(0, min(255, base_color[0] + shade - 10)),
                max(0, min(255, base_color[1] + shade - 10)),
                max(0, min(255, base_color[2] + shade - 8)),
            )
            draw.line([(fx, fy), (ex, ey)], fill=fiber_color, width=1)

    return img


def make_yellowed_paper(w, h, amount=1):
    """Paper with yellowing effect. amount: 0=slight, 1=moderate, 2=heavy."""
    base = PALETTE["paper"]
    yellow_shift = 8 * (amount + 1)
    yellowed = (
        min(255, base[0] - 2 * amount),
        min(255, base[1] - 4 * amount),
        max(0, base[2] - yellow_shift),
    )
    return make_paper_texture(w, h, yellowed, grain_amount=10 + 4 * amount,
                               fiber_density=amount)


# =============================================================================
# DRAWING HELPERS
# =============================================================================

def draw_bureau_header(draw, img, w, y_start, font_size=18):
    """Draw standard Bureau of Pattern Compliance header."""
    color = PALETTE["bureau_brown"]
    ink = PALETTE["ink"]

    # Double line at top
    draw.line([(20, y_start), (w - 20, y_start)], fill=color, width=2)
    draw.line([(20, y_start + 4), (w - 20, y_start + 4)], fill=color, width=1)

    # Header text
    header_font = load_font("serif_bold", font_size)
    text = "BUREAU OF PATTERN COMPLIANCE"
    bbox = header_font.getbbox(text)
    tw = bbox[2] - bbox[0]
    draw.text(((w - tw) // 2, y_start + 10), text, fill=color, font=header_font)

    # Lines below
    draw.line([(20, y_start + 34), (w - 20, y_start + 34)], fill=color, width=1)
    draw.line([(20, y_start + 38), (w - 20, y_start + 38)], fill=color, width=2)

    return y_start + 44


def draw_pattern_emblem(draw, cx, cy, radius, color=None):
    """Draw The Pattern emblem — circle with P."""
    if color is None:
        color = PALETTE["stamp_dark"]

    # Outer circle
    draw.ellipse(
        [cx - radius, cy - radius, cx + radius, cy + radius],
        outline=color, width=2
    )

    # Inner P
    font = load_font("serif_bold", int(radius * 1.4))
    bbox = font.getbbox("P")
    tw = bbox[2] - bbox[0]
    th = bbox[3] - bbox[1]
    draw.text((cx - tw // 2, cy - th // 2 - bbox[1]), "P", fill=color, font=font)


def draw_stamp_circle(draw, cx, cy, radius, text_top, text_bottom, color=None,
                       rotation=0, misalign=(0, 0)):
    """
    Draw a circular rubber stamp with text.

    Args:
        draw: ImageDraw instance
        cx, cy: Center position (before misalignment)
        radius: Stamp radius
        text_top: Text on upper arc
        text_bottom: Text on lower arc
        color: Stamp color (default: stamp_dark)
        rotation: Not implemented yet — future enhancement
        misalign: (dx, dy) offset to simulate registration error
    """
    if color is None:
        color = PALETTE["stamp_dark"]

    cx += misalign[0]
    cy += misalign[1]

    # Outer ring (double)
    draw.ellipse(
        [cx - radius, cy - radius, cx + radius, cy + radius],
        outline=color, width=2
    )
    draw.ellipse(
        [cx - radius + 4, cy - radius + 4, cx + radius - 4, cy + radius - 4],
        outline=color, width=1
    )

    # Center emblem
    draw_pattern_emblem(draw, cx, cy, radius // 3, color)

    # Top text
    font = load_font("sans_bold", max(8, radius // 4))
    bbox = font.getbbox(text_top)
    tw = bbox[2] - bbox[0]
    draw.text((cx - tw // 2, cy - radius + 6), text_top, fill=color, font=font)

    # Bottom text
    bbox = font.getbbox(text_bottom)
    tw = bbox[2] - bbox[0]
    draw.text((cx - tw // 2, cy + radius - 18), text_bottom, fill=color, font=font)


def draw_footer(draw, img, w, h, text="THE PATTERN PROVIDES", color=None,
                font_size=9):
    """Draw standard footer line with motto."""
    if color is None:
        color = PALETTE["ink_light"]
    font = load_font("sans", font_size)
    bbox = font.getbbox(text)
    tw = bbox[2] - bbox[0]
    y = h - 24
    draw.line([(20, y), (w - 20, y)], fill=color, width=1)
    draw.text(((w - tw) // 2, y + 4), text, fill=color, font=font)


def add_grain_overlay(img, amount=15):
    """Add random noise grain over the entire image."""
    w, h = img.size
    pixels = img.load()
    for y in range(h):
        for x in range(w):
            r, g, b = pixels[x, y]
            noise = random.randint(-amount, amount)
            pixels[x, y] = (
                max(0, min(255, r + noise)),
                max(0, min(255, g + noise)),
                max(0, min(255, b + noise)),
            )
    return img


def add_fold_crease(img, y_pos=None, horizontal=True):
    """Add a subtle fold crease line across the image."""
    w, h = img.size
    draw = ImageDraw.Draw(img)

    if horizontal:
        y = y_pos or h // 2
        for x in range(w):
            # Darken slightly above crease, lighten slightly below
            offset = random.randint(-1, 1)
            base_y = y + offset
            if 0 <= base_y < h and 0 <= base_y + 1 < h:
                r, g, b = img.getpixel((x, base_y))
                img.putpixel((x, base_y), (
                    max(0, r - 15), max(0, g - 15), max(0, b - 12)
                ))
                r2, g2, b2 = img.getpixel((x, base_y + 1))
                img.putpixel((x, base_y + 1), (
                    min(255, r2 + 8), min(255, g2 + 8), min(255, b2 + 6)
                ))
    return img
