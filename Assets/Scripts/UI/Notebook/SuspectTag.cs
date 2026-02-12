using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// Draggable complete suspect tag that can be dropped onto monitors
/// </summary>
public class SuspectTag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Drag Settings")]
    public float dragScale = 1.2f;
    public float dragAlpha = 0.8f;
    public LayerMask monitorLayerMask = -1;
    
    [Header("Animation Settings")]
    public float scaleAnimationDuration = 0.2f;
    public float snapAnimationDuration = 0.3f;
    public Ease scaleEase = Ease.OutBack;
    public Ease snapEase = Ease.OutQuart;
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color validDropColor = Color.green;
    public Color invalidDropColor = Color.red;
    
    [Header("Suspect Display")]
    public Image portraitImage;
    public TextMeshProUGUI citizenIdText;
    public TextMeshProUGUI firstNameText;
    public TextMeshProUGUI lastNameText;
    public Sprite unknownPortrait; // Placeholder image for missing portraits
    
    // Original state
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private float originalAlpha;
    
    // Drag state
    private GameObject dragCopy;
    private Canvas dragCanvas;
    private bool isDragging = false;
    private Camera mainCamera;
    
    // Suspect data
    private SuspectEntry.SuspectData suspectData;
    
    // Components
    private Image backgroundImage;
    private TextMeshProUGUI textComponent;
    
    private void Awake()
    {
        // Get components
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
        backgroundImage = GetComponent<Image>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        
        // Ensure we have the necessary components for dragging
        var rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[SuspectTag] Missing RectTransform component!");
        }
        
        // Ensure we have an Image component for raycasting
        if (backgroundImage == null)
        {
            Debug.LogError("[SuspectTag] Missing Image component for raycasting!");
        }
        
        // Store original state
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalAlpha = canvasGroup.alpha;
        
        // Get camera reference
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
            
        
        // Test drag functionality
        TestDragComponents();
    }
    
    /// <summary>
    /// Test if all required components for dragging are present
    /// </summary>
    private void TestDragComponents()
    {
      
        
        // Check for required components
        var rectTransform = GetComponent<RectTransform>();
        var image = GetComponent<Image>();
        var canvasGroup = GetComponent<CanvasGroup>();
        

        
        // Check if we're in a canvas
        var canvas = GetComponentInParent<Canvas>();
        
        // Check if EventSystem exists
        var eventSystem = FindFirstObjectByType<EventSystem>();
        
        // Check if GraphicRaycaster exists
        var graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
    }
    
    /// <summary>
    /// Initialize the suspect tag with data
    /// </summary>
    public void Initialize(SuspectEntry.SuspectData data)
    {
        suspectData = data;
        

        
        // Update portrait
        if (portraitImage != null)
        {
            portraitImage.sprite = data.hasPortrait ? data.portrait : unknownPortrait;
            Debug.Log($"[SuspectTag] Set portrait image: {portraitImage.sprite != null}");
        }
        else
        {
            Debug.LogWarning("[SuspectTag] portraitImage is null!");
        }
        
        // Update text fields
        if (citizenIdText != null)
        {
            citizenIdText.text = data.hasCitizenId ? data.citizenId : "???";
            Debug.Log($"[SuspectTag] Set citizen ID text: '{citizenIdText.text}'");
        }
        else
        {
            Debug.LogWarning("[SuspectTag] citizenIdText is null!");
        }
        
        if (firstNameText != null)
        {
            firstNameText.text = data.hasFirstName ? data.firstName : "???";
            Debug.Log($"[SuspectTag] Set first name text: '{firstNameText.text}'");
        }
        else
        {
            Debug.LogWarning("[SuspectTag] firstNameText is null!");
        }
        
        if (lastNameText != null)
        {
            lastNameText.text = data.hasLastName ? data.lastName : "???";
        }
        else
        {
            Debug.LogWarning("[SuspectTag] lastNameText is null!");
        }
        
        Debug.Log($"[SuspectTag] Initialized with suspect: {data.firstName} {data.lastName} (ID: {data.citizenId})");
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {

        
        if (suspectData == null || !suspectData.IsComplete())
        {
            Debug.LogWarning("[SuspectTag] Cannot drag incomplete suspect!");
            return;
        }
        
        if (isDragging) return;
        

        
        isDragging = true;
        
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
            
            Debug.Log($"[SuspectTag] Created drag copy at position: {dragCopy.transform.position}");
        }
        
        // Hide original tag after drag copy is confirmed to be visible
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || dragCopy == null) 
        {
            Debug.LogWarning($"[SuspectTag] OnDrag called but not dragging or no drag copy - isDragging: {isDragging}, dragCopy: {dragCopy != null}");
            return;
        }
        
        // Update drag copy position to follow mouse
        PositionDragCopyAtMouse();
        
        // Check for valid drop targets and provide visual feedback
        CheckForValidDropTarget(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {

        
        if (!isDragging) return;
        
        isDragging = false;
        
        // Check if dropped on a valid monitor
        InterrogationScreen targetMonitor = FindMonitorAtPosition(eventData.position);
        
        if (targetMonitor != null && CanDropOnMonitor(targetMonitor))
        {
            // Valid drop - assign suspect to monitor
            DropOnMonitor(targetMonitor);
        }
        else if (targetMonitor != null)
        {
            // Invalid drop - show error message and return to original position
            targetMonitor.ShowErrorMessage("Monitor already occupied!");
            ReturnToOriginalPosition();
        }
        else
        {
            Debug.Log($"[SuspectTag] Dropped on nothing");
            // Dropped on nothing - return to original position
            ReturnToOriginalPosition();
        }
    }
    
    /// <summary>
    /// Create a copy for dragging
    /// </summary>
    private void CreateDragCopy()
    {
        // Find or create drag canvas
        dragCanvas = FindDragCanvas();
        
        // Create copy of this tag
        dragCopy = Instantiate(gameObject, dragCanvas.transform);
        
        // Remove this script from copy to prevent recursive dragging
        SuspectTag copySuspectScript = dragCopy.GetComponent<SuspectTag>();
        if (copySuspectScript != null)
            Destroy(copySuspectScript);
        
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
    
    /// <summary>
    /// Find appropriate canvas for dragging
    /// </summary>
    private Canvas FindDragCanvas()
    {
        // Clean up any leftover drag canvases first
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.name == "SuspectDragCanvas")
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
            Debug.LogWarning("[SuspectTag] Could not find parent canvas to match settings");
            originalCanvas = FindFirstObjectByType<Canvas>();
        }
        
        // Create new drag canvas matching the original canvas settings
        GameObject dragCanvasGO = new GameObject("SuspectDragCanvas");
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
        
        
        return dragCanvas;
    }
    
    /// <summary>
    /// Position the drag copy at the mouse position
    /// </summary>
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
    
    /// <summary>
    /// Check for valid drop targets and provide visual feedback
    /// </summary>
    private void CheckForValidDropTarget(PointerEventData eventData)
    {
        if (dragCopy == null) return;
        
        InterrogationScreen targetMonitor = FindMonitorAtPosition(eventData.position);
        Image dragCopyImage = dragCopy.GetComponent<Image>();
        
        if (dragCopyImage != null)
        {
            if (targetMonitor != null && CanDropOnMonitor(targetMonitor))
            {
                dragCopyImage.color = validDropColor;
                
                // Also highlight the monitor border if it has one
                var monitorBorder = targetMonitor.GetComponent<Image>();
                if (monitorBorder != null)
                {
                    monitorBorder.color = validDropColor;
                }
            }
            else if (targetMonitor != null)
            {
                dragCopyImage.color = invalidDropColor;
                
                // Highlight the monitor border in red
                var monitorBorder = targetMonitor.GetComponent<Image>();
                if (monitorBorder != null)
                {
                    monitorBorder.color = invalidDropColor;
                }
            }
            else
            {
                dragCopyImage.color = normalColor;

                // Reset the active monitor border color
                var activeMonitor = SuspectManager.Instance?.interrogationScreen;
                if (activeMonitor != null)
                {
                    var monitorBorder = activeMonitor.GetComponent<Image>();
                    if (monitorBorder != null) monitorBorder.color = Color.white;
                }
            }
        }
    }
    
    /// <summary>
    /// Find monitor at screen position
    /// </summary>
    private InterrogationScreen FindMonitorAtPosition(Vector2 screenPosition)
    {
        // Raycast to find monitors
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };
        
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        
        
        foreach (var result in results)
        {
            
            var monitor = result.gameObject.GetComponent<InterrogationScreen>();
            if (monitor != null)
            {
                return monitor;
            }
            
            // Also check parent objects
            monitor = result.gameObject.GetComponentInParent<InterrogationScreen>();
            if (monitor != null)
            {
                return monitor;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if we can drop on the given monitor
    /// </summary>
    private bool CanDropOnMonitor(InterrogationScreen monitor)
    {
        if (monitor == null) return false;
        
        // Can only drop if monitor is available (no current suspect)
        return monitor.IsAvailable();
    }
    
    /// <summary>
    /// Drop suspect on monitor
    /// </summary>
    private void DropOnMonitor(InterrogationScreen monitor)
    {
        if (monitor == null || suspectData == null) return;
        
        
        // Validation 1: Check if monitor is free
        if (!monitor.IsAvailable())
        {
            Debug.LogWarning($"[SuspectTag] Monitor is not available - already has suspect: {monitor.GetCurrentSuspect()?.FullName}");
            monitor.ShowErrorMessage("Monitor already occupied");
            ReturnToOriginalPosition();
            return;
        }
        
        // Validation 2: Check if suspect profile is complete
        if (!suspectData.IsComplete())
        {
            Debug.LogWarning($"[SuspectTag] Suspect profile is not complete - missing fields");
            monitor.ShowErrorMessage("Suspect profile incomplete");
            ReturnToOriginalPosition();
            return;
        }
        
        // Get the citizen from database
        var suspectManager = SuspectManager.Instance;
        if (suspectManager == null)
        {
            Debug.LogError("[SuspectTag] SuspectManager instance not found");
            monitor.ShowErrorMessage("System error");
            ReturnToOriginalPosition();
            return;
        }
        
        var citizenDatabase = suspectManager.citizenDatabase;
        if (citizenDatabase == null)
        {
            Debug.LogError("[SuspectTag] CitizenDatabase reference not found in SuspectManager");
            monitor.ShowErrorMessage("Database not found");
            ReturnToOriginalPosition();
            return;
        }
        
        var citizen = citizenDatabase.GetCitizenById(suspectData.citizenId);
        if (citizen == null)
        {
            Debug.LogError($"[SuspectTag] Could not find citizen with ID: {suspectData.citizenId}");
            monitor.ShowErrorMessage("Citizen not found");
            ReturnToOriginalPosition();
            return;
        }
        
        // Validation 3: Check if suspect is already assigned to the monitor
        if (suspectManager.IsSuspectAssignedToAnyMonitor(citizen))
        {
            Debug.LogWarning($"[SuspectTag] Suspect {citizen.FullName} is already on the monitor");
            monitor.ShowErrorMessage("Suspect already assigned");
            ReturnToOriginalPosition();
            return;
        }

        // Call suspect to the single monitor
        suspectManager.CallSuspectToMonitor(citizen);

        Debug.Log($"[SuspectTag] Successfully called {citizen.FullName} to monitor");
        
        // Animate drag copy to monitor position and then cleanup
        if (dragCopy != null)
        {
            // Get the monitor's center position in the same coordinate space as the drag copy
            Vector3 targetPosition = GetMonitorCenterPosition(monitor);
            
            // Create a sequence for better visual effect
            Sequence dropSequence = DOTween.Sequence();
            
            // First, move to the monitor center
            dropSequence.Append(dragCopy.transform.DOMove(targetPosition, snapAnimationDuration * 0.7f)
                .SetEase(Ease.OutQuart));
            
            // Then scale down slightly and fade out for a nice "snap" effect
            dropSequence.Join(dragCopy.transform.DOScale(0.8f, snapAnimationDuration * 0.7f)
                .SetEase(Ease.OutQuart));
            
            // Fade out the drag copy
            CanvasGroup dragCanvasGroup = dragCopy.GetComponent<CanvasGroup>();
            if (dragCanvasGroup != null)
            {
                dropSequence.Join(dragCanvasGroup.DOFade(0f, snapAnimationDuration * 0.7f)
                    .SetEase(Ease.OutQuart));
            }
            
            // On complete, cleanup and restore original tag
            dropSequence.OnComplete(() => {
                CleanupDragCopy();
                RestoreOriginalTag();
            });
        }
        else
        {
            RestoreOriginalTag();
        }
    }
    
    /// <summary>
    /// Get the monitor's center position in the same coordinate space as the drag copy
    /// </summary>
    private Vector3 GetMonitorCenterPosition(InterrogationScreen monitor)
    {
        if (dragCanvas == null || monitor == null) return Vector3.zero;
        
        // Get the monitor's Image component (the actual display area)
        Image monitorImage = monitor.suspectImage;
        if (monitorImage == null)
        {
            // Fallback to getting Image component from the monitor GameObject
            monitorImage = monitor.GetComponent<Image>();
        }
        
        if (monitorImage == null)
        {
            // Fallback to RectTransform
            RectTransform monitorRect = monitor.GetComponent<RectTransform>();
            if (monitorRect == null) return monitor.transform.position;
            
            Vector3 monitorCenter = monitorRect.position;
            Vector2 fallbackScreenPoint = RectTransformUtility.WorldToScreenPoint(dragCanvas.worldCamera, monitorCenter);
            
            Vector2 fallbackLocalPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.GetComponent<RectTransform>(),
                fallbackScreenPoint,
                dragCanvas.worldCamera,
                out fallbackLocalPoint))
            {
                return dragCanvas.transform.TransformPoint(fallbackLocalPoint);
            }
            
            return monitor.transform.position;
        }
        
        // Use the monitor Image's RectTransform to get the true center
        RectTransform imageRect = monitorImage.GetComponent<RectTransform>();
        if (imageRect == null) return monitor.transform.position;
        
        // Get the center of the image in world space
        Vector3 imageCenter = imageRect.position;
        
        // Convert to screen space
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(dragCanvas.worldCamera, imageCenter);
        
        // Convert screen point to local point in drag canvas
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragCanvas.GetComponent<RectTransform>(),
            screenPoint,
            dragCanvas.worldCamera,
            out localPoint))
        {
            return dragCanvas.transform.TransformPoint(localPoint);
        }
        
        // Fallback: use image's world position
        return imageCenter;
    }
    
    /// <summary>
    /// Return to original position
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        CleanupDragCopy();
        RestoreOriginalTag();
    }
    
    /// <summary>
    /// Cleanup drag copy
    /// </summary>
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
            Destroy(dragCanvas.gameObject);
            dragCanvas = null;
        }
    }
    
    /// <summary>
    /// Restore original tag state
    /// </summary>
    private void RestoreOriginalTag()
    {
        // Restore original tag visibility and interaction
        canvasGroup.alpha = originalAlpha;
        canvasGroup.blocksRaycasts = true;
        
        // Restore scale
        transform.DOScale(originalScale, scaleAnimationDuration)
            .SetEase(scaleEase);
            
        // Restore color
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
        
        // Reset the active monitor border color
        var activeMonitor = SuspectManager.Instance?.interrogationScreen;
        if (activeMonitor != null)
        {
            var monitorBorder = activeMonitor.GetComponent<Image>();
            if (monitorBorder != null) monitorBorder.color = Color.white;
        }
    }
    
    /// <summary>
    /// Get the suspect data
    /// </summary>
    public SuspectEntry.SuspectData GetSuspectData()
    {
        return suspectData;
    }
    
    /// <summary>
    /// Force end drag operation (called externally to cancel drags)
    /// </summary>
    public void ForceEndDrag()
    {
        if (!isDragging) return;
        
        Debug.Log("[SuspectTag] Force ending drag operation");
        
        // Clean up and restore original tag
        ReturnToOriginalPosition();
    }
    
    /// <summary>
    /// Test method to verify drag functionality
    /// </summary>
    [ContextMenu("Test Drag Functionality")]
    public void TestDragFunctionality()
    {
        
        if (suspectData != null && suspectData.IsComplete())
        {
            Debug.Log($"[SuspectTag] Test passed - can drag {suspectData.firstName} {suspectData.lastName}");
        }
        else
        {
            Debug.LogWarning("[SuspectTag] Test failed - suspect data is incomplete or null");
        }
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