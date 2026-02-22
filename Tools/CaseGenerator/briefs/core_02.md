# Case Brief: core_02 — "The Queue Brawl"

## Meta
- **Type:** Core
- **Available Day:** 2
- **Sequence:** 2
- **Reward:** 70
- **Complexity:** Simple
- **Required Tools:** Document reading, Fingerprint (optional)
- **Deviation:** Agitation (§5) — Public disturbance
- **Involves Resistance:** No

## Story Context
First case with deliberately conflicting testimony. Two workers brawled at a ration queue. Three witnesses disagree about who started it. Introduces cross-referencing witness statements against physical evidence. Still clear-cut — both are guilty, but the player must identify the primary aggressor.

## Synopsis
Two factory workers — Marko Jovic and Stevo Lukic — got into a fistfight at the Block D ration queue. Three witnesses give conflicting accounts. Stevo cut the line because his wife is pregnant; Marko shoved him first; Stevo threw a punch back and then lied that Marko was drunk. Medical reports show Marko struck first. The queue log disproves the "drunk" claim — Marko bought standard rations, no alcohol.

## Suspects
- **Marko Jovic** — Brickyard Worker. Guilty (primary aggressor). Rough, direct, short-tempered. Doesn't lie much — just minimizes his role. Defensive tone. Worker-class speech.
- **Stevo Lukic** — Cannery Worker. Guilty (secondary — cut in line, fought back). Nervous, makes excuses, lies about Marko being drunk. Evasive. Worker-class speech.

## Evidence
- **Enforcer Incident Report** — Type: Document. Officer Dusek's account. Hotspot: description of mutual combat (clue_mutual_combat). Hotspot: witness list with contact info (clue_witness_list).
- **Queue Position Log** — Type: Document. Ration station records showing queue order. Hotspot: Stevo's position jumps forward (clue_line_cutting). Hotspot: timestamp shows no alcohol vendor nearby (clue_no_alcohol_vendor).
- **Medical Report** — Type: Document. Injuries documented. Hotspot: Marko's bruised knuckles suggest he struck first (clue_first_strike).

## Key Clues (discovery chain)
1. clue_mutual_combat — From enforcer report. Both were fighting.
2. clue_witness_list — From enforcer report. Witnesses exist.
3. clue_line_cutting — From queue log. Stevo jumped the line.
4. clue_no_alcohol_vendor — From queue log. Contradicts Stevo's "drunk" claim.
5. clue_first_strike — From medical report. Marko's injuries consistent with first punch.
6. clue_stevo_lie — From interrogation clickable. Stevo admits he lied about Marko drinking.
7. clue_pregnant_wife — From interrogation clickable. Stevo explains why he cut in line.

## Interrogation Flow
### Marko Jovic
- Tag: clue_mutual_combat → Truth (partial). "He cut in front of twenty people. Someone had to say something." Defensive.
- Tag: clue_line_cutting → Truth. "I saw him push past the old woman at position 12. That's when I grabbed his arm." Defensive.
- Tag: clue_first_strike → Lie (minimizes). "I didn't punch him. I pushed him. He swung first." Defensive.
  - Contradiction: Present clue_mutual_combat → "Fine, maybe I threw the first real punch. But he started it by cutting." → clickable: "threw the first real punch" → clue_marko_admission
- Tag: clue_no_alcohol_vendor → Truth. "I haven't had a drink in six months. I can't afford it. He made that up." Normal.
  - High stress: "I'm a bricklayer, not a drunk! Check my record!" Hostile.

### Stevo Lukic
- Tag: clue_mutual_combat → Lie. "He attacked me for no reason. I was just standing in line." Evasive.
  - Contradiction: Present clue_line_cutting → "I... look, my wife is pregnant. I needed to get home." Emotional. → clue_pregnant_wife (clickable: "wife is pregnant")
- Tag: clue_line_cutting → Lie. "I was in my proper position. The log must be wrong." Evasive.
  - Contradiction: Present clue_first_strike → Breaks down. "Fine, I moved up in line. But I didn't think anyone would care." → clue_stevo_lie
- Tag: clue_no_alcohol_vendor → (if clue_stevo_lie discovered) Truth. "I said he was drunk because I thought it would help my case. He wasn't drunk." Emotional.
- Tag: clue_first_strike → Lie. "He hit me first, definitely." Evasive.

## Verdict
- **Correct answer:** Marko Jovic committed Deviation: Agitation (primary), or both
- **Accepted violations:** agitation, assault, public_disturbance
- **Partial credit:** Convicting Stevo alone is wrong but system accepts it
- **Min confidence:** 70

## Resistance Choice
Not applicable.

## Narrative Hooks
- Officer Dusek appears here for the first time — recurring character
- The ration queue setting reinforces the scarcity that drives tension
- Stevo's pregnant wife humanizes even the liar — another "guilty but sympathetic" case
- Introduces conflicting testimony as a mechanic the player will face in harder cases
