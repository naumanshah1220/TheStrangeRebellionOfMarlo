# Case Brief: core_08 — "The Unregistered Couple"

## Meta
- **Type:** Core
- **Available Day:** 9
- **Sequence:** 8
- **Reward:** 75
- **Complexity:** Medium
- **Required Tools:** Document reading
- **Deviation:** Deviance (§7) — Unregistered social bond
- **Involves Resistance:** No

## Story Context
Love itself is a crime. Most emotionally naked case in Act 2. Two citizens from different Blocks in an unregistered romantic relationship. Cross-block registration takes six months and is routinely denied. Dragan tried to register — the clerk laughed at him. A love letter intercepted in mail review proves the relationship. Dragan's plea: "Don't separate us."

Lenka's letter this day mentions Eli's worsening cough — personal stakes rising.

## Synopsis
Ana Vidovic and Dragan Lazic have been in an unregistered romantic relationship for eight months. Ana lives in Block C, Dragan in Block D. Cross-block social bond registration takes six months minimum and is routinely denied. Dragan tried to register — the clerk laughed at him. A love letter intercepted in routine mail review proves the relationship.

## Suspects
- **Ana Vidovic** — Clinic Receptionist. Guilty. Quiet, protective of Dragan, refuses to say anything that might worsen his situation. Emotional but controlled. Worker-class speech.
- **Dragan Lazic** — Postal Worker. Guilty. Direct, sincere, doesn't understand why love requires paperwork. Emotional. Worker-class speech.

## Evidence
- **Intercepted Love Letter** — Type: Document. From Dragan to Ana, intercepted during routine mail screening. Hotspot: explicit declaration of relationship (clue_relationship_proof). Hotspot: mention of "eight months" together (clue_relationship_duration).
- **Registration Inquiry Record** — Type: Document. From the civil registration office. Hotspot: Dragan's cross-block registration request, stamped "DENIED — Insufficient Grounds" (clue_registration_denied). Hotspot: processing time noted as "6-9 months pending review" (clue_processing_time).

## Key Clues (discovery chain)
1. clue_relationship_proof — From letter. Clear evidence of romantic bond.
2. clue_relationship_duration — From letter. Eight months unregistered.
3. clue_registration_denied — From registration record. Dragan tried to register and was denied.
4. clue_processing_time — From registration record. System makes legal compliance nearly impossible.
5. clue_dragan_attempt — Found via interrogation clickable. Dragan describes trying to register.
6. clue_ana_protection — Found via interrogation clickable. Ana's protectiveness reveals depth.

## Interrogation Flow
### Ana Vidovic
- Tag: clue_relationship_proof → Truth (reluctant). "Yes. We love each other. I didn't know that was something I needed to apologize for." Emotional.
- Tag: clue_relationship_duration → Truth. "Eight months. The best eight months of my life." Emotional.
- Tag: clue_registration_denied → Truth. "He tried. He went to the office, filled out the forms, waited in line for four hours. They laughed at him." Emotional. → clue_ana_protection (clickable: "laughed at him")
- Tag: clue_processing_time → Truth. "Six to nine months to process, and then they deny it anyway. What are we supposed to do? Stop feeling?" Emotional.
- All responses truthful and emotional. She won't lie about her feelings.

### Dragan Lazic
- Tag: clue_relationship_proof → Truth. "I wrote that letter. Every word of it. I'm not ashamed." Emotional.
- Tag: clue_relationship_duration → Truth. "Eight months. I count every day." Normal.
- Tag: clue_registration_denied → Truth. "I tried to register. I went to the civil office, filled out form 28-A, paid the processing fee. The clerk read 'Block D to Block C' and said 'That's not how it works.' He stamped it denied." Emotional. → clue_dragan_attempt (clickable: "stamped it denied")
- Tag: clue_dragan_attempt → Truth. "I did everything right. The system said no. So what was I supposed to do — stop loving her?" Emotional. "Don't separate us. Please."
- All responses truthful. He doesn't lie because he doesn't think he's wrong.

## Verdict
- **Correct answer:** Both committed Deviation: Deviance
- **Accepted violations:** deviance, unregistered_bond, unauthorized_relationship
- **Partial credit:** Convicting either one alone is accepted
- **Min confidence:** 70

## Resistance Choice
Not applicable — but the emotional cost of this conviction is the highest so far.

## Narrative Hooks
- Love as a crime — the most emotionally direct case in Act 2
- Dragan tried to follow the rules and was denied — system designed to be impossible
- "Don't separate us" should be the line that stays with the player
- Lenka's letter mentions Eli's cough worsening — personal family stress rising
- Registration bureaucracy mirrors real-world dehumanizing paperwork
