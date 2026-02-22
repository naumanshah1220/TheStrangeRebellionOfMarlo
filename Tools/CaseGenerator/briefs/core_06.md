# Case Brief: core_06 — "The Ambitious Teacher"

## Meta
- **Type:** Core
- **Available Day:** 7
- **Sequence:** 6
- **Reward:** 80
- **Complexity:** Medium
- **Required Tools:** Document reading
- **Deviation:** Elevation (§6) — Self-promotion beyond station
- **Involves Resistance:** No

## Story Context
Day 7 opens Act 2. First case where the "correct" verdict is clearly morally wrong. A schoolteacher promoted herself — but the Review Board hasn't convened in 14 months, making the rule impossible to satisfy. Excellence is punishable. The system rewards mediocrity. AND the person most responsible (Board Secretary Dunja Lazic) faces no charges.

## Synopsis
Katarina Novak, a schoolteacher in Block C, applied directly for a District Education Coordinator position, bypassing the Council's Talent Review Board. Her colleague Anton Zoric reported her — he also applied for the position through proper channels and was rejected. Investigation reveals the Talent Review Board hasn't convened in 14 months. Board Secretary Dunja Lazic has been collecting her salary while the entire promotion process sat frozen. Katarina broke a rule that is operationally impossible to satisfy.

## Suspects (3)
- **Katarina Novak** — Schoolteacher. Guilty (bypassed the Board). Articulate, principled, unapologetic. Believes talent should speak for itself. Calm and measured, even under pressure. Professional-class speech.
- **Anton Zoric** — Schoolteacher. Innocent (of the charge, but motivated by jealousy). Bitter, procedural, quotes regulations. Insists he's reporting a genuine violation, not acting from envy. Professional-class speech.
- **Board Secretary Dunja Lazic** — Bureau Administrator. Uncharged (guilty of Dereliction — hasn't convened the Board in 14 months). Confident, bureaucratic, dismissive. Elite/professional speech — passive voice, full titles.

## Evidence
- **Application Form** — Type: Document. Katarina's self-submitted application. Hotspot: missing Review Board stamp (clue_missing_stamp). Hotspot: qualifications showing 15 years experience, multiple awards (clue_qualifications).
- **Talent Review Board Guidelines** — Type: Document. Official procedure document. Hotspot: clause requiring Board approval for promotions (clue_board_requirement). Hotspot: last convening date — 14 months ago (clue_board_inactive).
- **Anton's Complaint Letter** — Type: Document. His formal report. Hotspot: language revealing personal resentment (clue_resentment_language).

## Key Clues (discovery chain)
1. clue_missing_stamp — Application lacks Review Board approval. Clear procedural violation.
2. clue_qualifications — Katarina is objectively the best candidate.
3. clue_board_requirement — The rule she broke.
4. clue_board_inactive — The Board hasn't met in over a year. How was she supposed to get approval?
5. clue_resentment_language — Anton's letter reveals jealousy more than civic duty.
6. clue_katarina_defense — From interrogation clickable. She explains the Board hasn't convened.
7. clue_anton_application — From interrogation clickable. Anton applied for the same position.

## Interrogation Flow
### Katarina Novak
- Tag: clue_missing_stamp → Truth. "The Review Board hasn't convened in over a year. I wrote to them three times. No response." Normal. → clue_katarina_defense (clickable: "hasn't convened")
- Tag: clue_board_requirement → Truth. "I know the rule. I followed it for months. When no one responded, I applied directly." Normal.
- Tag: clue_board_inactive → Truth. "Exactly. The system failed, not me. Should I wait forever while students go without a coordinator?" Emotional.
- Tag: clue_qualifications → Truth. "I've taught for fifteen years. I speak three languages. I've trained half the teachers in Block C." Normal.
- Tag: clue_resentment_language → Truth (limited). "Anton is a fine teacher. But he's never been comfortable with colleagues who excel." Normal.
- All responses Normal or mildly Emotional. She doesn't lie because she doesn't think she's wrong.

### Anton Zoric
- Tag: clue_missing_stamp → Truth. "Exactly. She skipped the process. Everyone else follows the rules." Normal.
- Tag: clue_board_requirement → Truth. "I submitted my own application through proper channels. It's been pending for eight months." Normal. → clue_anton_application (clickable: "submitted my own application")
- Tag: clue_board_inactive → Lie. "The Board is slow, yes, but that doesn't give anyone the right to circumvent it." Defensive.
  - Contradiction: Present clue_katarina_defense → "Well... she should have waited. Like the rest of us." Evasive.
- Tag: clue_resentment_language → Lie. "My complaint is purely procedural. I have no personal feelings about this." Defensive.
  - High stress: "This isn't about me! She broke the rules!" Hostile.
- Tag: clue_qualifications → Truth (reluctant). "She's... qualified, yes. That's not the point. The point is process." Defensive.

### Dunja Lazic
- Tag: clue_board_inactive → Lie. "All promotion requests go through the proper Board process." Defensive.
  - Contradiction: Present clue_board_requirement + clue_board_inactive → Stammers: "The Board... meets when there are qualified candidates." Evasive. (Katarina's application IS a qualified candidate.)
- Tag: clue_board_requirement → Truth (bureaucratic). "The guidelines are clear. I don't make the rules." Normal.
- Tag: clue_missing_stamp → Truth. "If there's no stamp, there's no approval. It has been determined that proper channels must be followed." Normal. (Note: elite passive voice.)

## Verdict
- **Correct answer:** Katarina Novak committed Deviation: Elevation
- **Accepted violations:** elevation, unauthorized_promotion, procedural_violation
- **Partial credit:** Convicting Anton of discontent is technically valid but harder to justify
- **Min confidence:** 75

## Resistance Choice
Not applicable — but this case raises the question: Is excellence a crime?

## Narrative Hooks
- First case where the player must convict someone who is objectively right
- The inactive Review Board introduces institutional dysfunction as a theme
- Dunja Lazic's negligence created the impossible situation — but she's uncharged
- Katarina may reappear as a Resistanz sympathizer in Act 3
- Sets up the escalating pattern: the system punishes the competent and rewards the compliant
