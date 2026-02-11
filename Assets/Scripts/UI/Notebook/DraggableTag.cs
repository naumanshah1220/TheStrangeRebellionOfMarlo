using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Handles dragging of detective tags for interrogation system
/// </summary>
public class DraggableTag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public float dragScale = 1.2f; // Scale when dragging
    public float dragAlpha = 0.8f; // Alpha when dragging
    public LayerMask dropLayerMask = -1; // What layers can we drop on
    
    [Header("Animation Settings")]
    public float scaleAnimationDuration = 0.2f;
    public float snapAnimationDuration = 0.3f;
    public Ease scaleEase = Ease.OutBack;
    public Ease snapEase = Ease.OutQuart;
    
    // Original state
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Transform originalParent;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;
    private float originalAlpha;
    
    // Drag state
    private GameObject dragCopy;
    private Canvas dragCanvas;
    private bool isDragging = false;
    private Camera mainCamera;
    
    // Tag data
    private string tagContent;
    private string tagType;
    private string tagId;
    private string cachedQuestion = ""; // Cache the question for this drag session
    
    // Components
    private TextMeshProUGUI textComponent;
    
    private void Awake()
    {
        // Get components
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        
        // Store original state
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalAlpha = canvasGroup.alpha;
        
        // Get camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
    }
    
    /// <summary>
    /// Initialize the tag with content and type
    /// </summary>
    public void Initialize(string content, string type, string id = null)
    {
        tagContent = content;
        tagType = type;
        tagId = id;
        
        if (textComponent != null)
            textComponent.text = content;
    }
    
    /// <summary>
    /// Get the tag content
    /// </summary>
    public string GetTagContent() => tagContent;

    /// <summary>
    /// Get the tag's originating clue ID
    /// </summary>
    public string GetTagID() => tagId;
    
    /// <summary>
    /// Get the tag type
    /// </summary>
    public string GetTagType() => tagType;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isDragging) return;
        
        isDragging = true;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        // Clear cached question for fresh drag session
        cachedQuestion = "";
        
        // Create drag copy
        CreateDragCopy();
        
        // Ensure drag copy is visible and properly positioned
        if (dragCopy != null)
        {
            // Force immediate positioning
            PositionDragCopyAtMouse();
            
            // Ensure the drag copy is active and visible
            dragCopy.SetActive(true);
            
            // Force canvas update to ensure proper rendering
            Canvas.ForceUpdateCanvases();
            
            Debug.Log($"[DraggableTag] Created drag copy at position: {dragCopy.transform.position}");
        }
        
        // Hide original tag after drag copy is confirmed to be visible
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragCopy == null) return;
        
        // Update drag copy position to follow mouse
        PositionDragCopyAtMouse();
        
        // Update drop zone prompt text based on current hover
        GameObject dropZoneObject = GetDropZoneUnderPointer(eventData);
        UpdateDropZonePrompt(dropZoneObject);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("[DraggableTag] OnEndDrag called");
        
        if (!isDragging) 
        {
            Debug.Log("[DraggableTag] Not dragging, ignoring OnEndDrag");
            return;
        }
        
        isDragging = false;
        
        // Check if we're over a valid drop zone
        GameObject dropZoneObject = GetDropZoneUnderPointer(eventData);
        
        if (dropZoneObject != null)
        {
            Debug.Log($"[DraggableTag] Found drop zone: {dropZoneObject.name}, animating to it");
            // Successful drop - animate to drop zone
            // DON'T reset prompts yet - let the drop zone handle it after capturing the question
            AnimateToDropZone(dropZoneObject);
        }
        else
        {
            Debug.Log("[DraggableTag] No drop zone found, returning to original position");
            // Failed drop - reset prompts and return to original position
            UpdateDropZonePrompt(null);
            cachedQuestion = "";
            ReturnToOriginalPosition();
        }
    }
    
    private void CreateDragCopy()
    {
        // Find or create drag canvas
        dragCanvas = FindDragCanvas();
        
        // Create copy of this tag
        dragCopy = Instantiate(gameObject, dragCanvas.transform);
        
        // Remove this script from copy to prevent recursive dragging
        DraggableTag copyDragScript = dragCopy.GetComponent<DraggableTag>();
        if (copyDragScript != null)
            Destroy(copyDragScript);
        
        // Set up drag copy appearance
        CanvasGroup copyCanvasGroup = dragCopy.GetComponent<CanvasGroup>();
        if (copyCanvasGroup != null)
        {
            copyCanvasGroup.alpha = dragAlpha;
            copyCanvasGroup.blocksRaycasts = false; // Don't block raycasts while dragging
        }
        
        // Scale up the drag copy
        dragCopy.transform.localScale = originalScale * dragScale;
        
        // Position the drag copy properly based on canvas render mode
        PositionDragCopyAtMouse();
    }
    
    private void PositionDragCopyAtMouse()
    {
        if (dragCopy == null || dragCanvas == null) return;
        
        Vector3 mousePosition = Input.mousePosition;
        
        if (dragCanvas.renderMode == RenderMode.WorldSpace)
        {
            // For WorldSpace canvas, convert screen position to world position using canvas camera
            Camera canvasCamera = dragCanvas.worldCamera;
            if (canvasCamera == null) canvasCamera = Camera.main;
            
            if (canvasCamera != null)
            {
                // Calculate the proper world position for the drag copy
                float distanceFromCamera = Vector3.Distance(canvasCamera.transform.position, dragCanvas.transform.position);
                Vector3 worldPosition = canvasCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera));
                
                // Transform to local space of the drag canvas
                Vector3 localPosition = dragCanvas.transform.InverseTransformPoint(worldPosition);
                dragCopy.transform.localPosition = localPosition;
            }
            else
            {
                // Fallback: use original tag position as starting point
                dragCopy.transform.position = transform.position;
            }
        }
        else
        {
            // For ScreenSpace canvases, use RectTransformUtility for proper positioning
            RectTransform dragCopyRect = dragCopy.GetComponent<RectTransform>();
            if (dragCopyRect != null)
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragCanvas.GetComponent<RectTransform>(),
                    mousePosition,
                    dragCanvas.worldCamera,
                    out localPoint))
                {
                    dragCopyRect.localPosition = localPoint;
                }
                else
                {
                    // Fallback: convert using transform
                    dragCopyRect.localPosition = dragCanvas.transform.InverseTransformPoint(
                        mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f)));
                }
            }
        }
    }
    
    private Canvas FindDragCanvas()
    {
        // Clean up any leftover drag canvases first
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "DragCanvas")
            {
                // Check if this canvas has any children (active drag operations)
                if (canvas.transform.childCount == 0)
                {
                    // Empty drag canvas, clean it up
                    Destroy(canvas.gameObject);
                }
                else
                {
                    // Canvas in use, return it
                    return canvas;
                }
            }
        }
        
        // Find the original canvas to match its settings
        Canvas originalCanvas = GetComponentInParent<Canvas>();
        if (originalCanvas == null)
        {
            Debug.LogWarning("[DraggableTag] Could not find parent canvas to match settings");
            originalCanvas = FindFirstObjectByType<Canvas>();
        }
        
        // Create new drag canvas matching the original canvas settings
        GameObject dragCanvasGO = new GameObject("DragCanvas");
        Canvas dragCanvas = dragCanvasGO.AddComponent<Canvas>();
        
        if (originalCanvas != null)
        {
            // Match the original canvas render mode and settings
            dragCanvas.renderMode = originalCanvas.renderMode;
            dragCanvas.worldCamera = originalCanvas.worldCamera;
            dragCanvas.planeDistance = originalCanvas.planeDistance;
            dragCanvas.sortingLayerID = originalCanvas.sortingLayerID;
            dragCanvas.sortingOrder = originalCanvas.sortingOrder + 100; // Higher sorting order
            
            // Match the scale and position for WorldSpace canvases
            if (originalCanvas.renderMode == RenderMode.WorldSpace)
            {
                dragCanvasGO.transform.localScale = originalCanvas.transform.localScale;
                dragCanvasGO.transform.position = originalCanvas.transform.position;
                dragCanvasGO.transform.rotation = originalCanvas.transform.rotation;
            }
            
            // Copy CanvasScaler if it exists
            CanvasScaler originalScaler = originalCanvas.GetComponent<CanvasScaler>();
            if (originalScaler != null)
            {
                CanvasScaler scaler = dragCanvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = originalScaler.uiScaleMode;
                scaler.referenceResolution = originalScaler.referenceResolution;
                scaler.screenMatchMode = originalScaler.screenMatchMode;
                scaler.matchWidthOrHeight = originalScaler.matchWidthOrHeight;
                scaler.referencePixelsPerUnit = originalScaler.referencePixelsPerUnit;
            }
        }
        else
        {
            // Fallback to screen space overlay
            dragCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            dragCanvas.sortingOrder = 1000;
        }
        
        dragCanvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log($"[DraggableTag] Created new drag canvas with render mode: {dragCanvas.renderMode}");
        
        return dragCanvas;
    }
    
    private GameObject GetDropZoneUnderPointer(PointerEventData eventData)
    {
        // Raycast to find drop zones (looking for objects with InterrogationDropZone or CitizenDatabaseDropZone component)
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            // Check if this object or its parents have an InterrogationDropZone component
            var dropZone = result.gameObject.GetComponent("InterrogationDropZone");
            if (dropZone != null)
            {
                return result.gameObject;
            }
                
            // Also check parent objects
            var parentDropZone = result.gameObject.GetComponentInParent(System.Type.GetType("InterrogationDropZone"));
            if (parentDropZone != null)
            {
                return parentDropZone.gameObject;
            }
            
            // Check for CitizenDatabaseDropZone component
            var citizenDropZone = result.gameObject.GetComponent<CitizenDatabaseDropZone>();
            if (citizenDropZone != null)
            {
                return result.gameObject;
            }
            
            // Also check parent objects for CitizenDatabaseDropZone
            citizenDropZone = result.gameObject.GetComponentInParent<CitizenDatabaseDropZone>();
            if (citizenDropZone != null)
            {
                return citizenDropZone.gameObject;
            }
            
            // TEMPORARY: Also check for objects named "interrogation" for quick testing
            if (result.gameObject.name.ToLower().Contains("interrogation"))
            {
                return result.gameObject;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Update the prompt text of drop zones when hovering
    /// </summary>
    private void UpdateDropZonePrompt(GameObject dropZoneObject)
    {
        // Find all InterrogationDropZone components in the scene
        InterrogationDropZone[] dropZones = FindObjectsByType<InterrogationDropZone>(FindObjectsSortMode.None);
        
        foreach (var dropZone in dropZones)
        {
            if (dropZoneObject != null && dropZone.gameObject == dropZoneObject)
            {
                // This is the drop zone we're hovering - use cached question or generate it once
                if (string.IsNullOrEmpty(cachedQuestion))
                {
                    cachedQuestion = GetQuestionForTag(dropZone);
                    Debug.Log($"[DraggableTag] Generated and cached question: '{cachedQuestion}'");
                }
                
                // Set the cached question directly
                if (dropZone.promptText != null)
                {
                    dropZone.promptText.text = cachedQuestion;
                    Debug.Log($"[DraggableTag] Set drop zone prompt to: '{cachedQuestion}'");
                }
            }
            else
            {
                // Reset other drop zones to default prompt
                dropZone.ResetPromptText();
            }
        }
        
        // For CitizenDatabaseDropZone, we don't need to update prompts since they show field labels
        // But we could add visual feedback here if needed
    }
    
    /// <summary>
    /// Get the question for this tag from the drop zone's interrogation manager
    /// </summary>
    private string GetQuestionForTag(InterrogationDropZone dropZone)
    {
        if (dropZone.interrogationManager == null) 
            return $"Tell me about '{tagContent}'";
        
        // Get current suspect
        string currentSuspectId = dropZone.interrogationManager.CurrentSuspectId;
        if (string.IsNullOrEmpty(currentSuspectId)) 
            return $"Tell me about '{tagContent}'";
        
        // Find the citizen data
        var conversation = dropZone.interrogationManager.GetCurrentConversation();
        if (conversation?.citizen == null) 
            return $"Tell me about '{tagContent}'";
        
        // Get the appropriate question for this tag (this will cache the random question)
        return conversation.citizen.GetQuestionForTag(tagContent, tagType);
    }
    
    private void AnimateToDropZone(GameObject dropZoneObject)
    {
        if (dragCopy != null && dropZoneObject != null)
        {
            // Get the actual drop position from the drop zone (should be frame image position)
            Vector3 targetPosition = dropZoneObject.transform.position; // Default fallback
            
            // Check for CitizenDatabaseDropZone first
            var citizenDropZone = dropZoneObject.GetComponent<CitizenDatabaseDropZone>();
            if (citizenDropZone != null)
            {
                // For CitizenDatabaseDropZone, use the transform position
                targetPosition = dropZoneObject.transform.position;
                
                // Animate drag copy to the drop zone and fade it out
                dragCopy.transform.DOMove(targetPosition, snapAnimationDuration)
                    .SetEase(snapEase)
                    .OnComplete(() => {
                        // Fade out the drag copy
                        CanvasGroup dragCanvasGroup = dragCopy.GetComponent<CanvasGroup>();
                        if (dragCanvasGroup != null)
                        {
                            dragCanvasGroup.DOFade(0f, 0.2f)
                                .SetEase(Ease.InQuad)
                                .OnComplete(() => {
                                    // Call OnTagDropped method directly
                                    citizenDropZone.OnTagDropped(this, dragCopy);
                                    
                                    // Restore the original tag (this will make it visible again)
                                    RestoreOriginalTagOnly();
                                });
                        }
                        else
                        {
                            // Call OnTagDropped method directly
                            citizenDropZone.OnTagDropped(this, dragCopy);
                            
                            // Restore the original tag (this will make it visible again)
                            RestoreOriginalTagOnly();
                        }
                    });
                return;
            }
            
            // Use reflection to get the precise drop position from the InterrogationDropZone
            var dropZoneComponent = dropZoneObject.GetComponent("InterrogationDropZone");
            if (dropZoneComponent != null)
            {
                var getDropPositionMethod = dropZoneComponent.GetType().GetMethod("GetDropPosition");
                if (getDropPositionMethod != null)
                {
                    var result = getDropPositionMethod.Invoke(dropZoneComponent, null);
                    if (result is Vector3 position)
                    {
                        targetPosition = position;
                    }
                }
            }
            
            // Animate drag copy directly to the frame position
            dragCopy.transform.DOMove(targetPosition, snapAnimationDuration)
                .SetEase(snapEase)
                .OnComplete(() => {
                    if (dropZoneComponent != null)
                    {
                        // Use reflection to call OnTagDropped method
                        var method = dropZoneComponent.GetType().GetMethod("OnTagDropped");
                        if (method != null)
                        {
                            // The drop zone will handle positioning and ownership of dragCopy
                            method.Invoke(dropZoneComponent, new object[] { this, dragCopy });
                        }
                    }
                    else
                    {
                        // No drop zone found, fallback to normal cleanup
                        CleanupDragCopy();
                        RestoreOriginalTag();
                    }
                });
        }
        else
        {
            // No drag copy, just restore original
            RestoreOriginalTag();
        }
    }
    
    private void ReturnToOriginalPosition()
    {
        // Destroy drag copy and restore original tag
        CleanupDragCopy();
        RestoreOriginalTag();
        

    }
    
    private void CleanupDragCopy()
    {
        if (dragCopy != null)
        {
            // Kill any running animations on the drag copy and all its components
            DOTween.Kill(dragCopy, true); // true = complete all tweens immediately
            
            // Also kill tweens on specific components that might have animations
            CanvasGroup canvasGroup = dragCopy.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                DOTween.Kill(canvasGroup, true);
            }
            
            RectTransform rectTransform = dragCopy.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                DOTween.Kill(rectTransform, true);
            }
            
            Destroy(dragCopy);
            dragCopy = null;
        }
        
        // Clean up drag canvas if it's empty
        if (dragCanvas != null && dragCanvas.transform.childCount == 0)
        {
            Debug.Log("[DraggableTag] Cleaning up empty drag canvas");
            Destroy(dragCanvas.gameObject);
            dragCanvas = null;
        }
    }
    
    private void RestoreOriginalTag()
    {
        // Restore original tag visibility and interaction
        canvasGroup.alpha = originalAlpha;
        canvasGroup.blocksRaycasts = true;
        
        // Restore sibling index
        transform.SetSiblingIndex(originalSiblingIndex);
    }
    
    /// <summary>
    /// Public method to restore the original tag without cleaning up drag copy (for InterrogationDropZone)
    /// </summary>
    public void RestoreOriginalTagOnly()
    {
        // Only restore the original tag - don't cleanup dragCopy as it's now owned by drop zone
        canvasGroup.blocksRaycasts = true;
        transform.SetSiblingIndex(originalSiblingIndex);
        
        // Fade in the original tag
        canvasGroup.DOFade(originalAlpha, 0.3f)
            .SetEase(Ease.OutQuad);
        
        // NOW it's safe to reset prompts and clear cache (after drop zone captured the question)
        UpdateDropZonePrompt(null);
        cachedQuestion = "";
        
        // Clear drag copy reference since drop zone now owns it
        dragCopy = null;
        isDragging = false;
        
        Debug.Log("[DraggableTag] Original tag restored with fade-in animation, drag copy transferred to drop zone");
    }
    
    /// <summary>
    /// Force end drag operation (called externally to cancel drags)
    /// </summary>
    public void ForceEndDrag()
    {
        if (!isDragging) return;
        
        Debug.Log("[DraggableTag] Force ending drag operation");
        
        // Reset prompts and clear cache
        UpdateDropZonePrompt(null);
        cachedQuestion = "";
        
        // Clean up and restore original tag
        ReturnToOriginalPosition();
    }
    
    private void OnDestroy()
    {
        // Kill all tweens targeting this object and its components immediately
        DOTween.Kill(this, true); // Complete all tweens targeting this MonoBehaviour
        DOTween.Kill(transform, true); // Complete all tweens targeting the transform
        DOTween.Kill(gameObject, true); // Complete all tweens targeting the GameObject
        
        // Also specifically target common components that might have tweens
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup, true);
        }
        
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            DOTween.Kill(rectTransform, true);
        }
        
        Image image = GetComponent<Image>();
        if (image != null)
        {
            DOTween.Kill(image, true);
        }
        
        // Clean up any remaining drag copy
        CleanupDragCopy();
    }
} 