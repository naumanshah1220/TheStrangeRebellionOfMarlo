# The Strange Rebellion of Marlo — Game Design Document

This document defines the mechanical progression, tool unlock timeline, case pool system, and verdict slot rules. For narrative/lore, see `story_bible.md`.

---

## SECTION 1: TOOL UNLOCK TIMELINE

Tools are unlocked progressively. Each unlock must be accompanied by at least one core case that **teaches** the mechanic. Secondary cases in the same day-pool may also use it.

| Day | Unlock | Description | Teaching Case |
|---|---|---|---|
| **1** | Documents + Interrogation | Read evidence, click hotspots, drag clue tags to suspects, submit verdict | Core 01 (Ration Cards) |
| **5** | Computer (File Viewer) | Disc-type evidence can be inserted into the computer to view files, photos, and video. No citizen database yet. | Core 05 or nearby |
| **7** | Citizens Database App | Computer app for looking up suspects: criminal history, DOB, address, marital status, occupation. Used to verify claims made during interrogation. | Core 07 |
| **10** | Stress Mechanic + Tone Dial | Suspects now have visible stress levels. Player uses a tone dial (Calm / Neutral / Aggressive) during interrogation. Too high → suspect lawyers up or breaks down (interrogation ends). Too low → suspect stonewalls. Introduced gradually — early interactions are low-stress, difficulty ramps mid-interrogation. | Core 10 |
| **12** | Fingerprint Duster | Player brushes evidence with powder, turns on UV lamp, fingerprint glows. Shake to remove excess powder. Compare prints visually to suspect prints displayed in Citizens Database. Manual comparison only — no auto-match. | Core 12 |
| **14** | Partial Suspect Search | Cases now arrive with incomplete suspect info (first name only, or last name + occupation). Player searches Citizens Database with partial info → multiple results returned → "Add to Suspects" button → suspect added to notebook → can now be called for interrogation. | Core 14 |
| **16** | Fingerprint Scanner → DB Cross-Search | Fingerprint Duster now has a "Scan" function. Scanned prints can be inserted into the computer (as a disc) to auto-search the Citizens Database. Returns matching citizen profiles. Bridges physical evidence to digital identification. | Core 16 |
| **18** | Bribery System | Suspects may offer bribes during interrogation. A bribe prompt appears with an amount. Player can Accept (gains money, case is compromised — consequences later) or Refuse (interrogation continues normally). Overseer may also offer "reassignment bonuses" to redirect verdicts. | Core 17 or 18 |
| **20** | Spectrograph + Reference Book | Physical tool for analyzing foreign substances on evidence. Player places evidence on spectrograph → spectrum readout displayed (colored peaks). Player must consult "A Guide to Spectrography" book (added to desk library at unlock) to match spectrum patterns to known substances. Manual lookup — no auto-identification. | Core 20 |
| **22** | Resistanz Cipher Decoder | A codebook is found or delivered to the player. Encrypted messages appear as evidence — unreadable until the player uses the decoder. Manual symbol-matching using the reference book (similar to spectrograph pattern). Opens double-life gameplay: decode messages for the Council, or use knowledge for the Resistance? | Core or story event ~Day 22 |
| **24** | Surveillance Photo Analysis | Council provides surveillance photos from street cameras. Player gets a magnifying/zoom tool to identify faces in crowd shots. Cross-reference faces against Citizens Database. Can be used for justice ("find the criminal") or oppression ("identify Resistance members"). | Core 24 |
| **26** | Cross-Case File Access | Player gains access to a closed case archive. Previous cases (that the player solved) can be re-examined. Names, evidence, and verdicts from old cases surface as connections in new cases. The conspiracy threads converge — "these cases are all connected." | Core 26+ / Act 5 |

### Tool Availability Rules
- A case **MUST NOT** require a tool the player hasn't unlocked yet
- A case **MAY** optionally benefit from a tool not yet unlocked (e.g., fingerprints exist on evidence but the duster isn't required to solve the case)
- When a tool is unlocked, a brief tutorial overlay explains its use (first time only)
- Tools remain available forever once unlocked
- The case validator must check: `required_tools ⊆ unlocked_tools(day)`

---

## SECTION 2: VERDICT SLOT SYSTEM

### Core Rules
1. **"Who" is always required.** Every verdict must identify a suspect. Getting "Who" wrong has consequences.
2. **All other slots are optional.** They award bonus knots when correctly filled.
3. **Wrong "Who" tolerance:** The player receives a warning for incorrect suspect identification. After **3 cumulative wrong "Who" verdicts**, the player is fired (game over — Ending 5: The Coward).
4. **Optional slot bonuses:** Each correctly filled optional slot adds a percentage bonus to the base reward.

### Standard Slot Types

| Slot Type | Label | When Used | Bonus |
|---|---|---|---|
| `Suspect` | "Who" | **Every case** (required) | — (base reward) |
| `Violation` | "What deviation" | Most cases | +15% base reward |
| `Motive` | "Why" | Cases with discoverable motive | +10% base reward |
| `Method` | "How" | Cases with clear method evidence | +10% base reward |
| `Accomplice` | "Who helped" | Cases with multiple guilty parties | +15% base reward |
| `Evidence` | "Key evidence" | Complex cases with pivotal evidence | +10% base reward |
| `Property` | "What was damaged/stolen" | Property crime cases | +5% base reward |

### Slot Progression by Act

**Act 1 (Days 1-6):** Simple slots — Who + Violation only. Teaches the system.

**Act 2 (Days 7-12):** Add Motive slot. Player starts noticing WHY people deviate.

**Act 3 (Days 13-18):** Add Accomplice and Method slots. Crimes get complex, multiple guilty parties.

**Act 4 (Days 20-25):** Add Evidence slot. Player must identify the pivotal piece of evidence (e.g., "the confession tape" or "the serial number match").

**Act 5 (Days 27-30):** Custom slots. The Final Audit has unique meta-slots: Subject, Finding, Recommendation.

### Slot Sources

| Source | Meaning |
|---|---|
| `CaseOnly` | Options come only from this case's suspects/evidence |
| `GlobalOnly` | Options come from a global pool (e.g., all 7 Tenet violations) |
| `CaseAndGlobal` | Both case-specific and global options |
| `FromDiscoveredTags` | Options unlock as the player discovers clues (clueVerdictMappings) |

---

## SECTION 3: SECONDARY CASE POOL SYSTEM

### How It Works
- Secondary cases are assigned to **day-range pools** (e.g., "Days 1-5", "Days 6-10")
- On each day, the game selects secondary cases from the active pool to fill the player's case queue alongside any core cases
- Cases are picked **randomly without replacement** within a playthrough
- Pool sizes should be **generous** (8-12+ per range) so fast players don't run dry
- If a player completes all cases in a pool, the pool refills from the next range (early access to harder cases as a reward for efficiency)

### Pool Assignments

| Pool | Day Range | Required Tools | Max Complexity | Target Count |
|---|---|---|---|---|
| Pool A | Days 1-5 | Documents + Interrogation | Simple | 10-12 cases |
| Pool B | Days 6-10 | + Computer, Citizens DB, Stress | Medium | 10-12 cases |
| Pool C | Days 11-15 | + Fingerprint, Partial Search | Medium-Hard | 10-12 cases |
| Pool D | Days 16-20 | + FP Scanner, Bribery, Spectrograph | Hard | 8-10 cases |
| Pool E | Days 21-30 | All tools | Hard-Complex | 8-10 cases |

### Pool Validation Rules
- Every case in a pool must be solvable using **only** tools available at that pool's start day
- Cases may have optional tool usage from later pools (flagged as `optionalTools`)
- Core cases are **never** in the secondary pool — they are assigned to specific days
- A case's `firstAvailableDay` determines its pool assignment
- The case validator must verify: `case.requiredTools ⊆ pool.availableTools`

### Secondary Case Categories
Each pool should contain a mix of these categories for variety:

| Category | Description | Tone |
|---|---|---|
| Domestic | Neighbor disputes, family conflicts, shared-resource arguments | Human, sympathetic |
| Petty Crime | Shoplifting, small contraband, vandalism | Procedural, satisfying |
| Workplace | Quota violations, unauthorized breaks, fraternization | Systemic critique |
| Romantic | Secret relationships, intercepted letters, denied registrations | Emotional |
| Absurd | "Excessive smiling", suspicious gardening, weather complaints | Darkly funny |
| Connected | Minor offenses that seed main arc characters/locations | Reward attentive players |
| Informant | Anonymous tips — genuine or personal vendettas? | Trust assessment |
| Economic | Black market, counterfeit cards, under-table labor | Desperation |
| Youth | Juveniles (16-18) charged as adults | Uncomfortable |
| Historical | Cold cases, old evidence, faded documents | Puzzle-like |

---

## SECTION 4: CASE DIFFICULTY FRAMEWORK

### Complexity Levels

| Level | Suspects | Evidence Items | Clues | Tools Required | Contradictions | Description |
|---|---|---|---|---|---|---|
| **Simple** | 2 | 2-3 | 5-7 | 1-2 | 0-1 | Clear guilty party, obvious evidence trail |
| **Medium** | 2-3 | 3-4 | 7-10 | 2-3 | 1-2 | Misdirection or conflicting testimony |
| **Hard** | 3-4 | 4-5 | 10-14 | 3-4 | 2-3 | Multiple guilty parties, tool-gated clues |
| **Complex** | 4-5 | 5-6 | 14-18 | 4-5 | 3+ | Multi-step investigation chains, political stakes |

### Time Pressure
- Workday runs 06:00 → 18:00 (game time, not real time)
- Core cases must be completed before End Day
- Secondary cases can be deferred (but unfinished cases reduce daily rating)
- Overtime (past 18:00) increases stress, reduces pay, worsens family letters

---

## SECTION 5: REVISED CORE CASE ASSIGNMENTS

### Act 1: The Good Soldier (Days 1-6)

| Day | Case ID | Title | Crime | Suspects | Tools | Slots |
|---|---|---|---|---|---|---|
| 1 | core_01 | The Missing Ration Cards | Surplus (§1) — exceeding caloric allocation | 2 (worker + co-worker) | Documents | Who (req) + Violation |
| 2 | core_02 | The Pumpkin Brawl | Agitation (§5) — public disturbance, property destruction | 3 (aggressor + retaliator + bystander) | Documents + Interrogation | Who (req) + Violation + Property |
| 3 | core_03 | The Curfew Runner | Agitation (§5) — curfew violation, Enforcer injury | 2-3 (young worker + older worker + Enforcer witness) | Documents + Photo evidence (introduces Photo type) | Who (req) + Violation |
| 4 | core_04 | The Warehouse Ring | Accumulation (§2) — theft of state materials | 2 (clerk guilty, driver innocent) | Documents | Who (req) + Violation |
| 5 | core_05 | The Hoarder's Daughter (SEED) | Surplus (§1) — consuming beyond allocation | 2 (father + neighbor witness) | Documents + Computer (new — disc evidence intro) | Who (req) + Violation |
| 6 | — | Carry-over day | — | — | — | — |

**Act 1 Notes:**
- Core 03: Young man has a registered cross-block relationship with valid daytime visitation permit. He violated curfew because her shift ends at 22:00 — only time they can see each other. Relationship is legal; the curfew breach is the crime.
- Core 05: Computer unlocks here. One evidence item is a disc containing the ration purchase receipts database. Player inserts disc into computer to view the records.

### Act 2: Cracks in the Pattern (Days 7-12)

| Day | Case ID | Title | Crime | Suspects | Tools | Slots |
|---|---|---|---|---|---|---|
| 7 | core_06 | The Ambitious Teacher | Elevation (§6) — self-promotion beyond station | 3 (teacher + jealous colleague + negligent Board Secretary) | Documents + Citizens DB (new — verify Board Secretary's employment, Anton's application status) | Who + Violation + Motive |
| 8 | core_07 | The Librarian's Books | Accumulation (§2) — possessing banned materials | 2 (librarian + neighbor witness) | Documents + Citizens DB (handwriting match on margin annotations vs. employment form) | Who + Violation |
| 9 | core_08 | The Unregistered Couple | Deviance (§7) — unregistered social bond | 2 (both guilty) | Documents + Citizens DB (verify registration denial) | Who + Violation + Motive |
| 10 | core_09 | The Medicine Dilemma | Surplus (§1) — diverting medical supplies | 4 (orderly + admin + doctor + driver) | Documents + Stress mechanic (new — first tense multi-suspect interrogation) | Who + Violation + Accomplice |
| 11 | core_10 | The Silenced Inspector | Discontent (§4) — fabricated charge | 2 (inspector is innocent, director filed false charge) | Documents + Citizens DB + Stress | Who + Violation + Motive |
| 12 | — | Carry-over day | — | — | — | — |

**Act 2 Notes:**
- Core 07 (Librarian): No fingerprints needed. The eureka moment is matching Luka's **handwriting in the book margins** against his **signed library employment form** (both are document evidence). Same conclusive proof, purely visual comparison.
- Core 09 (Medicine): No spectrograph needed. The eureka is a **batch number cross-reference** — the "expired" disposal report lists batch numbers that exactly match the labels on seized black market pill packaging (a Photo evidence item). Player spots the match by reading both documents.
- Core 10 teaches the Stress mechanic: 4 suspects means longer interrogations where stress management matters for the first time.

### Act 3: The Corruption (Days 13-18)

| Day | Case ID | Title | Crime | Suspects | Tools | Slots |
|---|---|---|---|---|---|---|
| 13 | core_11 | The Dead Witness | Agitation/Homicide | 4 | + Fingerprint (new) | Who + Violation + Method + Accomplice |
| 14 | core_12 | The Party Photographs | Espionage | 3 | + Partial Search (new) | Who + Violation + Accomplice |
| 15 | core_13 | The Old Friend | Accumulation (§2) | 4 | All Act 3 tools | Who + Violation + Accomplice |
| 16 | core_14 | The Accountant's Numbers | Discontent/Accumulation | 2 | + FP Scanner (new) | Who + Violation + Evidence |
| 17 | core_15 | The Councilor's Nephew | Accumulation (§2) | 2 | All Act 3 tools + Bribery intro | Who + Violation |
| 18 | core_16 | The Falsified Audit | Dereliction/Accumulation | 2 | All Act 3 tools | Who + Violation + Evidence |

### Act 4: The Reckoning (Days 20-25)

| Day | Case ID | Title | Crime | Suspects | Tools | Slots |
|---|---|---|---|---|---|---|
| 20 | core_17 | The False Flag | Agitation/Terrorism | 4 | + Spectrograph (new) | Who + Violation + Method + Evidence |
| 21 | core_18 | The Poisoned Well | Agitation/Poisoning | 4 | Spectrograph + all | Who + Violation + Method + Accomplice |
| 22 | core_19 | The Purge List | Dereliction (§3) | 3 | + Cipher Decoder (new) | Who + Violation + Evidence |
| 23 | core_20 | The Assassination | Agitation/Murder | 4 | + Surveillance Photos (new) | Who + Violation + Method + Accomplice |
| 24 | core_21 | The Overseer Unmasked | Accumulation/Espionage | 3 | All tools | Who + Violation + Evidence |
| 25 | core_22 | The Confession Tape | Accumulation (§2) | 1+ | Computer (disc playback) | Who + Violation |

### Act 5: The Choice (Days 27-30)

| Day | Case ID | Title | Crime | Suspects | Tools | Slots |
|---|---|---|---|---|---|---|
| 27 | core_23 | The Vesna File | Multiple | 1 | All + Cross-Case Archive (new) | Who + custom |
| 28 | core_24 | The Final Audit | Meta | The system itself | All | Subject + Finding + Recommendation |
| 29-30 | — | Epilogue | — | — | — | — |

---

## SECTION 6: CROSS-CASE CONNECTIONS (Tracked Characters)

Characters that appear across multiple cases, building the conspiracy thread:

| Character | First Appears | Returns In | Connection |
|---|---|---|---|
| Marta Rezek | Secondary (Market) | Multiple secondaries | Market vendor, sees everything, Resistance sympathizer |
| Officer Dusek | Core 02 (Brawl) | Core 11 (Dead Witness), Core 20 (Assassination) | Enforcer who delivers suspects. Privately disgusted. Potential Act 4 ally. |
| Viktor Sevorin | Secondary (witness) | Multiple cores | Block A bureaucrat, Council informant |
| Dr. Yara Petrovic | Core 10 (Medicine) | Core 18 (Poisoned Well), Core 21 (Overseer) | Bureau forensics. Sardonic. Knows more than she says. |
| Pyotr Radin | Letters (Day 6+) | Core 14 (Accountant — name appears), Core 22 (Confession Tape courier) | Lenka's brother. Resistance. |
| Overseer V. Terzic | Letters (Day 1+) | Core 21 (identity revealed) | Marlo's handler. Colonel, Military Intelligence. |
| Danilo Radovanovic | Core 13 (Old Friend) | Core 15 (Nephew) | Third Councilor's nephew. Arrogant. Recurring antagonist. |
| Borislav Filipovic | Core 11 (Dead Witness — victim) | Core 21 (referenced) | Witness who was silenced. Was about to defect to Resistance. |
| Vesna/Sonja | Core 22 (coded message) | Core 23 (The Vesna File) | Resistance leader. Dragojevic heir. |

### Cross-Case File Access (Day 26+)
When this tool unlocks, the following connections become discoverable:
- Core 01 (Ration Cards) → Core 09 (Medicine Dilemma): same supply chain, same black market
- Core 04 (Warehouse Ring) → Core 09 (Medicine): stolen materials funded medical supply theft
- Core 11 (Dead Witness) → Core 14 (Accountant): Borislav was going to testify about the same funds
- Core 13 (Old Friend) → Core 15 (Nephew): same construction fraud, escalating
- Core 17 (False Flag) → Core 20 (Assassination): same military unit, same weapons
- Core 19 (Purge List) → Core 21 (Overseer): Terzic authored the list

---

## SECTION 7: ECONOMY & CONSEQUENCES

### Verdict Rewards
- **Base reward:** Set per case (65-100 knots)
- **Optional slot bonuses:** +5% to +15% per correct optional slot
- **State bonus:** Extra reward for Council-preferred verdict (available Act 2+)
- **Bribe:** One-time cash offer from suspect or Overseer (available Day 18+)
- **Wrong "Who" penalty:** -50% base reward + warning

### Wrong "Who" Consequences
| Strike | Consequence |
|---|---|
| 1st wrong | Warning letter from Overseer: "An error in judgment, Dashev. Corrected." |
| 2nd wrong | Formal reprimand. Pay docked 20% for the day. |
| 3rd wrong | **Fired.** Game over → Ending 5: The Coward. |

### Daily Expenses (approximate)
- Rent: 15 knots/day
- Food (family of 4): 20 knots/day
- Utilities: 5 knots/day
- **Base survival: ~40 knots/day**
- Medicine (when needed): 50 knots
- Random events: 10-30 knots (school fees, clothing, repairs)

This means a base reward of 65-75 knots covers survival with little margin. State bonuses and optional slot bonuses are the difference between comfort and desperation.
