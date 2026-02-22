# Case Brief: core_03 — "The False Accuser"

## Meta
- **Type:** Core
- **Available Day:** 3
- **Sequence:** 3
- **Reward:** 70
- **Complexity:** Simple
- **Required Tools:** Document reading
- **Deviation:** Discontent (§4) — Filing a malicious report
- **Involves Resistance:** No

## Story Context
First case where the accuser is actually the problem. A citizen filed noise complaints driven by jealousy of a neighbor's promotion. Introduces the idea that the Pattern can be weaponized by petty citizens — not just the Council. The satisfying twist: the accuser IS the criminal.

## Synopsis
Nadia Filipovic filed three noise complaints against her neighbor, Petar Simic, who just received a modest promotion she'd also applied for. Building records show zero corroborating noise violations. Two of the three alleged dates, Petar wasn't even home — he was on overnight shift. Investigation reveals Nadia is the actual Pattern violator: Discontent (envying another's allocation).

## Suspects
- **Nadia Filipovic** — Postal Clerk. Guilty (of Discontent — she's the one coveting). Bitter, precise, uses bureaucratic language to sound legitimate. Defensive when her motives are questioned. Professional-class speech patterns.
- **Petar Simic** — Water Treatment Worker. Innocent. Mild-mannered, confused about why he's being investigated. Cooperative and slightly bewildered. Worker-class speech.

## Evidence
- **Noise Complaint Form** — Type: Document. Nadia's official filing. Hotspot: specific language about "flaunting his promotion" (clue_jealousy_language). Hotspot: dates claimed for disturbances (clue_complaint_dates).
- **Building Incident Log** — Type: Document. Building monitor's records. Hotspot: no noise violations logged for Petar's apartment (clue_no_violations). Hotspot: a note from another neighbor supporting Petar (clue_neighbor_testimony).

## Key Clues (discovery chain)
1. clue_jealousy_language — Found via complaint form. Nadia's own words betray envy.
2. clue_complaint_dates — Found via complaint form. Specific dates she claims disturbances.
3. clue_no_violations — Found via building log. No corroborating records.
4. clue_neighbor_testimony — Found via building log. Another neighbor says Petar is quiet.
5. clue_nadia_rejected — Found via interrogation clickable (Nadia reveals she was rejected for the promotion).
6. clue_petar_alibi — Found via interrogation clickable (Petar was visiting his mother on two claimed dates).

## Interrogation Flow
### Nadia Filipovic
- Tag: clue_jealousy_language → Lie. "I used precise language as required by form 17-B. I'm reporting a violation, not expressing opinions." Defensive.
  - Contradiction: Present clue_no_violations → "The building monitor is incompetent. I know what I heard." Defensive.
- Tag: clue_complaint_dates → Lie. "Those are the nights I couldn't sleep because of his noise." Evasive.
  - Contradiction: Present clue_petar_alibi → Flustered. "Well, it was other nights too. I may have mixed up the dates." → clue_nadia_rejected (clickable: "applied for that position too")
- Tag: clue_no_violations → Lie. "The monitor didn't bother to investigate properly." Defensive.
- Tag: clue_nadia_rejected → Truth (forced). "Yes, I applied. And I was more qualified. But that's not why I filed the complaint." Emotional.
- Tag: clue_neighbor_testimony → Lie. "They're friends. Of course they'd cover for each other." Evasive.
  - High stress: "This is ridiculous. I'M the victim here!" Hostile.

### Petar Simic
- Tag: clue_jealousy_language → Truth. "She said I was flaunting? I just got a small promotion. I didn't even celebrate." Normal.
- Tag: clue_complaint_dates → Truth. "On the 5th, I was visiting my mother in Block E. I wasn't even home." Normal. → clue_petar_alibi (clickable: "visiting my mother")
- Tag: clue_no_violations → Truth. "I keep to myself. Ask anyone in the building." Normal.
- Tag: clue_neighbor_testimony → Truth. "I'm glad someone spoke up. I've been worried about this complaint." Normal.
- Tag: clue_nadia_rejected → Truth (limited). "I heard she applied too. I didn't think much of it. The decision wasn't mine." Normal.

## Verdict
- **Correct answer:** Nadia Filipovic committed Deviation: Discontent
- **Accepted violations:** discontent, false_reporting, malicious_complaint
- **Partial credit:** Convicting Petar of agitation is wrong but the system accepts it
- **Min confidence:** 75

## Resistance Choice
Not applicable.

## Narrative Hooks
- Demonstrates the Pattern can be weaponized by ordinary citizens against each other
- Player catches the real criminal — feels clever and righteous
- False complaints become a recurring problem (informant cases in secondary pool)
- Sets up the "who is the real offender?" theme that escalates through Acts 2-4
