using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
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
    
    [Header("CCTV Monitors")]
    public List<CCTVMonitor> cctvMonitors = new List<CCTVMonitor>(4); // 4 small monitors
    public DetailedSuspectView detailedView; // Large center view
    
    [Header("Channel Selection")]
    public List<Button> channelButtons = new List<Button>(4); // 1, 2, 3, 4 buttons
    public Color selectedChannelColor = Color.green;
    public Color unselectedChannelColor = Color.gray;
    
    [Header("Animation Sprite Sheets")]
    [Tooltip("Male silhouette sprite sheets - each should contain all animation frames")]
    public SuspectAnimationSet maleAnimations;
    [Tooltip("Female silhouette sprite sheets - each should contain all animation frames")]
    public SuspectAnimationSet femaleAnimations;
    
    [Header("Animation Settings")]
    public float animationFrameRate = 8f; // Frames per second
    public bool debugAnimationInfo = false;
    
    [Header("UI Transitions")]
    public float detailViewFadeInDuration = 0.5f;
    public Ease detailViewEase = Ease.OutQuart;
    
    [Header("Arrest System")]
    public List<CriminalViolation> availableViolations = new List<CriminalViolation>(); // List of charges
    
    // Current state
    private List<Citizen> currentSuspects = new List<Citizen>();
    private int selectedSuspectIndex = -1;
    private bool isDetailViewActive = false;
    private bool isInterrogationMode = false; // NEW: Track interrogation mode
    
    // Arrest state
    private Citizen suspectBeingArrested = null;
    
    // UI Components
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;

    protected override void OnSingletonAwake()
    {
        // Initialize CCTV monitors (remove click listeners since we use channel buttons now)
        for (int i = 0; i < cctvMonitors.Count; i++)
        {
            if (cctvMonitors[i] != null)
            {
                cctvMonitors[i].Initialize(i);
                // Don't add click listeners anymore - channels controlled by buttons
            }
        }
        
        // Setup channel button listeners
        for (int i = 0; i < channelButtons.Count; i++)
        {
            int channelIndex = i; // Capture for closure
            if (channelButtons[i] != null)
            {
                channelButtons[i].onClick.AddListener(() => SelectChannel(channelIndex));
                // Set initial visual state
                UpdateChannelButtonVisual(i, false);
            }
        }
        
        // Keep detailed view active but ensure it starts with proper state
        if (detailedView != null)
        {
            // Make sure detailed view is active so channel buttons work
            detailedView.gameObject.SetActive(true);
            isDetailViewActive = true;
            
            // Set initial alpha to 0 (will fade in when first suspect is selected)
            CanvasGroup canvasGroup = detailedView.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = detailedView.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }
        
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
    
    private void ConnectToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened += OnCaseOpened;
            GameManager.Instance.OnCaseClosed += OnCaseClosed;
            
            if (debugAnimationInfo)
                Debug.Log("[SuspectManager] Connected to GameManager events");
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

        // Cleanup channel button listeners
        for (int i = 0; i < channelButtons.Count; i++)
        {
            if (channelButtons[i] != null)
            {
                channelButtons[i].onClick.RemoveAllListeners();
            }
        }
    }
    
    private void OnCaseOpened(Case openedCase)
    {
        if (debugAnimationInfo)
    
            
        LoadSuspectsFromCase(openedCase);
    }
    
    private void OnCaseClosed(Case closedCase)
    {
        if (debugAnimationInfo)
    
            
        ClearAllMonitors();
        currentCase = null;
    }

    /// <summary>
    /// Load suspects from a case and display them on CCTV monitors
    /// </summary>
    public void LoadSuspectsFromCase(Case caseData)
    {
        currentCase = caseData;
        currentSuspects.Clear();
        
        if (caseData == null || caseData.suspects == null)
        {

            ClearAllMonitors();
            return;
        }
        
        // Add suspects up to monitor limit
        for (int i = 0; i < Mathf.Min(caseData.suspects.Count, cctvMonitors.Count); i++)
        {
            if (caseData.suspects[i] != null)
            {
                currentSuspects.Add(caseData.suspects[i]);
            }
        }
        
        // Display suspects on monitors
        DisplaySuspectsOnMonitors();
        
        // Automatically select channel 1 (index 0) if we have suspects
        if (currentSuspects.Count > 0)
        {
            SelectChannel(0); // This will show detailed view and update button states
        }
        else
        {
            // Update channel button states even if no suspects
            UpdateChannelButtonStates();
        }
    
    }

    /// <summary>
    /// Display current suspects on CCTV monitors
    /// </summary>
    private void DisplaySuspectsOnMonitors()
    {
        for (int i = 0; i < cctvMonitors.Count; i++)
        {
            if (i < currentSuspects.Count && cctvMonitors[i] != null)
            {
                Citizen suspect = currentSuspects[i];
                SuspectAnimationSet animSet = GetAnimationSetForSuspect(suspect);
                
                cctvMonitors[i].SetSuspect(suspect, animSet);
                cctvMonitors[i].SetAnimationState(SuspectAnimationState.Idle);
            }
            else if (cctvMonitors[i] != null)
            {
                cctvMonitors[i].ClearMonitor();
            }
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
    /// Select a channel (replaces old SelectSuspect method)
    /// </summary>
    public void SelectChannel(int channelIndex)
    {
        // Prevent channel switching during interrogation
        if (isInterrogationMode)
        {
            return;
        }
            
        if (channelIndex < 0 || channelIndex >= currentSuspects.Count)
        {
            return;
        }
        
        // Update selected index
        selectedSuspectIndex = channelIndex;
        
        // Update channel button visuals
        UpdateChannelButtonStates();
        
        // Get selected suspect
        Citizen selectedSuspect = currentSuspects[channelIndex];
        
        // Notify InterrogationManager and ChatManager about suspect change
        // This ensures chat state switches even when not in interrogation mode
        if (InterrogationManager.Instance != null)
        {
            // Switch the chat context to the new suspect
            InterrogationManager.Instance.SwitchToSuspect(selectedSuspect.citizenID);
            
            // Hide drop zone when switching channels outside of interrogation mode
            // (The drop zone should only be visible during active interrogation)
            if (InterrogationManager.Instance.chatManager != null)
            {
                InterrogationManager.Instance.chatManager.HideDropZoneImmediate();
            }
        }
        else
        {
            Debug.LogWarning("[SuspectManager] InterrogationManager.Instance not found - chat state may not update");
        }
        
        // Show detailed view for selected suspect
        ShowDetailedView(selectedSuspect);
    }

    /// <summary>
    /// Update visual states of channel buttons
    /// </summary>
    private void UpdateChannelButtonStates()
    {
        for (int i = 0; i < channelButtons.Count; i++)
        {
            if (channelButtons[i] != null)
            {
                bool isSelected = (i == selectedSuspectIndex);
                bool hasData = (i < currentSuspects.Count);
                
                // Enable/disable button based on data availability and interrogation mode
                channelButtons[i].interactable = hasData && !isInterrogationMode;
                
                // Update visual appearance
                UpdateChannelButtonVisual(i, isSelected);
            }
        }
    }

    /// <summary>
    /// Update visual appearance of a single channel button
    /// </summary>
    private void UpdateChannelButtonVisual(int buttonIndex, bool isSelected)
    {
        if (buttonIndex >= channelButtons.Count || channelButtons[buttonIndex] == null) return;
        
        Button button = channelButtons[buttonIndex];
        Image buttonImage = button.GetComponent<Image>();
        
        if (buttonImage != null)
        {
            buttonImage.color = isSelected ? selectedChannelColor : unselectedChannelColor;
        }
        
        // Optionally update button text or other visual elements
        var buttonText = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = (buttonIndex + 1).ToString(); // Show 1, 2, 3, 4
        }
    }

    /// <summary>
    /// Set interrogation mode (called by DetailedSuspectView)
    /// </summary>
    public void SetInterrogationMode(bool enabled)
    {
        isInterrogationMode = enabled;
        UpdateChannelButtonStates(); // Update button interactability
        
        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Interrogation mode set to: {enabled}");
    }

    /// <summary>
    /// Show the detailed view for a suspect
    /// </summary>
    private void ShowDetailedView(Citizen suspect)
    {
        if (suspect == null || detailedView == null) return;
        
        SuspectAnimationState currentState = SuspectAnimationState.BeingInterrogated;
        SuspectAnimationSet animSet = GetAnimationSetForSuspect(suspect);
        
        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Setting up detailed view with animation state: {currentState}");
        
        // Ensure detailed view is active and get canvas group
        CanvasGroup canvasGroup = detailedView.GetComponent<CanvasGroup>();
        if (canvasGroup == null && detailedView != null && detailedView.gameObject != null)
            canvasGroup = detailedView.gameObject.AddComponent<CanvasGroup>();
        
        // Setup detailed view content (GameObject should already be active)
        detailedView.SetSuspect(suspect, animSet);
        detailedView.SetAnimationState(currentState);
        
        // Fade in the detailed view if it's not visible
        if (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            if (debugAnimationInfo)
                Debug.Log("[SuspectManager] Fading in detailed view");
                
            canvasGroup.DOFade(1f, detailViewFadeInDuration).SetEase(detailViewEase);
        }
        
        // Mark as active
        isDetailViewActive = true;
    }

    /// <summary>
    /// Hide the detailed view (only use when you want to fully disable it)
    /// For normal case switching, ClearAllMonitors() will fade it out while keeping it active
    /// </summary>
    public void HideDetailedView()
    {
        if (detailedView == null || !isDetailViewActive) return;

        // If we're in interrogation mode, end it first
        if (isInterrogationMode && detailedView.GetComponent<DetailedSuspectView>() != null)
        {
            var detailedSuspectView = detailedView.GetComponent<DetailedSuspectView>();
            if (detailedSuspectView != null)
            {
                // This will call EndInterrogation which will call SetInterrogationMode(false)
                detailedSuspectView.EndInterrogation();
            }
        }

        CanvasGroup canvasGroup = detailedView.GetComponent<CanvasGroup>();
        if (canvasGroup != null && detailedView != null)
        {
            canvasGroup.DOFade(0f, detailViewFadeInDuration).SetEase(detailViewEase)
                .OnComplete(() => {
                    if (detailedView != null && detailedView.gameObject != null)
                    {
                        detailedView.gameObject.SetActive(false);
                        isDetailViewActive = false;
                    }
                });
        }
        else
        {
            if (detailedView != null && detailedView.gameObject != null)
            {
                detailedView.gameObject.SetActive(false);
            }
            isDetailViewActive = false;
        }
        
        selectedSuspectIndex = -1;
        
        // Update channel button states
        UpdateChannelButtonStates();
    }

    /// <summary>
    /// Set animation state for a specific suspect
    /// </summary>
    public void SetSuspectAnimationState(int suspectIndex, SuspectAnimationState state)
    {
        if (suspectIndex < 0 || suspectIndex >= cctvMonitors.Count) return;

        cctvMonitors[suspectIndex].SetAnimationState(state);
        
        // If this suspect is currently selected, sync detailed view
        if (selectedSuspectIndex == suspectIndex && isDetailViewActive)
        {
            detailedView.SetAnimationState(state);
        }
        
        if (debugAnimationInfo)
            Debug.Log($"[SuspectManager] Set suspect {suspectIndex} animation to {state}");
    }

    /// <summary>
    /// Set animation state for all suspects
    /// </summary>
    public void SetAllSuspectsAnimationState(SuspectAnimationState state)
    {
        for (int i = 0; i < currentSuspects.Count; i++)
        {
            SetSuspectAnimationState(i, state);
        }
    }

    /// <summary>
    /// Clear all monitors
    /// </summary>
    private void ClearAllMonitors()
    {
        foreach (var monitor in cctvMonitors)
        {
            if (monitor != null)
                monitor.ClearMonitor();
        }
        
        // Fade out detailed view instead of hiding it completely
        // This keeps the channel buttons accessible
        if (detailedView != null && isDetailViewActive)
        {
            CanvasGroup canvasGroup = detailedView.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, detailViewFadeInDuration).SetEase(detailViewEase);
            }
            // Keep isDetailViewActive = true so the view stays ready for new suspects
        }
        
        // Reset channel buttons
        selectedSuspectIndex = -1;
        UpdateChannelButtonStates();
    }

    /// <summary>
    /// Get current suspects
    /// </summary>
    public List<Citizen> GetCurrentSuspects()
    {
        return new List<Citizen>(currentSuspects);
    }

    /// <summary>
    /// Get currently selected suspect
    /// </summary>
    public Citizen GetSelectedSuspect()
    {
        if (selectedSuspectIndex >= 0 && selectedSuspectIndex < currentSuspects.Count)
            return currentSuspects[selectedSuspectIndex];
        return null;
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
            var suspects = GetCurrentSuspects();
            for (int i = 0; i < suspects.Count; i++)
            {
                if (suspects[i] == suspectBeingArrested)
                {
                    SetSuspectAnimationState(i, SuspectAnimationState.Idle);
                    break;
                }
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

    // Debug methods
    [ContextMenu("Test Random Animation States")]
    private void TestRandomAnimationStates()
    {
        var states = System.Enum.GetValues(typeof(SuspectAnimationState));
        for (int i = 0; i < currentSuspects.Count; i++)
        {
            var randomState = (SuspectAnimationState)states.GetValue(Random.Range(0, states.Length));
            SetSuspectAnimationState(i, randomState);
        }
    }
    
    [ContextMenu("Test Channel Switching")]
    private void TestChannelSwitching()
    {
        Debug.Log("=== Testing Channel Switching ===");
        Debug.Log($"Current suspects count: {currentSuspects.Count}");
        Debug.Log($"Selected suspect index: {selectedSuspectIndex}");
        Debug.Log($"Interrogation mode: {isInterrogationMode}");
        
        if (InterrogationManager.Instance != null)
        {
            Debug.Log($"InterrogationManager current suspect: {InterrogationManager.Instance.CurrentSuspectId}");
            
            if (InterrogationManager.Instance.chatManager != null)
            {
                Debug.Log("ChatManager is available");
            }
            else
            {
                Debug.LogWarning("ChatManager is NULL");
            }
        }
        else
        {
            Debug.LogWarning("InterrogationManager.Instance is NULL");
        }
        
        Debug.Log("=== End Test ===");
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
    
    public void ReleaseSuspect(Citizen suspect)
    {
        // Remove suspect from any monitor
        for (int i = 0; i < cctvMonitors.Count; i++)
        {
            if (cctvMonitors[i] != null && cctvMonitors[i].GetCurrentSuspect() == suspect)
            {
                cctvMonitors[i].ClearMonitor();
                break;
            }
        }
        
        Debug.Log($"Released {suspect.GetFullName()} from monitor");
    }
    
    public bool IsSuspectAssignedToAnyMonitor(Citizen suspect)
    {
        for (int i = 0; i < cctvMonitors.Count; i++)
        {
            if (cctvMonitors[i] != null && cctvMonitors[i].GetCurrentSuspect() == suspect)
            {
                return true;
            }
        }
        return false;
    }
    
    public int GetMonitorIndexForSuspect(Citizen suspect)
    {
        for (int i = 0; i < cctvMonitors.Count; i++)
        {
            if (cctvMonitors[i] != null && cctvMonitors[i].GetCurrentSuspect() == suspect)
            {
                return i;
            }
        }
        return -1; // Not found
    }
    
    public void AssignCitizenToMonitor(Citizen citizen, int monitorIndex)
    {
        if (monitorIndex < 0 || monitorIndex >= cctvMonitors.Count)
        {
            Debug.LogError($"[SuspectManager] Invalid monitor index: {monitorIndex}");
            return;
        }
        
        if (citizen == null)
        {
            Debug.LogError("[SuspectManager] Cannot assign null citizen to monitor");
            return;
        }
        
        // Clear the monitor first
        cctvMonitors[monitorIndex].ClearMonitor();
        
        // Get the appropriate animation set for this citizen
        SuspectAnimationSet animationSet = GetAnimationSetForSuspect(citizen);
        
        // Assign the citizen to the monitor
        cctvMonitors[monitorIndex].SetSuspect(citizen, animationSet);
        
        // Add to current suspects if not already there
        if (!currentSuspects.Contains(citizen))
        {
            currentSuspects.Add(citizen);
        }
        
        Debug.Log($"[SuspectManager] Assigned {citizen.FullName} to monitor {monitorIndex}");
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