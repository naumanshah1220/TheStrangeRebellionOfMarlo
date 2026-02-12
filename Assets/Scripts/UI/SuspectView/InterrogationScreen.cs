using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterrogationScreen : MonoBehaviour
{
    [Header("Display Components")]
    public Image suspectImage; // Large detailed suspect display
    public TextMeshProUGUI suspectNameText;
    public TextMeshProUGUI suspectInfoText; // Age, occupation, etc.
    public Image backgroundPanel;

    [Header("Drop Zone")]
    public TextMeshProUGUI dragHereText;       // "Drag suspect here" prompt
    public TextMeshProUGUI errorText;          // Error message overlay
    public Sprite defaultInactiveSprite;       // Image when no suspect assigned
    public float errorMessageDuration = 2.0f;
    public float errorFadeDuration = 0.5f;

    [Header("Interaction Buttons")]
    public Button interrogateButton;
    public Button arrestButton;
    public Button releaseButton; // New release button
    
    [Header("Visual Settings")]
    public float animationScale = 2f; // Scale compared to CCTV monitor
    public bool showDetailedTextures = true; // Use detailed sprites instead of silhouettes
    
    [Header("Interrogation UI")]
    public GameObject interrogationPanel; // Panel that shows during interrogation  
    public InterrogationManager interrogationManager; // Reference to interrogation manager
    
    // Current state
    private Citizen currentSuspect;
    private SuspectAnimationSet currentAnimationSet;
    private SuspectAnimationState currentAnimationState = SuspectAnimationState.Idle;
    private bool isInInterrogationMode = false;
    
    // Animation
    private Coroutine animationCoroutine;
    private int currentFrame = 0;

    private bool isCaseOpen = false;
    private Coroutine errorMessageCoroutine;

    private void Awake()
    {
        // Setup button listeners
        if (interrogateButton != null)
            interrogateButton.onClick.AddListener(OnInterrogateClicked);
            
        if (arrestButton != null)
            arrestButton.onClick.AddListener(OnArrestClicked);
            
        if (releaseButton != null)
            releaseButton.onClick.AddListener(OnReleaseClicked);
            
        // Ensure interrogation panel starts hidden
        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(false);
            isInInterrogationMode = false; // Ensure state is synced
            Debug.Log("[InterrogationScreen] Interrogation panel initialized and hidden");
        }
        else
        {
            Debug.LogError("[InterrogationScreen] interrogationPanel reference is missing! Please assign it in the Inspector.");
        }
        
        // Validate interrogation manager reference
        if (interrogationManager == null)
        {
            Debug.LogError("[InterrogationScreen] interrogationManager reference is missing! Please assign it in the Inspector.");
        }

        // Initialize drop zone elements
        if (errorText != null) errorText.alpha = 0f;
        if (dragHereText != null) dragHereText.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // Cleanup button listeners
        if (interrogateButton != null)
            interrogateButton.onClick.RemoveListener(OnInterrogateClicked);
            
        if (arrestButton != null)
            arrestButton.onClick.RemoveListener(OnArrestClicked);
            
        if (releaseButton != null)
            releaseButton.onClick.RemoveListener(OnReleaseClicked);
    }
    
    public void SetSuspect(Citizen suspect, SuspectAnimationSet animationSet)
    {
        Debug.Log($"[InterrogationScreen] SetSuspect called - New: '{suspect?.FullName}', Current: '{currentSuspect?.FullName}', InInterrogationMode: {isInInterrogationMode}");
        
        // If we're switching suspects while in interrogation mode, this should only happen
        // when interrogation mode is disabled (channel switching is prevented during interrogation)
        bool wasInInterrogationMode = isInInterrogationMode;
        
        currentSuspect = suspect;
        currentAnimationSet = animationSet;
        
        UpdateSuspectInfo();
        
        // If we were in interrogation mode and switching suspects, update the interrogation context
        if (wasInInterrogationMode && suspect != null && interrogationManager != null)
        {
            // Switch the interrogation manager to the new suspect
            interrogationManager.SwitchToSuspect(suspect.citizenID);
            Debug.Log($"[InterrogationScreen] Switched interrogation context to: {suspect.FullName} (ID: {suspect.citizenID})");
            
            // Update the suspect name in the interrogation UI if it exists
            UpdateInterrogationUI();
        }
        else if (!wasInInterrogationMode)
        {
            // If we weren't in interrogation mode, make sure we're not accidentally in it now
            isInInterrogationMode = false;
        }
        
        // Start with current animation state
        if (suspect != null)
        {
            SetAnimationState(currentAnimationState);
        }

        UpdateDragTextVisibility();
    }
    
    private void UpdateSuspectInfo()
    {
        if (currentSuspect == null) return;
        
        // Update name
        if (suspectNameText != null)
        {
            suspectNameText.text = currentSuspect.FullName;
        }
        
        // Update info text with personal details
        if (suspectInfoText != null)
        {
            string info = "";
            
            // Calculate age from date of birth (simplified)
            if (!string.IsNullOrEmpty(currentSuspect.dateOfBirth))
            {
                info += $"DOB: {currentSuspect.dateOfBirth}\n";
            }
            
            info += $"Gender: {currentSuspect.gender}\n";
            info += $"Ethnicity: {currentSuspect.ethnicity}\n";
            
            if (!string.IsNullOrEmpty(currentSuspect.occupation))
            {
                info += $"Occupation: {currentSuspect.occupation}\n";
            }
            
            info += $"Address: {currentSuspect.address}\n";
            info += $"Marital Status: {currentSuspect.maritalStatus}\n";
            
            if (currentSuspect.HasCriminalRecord)
            {
                info += $"Criminal Record: {currentSuspect.criminalHistory.Count} entries\n";
            }
            else
            {
                info += "Criminal Record: Clean\n";
            }
            
            suspectInfoText.text = info.TrimEnd();
        }
        
        // Enable/disable interaction buttons based on suspect
        UpdateButtonStates();
    }
    
    private void UpdateButtonStates()
    {
        bool hasSuspect = currentSuspect != null;
        
        if (interrogateButton != null)
        {
            interrogateButton.interactable = hasSuspect;
            
            // Initialize button appearance based on current state
            if (hasSuspect)
            {
                UpdateInterrogateButtonAppearance(isInInterrogationMode);
            }
            else
            {
                // Reset to default appearance when no suspect
                var buttonText = interrogateButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = "Start Interrogation";
                }
                
                var buttonImage = interrogateButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.white; // Default color
                }
            }
        }
            
        if (arrestButton != null)
            arrestButton.interactable = hasSuspect;
            
        if (releaseButton != null)
            releaseButton.interactable = hasSuspect;
    }
    
    public void SetAnimationState(SuspectAnimationState state)
    {
        currentAnimationState = state;
        
        if (currentAnimationSet == null || currentSuspect == null)
            return;
            
        // Stop current animation
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        // Start new animation
        Texture2D spriteSheet = currentAnimationSet.GetSheetForState(state);
        if (spriteSheet != null)
        {
            animationCoroutine = StartCoroutine(PlayDetailedAnimation(spriteSheet));
        }
        else
        {
            Debug.LogWarning($"[InterrogationScreen] No sprite sheet found for state: {state}");
        }
    }
    
    private IEnumerator PlayDetailedAnimation(Texture2D spriteSheet)
    {
        if (spriteSheet == null || currentAnimationSet == null) yield break;
        
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager == null) yield break;
        
        float frameTime = 1f / suspectManager.animationFrameRate;
        currentFrame = 0;
        
        while (currentSuspect != null && gameObject.activeInHierarchy)
        {
            // Calculate sprite position in sheet
            int framesPerRow = currentAnimationSet.framesPerRow;
            int totalFrames = currentAnimationSet.totalFrames;
            
            int x = (currentFrame % framesPerRow) * currentAnimationSet.spriteWidth;
            int y = (currentFrame / framesPerRow) * currentAnimationSet.spriteHeight;
            
            // Create sprite from sheet
            Rect spriteRect = new Rect(x, spriteSheet.height - y - currentAnimationSet.spriteHeight, 
                                     currentAnimationSet.spriteWidth, currentAnimationSet.spriteHeight);
            
            Sprite frameSprite = Sprite.Create(spriteSheet, spriteRect, Vector2.one * 0.5f, 100f);
            
            // Apply to suspect image
            if (suspectImage != null)
            {
                suspectImage.sprite = frameSprite;
                
                // Apply appropriate filter mode (detailed view might use bilinear)
                if (showDetailedTextures)
                {
                    suspectImage.sprite.texture.filterMode = FilterMode.Bilinear;
                }
                else
                {
                    suspectImage.sprite.texture.filterMode = FilterMode.Point;
                }
            }
            
            // Move to next frame
            currentFrame = (currentFrame + 1) % totalFrames;
            
            yield return new WaitForSeconds(frameTime);
        }
    }
    
    // Button event handlers
    private void OnCloseClicked()
    {
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager != null)
        {
            suspectManager.HideInterrogationScreen();
        }
    }
    
    private void OnInterrogateClicked()
    {
        Debug.Log($"[InterrogationScreen] Interrogate button clicked - currentSuspect: {currentSuspect?.FullName}, isInInterrogationMode: {isInInterrogationMode}");
        
        if (currentSuspect == null) 
        {
            Debug.LogWarning("[InterrogationScreen] Cannot start interrogation - no suspect selected");
            return;
        }
        
        if (!isInInterrogationMode)
        {
            Debug.Log("[InterrogationScreen] Starting interrogation...");
            StartInterrogation();
        }
        else
        {
            Debug.Log("[InterrogationScreen] Ending interrogation...");
            EndInterrogation();
        }
    }
    
    private void StartInterrogation()
    {
        isInInterrogationMode = true;
        
        // Notify SuspectManager about interrogation mode
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager != null)
        {
            suspectManager.SetInterrogationMode(true);
            
            // Update animation on the single monitor
            suspectManager.SetMonitorSuspectAnimationState(SuspectAnimationState.BeingInterrogated);
            
            // Mark suspect as interrogated in case management
            suspectManager.MarkSuspectInterrogated(currentSuspect);
        }
        
        // Open notebook
        OpenNotebook();
        
        // Show interrogation UI
        ShowInterrogationUI();
        
        // Update button appearance for toggle state
        UpdateInterrogateButtonAppearance(true);
        
        Debug.Log($"[InterrogationScreen] Started interrogating {currentSuspect.FullName} - Channel switching disabled");
    }
    
    public void EndInterrogation()
    {
        isInInterrogationMode = false;
        
        // Notify SuspectManager about leaving interrogation mode
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager != null)
        {
            suspectManager.SetInterrogationMode(false);
            
            // Update animation on the single monitor
            suspectManager.SetMonitorSuspectAnimationState(SuspectAnimationState.Idle);
        }
        
        // Hide interrogation UI
        HideInterrogationUI();
        
        // Hide the chat drop zone since interrogation is ending
        if (interrogationManager != null && interrogationManager.chatManager != null)
        {
            interrogationManager.chatManager.HideDropZoneImmediate();
        }
        
        // Update button appearance for toggle state
        UpdateInterrogateButtonAppearance(false);
        
        Debug.Log($"[InterrogationScreen] Ended interrogation with {currentSuspect.FullName} - Channel switching enabled");
    }
    
    /// <summary>
    /// Update the interrogate button appearance to show toggle state
    /// </summary>
    private void UpdateInterrogateButtonAppearance(bool isInterrogating)
    {
        if (interrogateButton != null)
        {
            var buttonText = interrogateButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isInterrogating ? "End Interrogation" : "Start Interrogation";
            }
            
            // Change button color to indicate toggle state
            var buttonImage = interrogateButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isInterrogating ? Color.red : Color.green;
            }
        }
    }
    
    private void OpenNotebook()
    {
        // Find and open the notebook
        var notebookManager = FindFirstObjectByType<NotebookManager>();
        if (notebookManager != null)
        {
            notebookManager.OpenNotebook();
        }
        else
        {
            Debug.LogWarning("[InterrogationScreen] Could not find NotebookManager to open");
        }
    }
    
    private void ShowInterrogationUI()
    {
        Debug.Log($"[InterrogationScreen] ShowInterrogationUI called - panel null: {interrogationPanel == null}, manager null: {interrogationManager == null}");
        
        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(true);
            Debug.Log($"[InterrogationScreen] Showed interrogation panel for {currentSuspect?.FullName}");
        }
        else
        {
            Debug.LogError("[InterrogationScreen] interrogationPanel is null, cannot show. Please check the Inspector assignment.");
            return; // Don't proceed if panel is missing
        }
        
        // Initialize interrogation manager with current suspect
        if (interrogationManager != null && currentSuspect != null)
        {
            interrogationManager.SwitchToSuspect(currentSuspect.citizenID);
            
            // Always show the dropzone when starting interrogation, regardless of conversation history
            if (interrogationManager.chatManager != null)
            {
                StartCoroutine(ShowDropZoneAfterUIUpdate());
            }
            else
            {
                Debug.LogWarning("[InterrogationScreen] interrogationManager.chatManager is null");
            }
        }
        else
        {
            if (interrogationManager == null)
                Debug.LogError("[InterrogationScreen] interrogationManager is null");
            if (currentSuspect == null)
                Debug.LogError("[InterrogationScreen] currentSuspect is null");
        }
    }
    
    /// <summary>
    /// Show dropzone after UI components are fully updated
    /// </summary>
    private IEnumerator ShowDropZoneAfterUIUpdate()
    {
        yield return new WaitForSeconds(0.1f);
        if (interrogationManager != null && interrogationManager.chatManager != null)
        {
            interrogationManager.chatManager.ShowDropZone();
            Debug.Log("[InterrogationScreen] Explicitly showed dropzone for interrogation mode");
        }
    }
    
    private void HideInterrogationUI()
    {
        Debug.Log($"[InterrogationScreen] HideInterrogationUI called - panel null: {interrogationPanel == null}");
        
        if (interrogationPanel != null)
        {
            interrogationPanel.SetActive(false);
            Debug.Log($"[InterrogationScreen] Hid interrogation panel for {currentSuspect?.FullName}");
        }
        else
        {
            Debug.LogError("[InterrogationScreen] interrogationPanel is null, cannot hide. Please check the Inspector assignment.");
        }
    }
    
    private void UpdateInterrogationUI()
    {
        // This method can be used to refresh any UI elements in the interrogation panel
        // when switching between suspects. For now, the interrogation manager handles
        // most of the state switching, but this can be extended if needed.
        
        Debug.Log($"[InterrogationScreen] Updated interrogation UI for: {currentSuspect?.FullName}");
    }
    
    private void OnArrestClicked()
    {
        if (currentSuspect == null) return;
        
        // The UIManager is now the single point of entry for showing the commit UI.
        // It will automatically use the GameManager's current case.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnCommitButtonPressed();
        }
        else
        {
            Debug.LogError("[InterrogationScreen] UIManager not found! Cannot initiate arrest.");
        }
    }
    
    private void OnReleaseClicked()
    {
        if (currentSuspect == null) return;
        
        // Delegate to SuspectManager to release from single monitor
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager != null)
        {
            suspectManager.ReleaseSuspectFromMonitor();
        }
        else
        {
            Debug.LogError("[InterrogationScreen] SuspectManager not found!");
        }
    }
    
    private void OnViewFileClicked()
    {
        if (currentSuspect == null) return;
        
        Debug.Log($"[InterrogationScreen] Viewing file for {currentSuspect.FullName}");
        // TODO: Open detailed citizen file/dossier view
    }
    
    /// <summary>
    /// Clear the detailed view
    /// </summary>
    public void ClearView()
    {
        // End interrogation if active
        if (isInInterrogationMode)
        {
            EndInterrogation();
        }
        
        currentSuspect = null;
        currentAnimationSet = null;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        if (suspectImage != null)
            suspectImage.sprite = defaultInactiveSprite;

        if (suspectNameText != null)
            suspectNameText.text = "";

        if (suspectInfoText != null)
            suspectInfoText.text = "";

        UpdateButtonStates();
        UpdateDragTextVisibility();
    }

    /// <summary>
    /// Check if the view is available for a new suspect (no current suspect assigned)
    /// </summary>
    public bool IsAvailable()
    {
        return currentSuspect == null;
    }

    /// <summary>
    /// Get the current suspect assigned to this view
    /// </summary>
    public Citizen GetCurrentSuspect()
    {
        return currentSuspect;
    }

    /// <summary>
    /// Set whether a case is currently open (controls drag-here text visibility)
    /// </summary>
    public void SetCaseOpen(bool caseOpen)
    {
        isCaseOpen = caseOpen;
        UpdateDragTextVisibility();
    }

    /// <summary>
    /// Show an error message on the view with fade animation
    /// </summary>
    public void ShowErrorMessage(string message)
    {
        if (errorText == null) return;

        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
        }

        errorMessageCoroutine = StartCoroutine(DisplayErrorMessage(message));
    }

    private IEnumerator DisplayErrorMessage(string message)
    {
        if (errorText == null) yield break;

        errorText.text = message;

        // Fade in
        float startTime = Time.time;
        while (Time.time < startTime + errorFadeDuration)
        {
            float t = (Time.time - startTime) / errorFadeDuration;
            errorText.alpha = t;
            yield return null;
        }
        errorText.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(errorMessageDuration);

        // Fade out
        startTime = Time.time;
        while (Time.time < startTime + errorFadeDuration)
        {
            float t = (Time.time - startTime) / errorFadeDuration;
            errorText.alpha = 1f - t;
            yield return null;
        }
        errorText.alpha = 0f;

        errorMessageCoroutine = null;
    }

    /// <summary>
    /// Update drag text visibility based on case state and view availability
    /// </summary>
    private void UpdateDragTextVisibility()
    {
        if (dragHereText != null)
        {
            bool shouldShow = isCaseOpen && currentSuspect == null;
            dragHereText.gameObject.SetActive(shouldShow);
        }
    }
} 