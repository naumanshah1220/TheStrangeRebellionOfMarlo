# Case Brief: core_12 — "The Party Photographs"

## Meta
- **Type:** Core
- **Available Day:** 14
- **Sequence:** 12
- **Reward:** 95 (+ 50 extraRewardForState)
- **Complexity:** Complex
- **Required Tools:** Document reading, Fingerprint, Spectrograph (photo analysis)
- **Deviation:** Surplus (§1) real / Accumulation (§2) charged / Dereliction (§3) — espionage
- **Involves Resistance:** Yes

## Story Context
Photos of a Council banquet — imported wine, luxury food — anonymously submitted to the Bureau. The Overseer's directive: "Focus on the photographer, not the subject." First hint of Council internal warfare — factions using the Bureau as a weapon against each other.

## Synopsis
Photographs of a Council banquet — imported wine, luxury food, items that would earn any citizen a Surplus charge — were anonymously submitted to the Bureau. The Bureau arrested Jovan Rezek, a freelance repairman who had building access. But WHO submitted the photos, and WHY?

The photos were printed on Bureau-quality paper — not consumer stock. Fingerprints on the envelope match Council Aide Vera Kolar. She's secretly working for the Fourth Councilor (Vitomir Zoroslav, Information) to build a case against the Third Councilor (Radovanovic). Council infighting.

## Suspects (3)
- **Jovan Rezek** — Freelance Repairman. Guilty of taking the photos, but innocent of the charged crime. Hired to fix a chandelier and couldn't resist photographing the excess. Street-smart, defiant. Worker-class speech.
- **Council Aide Vera Kolar** — Uncharged publicly. She submitted the photos. Working for the Fourth Councilor against the Third. Polished, composed, elite-class speech. "It has been determined that certain excesses require documentation."
- **Lieutenant Josip Matic** — Council Security. Innocent of the photo leak but guilty of accepting bribes to let unauthorized people in. Let Jovan in for 50 marks. Professional-class speech, nervous.

## Evidence
- **Party Photographs** — Type: Photo. Five photos of the banquet. Hotspot: Council ring on host's hand (clue_council_ring). Hotspot: imported wine labels (clue_luxury_goods). Hotspot: timestamp (clue_party_date). foreignSubstance: Ink (spectrograph reveals Bureau-quality printing paper → clue_bureau_paper).
- **Building Access Records** — Type: Document. Repair orders. Hotspot: Jovan's work order for chandelier repair (clue_work_order). Hotspot: his access authorized by building management (clue_authorized_access).
- **Submission Envelope** — Type: Item. The envelope the photos arrived in. Hotspot: fingerprints on envelope — Vera Kolar's (clue_vera_prints). Hotspot: Fourth Councilor's seal on an unrelated memo in Vera's desk (clue_fourth_councilor_seal).
- **Overseer Letter** — Type: Document. "Focus on the photographer." Hotspot: directive language (clue_overseer_directive).

## Key Clues (discovery chain)
1. clue_council_ring — The host is a Council member.
2. clue_luxury_goods — The party is a Surplus violation.
3. clue_party_date — Recent date.
4. clue_bureau_paper — Photos printed on Bureau-quality paper. EUREKA.
5. clue_work_order — Jovan had legitimate access.
6. clue_authorized_access — He was authorized to be there.
7. clue_vera_prints — Vera Kolar's fingerprints on the submission envelope.
8. clue_fourth_councilor_seal — Links Vera to the Fourth Councilor's faction.
9. clue_overseer_directive — Overseer protecting Council interests.
10. clue_jovan_motive — From interrogation. Why Jovan took the photos.

## Interrogation Flow
### Jovan Rezek
- Tag: clue_council_ring → Truth. "I know whose party it was. Everyone in the building knows." Defensive.
- Tag: clue_work_order → Truth. "I was there to fix a chandelier. I saw the food being carried in — more food than Block D sees in a month." Normal. → clue_jovan_motive (clickable: "more food than Block D sees in a month")
- Tag: clue_authorized_access → Truth. "I had a work order. They're making it sound like I broke in." Defensive.
- Tag: clue_overseer_directive → Truth. "You've been told to focus on me, haven't you? That's how they work." Defensive.
  - High stress: "You know this is wrong. But you'll convict me anyway, won't you?" Hostile.

### Vera Kolar
- Tag: clue_vera_prints → Truth (composed). "The Third Councilor's... excesses are a concern to certain parties. I was asked to ensure documentation reached the appropriate hands." Normal. Elite composure.
- Tag: clue_fourth_councilor_seal → Lie. "I serve the Council as a whole. I don't answer to individual members." Defensive.
- Tag: clue_bureau_paper → Truth (deflects). "Bureau supplies are available to many departments." Normal.
- Tag: clue_council_ring → Lie. "I can't comment on the identity of anyone in those photographs." Normal.

### Lieutenant Josip Matic
- Tag: clue_authorized_access → Truth (nervous). "I checked his work order. It was legitimate." Normal.
- Tag: clue_work_order → Lie. "He had proper authorization. That's all I verified." Evasive. (Hiding that he took 50 marks to expedite access.)

## Verdict
- **Correct answer:** Jovan Rezek committed Deviation: Accumulation (unauthorized photographs)
- **Accepted violations:** accumulation, unauthorized_documentation, trespassing
- **Partial credit:** None — but the REAL violation is the Council member's Surplus
- **Min confidence:** 70

## State/Resistance Paths
- **State path:** Convict Jovan as directed. 95 base + 50 state bonus = 145 marks.
- **Deeper path:** Follow the envelope to Vera and the Council infighting. 95 base only.

## Narrative Hooks
- Council internal warfare — factions using the Bureau as a weapon
- Bureau-quality paper proves someone on the inside submitted the photos
- Vera Kolar's elite composure contrasts with Jovan's working-class defiance
- "You know this is wrong" directly challenges the player
- Connects to Councilor Radovanovic's corruption arc
