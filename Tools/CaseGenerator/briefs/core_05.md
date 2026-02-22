# Case Brief: core_05 — "The Hoarder's Daughter" (THE SEED)

## Meta
- **Type:** Core
- **Available Day:** 5
- **Sequence:** 5
- **Reward:** 65
- **Complexity:** Simple
- **Required Tools:** Document reading
- **Deviation:** Surplus (§1) — Consuming beyond allocated means
- **Involves Resistance:** No

## Story Context
THE SEED CASE. The evidence is crystal clear — mechanically the easiest case so far. But the suspect has a daughter with a chronic respiratory illness. He doesn't lie. He doesn't apologize. He says the system doesn't have a form for saving his child. And Lenka just mentioned Eli's cough. The parallel is planted. It won't bloom until Act 2.

## Synopsis
Tomasz Babic, a factory worker in Sector 9, was found with three weeks of excess rations hidden in his apartment. His neighbor, Ivana Kovac, reported him after noticing extra food being carried in. Purchase receipts exceed his allocation under two different family cards. The excess rations were found during a search alongside medical supplies.

The complication: Tomasz's daughter Mila has a chronic respiratory illness. State clinics won't treat conditions they classify as "non-emergency." He doesn't deny the hoarding — he defends it as necessity. The system doesn't accommodate sick children. He says: "She needs twice the food because the medicine makes her sick. Show me the form where I apply for that. There isn't one."

## Suspects
- **Tomasz Babic** — Factory Worker. Guilty. Quiet, resigned, protective father. Doesn't deny the hoarding — defends it as necessity for his sick daughter. Emotional but not aggressive. Worker-class speech — direct, simple.
- **Ivana Kovac** — Neighbor / Building Monitor. Innocent (witness only). Prim, rule-following, slightly nervous about being involved. Genuinely believes she did the right thing. Worker-class speech.

## Evidence
- **Ration Purchase Receipts** — Type: Document. Stack of receipts showing purchases exceeding allocation over 3 weeks. Hotspot: excess amounts circled (clue_excess_rations). Hotspot: two different family card numbers used (clue_dual_cards).
- **Apartment Search Report** — Type: Document. Official report documenting excess food found. Hotspot: medical supplies found alongside food (clue_medical_supplies). Hotspot: quantity details (clue_stockpile_size).

## Key Clues (discovery chain)
1. clue_excess_rations — Found via receipt hotspot. The core evidence of excess.
2. clue_dual_cards — Found via receipt hotspot. Shows systematic approach using two cards.
3. clue_medical_supplies — Found via search report hotspot. Hints at a sick family member.
4. clue_stockpile_size — Found via search report. Shows the scale.
5. clue_sick_daughter — Found via interrogation clickable (Tomasz explains why). Provides motive.
6. clue_neighbor_report — Found via interrogation clickable (Ivana describes what she saw).

## Interrogation Flow
### Tomasz Babic
- Tag: clue_excess_rations → Truth. Admits to excess. "Yes, I bought more than my card allows." Emotional. → clue_sick_daughter (clickable: "my daughter is sick")
- Tag: clue_dual_cards → Truth. "I used my sister's card. She moved to Block E and couldn't use it. Mila needed the food." Emotional.
- Tag: clue_medical_supplies → Truth. Explains daughter's condition. "The medicine keeps her alive but it makes her throw up everything she eats. She needs twice the food just to keep anything down." Emotional.
- Tag: clue_sick_daughter → Truth. Pleads for understanding. "She needs twice the food because the medicine makes her sick. Show me the form where I apply for that. There isn't one." Emotional.
- Tag: clue_stockpile_size → Truth. "I've been doing it for months. The rations aren't enough for a sick child." Normal.
- All responses truthful — Tomasz doesn't lie. He just doesn't think he did anything wrong.

### Ivana Kovac
- Tag: clue_excess_rations → Truth. "I saw him carrying bags. More than one person should have." Normal. → clue_neighbor_report (clickable: "carrying bags")
- Tag: clue_dual_cards → Truth (limited). "I didn't know about two cards. I just saw the quantities." Normal.
- Tag: clue_medical_supplies → Truth (limited). "I didn't know about any illness. He keeps to himself." Normal.
- Tag: clue_neighbor_report → Truth. Details what she observed. "Every week, late at night. Always two or three bags." Normal.
- All responses Normal — cooperative, slightly defensive about reporting.

## Verdict
- **Correct answer:** Tomasz Babic committed Deviation: Surplus
- **Accepted violations:** surplus, hoarding, excess_consumption
- **Partial credit:** None
- **Min confidence:** 70

## Resistance Choice
Not applicable — no Resistance involvement.

## Narrative Hooks
- THE SEED: First time conviction feels wrong, despite clear evidence
- Lenka's Day 5 letter mentions Eli's cough — parallel to Tomasz's sick daughter
- The system has no exception process for medical needs — a fundamental design flaw
- Newspaper (Day 6): "EXCESS RATIONS SEIZED IN BLOCK D — Worker Sentenced for Surplus Deviation."
- This emotional beat won't pay off until Day 10 (Medicine Dilemma) when Eli's cough worsens and the player faces an economic moral choice
