# Case Brief: core_18 — "The Poisoned Well"

## Meta
- **Type:** Core
- **Available Day:** 21
- **Sequence:** 18
- **Reward:** 100
- **Complexity:** Complex
- **Required Tools:** Document reading, Spectrograph
- **Deviation:** Agitation (§5) — Mass endangerment / Accumulation (§2) — Possession of restricted chemicals
- **Involves Resistance:** Yes

## Story Context
Block D — the same Block that's been starved (core_01 original ledger), whose infrastructure funds were stolen (Days 15-16), whose children are in the orphanage — is now being poisoned. Twelve citizens hospitalized with neurological symptoms. The contaminant is Compound 7-R — the EXACT same chemical cocktail used in the Recalibration process. First physical evidence that Recalibration involves chemical manipulation.

## Synopsis
Twelve citizens in Block D are hospitalized with neurological symptoms — tremors, confusion, memory gaps. Water testing reveals contamination with a chemical compound. The Bureau assigns it as an Agitation case (public endangerment), not attempted murder.

Investigation reveals a chain: Maintenance Worker Vladan Stojic directly contaminated the water supply. Chemist Filip Todorovic supplied the chemicals, believing they were for "pest control." Dr. Natalija Savic was the first to report the symptoms and is now perversely treated as a suspect because the chemicals are medical-grade. A note in Vladan's locker reveals he was following orders from an unknown handler.

The spectrograph reveals the contaminant is Compound 7-R — the EXACT same chemical cocktail used in the Recalibration process. The Council may be testing chemical dosages on Block D residents.

## Suspects
- **Vladan Stojic** — Water treatment plant maintenance worker for Block D. Guilty (directly contaminated the water supply). His tool locker contains the same chemical compound found in the water. Quiet, follows orders, doesn't fully understand what he did. Worker-class speech, limited vocabulary.
- **Dr. Natalija Savic** — Block D clinic physician. Innocent. She was the first to report the poisoning symptoms to the Bureau. She is now a suspect because the chemicals are medical-grade — the system suspects its own whistleblower. Professional-class speech, precise and increasingly angry.
- **Filip Todorovic** — Pharmaceutical plant chemist. Complicit. He supplied the chemicals to Vladan, believing they were for "pest control." His access log at the pharmaceutical plant shows he signed out 3 liters of a restricted sedative compound. Professional-class speech, nervous and defensive.
- **Unknown "Handler"** — Referenced in a note found in Vladan's locker: "Apply at midnight. Concentration per instructions. Payment upon confirmation." Unsigned. Not present for interrogation.

## Evidence
- **Water Analysis Report** — Type: Document. Chemical testing results. Hotspot: contamination compound identified (clue_compound_identified). Hotspot: concentration levels consistent with deliberate application (clue_deliberate_concentration).
- **Chemical Sample** — Type: Item. Extracted from water supply. foreignSubstance: Compound 7-R (spectrograph reveals match with Recalibration pharmaceutical profile → clue_recalibration_match). Hotspot: chemical composition breakdown (clue_chemical_composition).
- **Vladan's Tool Locker Contents** — Type: Item. Items found in locker. Hotspot: remaining chemical compound matching water contaminant (clue_vladan_chemical). Hotspot: unsigned instruction note (clue_handler_note).
- **Pharmaceutical Access Log** — Type: Document. Filip Todorovic's sign-out records. Hotspot: 3 liters of restricted sedative compound signed out (clue_filip_signout). Hotspot: "pest control" listed as purpose (clue_false_purpose).
- **Dr. Savic's Patient Records** — Type: Document. Medical observations. Hotspot: symptom pattern matching known Recalibration side effects (clue_recal_symptoms). Hotspot: comparison with previously "recalibrated" returnees (clue_returnee_comparison).

## Key Clues (discovery chain)
1. clue_compound_identified — Chemical contaminant found in Block D water.
2. clue_deliberate_concentration — Concentration is deliberate, not accidental.
3. clue_chemical_composition — Specific chemical breakdown of the contaminant.
4. clue_recalibration_match — Spectrograph reveals: Compound 7-R matches Recalibration drugs. EUREKA.
5. clue_vladan_chemical — Same compound found in Vladan's locker.
6. clue_handler_note — Unsigned instructions: "Apply at midnight. Concentration per instructions. Payment upon confirmation."
7. clue_filip_signout — Filip signed out 3 liters of restricted compound.
8. clue_false_purpose — Filip listed "pest control" as the purpose.
9. clue_recal_symptoms — Dr. Savic recognizes the symptoms from Recalibration returnees.
10. clue_returnee_comparison — The symptoms are identical to those seen in "recalibrated" citizens.
11. clue_vladan_orders — From interrogation. Vladan was told what to do and paid.
12. clue_savic_recognition — From interrogation. Dr. Savic has seen these symptoms before — in patients returned from Recalibration.

## Interrogation Flow
### Vladan Stojic
- Tag: clue_vladan_chemical → Truth (limited). "A man gave me the bottles. Said to pour them in the treatment intake at midnight. Said it was water purification." Normal.
- Tag: clue_handler_note → Truth. "He wrote the instructions. I can't read well. He explained what to do." Emotional.
  - Contradiction: Present clue_deliberate_concentration → "He measured the amounts. He told me exactly how much. I just poured." Emotional. → clue_vladan_orders (clickable: "He told me exactly how much")
- Tag: clue_compound_identified → Truth (limited). "I didn't know what it was. He said it was for the water. To make it better." Normal.
- Tag: clue_recalibration_match → Truth (confused). "Recalibration? I don't know what that is. I just did what he told me." Normal.
  - High stress: "Please. I have a daughter. I didn't know it would hurt anyone." Emotional.

### Dr. Natalija Savic
- Tag: clue_recal_symptoms → Truth. "These symptoms... I've seen them before. In patients who came back from rehabilitation. The same tremors. The same confusion." Normal. → clue_savic_recognition (clickable: "came back from rehabilitation")
- Tag: clue_returnee_comparison → Truth. "Three patients returned from Recalibration last year. Same tremors, same memory gaps, same flat affect afterward. I wrote it up as 'post-rehabilitation adjustment.' Now I'm seeing it in twelve people who never went through Recalibration." Emotional.
- Tag: clue_compound_identified → Truth. "The compound is medical-grade. Not industrial. Not agricultural. Someone with pharmaceutical access supplied this." Normal.
- Tag: clue_recalibration_match → Truth. "If this compound IS what they use in Recalibration... then Recalibration isn't counseling. It's chemical." Emotional.
  - High stress: "I reported this because people were sick. Now I'm a suspect for knowing too much about chemistry. That's how this works, isn't it?" Defensive.

### Filip Todorovic
- Tag: clue_filip_signout → Lie. "The compound was for pest control. Rat infestation in the water treatment facility. Standard procedure." Evasive.
  - Contradiction: Present clue_chemical_composition → "The... the compound isn't typically used for pest control, no. But the requisition was approved by my supervisor." Defensive.
- Tag: clue_false_purpose → Lie. "I listed the purpose as instructed. My supervisor approved the request." Evasive.
- Tag: clue_recalibration_match → Truth (frightened). "I didn't know what it would be used for. I supply chemicals. What people do with them is not my responsibility." Defensive.
  - High stress: "If I'd refused the requisition, I'd have been charged with Dereliction. I have a family." Emotional.

## Verdict
- **Correct answer:** Vladan Stojic committed Deviation: Agitation (mass endangerment)
- **Accepted violations:** agitation, endangerment, contamination, possession_of_restricted_chemicals
- **Partial credit:** None
- **Min confidence:** 70

## State/Resistance Paths
- **State path:** Blame "contaminated pipes" — an accident, no perpetrator. State bonus for keeping it quiet. The chemical connection to Recalibration is buried.
- **Resistance path:** Follow the chemical trail. Compound 7-R proves Recalibration is chemical manipulation. The Council is testing dosages on its own citizens.

## Narrative Hooks
- Block D is being systematically attacked — starved, robbed, and now poisoned
- Compound 7-R is the first physical evidence of what Recalibration actually does
- Dr. Savic's "these symptoms... I've seen them before" is the Recalibration eureka
- The handler's note suggests this was ORDERED, not freelance — someone is testing dosages
- A coded Resistance message appears in Marlo's case file: "Now you see what they do behind closed doors. — V.C." The Resistance directly addresses Marlo for the first time
- Connects to the Recalibration reveal (Recalibration section of story bible)
- The game shifts from white-collar crime to mass poisoning — the system is actively attacking its own citizens
