using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Main interrogation manager - handles suspect switching and conversation flow
/// ARCHITECTURE: InterrogationManager handles logic, ChatManager handles UI
/// </summary>
public class InterrogationManager : MonoBehaviour
{
    public static InterrogationManager Instance { get; private set; }

    [Header("Chat System")]
    public ChatManager chatManager;
    
    [Header("UI References")]
    public TextMeshProUGUI currentSuspectNameText;
    public Button endInterrogationButton;
    public InterrogationDropZone interrogationDropZone;

    [Header("Response Settings")]
    [Tooltip("Base delay for truthful responses")]
    public float baseResponseDelay = 1f;
    [Tooltip("Delay for responses that are lies")]
    public float lieResponseDelay = 2.5f;
    [Tooltip("Speed of typewriter effect (seconds per character)")]
    public float typewriterSpeed = 0.05f;

    // Current state
    private string currentSuspectId = "";
    private Dictionary<string, CitizenConversation> conversations = new Dictionary<string, CitizenConversation>();
    private bool isWaitingForResponse = false;
    private bool isProcessingTag = false; // Lock to prevent multiple simultaneous processing

    // Events
    public System.Action<string> OnSuspectChanged;
    public System.Action<string[]> OnNewCluesExtracted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        // DialogueSystem functionality has been consolidated into InterrogationManager
        
        // Setup chat manager events
        if (chatManager != null)
        {
            // Removed: chatManager.OnPlayerMessageSent += HandleChatPlayerMessage;
            // This was causing duplicate processing - ChatManager now calls HandleTagQuestion directly
            chatManager.OnSuspectResponseReceived += HandleChatSuspectResponse;
        }
        
        // Setup end interrogation button
        if (endInterrogationButton != null)
        {
            endInterrogationButton.onClick.AddListener(EndInterrogation);
        }
    }

    private void Start()
    {
        Debug.Log("[InterrogationManager] Interrogation system initialized");
    }

    /// <summary>
    /// Switch to interrogating a different suspect
    /// </summary>
    public void SwitchToSuspect(string suspectId)
    {
        if (suspectId == currentSuspectId && !string.IsNullOrEmpty(currentSuspectId))
        {
            Debug.Log($"[InterrogationManager] Already interrogating suspect: {suspectId}");
            return; // Already interrogating this suspect
        }
        
        Debug.Log($"[InterrogationManager] Switching to suspect: {suspectId}");
        
        // Tell ChatManager to switch suspects (this handles saving/loading conversation state)
        if (chatManager != null)
        {
            chatManager.SwitchToSuspect(suspectId);
        }
        
        // Update current suspect
        string previousSuspectId = currentSuspectId;
        currentSuspectId = suspectId;
        
        // Create conversation if it doesn't exist
        if (!conversations.ContainsKey(suspectId))
        {
            Citizen citizen = FindCitizenById(suspectId);
            if (citizen != null)
            {
                // Reset the citizen's conversation state to ensure clean start
                citizen.ResetConversationState();
                Debug.Log($"[InterrogationManager] Reset conversation state for citizen: {citizen.FullName}");
                
                conversations[suspectId] = new CitizenConversation { citizen = citizen };
                Debug.Log($"[InterrogationManager] Created new conversation for suspect: {suspectId}");
            }
            else
            {
                Debug.LogError($"[InterrogationManager] Could not find citizen data for suspect: {suspectId}");
                return;
            }
        }
        
        // Update UI
        if (currentSuspectNameText != null)
        {
            currentSuspectNameText.text = conversations[suspectId].citizen.FullName;
        }
        
        // Reset waiting state when switching suspects
        isWaitingForResponse = false;
        
        // If this is the first suspect or we're switching from no suspect, ensure drop zone is shown
        if (string.IsNullOrEmpty(previousSuspectId) || chatManager != null && !chatManager.HasConversationHistory(suspectId))
        {
            // Show drop zone for new interrogation after a small delay
            StartCoroutine(ShowDropZoneAfterDelay());
        }
        
        // Trigger event
        OnSuspectChanged?.Invoke(suspectId);
        
        Debug.Log($"[InterrogationManager] Now interrogating: {conversations[suspectId].citizen.FullName}");
    }
    
    /// <summary>
    /// Show drop zone with small delay to ensure UI is ready
    /// </summary>
    private IEnumerator ShowDropZoneAfterDelay()
    {
        yield return new WaitForSeconds(0.1f);
        if (chatManager != null)
        {
            chatManager.ShowDropZone();
        }
    }

    /// <summary>
    /// Find citizen by ID from the current case
    /// </summary>
    private Citizen FindCitizenById(string citizenId)
    {
        // First try to get current case from SuspectManager
        SuspectManager suspectManager = SuspectManager.Instance;
        if (suspectManager != null)
        {
            var currentCase = suspectManager.GetCurrentCase();
            if (currentCase != null && currentCase.suspects != null)
            {
                foreach (var suspect in currentCase.suspects)
                {
                    if (suspect != null && suspect.citizenID == citizenId)
                    {
                        return suspect;
                    }
                }
            }
        }
        
        // Fallback: Try to find from CaseManager's all cases
        CaseManager caseManager = CaseManager.Instance;
        if (caseManager != null)
        {
            var allCases = new List<Case>();
            allCases.AddRange(caseManager.GetCoreCasesInOrder());
            allCases.AddRange(caseManager.GetSecondaryCases());
            
            foreach (var caseData in allCases)
            {
                if (caseData.suspects != null)
                {
                    foreach (var suspect in caseData.suspects)
                    {
                        if (suspect != null && suspect.citizenID == citizenId)
                        {
                            return suspect;
                        }
                    }
                }
            }
        }
        
        return null;
    }

    // REMOVED: HandleTagQuestion - was redundant with ProcessTagResponseDirectly
    // All processing now goes through ProcessTagResponseDirectly only

    /// <summary>
    /// Process tag response directly from ChatManager (bypasses checks that would cause circular calls)
    /// </summary>
    public void ProcessTagResponseDirectly(string tagContent)
    {
        Debug.Log($"[InterrogationManager] ProcessTagResponseDirectly called with: '{tagContent}'");
        
        if (string.IsNullOrEmpty(currentSuspectId))
        {
            Debug.LogError("[InterrogationManager] No suspect selected!");
            return;
        }

        if (isWaitingForResponse)
        {
            Debug.LogWarning("[InterrogationManager] Already waiting for response, ignoring tag");
            return;
        }

        if (isProcessingTag)
        {
            Debug.LogWarning($"[InterrogationManager] Already processing tag, ignoring duplicate call for '{tagContent}'");
            return;
        }

        isProcessingTag = true;

        // Process through chat system if available
        if (chatManager != null)
        {
            StartCoroutine(ProcessSuspectResponseWithChat(tagContent));
        }
        else
        {
            // Fallback for if ChatManager is not available
            StartCoroutine(ProcessSuspectResponse(tagContent));
        }
    }

    /// <summary>
    /// Process suspect response using ChatManager
    /// </summary>
    private IEnumerator ProcessSuspectResponseWithChat(string tagContent)
    {
        isWaitingForResponse = true;
        Debug.Log($"[InterrogationManager] Processing tag '{tagContent}' for suspect '{currentSuspectId}'");

        if (!conversations.ContainsKey(currentSuspectId))
        {
            Debug.LogError($"[InterrogationManager] No conversation found for suspect: {currentSuspectId}");
            isWaitingForResponse = false;
            isProcessingTag = false;
            yield break;
        }

        Citizen citizen = conversations[currentSuspectId].citizen;
        if (citizen == null)
        {
            Debug.LogError($"[InterrogationManager] No citizen data for suspect: {currentSuspectId}");
            isWaitingForResponse = false;
            isProcessingTag = false;
            yield break;
        }

        // Store the message in conversation history
        var playerMessage = new ConversationMessage(tagContent, true);
        conversations[currentSuspectId].messages.Add(playerMessage);

        // Get response from citizen data directly (no more DialogueSystem needed)
        TagResponse tagResponse = citizen.GetResponseForTag(tagContent);
        
        Debug.Log($"[InterrogationManager] Tag response: '{tagResponse.GetCombinedResponse()}'");
        Debug.Log($"[InterrogationManager] Is lie: {tagResponse.isLie}");
        
        // Calculate response delay (moved from DialogueSystem)
        float delay = CalculateResponseDelay(tagResponse);
        yield return new WaitForSeconds(delay);
        
        // Add to conversation history
        var message = new ConversationMessage(tagResponse.GetCombinedResponse(), false, tagResponse.isLie, ResponseType.Normal);
        conversations[currentSuspectId].messages.Add(message);
        
        // Send response to ChatManager for display
        if (chatManager != null)
        {
            // Use enhanced method if clickable clues are available
            if (tagResponse.HasClickableClues)
            {
                chatManager.ShowSuspectResponseWithClickableClues(tagResponse.GetCombinedResponse(), tagResponse.clickableClues);
            }
            else
            {
                // Fallback to simple response
                chatManager.ShowSuspectResponse(tagResponse.GetCombinedResponse());
            }
        }
        
        Debug.Log($"[InterrogationManager] Sent response to ChatManager: '{tagResponse.GetCombinedResponse()}'");
        
        isWaitingForResponse = false;
        isProcessingTag = false; // Release lock
    }

    /// <summary>
    /// Calculate response delay based on response properties (moved from DialogueSystem)
    /// </summary>
    private float CalculateResponseDelay(TagResponse response)
    {
        // Manual override takes precedence
        if (response.responseDelayOverride > 0)
            return response.responseDelayOverride;
        
        // Base calculation on lie/truth using configurable delays
        float delay = response.isLie ? lieResponseDelay : baseResponseDelay;
        
        // Add small random variation for naturalness
        delay += UnityEngine.Random.Range(-0.2f, 0.3f);
        
        return Mathf.Max(0.5f, delay); // Ensure minimum delay
    }

    /// <summary>
    /// Legacy fallback method - process suspect response without ChatManager
    /// </summary>
    private IEnumerator ProcessSuspectResponse(string tagContent)
    {
        isWaitingForResponse = true;
        Debug.Log($"[InterrogationManager] Processing tag '{tagContent}' for suspect '{currentSuspectId}'");

        if (!conversations.ContainsKey(currentSuspectId))
        {
            Debug.LogError($"[InterrogationManager] No conversation found for suspect: {currentSuspectId}");
            isWaitingForResponse = false;
            isProcessingTag = false;
            yield break;
        }

        Citizen citizen = conversations[currentSuspectId].citizen;
        if (citizen == null)
        {
            Debug.LogError($"[InterrogationManager] No citizen data for suspect: {currentSuspectId}");
            isWaitingForResponse = false;
            isProcessingTag = false;
            yield break;
        }

        // Add player message to conversation
        var playerMessage = new ConversationMessage(tagContent, true);
        conversations[currentSuspectId].messages.Add(playerMessage);
        Debug.Log($"[InterrogationManager] Message added to conversation - handled by ChatManager");

        // Get response from citizen data directly
        Debug.Log($"[InterrogationManager] ProcessSuspectResponse: About to call GetResponseForTag for '{tagContent}'");
        TagResponse tagResponse = citizen.GetResponseForTag(tagContent);
        Debug.Log($"[InterrogationManager] ProcessSuspectResponse: GetResponseForTag returned: '{tagResponse.GetCombinedResponse()}'");
        
        Debug.Log($"[InterrogationManager] Tag response: '{tagResponse.GetCombinedResponse()}'");
        Debug.Log($"[InterrogationManager] Is lie: {tagResponse.isLie}");
        
        // Calculate response delay
        float delay = CalculateResponseDelay(tagResponse);
        yield return new WaitForSeconds(delay);
        
        // Add to conversation history
        var message = new ConversationMessage(tagResponse.GetCombinedResponse(), false, tagResponse.isLie, ResponseType.Normal);
        conversations[currentSuspectId].messages.Add(message);
        
        isWaitingForResponse = false;
        isProcessingTag = false; // Release lock
    }

    public void EndInterrogation()
    {
        Debug.Log("[InterrogationManager] Ending interrogation");
        
        // Reset interrogation state
        isWaitingForResponse = false;
        
        // Clear the current suspect
        currentSuspectId = "";
        
        // Clear the chat interface
        if (chatManager != null)
        {
            chatManager.ClearChat();
            chatManager.HideDropZoneImmediate();
        }
        
        // Close notebook if open
        NotebookManager notebookManager = FindFirstObjectByType<NotebookManager>();
        if (notebookManager != null)
        {
            notebookManager.CloseNotebook();
        }
        
        // Update UI
        if (currentSuspectNameText != null)
        {
            currentSuspectNameText.text = "";
        }
        
        Debug.Log("[InterrogationManager] Interrogation ended - state reset");
    }

    public List<ConversationMessage> GetConversationHistory(string suspectId)
    {
        if (conversations.ContainsKey(suspectId))
        {
            return conversations[suspectId].messages;
        }
        return new List<ConversationMessage>();
    }

    public void ClearSuspectHistory(string suspectId)
    {
        if (conversations.ContainsKey(suspectId))
        {
            conversations[suspectId].messages.Clear();
            Debug.Log($"[InterrogationManager] Cleared conversation history for suspect: {suspectId}");
            
            // Clear chat manager as well
            if (chatManager != null)
            {
                chatManager.ClearChat();
            }
        }
    }

    public void ClearAllConversationHistories()
    {
        foreach (var conversation in conversations.Values)
        {
            conversation.messages.Clear();
        }
        Debug.Log("[InterrogationManager] Cleared all conversation histories");
        
        // Clear chat manager as well
        if (chatManager != null)
        {
            chatManager.ClearAllConversationStates();
        }
    }

    // Properties
    public bool IsWaitingForResponse => isWaitingForResponse;
    public string CurrentSuspectId => currentSuspectId;

    public CitizenConversation GetCurrentConversation()
    {
        if (!string.IsNullOrEmpty(currentSuspectId) && conversations.ContainsKey(currentSuspectId))
        {
            return conversations[currentSuspectId];
        }
        return null;
    }

    // REMOVED: HandleChatPlayerMessage and ProcessSuspectResponseForChat were redundant duplicates
    // All chat processing now goes through ProcessTagResponseDirectly -> ProcessSuspectResponseWithChat

    private void HandleChatSuspectResponse(string responseText)
    {
        Debug.Log($"[InterrogationManager] Received suspect response from ChatManager: {responseText}");
        isWaitingForResponse = false;
    }

    private void OnDestroy()
    {
        // Cleanup events
        if (chatManager != null)
        {
            // Removed: chatManager.OnPlayerMessageSent -= HandleChatPlayerMessage;
            chatManager.OnSuspectResponseReceived -= HandleChatSuspectResponse;
        }
        
        if (endInterrogationButton != null)
        {
            endInterrogationButton.onClick.RemoveListener(EndInterrogation);
        }
    }
} 