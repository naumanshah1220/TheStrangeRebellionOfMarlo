using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class DayCaseSettings
{
    public int day;
    public int maxCasesForDay = 5;     // Total cases that can be assigned this day
    public int maxCoreCases = 1;       // Maximum core cases allowed this day
    public int minSecondaryCases = 1;   // Minimum secondary cases to maintain
}

public class DaysManager : SingletonMonoBehaviour<DaysManager>
{
    
    [Header("Day Settings")]
    [Range(1, 20)] public int currentDay = 1;
    public int totalDays = 20;
    public List<DayCaseSettings> dayCaseSettings = new List<DayCaseSettings>();

    [Header("Default Settings")]
    public int defaultMaxCasesPerDay = 5;
    public int defaultMaxCoreCases = 1;
    public int defaultMinSecondaryCases = 1;

    [Header("Time Settings")]
    public float realSecondsPerGameHour = 60f;
    public float startHour = 6f;
    public float endHour = 18f;
    private float currentGameHour;
    private float timeElapsed = 0f;
    private bool isDayActive = false;

    [Header("UI References")]
    public GameObject newspaperPanel;
    public TMP_Text headlineText;
    public float headlineDisplayTime = 5f;

    [Header("Events")]
    public UnityEvent onDayStart;
    public UnityEvent onDayEnd;
    public UnityEvent onFailDay;

    private CaseManager caseManager;
    private UIManager uiManager;
    private NewspaperManager newspaperManager;
    private CaseProgressionManager progressionManager;
    public float TodaysEarnings { get; private set; }

    public List<Case> todaysTotalCases = new List<Case>();    // All cases shown in slots
    public List<Case> todaysPendingCases = new List<Case>();  // Currently active cases (in hand)
    public List<Case> todaysClosedCases = new List<Case>();   // Finished cases
    public Queue<Case> unassignedCases = new Queue<Case>();   // Queued up for later

    private List<Case> unsolvedCasesFromPreviousDay = new List<Case>();

    private void Start()
    {
        caseManager = FindFirstObjectByType<CaseManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        newspaperManager = FindFirstObjectByType<NewspaperManager>();
        progressionManager = FindFirstObjectByType<CaseProgressionManager>();

        if (!caseManager || !newspaperManager || !progressionManager)
            Debug.LogError("Required managers not found!");

        LoadDayScheduleFromJson();
    }

    /// <summary>
    /// Merges day schedule from JSON manifest into dayCaseSettings.
    /// For days that exist in both Inspector and JSON, JSON values override.
    /// For days only in Inspector, they're kept as-is.
    /// </summary>
    private void LoadDayScheduleFromJson()
    {
        var result = ContentLoader.LoadAllContent();
        if (result?.manifest?.daySchedule == null || result.manifest.daySchedule.Count == 0)
            return;

        if (dayCaseSettings == null)
            dayCaseSettings = new List<DayCaseSettings>();

        int merged = 0;
        foreach (var entry in result.manifest.daySchedule)
        {
            var existing = dayCaseSettings.Find(s => s.day == entry.day);
            if (existing != null)
            {
                // JSON overrides Inspector for this day
                existing.maxCasesForDay = entry.maxCasesForDay;
                existing.maxCoreCases = entry.maxCoreCases;
                existing.minSecondaryCases = entry.minSecondaryCases;
            }
            else
            {
                dayCaseSettings.Add(new DayCaseSettings
                {
                    day = entry.day,
                    maxCasesForDay = entry.maxCasesForDay,
                    maxCoreCases = entry.maxCoreCases,
                    minSecondaryCases = entry.minSecondaryCases
                });
            }
            merged++;
        }
        Debug.Log($"[DaysManager] Merged {merged} day schedule(s) from JSON manifest.");
    }

    private void Update()
    {
        if (isDayActive) UpdateTime();
    }

    public event System.Action OnCampaignComplete;

    public void StartDay(int dayNumber)
    {
        if (dayNumber > totalDays)
        {
            Debug.Log("Campaign Complete");
            OnCampaignComplete?.Invoke();
            return;
        }

        currentDay = dayNumber;
        TodaysEarnings = 0f;
        StartCoroutine(StartDayRoutine());
    }

    private IEnumerator StartDayRoutine()
    {
        // Newspaper display is now handled by DayBriefingPanel via FlowController.
        yield return null;

        // Reset day state but keep track of unsolved cases
        unsolvedCasesFromPreviousDay = new List<Case>(todaysPendingCases);
        todaysTotalCases.Clear();
        todaysPendingCases.Clear();
        todaysClosedCases.Clear();
        unassignedCases.Clear();
        timeElapsed = 0f;
        currentGameHour = startHour;
        isDayActive = true;

        // Get settings for the day
        var settings = GetSettingsForDay(currentDay);
        int maxSimultaneous = progressionManager.GetCurrentMaxSimultaneousCases();

        // Get available cases from CaseManager, excluding carried over cases
        var availableCases = caseManager.GetAvailableCasesForDay(currentDay, settings.maxCasesForDay, settings.minSecondaryCases);
        
        // Split available cases into core and secondary
        var coreCases = availableCases.FindAll(c => c.caseType == CaseType.Core);
        var secondaryCases = availableCases.FindAll(c => c.caseType == CaseType.Secondary);

        // Split unsolved cases into core and secondary
        var unsolvedCoreCases = unsolvedCasesFromPreviousDay.FindAll(c => c.caseType == CaseType.Core);
        var unsolvedSecondaryCases = unsolvedCasesFromPreviousDay.FindAll(c => c.caseType == CaseType.Secondary);

        // First, add any unsolved core cases
        foreach (var unsolvedCore in unsolvedCoreCases)
        {
            coreCases.RemoveAll(c => c.caseID == unsolvedCore.caseID);
            todaysTotalCases.Add(unsolvedCore);
        }

        // Then add new core cases up to the maximum
        int remainingCoreSlots = settings.maxCoreCases - todaysTotalCases.Count;
        if (remainingCoreSlots > 0)
        {
            var newCoreCases = coreCases.GetRange(0, Mathf.Min(coreCases.Count, remainingCoreSlots));
            todaysTotalCases.AddRange(newCoreCases);
        }

        // Now add unsolved secondary cases
        foreach (var unsolvedSecondary in unsolvedSecondaryCases)
        {
            secondaryCases.RemoveAll(c => c.caseID == unsolvedSecondary.caseID);
            if (todaysTotalCases.Count < settings.maxCasesForDay)
            {
                todaysTotalCases.Add(unsolvedSecondary);
            }
        }

        // Calculate how many secondary cases we still need
        int currentSecondaryCount = todaysTotalCases.Count(c => c.caseType == CaseType.Secondary);
        int minNeededSecondary = Mathf.Max(0, settings.minSecondaryCases - currentSecondaryCount);
        int remainingTotalSlots = settings.maxCasesForDay - todaysTotalCases.Count;
        int secondaryCasesToAdd = Mathf.Min(remainingTotalSlots, Mathf.Max(minNeededSecondary, remainingTotalSlots));

        // Add new secondary cases if needed
        if (secondaryCasesToAdd > 0 && secondaryCases.Count > 0)
        {
            var newSecondaryCases = secondaryCases.GetRange(0, Mathf.Min(secondaryCases.Count, secondaryCasesToAdd));
            todaysTotalCases.AddRange(newSecondaryCases);
        }

        // Assign cases to pending or queue based on maxSimultaneous
        // First, prioritize core cases
        var allCoreCases = todaysTotalCases.FindAll(c => c.caseType == CaseType.Core);
        var allSecondaryCases = todaysTotalCases.FindAll(c => c.caseType == CaseType.Secondary);

        // Add core cases first
        foreach (var coreCase in allCoreCases)
        {
            if (todaysPendingCases.Count < maxSimultaneous)
            {
                todaysPendingCases.Add(coreCase);
                caseManager.MarkCaseActive(coreCase.caseID);
            }
            else
            {
                unassignedCases.Enqueue(coreCase);
            }
        }

        // Then add secondary cases
        foreach (var secondaryCase in allSecondaryCases)
        {
            if (todaysPendingCases.Count < maxSimultaneous)
            {
                todaysPendingCases.Add(secondaryCase);
                caseManager.MarkCaseActive(secondaryCase.caseID);
            }
            else
            {
                unassignedCases.Enqueue(secondaryCase);
            }
        }

        onDayStart?.Invoke();

        var carriedOverCount = unsolvedCasesFromPreviousDay.Count;
        var newCoreCount = todaysTotalCases.Count(c => c.caseType == CaseType.Core && !unsolvedCasesFromPreviousDay.Contains(c));
        var newSecondaryCount = todaysTotalCases.Count(c => c.caseType == CaseType.Secondary && !unsolvedCasesFromPreviousDay.Contains(c));
        
        Debug.Log($"Day {currentDay} started with {todaysTotalCases.Count} total cases:\n" +
                  $"- {carriedOverCount} carried over from previous day\n" +
                  $"- {newCoreCount} new core cases\n" +
                  $"- {newSecondaryCount} new secondary cases\n" +
                  $"- {todaysPendingCases.Count} cases in hand");
    }

    private DayCaseSettings GetSettingsForDay(int day)
    {
        var settings = dayCaseSettings.Find(s => s.day == day);
        if (settings == null)
        {
            settings = new DayCaseSettings
            {
                day = day,
                maxCasesForDay = defaultMaxCasesPerDay,
                maxCoreCases = defaultMaxCoreCases,
                minSecondaryCases = defaultMinSecondaryCases
            };
        }
        return settings;
    }

    private void UpdateTime()
    {
        timeElapsed += Time.deltaTime;
        currentGameHour = startHour + (timeElapsed / realSecondsPerGameHour);

        // End day if all cases are solved or time is up
        if (currentGameHour >= endHour || 
            (todaysPendingCases.Count == 0 && unassignedCases.Count == 0))
        {
            EndDay();
        }
    }

    public string GetTimeString()
    {
        int hours = Mathf.FloorToInt(currentGameHour);
        int minutes = Mathf.FloorToInt((currentGameHour - hours) * 60);
        return $"{hours:D2}:{minutes:D2}";
    }
    
    public float GetCurrentGameHour()
    {
        return currentGameHour;
    }
    
    public bool IsDayActive()
    {
        return isDayActive;
    }

    public void MarkCaseAsOpened(Case openedCase)
    {
        todaysPendingCases.Remove(openedCase);
    }

    public void MarkCaseAsClosed(Case closedCase)
    {
        if (!todaysClosedCases.Contains(closedCase))
        {
            todaysClosedCases.Add(closedCase);
            TodaysEarnings += closedCase.reward;
            caseManager.MarkCaseSolved(closedCase.caseID);

            // Update progression
            progressionManager.ProcessCaseCompletion(closedCase.caseID);

            // Try to assign a new case immediately if we have room
            TryQueueNextCase();
        }
    }

    public void TryQueueNextCase()
    {
        if (unassignedCases.Count > 0)
        {
            int maxSimultaneous = progressionManager.GetCurrentMaxSimultaneousCases();
            if (todaysPendingCases.Count < maxSimultaneous)
            {
                var nextCase = unassignedCases.Dequeue();
                todaysPendingCases.Add(nextCase);
                caseManager.MarkCaseActive(nextCase.caseID);
                uiManager?.NotifyNewCaseAssigned(nextCase);
            }
        }
    }

    public void EndDay()
    {
        isDayActive = false;

        // Check if any core cases are still pending
        bool hasUnsolvedCoreCases = false;
        foreach (var c in todaysPendingCases)
        {
            if (c.caseType == CaseType.Core)
            {
                hasUnsolvedCoreCases = true;
                break;
            }
        }

        if (hasUnsolvedCoreCases)
        {
            onFailDay?.Invoke();
            TodaysEarnings = 0; // No pay if core cases are unsolved
        }
        else
        {
            onDayEnd?.Invoke();
        }

        // Day counter is now owned by FlowController.AdvanceToNextDay()
        GameEvents.RaiseDayEnded();
    }

    public List<Case> GetCasesForToday() => new List<Case>(todaysPendingCases);
    public List<Case> GetClosedCasesForToday() => new List<Case>(todaysClosedCases);
    public int GetCurrentDay() => currentDay;
}
