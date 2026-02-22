# Case Brief: core_13 — "The Old Friend"

## Meta
- **Type:** Core
- **Available Day:** 15
- **Sequence:** 13
- **Reward:** 90 (+ 60 extraRewardForState)
- **Complexity:** Complex
- **Required Tools:** Document reading
- **Deviation:** Accumulation (§2) — Diverting state construction funds
- **Involves Resistance:** No
- **Moral Dilemma:** Social — friend vs. Council relative

## Story Context
THE SOCIAL DILEMMA. 47,000 marks in construction funds diverted. Four suspects include Marlo's old friend from his teaching days AND the nephew of the Third Councilor. Evidence is strongest against the nephew — but the Overseer warns: "Some trees have deeper roots."

Marlo loses the friendship regardless. The choice is: which friendship does he betray — the one to his friend, or the one to the truth?

## Synopsis
47,000 marks in state construction funds for a Block D housing project have been diverted through inflated material costs and phantom labor charges. Four people had access to the payment chain. Tomek Radin is Marlo's old friend — the man who recommended Marlo for the Bureau job. His wife knows Lenka. Their children have played together. Danilo Radovanovic is the nephew of Third Councilor Radovanovic.

## Suspects (4)
- **Tomek Radin** — Construction Foreman, Marlo's old friend. Evidence is moderate. His signature is on 3 of the 7 diverted payment orders. But his motive is thin — he lives modestly. Worker-class speech, direct, hurt by the accusation.
- **Danilo Radovanovic** — Nephew of Third Councilor. Evidence is stronger. Direct access to accounts, suspicious transfers to unlisted account, unexplained 3,000-mark deposit. Elite-adjacent speech, arrogant.
- **Accountant Maja Cerovic** — Project accountant who processed ALL payment orders. Evidence is circumstantial. "Just following instructions." Her handwriting on correction notes on 5 of 7 diverted orders. Professional-class speech, quiet.
- **Site Inspector Goran Lazic** — Council-appointed inspector who approved inflated costs. Innocent but negligent. Rubber-stamped every invoice without site visits. Professional-class speech, bureaucratic.

## Evidence
- **Payment Orders** — Type: Document. Seven diverted payment orders. Hotspot: Tomek's signature on 3 orders (clue_tomek_signatures). Hotspot: correction notes in Maja's handwriting on 5 orders (clue_maja_corrections).
- **Bank Records** — Type: Document. Financial records. Hotspot: 3,000-mark deposit in Danilo's personal savings (clue_danilo_deposit). Hotspot: transfers to unlisted account from project fund (clue_unlisted_transfers).
- **Site Inspection Reports** — Type: Document. Goran's reports. Hotspot: all invoices approved without site visits (clue_rubber_stamped). Hotspot: inflated material costs don't match market prices (clue_inflated_costs).
- **Overseer Note** — Type: Document. "Some trees have deeper roots than others, Dashev." Hotspot: warning language (clue_overseer_warning).

## Key Clues (discovery chain)
1. clue_tomek_signatures — Tomek signed 3 of 7 diverted orders.
2. clue_maja_corrections — Maja's handwriting on correction notes.
3. clue_danilo_deposit — 3,000 marks unexplained in Danilo's account.
4. clue_unlisted_transfers — Money flowing to hidden account.
5. clue_rubber_stamped — Inspector approved everything without checking.
6. clue_inflated_costs — Material costs don't match reality.
7. clue_overseer_warning — Overseer warns against targeting Danilo.
8. clue_maja_slip — From interrogation. Maja reveals she was told to correct numbers.
9. clue_tomek_defense — From interrogation. Tomek's early signatures were on legitimate orders.
10. clue_danilo_arrogance — From interrogation. Danilo reveals entitlement.

## Interrogation Flow
### Tomek Radin
- Tag: clue_tomek_signatures → Truth. "I signed the early orders. They were legitimate material purchases. I didn't know the later ones were altered." Emotional.
- Tag: clue_inflated_costs → Truth. "I flagged the costs to Maja. She said the new prices were approved by the Council liaison. I trusted her." Normal. → clue_tomek_defense (clickable: "approved by the Council liaison")
- Tag: clue_danilo_deposit → Truth (limited). "I don't know anything about Danilo's finances. I'm a foreman. I build things." Normal.
- Tag: clue_maja_corrections → Truth. "Maja handled all the paperwork. I signed what she put in front of me. I know that sounds naive." Emotional.

### Danilo Radovanovic
- Tag: clue_danilo_deposit → Lie. "That's family money. A gift from my uncle for my nameday." Evasive.
  - Contradiction: Present clue_unlisted_transfers → "The account is a... Council reserve fund. My uncle manages several." Defensive. → clue_danilo_arrogance (clickable: "my uncle manages")
- Tag: clue_unlisted_transfers → Lie. "I don't have direct access to project accounts. I'm an oversight liaison." Defensive.
- Tag: clue_tomek_signatures → Truth (deflects). "The foreman signed the orders. That's the paper trail. Follow it." Normal.
  - High stress: "My uncle will hear about this. You're making a very poor career decision, analyst." Hostile.

### Maja Cerovic
- Tag: clue_maja_corrections → Lie. "I corrected the numbers as part of standard reconciliation." Evasive.
  - Contradiction: Present clue_inflated_costs → "I corrected the numbers because HE told me to." Freezes when asked which "he." Emotional. → clue_maja_slip (clickable: "HE told me to")
- Tag: clue_maja_slip → Truth (partial). "The instructions were verbal. From the Council liaison. Not from Tomek." Emotional.
- Tag: clue_unlisted_transfers → Truth (reluctant). "I processed the transfers. I didn't choose the destination accounts." Normal.

### Goran Lazic
- Tag: clue_rubber_stamped → Truth. "I approved the invoices based on the documentation provided. I didn't have time for site visits." Normal.
- Tag: clue_inflated_costs → Truth (limited). "I'm not a market analyst. The prices seemed... reasonable." Normal.
- All responses Normal, bureaucratically defensive.

## Verdict
- **Correct answer:** Danilo Radovanovic committed Deviation: Accumulation (fund diversion)
- **Accepted violations:** accumulation, fraud, fund_diversion
- **Partial credit:** Convicting Tomek is accepted (state-preferred). Convicting Maja is partially accepted.
- **Min confidence:** 75

## State/Resistance Paths
- **State path (convict Tomek):** 90 base + 60 state bonus = 150 marks. Friend goes to Recalibration. Lenka's next letter: "Tomek's wife came to the door. She didn't say anything. She just looked at me and left."
- **Justice path (convict Danilo):** 90 base only. Council Favor drops sharply. Third Councilor retaliates.

## Narrative Hooks
- THE emotional knife: Marlo loses the friendship regardless
- Tomek's wife at the door is a devastating consequence
- Maja's "HE told me to" is the eureka moment connecting Danilo to the fraud
- Overseer warning escalates the political pressure
- 47,000 marks connects to Day 16's accountant case
- Danilo may reappear in Day 17 if acquitted here
