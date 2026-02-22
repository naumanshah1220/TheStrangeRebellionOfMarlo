# Case Brief: core_15 — "The Councilor's Nephew"

## Meta
- **Type:** Core
- **Available Day:** 17
- **Sequence:** 15
- **Reward:** 100 (+ 150 bribe for dropping case)
- **Complexity:** Complex
- **Required Tools:** Document reading
- **Deviation:** Accumulation (§2) — Theft of medical supplies
- **Involves Resistance:** Yes

## Story Context
First direct financial bribe from the system. A Radovanovic family member stole medical supplies. The Overseer offers 150 marks to make the case disappear. 150 marks is enormous — nearly a week of survival for Marlo's family.

If Danilo was convicted on Day 15, this is a DIFFERENT relative (Filip Radovanovic, a cousin). If acquitted, Danilo appears again.

## Synopsis
Three crates of medical supplies stolen from a government depot. Security camera and witness statement identify a Radovanovic family member. The Overseer's letter: "This case involves a citizen whose family has rendered extraordinary service to the Collective. A reassignment bonus of 150 marks is available for cases redirected to the minor offenses clerk." Translation: make it disappear.

## Suspects
- **Danilo Radovanovic** (or **Filip Radovanovic** if Danilo was convicted on Day 15) — Council family member. Guilty. Arrogant, entitled, expects this to disappear. "My uncle will sort this out." Elite-adjacent speech.
- **Pavel Zoric** — Security Guard. Innocent (witness). Scared but honest. Identified the thief but terrified of retaliation. Worker-class speech.

## Evidence
- **Security Still Image** — Type: Photo. Figure matching the suspect at depot at 2 AM. Hotspot: face partially visible (clue_security_image). Hotspot: distinctive watch on wrist (clue_distinctive_watch).
- **Witness Statement** — Type: Document. Pavel's account. Hotspot: physical description (clue_witness_description). Hotspot: suspect said "Do you know who my uncle is?" (clue_uncle_reference).
- **Overseer Reassignment Letter** — Type: Document. The bribe. Hotspot: 150-mark bonus offer (clue_bribe_offer). Hotspot: "extraordinary service to the Collective" language (clue_dismissal_language).

## Key Clues (discovery chain)
1. clue_security_image — Figure at depot matches suspect.
2. clue_distinctive_watch — Expensive watch; rare in Drazhovia.
3. clue_witness_description — Pavel's description matches suspect.
4. clue_uncle_reference — "Do you know who my uncle is?" — Council connection.
5. clue_bribe_offer — 150 marks to make it go away.
6. clue_dismissal_language — Overseer minimizing the theft.
7. clue_danilo_admission — From interrogation. Casual admission of theft.
8. clue_pavel_fear — From interrogation. Pavel's fear of retaliation.

## Interrogation Flow
### Danilo/Filip Radovanovic
- Tag: clue_security_image → Lie. "That could be anyone. The image is blurry." Evasive.
- Tag: clue_distinctive_watch → Truth (arrogant). "It's a Meridian chronograph. My uncle gave it to me. What of it?" Normal.
- Tag: clue_witness_description → Lie. "Your guard is mistaken." Evasive.
  - Contradiction: Present clue_uncle_reference → "Fine, I was there. Three crates of bandages? My uncle will sort this out." Normal. → clue_danilo_admission (clickable: "I was there")
- Tag: clue_bribe_offer → Truth (surprised). "Oh, they sent you a letter? Good. So you know this is a waste of your time." Normal.
  - High stress: "You know what happens to analysts who make the wrong enemies." Hostile.

### Pavel Zoric
- Tag: clue_security_image → Truth. "That's the man I saw. He walked right past me." Normal.
- Tag: clue_uncle_reference → Truth. "He said those exact words. 'Do you know who my uncle is?'" Emotional. → clue_pavel_fear (clickable: "almost let him go")
- Tag: clue_pavel_fear → Truth. "I'm scared. If his uncle is who I think he is... I have a family, analyst." Emotional.

## Verdict
- **Correct answer:** Danilo/Filip Radovanovic committed Deviation: Accumulation
- **Accepted violations:** accumulation, theft, misappropriation
- **Partial credit:** None
- **Min confidence:** 75

## State/Resistance Paths
- **State path:** Reassign case. 150 marks bribe. Council Favor increases.
- **Justice path:** Convict the nephew. 100 base only. Potential consequences.

## Narrative Hooks
- First explicit bribe with real financial impact
- 150 marks is nearly a week of family survival
- "Do you know who my uncle is?" — entitled immunity
- Whether Marlo takes the bribe affects future case difficulty
- Pavel's fear humanizes the cost of standing up to power
- Links to Councilor Radovanovic's corruption arc
