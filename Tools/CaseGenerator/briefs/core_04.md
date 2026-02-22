# Case Brief: core_04 — "The Warehouse Ring"

## Meta
- **Type:** Core
- **Available Day:** 4
- **Sequence:** 4
- **Reward:** 80
- **Complexity:** Simple
- **Required Tools:** Document reading
- **Deviation:** Accumulation (§2) — Theft of state materials
- **Involves Resistance:** No

## Story Context
Classic misdirection case. The obvious suspect (delivery driver with route overlap) is innocent; the quiet clerk with the keys is guilty. Teaches careful evidence cross-referencing — a skill the player will need for harder cases. Last "purely good" case before the Day 5 seed.

## Synopsis
Factory materials (copper wire, precision tools) have been disappearing from a Sector 9 warehouse for weeks. Delivery logs show Nikola Horvat's truck was at the dock on 4 of 6 theft dates — suspicious. But the warehouse inventory access log shows Dragan Todorovic signed out the specific storage crates that were emptied. His apartment search reveals 2,400 marks in hidden cash — far above a clerk's salary. Nikola's alibi for the other 2 dates is airtight: loading receipts from the opposite end of the district with timestamps.

## Suspects
- **Dragan Todorovic** — Sector 9 Warehouse Clerk. Guilty. Has been systematically stealing copper wire and factory tools, selling them through a black market contact. Quiet, methodical, initially composed but crumbles under evidence pressure. Worker-class speech.
- **Nikola Horvat** — Delivery Driver. Innocent. His delivery routes happened to overlap with the theft dates, making him look suspicious. Straightforward, slightly indignant at the accusation. Worker-class speech.

## Evidence
- **Delivery Route Log** — Type: Document. Nikola's truck movements. Hotspot: truck at dock on 4 of 6 theft dates (clue_route_overlap). Hotspot: loading receipts from opposite end of district on 2 dates (clue_nikola_alibi).
- **Warehouse Inventory Access Log** — Type: Document. Crate sign-out records. Hotspot: Dragan signed out the specific crates that were emptied (clue_crate_signout). Hotspot: sign-out times match theft window (clue_timing_match).
- **Apartment Search Report** — Type: Document. Dragan's apartment. Hotspot: 2,400 marks in hidden cash (clue_hidden_cash).

## Key Clues (discovery chain)
1. clue_route_overlap — From delivery log. Nikola's truck was near the dock on 4 theft dates.
2. clue_nikola_alibi — From delivery log. Loading receipts prove he was elsewhere on 2 dates.
3. clue_crate_signout — From access log. Dragan signed out the emptied crates.
4. clue_timing_match — From access log. Sign-out times correlate with theft windows.
5. clue_hidden_cash — From apartment search. 2,400 marks is far beyond clerk salary.
6. clue_dragan_contact — Found via interrogation clickable. Dragan's black market buyer.

## Interrogation Flow
### Dragan Todorovic
- Tag: clue_crate_signout → Lie. "I sign out crates every day. That's my job. Doesn't mean I took anything." Defensive. stressImpact: 0.08.
  - Contradiction: Present clue_hidden_cash → "That money is from... extra shifts. My brother pays me for help with his stall." Evasive. → clue_dragan_contact (clickable: "brother pays me")
- Tag: clue_timing_match → Lie. "Coincidence. I process dozens of crates a week." Defensive.
- Tag: clue_hidden_cash → Lie. "I've been saving for years. It's not illegal to keep cash at home." Evasive. stressImpact: 0.10.
  - Contradiction: Present clue_crate_signout → "Fine. I took some materials. But 2,400 marks? That's years of skimming small amounts. It's not like anyone noticed." Emotional.
- Tag: clue_route_overlap → Truth (deflects). "Ask the driver. He had access too. More access than me." Defensive.
  - High stress: "Why are you only looking at me? He was there every time!" Hostile.

### Nikola Horvat
- Tag: clue_route_overlap → Truth. "Yes, my route goes past that dock. It goes past a lot of docks. That's what delivery drivers do." Normal.
- Tag: clue_nikola_alibi → Truth. "On those two dates I was loading at the other end of the district. Check the receipts — they're timestamped." Normal.
- Tag: clue_crate_signout → Truth (limited). "I don't go inside the warehouse. I pick up sealed crates from the dock. The clerk brings them out." Normal.
- Tag: clue_hidden_cash → Truth (limited). "I don't know anything about that. I make 30 marks a week. I can barely afford rent." Normal.

## Verdict
- **Correct answer:** Dragan Todorovic committed Deviation: Accumulation
- **Accepted violations:** accumulation, theft, theft_of_state_property
- **Partial credit:** None
- **Min confidence:** 75

## Resistance Choice
Not applicable.

## Narrative Hooks
- Classic detective work — obvious suspect is a red herring, quiet suspect is guilty
- 2,400 marks in stolen goods shows the scale of black market economy
- Overseer note arrives after this case: "Four for four, Analyst Dashev. The Council notices efficiency. Stay in the trace." — V. Terzic
- Player feels like a competent detective, primed for the Day 5 emotional disruption
