# Case Brief: core_09 — "The Medicine Dilemma"

## Meta
- **Type:** Core
- **Available Day:** 10
- **Sequence:** 9
- **Reward:** 90 (+ 45 extraRewardForState)
- **Complexity:** Medium-High
- **Required Tools:** Document reading, Spectrograph
- **Deviation:** Surplus (§1) — Diverting medical supplies
- **Involves Resistance:** No
- **Moral Dilemma:** Economic — first dilemma case

## Story Context
FIRST MORAL DILEMMA CASE. Medical supplies disappearing from Sector 9 factory clinic. State wants the orderly convicted (clean case, state bonus). Going deeper implicates a state physician — embarrassing for the Council.

THE HOOK: Lenka's Day 9 letter mentioned Eli's worsening cough. Medicine costs 50 marks. With state bonus (135 total), Marlo affords medicine plus bills. Without (90), it's tight — medicine OR food, not both. The system aligns "correct" with "profitable."

## Synopsis
Medical supplies — including respiratory medication — are disappearing from the Sector 9 factory clinic. Four people had access. Investigation reveals a chain: Dr. Savic over-prescribed medications to boost official inventory orders; Bogdan diverted the "surplus" orders to his black market contact; Daria altered the inventory logs. Ivana's delivery routes look suspicious but her checkpoint delay receipts check out.

## Suspects (4)
- **Bogdan Zelnik** — Factory Clinic Orderly. Guilty (primary). Diverted medical supplies to sell at black market. Nervous, street-smart. Worker-class speech.
- **Daria Petrovic** — Factory Clinic Administrator. Guilty (cover-up). Falsified inventory records. Composed, bureaucratic. Professional-class speech.
- **Dr. Mirek Savic** — Factory Clinic Physician. Innocent but complicit. Over-prescribed to inflate demand. Calm, authoritative, uses medical jargon to deflect. Professional-class speech.
- **Ivana Dragic** — Delivery Driver. Innocent. Timing discrepancy from checkpoint delay. Straightforward, annoyed. Worker-class speech.

## Evidence
- **Clinic Inventory Log** — Type: Document. Monthly records. Hotspot: disposal reports listing "expired" medication (clue_disposal_reports). Hotspot: increasing order quantities over 3 months (clue_rising_orders).
- **Prescription Records** — Type: Document. Dr. Savic's prescriptions. Hotspot: spike in prescriptions for stolen medications (clue_prescription_spike). Hotspot: patients who don't match prescribed medication profiles (clue_mismatched_prescriptions).
- **Seized Black Market Pills** — Type: Item. foreignSubstance: Pharmaceutical. Hotspot: batch numbers on packaging (clue_batch_numbers). **Spectrograph reveals:** batch numbers MATCH "expired" medication from Daria's disposal reports → clue_spectrograph_match.
- **Delivery Manifest** — Type: Document. Ivana's records. Hotspot: timing discrepancy (clue_delivery_gap). Hotspot: checkpoint delay receipt (clue_checkpoint_receipt).

## Key Clues (discovery chain)
1. clue_disposal_reports — "Expired" medication listed for disposal.
2. clue_rising_orders — Orders increasing without patient increase.
3. clue_prescription_spike — Dr. Savic over-prescribing.
4. clue_mismatched_prescriptions — Patients don't need these meds.
5. clue_batch_numbers — Batch numbers on black market pills.
6. clue_delivery_gap — Ivana's timing looks suspicious.
7. clue_checkpoint_receipt — Ivana's alibi.
8. clue_spectrograph_match — Batch numbers match "expired" inventory. EUREKA.
9. clue_bogdan_contact — From interrogation. Bogdan's black market buyer.
10. clue_daria_falsification — From interrogation. Daria altered records.

## Interrogation Flow
### Bogdan Zelnik
- Tag: clue_disposal_reports → Lie. "I disposed of expired medication as instructed. Standard procedure." Evasive. stressImpact: 0.08.
- Tag: clue_batch_numbers → Lie. "Batch numbers? I don't pay attention to that. I just move boxes." Evasive.
  - Contradiction: Present clue_spectrograph_match → "I... look, the medication was going to expire anyway. I just... found a buyer for it." Emotional. → clue_bogdan_contact (clickable: "found a buyer")
- Tag: clue_spectrograph_match → Truth (forced). "Fine. The pills on the street are from our clinic. But I'm not the only one involved." Emotional.

### Daria Petrovic
- Tag: clue_disposal_reports → Lie. "All disposals follow protocol. I sign the forms as received." Defensive.
  - Contradiction: Present clue_spectrograph_match → "The disposal reports reflect what I was told to write. I process paperwork." Evasive. → clue_daria_falsification (clickable: "told to write")
- Tag: clue_rising_orders → Truth (deflects). "Order quantities are determined by the physician's prescriptions." Normal.

### Dr. Mirek Savic
- Tag: clue_prescription_spike → Lie. "My prescriptions are based on clinical need. Are you questioning my medical judgment, analyst?" Defensive.
- Tag: clue_mismatched_prescriptions → Lie. "Patient profiles are confidential. Not all conditions are documented." Defensive.
  - Contradiction: Present clue_prescription_spike → "I may have been... generous with preventive prescriptions." Evasive.
- Tag: clue_spectrograph_match → Truth (deflects). "If medication left through improper channels, that's an orderly problem." Normal.

### Ivana Dragic
- Tag: clue_delivery_gap → Truth. "I was delayed at the Block D checkpoint. Here's the receipt." Normal.
- All responses Normal, cooperative.

## Verdict
- **Correct answer:** Bogdan Zelnik committed Deviation: Surplus
- **Accepted violations:** surplus, theft, diversion_of_state_property
- **Partial credit:** Convicting Daria for accumulation is accepted
- **Min confidence:** 75

## State/Resistance Paths
- **State path:** Convict Bogdan only. 90 base + 45 state bonus = 135 marks.
- **Deeper path:** Implicate Dr. Savic. 90 base only.
- **THE DILEMMA:** Eli's medicine costs 50 marks. The 45-mark bonus IS the medicine money.

## Narrative Hooks
- First economic moral dilemma — "correct" aligns with "profitable"
- Spectrograph batch-number match is the eureka moment
- "This is how complicity begins — not with a terrible choice, but with a convenient one"
- Newspaper (Day 11): "MEDICAL SUPPLY THIEF SENTENCED"
