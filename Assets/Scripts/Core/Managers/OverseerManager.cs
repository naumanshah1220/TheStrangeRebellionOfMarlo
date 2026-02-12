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
    
    [Header("Note Collection")]
    public List<OverseerNotes> allNotes = new List<OverseerNotes>();
    
    [Header("Settings")]
    public int maxNotesInHand = 5; // Maximum notes that can be in hand at once

    protected override void OnSingletonAwake() { }
    
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
}
