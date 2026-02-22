# Case Brief: core_10 — "The Silenced Inspector"

## Meta
- **Type:** Core
- **Available Day:** 11
- **Sequence:** 10
- **Reward:** 85 (+ 40 extraRewardForState)
- **Complexity:** Medium
- **Required Tools:** Document reading
- **Deviation:** Discontent (§4) — Fabricated charge
- **Involves Resistance:** No
- **Moral Dilemma:** System complicity — convict someone you can prove is innocent

## Story Context
THE PIVOT CASE. Player is asked to convict someone they can prove is innocent. A safety inspector documented real hazards; the factory director charged HIM with Discontent. Convicting Henrik is a conscious submission to the system.

Lenka's letter: a Bureau agent visited the family for a "routine loyalty wellness check."

## Synopsis
Henrik Petrov, a factory safety inspector, documented real hazards at the Block F textile factory: cracked ventilation, expired chemical filters, two unreported worker injuries. Factory director Mila Babic charged him with Discontent — "questioning the Council's allocation of safety resources." Henrik's inspection report is thorough, professional, and correct. The evidence fully supports his findings.

## Suspects
- **Henrik Petrov** — Factory Safety Inspector. Innocent (charge is fabricated). Professional, meticulous, quietly defiant. Professional-class speech — measured, precise, data-driven.
- **Mila Babic** — Factory Director. Not charged (guilty of cover-up). Smooth, corporate, deflects with procedure. Professional-class speech bordering on elite register.

## Evidence
- **Henrik's Inspection Report** — Type: Document. Detailed safety assessment. Hotspot: cracked ventilation documentation (clue_ventilation_damage). Hotspot: expired chemical filter dates (clue_expired_filters). Hotspot: two unreported worker injuries (clue_unreported_injuries).
- **Babic's Complaint Filing** — Type: Document. Discontent charge against Henrik. Hotspot: language framing report as "questioning Council resource allocation" (clue_fabricated_charge). Hotspot: filing date — 3 days after Henrik's report submitted (clue_retaliatory_timing).

## Key Clues (discovery chain)
1. clue_ventilation_damage — Real hazard documented.
2. clue_expired_filters — Filters expired 4 months ago.
3. clue_unreported_injuries — Two workers hurt, not reported.
4. clue_fabricated_charge — Charge reframes safety concerns as Discontent.
5. clue_retaliatory_timing — Filed 3 days after Henrik's report.
6. clue_henrik_defense — From interrogation. Henrik explains professional obligation.
7. clue_babic_motive — From interrogation. Babic's report would trigger Council audit.

## Interrogation Flow
### Henrik Petrov
- Tag: clue_ventilation_damage → Truth. "Section 4, panel 3. Cracked along the weld seam. I documented it with measurements and photographs." Normal.
- Tag: clue_expired_filters → Truth. "Expired four months ago. The replacement order was filed but never processed." Normal.
- Tag: clue_unreported_injuries → Truth. "Two workers. Respiratory symptoms consistent with chemical exposure. Director Babic told them to 'rest at home' instead of filing incident reports." Normal. → clue_henrik_defense (clickable: "instead of filing incident reports")
- Tag: clue_fabricated_charge → Truth. "She charged me with Discontent for doing my job. I inspected. I found problems. I reported them." Emotional.
- Tag: clue_retaliatory_timing → Truth. "Three days after my report went to the District Office. Draw your own conclusions, analyst." Normal.

### Mila Babic
- Tag: clue_ventilation_damage → Lie (deflects). "Safety allocations are determined at the Council level. I implement what I'm given." Defensive.
- Tag: clue_unreported_injuries → Lie. "Minor workplace incidents. The workers chose not to file reports." Defensive. → clue_babic_motive (clickable: "chose not to file")
  - Contradiction: Present clue_henrik_defense → "Inspector Petrov exceeded his authority." Defensive.
- Tag: clue_fabricated_charge → Truth (reframes). "His report questioned the adequacy of Council-allocated resources. That is, by definition, Discontent." Normal.
- Tag: clue_retaliatory_timing → Lie. "The timing is coincidental." Evasive.

## Verdict
- **Correct answer:** Henrik Petrov is INNOCENT
- **Accepted violations:** discontent (system accepts wrongful conviction), fabricated_charge (if convicting Babic)
- **Partial credit:** Convicting Henrik earns state bonus
- **Min confidence:** 75

## State/Resistance Paths
- **State path:** Convict Henrik. 85 base + 40 state bonus = 125 marks.
- **Justice path:** Clear Henrik. 85 base only. Babic faces no consequences either way.

## Narrative Hooks
- First case requiring conscious choice between justice and reward
- Henrik is provably innocent — convicting him is entirely knowing
- Director Babic faces no charges regardless
- Lenka's letter: "routine loyalty wellness check" — personal threat escalating
- Fabricated charge mechanism returns in Acts 3-4
