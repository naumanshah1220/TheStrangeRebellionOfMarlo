# Case Brief: core_11 — "The Dead Witness"

## Meta
- **Type:** Core
- **Available Day:** 13
- **Sequence:** 11
- **Reward:** 90 (+ 55 extraRewardForState)
- **Complexity:** Complex
- **Required Tools:** Document reading, Fingerprint
- **Deviation:** Agitation (§5) — Homicide / Dereliction (§3) — Obstruction of investigation
- **Involves Resistance:** Yes
- **Moral Dilemma:** Political — ruling homicide vs. "accident"

## Story Context
ACT 3 OPENER — THE ESCALATION. A witness from a prior case is dead. Official report: fell down a factory stairwell. The Bureau assigns it as a "workplace accident." But the autopsy contradicts a fall. His relocation papers were filed BEFORE his death. A Resistance symbol and radio frequency are hidden in his personal effects — he was about to defect.

First case where the player investigates a serious violent crime. The system doesn't just convict people unfairly — it kills them.

## Synopsis
Borislav Filipovic — a key witness in a prior case — is dead. Official report: fell down a factory stairwell. The Bureau assigns it to Marlo as a "workplace accident" investigation. But the autopsy report shows injuries inconsistent with a fall: defensive wounds on his arms, a blow to the back of the head BEFORE the fall. His relocation papers were already filed three days before his death — the system was erasing him before he was even dead.

## Suspects (4)
- **Enforcer Sergeant Denic** — The officer who "found" Borislav's body. Guilty. Forensic evidence contradicts his account. He was ordered to silence Borislav before he could testify about Council financial fraud. Professional-class speech, controlled.
- **Milena Jovic** — Records clerk who processed Borislav's relocation papers THREE DAYS before his death. Complicit. Filed papers early to create a post-relocation cover. Won't speak: "I have children, analyst." Worker-class speech, terrified.
- **Factory Foreman Branko Simic** — Borislav's supervisor. Innocent. Had a workplace dispute but was at a production meeting with 12 witnesses. Worker-class speech, straightforward.
- **Nikola Radic** — Fellow worker who owed Borislav gambling debts. Innocent (red herring). Has motive but airtight alibi. Worker-class speech.

## Evidence
- **Autopsy Report** — Type: Document. Hotspot: defensive wounds on arms (clue_defensive_wounds). Hotspot: blow to back of head occurred BEFORE the fall (clue_pre_fall_injury).
- **Stairwell Fingerprint Analysis** — Type: Item. Hotspot: Sergeant Denic's prints on INSIDE of railing — pushing angle, not pulling (clue_railing_prints). foreignSubstance: None.
- **Relocation Order** — Type: Document. Hotspot: filed 3 days BEFORE death (clue_premature_relocation). Hotspot: authorizing signature is Denic's (clue_denic_signature).
- **Borislav's Personal Effects** — Type: Item. Hotspot: broken circle symbol drawn on a note (clue_resistance_symbol). Hotspot: radio frequency on scrap paper (clue_radio_frequency).

## Key Clues (discovery chain)
1. clue_defensive_wounds — Injuries inconsistent with a fall.
2. clue_pre_fall_injury — Blow to head happened BEFORE the stairwell.
3. clue_railing_prints — Denic's prints at pushing angle on railing. EUREKA.
4. clue_premature_relocation — Papers filed before death.
5. clue_denic_signature — Denic authorized the relocation papers.
6. clue_resistance_symbol — Borislav was connected to the Resistance.
7. clue_radio_frequency — Communication channel.
8. clue_milena_knowledge — From interrogation. Milena received the order from "upstairs."
9. clue_denic_alibi_collapse — From interrogation. Denic's "helping" story falls apart.

## Interrogation Flow
### Sergeant Denic
- Tag: clue_defensive_wounds → Lie. "He must have flailed as he fell. People grab at things instinctively." Defensive.
- Tag: clue_pre_fall_injury → Lie. "He could have struck his head on a pipe during the fall. Stairwells are cluttered." Defensive.
  - Contradiction: Present clue_railing_prints → "I grabbed the railing when I tried to help him. My prints being there proves I responded." Evasive.
- Tag: clue_railing_prints → Lie. "I grabbed the railing while trying to pull him up." Defensive.
  - Contradiction: Present clue_pre_fall_injury → "The angle... I was reaching down. Look, it happened fast." Evasive. → clue_denic_alibi_collapse (clickable: "happened fast")
- Tag: clue_premature_relocation → Lie. "Standard processing. Relocation orders are queued in advance." Evasive.
  - Contradiction: Present clue_denic_signature → "I... signed what I was told to sign. I follow orders." Emotional.

### Milena Jovic
- Tag: clue_premature_relocation → Lie. "I process many relocations. The dates must be a clerical error." Evasive.
  - Contradiction: Present clue_denic_signature → "The order came from... from upstairs. I was told to process it and stop asking questions." Emotional. → clue_milena_knowledge (clickable: "stop asking questions")
- Tag: clue_milena_knowledge → Truth (terrified). "I have children, analyst. Please don't ask me more." Emotional.
- Tag: clue_resistance_symbol → Lie. "I don't know what that symbol means." Evasive.

### Branko Simic
- Tag: clue_defensive_wounds → Truth (limited). "I wasn't there. I was in a production meeting. Twelve people saw me." Normal.
- All responses Normal, cooperative, alibi airtight.

### Nikola Radic
- Tag: clue_defensive_wounds → Truth. "I owed him money, not the other way around. Why would I hurt him?" Normal.
- All responses Normal, alibi confirmed.

## Verdict
- **Correct answer:** Sergeant Denic committed Deviation: Agitation (homicide)
- **Accepted violations:** agitation, homicide, obstruction_of_investigation
- **Partial credit:** Ruling it "workplace accident" is accepted (state-preferred)
- **Min confidence:** 75

## State/Resistance Paths
- **State path:** Rule it an "accident." 90 base + 55 state bonus = 145 marks.
- **Justice path:** Rule it homicide. 90 base only. Exposes state-ordered killing.

## Narrative Hooks
- ACT 3 opens with MURDER — the system kills people
- Resistance symbol and radio frequency are direct breadcrumbs
- Milena's "I have children" shows the human cost of complicity
- Denic's prints at the pushing angle is the forensic eureka moment
- Connects to Day 16's accountant case (Borislav was going to testify about financial fraud)
- Premature relocation papers prove the system was erasing him before he died
