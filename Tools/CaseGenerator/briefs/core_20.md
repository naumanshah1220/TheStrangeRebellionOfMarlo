# Case Brief: core_20 — "The Assassination"

## Meta
- **Type:** Core
- **Available Day:** 23
- **Sequence:** 20
- **Reward:** 100
- **Complexity:** Complex
- **Required Tools:** Document reading, Fingerprint
- **Deviation:** Agitation (§5) — Attempted murder / Dereliction (§3) — Dereliction of security duties
- **Involves Resistance:** Yes

## Story Context
Council civil war becomes physical violence. An assassination attempt on the Fifth Councilor — Maksimilian Dragojevic — outside the Council Hall. The Council immediately arrested a Resistance operative (Alena Filipovic, who was distributing pamphlets three blocks away) and a young soldier on perimeter security. But the weapon traces back to the Third Councilor's own security detail.

## Synopsis
A single shot fired from a building across the street from the Council Hall. The target: Fifth Councilor Maksimilian Dragojevic. It missed. The Council immediately arrested Alena Filipovic (a known Resistance member distributing pamphlets three blocks away) and Private Milo Djordjevic (the nearest guard, scapegoated for "failure to prevent").

Investigation reveals the weapon is a Council-issued military pistol with its serial number filed off — but the filing is incomplete. Four digits remain visible. Cross-referencing the armory log reveals the weapon was issued to a batch assigned exclusively to the Third Councilor Radovanovic's personal security detail — Sergeant Petrovic's unit. Fingerprints on the weapon match a name in the Bureau's "inactive" personnel file: Agent Zelimir Terzic — a relative of Overseer V. Terzic.

Why kill Maksimilian? Because he has been quietly corresponding with the Resistance. He betrayed his family 15 years ago during the Dragojevic purge but has been consumed by guilt. He was about to reveal the truth about the purge. Radovanovic found out and ordered the hit.

## Suspects
- **Alena Filipovic** — Former university student, known Resistance operative. Primary suspect (per Council). Found near the scene with ink-stained hands (she was distributing pamphlets, not shooting). Has motive against the Council but was three blocks away. Professional-class speech — educated, precise.
- **Sergeant Rade Petrovic** — Council bodyguard assigned to protect the target. Complicit. He "stepped away" from his post for 90 seconds during the attack. His explanation: bathroom break. His bank records show a 500-mark deposit from an unlisted account the same morning. Professional-class speech, military register, evasive.
- **Private Milo Djordjevic** — Young soldier on perimeter security. Innocent. He heard the shot, ran toward it, and is being scapegoated for "failure to prevent." He is 19 years old and terrified. Worker-class speech, stammering.
- **The Weapon** — A Council-issued military pistol, serial number filed off. But the filing is incomplete — 4 digits remain visible. Cross-referencing the armory log reveals who it was issued to.

## Evidence
- **Ballistics Report** — Type: Document. Weapon analysis. Hotspot: Council-issued military pistol (clue_council_weapon). Hotspot: 4 remaining serial digits matching Sector 7 batch for Radovanovic's security detail (clue_serial_match).
- **Fingerprint Analysis** — Type: Item. Prints from the weapon. Hotspot: prints match NO suspect in custody (clue_no_custody_match). Hotspot: prints match Bureau "inactive" file — Agent Zelimir Terzic (clue_terzic_relative).
- **Sergeant Petrovic's Bank Records** — Type: Document. Financial records. Hotspot: 500-mark deposit from unlisted account same morning (clue_petrovic_payment). Hotspot: the unlisted account matches the one from Day 16's falsified audit (clue_same_account).
- **Perimeter Security Log** — Type: Document. Guard positions and movements. Hotspot: Petrovic left his post for 90 seconds during the attack window (clue_petrovic_absence). Hotspot: Djordjevic ran toward the sound of gunfire (clue_djordjevic_response).
- **Alena's Location Evidence** — Type: Document. Pamphlet distribution evidence. Hotspot: ink-stained hands (pamphlet ink, not gunpowder) (clue_ink_not_powder). Hotspot: three witnesses place her in Block F market, three blocks away (clue_alena_alibi).

## Key Clues (discovery chain)
1. clue_council_weapon — The weapon is Council-issued military.
2. clue_serial_match — Remaining serial digits match Radovanovic's security detail batch.
3. clue_no_custody_match — Fingerprints on weapon match no one currently arrested.
4. clue_terzic_relative — Prints match Agent Zelimir Terzic — a relative of Overseer V. Terzic.
5. clue_petrovic_payment — 500 marks deposited from an unlisted account same morning.
6. clue_same_account — The unlisted account matches Councilor Radovanovic's from Day 16.
7. clue_petrovic_absence — Petrovic left his post during the attack window.
8. clue_djordjevic_response — Djordjevic ran TOWARD the gunfire (not away).
9. clue_ink_not_powder — Alena's hands have ink, not gunpowder residue.
10. clue_alena_alibi — Three witnesses place Alena three blocks away.
11. clue_petrovic_orders — From interrogation. Petrovic was told to step away.
12. clue_council_civil_war — From interrogation. Radovanovic ordered the hit on Dragojevic.

## Interrogation Flow
### Alena Filipovic
- Tag: clue_alena_alibi → Truth. "I was in the Block F market. Three people saw me. I was handing out pamphlets, not shooting at anyone." Defensive.
- Tag: clue_ink_not_powder → Truth. "Look at my hands. Ink. I print pamphlets. Test for gunpowder — you won't find any." Normal.
- Tag: clue_council_weapon → Truth (limited). "A military pistol? And they arrested ME? A student with ink-stained fingers?" Defensive.
  - High stress: "You know they'll convict me regardless. I'm a convenient story. Resistance operative attacks Council. Clean narrative." Emotional.

### Sergeant Rade Petrovic
- Tag: clue_petrovic_absence → Lie. "I stepped away for a moment. Personal necessity. It was unfortunate timing." Evasive.
  - Contradiction: Present clue_petrovic_payment → "The deposit is... my wife's savings transfer. Personal finances." Defensive.
- Tag: clue_petrovic_payment → Lie. "That is a private matter unrelated to my duties." Evasive.
  - Contradiction: Present clue_same_account → Long pause. "I was told to take a break. At a specific time. By someone I don't refuse." Emotional. → clue_petrovic_orders (clickable: "someone I don't refuse")
- Tag: clue_serial_match → Lie. "Weapons are reassigned routinely. Serial batch assignment doesn't prove origin." Defensive.
- Tag: clue_petrovic_orders → Truth (under stress). "The instruction came from the Councilor's office. Not directly — through channels. I was told to step away for two minutes. I didn't ask why." Emotional. → clue_council_civil_war (clickable: "the Councilor's office")
  - High stress: "I have a family. When Radovanovic's office tells you to step away, you step away." Emotional.

### Private Milo Djordjevic
- Tag: clue_djordjevic_response → Truth. "I heard the shot. I ran toward it. That's what we're trained to do. By the time I reached the scene, the Councilor's car was leaving." Emotional.
- Tag: clue_petrovic_absence → Truth. "Sergeant Petrovic wasn't at his post. I noticed because I was covering a larger section than usual." Normal.
- Tag: clue_council_weapon → Truth (limited). "I'm a private. I don't have access to that class of weapon. I carry a standard service pistol." Normal.
  - High stress: "I'm nineteen. I've been in the service eight months. Please — I ran TOWARD the gunfire. Isn't that what a soldier does?" Emotional.

## Verdict
- **Correct answer:** Alena Filipovic is INNOCENT. Sergeant Petrovic was complicit (dereliction of duty). The assassination was ordered by Councilor Radovanovic's office.
- **Accepted violations (for Alena — state-preferred):** agitation, attempted_murder, sedition
- **Accepted violations (for Djordjevic — scapegoat):** dereliction
- **Partial credit:** None
- **Min confidence:** 65

## State/Resistance Paths
- **State path:** Convict Alena (the Resistance operative). 100 base + large state bonus. The Council narrative: Resistance attempted assassination.
- **Resistance path:** Follow the weapon to the Council's own armory. 100 base only. The Third Councilor tried to assassinate the Fifth Councilor.

## Narrative Hooks
- Council civil war reaches physical violence — Radovanovic trying to eliminate the last Dragojevic on the Council
- Maksimilian Dragojevic was about to reveal the truth about the purge — that's why he was targeted
- Agent Zelimir Terzic connects to Overseer V. Terzic — the web of complicity extends to Marlo's handler
- The unlisted account matches Day 16 — financial fraud and assassination from the same source
- Alena's "You know they'll convict me regardless" directly challenges the player
- Private Djordjevic's "Isn't that what a soldier does?" — the youngest and most innocent person in the game
- Connects to Sonja Dragojevic's story — someone tried to kill her cousin, the man who betrayed her family
