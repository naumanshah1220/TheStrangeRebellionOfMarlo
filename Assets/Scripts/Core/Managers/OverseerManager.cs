using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages overseer notes and their trigger-based distribution
/// Similar to BookManager but for overseer notes with trigger system
/// </summary>
public class OverseerManager : SingletonMonoBehaviour<OverseerManager>
{
    
    [Header("Overseer Hand")]
    public HorizontalCardHolder overseerHand;

    [Header("Mat Delivery")]
    [Tooltip("Mat hand where morning items are delivered as stacked big cards")]
    public HorizontalCardHolder matHand;
    
    [Header("Note Collection")]
    public List<OverseerNotes> allNotes = new List<OverseerNotes>();
    
    [Header("Settings")]
    public int maxNotesInHand = 5; // Maximum notes that can be in hand at once

    protected override void OnSingletonAwake() { }

    /// <summary>
    /// Clears all cards from the overseer hand (called at each day start before new deliveries).
    /// </summary>
    public void ClearOverseerHand()
    {
        if (overseerHand == null) return;

        var cardsToDelete = overseerHand.Cards.ToList();
        foreach (var card in cardsToDelete)
        {
            overseerHand.DeleteCard(card);
        }

        Debug.Log($"[OverseerManager] Cleared {cardsToDelete.Count} card(s) from overseer hand");
    }

    private void Start()
    {
        // Wait for managers to be ready, then set up event listeners
        StartCoroutine(WaitForManagers());
    }
    
    private System.Collections.IEnumerator WaitForManagers()
    {
        // Wait until DaysManager.Instance is available
        while (DaysManager.Instance == null)
        {
            yield return null;
        }
        
        // Wait until GameManager.Instance is available
        while (GameManager.Instance == null)
        {
            yield return null;
        }
        
        // Set up the event listeners
        SetupEventListeners();
        
        // Start checking for hour-based triggers
        StartCoroutine(CheckHourlyTriggers());
        
        Debug.Log("[OverseerManager] Event listeners connected");
    }
    
    protected override void OnSingletonDestroy()
    {
        RemoveEventListeners();
    }
    
    private void SetupEventListeners()
    {
        // Listen to day events
        if (DaysManager.Instance != null)
        {
            DaysManager.Instance.onDayStart.AddListener(OnDayStart);
            DaysManager.Instance.onDayEnd.AddListener(OnDayEnd);
        }
        
        // Listen to case events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened += OnCaseOpened;
            GameManager.Instance.OnCaseClosed += OnCaseClosed;
        }
    }
    
    private void RemoveEventListeners()
    {
        if (DaysManager.Instance != null)
        {
            DaysManager.Instance.onDayStart.RemoveListener(OnDayStart);
            DaysManager.Instance.onDayEnd.RemoveListener(OnDayEnd);
        }
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened -= OnCaseOpened;
            GameManager.Instance.OnCaseClosed -= OnCaseClosed;
        }
    }

    /// <summary>
    /// Called when a day starts - check for start-of-day triggers
    /// </summary>
    private void OnDayStart()
    {
        int currentDay = DaysManager.Instance?.GetCurrentDay() ?? 1;
        Debug.Log($"[OverseerManager] Day {currentDay} started - checking for notes");
        CheckAndTriggerNotes(NoteTrigger.StartOfDay, currentDay);
    }
    
    /// <summary>
    /// Called when a day ends - check for all cases solved triggers
    /// </summary>
    private void OnDayEnd()
    {
        int currentDay = DaysManager.Instance?.GetCurrentDay() ?? 1;
        
        // Check if all cases were solved
        if (WereAllCasesSolvedToday())
        {
            CheckAndTriggerNotes(NoteTrigger.AllCasesSolved, currentDay);
        }
    }
    
    /// <summary>
    /// Called when a case is opened - check for case open triggers
    /// </summary>
    private void OnCaseOpened(Case openedCase)
    {
        if (openedCase == null) return;
        
        CheckAndTriggerNotes(NoteTrigger.CaseXOpen, 0, openedCase.caseID);
    }
    
    /// <summary>
    /// Called when a case is closed - check for case submission triggers
    /// </summary>
    private void OnCaseClosed(Case closedCase)
    {
        if (closedCase == null) return;
        
        CheckAndTriggerNotes(NoteTrigger.CaseXSubmission, 0, closedCase.caseID);
    }
    
    /// <summary>
    /// Main method to check and trigger notes based on conditions
    /// </summary>
    private void CheckAndTriggerNotes(NoteTrigger trigger, int dayNumber, string caseId = "")
    {
        Debug.Log($"[OverseerManager] Checking {trigger} trigger for day {dayNumber}. Total notes: {allNotes.Count}");
        
        var eligibleNotes = allNotes.Where(note => 
            ShouldTriggerNote(note, trigger, dayNumber, caseId)
        ).ToList();
        
        Debug.Log($"[OverseerManager] Found {eligibleNotes.Count} eligible notes for {trigger}");
        
        if (eligibleNotes.Count == 0)
        {
            Debug.Log($"[OverseerManager] No eligible notes found. Available notes:");
            foreach (var note in allNotes)
            {
                Debug.Log($"  - '{note.title}': trigger={note.trigger}, triggerDay={note.triggerDay}");
            }
        }
        
        foreach (var note in eligibleNotes)
        {
            if (CanAddNoteToHand())
            {
                AddNoteToHand(note);
                Debug.Log($"[OverseerManager] Added note '{note.title}' to overseer hand (trigger: {trigger})");
            }
        }
    }
    
    /// <summary>
    /// Determines if a note should be triggered based on the current conditions
    /// </summary>
    private bool ShouldTriggerNote(OverseerNotes note, NoteTrigger trigger, int dayNumber, string caseId)
    {
        switch (trigger)
        {
            case NoteTrigger.StartOfDay:
                return note.trigger == NoteTrigger.StartOfDay && note.triggerDay == dayNumber;
                
            case NoteTrigger.DayXAfterYHours:
                return note.trigger == NoteTrigger.DayXAfterYHours && 
                       note.triggerDay == dayNumber && 
                       IsTimeForHourTrigger(note);
                
            case NoteTrigger.CaseXOpen:
                return note.trigger == NoteTrigger.CaseXOpen && note.triggerCaseId == caseId;
                
            case NoteTrigger.CaseXSubmission:
                return note.trigger == NoteTrigger.CaseXSubmission && note.triggerCaseId == caseId;
                
            case NoteTrigger.AllCasesSolved:
                return note.trigger == NoteTrigger.AllCasesSolved && note.triggerDay == dayNumber;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Checks if enough time has passed for a DayXAfterYHours trigger
    /// </summary>
    private bool IsTimeForHourTrigger(OverseerNotes note)
    {
        if (DaysManager.Instance == null) return false;
        
        float currentGameHour = DaysManager.Instance.GetCurrentGameHour();
        float targetHour = DaysManager.Instance.startHour + note.triggerHoursAfterStart;
        
        return currentGameHour >= targetHour;
    }
    
    /// <summary>
    /// Coroutine to check for hour-based triggers every minute
    /// </summary>
    private System.Collections.IEnumerator CheckHourlyTriggers()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Check every minute
            
            if (DaysManager.Instance != null && DaysManager.Instance.IsDayActive())
            {
                int currentDay = DaysManager.Instance.GetCurrentDay();
                CheckAndTriggerNotes(NoteTrigger.DayXAfterYHours, currentDay);
            }
        }
    }
    
    /// <summary>
    /// Checks if there's room in the overseer hand for a new note
    /// </summary>
    private bool CanAddNoteToHand()
    {
        if (overseerHand == null) return false;
        return overseerHand.Cards.Count < maxNotesInHand;
    }
    
    /// <summary>
    /// Adds a note to the overseer hand
    /// </summary>
    private void AddNoteToHand(OverseerNotes note)
    {
        if (overseerHand == null) return;
        
        // Use the proper LoadCardsFromData method to load the note
        overseerHand.LoadCardsFromData(new List<OverseerNotes> { note }, false);
    }
    
    /// <summary>
    /// Checks if all cases for today were solved
    /// </summary>
    private bool WereAllCasesSolvedToday()
    {
        if (DaysManager.Instance == null) return false;
        
        var todayCases = DaysManager.Instance.todaysTotalCases;
        var closedCases = DaysManager.Instance.GetClosedCasesForToday();
        
        return todayCases.Count > 0 && todayCases.Count == closedCases.Count;
    }
    
    /// <summary>
    /// Manually trigger a specific note (for testing or special events)
    /// </summary>
    public void ManuallyTriggerNote(string noteId)
    {
        var note = allNotes.FirstOrDefault(n => n.id == noteId);
        if (note != null && CanAddNoteToHand())
        {
            AddNoteToHand(note);
            Debug.Log($"[OverseerManager] Manually triggered note '{note.title}'");
        }
    }
    
    /// <summary>
    /// Get all notes for a specific trigger type
    /// </summary>
    public List<OverseerNotes> GetNotesByTrigger(NoteTrigger trigger)
    {
        return allNotes.Where(note => note.trigger == trigger).ToList();
    }
    
    /// <summary>
    /// Reset all notes (useful for testing or new game)
    /// </summary>
    public void ResetAllNotes()
    {
        // Clear all notes from overseer hand
        if (overseerHand != null)
        {
            overseerHand.ClearCards();
            // Clear visual children
            foreach (Transform child in overseerHand.transform)
            {
                Destroy(child.gameObject);
            }
        }
        Debug.Log("[OverseerManager] Reset all notes");
    }

    // --- Bonus Letter System ---

    private BonusLetterData pendingBonusLetter;

    /// <summary>
    /// Build a BonusLetterData from previous day's results.
    /// </summary>
    public static BonusLetterData BuildBonusLetter(DayResults previousDayResults)
    {
        if (previousDayResults == null) return null;

        var letter = new BonusLetterData
        {
            forDay = previousDayResults.dayNumber,
            caseBreakdown = new List<CaseResult>(previousDayResults.caseResults),
            totalBonus = previousDayResults.totalBonusEarned
        };

        // Overtime penalty: 5 per overtime hour
        if (previousDayResults.hadOvertime)
        {
            letter.hasPenalty = true;
            letter.penaltyAmount = previousDayResults.overtimeHours * 5f;
            letter.penaltyReason = $"Overtime ({previousDayResults.overtimeHours:F1} hours)";
        }

        // Unfinished core penalty
        if (previousDayResults.unfinishedCoreCount > 0)
        {
            letter.hasPenalty = true;
            letter.penaltyAmount += previousDayResults.unfinishedCoreCount * 20f;
            string coreReason = $"Unfinished core cases ({previousDayResults.unfinishedCoreCount})";
            letter.penaltyReason = string.IsNullOrEmpty(letter.penaltyReason)
                ? coreReason
                : $"{letter.penaltyReason}; {coreReason}";
        }

        // Determine overseer message based on performance
        float avgConfidence = previousDayResults.AverageConfidence();
        int caseCount = previousDayResults.caseResults.Count;
        bool allCorrect = previousDayResults.caseResults.TrueForAll(r => r.isFullyCorrect);

        if (allCorrect && caseCount > 0)
        {
            letter.overseerMessage = "The Council commends your precision, Analyst. Every verdict was flawless. Continue this standard of excellence and advancement will follow.";
        }
        else if (avgConfidence > 70f)
        {
            letter.overseerMessage = "Satisfactory work. The Council rewards diligence. Your analysis was competent, though perfection remains the expectation.";
        }
        else if (avgConfidence > 40f)
        {
            letter.overseerMessage = "Your performance has been noted. The Bureau expects improvement. Review your methodology — carelessness is not tolerated.";
        }
        else if (caseCount > 0)
        {
            letter.overseerMessage = "Your work yesterday was unacceptable. The Council does not employ analysts who cannot meet basic standards. Consider this a warning.";
        }
        else
        {
            letter.overseerMessage = "No cases were processed yesterday. The Bureau does not pay for inactivity. Rectify this immediately.";
        }

        // Append overtime note if applicable
        if (previousDayResults.hadOvertime)
        {
            letter.overseerMessage += $"\n\nAdditionally, your overtime of {previousDayResults.overtimeHours:F1} hours has been noted. A penalty has been applied.";
        }

        Debug.Log($"[OverseerManager] Built bonus letter for day {letter.forDay}: bonus=${letter.totalBonus:F0}, penalty=${letter.penaltyAmount:F0}, net=${letter.NetBonus:F0}");
        return letter;
    }

    /// <summary>
    /// Deliver a bonus letter to the overseer hand. Called by FlowController at day start.
    /// The bonus is credited when the player reads/acknowledges the letter.
    /// </summary>
    public void DeliverBonusLetter(BonusLetterData data)
    {
        if (data == null) return;

        pendingBonusLetter = data;

        // Create a runtime OverseerNotes ScriptableObject to represent the bonus letter
        var bonusNote = ScriptableObject.CreateInstance<OverseerNotes>();
        bonusNote.id = $"bonus_day_{data.forDay}";
        bonusNote.title = "Performance Review";
        bonusNote.sender = "Senior Overseer Harlan";
        bonusNote.description = data.overseerMessage;

        if (data.NetBonus > 0f)
            bonusNote.description += $"\n\nBonus credited: ${data.NetBonus:F0}";
        if (data.hasPenalty)
            bonusNote.description += $"\nPenalty: -${data.penaltyAmount:F0} ({data.penaltyReason})";

        bonusNote.trigger = NoteTrigger.StartOfDay;
        bonusNote.triggerDay = data.forDay + 1;

        // Add to hand
        if (CanAddNoteToHand())
        {
            AddNoteToHand(bonusNote);
            Debug.Log($"[OverseerManager] Bonus letter delivered for day {data.forDay}: ${data.NetBonus:F0}");
        }

        // Credit the bonus immediately (player will see letter as confirmation)
        ApplyBonusCredit(data);
    }

    private void ApplyBonusCredit(BonusLetterData data)
    {
        if (data == null) return;

        float netBonus = data.NetBonus;
        if (netBonus > 0f)
        {
            var playerProgress = PlayerProgressManager.Instance;
            if (playerProgress != null)
            {
                playerProgress.ApplyBonus(netBonus);
                Debug.Log($"[OverseerManager] Bonus of ${netBonus:F0} credited to player savings");
            }
        }
    }

    // --- Morning Delivery System ---
    // Newspaper, letters, bonuses, and notices are delivered as Overseer Hand cards
    // staggered one by one after the workday begins.

    [Header("Delivery Settings")]
    public float deliveryStaggerDelay = 1.5f;
    public float initialDeliveryDelay = 1.0f;

    /// <summary>
    /// Deliver all morning items (newspaper, letter, bonus, notices) as staggered Overseer cards.
    /// Called by FlowController after workday starts.
    /// </summary>
    public void DeliverMorningItems(DayBriefingData briefingData, BonusLetterData bonusData)
    {
        var items = new List<OverseerNotes>();

        // 1. Newspaper (day 2+)
        if (briefingData.newspaperData != null && briefingData.newspaperData.articles.Count > 0)
        {
            items.Add(BuildNewspaperNote(briefingData.dayNumber, briefingData.newspaperData));
        }

        // 2. Bonus / performance review letter (day 2+)
        if (bonusData != null)
        {
            items.Add(BuildBonusNote(bonusData));
            ApplyBonusCredit(bonusData);
        }

        // 3. Family letter
        if (!string.IsNullOrEmpty(briefingData.letterBody))
        {
            items.Add(BuildFamilyLetterNote(briefingData.dayNumber, briefingData.letterFrom, briefingData.letterBody));
        }

        // 4. Unlock notices
        if (briefingData.unlockNotices != null)
        {
            foreach (var notice in briefingData.unlockNotices)
            {
                if (!string.IsNullOrEmpty(notice))
                    items.Add(BuildUnlockNoticeNote(briefingData.dayNumber, notice));
            }
        }

        if (items.Count > 0)
        {
            StartCoroutine(DeliverStaggered(items));
            Debug.Log($"[OverseerManager] Queued {items.Count} morning item(s) for delivery");
        }
    }

    private OverseerNotes BuildNewspaperNote(int day, NewspaperData data)
    {
        var note = ScriptableObject.CreateInstance<OverseerNotes>();
        note.id = $"newspaper_day_{day}";
        note.title = "THE DAILY PATTERN";
        note.sender = "State Press Bureau";

        // Main headline + secondary articles
        string body = $"<b>{data.mainHeadline}</b>";
        foreach (var article in data.articles)
        {
            // Skip duplicate of main headline
            if (article.headline == data.mainHeadline) continue;
            body += $"\n\n<b>{article.headline}</b>\n{article.body}";
        }

        note.description = body;
        note.trigger = NoteTrigger.StartOfDay;
        note.triggerDay = day;
        return note;
    }

    private OverseerNotes BuildBonusNote(BonusLetterData data)
    {
        var note = ScriptableObject.CreateInstance<OverseerNotes>();
        note.id = $"bonus_day_{data.forDay}";
        note.title = "Performance Review";
        note.sender = "Senior Overseer Harlan";
        note.description = data.overseerMessage;

        if (data.NetBonus > 0f)
            note.description += $"\n\nBonus credited: ${data.NetBonus:F0}";
        if (data.hasPenalty)
            note.description += $"\nPenalty: -${data.penaltyAmount:F0} ({data.penaltyReason})";

        note.trigger = NoteTrigger.StartOfDay;
        note.triggerDay = data.forDay + 1;
        return note;
    }

    private OverseerNotes BuildFamilyLetterNote(int day, string from, string body)
    {
        var note = ScriptableObject.CreateInstance<OverseerNotes>();
        note.id = $"family_letter_day_{day}";
        note.title = "Family Letter";
        note.sender = !string.IsNullOrEmpty(from) ? from : "Family";
        note.description = body;
        note.trigger = NoteTrigger.StartOfDay;
        note.triggerDay = day;
        return note;
    }

    private OverseerNotes BuildUnlockNoticeNote(int day, string text)
    {
        var note = ScriptableObject.CreateInstance<OverseerNotes>();
        note.id = $"notice_day_{day}_{text.GetHashCode():X8}";
        note.title = "Bureau Notice";
        note.sender = "The Bureau";
        note.description = text;
        note.trigger = NoteTrigger.StartOfDay;
        note.triggerDay = day;
        return note;
    }

    private System.Collections.IEnumerator DeliverStaggered(List<OverseerNotes> items)
    {
        yield return new WaitForSeconds(initialDeliveryDelay);

        // Determine delivery target: mat hand (stacked pile) or fallback to overseer hand
        var targetHand = matHand != null ? matHand : overseerHand;

        // Calculate pile base position (top-left area of mat)
        Vector3 pileBase = Vector3.zero;
        if (targetHand == matHand && matHand != null)
        {
            RectTransform matRect = matHand.GetComponent<RectTransform>();
            if (matRect != null)
            {
                // Top-left quadrant of the mat in world space
                Vector3[] corners = new Vector3[4];
                matRect.GetWorldCorners(corners);
                // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
                float insetX = (corners[2].x - corners[1].x) * 0.15f;
                float insetY = (corners[1].y - corners[0].y) * 0.15f;
                pileBase = new Vector3(corners[1].x + insetX, corners[1].y - insetY, 0f);
            }
        }

        for (int i = 0; i < items.Count; i++)
        {
            var note = items[i];

            if (targetHand == matHand && matHand != null)
            {
                // Deliver to mat as stacked big cards with staggered offset
                AddNoteToMat(note, pileBase, i);
            }
            else if (CanAddNoteToHand())
            {
                AddNoteToHand(note);
            }
            else
            {
                Debug.LogWarning($"[OverseerManager] Could not deliver: '{note.title}'");
                continue;
            }

            Debug.Log($"[OverseerManager] Delivered: '{note.title}' ({note.id})");
            yield return new WaitForSeconds(deliveryStaggerDelay);
        }
    }

    /// <summary>
    /// Delivers a note to the mat hand at a stacked pile position.
    /// Each subsequent card is offset slightly to create a "pile of mail" look.
    /// </summary>
    private void AddNoteToMat(OverseerNotes note, Vector3 pileBase, int stackIndex)
    {
        if (matHand == null) return;

        // Small offset per card for the pile look (in world units, scaled for UI)
        RectTransform matRect = matHand.GetComponent<RectTransform>();
        float offsetScale = 1f;
        if (matRect != null)
        {
            Canvas canvas = matRect.GetComponentInParent<Canvas>();
            if (canvas != null) offsetScale = canvas.scaleFactor;
        }

        Vector3 offset = new Vector3(15f * offsetScale * stackIndex, -10f * offsetScale * stackIndex, 0f);
        Vector3 worldPos = pileBase + offset;

        matHand.LoadCardsFromData(new List<OverseerNotes> { note }, false);

        // After loading, position the last added card at the pile position
        // and set homeHand to overseerHand so dragging off mat sends it there
        if (matHand.Cards.Count > 0)
        {
            var lastCard = matHand.Cards[matHand.Cards.Count - 1];
            if (matHand.enableFreeFormPlacement)
            {
                matHand.MoveCardToPosition(lastCard, worldPos);
            }
            if (overseerHand != null)
            {
                lastCard.homeHand = overseerHand;
            }
        }
    }
}
