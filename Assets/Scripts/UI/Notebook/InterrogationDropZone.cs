using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Handles the drop zone for detective tags during interrogation
/// </summary>
public class InterrogationDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Settings")]
    public Image frameImage; // The frame that shows drop zone
    public TextMeshProUGUI promptText; // "Tell me about ''" text
    public Color normalFrameColor = Color.white;
    public Color highlightFrameColor = Color.yellow;
    public Color acceptFrameColor = Color.green;
    
    [Header("Animation Settings")]
    public float hoverScaleFactor = 1.05f;
    public float hoverAnimationDuration = 0.2f;
    public float colorTransitionDuration = 0.3f;
    public Ease hoverEase = Ease.OutQuart;
    
    [Header("Drop Settings")]
    public bool acceptAnyTagType = true;
    public string[] acceptedTagTypes = { "person", "location", "date", "time", "item", "value", "object" };
    
    // State
    private bool isHighlighted = false;
    private bool hasDroppedTag = false;
    private Vector3 originalScale;
    private Color originalFrameColor;
    private string currentTagContent = "";
    private string currentTagType = "";
    private string currentTagId = "";
    private GameObject droppedTagObject; // Keep reference to the dropped tag

    // Events
    public System.Action<string, string, string, string> OnTagDroppedEvent; // content, type, questionText, tagId
    
    [Header("Manager Reference")]
    public InterrogationManager interrogationManager;
    
    private void Awake()
    {
        // Store original state
        originalScale = transform.localScale;
        originalFrameColor = frameImage != null ? frameImage.color : normalFrameColor;
        
        // Initialize prompt text
        UpdatePromptText();
    }
    
    /// <summary>
    /// Update prompt text based on tag being dragged
    /// </summary>
    public void UpdatePromptForTag(string tagContent, string tagType, string tagId = null)
    {
        if (interrogationManager == null) return;

        // Get current suspect
        string currentSuspectId = interrogationManager.CurrentSuspectId;
        if (string.IsNullOrEmpty(currentSuspectId)) return;

        // Find the citizen data
        var conversation = interrogationManager.GetCurrentConversation();
        if (conversation?.citizen == null) return;

        // Get the appropriate question for this tag — use tagId for lookup, tagContent for display
        string questionText = conversation.citizen.GetQuestionForTag(tagId ?? tagContent, tagType, tagContent);
        
        // Update prompt text
        if (promptText != null)
        {
            promptText.text = questionText;
        }
    }
    
    /// <summary>
    /// Reset prompt text to default
    /// </summary>
    public void ResetPromptText()
    {
        if (promptText != null)
        {
            promptText.text = "Drop a tag here to ask about it";
        }
    }
    
    /// <summary>
    /// Check if this drop zone can accept the given tag
    /// </summary>
    public bool CanAcceptDrop(DraggableTag tag)
    {
        if (tag == null) return false;
        
        // Check if we accept any tag type or specific types
        if (acceptAnyTagType) return true;
        
        string tagType = tag.GetTagType();
        foreach (string acceptedType in acceptedTagTypes)
        {
            if (tagType.Equals(acceptedType, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the world position where dropped tags should snap to
    /// </summary>
    public Vector3 GetDropPosition()
    {
        return frameImage != null ? frameImage.transform.position : transform.position;
    }
    
    /// <summary>
    /// Called when a tag is successfully dropped on this zone
    /// </summary>
    public void OnTagDropped(DraggableTag tag, GameObject draggedObject)
    {
        if (tag == null) return;
        
        // Store tag information
        currentTagContent = tag.GetTagContent();
        currentTagType = tag.GetTagType();
        currentTagId = tag.GetTagID() ?? currentTagContent;
        hasDroppedTag = true;
        
        // Clean up any existing dropped tag first
        if (droppedTagObject != null)
        {
            DOTween.Kill(droppedTagObject, true);
            Destroy(droppedTagObject);
        }
        
        // Get the current question text FIRST (before any cleanup that might reset prompts)
        string questionText = (promptText != null) ? promptText.text : $"Tell me about '{currentTagContent}'";
        
        // If we somehow still have the default prompt text, try to get a proper question
        if (questionText == "Drop a tag here to ask about it" || questionText == "Tell me about ''" || string.IsNullOrEmpty(questionText))
        {
            Debug.LogWarning($"[InterrogationDropZone] Captured default prompt text: '{questionText}', trying fallback");
            // Try to get a proper question from the interrogation manager
            if (interrogationManager != null)
            {
                var conversation = interrogationManager.GetCurrentConversation();
                if (conversation?.citizen != null)
                {
                    questionText = conversation.citizen.GetQuestionForTag(currentTagId, currentTagType, currentTagContent);
                    Debug.Log($"[InterrogationDropZone] Using fallback question: '{questionText}'");
                }
            }
            
            // Final fallback
            if (questionText == "Drop a tag here to ask about it" || questionText == "Tell me about ''")
            {
                questionText = $"Tell me about '{currentTagContent}'";
            }
        }
        
        Debug.Log($"[InterrogationDropZone] Final captured question text: '{questionText}'");
        
        // For the chat flow, we don't keep the tag in the frame
        // Instead, we immediately clean up the dragged object and let the conversation handle it
        if (draggedObject != null)
        {
            DOTween.Kill(draggedObject, true);
            Destroy(draggedObject);
        }
        
        // Restore the original tag in the notebook (this will reset prompts, so do it AFTER capturing)
        if (tag != null)
        {
            tag.RestoreOriginalTagOnly(); // Restore the original tag for future use
        }
        
        // Update visual state briefly (will be hidden when conversation starts)
        SetFrameColor(acceptFrameColor);
        UpdatePromptText();
        
        // Trigger event
        OnTagDroppedEvent?.Invoke(currentTagContent, currentTagType, questionText, currentTagId);
        
        Debug.Log($"[InterrogationDropZone] Tag dropped: {currentTagContent} ({currentTagType})");
    }
    
    /// <summary>
    /// Clear the current dropped tag
    /// </summary>
    public void ClearDroppedTag()
    {
        // Clean up the dropped tag object
        if (droppedTagObject != null)
        {
            DOTween.Kill(droppedTagObject, true);
            Destroy(droppedTagObject);
            droppedTagObject = null;
        }
        
        hasDroppedTag = false;
        currentTagContent = "";
        currentTagType = "";
        currentTagId = "";
        
        SetFrameColor(originalFrameColor);
        UpdatePromptText();
        
        Debug.Log("[InterrogationDropZone] Cleared dropped tag");
    }
    
    /// <summary>
    /// Hide the prompt text and frame during conversation
    /// </summary>
    public void HidePromptAndFrame()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
        
        if (frameImage != null)
        {
            frameImage.gameObject.SetActive(false);
        }
        
        Debug.Log("[InterrogationDropZone] Hidden prompt and frame for conversation");
    }
    
    /// <summary>
    /// Show the prompt text and frame after conversation
    /// </summary>
    public void ShowPromptAndFrame()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(true);
        }
        
        if (frameImage != null)
        {
            frameImage.gameObject.SetActive(true);
        }
        
        Debug.Log("[InterrogationDropZone] Showed prompt and frame for next question");
    }
    
    /// <summary>
    /// Get the currently dropped tag content
    /// </summary>
    public string GetDroppedTagContent() => currentTagContent;
    
    /// <summary>
    /// Get the currently dropped tag type
    /// </summary>
    public string GetDroppedTagType() => currentTagType;
    
    /// <summary>
    /// Check if a tag has been dropped
    /// </summary>
    public bool HasDroppedTag() => hasDroppedTag;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            
            // Scale up animation
            transform.DOScale(originalScale * hoverScaleFactor, hoverAnimationDuration)
                .SetEase(hoverEase);
            
            // Color change animation (only if no tag is dropped)
            if (!hasDroppedTag)
            {
                SetFrameColor(highlightFrameColor);
            }
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            
            // Scale down animation
            transform.DOScale(originalScale, hoverAnimationDuration)
                .SetEase(hoverEase);
            
            // Color revert animation (only if no tag is dropped)
            if (!hasDroppedTag)
            {
                SetFrameColor(originalFrameColor);
            }
        }
    }
    
    private void SetFrameColor(Color targetColor)
    {
        if (frameImage != null)
        {
            frameImage.DOColor(targetColor, colorTransitionDuration);
        }
    }
    
    private void UpdatePromptText()
    {
        if (promptText == null) return;
        
        if (hasDroppedTag)
        {
            promptText.text = $"Tell me about '{currentTagContent}'";
        }
        else
        {
            promptText.text = "Tell me about ''";
        }
    }
    
    private void OnDestroy()
    {
        // Clean up dropped tag
        if (droppedTagObject != null)
        {
            DOTween.Kill(droppedTagObject, true);
            Destroy(droppedTagObject);
        }
        
        // Kill all tweens targeting this object and its components immediately
        DOTween.Kill(this, true); // Complete all tweens targeting this MonoBehaviour
        DOTween.Kill(transform, true); // Complete all tweens targeting the transform
        DOTween.Kill(gameObject, true); // Complete all tweens targeting the GameObject
        
        // Also specifically target components that might have tweens
        if (frameImage != null)
            DOTween.Kill(frameImage, true);
            
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            DOTween.Kill(rectTransform, true);
    }
} 