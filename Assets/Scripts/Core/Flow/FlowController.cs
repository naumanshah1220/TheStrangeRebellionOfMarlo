using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Master flow orchestrator. Sits ABOVE existing managers.
/// Owns high-level state transitions: MainMenu → Slideshow → DayBriefing → Workday → NightSummary → BillsDesk → repeat.
/// GameManager's internal states (CaseSelection, CaseActive, etc.) remain as sub-states of Workday.
/// </summary>
public class FlowController : SingletonMonoBehaviour<FlowController>
{
    [Header("Panel References")]
    [SerializeField] private SlideshowPanel slideshowPanel;
    [SerializeField] private DayBriefingPanel dayBriefingPanel;
    [SerializeField] private NightSummaryPanel nightSummaryPanel;
    [SerializeField] private BillsDeskPanel billsDeskPanel;
    [SerializeField] private TopBarController topBar;

    [Header("Data")]
    [SerializeField] private SlideshowData slideshowData;

    public FlowState CurrentFlowState { get; private set; } = FlowState.None;
    public int CurrentDay { get; private set; } = 1;

    private DaysManager daysManager;
    private NewspaperManager newspaperManager;
    private PlayerProgressManager playerProgress;

    private NightSummaryData currentNightData;
    private DaysBriefingDataJson daysJsonData;

    [Header("Debug")]
    [Tooltip("When true, skip flow panels and let debug buttons drive the game (old behavior).")]
    [SerializeField] private bool debugMode;

    private void Start()
    {
        daysManager = DaysManager.Instance;
        newspaperManager = NewspaperManager.Instance;
        playerProgress = PlayerProgressManager.Instance;

        // If we came from MainMenu, FlowBootstrap was initialized.
        // If playing Detective 6.0 directly in Editor without going through MainMenu,
        // fall back to debug mode so existing debug buttons still work.
        if (!FlowBootstrap.WasInitialized && !debugMode)
        {
            debugMode = true;
            Debug.Log("[FlowController] No FlowBootstrap detected — entering debug mode (use MainMenu scene or toggle off debugMode).");
        }

        if (debugMode)
        {
            // Don't auto-start flow — let debug buttons drive everything as before
            SetFlowState(FlowState.Workday);
            return;
        }

        // Load JSON content for lore and days
        LoadJsonContent();

        CurrentDay = FlowBootstrap.ContinueFromDay;

        if (FlowBootstrap.ShouldShowSlideshow)
        {
            BeginSlideshow();
        }
        else
        {
            BeginDayBriefing();
        }
    }

    private void LoadJsonContent()
    {
        var content = ContentLoader.LoadAllContent();
        if (content == null) return;

        // Build SlideshowData from JSON if available (fallback to inspector-assigned)
        if (content.loreData != null && content.loreData.slides != null && content.loreData.slides.Count > 0)
        {
            var jsonSlideshow = SlideshowData.CreateFromJson(content.loreData);
            if (jsonSlideshow != null)
                slideshowData = jsonSlideshow;
        }

        // Store days data for building DayBriefingData each day
        daysJsonData = content.daysData;

        // Feed newspaper headlines from JSON
        if (newspaperManager != null && content.daysData != null)
            newspaperManager.LoadFromJson(content.daysData);
    }

    private void SetFlowState(FlowState newState)
    {
        var old = CurrentFlowState;
        CurrentFlowState = newState;
        GameEvents.RaiseFlowStateChanged(old, newState);
        Debug.Log($"[FlowController] {old} → {newState}");
    }

    // --- State Transitions ---

    public void BeginSlideshow()
    {
        SetFlowState(FlowState.LoreSlideshow);

        if (slideshowPanel != null && slideshowData != null && slideshowData.slides.Count > 0)
        {
            slideshowPanel.Show(slideshowData, () => BeginDayBriefing());
        }
        else
        {
            // No slideshow data — skip straight to briefing
            BeginDayBriefing();
        }
    }

    public void BeginDayBriefing()
    {
        SetFlowState(FlowState.DayBriefing);

        var briefingData = BuildDayBriefingData();

        if (dayBriefingPanel != null)
        {
            dayBriefingPanel.Show(briefingData, () => BeginWorkday());
        }
        else
        {
            // No panel — go straight to workday
            BeginWorkday();
        }
    }

    private DayBriefingData BuildDayBriefingData()
    {
        var data = new DayBriefingData
        {
            dayNumber = CurrentDay,
            headline = "",
            subheadline = "",
            letterFrom = "",
            letterBody = "",
            unlockNotices = new List<string>()
        };

        // Get headline from NewspaperManager (handles case outcome overrides)
        if (newspaperManager != null && daysManager != null && CurrentDay > 1)
        {
            data.headline = newspaperManager.GetHeadlineForDay(CurrentDay, daysManager.todaysClosedCases);
        }

        // Fill from JSON days data if available
        if (daysJsonData != null && daysJsonData.days != null)
        {
            var dayJson = daysJsonData.days.Find(d => d.day == CurrentDay);
            if (dayJson != null)
            {
                // Subheadline from JSON
                if (!string.IsNullOrEmpty(dayJson.subheadline))
                    data.subheadline = dayJson.subheadline;

                // If headline wasn't set by NewspaperManager (day 1 or no override), use JSON default
                if (string.IsNullOrEmpty(data.headline) && !string.IsNullOrEmpty(dayJson.headline))
                    data.headline = dayJson.headline;

                // Family letter
                if (dayJson.familyLetter != null)
                {
                    data.letterFrom = dayJson.familyLetter.from ?? "";
                    data.letterBody = dayJson.familyLetter.body ?? "";
                }

                // Unlock notices
                if (dayJson.unlockNotices != null)
                    data.unlockNotices = new List<string>(dayJson.unlockNotices);
            }
        }

        return data;
    }

    public void BeginWorkday()
    {
        SetFlowState(FlowState.Workday);

        if (topBar != null) topBar.Show(CurrentDay);

        // Tell DaysManager to start the day
        if (daysManager != null)
        {
            daysManager.StartDay(CurrentDay);
        }
        else
        {
            Debug.LogError("[FlowController] DaysManager not found — cannot start workday!");
        }
    }

    public void BeginNightSummary()
    {
        if (debugMode) return; // Debug mode — don't show night summary overlay

        SetFlowState(FlowState.NightSummary);

        if (topBar != null) topBar.Hide();

        currentNightData = GatherNightSummaryData();

        if (nightSummaryPanel != null)
        {
            nightSummaryPanel.Show(currentNightData, () => BeginBillsDesk());
        }
        else
        {
            // No panel — skip to bills desk
            BeginBillsDesk();
        }
    }

    public void BeginBillsDesk()
    {
        SetFlowState(FlowState.BillsDesk);

        if (currentNightData == null)
            currentNightData = GatherNightSummaryData();

        var bills = GatherBills(currentNightData);

        if (billsDeskPanel != null)
        {
            billsDeskPanel.Show(bills, currentNightData, () => AdvanceToNextDay());
        }
        else
        {
            // No panel — process day end with full earnings (no expenses deducted) and advance
            if (playerProgress != null)
                playerProgress.ProcessDayEnd(currentNightData.totalEarnings);
            AdvanceToNextDay();
        }
    }

    public void AdvanceToNextDay()
    {
        currentNightData = null;
        CurrentDay++;

        if (daysManager != null && CurrentDay > daysManager.totalDays)
        {
            EndGame();
            return;
        }

        if (playerProgress != null && playerProgress.IsGameOver())
        {
            EndGame();
            return;
        }

        BeginDayBriefing();
    }

    public void EndGame()
    {
        SetFlowState(FlowState.GameEnd);
        if (topBar != null) topBar.Hide();
        Debug.Log("[FlowController] Game ended.");
        // TODO: show ending screen
    }

    // --- Data Gathering ---

    private NightSummaryData GatherNightSummaryData()
    {
        var data = new NightSummaryData();
        data.dayNumber = CurrentDay;

        if (daysManager != null)
        {
            data.casesSolved = daysManager.todaysClosedCases.Count;
            data.totalEarnings = daysManager.TodaysEarnings;

            // Check for unsolved core cases (penalty)
            data.hadUnsolvedCoreCases = false;
            foreach (var c in daysManager.todaysPendingCases)
            {
                if (c.caseType == CaseType.Core)
                {
                    data.hadUnsolvedCoreCases = true;
                    break;
                }
            }

            // Build per-case earnings
            foreach (var closedCase in daysManager.todaysClosedCases)
            {
                data.caseEarnings.Add(new CaseEarning
                {
                    caseTitle = closedCase.title,
                    reward = closedCase.reward
                });
            }
        }

        if (playerProgress != null)
        {
            data.baseSalary = playerProgress.baseIncome;
            data.currentSavings = playerProgress.familyStatus.savings;
            data.rent = playerProgress.baseDailyExpense;
            data.foodCost = 15f; // Tunable — could come from data
            data.currentHunger = playerProgress.familyStatus.hunger;
        }
        else
        {
            data.baseSalary = 50f;
            data.rent = 30f;
            data.foodCost = 15f;
            data.currentHunger = 100f;
        }

        return data;
    }

    private List<BillInfo> GatherBills(NightSummaryData data)
    {
        var bills = new List<BillInfo>();

        if (data.rent > 0f)
        {
            bills.Add(new BillInfo
            {
                title = "Monthly Rent",
                description = "Your apartment in Block 14-C",
                amount = data.rent,
                category = "rent",
                skipWarning = "Eviction notice will be issued"
            });
        }

        if (data.foodCost > 0f)
        {
            bills.Add(new BillInfo
            {
                title = "Family Food",
                description = "Groceries for the family",
                amount = data.foodCost,
                category = "food",
                skipWarning = "Your family will go hungry tonight"
            });
        }

        return bills;
    }
}
