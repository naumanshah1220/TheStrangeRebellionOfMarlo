using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Handles the drop zone for case cards during case submission
/// </summary>
public class CaseSubmissionZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Settings")]
    public Image frameImage; // The frame that shows drop zone
    public TextMeshProUGUI promptText; // "Drop case here to submit" text
    public Color normalFrameColor = Color.white;
    public Color dragHoverFrameColor = Color.yellow;
    public Color canAcceptFrameColor = Color.green;
    public Color highlightFrameColor = Color.cyan; // For mouse hover
    
    [Header("Animation Settings")]
    public float hoverScaleFactor = 1.05f;
    public float hoverAnimationDuration = 0.2f;
    public float colorTransitionDuration = 0.3f;
    public Ease hoverEase = Ease.OutQuart;
    
    // State
    public enum HighlightState { Normal, MouseHover, DragHover, CanAccept }
    private HighlightState currentState = HighlightState.Normal;
    private bool isHighlighted = false;
    private Vector3 originalScale;
    private Color originalFrameColor;
    
    // Events
    public System.Action<Card> OnCaseSubmittedEvent;
    
    private void Awake()
    {
        // Store original state
        originalScale = transform.localScale;
        originalFrameColor = frameImage != null ? frameImage.color : normalFrameColor;
        
        // Initialize prompt text
        UpdatePromptText();
    }
    
    /// <summary>
    /// Check if this drop zone can accept the given card
    /// </summary>
    public bool CanAcceptDrop(Card card)
    {
        if (card == null) return false;
        
        // Only accept case cards that can be submitted
        return card.mode == CardMode.Case && card.canBeSubmitted;
    }
    
    /// <summary>
    /// Get the world position where dropped cards should snap to
    /// </summary>
    public Vector3 GetDropPosition()
    {
        return frameImage != null ? frameImage.transform.position : transform.position;
    }
    
    /// <summary>
    /// Called when a case card is successfully dropped on this zone
    /// </summary>
    public void OnCaseDropped(Card caseCard)
    {
        if (caseCard == null || !CanAcceptDrop(caseCard)) return;
        
        Debug.Log($"[CaseSubmissionZone] Case '{caseCard.name}' submitted successfully!");
        
        // Update visual state briefly
        SetFrameColor(canAcceptFrameColor);
        
        // Trigger event
        OnCaseSubmittedEvent?.Invoke(caseCard);
    }
    
    /// <summary>
    /// Set the highlight state (called by DragManager during card dragging)
    /// </summary>
    public void SetHighlightState(HighlightState state)
    {
        if (currentState == state) return;
        

        
        currentState = state;
        
        Color targetColor = normalFrameColor;
        float targetScale = 1f;
        
        switch (state)
        {
            case HighlightState.Normal:
                targetColor = normalFrameColor;
                targetScale = 1f;

                break;
            case HighlightState.MouseHover:
                targetColor = highlightFrameColor;
                targetScale = hoverScaleFactor;

                break;
            case HighlightState.DragHover:
                targetColor = dragHoverFrameColor;
                targetScale = hoverScaleFactor;

                break;
            case HighlightState.CanAccept:
                targetColor = canAcceptFrameColor;
                targetScale = hoverScaleFactor * 1.1f; // Slightly larger when can accept

                break;
        }
        
        SetFrameColor(targetColor);
        transform.DOScale(originalScale * targetScale, hoverAnimationDuration).SetEase(hoverEase);
    }
    
    /// <summary>
    /// Update prompt text
    /// </summary>
    public void UpdatePromptText()
    {
        if (promptText != null)
        {
            promptText.text = "Drop case here to submit";
        }
    }
    
    /// <summary>
    /// Set frame color with smooth transition
    /// </summary>
    private void SetFrameColor(Color targetColor)
    {
        if (frameImage != null)
        {
            frameImage.DOColor(targetColor, colorTransitionDuration);
        }
    }
    
    /// <summary>
    /// Check if pointer is over this drop zone
    /// </summary>
    public bool IsPointerOverZone(Vector2 screenPosition, Camera camera)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null) 
        {

            return false;
        }
        
        Vector2 localPoint;
        bool result = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, 
            screenPosition, 
            camera, 
            out localPoint) && rectTransform.rect.Contains(localPoint);
            
        // Debug logging

        
        return result;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isHighlighted)
        {
            isHighlighted = true;
            SetFrameColor(highlightFrameColor);
            
            // Scale animation
            transform.DOScale(originalScale * hoverScaleFactor, hoverAnimationDuration)
                .SetEase(hoverEase);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        if (isHighlighted)
        {
            isHighlighted = false;
            SetFrameColor(originalFrameColor);
            
            // Scale back
            transform.DOScale(originalScale, hoverAnimationDuration)
                .SetEase(hoverEase);
        }
    }


} 