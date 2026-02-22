# Case Brief: core_21 — "The Overseer Unmasked"

## Meta
- **Type:** Core
- **Available Day:** 24
- **Sequence:** 21
- **Reward:** 100
- **Complexity:** Complex
- **Required Tools:** Document reading, Fingerprint
- **Deviation:** Accumulation (§2) / Dereliction (§3) — Espionage, unauthorized document retention
- **Involves Resistance:** Yes

## Story Context
The most personal case in the game. A filing clerk is charged with retaining classified memos — and those memos contain Marlo's complete case assignment history with annotations proving every case since Day 1 was stage-managed by Colonel Vlastimir Terzic (Overseer V. Terzic). The player's entire experience has been designed.

## Synopsis
A seemingly routine case about unauthorized document retention leads to an explosive discovery. Filing clerk Ivan Tomic is charged with keeping copies of 14 classified interoffice memos over 6 months. Among the memos is a series of communications revealing systematic manipulation of Marlo's caseload.

The memos contain Marlo's complete case assignment history with annotations:
- Day 1: "Assign Depot Theft. Straightforward. Build confidence."
- Day 5: "Assign Babic ration case. Observe emotional response. If sympathetic, flag for management."
- Day 10: "Assign medicine case. Ensure state-preferred outcome aligns with personal incentive. Test compliance under economic pressure."
- Day 15: "Assign Radin case. Personal connection. Observe loyalty threshold."

Fingerprints on the original routing memos match Colonel Vlastimir Terzic, Military Intelligence — the same man who signed himself "V. Terzic" on every Overseer letter since Day 1. Marlo was labeled "manageable" in the Day 1 memo.

## Suspects
- **Ivan Tomic** — Bureau Filing Clerk. Guilty (of retaining 14 classified memos over 6 months). Small, nervous, apologetic. He kept copies because he noticed the pattern of case manipulation and wanted insurance. A quiet man who has been systematically copying and hiding documents he knows will be destroyed. Worker-class speech, careful.
- **Analyst Milena Stojakovic** — Fellow Bureau analyst. Innocent. Her name appears on two of the memos as "flagged for review" — she's being set up as a secondary target. Professional-class speech, confused and frightened.
- **Colonel Vlastimir Terzic ("V. Terzic")** — NOT PRESENT, NOT CHARGED — but his identity is the discovery. The man behind the curtain.

## Evidence
- **Retained Memo Collection** — Type: Document. 14 classified memos Ivan kept. Hotspot: memo directing case reassignment for "Council-adjacent suspects" (clue_case_manipulation). Hotspot: memo specifically mentioning "Analyst Dashev" as "manageable" (clue_marlo_mentioned). Hotspot: day-by-day case annotations showing deliberate curation (clue_curation_trail).
- **Case Assignment Records** — Type: Document. Bureau records showing which cases went to which analysts. Hotspot: pattern of Council-related cases being diverted from Marlo (clue_diversion_pattern). Hotspot: cases that WERE assigned to Marlo — always ones that serve the Council narrative (clue_narrative_cases).
- **Fingerprint Evidence** — Type: Item. Prints from the original memos. Hotspot: prints match military database entry for Colonel Vlastimir Terzic (clue_terzic_identity). Hotspot: same prints appear on Overseer guidance letters in Marlo's file (clue_terzic_letters).

## Key Clues (discovery chain)
1. clue_case_manipulation — Cases involving Council members are being systematically redirected.
2. clue_marlo_mentioned — Marlo is specifically named as "manageable" in internal communications.
3. clue_curation_trail — Day-by-day annotations proving every case was deliberately chosen.
4. clue_diversion_pattern — A clear pattern of case steering across 8 months.
5. clue_narrative_cases — Marlo's cases were chosen to reinforce the Council's narrative.
6. clue_terzic_identity — Colonel Vlastimir Terzic, Military Intelligence, is Overseer V. Terzic.
7. clue_terzic_letters — The same person who wrote the guidance letters wrote the manipulation memos.
8. clue_ivan_motive — From interrogation. Ivan explains why he kept the memos.
9. clue_full_picture — From interrogation. Ivan reveals the scope of manipulation.

## Interrogation Flow
### Ivan Tomic
- Tag: clue_case_manipulation → Truth. "I started noticing it six months ago. Certain cases vanished from the queue. Others were fast-tracked to specific analysts." Normal.
- Tag: clue_marlo_mentioned → Truth. "Your name came up several times. 'Dashev is manageable. Assign him the standard Pattern cases. Keep the Council matters in Sector 2.'" Normal. → clue_ivan_motive (clickable: "started noticing")
- Tag: clue_curation_trail → Truth. "Every case you received was annotated in advance. Day by day. 'Build confidence.' 'Test compliance.' 'Observe loyalty threshold.' Like a training program." Emotional.
- Tag: clue_diversion_pattern → Truth. "I pulled the numbers. Over eight months, 100% of cases involving Council family members were assigned to three specific analysts — none of them you." Normal.
- Tag: clue_narrative_cases → Truth. "You got the cases they WANTED you to see. Clear-cut Pattern violations. Sympathetic victims. Cases designed to make you believe the system works." Emotional. → clue_full_picture (clickable: "cases they wanted you to see")
- Tag: clue_terzic_identity → Truth. "I matched the prints on the memos to your Overseer letters. Same person. Colonel Vlastimir Terzic, Military Intelligence, seconded to Bureau Oversight." Normal.
- Tag: clue_ivan_motive → Truth. "I kept copies because I knew the originals would disappear. Just like Borislav. Just like everyone who sees too much." Emotional.
- Tag: clue_full_picture → Truth. "Your entire career at the Bureau has been curated, Analyst Dashev. Every case, every letter, every commendation — part of a program to shape you into a reliable tool." Emotional.
  - High stress: "And now you have to decide what to do with what you know. Just like I did." Emotional.

### Analyst Milena Stojakovic
- Tag: clue_marlo_mentioned → Truth. "I didn't know my name was in those memos. I've never seen them before today." Normal.
- Tag: clue_case_manipulation → Truth. "I've... suspected something. My caseload has been oddly consistent. Always workplace disputes. Never anything political." Normal.
- Tag: clue_diversion_pattern → Truth (frightened). "If they've been managing all of us... how many analysts even know?" Emotional.
- All responses Normal, cooperative, increasingly alarmed.

## Verdict
- **Correct answer:** Ivan Tomic committed Deviation: Accumulation / Dereliction (unauthorized retention of classified documents)
- **Accepted violations:** accumulation, unauthorized_retention, dereliction, espionage
- **Partial credit:** None
- **Min confidence:** 60

## State/Resistance Paths
- **State path:** Convict Ivan. The memos are classified. Terzic's identity stays hidden. The manipulation program continues.
- **Resistance path:** Ivan's evidence proves systematic manipulation. The Resistance can use this to expose the Bureau's true nature.

## Narrative Hooks
- THE ultimate betrayal — the player's entire experience has been stage-managed
- Every feeling of competence in Act 1, every dilemma in Act 2-3 — designed
- The player isn't just Marlo realizing he's been manipulated; the PLAYER realizes they've been manipulated
- "Dashev is manageable" recontextualizes every Overseer letter in the game
- Ivan parallels Sonja Petrovic (Day 16) and Stefan Novak (Day 22) — people who kept evidence and were punished
- Colonel Terzic's military background connects to the Day 23 assassination (Agent Zelimir Terzic — relative)
- Directly sets up the Day 25 revelation about the Pattern itself
