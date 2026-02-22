# Art Style Bible — The Strange Rebellion of Marlo

## Halftone Duotone Print Aesthetic

---

## 1. VISUAL PHILOSOPHY

**One-line pitch:** "Every person is a file. Every file is printed by the regime."

**Core technique:** Halftone duotone — all imagery looks like it was printed on a limited-color press. Visible dot patterns. Ink-on-paper texture. No smooth digital gradients. The entire game feels like a state-produced dossier.

**Why this fits Marlo:** The regime reduces citizens to paperwork. The halftone aesthetic makes everything look *printed* — processed, catalogued, dehumanized. When the player stamps an indictment, they're adding one more printed page to the machine.

**Key references:**
- "Wake Up Dead Man" poster (primary reference — monochrome figures, flat color backgrounds, halftone grain)
- John Heartfield's anti-fascist photomontage (political collage)
- 1960s–70s Czech/Polish film posters (muted palettes, subtle subversion)
- Risograph/screen-print zines (ink registration imperfections, limited color runs)

**What this is NOT:**
- Not pixel art (no visible square pixels, no retro game feel)
- Not realistic illustration (not painted, not photorealistic)
- Not clean digital vector art (must have print imperfection, grain, texture)

---

## 2. MASTER PALETTE

*Inspired by The Handmaid's Tale's class-color system: each social role is immediately readable by color.*

### Base Colors (always present)

| Name | Hex | Use |
|---|---|---|
| `paper` | `#F0E6D0` | Base background. Aged cream stock. Desk surface, card backs, document base. |
| `ink` | `#1A1A1A` | Near-black. Primary text, halftone dots, outlines. |
| `ink-mid` | `#4A4640` | Warm dark gray. Secondary text, lighter halftone values. |
| `ink-light` | `#8A8478` | Warm medium gray. Faded text, background texture, subtle grain. |

### Power Structure Colors

| Class | Color Name | Hex | Meaning |
|---|---|---|---|
| Council / Elite | `council-blue` | `#2D4B8E` | Royal, noble, aristocratic. Blue blood. Cold authority. |
| Bureau / Officials | `bureau-brown` | `#6B5340` | Institutional authority. Earthy, stern, enforcer. Like Aunt Lydia. |
| Resistanz | `resistance-red` | `#A02028` | Defiance, passion, blood. The people's fire. |

### Worker Occupation Colors (3 categories)

Citizens are color-coded by their labor sector. The state sorts people by function.

| Occupation Sector | Color Name | Hex | Who |
|---|---|---|---|
| Industrial / Technical | `worker-orange` | `#C87830` | Factory workers, construction, warehouse, water treatment, repair, maintenance |
| Commerce / Agriculture | `worker-green` | `#3A7A40` | Market vendors, food workers, ration depot, farmers, agriculture |
| Service / Clerical | `worker-amber` | `#B89050` | Postal, couriers, janitors, building monitors, filing clerks, clinic orderlies, domestic |

### Special Colors

| Name | Hex | Use |
|---|---|---|
| `unreliable-gray` | `#8A8A8A` | Block G / Unreliables — washed out, desaturated. Identity erased by the state. No occupation color remains. |
| `stamp-dark` | `#3A2820` | Official stamp ink — very dark brown-maroon. Bureau and Council seals, "DEVIATION" marks. |
| `newspaper-yellow` | `#D4C878` | Aged newsprint. Day briefing background. |

### Palette Rules
- **Maximum 4 colors per composition:** paper + ink + one class/occupation color + one accent
- **Halftone creates perceived tones:** Ink dots at varying density on paper create the illusion of many gray tones. Class color dots at varying density create perceived tints.
- **No gradients.** All tonal transitions are halftone dot patterns or hard-edge steps.
- **Color is information.** If a color appears, it means something. No decorative color.
- **Workers' occupation color appears on:** portrait background, clothing accents, document headers, ID cards
- **Resistance red is rare and dangerous.** It appears on: pamphlets, graffiti, the broken circle symbol, and (subtly) on Resistance-aligned characters. Seeing red in-game should feel electric — someone is risking their life.

---

## 3. CLASS VISUAL SYSTEM

Five visual categories, each with a complete identity: signature color, portrait treatment, fashion vocabulary, document style, and in-game font.

### 3A. Council / Elite (Block A)

**Signature color:** `council-blue` (#2D4B8E)
**In-game font:** Cursive / script (formal, ornate)
**Halftone density:** Fine dots, high resolution — expensive printing

**Portrait treatment:**
- Solid `council-blue` background
- Figure rendered in monochrome halftone (ink on paper)
- High contrast — sharp features, strong shadows
- Faces rarely shown clearly (obscured, profile, or masked) — reflecting secrecy
- Porcelain mask motif for ceremonial appearances
- Blue clothing accents visible even in monochrome (rendered as blue halftone tint)

**Fashion vocabulary:**
- Well-tailored blue coats, high collars, capes (like Handmaid's Tale Wives)
- Quality fabric — smooth halftone (fewer visible dots = finer material)
- Accessories: pocket watches, seal rings, Council pins
- Women: structured blue dresses, pearl-like accents, hair up
- Everything pressed, everything precise — not a wrinkle

**Document style:**
- Heavy cardstock texture (cream with slight warmth)
- Ornate borders — double-line with corner flourishes
- Cursive headers, formal serif body text
- `council-blue` Council seal (embossed appearance via halftone)
- Watermark patterns (The Pattern emblem, faint)
- High ink quality — sharp, clean printing
- Classification: "COUNCIL DIRECTIVE" in `council-blue`

**Named examples:** Dobroslav Vitomirovic (porcelain mask), Aleksandra Moravska-Grod, Vera Kolar (polished composure)

---

### 3B. Bureau / Officials (Blocks B–C)

**Signature color:** `bureau-brown` (#6B5340)
**In-game font:** Serif (Georgia / Times-like — readable, institutional)
**Halftone density:** Medium dots — standard government quality

**Portrait treatment:**
- Solid `bureau-brown` background
- Figure in monochrome halftone
- Medium contrast — competent, measured, unremarkable
- Direct front-facing angle (mugshot-bureaucratic)
- Neutral expressions — trained composure
- Brown clothing tints (coats, blazers, belts)

**Fashion vocabulary:**
- Long brown coats, structured blazers (like Aunt Lydia's severity)
- Bureau pin on lapel (The Pattern emblem)
- Belted overcoats for enforcement roles
- Ties (men), high-collar blouses (women)
- Clipboards, folders, pens in breast pocket
- Marlo's look: brown vest over shirt, sleeves rolled up, loosened collar — official but worn down

**Document style:**
- Standard paper stock (cream, slight grain)
- Clean typed forms — serif headers, monospace form fields
- Single-line borders, form numbers ("Form PA-7", "BPC-ANALYSIS-014")
- `stamp-dark` Bureau seal (circular, The Pattern emblem center)
- "BUREAU OF PATTERN COMPLIANCE" header in `bureau-brown`
- "SILENCE IS ORDER" watermark (subtle)
- Classification strips: solid `bureau-brown` bar with `paper` text
- Footer: "THE PATTERN PROVIDES" in small caps

**Named examples:** Marlo Dashev (brown vest, tired eyes), Dr. Yara Petrovic (lab coat over brown), Henrik Petrov (inspector uniform)

---

### 3C. Workers — by Occupation Sector (Blocks D–F)

Workers are subdivided into 3 occupation colors. The state sorts its labor force by function — your job IS your color.

**In-game font (all workers):** Sans-serif (Helvetica-like — functional, direct)
**Halftone density:** Coarse dots — cheap printing, rough paper

#### Industrial / Technical — `worker-orange` (#C87830)

**Who:** Factory workers, construction, warehouse, water treatment, repair, maintenance, mining

**Portrait treatment:**
- Solid `worker-orange` background
- Coarser halftone dots — less refined printing = lower status
- Weathered faces, strong hands, deeper shadows
- More expressive than Bureau — workers haven't learned to mask emotion

**Fashion:** Heavy coveralls, hard hats, tool belts, rolled sleeves, steel-toed boots. Orange-tinted accents on uniforms. Factory numbered badges. Soot, grease stains suggested by halftone density.

**Named examples:** Miroslav Zelnik (night guard), Tomasz Babic (factory worker), Tomek Radin (construction foreman), Vladan Stojic (water treatment)

#### Commerce / Agriculture — `worker-green` (#3A7A40)

**Who:** Market vendors, food workers, ration depot staff, farmers, agricultural workers

**Portrait treatment:**
- Solid `worker-green` background
- Same coarse halftone as industrial
- Open faces, weather-beaten skin, hands that handle produce

**Fashion:** Aprons, headscarves, market stall smocks, practical layers for outdoor work. Green-tinted cloth accents. Baskets, scales, ration stamps. Flour dust, earth tones.

**Named examples:** Marta Rezek (market vendor — knowing eyes), Sasha Kolar (soap maker), Josip Dragic (cannery worker)

#### Service / Clerical — `worker-amber` (#B89050)

**Who:** Postal workers, couriers, janitors, building monitors, filing clerks, clinic orderlies, nurses, domestic workers

**Portrait treatment:**
- Solid `worker-amber` background
- Same coarse halftone
- Quieter faces, observant eyes — these are the people who see everything

**Fashion:** Postal uniforms, cleaning smocks, simple collared shirts, nurse caps. Amber/tan cloth accents. Messenger bags, mops, clipboards. Worn but clean.

**Named examples:** Nadia Filipovic (postal clerk), Bogdan Zelnik (clinic orderly), Lenka Dashev (nurse — Marlo's wife), Branko Zelnik (retired janitor)

#### All Workers — Document Style

- Rougher paper stock (yellowed, visible fiber texture)
- Hand-filled forms — sans-serif printed headers, handwritten entries
- Simpler borders (single line or none)
- Rubber stamps in `stamp-dark` (slightly offset, smudged edges)
- "WORKER IDENTIFICATION CARD" / "RATION ALLOCATION FORM"
- Occupation color stripe at top of ID cards (orange, green, or amber)
- Condensed sans-serif typography
- Occasional handwritten corrections, crossed-out entries
- Footer: "THE PATTERN PROVIDES" (lighter ink — cheaper printing)

---

### 3D. Unreliable / Block G — ERASED

**Signature color:** `unreliable-gray` (#8A8A8A) — **the absence of color**
**In-game font:** Rough monospace (Courier degraded — typewriter, raw)
**Halftone density:** Very coarse, degraded — photocopy of a photocopy

**The concept:** When a citizen is designated Unreliable and relocated to Block G, they lose their occupation color. The system strips their identity. Their portrait background goes gray. Their clothing loses its color accents. They become *washed out* — like a photograph left in the sun.

**Portrait treatment:**
- Solid `unreliable-gray` background (flat, lifeless)
- Figure in monochrome halftone — most degraded quality
- Highest grain, most visible dot pattern, noise artifacts
- As if photocopied multiple times — degradation = dehumanization
- Faces partially obscured by grain — the system is already erasing them
- **Ghost of original color:** Faintest trace of their former occupation color might bleed through — a visual echo of who they were before the state erased them

**Fashion vocabulary:**
- Mismatched clothing — whatever they could get
- Patched, threadbare, layered for warmth
- NO badges, NO occupation colors — stripped of institutional identity
- Street-worn shoes, hand-mended coats
- Occasional Resistance symbols (broken circle) hidden in folds

**Document style:**
- Cheapest paper (nearly gray, thin)
- Monospace typewriter text — worn ribbon, uneven density
- No ornamental borders
- "RELOCATION ORDER" / "BLOCK G RESIDENCY NOTICE"
- Heavy `stamp-dark` stamps: "UNRELIABLE", "RESTRICTED", "FLAGGED"
- Documents look handled many times — creases, smudges

**Recalibrated returnees:** Unsettlingly clean for Block G — like someone printed a fresh copy where there should be degradation. The photocopy is too crisp. Something is wrong.

---

### 3E. Resistanz — THE FORBIDDEN COLOR

**Signature color:** `resistance-red` (#A02028)
**Not a "class" — the Resistance draws from all classes.** Red is not a background color on ID cards. It's contraband. It's a message.

**Where red appears:**
- Pamphlets — rough printed, monospace text, broken-circle watermark in red
- Graffiti — stenciled broken circle on walls, quick and crude
- Hidden marks — red thread sewn into clothing hems, red ink on the inside of a book cover
- Coded messages — red pen annotations hidden in otherwise normal documents
- Vesna/Sonja's true card — when her Resistance identity is confirmed, a red border appears

**The broken circle:** The "unpattern" — a circle with a gap at bottom-right. Always in `resistance-red`. Hand-drawn, never mechanical. Each one slightly different — because each was drawn by a human, not a machine.

**The power of red:** In a world of blue authority, brown enforcement, and muted occupation colors, red SCREAMS. Seeing red in the game should feel electric and dangerous. Someone is risking their life.

---

## 4. GAME ELEMENT TREATMENTS

### 4A. Suspect Portrait Cards

The primary in-game asset the player interacts with for each citizen.

**Structure:**
- Rectangular card (portrait ratio, ~3:4)
- Solid class-color background (full bleed)
- Monochrome halftone figure: head and shoulders, front-facing
- Name in class-appropriate font at bottom
- Occupation in smaller text
- Block designation (small, corner)
- Thin `ink` border

**Visual progression:** The player learns to "read" background color instantly:
- Orange/green/amber = worker (which job sector)
- Brown = Bureau official
- Blue = Council-connected
- Gray = already condemned
- Red border = Resistance (dangerous)

**The Sonja Reveal:** Vesna Cernak's card starts with `worker-green` background (she poses as a market worker) and sans-serif font. When her identity as a Dragojevic is revealed, the background shifts to `council-blue` + cursive font. The visual shock: a green-backed worker was blue-blood all along.

### 4B. Evidence Document Cards

Each piece of evidence is a document card the player examines on the desk mat.

**Base treatment:**
- `paper` background with halftone grain texture
- Class-appropriate typography and border style matching the issuing institution
- All text rendered in `ink` with halftone dot texture
- Official stamps in `stamp-dark` (slightly misaligned, realistic printing imperfection)
- Hotspot regions (clickable clue areas) subtly indicated by slight ink density change

**Types:**
- **Official Bureau forms:** Serif type, `bureau-brown` headers, clean borders, form numbers
- **Worker documents (ration cards, work permits):** Sans-serif, occupation-color stripe at top, rougher paper
- **Council directives:** Ornate, `council-blue` seal, cursive headers
- **Personal items (letters, notes):** Handwritten style, informal, no borders
- **Resistance materials:** Monospace, degraded, broken-circle watermark in `resistance-red`
- **Newspaper clippings:** `newspaper-yellow` background, bold headline type, column layout, halftone photos

### 4C. The Desk / Mat

**Treatment:**
- Dark warm surface (`#2A2622` — dark brown-gray, like institutional wood)
- Subtle wood grain texture in halftone
- Slightly lighter at center where documents sit (wear pattern)
- Edge shadow/vignette — the desk is the player's confined world
- Lamp light suggested by warmer tone in upper-left quadrant

### 4D. Newspaper (Day Briefing)

**Treatment:**
- `newspaper-yellow` background — aged newsprint stock
- Bold black headline typography (condensed, uppercase)
- Column layout with halftone body text
- Halftone photograph of relevant case outcome (monochrome, coarse dots)
- "THE PATTERN TIMES" masthead with date
- Propaganda sidebar: "COUNCIL REMINDS: SILENCE IS ORDER"
- Wear: fold crease across middle, slight corner curl

### 4E. Overseer Letters

**Treatment:**
- High-quality `paper` (warmer, thicker stock implied by smooth texture)
- Bureau letterhead in `bureau-brown`
- Typewriter-style monospace body text (personal, deliberate)
- Signature: "V. Terzic" in cursive (the only cursive on a Bureau document — subtle class tell)
- After Day 24 reveal: the cursive signature retroactively reads as Council/Elite, not Bureau

### 4F. Family Letters (Lenka)

**Treatment:**
- Soft cream paper, slightly textured (personal stationery, not official)
- Handwritten font — warm, slightly uneven, human
- No borders, no stamps, no institutional markings
- Occasionally: a child's drawing (Eli) in the margin — simple lines, crayon-like color
- The visual opposite of every official document. Pure warmth. No halftone grain — this is the one thing in the game that feels *real*, not printed.

### 4G. UI Elements

**Top bar:** `ink` background, `paper`-colored text, `state-red` alerts
**Buttons:** Flat `ink` with `paper` text, halftone hover state
**Card hand area:** Slight `paper` tint behind cards
**Notebook:** Cream pages, `ink` text, tab dividers in class/occupation colors
**Verdict form:** Heavy `bureau-brown` border, multiple `stamp-dark` stamp zones, `resistance-red` glow on moral-choice elements

### 4H. The Pattern Branding

**The Pattern emblem:** A circle containing the letter P — rendered differently per context:
- **Official (Council):** Clean, precise, `council-blue` — authority
- **Official (Bureau):** Precise, `bureau-brown` or `stamp-dark` — enforcement
- **Propaganda:** Bold, angular, constructivist influence — printed large on posters
- **Worker context:** Simplified, worn, sometimes incomplete — cheap reproduction
- **Resistance subversion:** The broken circle (the "unpattern") — circle with a gap, in `resistance-red`

---

## 5. HALFTONE TECHNICAL SPECIFICATIONS

### For Generated Assets (halftone_doc.py / future pipeline)

**Halftone dot patterns:**
- Elite: 85 LPI (lines per inch equivalent) — fine, almost invisible dots
- Bureau: 65 LPI — standard newspaper quality
- Worker: 45 LPI — coarse, clearly visible dots
- Unreliable: 30 LPI — photocopy-of-photocopy quality, heavy grain

**Implementation approach:**
- Base images rendered at target resolution
- Halftone filter applied as post-process: convert continuous tone to dot pattern
- Dot shape: circular (classic halftone)
- Slight rotation per color layer (15° offset) to simulate CMYK misregistration
- Optional: slight position offset between layers for "registration error" feel

**Asset resolution targets:**
- Portrait cards: 256x340 (final display), 512x680 (source)
- Evidence documents: 400x560 (final display), 800x1120 (source)
- Newspaper: 600x800 (final display)
- All assets nearest-neighbor upscaled for crisp dot edges in Unity

### For AI-Generated Art (manifest.py prompts)

**Prompt prefix:** `"halftone duotone print, risograph style, limited color palette, visible dot pattern, vintage print aesthetic, monochromatic figure on solid color background, 1970s Eastern European, institutional, ink on paper texture, no smooth gradients, screen-printed look"`

**Per-class modifiers:**
- Council: `"fine halftone, high contrast, ornate, formal, aristocratic, royal blue background"`
- Bureau: `"medium halftone, measured, institutional, authoritarian, muted brown background"`
- Worker (Industrial): `"coarse halftone, weathered, rough, labor-worn, burnt orange background"`
- Worker (Commerce): `"coarse halftone, market vendor, earth-worn, deep green background"`
- Worker (Service): `"coarse halftone, quiet, observant, amber-tan background"`
- Unreliable: `"degraded halftone, photocopied, washed out, grainy, neutral gray background"`

---

## 6. ANIMATION PRINCIPLES

**Core feel:** Stop-motion / puppet paper — not fluid, not digital. Things move in *steps*.

**Character animations:**
- Idle: Subtle breathing — slight chest rise/fall, 2-3 frame loop
- Talking: Mouth opens/closes in 2 frames (open/closed), not lip-sync
- Emotion: Head tilt, arm gesture — each a 3-4 frame animation
- Entry/exit: Slide in from side, paper-puppet style (like a cutout being pushed on stage)

**Document animations:**
- Cards slide with slight paper friction
- Stamp application: quick slam with slight bounce and ink splatter particles
- Page turn: 3-frame flip, paper texture visible on edge
- Drag: Document follows with slight angular lag (paper has weight)

**Transitions:**
- Scene transitions: horizontal wipe with halftone dot leading edge (like ink spreading)
- Day start: newspaper unfolds (3-4 frame animation)
- Day end: desk lamp dims, shadows darken in steps

**What to avoid:**
- No tweening/easing that feels digital
- No particle effects that feel like a game engine
- No UI animations that break the "printed material" metaphor

---

## 7. RESISTANCE VISUAL LANGUAGE

Fully defined in Section 3E above. Key summary:

**Color:** `resistance-red` (#A02028) — blood, fire, defiance. The most dangerous color in Drazhovia.
**Symbol:** Broken circle (gap at bottom-right) — the "unpattern." Always hand-drawn.
**Typography:** Hand-drawn, uneven, deliberately anti-institutional
**Materials:** Hand-made — letterpress, woodblock print, rough edges
**Pamphlets:** Monospace text on rough paper, broken-circle watermark in red
**Graffiti:** Stenciled broken circle, crude, quick — done at night, done in fear

The Resistance aesthetic is the **negative space** of the state's aesthetic. Where the state uses blue (nobility) and brown (enforcement), the Resistance uses red (the people). Where the state prints with machines, the Resistance stamps by hand. Both are "printed," but one is a press and the other is a fist.

---

## 8. BLOCK VISUAL GRADIENT

The Blocks (A through G) create a visual spectrum from polished to decayed. This doesn't introduce new colors — it modulates the existing class palette through texture and quality.

| Block | Print Quality | Paper Quality | Ink Density | Feel |
|---|---|---|---|---|
| Block A | Crisp, fine halftone | Smooth, warm cream | Full, sharp | "Expensive printing" |
| Block B-C | Standard halftone | Standard paper | Consistent | "Government quality" |
| Block D-E | Coarse halftone | Slightly yellowed | Occasionally thin | "Budget printing" |
| Block F | Very coarse | Clearly yellowed | Uneven | "Cheap and worn" |
| Block G | Degraded, noisy | Nearly gray | Smudged | "Photocopy of a photocopy" |

This gradient tells a story: the state spends more ink on people it values and less on people it's discarding.

---

## 9. COLOR SEMANTICS SUMMARY

| Color | Means | Where It Appears |
|---|---|---|
| `council-blue` | Royalty, nobility, cold power | Elite portraits, Council seals, directives, blue clothing |
| `bureau-brown` | Authority, enforcement, institution | Official portraits, Bureau stamps, form headers, brown coats |
| `worker-orange` | Industrial labor, technical work | Factory worker portraits, industrial ID cards |
| `worker-green` | Commerce, agriculture, food | Market vendor portraits, ration depot forms |
| `worker-amber` | Service, clerical, domestic | Postal/service portraits, clinic forms |
| `unreliable-gray` | Erasure, loss of identity | Block G portraits (washed out), relocation orders |
| `resistance-red` | Defiance, blood, the people's fire | Pamphlets, broken circle, graffiti, hidden marks |
| `stamp-dark` | Official authority (mechanical) | Bureau seals, Council stamps, "DEVIATION" marks |
| `paper` | Neutrality, the desk, the world | Base background everywhere |
| `ink` | Information, text, the printed word | All text, all halftone patterns |

---

## 10. ASSET PRODUCTION CHECKLIST

For each case, the art pipeline needs:

- [ ] 2-4 **suspect portrait cards** (monochrome halftone on class-color background)
- [ ] 3-8 **evidence document cards** (class-appropriate typography/paper/stamps)
- [ ] 1 **newspaper article** (halftone photo, column layout, headline)
- [ ] 0-1 **Overseer letter** (typewriter text, Bureau letterhead)
- [ ] 0-1 **family letter** (handwritten, warm, no institutional markers)
- [ ] 0-2 **tool-specific assets** (spectrograph readouts, fingerprint cards, computer files)

Total per case: ~8-15 unique art assets.
Total for 30 days (24 core + 10 secondary): ~300-500 assets.

---

## 11. VISUAL SAMPLES TO GENERATE

To validate the art direction before committing, produce one sample of each major asset type.

### Sample 1: Suspect Portrait Cards (Midjourney)

Generate 4 portraits showing the class system in action. Paste these prompts into Midjourney:

**Council suspect — Vera Kolar (Council Aide):**
```
Halftone duotone print portrait, head and shoulders, woman in her 40s, severe pulled-back hair, high-collar blue coat, composed expression, monochromatic figure with visible halftone dot texture, solid royal blue background #2D4B8E, vintage 1970s Eastern European feel, risograph print style, no smooth gradients, ink on paper texture, institutional mugshot framing --ar 3:4 --style raw --s 200
```

**Bureau suspect — Marlo Dashev (Pattern Analyst):**
```
Halftone duotone print portrait, head and shoulders, man in his mid-30s, tired eyes, brown vest over white shirt, loosened collar, ink-stained fingers, monochromatic figure with visible halftone dot texture, solid muted brown background #6B5340, vintage 1970s Eastern European bureaucrat, risograph print style, no smooth gradients --ar 3:4 --style raw --s 200
```

**Worker suspect — Miroslav Zelnik (Night Guard, Industrial):**
```
Halftone duotone print portrait, head and shoulders, heavy-set man in his 50s, nervous expression, work uniform with numbered badge, rough weathered face, coarse visible halftone dots, solid burnt orange background #C87830, 1970s Eastern European factory worker, risograph print style, coarse newspaper print quality --ar 3:4 --style raw --s 200
```

**Unreliable / Block G citizen:**
```
Halftone duotone print portrait, head and shoulders, gaunt person in mismatched worn clothing, hollow eyes, heavily degraded print quality, extreme halftone grain and noise, washed out faded appearance like a bad photocopy, solid neutral gray background #8A8A8A, 1970s Eastern European, identity erased by state bureaucracy --ar 3:4 --style raw --s 200
```

### Sample 2: Bureau Evidence Document — `halftone_doc.py`

Generate a Bureau evidence document — the Block C Depot Access Log from Case 1.

### Sample 3: Newspaper — `halftone_newspaper.py`

Generate Day 2's newspaper — "THE PATTERN TIMES".

### Sample 4: Family Letter — `halftone_letter.py`

Generate Lenka's Day 1 letter to Marlo.

### Sample 5: Overseer Letter — `halftone_overseer.py`

Generate Terzic's Day 4 commendation letter.

### Sample 6: Worker ID Card — `halftone_id_card.py`

Generate Miroslav Zelnik's worker ID card.

---

## VERIFICATION CHECKLIST

After samples are generated:

1. [ ] Review suspect portraits (Midjourney) against the "Wake Up Dead Man" poster reference
2. [ ] Review documents (Pillow) for consistent palette and halftone feel
3. [ ] Check that all class colors are distinct and immediately readable side by side
4. [ ] Verify the newspaper, letters, and documents each feel like different "materials"
5. [ ] Confirm Lenka's letter feels warm and human vs. the cold institutional documents
6. [ ] Get user approval on the visual direction before updating the pipeline tools
