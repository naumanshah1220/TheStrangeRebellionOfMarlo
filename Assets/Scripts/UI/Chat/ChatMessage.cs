using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// Chat message component with animations for WhatsApp-like messaging interface
/// </summary>
public class ChatMessage : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    public TextMeshProUGUI messageText;
    public Image bubbleBackground;
    public CanvasGroup canvasGroup;
    
    [Header("Clickable Clue Prefabs")]
    [Tooltip("Prefab for clickable clue frames - should have Image background and TextMeshPro text")]
    public GameObject clickableClueFramePrefab;
    
    [Header("Animation Settings")]
    public float popInDuration = 0.3f;
    public Ease popInEase = Ease.OutBack;
    
    [Header("Styling")]
    public Color playerBubbleColor = new Color(0.2f, 0.6f, 1f, 1f);
    public Color suspectBubbleColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    
    private string fullText = "";
    private bool isAnimating = false;
    private Coroutine typewriterCoroutine;
    private string[] extractableClues;
    private ClickableClueSegment[] clickableClues;
    private Dictionary<string, bool> activatedClues = new Dictionary<string, bool>();
    private List<ClueFrameInfo> instantiatedClueFrames = new List<ClueFrameInfo>(); // Track created prefabs
    
    public System.Action OnAnimationComplete;

// Settings - get from InterrogationManager instead of having duplicates
private float TypewriterSpeed => InterrogationManager.Instance != null ? InterrogationManager.Instance.typewriterSpeed : 0.05f; // Default fallback

[System.Serializable]
public class ClueFrameInfo
{
    public string clueId;
    public GameObject frameInstance;
    public ClickableClueSegment clueSegment;
    
    public ClueFrameInfo(string id, GameObject instance, ClickableClueSegment segment)
    {
        clueId = id;
        frameInstance = instance;
        clueSegment = segment;
    }
}
    
    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Start invisible
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
    }
    
    /// <summary>
    /// Setup and animate the message
    /// </summary>
    public void SetupMessage(string text, bool isPlayerMessage, bool useTypewriter = true)
    {
        fullText = text;
        
        // Find and setup the background - be more aggressive about finding it
        SetupBackground(isPlayerMessage);
        
        // Setup MessageBubble if it exists (for width constraints and layout)
        MessageBubble messageBubble = GetComponent<MessageBubble>();
        if (messageBubble != null)
        {
            messageBubble.SetupMessage(text, isPlayerMessage);
        }
        
        // Start pop-in animation
        StartCoroutine(AnimateMessage(useTypewriter));
    }
    
    /// <summary>
    /// Setup the background image with proper color and visibility
    /// </summary>
    private void SetupBackground(bool isPlayerMessage)
    {
        Color bubbleColor = isPlayerMessage ? playerBubbleColor : suspectBubbleColor;
        
        // Try multiple ways to find the background Image
        Image backgroundImage = bubbleBackground;
        
        if (backgroundImage == null)
        {
            // Try to find by name "MessageBubble"
            Transform messageBubbleTransform = transform.Find("MessageBubble");
            if (messageBubbleTransform != null)
            {
                backgroundImage = messageBubbleTransform.GetComponent<Image>();
            }
        }
        
        if (backgroundImage == null)
        {
            // Try to find any Image component in children
            backgroundImage = GetComponentInChildren<Image>();
        }
        
        if (backgroundImage == null)
        {
            // Last resort - find by component type in this GameObject
            backgroundImage = GetComponent<Image>();
        }
        
        if (backgroundImage != null)
        {
            Debug.Log($"[ChatMessage] Setting bubble color to {bubbleColor} for {(isPlayerMessage ? "player" : "suspect")} message");
            Debug.Log($"[ChatMessage] Background Image object: {backgroundImage.gameObject.name}, enabled: {backgroundImage.enabled}");
            
            backgroundImage.color = bubbleColor;
            backgroundImage.enabled = true;
            backgroundImage.gameObject.SetActive(true);
            
            // Force a sprite if none exists
            if (backgroundImage.sprite == null)
            {
                // Use Unity's built-in UI sprite
                backgroundImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
                Debug.Log("[ChatMessage] Applied fallback sprite");
            }
            
            Debug.Log($"[ChatMessage] Background setup complete - Final color: {backgroundImage.color}, Sprite: {backgroundImage.sprite?.name}, Active: {backgroundImage.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError("[ChatMessage] Could not find any Image component for background!");
        }
    }
    
    /// <summary>
    /// Setup message with extractable clues
    /// </summary>
    public void SetupMessageWithClues(string text, bool isPlayerMessage, string[] clues, bool useTypewriter = true)
    {
        Debug.Log($"[ChatMessage] SetupMessageWithClues called - isPlayerMessage: {isPlayerMessage}, text: '{text}'");
        
        fullText = text;
        extractableClues = clues;
        
        // Set bubble color and ensure it's visible
        if (bubbleBackground != null)
        {
            Color bubbleColor = isPlayerMessage ? playerBubbleColor : suspectBubbleColor;
            bubbleBackground.color = bubbleColor;
            bubbleBackground.gameObject.SetActive(true);
            bubbleBackground.enabled = true;
            Debug.Log($"[ChatMessage] Set bubble color to {bubbleColor} for {(isPlayerMessage ? "player" : "suspect")} message with clues");
        }
        else
        {
            Debug.LogWarning("[ChatMessage] bubbleBackground is null in SetupMessageWithClues!");
            bubbleBackground = GetComponentInChildren<Image>();
            if (bubbleBackground != null)
            {
                Color bubbleColor = isPlayerMessage ? playerBubbleColor : suspectBubbleColor;
                bubbleBackground.color = bubbleColor;
                bubbleBackground.gameObject.SetActive(true);
                bubbleBackground.enabled = true;
            }
        }
        
        // Setup MessageBubble if it exists (for width constraints and layout)
        MessageBubble messageBubble = GetComponent<MessageBubble>();
        if (messageBubble != null)
        {
            messageBubble.SetupMessage(text, isPlayerMessage);
        }
        
        // Start pop-in animation
        StartCoroutine(AnimateMessage(useTypewriter));
    }
    
    /// <summary>
    /// Setup message with clickable clue segments (new enhanced version)
    /// </summary>
    public void SetupMessageWithClickableClues(string text, bool isPlayerMessage, ClickableClueSegment[] clueSegments, bool useTypewriter = true)
    {
        Debug.Log($"[ChatMessage] SetupMessageWithClickableClues called - isPlayerMessage: {isPlayerMessage}, text: '{text}'");
        
        clickableClues = clueSegments;
        
        // Process clickable clues and remove tags BEFORE animation
        string cleanText = ProcessClickableClueText(text);
        fullText = cleanText;
        
        // Setup background
        SetupBackground(isPlayerMessage);
        
        // Setup MessageBubble if it exists (for width constraints and layout)
        MessageBubble messageBubble = GetComponent<MessageBubble>();
        if (messageBubble != null)
        {
            messageBubble.SetupMessage(cleanText, isPlayerMessage);
        }
        
        // Start pop-in animation
        StartCoroutine(AnimateMessage(useTypewriter));
    }
    
    /// <summary>
    /// Setup message with both extractable clues and clickable clue segments
    /// </summary>
    public void SetupMessageWithAllClues(string text, bool isPlayerMessage, string[] extractableClues, ClickableClueSegment[] clueSegments, bool useTypewriter = true)
    {
        this.extractableClues = extractableClues;
        this.clickableClues = clueSegments;
        
        // Process clickable clues and remove tags BEFORE animation
        string cleanText = ProcessClickableClueText(text);
        fullText = cleanText;
        
        // Set bubble color
        if (bubbleBackground != null)
        {
            bubbleBackground.color = isPlayerMessage ? playerBubbleColor : suspectBubbleColor;
        }
        
        // Setup MessageBubble if it exists (for width constraints and layout)
        MessageBubble messageBubble = GetComponent<MessageBubble>();
        if (messageBubble != null)
        {
            messageBubble.SetupMessage(cleanText, isPlayerMessage);
        }
        
        // Start pop-in animation
        StartCoroutine(AnimateMessage(useTypewriter));
    }
    
    /// <summary>
    /// Animate message appearance and text
    /// </summary>
    private IEnumerator AnimateMessage(bool useTypewriter)
    {
        isAnimating = true;
        
        // Pop-in animation - add null checks
        if (canvasGroup != null && transform != null)
        {
            var popSequence = DOTween.Sequence();
            popSequence.Append(canvasGroup.DOFade(1f, popInDuration));
            popSequence.Join(transform.DOScale(Vector3.one, popInDuration).SetEase(popInEase));
            
            yield return popSequence.WaitForCompletion();
        }
        
        // Typewriter effect
        if (useTypewriter && messageText != null)
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect());
            yield return typewriterCoroutine;
        }
        else if (messageText != null)
        {
            messageText.text = fullText;
        }
        
        // Process extractable clues and clickable clue segments after text animation completes
        ProcessExtractableClues();
        ProcessClickableClues();
        
        isAnimating = false;
        OnAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Typewriter text animation effect
    /// </summary>
    private IEnumerator TypewriterEffect()
    {
        messageText.text = "";
        
        for (int i = 0; i <= fullText.Length; i++)
        {
            messageText.text = fullText.Substring(0, i);
            yield return new WaitForSeconds(TypewriterSpeed);
        }
        
        typewriterCoroutine = null;
    }
    
    /// <summary>
    /// Update message text (for thinking dots animation)
    /// </summary>
    public void UpdateMessageText(string text, bool animate = false)
    {
        if (animate && messageText != null)
        {
            // Stop current typewriter if running
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            
            fullText = text;
            isAnimating = true; // Reset animation state
            typewriterCoroutine = StartCoroutine(TypewriterEffectWithCallback());
        }
        else if (messageText != null)
        {
            messageText.text = text;
        }
    }
    
    /// <summary>
    /// Typewriter effect with proper callback handling
    /// </summary>
    private IEnumerator TypewriterEffectWithCallback()
    {
        yield return StartCoroutine(TypewriterEffect());
        isAnimating = false;
        OnAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Set message color
    /// </summary>
    public void SetMessageColor(Color color)
    {
        if (messageText != null)
        {
            messageText.color = color;
        }
    }
    
    /// <summary>
    /// Process clickable clue text and remove tags, storing position information
    /// </summary>
    private string ProcessClickableClueText(string originalText)
    {
        if (clickableClues == null || clickableClues.Length == 0)
            return originalText;

        string cleanText = originalText;
        
        // First pass: Find all tagged clues and store their original positions
        List<(ClickableClueSegment segment, int originalPos)> cluePositions = new List<(ClickableClueSegment, int)>();
        
        foreach (var clueSegment in clickableClues)
        {
            if (string.IsNullOrEmpty(clueSegment.clickableText))
                continue;

            // Look for the clue wrapped in tags: <clue>word</clue>
            string taggedClue = $"<clue>{clueSegment.clickableText}</clue>";
            int taggedIndex = originalText.IndexOf(taggedClue, System.StringComparison.OrdinalIgnoreCase);
            
            if (taggedIndex != -1)
            {
                // Store the position where the word will be after removing tags
                cluePositions.Add((clueSegment, taggedIndex));
                Debug.Log($"[ChatMessage] Found tagged clue '{clueSegment.clickableText}' at original position {taggedIndex}");
            }
            else
            {
                // Fallback: look for the clue text without tags
                int clueIndex = originalText.IndexOf(clueSegment.clickableText, System.StringComparison.OrdinalIgnoreCase);
                if (clueIndex != -1)
                {
                    cluePositions.Add((clueSegment, clueIndex));
                    Debug.Log($"[ChatMessage] Found untagged clue '{clueSegment.clickableText}' at position {clueIndex}");
                }
            }
        }
        
        // Second pass: Remove all clue tags from the text and add spacing
        foreach (var clueSegment in clickableClues)
        {
            if (string.IsNullOrEmpty(clueSegment.clickableText))
                continue;
                
            string taggedClue = $"<clue>{clueSegment.clickableText}</clue>";
            string spacedClue = $" {clueSegment.clickableText} "; // Add spaces around the word
            cleanText = cleanText.Replace(taggedClue, spacedClue);
        }
        
        // Third pass: Calculate final positions in the clean text and store them
        foreach (var (clueSegment, originalPos) in cluePositions)
        {
            // Calculate how many tag characters were removed before this clue's position
            int charactersRemovedBefore = 0;
            string textBeforeClue = originalText.Substring(0, originalPos);
            
            // Count all <clue> and </clue> tags that appear before this position
            charactersRemovedBefore += CountOccurrences(textBeforeClue, "<clue>") * "<clue>".Length;
            charactersRemovedBefore += CountOccurrences(textBeforeClue, "</clue>") * "</clue>".Length;
            
            // Calculate the final position in clean text
            // Add 1 to account for the leading space we added around the clue
            int finalPosition = originalPos - charactersRemovedBefore + 1;
            
            clueSegment.frameStartIndex = finalPosition;
            clueSegment.actualTextLength = clueSegment.clickableText.Length;
            
            Debug.Log($"[ChatMessage] Clue '{clueSegment.clickableText}': original pos {originalPos}, removed {charactersRemovedBefore} chars, final pos {finalPosition}");
        }
        
        return cleanText;
    }

    /// <summary>
    /// Process extractable clues and make them clickable
    /// </summary>
    private void ProcessExtractableClues()
    {
        if (extractableClues == null || extractableClues.Length == 0 || messageText == null)
            return;
        
        // Use the simple highlighting approach for now
        ClueProcessor clueProcessor = FindFirstObjectByType<ClueProcessor>();
        if (clueProcessor != null)
        {
            clueProcessor.HighlightCluesInText(messageText, extractableClues);
        }
        else
        {
            // Fallback: simple text highlighting without ClueProcessor
            HighlightCluesDirectly();
        }
    }
    
    /// <summary>
    /// Fallback method to highlight clues directly
    /// </summary>
    private void HighlightCluesDirectly()
    {
        string text = messageText.text;
        Color clueColor = Color.yellow;
        
        foreach (string clue in extractableClues)
        {
            if (string.IsNullOrEmpty(clue)) continue;
            
            // Simple highlighting with rich text
            string colorHex = ColorUtility.ToHtmlStringRGB(clueColor);
            string highlightedClue = $"<color=#{colorHex}><u>{clue}</u></color>";
            
            // Replace the clue with highlighted version (case-insensitive)
            text = System.Text.RegularExpressions.Regex.Replace(text, 
                System.Text.RegularExpressions.Regex.Escape(clue), 
                highlightedClue, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        
        messageText.text = text;
    }
    
    /// <summary>
    /// Process clickable clue segments and create interactive visual frames
    /// (Text processing and tag removal is done earlier in ProcessClickableClueText)
    /// </summary>
    private void ProcessClickableClues()
    {
        if (clickableClues == null || clickableClues.Length == 0 || messageText == null)
            return;

        // Clear any existing clue frames
        ClearClueFrames();
        
        // The text has already been processed and cleaned, just create frames
        StartCoroutine(CreateClueFramesAfterLayout());
    }
    
    /// <summary>
    /// Create visual frames for clues after the text layout is updated
    /// </summary>
    private IEnumerator CreateClueFramesAfterLayout()
    {
        // Wait multiple frames for text layout and ContentSizeFitter to update completely
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();
        
        if (clickableClueFramePrefab == null)
        {
            Debug.LogWarning("[ChatMessage] No clickable clue frame prefab assigned!");
            yield break;
        }

        // Force layout rebuild before positioning frames
        if (messageText != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
        }
        
        string currentText = messageText.text; // Text with spacing
        
        foreach (var clueSegment in clickableClues)
        {
            if (string.IsNullOrEmpty(clueSegment.clickableText) || clueSegment.frameStartIndex == -1)
                continue;

            // Create the frame prefab (will be reparented in SetupClueFrame)
            GameObject frameInstance = Instantiate(clickableClueFramePrefab);
            
            // Setup the frame using the stored frame start index
            SetupClueFrame(frameInstance, clueSegment, clueSegment.frameStartIndex);
            
            // Store frame info
            ClueFrameInfo frameInfo = new ClueFrameInfo(clueSegment.clueId, frameInstance, clueSegment);
            instantiatedClueFrames.Add(frameInfo);
        }
        
        Debug.Log($"[ChatMessage] Created {instantiatedClueFrames.Count} clue frames");
    }
    
    /// <summary>
    /// Setup a clue frame prefab with positioning and basic functionality
    /// </summary>
    private void SetupClueFrame(GameObject frameInstance, ClickableClueSegment clueSegment, int textIndex)
    {
        // Find the TextMeshPro component in the frame and set text content
        TextMeshProUGUI frameText = frameInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (frameText != null)
        {
            frameText.text = clueSegment.clickableText;
            
            // Copy font settings from the main message text for consistency
            if (messageText != null)
            {
                frameText.font = messageText.font;
                frameText.fontSharedMaterial = messageText.fontSharedMaterial;
                frameText.fontSize = messageText.fontSize;
                frameText.fontStyle = messageText.fontStyle;
                frameText.characterSpacing = messageText.characterSpacing;
                frameText.wordSpacing = messageText.wordSpacing;
                frameText.lineSpacing = messageText.lineSpacing;
            }
            
            // Disable the text's raycast target so clicks go through to the frame
            frameText.raycastTarget = false;
        }
        
        // Remove ContentSizeFitter from frame to allow manual sizing
        ContentSizeFitter frameSizeFitter = frameInstance.GetComponent<ContentSizeFitter>();
        if (frameSizeFitter != null)
        {
            DestroyImmediate(frameSizeFitter);
            Debug.Log("[ChatMessage] Removed ContentSizeFitter from clue frame for manual positioning");
        }
        
        // Position the frame over the text word using precise positioning
        RectTransform frameRect = frameInstance.GetComponent<RectTransform>();
        if (frameRect != null)
        {
            // Make sure the frame ignores layout groups (so it doesn't interfere with HorizontalLayoutGroup)
            LayoutElement layoutElement = frameInstance.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = frameInstance.AddComponent<LayoutElement>();
            }
            layoutElement.ignoreLayout = true;
            
            // Get word position and size in TextMeshPro local space
            Vector2 wordPositionTMP = GetPreciseWordPosition(textIndex, clueSegment.actualTextLength);
            Vector2 wordSize = GetWordSize(textIndex, clueSegment.actualTextLength);
            
            // Set frame as child of messageText for direct positioning
            frameRect.SetParent(messageText.transform, false);
            
            // Position directly in TextMeshPro local space (much simpler and more reliable)
            frameRect.anchoredPosition = wordPositionTMP;
            
            // Set frame size to match word bounds with extra padding due to added spaces
            float padding = 8f; // Increased padding to take advantage of the spacing
            frameRect.sizeDelta = new Vector2(wordSize.x + padding, wordSize.y + padding);
            
            // Make sure the frame renders on top of the text
            frameInstance.transform.SetAsLastSibling();
            
            Debug.Log($"[ChatMessage] Frame for '{clueSegment.clickableText}' positioned at {frameRect.anchoredPosition} with size {frameRect.sizeDelta}");
            Debug.Log($"[ChatMessage] Word bounds: pos={wordPositionTMP}, size={wordSize}");
        }
        
        // Add click handler to the frame
        Button frameButton = frameInstance.GetComponent<Button>();
        if (frameButton == null)
        {
            frameButton = frameInstance.AddComponent<Button>();
        }
        
        // Setup click event
        frameButton.onClick.RemoveAllListeners();
        frameButton.onClick.AddListener(() => OnClueFrameClicked(clueSegment));
        
        // Add CanvasGroup for fade effects if it doesn't exist
        CanvasGroup frameCanvasGroup = frameInstance.GetComponent<CanvasGroup>();
        if (frameCanvasGroup == null)
        {
            frameCanvasGroup = frameInstance.AddComponent<CanvasGroup>();
        }
        
        // Fade in the frame
        frameCanvasGroup.alpha = 0f;
        frameCanvasGroup.DOFade(1f, 0.3f);
    }
    
    /// <summary>
    /// Get the position of text at a specific character index
    /// </summary>
    private Vector2 GetTextPositionAtIndex(int characterIndex)
    {
        if (messageText == null || characterIndex < 0 || characterIndex >= messageText.text.Length)
        {
            Debug.LogWarning($"[ChatMessage] Invalid character index: {characterIndex}, text length: {(messageText?.text?.Length ?? 0)}");
            return Vector2.zero;
        }
            
        // Force text mesh to update
        messageText.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = messageText.textInfo;
        if (characterIndex >= textInfo.characterCount)
        {
            Debug.LogWarning($"[ChatMessage] Character index {characterIndex} exceeds character count: {textInfo.characterCount}");
            return Vector2.zero;
        }
            
        TMP_CharacterInfo charInfo = textInfo.characterInfo[characterIndex];
        
        // Calculate the center position of the character for better alignment
        Vector2 position = new Vector2(
            (charInfo.bottomLeft.x + charInfo.topRight.x) * 0.5f,  // Center X
            (charInfo.bottomLeft.y + charInfo.topRight.y) * 0.5f   // Center Y
        );
        
        Debug.Log($"[ChatMessage] Character '{messageText.text[characterIndex]}' at index {characterIndex} positioned at: {position}");
        
        return position;
    }
    
    /// <summary>
    /// Get the precise position of a word (center point) - using direct TextMeshPro coordinates
    /// </summary>
    private Vector2 GetPreciseWordPosition(int startIndex, int wordLength)
    {
        if (messageText == null || startIndex < 0 || startIndex + wordLength > messageText.text.Length)
        {
            Debug.LogWarning($"[ChatMessage] Invalid word position: startIndex={startIndex}, wordLength={wordLength}, text length={messageText?.text?.Length ?? 0}");
            return Vector2.zero;
        }
        
        // Force text mesh to update
        messageText.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = messageText.textInfo;
        if (startIndex >= textInfo.characterCount || startIndex + wordLength > textInfo.characterCount)
        {
            Debug.LogWarning($"[ChatMessage] Word position exceeds character count: startIndex={startIndex}, wordLength={wordLength}, characterCount={textInfo.characterCount}");
            return Vector2.zero;
        }
        
        // Calculate word bounds in TextMeshPro local space
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float maxAscender = -Mathf.Infinity;
        float minDescender = Mathf.Infinity;
        
        bool hasValidChar = false;
        
        // Iterate through each character of the word
        for (int i = 0; i < wordLength; i++)
        {
            int characterIndex = startIndex + i;
            if (characterIndex >= textInfo.characterCount) break;
            
            var charInfo = textInfo.characterInfo[characterIndex];
            if (!charInfo.isVisible) continue;
            
            // Track bounds in TextMeshPro local coordinates
            minX = Mathf.Min(minX, charInfo.bottomLeft.x);
            maxX = Mathf.Max(maxX, charInfo.topRight.x);
            maxAscender = Mathf.Max(maxAscender, charInfo.ascender);
            minDescender = Mathf.Min(minDescender, charInfo.descender);
            
            hasValidChar = true;
        }
        
        if (hasValidChar)
        {
            // Calculate center position in TextMeshPro local space
            Vector2 wordCenter = new Vector2(
                (minX + maxX) * 0.5f,
                (minDescender + maxAscender) * 0.5f
            );
            
            Debug.Log($"[ChatMessage] Word '{messageText.text.Substring(startIndex, wordLength)}' positioned at: {wordCenter} (TMP local space)");
            
            return wordCenter;
        }
        
        // Fallback to first character position
        return GetTextPositionAtIndex(startIndex);
    }
    
    /// <summary>
    /// Get the size of a word in TextMeshPro units
    /// </summary>
    private Vector2 GetWordSize(int startIndex, int wordLength)
    {
        if (messageText == null || startIndex < 0 || startIndex + wordLength > messageText.text.Length)
        {
            Debug.LogWarning($"[ChatMessage] Invalid word size: startIndex={startIndex}, wordLength={wordLength}, text length={messageText?.text?.Length ?? 0}");
            return Vector2.zero;
        }
        
        // Force text mesh to update
        messageText.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = messageText.textInfo;
        if (startIndex >= textInfo.characterCount || startIndex + wordLength > textInfo.characterCount)
        {
            Debug.LogWarning($"[ChatMessage] Word size exceeds character count: startIndex={startIndex}, wordLength={wordLength}, characterCount={textInfo.characterCount}");
            return Vector2.zero;
        }
        
        // Calculate word bounds in TextMeshPro local space
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float maxAscender = -Mathf.Infinity;
        float minDescender = Mathf.Infinity;
        
        bool hasValidChar = false;
        
        // Iterate through each character of the word
        for (int i = 0; i < wordLength; i++)
        {
            int characterIndex = startIndex + i;
            if (characterIndex >= textInfo.characterCount) break;
            
            var charInfo = textInfo.characterInfo[characterIndex];
            if (!charInfo.isVisible) continue;
            
            // Track bounds in TextMeshPro local coordinates
            minX = Mathf.Min(minX, charInfo.bottomLeft.x);
            maxX = Mathf.Max(maxX, charInfo.topRight.x);
            maxAscender = Mathf.Max(maxAscender, charInfo.ascender);
            minDescender = Mathf.Min(minDescender, charInfo.descender);
            
            hasValidChar = true;
        }
        
        if (hasValidChar)
        {
            Vector2 size = new Vector2(maxX - minX, maxAscender - minDescender);
            Debug.Log($"[ChatMessage] Word '{messageText.text.Substring(startIndex, wordLength)}' size: {size}");
            return size;
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// Handle clicks on clue frames
    /// </summary>
    private void OnClueFrameClicked(ClickableClueSegment clueSegment)
    {
        Debug.Log($"[ChatMessage] Clue frame clicked: {clueSegment.clueId}");
        
        // Check if this clue can only be activated once
        if (clueSegment.oneTimeOnly && activatedClues.ContainsKey(clueSegment.clueId))
        {
            Debug.Log($"[ChatMessage] Clue {clueSegment.clueId} already activated");
            return;
        }
        
        // Activate the clue
        ActivateClueSegment(clueSegment);
        
        // Mark as activated if it's one-time only
        if (clueSegment.oneTimeOnly)
        {
            activatedClues[clueSegment.clueId] = true;
            
            // Optionally dim or disable the frame to show it's been used
            DisableClueFrame(clueSegment.clueId);
        }
    }
    
    /// <summary>
    /// Disable a clue frame after it's been activated (one-time only clues)
    /// </summary>
    private void DisableClueFrame(string clueId)
    {
        ClueFrameInfo frameInfo = instantiatedClueFrames.Find(f => f.clueId == clueId);
        if (frameInfo != null && frameInfo.frameInstance != null)
        {
            Button frameButton = frameInfo.frameInstance.GetComponent<Button>();
            if (frameButton != null)
            {
                frameButton.interactable = false;
            }
            
            CanvasGroup frameCanvasGroup = frameInfo.frameInstance.GetComponent<CanvasGroup>();
            if (frameCanvasGroup != null)
            {
                frameCanvasGroup.DOFade(0.5f, 0.3f); // Dim the frame
            }
        }
    }
    
    /// <summary>
    /// Count occurrences of a substring in a string
    /// </summary>
    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, System.StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
    
    /// <summary>
    /// Clear all instantiated clue frames
    /// </summary>
    private void ClearClueFrames()
    {
        foreach (var frameInfo in instantiatedClueFrames)
        {
            if (frameInfo.frameInstance != null)
            {
                DOTween.Kill(frameInfo.frameInstance);
                Destroy(frameInfo.frameInstance);
            }
        }
        instantiatedClueFrames.Clear();
    }
    
    /// <summary>
    /// Handle pointer clicks to detect link interactions
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (messageText == null || clickableClues == null || clickableClues.Length == 0)
            return;
        
        // Get the camera for the canvas
        Camera camera = null;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera;
        }
        
        // Check if the click intersects with any links
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(messageText, eventData.position, camera);
        
        if (linkIndex != -1)
        {
            // Get the link info
            TMP_LinkInfo linkInfo = messageText.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            
            Debug.Log($"[ChatMessage] Link clicked: {linkId}");
            OnLinkClicked(linkId, linkInfo.GetLinkText(), linkIndex);
        }
    }
    
    /// <summary>
    /// Handle link clicks for clickable clue segments
    /// </summary>
    private void OnLinkClicked(string linkId, string linkText, int linkIndex)
    {
        Debug.Log($"[ChatMessage] Processing link click: {linkId}");
        
        // Find the corresponding clue segment
        if (clickableClues == null) return;
        
        foreach (var clueSegment in clickableClues)
        {
            if (clueSegment.clueId == linkId)
            {
                // Check if this clue can only be activated once
                if (clueSegment.oneTimeOnly && activatedClues.ContainsKey(linkId))
                {
                    Debug.Log($"[ChatMessage] Clue {linkId} already activated");
                    return;
                }
                
                // Activate the clue
                ActivateClueSegment(clueSegment);
                
                // Mark as activated if it's one-time only
                if (clueSegment.oneTimeOnly)
                {
                    activatedClues[linkId] = true;
                }
                
                break;
            }
        }
    }
    
    /// <summary>
    /// Activate a clickable clue segment (add to notebook)
    /// </summary>
    private void ActivateClueSegment(ClickableClueSegment clueSegment)
    {
        Debug.Log($"[ChatMessage] Activating clue segment: {clueSegment.clueId}");
        
        // Find the notebook manager
        NotebookManager notebook = FindFirstObjectByType<NotebookManager>();
        if (notebook == null)
        {
            Debug.LogError("[ChatMessage] NotebookManager not found!");
            return;
        }
        
        // Add note to notebook
        bool isInInterrogationMode = InterrogationManager.Instance != null && 
                                    !string.IsNullOrEmpty(InterrogationManager.Instance.CurrentSuspectId);
        
        if (isInInterrogationMode)
        {
            // In interrogation mode - add clue but don't auto-close notebook
            notebook.AddClueNoteWithoutClosing(clueSegment.noteText, clueSegment.clueId);
        }
        else
        {
            // Not in interrogation mode - use normal behavior
            notebook.AddClueNote(clueSegment.noteText, clueSegment.clueId);
        }
        
        Debug.Log($"[ChatMessage] Added clue to notebook: {clueSegment.clueId}");
    }
    
    /// <summary>
    /// Check if message is currently animating
    /// </summary>
    public bool IsAnimating => isAnimating;
    
    /// <summary>
    /// Skip current animation
    /// </summary>
    public void SkipAnimation()
    {
        if (isAnimating)
        {
            // Stop all animations - add null checks
            if (transform != null)
                DOTween.Kill(transform);
            if (canvasGroup != null)
                DOTween.Kill(canvasGroup);
            
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            
            // Set final state - add null checks
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            if (transform != null)
                transform.localScale = Vector3.one;
            if (messageText != null)
                messageText.text = fullText;
            
            isAnimating = false;
            OnAnimationComplete?.Invoke();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up clue frames
        ClearClueFrames();
        
        // Clean up tweens - add null checks
        if (transform != null)
            DOTween.Kill(transform);
        if (canvasGroup != null)
            DOTween.Kill(canvasGroup);
        
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }
    }

    /// <summary>
    /// Create and position clickable clue frames after text animation completes
    /// </summary>
    private void CreateClickableClueFrames()
    {
        if (clickableClues == null || clickableClues.Length == 0 || clickableClueFramePrefab == null)
        {
            Debug.Log("[ChatMessage] No clickable clues to create frames for");
            return;
        }

        Debug.Log($"[ChatMessage] Creating {clickableClues.Length} clickable clue frames");

        foreach (var clueSegment in clickableClues)
        {
            CreateFrameForClue(clueSegment);
        }
    }

    /// <summary>
    /// Create a frame for a specific clue segment with proper positioning
    /// </summary>
    private void CreateFrameForClue(ClickableClueSegment clueSegment)
    {
        int textIndex = fullText.IndexOf(clueSegment.clickableText);
        if (textIndex == -1)
        {
            Debug.LogWarning($"[ChatMessage] Could not find word '{clueSegment.clickableText}' in text for frame creation");
            return;
        }

        // Instantiate the frame as a child of the MessageText (same coordinate space)
        GameObject frameInstance = Instantiate(clickableClueFramePrefab, messageText.transform);
        
        // Make sure the frame ignores layout groups
        LayoutElement layoutElement = frameInstance.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = frameInstance.AddComponent<LayoutElement>();
        }
        layoutElement.ignoreLayout = true;

        // Position the frame using TextMeshPro's built-in positioning
        RectTransform frameRect = frameInstance.GetComponent<RectTransform>();
        if (frameRect != null)
        {
            // Use the existing precise positioning method but in the same coordinate space
            Vector2 wordPosition = GetPreciseWordPosition(textIndex, clueSegment.actualTextLength);
            Vector2 wordSize = GetWordSize(textIndex, clueSegment.actualTextLength);
            
            // Set frame position directly (no coordinate conversion needed since we're in the same space)
            frameRect.anchoredPosition = wordPosition;
            frameRect.sizeDelta = new Vector2(wordSize.x + 4f, wordSize.y + 4f);
            
            Debug.Log($"[ChatMessage] Frame for '{clueSegment.clickableText}' positioned at {frameRect.anchoredPosition} with size {frameRect.sizeDelta}");
        }

        // Setup the frame's text content
        TextMeshProUGUI frameText = frameInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (frameText != null)
        {
            frameText.text = clueSegment.clickableText;
        }

        // Store frame info for cleanup using the existing constructor
        ClueFrameInfo frameInfo = new ClueFrameInfo(clueSegment.clickableText, frameInstance, clueSegment);
        instantiatedClueFrames.Add(frameInfo);
    }

    /// <summary>
    /// Get world corners of a word using TextMeshPro's character info
    /// </summary>
    private Vector3[] GetWordWorldCorners(int startCharIndex, int wordLength)
    {
        if (messageText == null || startCharIndex < 0 || startCharIndex + wordLength > messageText.text.Length)
        {
            return null;
        }

        // Force update the text mesh to ensure character info is available
        messageText.ForceMeshUpdate();
        
        TMP_TextInfo textInfo = messageText.textInfo;
        if (textInfo == null || textInfo.characterInfo == null || startCharIndex >= textInfo.characterCount)
        {
            Debug.LogWarning($"[ChatMessage] TextInfo not available or character index out of range: {startCharIndex}");
            return null;
        }

        // Get the bounds of the word
        Vector3 bottomLeft = Vector3.zero;
        Vector3 topRight = Vector3.zero;
        bool foundValidChar = false;

        for (int i = startCharIndex; i < startCharIndex + wordLength && i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            
            if (charInfo.isVisible)
            {
                Vector3 charBottomLeft = new Vector3(charInfo.bottomLeft.x, charInfo.bottomLeft.y, 0);
                Vector3 charTopRight = new Vector3(charInfo.topRight.x, charInfo.topRight.y, 0);
                
                if (!foundValidChar)
                {
                    bottomLeft = charBottomLeft;
                    topRight = charTopRight;
                    foundValidChar = true;
                }
                else
                {
                    bottomLeft = new Vector3(Mathf.Min(bottomLeft.x, charBottomLeft.x), Mathf.Min(bottomLeft.y, charBottomLeft.y), 0);
                    topRight = new Vector3(Mathf.Max(topRight.x, charTopRight.x), Mathf.Max(topRight.y, charTopRight.y), 0);
                }
            }
        }

        if (!foundValidChar)
        {
            Debug.LogWarning($"[ChatMessage] No visible characters found for word at index {startCharIndex}");
            return null;
        }

        // Convert to world coordinates
        Vector3[] corners = new Vector3[4];
        corners[0] = messageText.transform.TransformPoint(bottomLeft); // Bottom-left
        corners[1] = messageText.transform.TransformPoint(new Vector3(bottomLeft.x, topRight.y, 0)); // Top-left
        corners[2] = messageText.transform.TransformPoint(topRight); // Top-right
        corners[3] = messageText.transform.TransformPoint(new Vector3(topRight.x, bottomLeft.y, 0)); // Bottom-right

        return corners;
    }
} 