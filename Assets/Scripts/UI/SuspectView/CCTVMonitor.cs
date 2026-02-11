using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CCTV Monitor display - shows suspect animations (no longer clickable)
/// Channel selection is now handled by numbered buttons in SuspectManager
/// </summary>
public class CCTVMonitor : MonoBehaviour
{
    [Header("Monitor Components")]
    public Image monitorImage; // The main display image
    public Image backgroundFrame; // Optional monitor frame/border
    public TextMeshProUGUI suspectNameLabel; // Optional name display
    public TextMeshProUGUI dragHereText; // Text to show when monitor is empty
    public TextMeshProUGUI errorText; // Text to show error messages
    
    [Header("Visual Settings")]
    public Sprite defaultInactiveSprite; // Default image when no suspect is assigned
    public Color activeBorderColor = Color.green;
    public Color inactiveBorderColor = Color.gray;
    public float pixelationLevel = 4f; // How pixelated the silhouette should be
    
    [Header("Error Message Settings")]
    public float errorMessageDuration = 2.0f;
    public float errorFadeDuration = 0.5f;
    
    // Current state
    private Citizen currentSuspect;
    private SuspectAnimationSet currentAnimationSet;
    private SuspectAnimationState currentAnimationState = SuspectAnimationState.Idle;
    private int monitorIndex;
    private bool isActive = false;
    private bool isCaseOpen = false; // Track if a case is currently open
    
    // Animation
    private Coroutine animationCoroutine;
    private int currentFrame = 0;
    
    // Error message
    private Coroutine errorMessageCoroutine;
    
    // Public property
    public SuspectAnimationState CurrentAnimationState => currentAnimationState;
    
    public void Initialize(int index)
    {
        monitorIndex = index;
        
        // Ensure we have required components
        if (monitorImage == null)
            monitorImage = GetComponent<Image>();
            
        // Set initial state
        SetActive(false);
        ClearMonitor();
        
        // Ensure error text starts invisible
        if (errorText != null)
        {
            errorText.alpha = 0f;
        }
        
        // Initially hide drag text (no case open)
        if (dragHereText != null)
        {
            dragHereText.gameObject.SetActive(false);
        }
    }
    
    public void SetSuspect(Citizen suspect, SuspectAnimationSet animationSet)
    {
        currentSuspect = suspect;
        currentAnimationSet = animationSet;
        
        // Update name label if available
        if (suspectNameLabel != null)
        {
            suspectNameLabel.text = suspect != null ? suspect.FullName : "";
        }
        
        SetActive(suspect != null);
        
        // Update drag text visibility
        UpdateDragTextVisibility();
        
        // Start with idle animation
        if (suspect != null)
        {
            SetAnimationState(SuspectAnimationState.Idle);
        }
    }
    
    public void SetAnimationState(SuspectAnimationState state)
    {
        currentAnimationState = state;
        
        if (currentAnimationSet == null || !isActive)
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
            animationCoroutine = StartCoroutine(PlaySpriteSheetAnimation(spriteSheet));
        }
        else
        {
            Debug.LogWarning($"[CCTVMonitor] No sprite sheet found for state: {state}");
        }
    }
    
    private IEnumerator PlaySpriteSheetAnimation(Texture2D spriteSheet)
    {
        if (spriteSheet == null || currentAnimationSet == null) yield break;
        
        float frameTime = 1f / SuspectManager.Instance.animationFrameRate;
        currentFrame = 0;
        
        while (isActive && currentSuspect != null)
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
            
            // Apply to monitor image
            if (monitorImage != null)
            {
                monitorImage.sprite = frameSprite;
                
                // Apply pixelation effect by adjusting filter mode
                monitorImage.sprite.texture.filterMode = FilterMode.Point;
            }
            
            // Move to next frame
            currentFrame = (currentFrame + 1) % totalFrames;
            
            yield return new WaitForSeconds(frameTime);
        }
    }
    
    public void SetActive(bool active)
    {
        isActive = active;
        
        // Update visual state
        if (backgroundFrame != null)
        {
            backgroundFrame.color = active ? activeBorderColor : inactiveBorderColor;
        }
        
        // Set appropriate display image
        if (!active && monitorImage != null)
        {
            // Show default inactive image instead of null
            monitorImage.sprite = defaultInactiveSprite;
        }
        
        // Update drag text visibility
        UpdateDragTextVisibility();
        
        // Stop animation if inactive
        if (!active && animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
    
    public void ClearMonitor()
    {
        currentSuspect = null;
        currentAnimationSet = null;
        currentAnimationState = SuspectAnimationState.Idle;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        
        if (monitorImage != null)
            monitorImage.sprite = defaultInactiveSprite; // Use default image instead of null
            
        if (suspectNameLabel != null)
            suspectNameLabel.text = "";
        
        SetActive(false);
        
        // Update drag text visibility after clearing
        UpdateDragTextVisibility();
    }
    
    /// <summary>
    /// Show an error message on the monitor
    /// </summary>
    public void ShowErrorMessage(string message)
    {
        if (errorText == null) return;
        
        // Stop any existing error message
        if (errorMessageCoroutine != null)
        {
            StopCoroutine(errorMessageCoroutine);
        }
        
        errorMessageCoroutine = StartCoroutine(DisplayErrorMessage(message));
    }
    
    /// <summary>
    /// Display error message with fade in/out animation
    /// </summary>
    private System.Collections.IEnumerator DisplayErrorMessage(string message)
    {
        if (errorText == null) yield break;
        
        // Set the message
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
        
        // Wait for duration
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
    /// Check if monitor is available for new suspect assignment
    /// </summary>
    public bool IsAvailable()
    {
        return currentSuspect == null;
    }
    
    /// <summary>
    /// Get the current suspect assigned to this monitor
    /// </summary>
    public Citizen GetCurrentSuspect()
    {
        return currentSuspect;
    }
    
    /// <summary>
    /// Set whether a case is currently open
    /// </summary>
    public void SetCaseOpen(bool caseOpen)
    {
        isCaseOpen = caseOpen;
        UpdateDragTextVisibility();
    }
    
    /// <summary>
    /// Update drag text visibility based on case state and monitor availability
    /// </summary>
    private void UpdateDragTextVisibility()
    {
        if (dragHereText != null)
        {
            // Only show drag text when case is open AND monitor is empty
            bool shouldShow = isCaseOpen && currentSuspect == null;
            dragHereText.gameObject.SetActive(shouldShow);
        }
    }
} 