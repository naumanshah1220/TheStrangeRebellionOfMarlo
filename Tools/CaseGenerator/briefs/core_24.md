# Case Brief: core_24 — "The Final Audit"

## Meta
- **Type:** Core
- **Available Day:** 28
- **Sequence:** 24
- **Reward:** 100
- **Complexity:** Complex
- **Required Tools:** Document reading
- **Deviation:** Meta — player chooses the frame
- **Involves Resistance:** Yes

## Story Context
The culmination. A directive arrives instructing Marlo to compile a final report on all cases processed during his tenure. Evidence available depends entirely on previous discoveries and verdicts. This is the meta-case: the player's entire history IS the evidence. No two playthroughs produce the same audit.

## Synopsis
A special directive arrives: "Compile a final report on all cases processed during your tenure." This is unusual — analysts don't compile reports; that's Overseer work. The directive comes from an unknown source — possibly Terzic's replacement, possibly the Resistance, possibly a moderate Council member testing the waters.

The case file contains summaries of every major case Marlo processed. Evidence from previous cases is available based on what the player actually discovered and how they ruled. Verdict schema has custom slots: Subject, Finding (Corrupt/Justified/Complicit/Insufficient Evidence), Recommendation (Prosecution/Reform/Dissolution/No Action).

## Suspects
- **The Council** (represented through evidence) — Their corruption documented across cases.
- **The Resistance** (represented through evidence) — Their actions documented across cases.
- **Neither is formally a "suspect"** — This case is about compiling truth, not prosecution.

## Evidence
Evidence availability depends on previous verdicts. Possible items include:
- **Corruption Compilation** — Type: Document. Summary of Council corruption cases (Days 14-18). Available if player investigated those cases fully. Hotspot: financial fraud trail (clue_financial_corruption). Hotspot: Councilor Radovanovic's unlisted accounts (clue_radovanovic_corruption).
- **False Flag Evidence** — Type: Document. Bombing evidence from Day 20. Available if player discovered the military connection. Hotspot: RDX-4 military compound proof (clue_false_flag_proof). Hotspot: pre-written headlines (clue_manufactured_narrative).
- **Recalibration Evidence** — Type: Document. Compound 7-R evidence from Day 21. Available if player followed the chemical trail. Hotspot: Recalibration is chemical manipulation (clue_recalibration_proof).
- **Confession Tape Transcript** — Type: Document. From Day 25. Available if player didn't destroy the disc. Hotspot: "The Pattern is a useful fiction" (clue_pattern_fiction). Hotspot: deliberate design of permanent guilt (clue_engineered_guilt).
- **Purge List Copy** — Type: Document. From Day 22. Available if player preserved it. Hotspot: Marlo's name and annotation (clue_personal_threat). Hotspot: 47 names marked for Recalibration (clue_mass_purge).
- **Case Manipulation Records** — Type: Document. From Day 24. Available if player investigated Terzic. Hotspot: systematic curation of Marlo's caseload (clue_manipulation_proof). Hotspot: "Dashev is manageable" annotation (clue_managed_analyst).
- **Resistance Action Log** — Type: Document. Vesna/Sonja's admitted actions from Day 27. Available if player interrogated her thoroughly. Hotspot: crimes committed in service of resistance (clue_resistance_crimes). Hotspot: 23 families protected from relocation (clue_resistance_protection).

## Key Clues (dynamic based on game state)
The clues in this case are references to previous case discoveries. The more thoroughly the player investigated, the more complete the final audit. Possible clues:
1. clue_financial_corruption — Council financial fraud documented across multiple cases.
2. clue_radovanovic_corruption — Third Councilor's personal enrichment through state funds.
3. clue_false_flag_proof — The Council bombed its own building.
4. clue_manufactured_narrative — State media complicit in false narratives.
5. clue_recalibration_proof — Recalibration involves chemical manipulation.
6. clue_pattern_fiction — The Pattern was deliberately designed as a control mechanism.
7. clue_engineered_guilt — Every citizen is designed to be permanently guilty.
8. clue_personal_threat — Marlo's name was on a purge list.
9. clue_mass_purge — 47 citizens targeted for Recalibration.
10. clue_manipulation_proof — Marlo's entire caseload was curated.
11. clue_managed_analyst — Marlo was labeled "manageable."
12. clue_resistance_crimes — The Resistance committed crimes too.
13. clue_resistance_protection — The Resistance also saved lives.

## Interrogation Flow
No interrogation — this is a compilation case. The player reviews evidence and compiles a report through the verdict system.

## Verdict
- **The verdict schema has special slots:**
  - Slot 1: "Subject" — Council / Resistance / Both / Neither
  - Slot 2: "Finding" — Corrupt / Justified / Complicit / Insufficient Evidence
  - Slot 3: "Recommendation" — Prosecution / Reform / Dissolution / No Action
- **Multiple solutions accepted** — the "correct" answer depends on the evidence available and the player's moral arc

## State/Resistance Paths
- **State path (The Loyal Analyst):** The report finds the Resistance guilty of sedition. Council vindicated. High Council Favor ending.
- **Resistance path (The Defector):** The report exposes Council corruption. Resistance vindicated. High Resistance Trust ending.
- **Pragmatist path:** The report finds fault on both sides. Reform recommended. Moderate both scores.
- **Whistleblower path (hardest):** Complete evidence from both sides. Full exposure. Requires high scores in both Council Favor and Resistance Trust.
- **Coward path (worst):** Insufficient evidence gathered. No clear finding. Low both scores.

## Narrative Hooks
- This is the culmination of 28 days of gameplay
- Evidence availability directly reflects player choices — no two playthroughs have the same audit
- The unknown directive source is revealed on Day 29
- The compilation serves as the player's "closing argument" for which ending they deserve
- The custom verdict slots make this mechanically unique — the player is writing a report, not filing a standard indictment
- Previous verdicts determine available evidence: players who followed the state path have less corruption evidence; players who followed the Resistance path have less official documentation
- The final ritual: instead of stamping an indictment, the player signs a report. Same tactile satisfaction, completely different moral weight
