using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random=UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    

    public enum GameState
    {
        Boot,
        DailyStart,
        CaseSelection,
        CaseActive,
        CaseClosed,
        DayComplete,
        GameEnd
    }

    public GameState CurrentState { get; private set; }

    // --- Case State ---
    public Case CurrentCase { get; set; }
    public int currentCaseStep = 0;
    public bool caseSubmitted = false;

    // --- Manager References ---
    private DaysManager daysManager;
    private MatManager matManager;
    private EvidenceManager evidenceManager;
    private CluesManager cluesManager;
    private UIManager uiManager;
    private NotebookManager notebookManager;
    private SuspectManager suspectManager;
    private CardTypeManager cardTypeManager;
    private BookManager bookManager;
    private OverseerManager overseerManager;
    

    [Header("Debug Menu")]
    public GameObject debugMenuPanel;
    public Button debugStartDayButton;
    public Button debugOpenCaseButton;
    public Button debugCloseCaseButton;
    public Button debugEndDayButton;
    public Button debugAddSampleNoteButton;
    public Button debugAddBooksButton;
    public Button debugAddOverseerNoteButton;
    public Button debugResetOverseerNotesButton;
    public Button debugLoadBooksButton;
    public TMP_Text debugInfoText;

    // --- Events ---
    public event Action<Case> OnCaseOpened;
    public event Action<Case> OnCaseClosed;
    public event Action<int> OnDayStarted;
    public event Action OnDayEnded;

    protected override void OnSingletonAwake() { }

    protected override void OnSingletonDestroy()
    {
        GameEvents.OnDayEnded -= SaveGame;
        if (daysManager != null)
        {
            daysManager.OnCampaignComplete -= HandleCampaignComplete;
        }
    }

    private void HandleCampaignComplete()
    {
        SetState(GameState.GameEnd);
    }

    private void Start()
    {
        FindAllManagers();
        SetState(GameState.Boot);

        // Listen to day events to update state & debug info
        if (daysManager != null)
        {
            daysManager.onDayStart.AddListener(() => { StartNewDay(daysManager.currentDay); UpdateDebugInfo(); });
            daysManager.onDayEnd.AddListener(() =>
            {
                EndCurrentDay();
                UpdateDebugInfo();
                FlowController.Instance?.BeginNightSummary();
            });
            daysManager.onFailDay.AddListener(() =>
            {
                EndCurrentDay();
                UpdateDebugInfo();
                FlowController.Instance?.BeginNightSummary();
            });
            daysManager.OnCampaignComplete += HandleCampaignComplete;
        }

        // Subscribe to static event bus
        GameEvents.OnDayEnded += SaveGame;

        // Debug button wiring
        if (debugStartDayButton) debugStartDayButton.onClick.AddListener(() => { daysManager.StartDay(daysManager.currentDay); UpdateDebugInfo(); });
        if (debugOpenCaseButton) debugOpenCaseButton.onClick.AddListener(() => { OpenFirstPendingCase(); UpdateDebugInfo(); });
        if (debugCloseCaseButton) debugCloseCaseButton.onClick.AddListener(() => { DebugCloseCaseFlow(); UpdateDebugInfo(); });
        if (debugEndDayButton) debugEndDayButton.onClick.AddListener(() => { daysManager.EndDay(); UpdateDebugInfo(); });
        if (debugAddSampleNoteButton) debugAddSampleNoteButton.onClick.AddListener(() => { DebugAddSampleNote(); });
        if (debugAddBooksButton) debugAddBooksButton.onClick.AddListener(() => { DebugAddBooks(); });
        if (debugAddOverseerNoteButton) debugAddOverseerNoteButton.onClick.AddListener(() => { DebugAddOverseerNote(); });
        if (debugResetOverseerNotesButton) debugResetOverseerNotesButton.onClick.AddListener(() => { DebugResetOverseerNotes(); });
        if (debugLoadBooksButton) debugLoadBooksButton.onClick.AddListener(() => { DebugLoadBooks(); });
        

        // Case open/close listeners
        OnCaseOpened += (c) => UpdateDebugInfo();
        OnCaseClosed += (c) => UpdateDebugInfo();

        UpdateDebugInfo();
    }

    private void FindAllManagers()
    {
        daysManager = FindFirstObjectByType<DaysManager>();
        evidenceManager = FindFirstObjectByType<EvidenceManager>();
        cluesManager = FindFirstObjectByType<CluesManager>();
        matManager = FindFirstObjectByType<MatManager>();
        uiManager = FindFirstObjectByType<UIManager>();
        notebookManager = FindFirstObjectByType<NotebookManager>();
        suspectManager = FindFirstObjectByType<SuspectManager>();
        cardTypeManager = FindFirstObjectByType<CardTypeManager>();
        bookManager = FindFirstObjectByType<BookManager>();
        overseerManager = FindFirstObjectByType<OverseerManager>();
        if (!daysManager) Debug.LogError("[GameManager] DaysManager not found!");
        if (!evidenceManager) Debug.LogError("[GameManager] EvidenceManager not found!");
        if (!cluesManager) Debug.LogError("[GameManager] CluesManager not found!");
        if (!matManager) Debug.LogError("[GameManager] MatManager not found!");
        if (!uiManager) Debug.LogError("[GameManager] UIManager not found!");
        if (!notebookManager) Debug.LogError("[GameManager] NotebookManager not found!");
        if (!suspectManager) Debug.LogError("[GameManager] SuspectManager not found!");
        if (!cardTypeManager) Debug.LogWarning("[GameManager] CardTypeManager not found - using fallback for case closing");
        if (!bookManager) Debug.LogWarning("[GameManager] BookManager not found!");
        if (!overseerManager) Debug.LogWarning("[GameManager] OverseerManager not found!");
                  
    }

    public void SetState(GameState newState) => CurrentState = newState;

    // ----------- Day & Case Logic -----------

    private void StartNewDay(int dayNumber)
    {
        SetState(GameState.DailyStart);
        OnDayStarted?.Invoke(dayNumber);
        Debug.Log($"[GameManager] Day {dayNumber} started.");
        SetState(GameState.CaseSelection);
    }

    private void OpenFirstPendingCase()
    {
        var pending = daysManager.GetCasesForToday();
        if (pending != null && pending.Count > 0)
        {
            OpenCase(pending[0]);
        }
        else
        {
            Debug.Log("[GameManager] No pending cases to open.");
        }
    }

    public void OpenCase(Case caseData)
    {
        if (CurrentCase != null)
            CloseCurrentCase(); // Auto-close previous

        CurrentCase = caseData;
        currentCaseStep = 0;
        caseSubmitted = false;
        SetState(GameState.CaseActive);

        daysManager.MarkCaseAsOpened(CurrentCase);


        OnCaseOpened?.Invoke(CurrentCase);
        Debug.Log($"[GameManager] Case '{CurrentCase.title}' activated.");
    }

    public void CompleteCaseStep()
    {
        currentCaseStep++;
        Debug.Log($"[GameManager] Case step completed. Step {currentCaseStep}.");
    }

    public void CloseCurrentCase()
    {

        if (CurrentCase == null)
        {
            Debug.LogError("No case is currently open to close.");
            return;
        }

        daysManager.MarkCaseAsClosed(CurrentCase);
        OnCaseClosed?.Invoke(CurrentCase);

        Debug.Log($"[GameManager] Case '{CurrentCase.title}' closed.");
        CurrentCase = null;
        currentCaseStep = 0;

        SyncCaseListUI();
        SetState(GameState.CaseSelection);
        GetNextPendingCase();
    }

    private void GetNextPendingCase()
    {
        // Replenish hand if needed
        daysManager.TryQueueNextCase();
    }

    private void LoadNewPendingCases()
    {
        // Get any new pending cases from the days manager
        if (daysManager != null)
        {
            daysManager.TryQueueNextCase();
        }
        
        // Notify UIManager if it needs to update case list (optional)
        if (uiManager != null && uiManager.caseHand != null)
        {
            // The case hand should automatically load new cases from the days manager
            uiManager.caseHand.LoadCasesOnDayStart();
        }
    }

    public void DebugCloseCaseFlow()
    {
        if (CurrentCase == null)
        {
            Debug.LogError("No case is currently open to close.");
            return;
        }

        StartCoroutine(CloseCaseRoutine());
    }

    public void DebugAddSampleNote()
    {
        if (notebookManager == null)
        {
            Debug.LogError("NotebookManager not found! Cannot add sample note.");
            return;
        }

        // Sample clue text with various tag types
        string sampleClueText = Random.Range(0,10) < 5 
            ? "<person>James</person> was at the <location>gym</location> at <time>3:00pm</time>"
            : "<person>Detective Smith</person> found an evidence at the <location>park</location> which was a <item>knife</item> worth <value>$100</value>";
        
        Debug.Log("[GameManager] Adding sample note to notebook...");
        notebookManager.AddClueNote(sampleClueText);
    }

    public void DebugAddBooks()
    {
        if (bookManager == null)
        {
            Debug.LogError("[GameManager] BookManager not found! Cannot add books.");
            return;
        }

        if (bookManager.bookHand2 == null)
        {
            Debug.LogError("[GameManager] BookManager.bookHand2 is not assigned! Cannot add books.");
            return;
        }

#if UNITY_EDITOR
        // 1. Find all Book assets in the specified folder
        string[] guids = AssetDatabase.FindAssets("t:Book", new[] { "Assets/Scripts/Core/Data/Books" });
        if (guids.Length == 0)
        {
            Debug.LogWarning("[GameManager] No book assets found in Assets/Scripts/Core/Data/Books folder.");
            return;
        }

        List<Book> books = new List<Book>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Book book = AssetDatabase.LoadAssetAtPath<Book>(path);
            if (book != null)
            {
                books.Add(book);
                Debug.Log($"[GameManager] Found book asset: {book.name} at path {path}");
            }
        }
        
        Debug.Log($"[GameManager] Found a total of {books.Count} book assets.");

        // 2. Load the books into BookHand2
        Debug.Log($"[GameManager] Loading books into hand: {bookManager.bookHand2.name}");
        bookManager.bookHand2.LoadCardsFromData(books, true);
#else
        Debug.LogWarning("[GameManager] DebugAddBooks only works in the Unity Editor.");
#endif
    }
    
    public void OnCaseSubmitted(Card submittedCard)
    {
        Debug.Log($"[GameManager] Case submitted: {submittedCard?.name}. Starting comprehensive cleanup...");
        
        caseSubmitted = true;
        
        // Process case score/completion logic here if needed
        // TODO: Add scoring system for submitted case
        
        // Comprehensive cleanup of all case-related cards and visuals
        CleanupSubmittedCase(submittedCard);
    }
    
    /// <summary>
    /// Completely clean up all cards and visuals associated with a submitted case
    /// </summary>
    private void CleanupSubmittedCase(Card submittedCaseCard)
    {
        if (CurrentCase == null)
        {
            Debug.LogWarning("[GameManager] No current case to clean up");
            return;
        }
        
        Debug.Log($"[GameManager] Starting cleanup for case: {CurrentCase.title}");
        
        // 1. Delete all evidence cards from evidenceHand and matHand
        if (evidenceManager != null)
        {
            CleanupEvidenceCards();
        }
        
        // 2. Delete the case card itself from whichever hand it's in
        if (submittedCaseCard != null)
        {
            CleanupCaseCard(submittedCaseCard);
        }
        
        // 3. Clean up any orphaned visual objects in visual handlers
        CleanupOrphanedVisuals();
        
        Debug.Log("[GameManager] Case cleanup completed");
    }
    
    /// <summary>
    /// Delete all evidence cards from evidence hand and mat hand
    /// </summary>
    private void CleanupEvidenceCards()
    {
        Debug.Log("[GameManager] Cleaning up evidence cards...");
        
        var evidenceHand = evidenceManager.evidenceHand;
        var matHand = evidenceManager.matHand;
        
        // Clean up evidence hand
        if (evidenceHand != null && evidenceHand.Cards.Count > 0)
        {
            var evidenceCards = evidenceHand.Cards.ToList(); // Copy to avoid modification during iteration
            Debug.Log($"[GameManager] Deleting {evidenceCards.Count} cards from evidence hand");
            
            foreach (var card in evidenceCards)
            {
                Debug.Log($"[GameManager] Deleting evidence card: {card.name}");
                evidenceHand.DeleteCard(card);
            }
        }
        
        // Clean up mat hand (evidence and other card types that were on the mat)
        if (matHand != null && matHand.Cards.Count > 0)
        {
            var matCards = matHand.Cards.ToList(); // Copy to avoid modification during iteration
            Debug.Log($"[GameManager] Deleting {matCards.Count} cards from mat hand");
            
            foreach (var card in matCards)
            {
                Debug.Log($"[GameManager] Deleting mat card: {card.name} (type: {card.mode})");
                matHand.DeleteCard(card);
            }
        }
    }
    
    /// <summary>
    /// Delete the submitted case card from whichever hand it's in
    /// </summary>
    private void CleanupCaseCard(Card caseCard)
    {
        Debug.Log($"[GameManager] Cleaning up case card: {caseCard.name}");
        
        if (caseCard.parentHolder != null)
        {
            Debug.Log($"[GameManager] Deleting case card from holder: {caseCard.parentHolder.name}");
            caseCard.parentHolder.DeleteCard(caseCard);
        }
        else
        {
            Debug.LogWarning("[GameManager] Case card has no parent holder, attempting to find and clean up manually");
            
            // Try to find the case in known hands
            if (uiManager != null)
            {
                // Check case hand
                if (uiManager.caseHand != null && uiManager.caseHand.Cards.Contains(caseCard))
                {
                    Debug.Log("[GameManager] Found case card in case hand, deleting...");
                    uiManager.caseHand.DeleteCard(caseCard);
                }
                // Check case slot (mat manager's case slot)
                else if (matManager != null && matManager.caseSlot != null && matManager.caseSlot.Cards.Contains(caseCard))
                {
                    Debug.Log("[GameManager] Found case card in case slot, deleting...");
                    matManager.caseSlot.DeleteCard(caseCard);
                }
                else
                {
                    Debug.LogWarning("[GameManager] Could not find case card in any known hands");
                    // Last resort - destroy the game object directly
                    if (caseCard != null && caseCard.gameObject != null)
                    {
                        Debug.Log("[GameManager] Destroying case card GameObject directly as last resort");
                        if (caseCard.transform.parent != null)
                        {
                            Destroy(caseCard.transform.parent.gameObject);
                        }
                        else
                        {
                            Destroy(caseCard.gameObject);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Clean up any orphaned BigCardVisual objects that don't have BigCardVisual scripts
    /// </summary>
    private void CleanupOrphanedVisuals()
    {
        Debug.Log("[GameManager] Cleaning up orphaned visual objects...");
        
        // Clean up visual handlers that might have orphaned objects
        CleanupVisualHandler("CaseHand BigCardVisual Handler", uiManager?.caseHand?.bigVisualHandler);
        CleanupVisualHandler("EvidenceHand BigCardVisual Handler", evidenceManager?.evidenceHand?.bigVisualHandler);
        CleanupVisualHandler("MatHand BigCardVisual Handler", evidenceManager?.matHand?.bigVisualHandler);
        
        // Also clean up any other known visual handlers from card type manager
        if (cardTypeManager != null)
        {
            CleanupCardTypeManagerVisuals();
        }
    }
    
    /// <summary>
    /// Clean up orphaned visuals in a specific visual handler
    /// </summary>
    private void CleanupVisualHandler(string handlerName, Transform visualHandler)
    {
        if (visualHandler == null) return;
        
        Debug.Log($"[GameManager] Checking {handlerName} for orphaned visuals...");
        
        List<Transform> orphanedObjects = new List<Transform>();
        
        // Find children that look like BigCardVisual clones but don't have the script
        for (int i = 0; i < visualHandler.childCount; i++)
        {
            Transform child = visualHandler.GetChild(i);
            
            // Check if it's a BigCardVisual clone without the script
            if (child.name.Contains("BigCardVisual") && child.name.Contains("Clone"))
            {
                BigCardVisual bigCardScript = child.GetComponent<BigCardVisual>();
                if (bigCardScript == null)
                {
                    Debug.Log($"[GameManager] Found orphaned visual object: {child.name} in {handlerName}");
                    orphanedObjects.Add(child);
                }
            }
        }
        
        // Destroy orphaned objects
        foreach (Transform orphan in orphanedObjects)
        {
            Debug.Log($"[GameManager] Destroying orphaned visual: {orphan.name}");
            Destroy(orphan.gameObject);
        }
        
        if (orphanedObjects.Count > 0)
        {
            Debug.Log($"[GameManager] Cleaned up {orphanedObjects.Count} orphaned visuals from {handlerName}");
        }
    }
    
    /// <summary>
    /// Clean up visuals in CardTypeManager's managed hands
    /// </summary>
    private void CleanupCardTypeManagerVisuals()
    {
        Debug.Log("[GameManager] Cleaning up CardTypeManager visual handlers...");
        
        // CardTypeManager now has a CleanupAllVisualHandlers method
        cardTypeManager.CleanupAllVisualHandlers();
    }

    public IEnumerator CloseCaseRoutine()
    {
        Debug.Log("[GameManager] Starting case closing routine...");

        ForceEjectComputerDisc();

        yield return StartCoroutine(ReturnAllCardsToHands());

        yield return StartCoroutine(PlayCaseCloseAnimation());

        TransitionToSubmitPhase();

        yield return StartCoroutine(WaitForCaseSubmission());

        TransitionBackToCaseSelection();
    }

    private void ForceEjectComputerDisc()
    {
        ComputerSystem computerSystem = FindFirstObjectByType<ComputerSystem>();
        if (computerSystem != null)
        {
            computerSystem.ForceEjectDiscForCaseClosing();
        }
    }

    private IEnumerator ReturnAllCardsToHands()
    {
        if (evidenceManager?.matHand == null)
        {
            Debug.LogError("[GameManager] EvidenceManager or matHand is null! Cannot close case.");
            yield break;
        }

        var matHand = evidenceManager.matHand;
        var cardsToMove = matHand.Cards.ToList();
        Debug.Log($"[GameManager] Found {cardsToMove.Count} cards on mat to move back");

        if (cardTypeManager != null)
        {
            yield return StartCoroutine(cardTypeManager.ReturnAllCardsFromMatToHands());
        }
        else
        {
            var evidenceHand = evidenceManager.evidenceHand;
            if (evidenceHand == null)
            {
                Debug.LogError("[GameManager] EvidenceHand is null! Cannot return cards.");
                yield break;
            }

            foreach (var card in cardsToMove)
            {
                matHand.RemoveCard(card);
                evidenceHand.AddCardToHand(card, -1);
                yield return new WaitForSeconds(0.1f);
            }
        }

        Debug.Log($"[GameManager] Finished moving {cardsToMove.Count} cards from mat");
    }

    private IEnumerator PlayCaseCloseAnimation()
    {
        Debug.Log("[GameManager] Playing case close animation...");
        yield return new WaitForSeconds(0.3f);
    }

    private void TransitionToSubmitPhase()
    {
        uiManager.AnimateEvidenceHandOut();
        uiManager.AnimateSubmitZoneIn();
        matManager.EnableCaseSubmission();
    }

    private IEnumerator WaitForCaseSubmission()
    {
        if (!caseSubmitted)
        {
            Debug.Log("[GameManager] Waiting for case submission...");
            while (!caseSubmitted)
            {
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void TransitionBackToCaseSelection()
    {
        uiManager.AnimateSubmitZoneOut();
        uiManager.AnimateCaseHandIn();

        if (CurrentCase != null)
        {
            CloseCurrentCase();
        }
        else
        {
            SetState(GameState.CaseSelection);
        }

        LoadNewPendingCases();
    }

    private void EndCurrentDay()
    {
        SetState(GameState.DayComplete);
        OnDayEnded?.Invoke();
        Debug.Log("[GameManager] Day ended.");
    }

    private void SyncCaseListUI()
    {
        var remainingCases = daysManager.GetCasesForToday();
        // uiManager.UpdateCaseList(remainingCases); // Uncomment if you want UI sync
    }

    public void SaveGame()
    {
        Debug.Log("Saving game...");
        // TODO: implement save system
    }

    public void LoadGame()
    {
        Debug.Log("Loading game...");
        // TODO: implement load system
    }


    private void UpdateDebugInfo()
    {
        if (debugInfoText == null) return;

        string timeOfDay = daysManager != null ? daysManager.GetTimeString() : "";

        // Evidence on mat (from EvidenceManager's evidenceHolder)
        int evidencesOnMat = 0;
        if (evidenceManager != null && evidenceManager.evidenceHand != null)
            evidencesOnMat = evidenceManager.matHand.Cards.Count;

        // Clues found/total (from EvidenceManager & CluesManager)
        int cluesTotal = 0, cluesFound = 0;
        if (CurrentCase != null && evidenceManager != null && cluesManager != null)
        {
            var allClues = evidenceManager.GetAllCluesForCurrentCase();
            cluesTotal = allClues.Count();
            cluesFound = allClues.Count(c => cluesManager.IsClueFound(c.clueID));
        }
        if (!debugInfoText)
          return;

        debugInfoText.text =
            $"State: {CurrentState}\n" +
            $"Day: {daysManager?.currentDay}\n" +
            $"Pending Cases: {daysManager?.GetCasesForToday()?.Count}\n" +
            $"Closed Cases: {daysManager?.GetClosedCasesForToday()?.Count}\n" +
            $"Current Case: {CurrentCase?.title ?? "None"}\n" +
            $"Time of Day: {timeOfDay}\n" +
            $"Evidences on Mat: {evidencesOnMat}\n" +
            $"Clues found: {cluesFound}/{cluesTotal}";
    }

    public void DebugAddOverseerNote()
    {
        if (overseerManager == null)
        {
            Debug.LogError("[GameManager] OverseerManager not found! Cannot add overseer note.");
            return;
        }

        // Manually trigger the first available note for testing
        if (overseerManager.allNotes.Count > 0)
        {
            var firstNote = overseerManager.allNotes[0];
            overseerManager.ManuallyTriggerNote(firstNote.id);
            Debug.Log($"[GameManager] Manually triggered overseer note: {firstNote.title}");
        }
        else
        {
            Debug.LogWarning("[GameManager] No overseer notes available to trigger.");
        }
    }

    public void DebugResetOverseerNotes()
    {
        if (overseerManager == null)
        {
            Debug.LogError("[GameManager] OverseerManager not found! Cannot reset overseer notes.");
            return;
        }

        overseerManager.ResetAllNotes();
        Debug.Log("[GameManager] Reset all overseer notes.");
    }

    public void DebugLoadBooks()
    {
        if (bookManager == null)
        {
            Debug.LogError("[GameManager] BookManager not found!");
            return;
        }

        bookManager.ManualLoadBooksForCurrentDay();
    }
}
