using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Chat conversation state for a single suspect
/// </summary>
[System.Serializable]
public class ChatConversationState
{
    public string suspectId;
    public List<ChatMessage> messages = new List<ChatMessage>();
    public Vector3 savedScrollPosition;
    
    public ChatConversationState(string id)
    {
        suspectId = id;
        messages = new List<ChatMessage>();
    }
}

/// <summary>
/// Manages the WhatsApp-like chat interface for interrogations
/// </summary>
public class ChatManager : MonoBehaviour
{
    [Header("UI References")]
    public ScrollRect conversationScrollRect;
    public Transform messageContainer;
    public InterrogationDropZone interrogationDropZone;
    public RectTransform dropZoneContainer; // Container that holds the drop zone
    
    [Header("Message Prefabs")]
    public GameObject playerMessagePrefab;
    public GameObject suspectMessagePrefab;
    
    [Header("Animation Settings")]
    public float messageSpacing = 10f;
    public float dropZoneAnimationDuration = 0.3f;
    public float thinkingMessageDelay = 1f;
    public Ease dropZoneEase = Ease.OutQuart;
    
    [Header("Auto Fade Settings")]
    [Tooltip("Delay before auto-fading each message after it appears")]
    public float autoFadeDelay = 1f;
    [Tooltip("Alpha value for faded messages")]
    public float fadedAlpha = 0.3f;
    [Tooltip("Duration of the fade animation")]
    public float fadeAnimationDuration = 0.5f;
    [Tooltip("How many recent messages to keep visible (0 = fade all, 1 = keep newest, 2 = keep 2 newest, etc.)")]
    public int recentMessagesToKeep = 1;
    [Tooltip("How long to wait after scroll stops before fading messages")]
    public float fadeAfterScrollDelay = 3f;
    [Tooltip("Easing for the fade animation")]
    public Ease fadeEase = Ease.OutQuart;
    
    // Auto fade state
    private Coroutine autoFadeCoroutine;
    private Coroutine fadeAfterScrollCoroutine;
    private bool messagesFaded = false;
    private bool userIsScrolling = false;
    private float lastScrollPosition = 0f;
    private float scrollStopTimer = 0f;
    private const float SCROLL_STOP_DELAY = 0.2f; // How long to wait after scroll stops to consider it "stopped"
    
    [Header("Thinking Message Settings")]
    public float minThinkingTime = 2f;
    public float maxThinkingTime = 4f;
    
    // State Management
    private Dictionary<string, ChatConversationState> chatStates = new Dictionary<string, ChatConversationState>();
    private string currentSuspectId = "";
    private List<ChatMessage> activeChatMessages = new List<ChatMessage>();
    private ChatMessage currentThinkingMessage;
    private bool isProcessingMessage = false;
    
    // Events
    public System.Action<string> OnPlayerMessageSent;
    public System.Action<string> OnSuspectResponseReceived;
    
    private void Awake()
    {
        // Setup initial state
        if (interrogationDropZone != null)
        {
            interrogationDropZone.OnTagDroppedEvent += HandleTagDropped;
        }
        
        // Setup scroll listener to stop auto-scroll when user manually scrolls
        if (conversationScrollRect != null)
        {
            conversationScrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            lastScrollPosition = conversationScrollRect.verticalNormalizedPosition;
        }
    }
    
    private void Update()
    {
        // Handle scroll detection
        if (conversationScrollRect != null)
        {
            float currentScrollPosition = conversationScrollRect.verticalNormalizedPosition;
            bool scrollPositionChanged = Mathf.Abs(currentScrollPosition - lastScrollPosition) > 0.001f;
            
            // Check if user is actively providing input
            bool hasInput = Input.GetMouseButton(0) || Input.touchCount > 0 || 
                           Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.01f;
            
            // If scroll position changed and user has input, they're scrolling
            if (scrollPositionChanged && hasInput)
            {
                if (!userIsScrolling)
                {
                    userIsScrolling = true;
                    OnUserStartedScrolling();
                }
                scrollStopTimer = 0f; // Reset the stop timer
            }
            // If position changed but no input (programmatic scroll), don't count as user scrolling
            else if (scrollPositionChanged && !hasInput)
            {
                scrollStopTimer = 0f; // Reset timer but don't count as user scroll
            }
            // If user was scrolling but no recent changes, start stop timer
            else if (userIsScrolling && !scrollPositionChanged)
            {
                scrollStopTimer += Time.deltaTime;
                if (scrollStopTimer >= SCROLL_STOP_DELAY)
                {
                    userIsScrolling = false;
                    OnUserStoppedScrolling();
                }
            }
            
            lastScrollPosition = currentScrollPosition;
        }
    }
    
    /// <summary>
    /// Handle tag dropped on the interrogation zone
    /// </summary>
    private void HandleTagDropped(string tagContent, string tagType, string questionText)
    {
        if (isProcessingMessage) return;
        
        // Use the question text directly passed from the drop zone
        StartCoroutine(ProcessChatSequence(questionText, tagContent));
    }
    
    /// <summary>
    /// Handle scroll value changes (mainly for legacy compatibility)
    /// </summary>
    private void OnScrollValueChanged(Vector2 scrollValue)
    {
        // Main scroll detection is now handled in Update()
        // This method remains for compatibility but the actual logic is in Update()
    }
    
    /// <summary>
    /// Called when user starts scrolling
    /// </summary>
    private void OnUserStartedScrolling()
    {
        // Stop any pending fade timers
        if (fadeAfterScrollCoroutine != null)
        {
            StopCoroutine(fadeAfterScrollCoroutine);
            fadeAfterScrollCoroutine = null;
        }
        
        // Fade in all messages so user can read old chat
        FadeInAllMessages();
    }
    
    /// <summary>
    /// Called when user stops scrolling
    /// </summary>
    private void OnUserStoppedScrolling()
    {
        // Start timer to fade messages back out after delay
        StartFadeAfterScrollTimer();
    }
    
    /// <summary>
    /// Process the complete chat sequence (drop zone -> player message -> thinking -> response)
    /// </summary>
    private IEnumerator ProcessChatSequence(string questionText, string tagContent)
    {
        isProcessingMessage = true;
        
        // 1. Hide the drop zone with animation
        yield return StartCoroutine(HideDropZone());
        
        // 2. Show player message with animation
        yield return StartCoroutine(ShowPlayerMessage(questionText));
        
        // 3. Show thinking message
        yield return StartCoroutine(ShowThinkingMessage());
        
        // 4. Trigger the response processing directly (instead of using event that causes duplicate processing)
        // Don't use OnPlayerMessageSent event as it causes duplicate processing in InterrogationManager
        
        // Process the response directly through InterrogationManager using internal method
        if (InterrogationManager.Instance != null)
        {
            InterrogationManager.Instance.ProcessTagResponseDirectly(tagContent);
        }
        
        // Wait for response (this will be controlled externally)
        // The response will call ShowSuspectResponse when ready
    }
    
    /// <summary>
    /// Hide the drop zone with animation
    /// </summary>
    private IEnumerator HideDropZone()
    {
        if (dropZoneContainer != null && dropZoneContainer.gameObject != null)
        {
            var canvasGroup = dropZoneContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null && dropZoneContainer.gameObject != null)
                canvasGroup = dropZoneContainer.gameObject.AddComponent<CanvasGroup>();
            
            if (canvasGroup != null && dropZoneContainer != null)
            {
                yield return canvasGroup.DOFade(0f, dropZoneAnimationDuration).WaitForCompletion();
            }
            
            if (dropZoneContainer != null && dropZoneContainer.gameObject != null)
            {
                dropZoneContainer.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Show the drop zone (public method for external calls)
    /// </summary>
    public void ShowDropZone()
    {
        StartCoroutine(ShowDropZoneCoroutine());
    }
    
    /// <summary>
    /// Show the drop zone with animation (internal coroutine)
    /// </summary>
    private IEnumerator ShowDropZoneCoroutine()
    {
        if (dropZoneContainer != null && dropZoneContainer.gameObject != null)
        {
            dropZoneContainer.gameObject.SetActive(true);
            
            // Move drop zone to the very bottom (after all messages)
            if (messageContainer != null && dropZoneContainer != null)
            {
                dropZoneContainer.SetParent(messageContainer);
                dropZoneContainer.SetAsLastSibling(); // This ensures it's always at the bottom
            }
            
            var canvasGroup = dropZoneContainer.GetComponent<CanvasGroup>();
            if (canvasGroup == null && dropZoneContainer != null && dropZoneContainer.gameObject != null)
                canvasGroup = dropZoneContainer.gameObject.AddComponent<CanvasGroup>();
            
            if (canvasGroup != null && dropZoneContainer != null)
            {
                canvasGroup.alpha = 0f;
                yield return canvasGroup.DOFade(1f, dropZoneAnimationDuration).WaitForCompletion();
            }
            
            // Scroll to bottom to show the drop zone
            yield return StartCoroutine(ForceScrollToBottom());
        }
    }
    
    /// <summary>
    /// Show player message with pop-in animation
    /// </summary>
    private IEnumerator ShowPlayerMessage(string messageText)
    {
        if (playerMessagePrefab == null || messageContainer == null) yield break;
        
        // Create message
        GameObject messageObj = Instantiate(playerMessagePrefab, messageContainer);
        
        // Move to bottom of the list (latest message goes to the end)
        messageObj.transform.SetSiblingIndex(messageContainer.childCount - 1);
        
        ChatMessage chatMessage = messageObj.GetComponent<ChatMessage>();
        
        if (chatMessage == null)
            chatMessage = messageObj.AddComponent<ChatMessage>();
        
        // Setup components if needed
        SetupMessageComponents(chatMessage, messageObj);
        
        // Setup and animate
        bool animationComplete = false;
        chatMessage.OnAnimationComplete += () => animationComplete = true;
        chatMessage.SetupMessage(messageText, true, true); // Enable typewriter for player messages
        
        activeChatMessages.Add(chatMessage);
        
        // Wait for animation to complete
        yield return new WaitUntil(() => animationComplete);
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Start individual fade timer for this message
        StartIndividualMessageFade(chatMessage);
    }
    
    /// <summary>
    /// Show thinking message (with dots animation)
    /// </summary>
    private IEnumerator ShowThinkingMessage()
    {
        if (suspectMessagePrefab == null || messageContainer == null) yield break;
        
        // Create thinking message
        GameObject messageObj = Instantiate(suspectMessagePrefab, messageContainer);
        
        // Move to bottom of the list (latest message goes to the end)
        messageObj.transform.SetSiblingIndex(messageContainer.childCount - 1);
        
        ChatMessage chatMessage = messageObj.GetComponent<ChatMessage>();
        
        if (chatMessage == null)
            chatMessage = messageObj.AddComponent<ChatMessage>();
        
        // Setup components
        SetupMessageComponents(chatMessage, messageObj);
        
        // Add thinking dots animation
        ThinkingDotsAnimation thinkingDots = messageObj.AddComponent<ThinkingDotsAnimation>();
        
        // Setup and animate
        bool animationComplete = false;
        chatMessage.OnAnimationComplete += () => animationComplete = true;
        chatMessage.SetupMessage("", false, false); // Start with empty text
        
        currentThinkingMessage = chatMessage;
        
        // Wait for pop-in animation
        yield return new WaitUntil(() => animationComplete);
        
        // Start thinking dots
        thinkingDots.StartThinkingAnimation("");
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Wait minimum thinking time
        yield return new WaitForSeconds(thinkingMessageDelay);
        
        // DO NOT add thinking message to activeChatMessages yet - it will be added when converted to real response
        // DO NOT set currentThinkingMessage to null - it needs to remain available for replacement
    }
    
    /// <summary>
    /// Show suspect response (only if for current suspect)
    /// </summary>
    public void ShowSuspectResponse(string responseText, System.Action onComplete = null)
    {
        // Verify we're still showing the same suspect
        if (string.IsNullOrEmpty(currentSuspectId))
        {
            Debug.LogWarning($"[ChatManager] Ignoring response - no current suspect");
            return;
        }
        
        StartCoroutine(ShowSingleResponseWithClues(responseText, null, onComplete, true));
    }
    
    /// <summary>
    /// Show suspect response with extractable clues (only if for current suspect)
    /// </summary>
    public void ShowSuspectResponseWithClues(string responseText, string[] extractableClues, System.Action onComplete = null)
    {
        // Verify we're still showing the same suspect
        if (string.IsNullOrEmpty(currentSuspectId))
        {
            Debug.LogWarning($"[ChatManager] Ignoring response with clues - no current suspect");
            return;
        }
        
        StartCoroutine(ShowSuspectResponseCoroutineWithClues(responseText, extractableClues, onComplete));
    }
    
    /// <summary>
    /// Show suspect response with clickable clue segments (enhanced version)
    /// </summary>
    public void ShowSuspectResponseWithClickableClues(string responseText, ClickableClueSegment[] clickableClues, System.Action onComplete = null)
    {
        // Verify we're still showing the same suspect
        if (string.IsNullOrEmpty(currentSuspectId))
        {
            Debug.LogWarning($"[ChatManager] Ignoring response with clickable clues - no current suspect");
            return;
        }
        
        StartCoroutine(ShowSuspectResponseCoroutineWithClickableClues(responseText, clickableClues, onComplete));
    }
    
    /// <summary>
    /// Replace thinking message with actual suspect response including extractable clues
    /// </summary>
    private IEnumerator ShowSuspectResponseCoroutineWithClues(string responseText, string[] extractableClues, System.Action onComplete, bool completeSequence = true)
    {
        // Check if response contains multiple parts (separated by ". ")
        string[] responseParts = responseText.Split(new string[] { ". " }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (responseParts.Length > 1)
        {
            // Multiple sequential responses
            yield return StartCoroutine(ShowSequentialResponsesWithClues(responseParts, extractableClues, onComplete));
        }
        else
        {
            // Single response
            yield return StartCoroutine(ShowSingleResponseWithClues(responseText, extractableClues, onComplete, completeSequence));
        }
    }
    
    /// <summary>
    /// Replace thinking message with actual suspect response including clickable clue segments
    /// </summary>
    private IEnumerator ShowSuspectResponseCoroutineWithClickableClues(string responseText, ClickableClueSegment[] clickableClues, System.Action onComplete, bool completeSequence = true)
    {
        // Check if response contains multiple parts (separated by ". ")
        string[] responseParts = responseText.Split(new string[] { ". " }, System.StringSplitOptions.RemoveEmptyEntries);
        
        if (responseParts.Length > 1)
        {
            // Multiple sequential responses with clickable clues
            yield return StartCoroutine(ShowSequentialResponsesWithClickableClues(responseParts, clickableClues, onComplete));
        }
        else
        {
            // Single response with clickable clues
            yield return StartCoroutine(ShowSingleResponseWithClickableClues(responseText, clickableClues, onComplete, completeSequence));
        }
    }
    
    /// <summary>
    /// Show multiple sequential responses as separate messages with clues
    /// </summary>
    private IEnumerator ShowSequentialResponsesWithClues(string[] responseParts, string[] extractableClues, System.Action onComplete = null)
    {
        // Replace thinking message with first response
        string firstResponse = responseParts[0];
        
        // Add period back if it doesn't end with punctuation
        if (!firstResponse.EndsWith(".") && !firstResponse.EndsWith("!") && !firstResponse.EndsWith("?"))
        {
            // Don't add period for incomplete expressions (like "Oh man" or "Umm")
            if (!firstResponse.ToLower().Contains("oh") && !firstResponse.ToLower().Contains("umm") && !firstResponse.ToLower().Contains("uh"))
            {
                firstResponse += ".";
            }
        }
        
        // Only apply clues to the last message in the sequence
        string[] firstClues = (responseParts.Length == 1) ? extractableClues : null;
        yield return StartCoroutine(ShowSingleResponseWithClues(firstResponse, firstClues, null, false));
        
        // Create additional messages for remaining responses
        for (int i = 1; i < responseParts.Length; i++)
        {
            string responsePart = responseParts[i];
            
            // Add period back if it doesn't end with punctuation
            if (!responsePart.EndsWith(".") && !responsePart.EndsWith("!") && !responsePart.EndsWith("?"))
            {
                responsePart += ".";
            }
            
            // Small delay between messages
            yield return new WaitForSeconds(0.5f);
            
            // Apply clues only to the last message
            string[] partClues = (i == responseParts.Length - 1) ? extractableClues : null;
            
            // Create new suspect message
            yield return StartCoroutine(CreateNewSuspectMessageWithClues(responsePart, partClues, null));
        }
        
        // NOW complete the entire sequence (show drop zone and call onComplete)
        yield return StartCoroutine(CompleteMessageSequence(onComplete));
    }
    
    /// <summary>
    /// Show multiple sequential responses as separate messages with clickable clues
    /// </summary>
    private IEnumerator ShowSequentialResponsesWithClickableClues(string[] responseParts, ClickableClueSegment[] clickableClues, System.Action onComplete = null)
    {
        // Replace thinking message with first response
        string firstResponse = responseParts[0];
        
        // Add period back if it doesn't end with punctuation
        if (!firstResponse.EndsWith(".") && !firstResponse.EndsWith("!") && !firstResponse.EndsWith("?"))
        {
            // Don't add period for incomplete expressions (like "Oh man" or "Umm")
            if (!firstResponse.ToLower().Contains("oh") && !firstResponse.ToLower().Contains("umm") && !firstResponse.ToLower().Contains("uh"))
            {
                firstResponse += ".";
            }
        }
        
        // Only apply clues to the last message in the sequence
        ClickableClueSegment[] firstClickableClues = (responseParts.Length == 1) ? clickableClues : null;
        yield return StartCoroutine(ShowSingleResponseWithClickableClues(firstResponse, firstClickableClues, null, false));
        
        // Create additional messages for remaining responses
        for (int i = 1; i < responseParts.Length; i++)
        {
            string responsePart = responseParts[i];
            
            // Add period back if it doesn't end with punctuation
            if (!responsePart.EndsWith(".") && !responsePart.EndsWith("!") && !responsePart.EndsWith("?"))
            {
                responsePart += ".";
            }
            
            // Small delay between messages
            yield return new WaitForSeconds(0.5f);
            
            // Apply clues only to the last message
            ClickableClueSegment[] partClickableClues = (i == responseParts.Length - 1) ? clickableClues : null;
            
            // Create new suspect message with clickable clues
            yield return StartCoroutine(CreateNewSuspectMessageWithClickableClues(responsePart, partClickableClues, null));
        }
        
        // NOW complete the entire sequence (show drop zone and call onComplete)
        yield return StartCoroutine(CompleteMessageSequence(onComplete));
    }
    
    /// <summary>
    /// Show single response with clues
    /// </summary>
    private IEnumerator ShowSingleResponseWithClues(string responseText, string[] extractableClues, System.Action onComplete, bool completeSequence = true)
    {
        if (currentThinkingMessage != null)
        {
            // Stop thinking animation - add null checks
            ThinkingDotsAnimation thinkingDots = currentThinkingMessage.GetComponent<ThinkingDotsAnimation>();
            if (thinkingDots != null && currentThinkingMessage != null && currentThinkingMessage.gameObject != null)
            {
                thinkingDots.StopThinkingAnimation();
            }
            
            // Clear any existing animation complete callbacks - add null check
            if (currentThinkingMessage != null)
            {
                currentThinkingMessage.OnAnimationComplete = null;
            }
            
            // Update with actual response using typewriter
            bool animationComplete = false;
            if (currentThinkingMessage != null)
            {
                currentThinkingMessage.OnAnimationComplete = () => {
                    animationComplete = true;
                    Debug.Log("[ChatManager] Typewriter animation completed");
                };
                
                // Setup message with clues if available
                if (extractableClues != null && extractableClues.Length > 0)
                {
                    currentThinkingMessage.SetupMessageWithClues(responseText, false, extractableClues, true);
                }
                else
                {
                    currentThinkingMessage.UpdateMessageText(responseText, true);
                }
                

            }
            
            // Wait for typewriter to complete with timeout
            float timeout = responseText.Length * 0.05f + 10f;
            float timer = 0f;
            while (!animationComplete && timer < timeout && currentThinkingMessage != null)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!animationComplete && currentThinkingMessage != null)
            {
                Debug.LogWarning("[ChatManager] Typewriter animation timed out, skipping");
                currentThinkingMessage.SkipAnimation();
            }
            
            if (currentThinkingMessage != null)
            {
                activeChatMessages.Add(currentThinkingMessage);
                

                
                // Start individual fade timer for this message
                StartIndividualMessageFade(currentThinkingMessage);
            }
            currentThinkingMessage = null;
        }
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Only complete the sequence if requested (for single responses or last in sequence)
        if (completeSequence)
        {
            yield return StartCoroutine(CompleteMessageSequence(onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Create a new suspect message with extractable clues (for sequential responses)
    /// </summary>
    private IEnumerator CreateNewSuspectMessageWithClues(string messageText, string[] extractableClues, System.Action onComplete = null)
    {
        if (suspectMessagePrefab == null || messageContainer == null) yield break;
        
        // Create new suspect message
        GameObject messageObj = Instantiate(suspectMessagePrefab, messageContainer);
        messageObj.transform.SetSiblingIndex(messageContainer.childCount - 1);
        
        ChatMessage chatMessage = messageObj.GetComponent<ChatMessage>();
        if (chatMessage == null)
            chatMessage = messageObj.AddComponent<ChatMessage>();
        
        SetupMessageComponents(chatMessage, messageObj);
        
        // Setup and animate
        bool animationComplete = false;
        chatMessage.OnAnimationComplete += () => animationComplete = true;
        
        // Use clues if available
        if (extractableClues != null && extractableClues.Length > 0)
        {
            chatMessage.SetupMessageWithClues(messageText, false, extractableClues, true);
        }
        else
        {
            chatMessage.SetupMessage(messageText, false, true); // Suspect message with typewriter
        }
        
        // Wait for animation to complete
        float timeout = messageText.Length * 0.05f + 5f;
        float timer = 0f;
        while (!animationComplete && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!animationComplete)
        {
            chatMessage.SkipAnimation();
        }
        
        activeChatMessages.Add(chatMessage);
        
        Debug.Log($"[ChatManager] Added player message to activeChatMessages. Total count: {activeChatMessages.Count}");
        Debug.Log($"[ChatManager] Message details - Name: {chatMessage.name}, Text: {chatMessage.messageText?.text}");
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Start individual fade timer for this message
        StartIndividualMessageFade(chatMessage);
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Show single response with clickable clue segments
    /// </summary>
    private IEnumerator ShowSingleResponseWithClickableClues(string responseText, ClickableClueSegment[] clickableClues, System.Action onComplete, bool completeSequence = true)
    {
        if (currentThinkingMessage != null)
        {
            Debug.Log($"[ChatManager] Replacing thinking message with clickable clue response: '{responseText}'");
            
            // Stop thinking animation - add null checks
            ThinkingDotsAnimation thinkingDots = currentThinkingMessage.GetComponent<ThinkingDotsAnimation>();
            if (thinkingDots != null && currentThinkingMessage != null && currentThinkingMessage.gameObject != null)
            {
                thinkingDots.StopThinkingAnimation();
                Debug.Log("[ChatManager] Stopped thinking animation");
            }
            
            // Clear any existing animation complete callbacks - add null check
            if (currentThinkingMessage != null)
            {
                currentThinkingMessage.OnAnimationComplete = null;
            }
            
            // Update with actual response using typewriter
            bool animationComplete = false;
            if (currentThinkingMessage != null)
            {
                currentThinkingMessage.OnAnimationComplete = () => {
                    animationComplete = true;
                    Debug.Log("[ChatManager] Typewriter animation completed");
                };
                
                // Setup message with clickable clues
                currentThinkingMessage.SetupMessageWithClickableClues(responseText, false, clickableClues, true);
                

            }
            
            // Wait for typewriter to complete with timeout
            float timeout = responseText.Length * 0.05f + 10f;
            float timer = 0f;
            while (!animationComplete && timer < timeout && currentThinkingMessage != null)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            
            if (!animationComplete && currentThinkingMessage != null)
            {
                Debug.LogWarning("[ChatManager] Typewriter animation timed out, skipping");
                currentThinkingMessage.SkipAnimation();
            }
            
            if (currentThinkingMessage != null)
            {
                activeChatMessages.Add(currentThinkingMessage);
                

                
                // Start individual fade timer for this message
                StartIndividualMessageFade(currentThinkingMessage);
            }
            currentThinkingMessage = null;
        }
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Only complete the sequence if requested (for single responses or last in sequence)
        if (completeSequence)
        {
            yield return StartCoroutine(CompleteMessageSequence(onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// Create a new suspect message with clickable clue segments (for sequential responses)
    /// </summary>
    private IEnumerator CreateNewSuspectMessageWithClickableClues(string messageText, ClickableClueSegment[] clickableClues, System.Action onComplete = null)
    {
        if (suspectMessagePrefab == null || messageContainer == null) yield break;
        
        // Create new suspect message
        GameObject messageObj = Instantiate(suspectMessagePrefab, messageContainer);
        messageObj.transform.SetSiblingIndex(messageContainer.childCount - 1);
        
        ChatMessage chatMessage = messageObj.GetComponent<ChatMessage>();
        if (chatMessage == null)
            chatMessage = messageObj.AddComponent<ChatMessage>();
        
        SetupMessageComponents(chatMessage, messageObj);
        
        // Setup and animate
        bool animationComplete = false;
        chatMessage.OnAnimationComplete += () => animationComplete = true;
        
        // Use clickable clues if available
        if (clickableClues != null && clickableClues.Length > 0)
        {
            chatMessage.SetupMessageWithClickableClues(messageText, false, clickableClues, true);
        }
        else
        {
            chatMessage.SetupMessage(messageText, false, true); // Suspect message with typewriter
        }
        
        // Wait for animation to complete
        float timeout = messageText.Length * 0.05f + 5f;
        float timer = 0f;
        while (!animationComplete && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!animationComplete)
        {
            chatMessage.SkipAnimation();
        }
        
        activeChatMessages.Add(chatMessage);
        
        Debug.Log($"[ChatManager] Added player message to activeChatMessages. Total count: {activeChatMessages.Count}");
        Debug.Log($"[ChatManager] Message details - Name: {chatMessage.name}, Text: {chatMessage.messageText?.text}");
        
        // Scroll to bottom
        yield return StartCoroutine(ScrollToBottom());
        
        // Start individual fade timer for this message
        StartIndividualMessageFade(chatMessage);
        
        onComplete?.Invoke();
    }
    
    /// <summary>
    /// Complete the entire message sequence (show drop zone and finish)
    /// </summary>
    private IEnumerator CompleteMessageSequence(System.Action onComplete = null)
    {
        // Wait a moment then show drop zone again
        yield return new WaitForSeconds(0.5f);
        
        // Show drop zone and clear any dropped tags
        yield return StartCoroutine(ShowDropZoneCoroutine());
        
        if (interrogationDropZone != null)
            interrogationDropZone.ClearDroppedTag();
        
        isProcessingMessage = false;
        
        OnSuspectResponseReceived?.Invoke("sequence_completed");
        onComplete?.Invoke();
        
        Debug.Log("[ChatManager] Chat sequence completed");
    }
    
    /// <summary>
    /// Setup message components (ensure ChatMessage has required components)
    /// </summary>
    private void SetupMessageComponents(ChatMessage chatMessage, GameObject messageObj)
    {
        // Ensure required components exist
        if (chatMessage.messageText == null)
            chatMessage.messageText = messageObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        
        if (chatMessage.bubbleBackground == null)
        {
            // Specifically look for the MessageBubble's Image component
            Transform messageBubbleTransform = messageObj.transform.Find("MessageBubble");
            if (messageBubbleTransform != null)
            {
                chatMessage.bubbleBackground = messageBubbleTransform.GetComponent<Image>();
                Debug.Log($"[ChatManager] Found MessageBubble Image component: {chatMessage.bubbleBackground != null}");
            }
            
            // Fallback to first Image component if MessageBubble not found
            if (chatMessage.bubbleBackground == null)
            {
                chatMessage.bubbleBackground = messageObj.GetComponentInChildren<Image>();
                Debug.Log($"[ChatManager] Using fallback Image component: {chatMessage.bubbleBackground != null}");
            }
        }
        
        if (chatMessage.canvasGroup == null)
        {
            chatMessage.canvasGroup = messageObj.GetComponent<CanvasGroup>();
            if (chatMessage.canvasGroup == null)
                chatMessage.canvasGroup = messageObj.AddComponent<CanvasGroup>();
        }
    }
    
    /// <summary>
    /// Scroll to bottom of conversation (auto-scroll for new messages)
    /// </summary>
    private IEnumerator ScrollToBottom()
    {
        // Don't auto-scroll if user is manually scrolling
        if (userIsScrolling) yield break;
        
        // Wait multiple frames to ensure layout is rebuilt
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (conversationScrollRect != null)
        {
            // Force layout rebuild first
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(conversationScrollRect.content);
            
            // Set scroll position to bottom (0 = bottom, 1 = top for vertical scroll)
            conversationScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Force scroll to bottom (ignores user scrolling state - used for programmatic scrolling)
    /// </summary>
    private IEnumerator ForceScrollToBottom()
    {
        // Wait multiple frames to ensure layout is rebuilt
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (conversationScrollRect != null)
        {
            // Force layout rebuild first
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(conversationScrollRect.content);
            
            // Set scroll position to bottom (0 = bottom, 1 = top for vertical scroll)
            conversationScrollRect.verticalNormalizedPosition = 0f;
        }
    }
    
    /// <summary>
    /// Start individual fade timer for a message
    /// </summary>
    private void StartIndividualMessageFade(ChatMessage message)
    {
        if (message != null)
        {
            // Ensure the message has a CanvasGroup for fading
            if (message.canvasGroup == null)
            {
                message.canvasGroup = message.gameObject.GetComponent<CanvasGroup>();
                if (message.canvasGroup == null)
                {
                    message.canvasGroup = message.gameObject.AddComponent<CanvasGroup>();
                }
            }
            
            // Ensure the CanvasGroup starts at full alpha
            message.canvasGroup.alpha = 1f;
            
            StartCoroutine(FadeIndividualMessageAfterDelay(message));
        }
        
        // After adding a new message, re-evaluate fade states for all messages
        StartCoroutine(RevaluateAllMessageFadeStates());
    }
    
    /// <summary>
    /// Fade individual message after delay
    /// </summary>
    private IEnumerator FadeIndividualMessageAfterDelay(ChatMessage message)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(autoFadeDelay);
        
        // Check if this message should be kept visible based on recentMessagesToKeep setting
        if (ShouldMessageBeFaded(message))
        {
            if (message != null && message.canvasGroup != null)
            {
                // Use DOTween to fade the message
                message.canvasGroup.DOFade(fadedAlpha, fadeAnimationDuration)
                    .SetEase(fadeEase);
            }
        }
    }
    
    /// <summary>
    /// Check if a message should be faded based on recentMessagesToKeep setting
    /// </summary>
    private bool ShouldMessageBeFaded(ChatMessage message)
    {
        if (recentMessagesToKeep <= 0)
        {
            return true; // Fade all messages
        }
        
        // Find the index of this message in the active messages list
        int messageIndex = activeChatMessages.IndexOf(message);
        if (messageIndex == -1)
        {
            return false; // Message not found, don't fade
        }
        
        // Check if this message is among the most recent ones to keep
        int totalMessages = activeChatMessages.Count;
        int cutoffIndex = totalMessages - recentMessagesToKeep;
        
        return messageIndex < cutoffIndex; // Fade if message is older than the cutoff
    }
    
    /// <summary>
    /// Fade in all messages (when user scrolls)
    /// </summary>
    private void FadeInAllMessages()
    {
        foreach (var message in activeChatMessages)
        {
            if (message != null && message.canvasGroup != null)
            {
                message.canvasGroup.DOFade(1f, fadeAnimationDuration)
                    .SetEase(fadeEase);
            }
        }
        
        messagesFaded = false;
    }
    
    /// <summary>
    /// Re-fade messages based on current recentMessagesToKeep setting
    /// </summary>
    private void RefadeMessages()
    {
        foreach (var message in activeChatMessages)
        {
            if (ShouldMessageBeFaded(message))
            {
                if (message != null && message.canvasGroup != null)
                {
                    message.canvasGroup.DOFade(fadedAlpha, fadeAnimationDuration)
                        .SetEase(fadeEase);
                }
            }
        }
        
        messagesFaded = true;
    }
    
    /// <summary>
    /// Stop auto-fade and start fade-after-scroll timer
    /// </summary>
    public void StopAutoFade()
    {
        // If messages are faded, fade them back in
        if (messagesFaded)
        {
            FadeInAllMessages();
        }
        
        // Start timer to fade them out again after user stops scrolling
        StartFadeAfterScrollTimer();
    }
    
    /// <summary>
    /// Start timer to fade messages after user stops scrolling
    /// </summary>
    private void StartFadeAfterScrollTimer()
    {
        // Stop any existing timer
        if (fadeAfterScrollCoroutine != null)
        {
            StopCoroutine(fadeAfterScrollCoroutine);
        }
        
        // Start new timer
        fadeAfterScrollCoroutine = StartCoroutine(FadeAfterScrollTimer());
    }
    
    /// <summary>
    /// Timer that waits after scrolling stops, then fades messages
    /// </summary>
    private IEnumerator FadeAfterScrollTimer()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(fadeAfterScrollDelay);
        
        // Only fade if user isn't currently scrolling
        if (!userIsScrolling)
        {
            RefadeMessages();
        }
        
        fadeAfterScrollCoroutine = null;
    }
    
    /// <summary>
    /// Stop auto-scroll (called when user manually scrolls)
    /// </summary>
    public void StopAutoScroll()
    {
        if (autoFadeCoroutine != null)
        {
            StopCoroutine(autoFadeCoroutine);
            autoFadeCoroutine = null;
            Debug.Log("[ChatManager] Auto-scroll stopped by user interaction");
        }
    }
    
    /// <summary>
    /// Clear all chat messages
    /// </summary>
    public void ClearChat()
    {
        foreach (var chatMessage in activeChatMessages)
        {
            if (chatMessage != null && chatMessage.gameObject != null)
                Destroy(chatMessage.gameObject);
        }
        
        activeChatMessages.Clear();
        
        if (currentThinkingMessage != null && currentThinkingMessage.gameObject != null)
        {
            Destroy(currentThinkingMessage.gameObject);
            currentThinkingMessage = null;
        }
        
        isProcessingMessage = false;
        
        // Show drop zone
        ShowDropZone();
    }
    
    /// <summary>
    /// Switch to a different suspect's conversation
    /// </summary>
    public void SwitchToSuspect(string suspectId)
    {
        if (suspectId == currentSuspectId) return;
        
        Debug.Log($"[ChatManager] Switching from suspect '{currentSuspectId}' to '{suspectId}'");
        
        // Complete any ongoing animations before saving state
        CompleteAllActiveAnimations();
        
        // If we're in the middle of processing a message, complete it properly
        if (isProcessingMessage)
        {
            Debug.Log("[ChatManager] Completing ongoing message processing before switching suspects");
            // Complete any thinking message sequence immediately
            if (currentThinkingMessage != null)
            {
                // The thinking message will be completed by CompleteAllActiveAnimations() above
                // Just clear the processing flag
                isProcessingMessage = false;
            }
        }
        
        // Save current conversation state
        if (!string.IsNullOrEmpty(currentSuspectId))
        {
            SaveCurrentConversationState();
        }
        
        // Clear current UI
        ClearActiveMessages();
        
        // Reset processing state to ensure UI is responsive
        isProcessingMessage = false;
        
        // Load new conversation state
        currentSuspectId = suspectId;
        LoadConversationState(suspectId);
        
        Debug.Log($"[ChatManager] Successfully switched to suspect: {suspectId}");
    }
    
    /// <summary>
    /// Complete all ongoing animations in active messages so they're saved in their final state
    /// </summary>
    private void CompleteAllActiveAnimations()
    {
        // Complete all message animations
        foreach (var message in activeChatMessages)
        {
            if (message != null && message.gameObject != null && message.IsAnimating)
            {
                message.SkipAnimation();
                Debug.Log($"[ChatManager] Completed animation for message: {message.name}");
            }
        }
        
        // Complete thinking message animation if it exists
        if (currentThinkingMessage != null && currentThinkingMessage.gameObject != null && currentThinkingMessage.IsAnimating)
        {
            currentThinkingMessage.SkipAnimation();
            Debug.Log("[ChatManager] Completed thinking message animation");
        }
        
        // Wait one frame to ensure all animations are processed
        // This is needed because SkipAnimation might trigger callbacks that need to complete
        if (activeChatMessages.Count > 0 || currentThinkingMessage != null)
        {
            Debug.Log("[ChatManager] All animations completed before saving conversation state");
        }
    }
    
    /// <summary>
    /// Save the current conversation state for the active suspect
    /// </summary>
    private void SaveCurrentConversationState()
    {
        if (string.IsNullOrEmpty(currentSuspectId)) return;
        
        if (!chatStates.ContainsKey(currentSuspectId))
        {
            chatStates[currentSuspectId] = new ChatConversationState(currentSuspectId);
        }
        
        var state = chatStates[currentSuspectId];
        state.messages.Clear();
        state.messages.AddRange(activeChatMessages);
        
        // Save scroll position
        if (conversationScrollRect != null)
        {
            state.savedScrollPosition = new Vector3(
                conversationScrollRect.horizontalNormalizedPosition,
                conversationScrollRect.verticalNormalizedPosition,
                0f
            );
        }
        
        Debug.Log($"[ChatManager] Saved conversation state for {currentSuspectId} - {state.messages.Count} messages");
    }
    
    /// <summary>
    /// Load conversation state for a suspect
    /// </summary>
    private void LoadConversationState(string suspectId)
    {
        if (!chatStates.ContainsKey(suspectId))
        {
            chatStates[suspectId] = new ChatConversationState(suspectId);
            Debug.Log($"[ChatManager] Created new conversation state for {suspectId}");
        }

        var state = chatStates[suspectId];
        
        // Sync with InterrogationManager's conversation history first
        // This handles messages that were processed in the background while this suspect wasn't active
        SyncWithInterrogationManager(suspectId, state);
        
        // Restore messages
        foreach (var savedMessage in state.messages)
        {
            if (savedMessage != null)
            {
                // Re-parent the message to our container and reactivate
                savedMessage.transform.SetParent(messageContainer, false);
                savedMessage.gameObject.SetActive(true);
                activeChatMessages.Add(savedMessage);
            }
        }
        
        Debug.Log($"[ChatManager] Loaded conversation state for {suspectId} - {activeChatMessages.Count} messages");
        
        // Show drop zone at bottom and restore scroll position
        StartCoroutine(LoadConversationComplete(state.savedScrollPosition));
    }
    
    /// <summary>
    /// Sync ChatManager state with InterrogationManager's conversation history
    /// Creates ChatMessage objects for any messages that were processed while this suspect wasn't active
    /// </summary>
    private void SyncWithInterrogationManager(string suspectId, ChatConversationState chatState)
    {
        if (InterrogationManager.Instance == null) return;
        
        var interrogationHistory = InterrogationManager.Instance.GetConversationHistory(suspectId);
        var chatMessageCount = chatState.messages.Count;
        var interrogationMessageCount = interrogationHistory.Count;
        
        Debug.Log($"[ChatManager] Syncing suspect '{suspectId}': ChatManager has {chatMessageCount} messages, InterrogationManager has {interrogationMessageCount} messages");
        
        // If InterrogationManager has more messages, create ChatMessage objects for the missing ones
        if (interrogationMessageCount > chatMessageCount)
        {
            for (int i = chatMessageCount; i < interrogationMessageCount; i++)
            {
                var missingMessage = interrogationHistory[i];
                GameObject messageObj;
                
                // Create appropriate message prefab
                if (missingMessage.isPlayerMessage)
                {
                    messageObj = Instantiate(playerMessagePrefab);
                }
                else
                {
                    messageObj = Instantiate(suspectMessagePrefab);
                }
                
                // Set up the ChatMessage component
                ChatMessage chatMessage = messageObj.GetComponent<ChatMessage>();
                if (chatMessage == null)
                    chatMessage = messageObj.AddComponent<ChatMessage>();
                
                SetupMessageComponents(chatMessage, messageObj);
                
                // Set up the message content (no animation since this is background sync)
                chatMessage.SetupMessage(missingMessage.messageText, missingMessage.isPlayerMessage, false);
                
                // Add to chat state
                chatState.messages.Add(chatMessage);
                
                // Keep it inactive for now (will be activated when loaded)
                messageObj.SetActive(false);
                
                Debug.Log($"[ChatManager] Created missing ChatMessage for: '{missingMessage.messageText.Substring(0, Mathf.Min(30, missingMessage.messageText.Length))}...'");
            }
        }
    }
    
    /// <summary>
    /// Clear active messages from UI without destroying them
    /// </summary>
    private void ClearActiveMessages()
    {
        foreach (var message in activeChatMessages)
        {
            if (message != null && message.gameObject != null)
            {
                // Don't destroy, just remove from parent so they're preserved
                message.transform.SetParent(null, false);
                message.gameObject.SetActive(false);
            }
        }
        
        activeChatMessages.Clear();
        
        if (currentThinkingMessage != null && currentThinkingMessage.gameObject != null)
        {
            // Stop any thinking message and clean it up properly
            Destroy(currentThinkingMessage.gameObject);
            currentThinkingMessage = null;
        }
    }
    
    /// <summary>
    /// Complete the conversation loading process: show drop zone and restore scroll position
    /// </summary>
    private IEnumerator LoadConversationComplete(Vector3 savedScrollPosition)
    {
        // First, show the drop zone at the bottom
        yield return StartCoroutine(ShowDropZoneCoroutine());
        
        // Then restore the scroll position if there were messages
        if (activeChatMessages.Count > 0)
        {
            yield return StartCoroutine(RestoreScrollPosition(savedScrollPosition));
        }
    }
    
    /// <summary>
    /// Restore scroll position after loading conversation
    /// </summary>
    private IEnumerator RestoreScrollPosition(Vector3 savedPosition)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        if (conversationScrollRect != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(conversationScrollRect.content);
            
            conversationScrollRect.horizontalNormalizedPosition = savedPosition.x;
            conversationScrollRect.verticalNormalizedPosition = savedPosition.y;
        }
    }
    
    /// <summary>
    /// Clear all conversation states (when starting new game)
    /// </summary>
    public void ClearAllConversationStates()
    {
        // Destroy all stored message objects
        foreach (var state in chatStates.Values)
        {
            foreach (var message in state.messages)
            {
                if (message != null && message.gameObject != null)
                    Destroy(message.gameObject);
            }
        }
        
        chatStates.Clear();
        ClearChat();
        currentSuspectId = "";
        
        Debug.Log("[ChatManager] Cleared all conversation states");
    }
    
    /// <summary>
    /// Get conversation history for a specific suspect
    /// </summary>
    public List<ChatMessage> GetConversationHistory(string suspectId)
    {
        if (suspectId == currentSuspectId)
        {
            return new List<ChatMessage>(activeChatMessages);
        }
        
        if (chatStates.ContainsKey(suspectId))
        {
            return new List<ChatMessage>(chatStates[suspectId].messages);
        }
        
        return new List<ChatMessage>();
    }
    
    /// <summary>
    /// Check if suspect has any conversation history
    /// </summary>
    public bool HasConversationHistory(string suspectId)
    {
        if (suspectId == currentSuspectId)
        {
            return activeChatMessages.Count > 0;
        }
        
        return chatStates.ContainsKey(suspectId) && chatStates[suspectId].messages.Count > 0;
    }
    
    /// <summary>
    /// Hide the drop zone immediately (for when interrogation ends)
    /// </summary>
    public void HideDropZoneImmediate()
    {
        if (dropZoneContainer != null)
        {
            dropZoneContainer.gameObject.SetActive(false);
            Debug.Log("[ChatManager] Drop zone hidden immediately");
        }
    }
    
    /// <summary>
    /// Check if chat is currently processing a message
    /// </summary>
    public bool IsProcessingMessage => isProcessingMessage;
    
    /// <summary>
    /// Get the currently active suspect ID
    /// </summary>
    public string GetCurrentSuspectId() => currentSuspectId;
    
    /// <summary>
    /// Re-evaluate fade states for all messages when a new message is added
    /// This ensures that older messages get faded when they're no longer in the "recent messages to keep" range
    /// </summary>
    private IEnumerator RevaluateAllMessageFadeStates()
    {
        // Wait for the new message to be fully set up
        yield return new WaitForSeconds(autoFadeDelay);
        
        // Don't interfere if user is scrolling
        if (userIsScrolling) yield break;
        
        // Go through all existing messages and update their fade state
        for (int i = 0; i < activeChatMessages.Count; i++)
        {
            ChatMessage message = activeChatMessages[i];
            if (message != null && message.canvasGroup != null)
            {
                bool shouldBeFaded = ShouldMessageBeFaded(message);
                float currentAlpha = message.canvasGroup.alpha;
                
                // If message should be faded but isn't, fade it
                if (shouldBeFaded && currentAlpha > fadedAlpha + 0.1f)
                {
                    message.canvasGroup.DOFade(fadedAlpha, fadeAnimationDuration)
                        .SetEase(fadeEase);
                }
                // If message should be visible but is faded, show it
                else if (!shouldBeFaded && currentAlpha < 1f - 0.1f)
                {
                    message.canvasGroup.DOFade(1f, fadeAnimationDuration)
                        .SetEase(fadeEase);
                }
            }
        }
    }
    
    private void OnDestroy()
    {
        // Cleanup scroll listener
        if (conversationScrollRect != null)
        {
            conversationScrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        }
        
        // Stop any running fade-after-scroll coroutine
        if (fadeAfterScrollCoroutine != null)
        {
            StopCoroutine(fadeAfterScrollCoroutine);
            fadeAfterScrollCoroutine = null;
        }
        
        // Cleanup drop zone events
        if (interrogationDropZone != null)
        {
            interrogationDropZone.OnTagDroppedEvent -= HandleTagDropped;
        }
    }
} 