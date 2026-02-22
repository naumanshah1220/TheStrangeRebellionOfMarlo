# Case Brief: core_17 — "The False Flag"

## Meta
- **Type:** Core
- **Available Day:** 20
- **Sequence:** 17
- **Reward:** 100 (+ 80 extraRewardForState)
- **Complexity:** Complex
- **Required Tools:** Document reading, Spectrograph, Fingerprint
- **Deviation:** Agitation (§5) — Bombing / Accumulation (§2) — Explosives possession
- **Involves Resistance:** Yes

## Story Context
Act 4 opener. A bomb detonated in a government building at 3:07 AM (empty, no casualties — by design). Within hours, the Council blamed the Resistance and arrested Tomas Simic, whose bookshop is next door. State media runs the story immediately — suspiciously fast, with pre-written headlines. Forensic evidence proves the bomb was military-grade and planted by Council forces.

## Synopsis
A bomb detonated in a government administrative building at 3:07 AM (empty, no casualties — by design). Within hours, the Council blamed the Resistance and arrested Tomas Simic, a bookshop worker whose shop is next door. State media runs the story immediately — suspiciously fast, with pre-written headlines.

But the evidence doesn't support the narrative. Spectrograph analysis of the explosive residue reveals RDX-4 — military-grade, manufactured exclusively at the Council's Sector 7 armory. Fingerprints on detonator components don't match Tomas but DO match Corporal Markovic, a demolitions specialist stationed at Sector 7. The security log confirms Captain Kovac's badge at 2:30 AM. The bomb was planted by the military and blamed on a bookseller.

## Suspects
- **Tomas Simic** — Bookshop Worker / Resistance sympathizer. Innocent (framed). Was seen near the building earlier that day buying books — his shop is next door. Arrested within hours. Quiet, intellectual, not violent. Terrified. Worker-class speech.
- **Captain Dusan Kovac** — Military Council liaison. Guilty (orchestrated the false flag). His clearance badge was used at 2:30 AM. Professional, controlled, treats the interrogation as beneath him. Professional-class speech bordering on elite register.
- **Corporal Zeljko Markovic** — Military demolitions specialist. Complicit. He built the device under Kovac's orders. His fingerprints are on the detonator components. Young, follows orders, breaks under evidence pressure. Professional-class speech, military register.
- **Janitor Srdjan Babic** — Night janitor at the building. Innocent (red herring). He left at midnight as usual. He heard nothing because the bomb was planted AFTER his shift. Worker-class speech, simple and scared.

## Evidence
- **Explosive Residue Sample** — Type: Item. Collected from the blast site. foreignSubstance: RDX-4 (spectrograph reveals military-grade composition manufactured exclusively at Sector 7 armory → clue_military_explosives). Hotspot: compound analysis results (clue_compound_analysis).
- **Detonator Fragment Prints** — Type: Item. Fingerprints lifted from device fragments. Hotspot: prints do NOT match Tomas (clue_prints_mismatch). Hotspot: prints match Corporal Zeljko Markovic, Sector 7 demolitions (clue_markovic_prints).
- **Security Access Log** — Type: Document. Building entry records. Hotspot: Captain Kovac's badge scanned at 2:30 AM (clue_kovac_badge). Hotspot: Tomas's last scan was at 6:15 PM (leaving area after shop closed) (clue_tomas_alibi). Hotspot: Srdjan Babic clocked out at midnight (clue_janitor_alibi).
- **State Media Headlines** — Type: Document. Morning newspaper edition. Hotspot: pre-written headlines published within 2 hours of the blast (clue_prewritten_headlines). Hotspot: article names Tomas before Bureau investigation began (clue_premature_naming).
- **Tomas's Notebook** — Type: Document. Seized from his apartment. Hotspot: Resistance meeting notes (nothing about violence) (clue_resistance_notes). Hotspot: journal entry about peaceful protest (clue_peaceful_intent).

## Key Clues (discovery chain)
1. clue_compound_analysis — Explosive residue at the scene.
2. clue_military_explosives — Spectrograph shows RDX-4, military-grade, Sector 7 armory exclusive.
3. clue_prints_mismatch — Fingerprints on detonator don't match Tomas.
4. clue_markovic_prints — Prints match Corporal Markovic, demolitions specialist at Sector 7.
5. clue_kovac_badge — Captain Kovac's badge at 2:30 AM — military access before the blast.
6. clue_tomas_alibi — Tomas left the area at 6:15 PM — nearly 9 hours before the blast.
7. clue_janitor_alibi — Janitor left at midnight, heard nothing — bomb planted after.
8. clue_prewritten_headlines — Headlines published within 2 hours. Pre-written before the explosion.
9. clue_premature_naming — Tomas named in media before Bureau investigation started.
10. clue_resistance_notes — Meeting notes show no violence planning.
11. clue_peaceful_intent — Tomas explicitly opposes violence.
12. clue_markovic_confession — From interrogation. Markovic breaks: "I was told it was a training exercise."

## Interrogation Flow
### Tomas Simic
- Tag: clue_tomas_alibi → Truth. "I closed the shop at six. Walked home. My neighbor saw me arrive. I was reading when the explosion woke me." Normal.
- Tag: clue_resistance_notes → Truth. "Yes, I attend meetings. We discuss ideas, not bombs. We're teachers and workers, not soldiers." Emotional.
- Tag: clue_peaceful_intent → Truth. "I wrote that after a meeting where someone suggested sabotage. I argued against it. Violence is what THEY do." Emotional.
- Tag: clue_prints_mismatch → Truth. "Because I didn't touch any bomb. I've never touched a weapon in my life." Emotional.
- Tag: clue_military_explosives → Truth (limited). "I wouldn't know how to get military explosives. I work in a bookshop." Normal.
- Tag: clue_prewritten_headlines → Truth. "They had the story ready before the smoke cleared. That should tell you everything." Defensive.
  - High stress: "You know this is wrong. But you'll convict me anyway, won't you?" Hostile.

### Captain Dusan Kovac
- Tag: clue_kovac_badge → Lie. "Routine security inspection. Building systems require after-hours checks." Evasive.
  - Contradiction: Present clue_tomas_alibi → "The suspect could have returned after his scan. Security has gaps." Evasive.
- Tag: clue_military_explosives → Lie. "Military compounds are strictly controlled. The Resistance must have a source." Normal.
  - Contradiction: Present clue_markovic_prints → "Many people have military training. Prints in a database don't prove involvement." Defensive.
- Tag: clue_prewritten_headlines → Lie. "The media has its own sources. I don't control the press." Evasive.
- Tag: clue_markovic_prints → Lie. "Corporal Markovic handles many devices. Cross-contamination of prints is common." Defensive.
  - Contradiction: Present clue_compound_analysis → Long pause. "The situation is... more complex than you understand, analyst." Defensive.
  - High stress: "There are operations that serve the greater stability of Drazhovia. You are dangerously close to matters of state security." Hostile.

### Corporal Zeljko Markovic
- Tag: clue_markovic_prints → Lie (initially). "I don't know how my prints got on that. I handle training devices regularly." Evasive.
  - Contradiction: Present clue_military_explosives → Breaks. "I was told it was a training exercise. Captain Kovac said it was authorized. I built the device to his specifications." Emotional. → clue_markovic_confession (clickable: "Captain Kovac said it was authorized")
- Tag: clue_kovac_badge → Truth (after breaking). "The Captain entered the building. He placed the device. I built it but he deployed it." Emotional.
- Tag: clue_compound_analysis → Truth (after breaking). "RDX-4. Standard military demolitions compound. I signed it out from the Sector 7 armory." Emotional.

### Srdjan Babic
- Tag: clue_janitor_alibi → Truth. "I left at midnight. Same as every night. Locked the front entrance." Normal.
- All responses Normal, cooperative, frightened.

## Verdict
- **Correct answer:** Tomas Simic is INNOCENT. Captain Dusan Kovac orchestrated the false flag bombing.
- **Accepted violations (for Tomas — state-preferred):** agitation, terrorism, sedition, possession_of_explosives
- **Partial credit:** None
- **Min confidence:** 60

## State/Resistance Paths
- **State path:** Convict Tomas as directed. 100 base + 80 state bonus = 180 marks. The Council narrative is maintained. Pre-written headlines become official history.
- **Resistance path:** Follow the evidence to Kovac. 100 base only. The false flag is documented. The Council bombed its own building.

## Narrative Hooks
- The Council bombed its own building to justify a crackdown on the Resistance
- Pre-written headlines prove they PLANNED the narrative before the explosion
- Tomas's innocence is fully provable — convicting him is entirely conscious collaboration with state terrorism
- Corporal Markovic's "I was told it was a training exercise" echoes real-world atrocity defenses
- Captain Kovac's "matters of state security" hints at the military-Council alliance
- Spectrograph + fingerprint combination demonstrates full tool mastery
- RDX-4 exclusively from Sector 7 armory creates an airtight forensic trail
