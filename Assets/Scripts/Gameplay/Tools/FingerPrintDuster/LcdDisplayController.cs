using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Controls a retro LCD display for the FingerPrintDuster with scrolling text animations
/// </summary>
public class LcdDisplayController : MonoBehaviour
{
    [Header("LCD Display Settings")]
    [SerializeField] private TextMeshProUGUI lcdText;
    [SerializeField] private int characterLimit = 16; // Typical LCD character limit
    [SerializeField] private float scrollInSpeed = 1f; // Time between characters when scrolling in (seconds per character)
    [SerializeField] private float scrollOutSpeed = 0.5f; // Time between characters when scrolling out (seconds per character)
    [SerializeField] private float blinkSpeed = 0.5f; // Blinking cursor speed
    [SerializeField] private bool showCursor = true;
    
    [Header("Display Messages")]
    [SerializeField] private string loadEvidenceMessage = "LOAD EVIDENCE";
    [SerializeField] private string brushForPrintsMessage = "BRUSH FOR PRINTS";
    [SerializeField] private string scanSavedMessage = "SCAN SAVED TO DISC";
    [SerializeField] private string noPrintsFoundMessage = "NO PRINTS FOUND";
    [SerializeField] private string emptySquareChar = "□";
    [SerializeField] private string filledSquareChar = "■";
    [SerializeField] private int maxProgressSquares = 5;
    
    [Header("Animation Settings")]
    [SerializeField] private float messageDisplayDuration = 2f; // How long to show status messages
    [SerializeField] private bool enableScrollAnimation = true;
    
    // Internal state
    private string currentDisplayText = "";
    private string targetDisplayText = "";
    private bool isAnimating = false;
    private Coroutine currentAnimation;
    private Coroutine blinkCoroutine;
    private bool isScrollingIn = false;
    private bool isScrollingOut = false;
    
    // Display states
    public enum LcdState
    {
        LoadEvidence,
        BrushForPrints,
        ShowingProgress,
        ScanSaved,
        NoPrintsFound
    }
    
    private LcdState currentState = LcdState.NoPrintsFound; // Use a different default state
    private int currentProgress = 0; // 0-5 progress squares
    
    void Start()
    {
        
        if (lcdText == null)
        {
            Debug.LogError("[LcdDisplayController] No TextMeshProUGUI component assigned!");
            return;
        }
        
        
        // Initialize with load evidence message
        SetState(LcdState.LoadEvidence);
    }
    
    /// <summary>
    /// Set the LCD display state
    /// </summary>
    public void SetState(LcdState newState)
    {
        
        if (currentState == newState && !string.IsNullOrEmpty(currentDisplayText)) 
        {
            Debug.Log($"[LcdDisplayController] State unchanged and text already displayed, returning");
            return;
        }
        
        currentState = newState;
        
        string newMessage = GetMessageForState(newState);
        ShowMessage(newMessage);
    }
    
    /// <summary>
    /// Force stop any ongoing animations and show the target state with proper scrolling
    /// </summary>
    public void ForceSetState(LcdState newState)
    {
        
        // Stop any ongoing animation
        if (isAnimating && currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            isAnimating = false;
            isScrollingIn = false;
            isScrollingOut = false;
        }
        
        currentState = newState;
        
        string newMessage = GetMessageForState(newState);
        targetDisplayText = TruncateMessage(newMessage);
        
        // Start fresh scrolling animation
        if (enableScrollAnimation)
        {
            currentAnimation = StartCoroutine(ScrollMessageCoroutine());
        }
        else
        {
            // Instant display
            currentDisplayText = targetDisplayText;
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Update progress squares (0-5)
    /// </summary>
    public void UpdateProgress(int progress)
    {
        progress = Mathf.Clamp(progress, 0, maxProgressSquares);
        
        // If we're already showing progress and just updating squares, don't scroll
        if (currentState == LcdState.ShowingProgress && !isAnimating)
        {
            // Just update the squares in place without scrolling
            currentProgress = progress;
            string progressText = GenerateProgressText(progress);
            currentDisplayText = TruncateMessage(progressText);
            UpdateDisplay();
            return;
        }
        
        // If changing from a different state to progress, scroll the new text
        if (currentState != LcdState.ShowingProgress)
        {
            currentProgress = progress;
            currentState = LcdState.ShowingProgress;
            
            string progressText = GenerateProgressText(progress);
            ShowMessage(progressText);
        }
    }
    
    /// <summary>
    /// Show a temporary status message, then return to previous state
    /// </summary>
    public void ShowTemporaryMessage(LcdState messageState)
    {
        LcdState previousState = currentState;
        SetState(messageState);
        
        // Return to previous state after duration
        StartCoroutine(ReturnToPreviousStateCoroutine(previousState, messageDisplayDuration));
    }
    
    /// <summary>
    /// Get the appropriate message for a state
    /// </summary>
    private string GetMessageForState(LcdState state)
    {
        switch (state)
        {
            case LcdState.LoadEvidence:
                return loadEvidenceMessage;
            case LcdState.BrushForPrints:
                return brushForPrintsMessage;
            case LcdState.ShowingProgress:
                return GenerateProgressText(currentProgress);
            case LcdState.ScanSaved:
                return scanSavedMessage;
            case LcdState.NoPrintsFound:
                return noPrintsFoundMessage;
            default:
                return loadEvidenceMessage;
        }
    }
    
    /// <summary>
    /// Generate progress text with squares
    /// </summary>
    private string GenerateProgressText(int progress)
    {
        string progressText = "";
        
        for (int i = 0; i < maxProgressSquares; i++)
        {
            if (i < progress)
            {
                progressText += filledSquareChar;
            }
            else
            {
                progressText += emptySquareChar;
            }
        }
        
        return progressText;
    }
    
    /// <summary>
    /// Show a message with scrolling animation
    /// </summary>
    private void ShowMessage(string message)
    {
        
        string truncatedMessage = TruncateMessage(message);
        
        // If we're already showing this exact text, don't animate
        if (currentDisplayText == truncatedMessage && !isAnimating)
        {
            Debug.Log($"[LcdDisplayController] Already showing this text, returning");
            return;
        }
        
        targetDisplayText = truncatedMessage;
        
        // Stop any ongoing animation and reset state
        if (isAnimating && currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            isAnimating = false;
            isScrollingIn = false;
            isScrollingOut = false;
        }
        
        if (enableScrollAnimation)
        {
            currentAnimation = StartCoroutine(ScrollMessageCoroutine());
        }
        else
        {
            // Instant display
            currentDisplayText = truncatedMessage;
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Animate scrolling text from right to left
    /// </summary>
    private IEnumerator ScrollMessageCoroutine()
    {
        isAnimating = true;
        
        // If there's existing text, scroll it out to the left
        if (!string.IsNullOrEmpty(currentDisplayText))
        {
            yield return StartCoroutine(ScrollOutCoroutine());
            
            // Check if we were interrupted during scroll out
            if (!isAnimating)
            {
                yield break;
            }
        }
        
        // Scroll in the new message from the right
        yield return StartCoroutine(ScrollInCoroutine());
        
        isAnimating = false;
    }
    
    /// <summary>
    /// Scroll current text out to the left character by character
    /// </summary>
    private IEnumerator ScrollOutCoroutine()
    {
        isScrollingOut = true;
        string originalText = currentDisplayText;
        
        // Scroll out character by character
        for (int i = 1; i <= originalText.Length; i++)
        {
            // Check if we were interrupted
            if (!isAnimating)
            {
                isScrollingOut = false;
                yield break;
            }
            
            currentDisplayText = originalText.Substring(i);
            UpdateDisplay();
            yield return new WaitForSeconds(scrollOutSpeed);
        }
        
        currentDisplayText = "";
        UpdateDisplay();
        isScrollingOut = false;
    }
    
    /// <summary>
    /// Scroll new text in from the right character by character
    /// </summary>
    private IEnumerator ScrollInCoroutine()
    {
        isScrollingIn = true;
        currentDisplayText = "";
        
        // Scroll in character by character
        for (int i = 1; i <= targetDisplayText.Length; i++)
        {
            // Check if we were interrupted
            if (!isAnimating)
            {
                isScrollingIn = false;
                yield break;
            }
            
            currentDisplayText = targetDisplayText.Substring(0, i);
            UpdateDisplay();
            yield return new WaitForSeconds(scrollInSpeed);
        }
        
        isScrollingIn = false;
    }
    
    /// <summary>
    /// Truncate message to fit character limit
    /// </summary>
    private string TruncateMessage(string message)
    {
        if (message.Length <= characterLimit)
        {
            return message;
        }
        
        return message.Substring(0, characterLimit);
    }
    
    /// <summary>
    /// Update the display text with optional blinking cursor
    /// </summary>
    private void UpdateDisplay()
    {
        if (lcdText == null) 
        {
            Debug.LogError("[LcdDisplayController] lcdText is null!");
            return;
        }
        
        string displayText = currentDisplayText;
        
        // Add padding to maintain consistent width
        while (displayText.Length < characterLimit)
        {
            displayText += " ";
        }
        
        lcdText.text = displayText;
    }
    
    /// <summary>
    /// Start blinking cursor effect
    /// </summary>
    private void StartBlinkingCursor()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
        
        if (showCursor)
        {
            blinkCoroutine = StartCoroutine(BlinkCursorCoroutine());
        }
    }
    
    /// <summary>
    /// Blinking cursor animation
    /// </summary>
    private IEnumerator BlinkCursorCoroutine()
    {
        bool showingCursor = true;
        
        while (true)
        {
            if (!isAnimating && lcdText != null)
            {
                string baseText = currentDisplayText;
                while (baseText.Length < characterLimit - 1)
                {
                    baseText += " ";
                }
                
                if (showingCursor)
                {
                    lcdText.text = baseText + "_";
                }
                else
                {
                    lcdText.text = baseText + " ";
                }
                
                showingCursor = !showingCursor;
            }
            
            yield return new WaitForSeconds(blinkSpeed);
        }
    }
    
    /// <summary>
    /// Return to previous state after showing temporary message
    /// </summary>
    private IEnumerator ReturnToPreviousStateCoroutine(LcdState previousState, float delay)
    {
        yield return new WaitForSeconds(delay);
        SetState(previousState);
    }
    
    /// <summary>
    /// Clear the display
    /// </summary>
    public void ClearDisplay()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentDisplayText = "";
        UpdateDisplay();
        isAnimating = false;
    }
    
    void OnEnable()
    {
        StartBlinkingCursor();
    }
    
    void OnDisable()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }
    
    #region Public Configuration Methods
    
    public void SetScrollInSpeed(float speed)
    {
        scrollInSpeed = Mathf.Max(0.01f, speed);
    }
    
    public void SetScrollOutSpeed(float speed)
    {
        scrollOutSpeed = Mathf.Max(0.01f, speed);
    }
    
    public void SetCharacterLimit(int limit)
    {
        characterLimit = Mathf.Max(1, limit);
    }
    
    public void SetBlinkSpeed(float speed)
    {
        blinkSpeed = Mathf.Max(0.1f, speed);
    }
    
    public void SetMessages(string loadEvidence, string brushForPrints, string scanSaved, string noPrintsFound)
    {
        loadEvidenceMessage = loadEvidence;
        brushForPrintsMessage = brushForPrints;
        scanSavedMessage = scanSaved;
        noPrintsFoundMessage = noPrintsFound;
    }
    
    #endregion
}