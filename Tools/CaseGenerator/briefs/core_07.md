# Case Brief: core_07 — "The Librarian's Books"

## Meta
- **Type:** Core
- **Available Day:** 8
- **Sequence:** 7
- **Reward:** 85
- **Complexity:** Medium
- **Required Tools:** Document reading, Fingerprint
- **Deviation:** Accumulation (§2) — Possessing banned materials
- **Involves Resistance:** No

## Story Context
Knowledge as crime. A state librarian hid 14 pre-Collapse philosophy books behind a false panel. The books are Plato, Descartes, Marcus Aurelius — not bomb manuals. He asks: "Have you ever read Plato, analyst?" Escalates the moral discomfort of Act 2 — the system criminalizes thought itself.

## Synopsis
14 banned pre-Collapse philosophy books were found behind a false panel in state librarian Luka Kolar's apartment during a routine building inspection. Fingerprint analysis confirms he handled them regularly. His neighbor, retired seamstress Vesna Tomic, reported seeing candlelight late at night — he was reading by candle to avoid the electricity monitor flagging after-hours activity. His crime is wanting to think.

## Suspects
- **Luka Kolar** — State Librarian. Guilty. Calm, intelligent, resigned to his fate. Lies initially about knowing the books were there, but once caught, speaks eloquently about why knowledge matters. Professional-class speech with literary vocabulary hinting at deeper education.
- **Vesna Tomic** — Retired Seamstress. Innocent (witness). Elderly, observant, reported the candlelight out of building monitor habit. Slightly guilty about it. Worker-class speech.

## Evidence
- **Contraband Book Inventory** — Type: Document. List of 14 books with titles and conditions. Hotspot: book titles are all philosophy (clue_philosophy_titles). Hotspot: condition notes show heavy use — dog-eared, annotated (clue_heavy_use).
- **Apartment Inspection Report** — Type: Document. False panel description. Hotspot: panel was recently modified (clue_recent_modification). Hotspot: candle wax residue near reading spot (clue_candle_wax).
- **Fingerprint Evidence Card** — Type: Item. Prints lifted from book covers. Hotspot: prints match Luka (clue_luka_prints). foreignSubstance: None. (Fingerprint tool can be used.)

## Key Clues (discovery chain)
1. clue_philosophy_titles — These aren't bomb manuals. They're Plato and Descartes.
2. clue_heavy_use — Someone has been reading these extensively.
3. clue_recent_modification — The hiding spot was built recently, not by a previous tenant.
4. clue_candle_wax — Someone reads by candlelight (to avoid electric light being noticed).
5. clue_luka_prints — Fingerprints on books match Luka. Definitive proof.
6. clue_luka_motive — From interrogation clickable. Luka explains why knowledge matters.
7. clue_vesna_observation — From interrogation clickable. Vesna describes what she saw.

## Interrogation Flow
### Luka Kolar
- Tag: clue_philosophy_titles → Lie. "I don't know anything about those books. I work at the state library — I know which books are approved." Evasive.
- Tag: clue_heavy_use → Lie. "If someone used them, it wasn't me. They must have been there before I moved in." Evasive.
  - Contradiction: Present clue_recent_modification → "The... the panel. I..." Emotional. Breaks.
- Tag: clue_recent_modification → Lie (if asked before contradiction). "The apartment had issues when I moved in. I did some repairs." Evasive.
- Tag: clue_luka_prints → Truth (forced). "Yes. They're mine." Long pause. "Have you ever read Plato, analyst?" Normal. → clue_luka_motive (clickable: "read Plato")
- Tag: clue_candle_wax → Truth. "I read by candle because electric light would show under the door. I'm not stupid — just careful." Emotional.
- Tag: clue_luka_motive → Truth. "The Pattern tells us what to think. These books teach us HOW to think. That's the real crime, isn't it?" Emotional.
  - High stress: "Go ahead. Send me to Recalibration. At least I'll have read something real before I disappear." Hostile.

### Vesna Tomic
- Tag: clue_philosophy_titles → Truth (limited). "I didn't know what he was reading. Just that he was reading." Normal.
- Tag: clue_candle_wax → Truth. "I saw light under his door late at night. Not electric — flickering. Like a candle." Normal. → clue_vesna_observation (clickable: "light under his door")
- Tag: clue_vesna_observation → Truth. "I told the building monitor. It's what we're supposed to do. I didn't want him to get in trouble..." Emotional.
- Tag: clue_luka_prints → Truth (limited). "I never touched the books. I don't read much anymore." Normal.

## Verdict
- **Correct answer:** Luka Kolar committed Deviation: Accumulation
- **Accepted violations:** accumulation, contraband, possession_of_banned_materials
- **Partial credit:** None
- **Min confidence:** 80

## Resistance Choice
Not applicable — but Luka's words plant a seed. Knowledge as crime. The Pattern as thought control.

## Narrative Hooks
- "Have you ever read Plato?" is a direct challenge to the player
- The banned philosophy books connect to the Day 25 Confession Tape (Pattern as manufactured control)
- Vesna's guilt about reporting humanizes the informant culture
- Fingerprint evidence introduces the mechanic for later, harder cases
- Luka may connect to Resistanz intellectual network in later cases
