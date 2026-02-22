# Case Brief: core_01 — "The Depot Theft"

## Meta
- **Type:** Core
- **Available Day:** 1
- **Sequence:** 1
- **Reward:** 75
- **Complexity:** Simple (Tutorial)
- **Required Tools:** Document reading
- **Deviation:** Accumulation (§2) — Theft of state rations
- **Involves Resistance:** No

## Story Context
Tutorial case. The player's first day on the job. Everything is straightforward — clear evidence, obvious guilty suspect, simple verdict. This establishes the gameplay loop before later cases complicate it. Lenka's letter is warm and hopeful. The system feels legitimate. No moral weight whatsoever.

## Synopsis
Medical supplies and food rations vanish from the Block C ration depot overnight. The "break-in" was staged — the lock was broken from inside. Access log shows guard Miroslav Zelnik entered at 2:17 AM. He claims he was responding to the alarm, but the alarm system log shows it triggered at 2:45 AM — 28 minutes AFTER his badge scan. Josip Dragic was simply walking home from night shift and was seen near the loading dock.

## Suspects
- **Miroslav Zelnik** — Night Security Guard, Block C Ration Depot. Guilty. Staged a break-in to steal three months of medical supplies. Selling them at the black market. Nervous, defensive, inconsistent with details. Worker-class speech.
- **Josip Dragic** — Block F Factory Worker. Innocent. Was walking home from night shift, seen near the depot by a patrol. Confused, cooperative, slightly annoyed at being dragged in. Worker-class speech.

## Evidence
- **Depot Access Log** — Type: Document. Badge scan records for the depot. Hotspot: Miroslav's badge scan at 2:17 AM (clue_badge_timestamp). Hotspot: Josip has no badge scan at all (clue_no_josip_badge).
- **Alarm System Report** — Type: Document. Automated alarm log. Hotspot: alarm triggered at 2:45 AM — 28 minutes after Miroslav entered (clue_alarm_discrepancy). Hotspot: alarm was triggered from inside the building, not the perimeter (clue_inside_trigger).

## Key Clues (discovery chain)
1. clue_badge_timestamp — Found via access log hotspot. Miroslav entered at 2:17 AM.
2. clue_no_josip_badge — Found via access log hotspot. Josip never badged in.
3. clue_alarm_discrepancy — Found via alarm report hotspot. 28-minute gap between entry and alarm.
4. clue_inside_trigger — Found via alarm report hotspot. Alarm triggered from inside.
5. clue_miroslav_lie — Found via interrogation clickable. Miroslav's "alarm response" story collapses.
6. clue_staged_breakin — Found via interrogation clickable. Lock broken from inside confirms staging.

## Interrogation Flow
### Miroslav Zelnik
- Tag: clue_badge_timestamp → Lie. "I badged in when the alarm went off. Standard procedure." Evasive. stressImpact: 0.08.
  - Contradiction: Present clue_alarm_discrepancy → "I... the system must have a delay. Those logs aren't always accurate." → clue_miroslav_lie (clickable: "logs aren't always accurate")
  - High stress variant: "Stop trying to confuse me with numbers!" Hostile.
- Tag: clue_alarm_discrepancy → Lie. "The alarm system has been glitchy for months. Ask anyone." Defensive. stressImpact: 0.10.
  - Contradiction: Present clue_inside_trigger → "Look, I was just doing my rounds. Things happened fast." Evasive. → clue_staged_breakin (clickable: "things happened fast")
- Tag: clue_inside_trigger → Truth (forced). "Fine. The lock was broken from inside. I didn't break in — I was already inside." Emotional. stressImpact: 0.12.
- Tag: clue_no_josip_badge → Truth. "I don't know anything about the other guy. I was inside the whole time." Normal.
- Tag: clue_staged_breakin → Truth (final). "I needed the money. My cousin runs a stall in the market. He said he could move medical supplies." Emotional.

### Josip Dragic
- Tag: clue_badge_timestamp → Truth. "I don't have a depot badge. I was walking past on my way home from the factory." Normal.
- Tag: clue_no_josip_badge → Truth. "Exactly — I never went inside. I just happened to walk past at the wrong time." Normal.
- Tag: clue_alarm_discrepancy → Truth (limited). "I heard the alarm go off, maybe around 3 AM. I kept walking. Not my business." Normal.
- All responses Normal, cooperative. Classic innocent bystander.

## Verdict
- **Correct answer:** Miroslav Zelnik committed Deviation: Accumulation
- **Accepted violations:** accumulation, theft, theft_of_state_property
- **Partial credit:** None
- **Min confidence:** 80

## Resistance Choice
Not applicable — no Resistance involvement.

## State/Resistance Paths
- **State path:** Convict Miroslav. Quick, clean, straightforward.
- No moral complexity. This is pure tutorial.

## Narrative Hooks
- Teaches all core mechanics: evidence examination, hotspot clicking, interrogation, lie detection, verdict submission
- The 28-minute gap is designed to be obvious — player should feel clever catching it
- Establishes the "feel good" competence loop that will be disrupted starting Day 5
- Miroslav's black market connection seeds the economic desperation theme
