using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

[System.Serializable]
public class CriminalViolation
{
    public string violationName;
    public string description;
    public CrimeSeverity severity;

    public CriminalViolation(string name, string desc, CrimeSeverity sev)
    {
        violationName = name;
        description = desc;
        severity = sev;
    }
}

public class SuspectManager : SingletonMonoBehaviour<SuspectManager>
{
    [Header("References")]
    public CitizenDatabase citizenDatabase;

    // Current case will be set by GameManager
    private Case currentCase;

    [Header("Suspect View")]
    public InterrogationScreen interrogationScreen; // Unified monitor + display + interaction panel

    [Header("Animation Sprite Sheets")]
    [Tooltip("Male silhouette sprite sheets - each should contain all animation frames")]
    public SuspectAnimationSet maleAnimations;
    [Tooltip("Female silhouette sprite sheets - each should contain all animation frames")]
    public SuspectAnimationSet femaleAnimations;

    [Header("Animation Settings")]
    public float animationFrameRate = 8f; // Frames per second
    public bool debugAnimationInfo = false;

    [Header("Arrest System")]
    public List<CriminalViolation> availableViolations = new List<CriminalViolation>(); // List of charges

    // Current state
    private Citizen currentMonitorSuspect = null; // The one suspect on the monitor
    private bool isInterrogationMode = false;

    // Arrest state
    private Citizen suspectBeingArrested = null;

    // UI Components
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    protected override void OnSingletonAwake()
    {
        // Initialize available violations
        InitializeViolations();
    }

    private void Start()
    {
        // Setup UI components
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Subscribe to GameManager events once it's available
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened += OnCaseOpened;
            GameManager.Instance.OnCaseClosed += OnCaseClosed;
        }
        else
        {
            Debug.LogError("[SuspectManager] GameManager.Instance is null! Cannot connect to case events.");
        }
    }

    protected override void OnSingletonDestroy()
    {
        // Disconnect from GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened -= OnCaseOpened;
            GameManager.Instance.OnCaseClosed -= OnCaseClosed;
        }
    }

    private void OnCaseOpened(Case openedCase)
    {
        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Case opened: {openedCase?.title}");

        LoadSuspectsFromCase(openedCase);
    }

    private void OnCaseClosed(Case closedCase)
    {
        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Case closed: {closedCase?.title}");

        ClearMonitor();
        currentCase = null;
    }

    /// <summary>
    /// Store case reference and mark monitor as ready for suspect drops.
    /// Suspects are no longer auto-loaded — player must drag completed tags to the monitor.
    /// </summary>
    public void LoadSuspectsFromCase(Case caseData)
    {
        currentCase = caseData;

        if (caseData == null)
        {
            ClearMonitor();
            return;
        }

        // Mark the view as ready (shows "drag here" text)
        if (interrogationScreen != null)
        {
            interrogationScreen.SetCaseOpen(true);
        }
    }

    /// <summary>
    /// Get the appropriate animation set based on suspect gender
    /// </summary>
    private SuspectAnimationSet GetAnimationSetForSuspect(Citizen suspect)
    {
        switch (suspect.gender)
        {
            case Gender.Male:
                return maleAnimations;
            case Gender.Female:
                return femaleAnimations;
            default:
                return maleAnimations; // Default fallback
        }
    }

    /// <summary>
    /// Call a suspect to the single monitor. Shows animation, detailed view,
    /// and notifies InterrogationManager of new suspect context.
    /// </summary>
    public void CallSuspectToMonitor(Citizen citizen)
    {
        if (citizen == null)
        {
            Debug.LogError("[SuspectManager] Cannot call null citizen to monitor");
            return;
        }

        // If there's already a suspect on the monitor, release them first
        if (currentMonitorSuspect != null)
        {
            Debug.LogWarning($"[SuspectManager] Monitor already occupied by {currentMonitorSuspect.FullName}. Release first.");
            return;
        }

        currentMonitorSuspect = citizen;

        // Notify InterrogationManager about the new suspect context
        if (InterrogationManager.Instance != null)
        {
            InterrogationManager.Instance.SwitchToSuspect(citizen.citizenID);

            // Hide drop zone — only visible during active interrogation
            if (InterrogationManager.Instance.chatManager != null)
            {
                InterrogationManager.Instance.chatManager.HideDropZoneImmediate();
            }
        }

        // Show detailed view for this suspect
        ShowInterrogationScreen(citizen);

        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Called {citizen.FullName} to monitor");
    }

    /// <summary>
    /// Release the current suspect from the monitor. Clears monitor, ends interrogation if active,
    /// fades out InterrogationScreen, re-shows "drag here" text if case still open.
    /// </summary>
    public void ReleaseSuspectFromMonitor()
    {
        if (currentMonitorSuspect == null)
        {
            Debug.LogWarning("[SuspectManager] No suspect on monitor to release");
            return;
        }

        string releasedName = currentMonitorSuspect.FullName;

        // End interrogation if active
        if (isInterrogationMode && interrogationScreen != null)
        {
            interrogationScreen.EndInterrogation();
        }

        currentMonitorSuspect = null;

        // Clear the view content
        if (interrogationScreen != null)
        {
            interrogationScreen.ClearView();

            // Re-show "drag here" if case is still open
            if (currentCase != null)
                interrogationScreen.SetCaseOpen(true);
        }

        Debug.Log($"[SuspectManager] Released {releasedName} from monitor");
    }

    /// <summary>
    /// Set animation state on both monitor and detailed view (replaces index-based version).
    /// </summary>
    public void SetMonitorSuspectAnimationState(SuspectAnimationState state)
    {
        if (currentMonitorSuspect == null) return;

        if (interrogationScreen != null)
        {
            interrogationScreen.SetAnimationState(state);
        }

        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Set monitor suspect animation to {state}");
    }

    /// <summary>
    /// Set interrogation mode (called by InterrogationScreen)
    /// </summary>
    public void SetInterrogationMode(bool enabled)
    {
        isInterrogationMode = enabled;

        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Interrogation mode set to: {enabled}");
    }

    /// <summary>
    /// Show the detailed view for a suspect
    /// </summary>
    private void ShowInterrogationScreen(Citizen suspect)
    {
        if (suspect == null || interrogationScreen == null) return;

        SuspectAnimationState currentState = SuspectAnimationState.BeingInterrogated;
        SuspectAnimationSet animSet = GetAnimationSetForSuspect(suspect);

        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Setting up interrogation screen with animation state: {currentState}");

        interrogationScreen.SetSuspect(suspect, animSet);
        interrogationScreen.SetAnimationState(currentState);
    }

    /// <summary>
    /// Hide the interrogation screen (ends interrogation and clears suspect)
    /// </summary>
    public void HideInterrogationScreen()
    {
        if (interrogationScreen == null) return;

        // If we're in interrogation mode, end it first
        if (isInterrogationMode)
        {
            interrogationScreen.EndInterrogation();
        }

        interrogationScreen.ClearView();
    }

    /// <summary>
    /// Clear the single monitor
    /// </summary>
    private void ClearMonitor()
    {
        // Release suspect if present
        if (currentMonitorSuspect != null)
        {
            ReleaseSuspectFromMonitor();
        }

        if (interrogationScreen != null)
            interrogationScreen.SetCaseOpen(false);
    }

    /// <summary>
    /// Get currently selected suspect (the one on the monitor)
    /// </summary>
    public Citizen GetSelectedSuspect()
    {
        return currentMonitorSuspect;
    }

    /// <summary>
    /// Get the current active case
    /// </summary>
    public Case GetCurrentCase()
    {
        return currentCase;
    }

    /// <summary>
    /// Mark a suspect as interrogated in the case management system
    /// </summary>
    public void MarkSuspectInterrogated(Citizen suspect)
    {
        if (currentCase != null && suspect != null && CaseManager.Instance != null)
        {
            CaseManager.Instance.MarkSuspectInterrogated(currentCase.caseID, suspect.citizenID);

            if (debugAnimationInfo)
                Debug.Log($"[SuspectManager] Marked {suspect.FullName} as interrogated for case {currentCase.title}");
        }
    }

    /// <summary>
    /// Check if currently in interrogation mode
    /// </summary>
    public bool IsInInterrogationMode => isInterrogationMode;

    /// <summary>
    /// Initialize the list of available criminal violations
    /// </summary>
    private void InitializeViolations()
    {
        if (availableViolations.Count == 0)
        {
            availableViolations.Add(new CriminalViolation("Theft", "Unlawfully taking property belonging to another", CrimeSeverity.Minor));
            availableViolations.Add(new CriminalViolation("Burglary", "Breaking and entering with intent to commit theft", CrimeSeverity.Moderate));
            availableViolations.Add(new CriminalViolation("Assault", "Intentionally causing bodily harm to another person", CrimeSeverity.Moderate));
            availableViolations.Add(new CriminalViolation("Armed Robbery", "Theft using a weapon or threat of violence", CrimeSeverity.Major));
            availableViolations.Add(new CriminalViolation("Fraud", "Intentionally deceiving others for financial gain", CrimeSeverity.Moderate));
            availableViolations.Add(new CriminalViolation("Vandalism", "Deliberately damaging or destroying property", CrimeSeverity.Minor));
            availableViolations.Add(new CriminalViolation("Drug Possession", "Unlawful possession of controlled substances", CrimeSeverity.Minor));
            availableViolations.Add(new CriminalViolation("Drug Trafficking", "Manufacturing or distributing illegal drugs", CrimeSeverity.Major));
            availableViolations.Add(new CriminalViolation("Murder", "Unlawfully killing another human being", CrimeSeverity.Severe));
            availableViolations.Add(new CriminalViolation("Kidnapping", "Unlawfully detaining or abducting another person", CrimeSeverity.Severe));
            availableViolations.Add(new CriminalViolation("Arson", "Deliberately setting fire to property", CrimeSeverity.Major));
            availableViolations.Add(new CriminalViolation("Embezzlement", "Misappropriating funds or property entrusted to one's care", CrimeSeverity.Moderate));
        }
    }

    /// <summary>
    /// Called when arrest process is completed successfully
    /// </summary>
    private void OnArrestCompleted(Citizen suspect, CriminalViolation selectedViolation)
    {
        Debug.Log($"[SuspectManager] Arrest completed for {suspect.FullName} - Charge: {selectedViolation.violationName}");

        // Close the case using the same logic as debug menu
        StartCoroutine(ProcessArrestCompletion(suspect, selectedViolation));
    }

    /// <summary>
    /// Called when arrest process is cancelled
    /// </summary>
    private void OnArrestCancelled()
    {
        Debug.Log("[SuspectManager] Arrest cancelled");

        // Reset suspect animation to idle
        if (suspectBeingArrested != null)
        {
            if (suspectBeingArrested == currentMonitorSuspect)
            {
                SetMonitorSuspectAnimationState(SuspectAnimationState.Idle);
            }
        }

        suspectBeingArrested = null;
    }

    /// <summary>
    /// Process the completion of an arrest and close the case
    /// </summary>
    private IEnumerator ProcessArrestCompletion(Citizen suspect, CriminalViolation violation)
    {
        // Wait for popup close animation (handled by UIManager)
        yield return new WaitForSeconds(0.5f);

        suspectBeingArrested = null;

        // Close the case using GameManager's debug close logic
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DebugCloseCaseFlow();
        }
        else
        {
            Debug.LogError("[SuspectManager] GameManager not found - cannot close case");
        }
    }

    public void CommitSuspect(Citizen suspect, CriminalViolation crime)
    {
        if (currentCase == null)
        {
            Debug.LogWarning("[SuspectManager] Cannot commit suspect - no active case");
            return;
        }

        // Add violation to suspect's record
        suspect.AddViolation(crime);

        // Mark case as solved
        currentCase.MarkAsSolved(suspect, crime);

        // Notify case manager
        if (CaseManager.Instance != null)
        {
            CaseManager.Instance.OnCaseSolved(currentCase);
        }

        // Clear current case
        currentCase = null;
    }

    /// <summary>
    /// Check if suspect is assigned to the monitor
    /// </summary>
    public bool IsSuspectAssignedToAnyMonitor(Citizen suspect)
    {
        return currentMonitorSuspect != null && currentMonitorSuspect == suspect;
    }

    // Debug methods
    [ContextMenu("Test Random Animation States")]
    private void TestRandomAnimationStates()
    {
        if (currentMonitorSuspect == null)
        {
            Debug.Log("[SuspectManager] No suspect on monitor to test animation states");
            return;
        }

        var states = System.Enum.GetValues(typeof(SuspectAnimationState));
        var randomState = (SuspectAnimationState)states.GetValue(Random.Range(0, states.Length));
        SetMonitorSuspectAnimationState(randomState);
    }
}

// Enums and supporting classes
public enum SuspectAnimationState
{
    Idle,
    Nervous,
    Crying,
    HeadDown,
    Angry,
    Pacing,
    BeingInterrogated,
    GettingArrested
}

[System.Serializable]
public class SuspectAnimationSet
{
    [Header("Male/Female Silhouette Animations")]
    public Texture2D idleSheet;
    public Texture2D nervousSheet;
    public Texture2D cryingSheet;
    public Texture2D headDownSheet;
    public Texture2D angrySheet;
    public Texture2D pacingSheet;
    public Texture2D interrogationSheet;
    public Texture2D arrestSheet;

    [Header("Sprite Sheet Settings")]
    public int framesPerRow = 8;
    public int totalFrames = 16;
    public int spriteWidth = 64;
    public int spriteHeight = 64;

    public Texture2D GetSheetForState(SuspectAnimationState state)
    {
        switch (state)
        {
            case SuspectAnimationState.Idle: return idleSheet;
            case SuspectAnimationState.Nervous: return nervousSheet;
            case SuspectAnimationState.Crying: return cryingSheet;
            case SuspectAnimationState.HeadDown: return headDownSheet;
            case SuspectAnimationState.Angry: return angrySheet;
            case SuspectAnimationState.Pacing: return pacingSheet;
            case SuspectAnimationState.BeingInterrogated: return interrogationSheet;
            case SuspectAnimationState.GettingArrested: return arrestSheet;
            default: return idleSheet;
        }
    }
}
