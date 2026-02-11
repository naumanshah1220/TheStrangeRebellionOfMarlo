using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class DraggableFileIcon : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Drag Settings")]
    [SerializeField] private float dragScale = 1.2f;
    [SerializeField] private float dragAlpha = 0.8f;
    
    [Header("Animation Settings")]
    [SerializeField] private float scaleAnimationDuration = 0.2f;
    [SerializeField] private float snapAnimationDuration = 0.3f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    [SerializeField] private Ease snapEase = Ease.OutQuart;
    
    [Header("Settings")]
    [SerializeField] private float doubleClickTime = 0.5f;
    
    private DiscFile file;
    private Action<DraggableFileIcon> onSingleClick;
    private Action<DraggableFileIcon> onDoubleClick;
    private float lastClickTime;
    private bool isSelected;
    private FolderApp parentFolder;
    private FileIconSettings iconSettings;
    
    // Drag state
    private GameObject dragCopy;
    private Canvas dragCanvas;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;
    private Transform originalParent;
    private int originalSiblingIndex;
    private float originalAlpha;
    
    public DiscFile File => file;
    public bool IsSelected => isSelected;
    
    private void Awake()
    {
        // Ensure the selection overlay starts hidden
        if (selectionOverlay != null)
        {
            selectionOverlay.SetActive(false);
        }
        
        // Get canvas group for fade animations
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Start invisible for fade-in animation
        canvasGroup.alpha = 0f;
        
        // Find the parent folder app
        parentFolder = GetComponentInParent<FolderApp>();
        if (parentFolder == null)
        {
            Debug.LogWarning("DraggableFileIcon: No FolderApp found in parent hierarchy!");
        }
        
        // Store original state
        originalScale = transform.localScale;
        originalAlpha = canvasGroup.alpha;
        
        // Ensure the Button component doesn't interfere with dragging
        Button button = GetComponent<Button>();
        if (button != null)
        {
            // Set the button to not block raycasts during drag
            button.transition = Selectable.Transition.None;
        }
    }
    
    public void Initialize(DiscFile discFile, Action<DraggableFileIcon> singleClickCallback, Action<DraggableFileIcon> doubleClickCallback, FileIconSettings settings = null)
    {
        file = discFile;
        onSingleClick = singleClickCallback;
        onDoubleClick = doubleClickCallback;
        iconSettings = settings;
        
        // Set icon
        if (iconImage != null && discFile != null)
        {
            Sprite displayIcon = discFile.GetDisplayIcon(iconSettings);
            if (displayIcon != null)
            {
                iconImage.sprite = displayIcon;
            }
        }
        
        // Set name with color
        if (nameText != null && discFile != null)
        {
            nameText.text = discFile.fileName;
            nameText.color = discFile.fileNameColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return; // Don't handle clicks while dragging
        
        if (file == null) return;
        
        float timeSinceLastClick = Time.time - lastClickTime;
        
        if (timeSinceLastClick <= doubleClickTime)
        {
            // Double click - only if file has openable content
            if (file.HasOpenableContent())
            {
                onDoubleClick?.Invoke(this);
            }
        }
        else
        {
            // Single click - notify parent folder
            if (parentFolder != null)
            {
                // Use reflection to call the method on the parent folder
                var method = parentFolder.GetType().GetMethod("OnFileIconClicked");
                if (method != null)
                {
                    // Create a temporary FileIcon to pass to the method
                    var tempFileIcon = new GameObject("TempFileIcon").AddComponent<FileIcon>();
                    tempFileIcon.Initialize(file, null, null, iconSettings);
                    method.Invoke(parentFolder, new object[] { tempFileIcon });
                    Destroy(tempFileIcon.gameObject);
                }
            }
            else
            {
                // Fallback to callback if no parent folder found
                onSingleClick?.Invoke(this);
            }
        }
        
        lastClickTime = Time.time;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isDragging) return;
        
        Debug.Log($"[DraggableFileIcon] Begin drag for file: {file?.fileName}");
        
        isDragging = true;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        
        Debug.Log($"[DraggableFileIcon] Begin drag at position: {eventData.position}");
        
        // Create drag copy
        CreateDragCopy();
        
        // Hide original
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) 
        {
            Debug.LogWarning("[DraggableFileIcon] OnDrag called but not dragging!");
            return;
        }
        
        if (dragCopy == null) 
        {
            Debug.LogWarning("[DraggableFileIcon] OnDrag called but drag copy is null!");
            return;
        }
        
        // Update drag copy position to follow mouse
        PositionDragCopyAtMouse();
        
        // Check for drop zones under the pointer and provide feedback
        CheckDropZoneHover(eventData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Debug.Log($"[DraggableFileIcon] End drag for file: {file?.fileName}");
        
        isDragging = false;
        
        // Clear any hover feedback
        if (lastHoveredDropZone != null)
        {
            lastHoveredDropZone.OnFileDragExit();
            lastHoveredDropZone = null;
        }
        
        // Check if we're over a valid drop zone
        CitizenDatabaseDropZone dropZone = GetDropZoneUnderPointer(eventData);
        
        if (dropZone != null)
        {
            Debug.Log($"[DraggableFileIcon] Found drop zone: {dropZone.name}");
            
            // Check if this drop zone can accept our file type
            if (dropZone.CanAcceptFile(file))
            {
                Debug.Log($"[DraggableFileIcon] Valid drop zone found: {dropZone.name}");
                // Successful drop - animate to drop zone
                AnimateToDropZone(dropZone);
            }
            else
            {
                Debug.Log($"[DraggableFileIcon] Drop zone does not accept this file type, triggering invalid drop feedback");
                // Trigger invalid drop feedback on the drop zone
                dropZone.OnInvalidDrop(file, dragCopy);
                ReturnToOriginalPosition();
            }
        }
        else
        {
            Debug.Log($"[DraggableFileIcon] No drop zone found, returning to original position");
            // Failed drop - return to original position
            ReturnToOriginalPosition();
        }
    }
    
    private void CreateDragCopy()
    {
        // Find or create drag canvas
        dragCanvas = FindDragCanvas();
        
        if (dragCanvas == null)
        {
            Debug.LogError("[DraggableFileIcon] Could not find drag canvas!");
            return;
        }
        
        // Create copy of this icon as a direct child of the drag canvas
        dragCopy = Instantiate(gameObject, dragCanvas.transform);
        
        if (dragCopy == null)
        {
            Debug.LogError("[DraggableFileIcon] Failed to instantiate drag copy!");
            return;
        }
        
        // Ensure it's not a child of the original icon
        if (dragCopy.transform.parent != dragCanvas.transform)
        {
            Debug.LogWarning("[DraggableFileIcon] Drag copy was created with wrong parent, fixing...");
            dragCopy.transform.SetParent(dragCanvas.transform, false);
        }
        
        // Remove this script from copy to prevent recursive dragging
        DraggableFileIcon copyDragScript = dragCopy.GetComponent<DraggableFileIcon>();
        if (copyDragScript != null)
            Destroy(copyDragScript);
        
        // Remove Button component from copy to prevent click events
        Button copyButton = dragCopy.GetComponent<Button>();
        if (copyButton != null)
            Destroy(copyButton);
        
        // Remove any CanvasGroup that might interfere
        CanvasGroup existingCanvasGroup = dragCopy.GetComponent<CanvasGroup>();
        if (existingCanvasGroup != null)
            Destroy(existingCanvasGroup);
        
        // Ensure all child objects are visible and independent
        CanvasRenderer[] allRenderers = dragCopy.GetComponentsInChildren<CanvasRenderer>();
        foreach (var renderer in allRenderers)
        {
            // Force each renderer to be fully visible regardless of parent
            renderer.SetAlpha(1f);
            renderer.SetColor(Color.white);
        }
        
        Debug.Log($"[DraggableFileIcon] Set {allRenderers.Length} CanvasRenderers to full alpha");
        
        // Scale up the drag copy (with null checks)
        if (dragCopy != null && dragCopy.transform != null)
        {
            if (originalScale != Vector3.zero)
            {
                dragCopy.transform.localScale = originalScale * dragScale;
            }
            else
            {
                Debug.LogWarning("[DraggableFileIcon] originalScale is zero, using default scale");
                dragCopy.transform.localScale = Vector3.one * dragScale;
            }
            
            // Ensure the drag copy is visible and on top
            dragCopy.SetActive(true);
            dragCopy.transform.SetAsLastSibling();
            
            // Reset any inherited transform properties
            dragCopy.transform.localScale = Vector3.one * dragScale;
            dragCopy.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("[DraggableFileIcon] dragCopy or its transform is null!");
            return;
        }
        
        // Ensure the drag copy is visible and on top
        dragCopy.SetActive(true);
        dragCopy.transform.SetAsLastSibling();
        
        Debug.Log($"[DraggableFileIcon] Drag copy transform - scale: {dragCopy.transform.localScale}, rotation: {dragCopy.transform.localRotation}");
        
        // Ensure all images are visible and independent
        Image[] images = dragCopy.GetComponentsInChildren<Image>();
        foreach (var image in images)
        {
            Color color = image.color;
            color.a = 1f;
            image.color = color;
            image.raycastTarget = false; // Prevent raycast interference
        }
        
        // Ensure all text is visible and independent
        TMP_Text[] texts = dragCopy.GetComponentsInChildren<TMP_Text>();
        foreach (var text in texts)
        {
            Color color = text.color;
            color.a = 1f;
            text.color = color;
        }
        
        Debug.Log($"[DraggableFileIcon] Set {images.Length} images and {texts.Length} texts to full alpha");
        
        // Force canvas update to ensure all changes are applied
        Canvas.ForceUpdateCanvases();
        
        // Position the drag copy
        PositionDragCopyAtMouse();
        
        Debug.Log($"[DraggableFileIcon] Created drag copy: {dragCopy.name}, active: {dragCopy.activeInHierarchy}");
        Debug.Log($"[DraggableFileIcon] Drag copy parent: {dragCopy.transform.parent?.name}, canvas: {dragCanvas?.name}");
        Debug.Log($"[DraggableFileIcon] Canvas render mode: {dragCanvas?.renderMode}, sorting order: {dragCanvas?.sortingOrder}");
        
        // Debug hierarchy
        Debug.Log($"[DraggableFileIcon] Drag copy hierarchy:");
        Transform current = dragCopy.transform;
        string hierarchy = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            hierarchy = current.name + " -> " + hierarchy;
        }
        Debug.Log($"[DraggableFileIcon] Full hierarchy: {hierarchy}");
        
        // Verify we're in the right canvas
        if (dragCanvas != null && dragCopy.transform.IsChildOf(dragCanvas.transform))
        {
            Debug.Log($"[DraggableFileIcon] ✅ Drag copy is properly parented to {dragCanvas.name}");
        }
        else
        {
            Debug.LogError($"[DraggableFileIcon] ❌ Drag copy is NOT properly parented to {dragCanvas?.name}!");
        }
    }
    
    private void PositionDragCopyAtMouse()
    {
        if (dragCopy == null) 
        {
            Debug.LogWarning("[DraggableFileIcon] Drag copy is null!");
            return;
        }
        
        Vector3 mousePosition = Input.mousePosition;
        
        if (dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            dragCopy.transform.position = mousePosition;
        }
        else
        {
            // For world space canvas, convert screen position to world position
            Camera camera = dragCanvas.worldCamera;
            if (camera == null) camera = Camera.main;
            
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, dragCanvas.planeDistance));
            dragCopy.transform.position = worldPosition;
        }
        
        // Ensure the drag copy is visible and on top
        dragCopy.transform.SetAsLastSibling();
        
        // Force canvas update to ensure visibility
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"[DraggableFileIcon] Positioned drag copy at: {dragCopy.transform.position}, canvas: {dragCanvas.name}, renderMode: {dragCanvas.renderMode}");
    }
    
    private Canvas FindDragCanvas()
    {
        // First try to find canvas with "DragCanvas" tag
        Canvas dragCanvas = GameObject.FindWithTag("DragCanvas")?.GetComponent<Canvas>();
        if (dragCanvas != null) 
        {
            Debug.Log($"[DraggableFileIcon] Found drag canvas by tag: {dragCanvas.name}");
            return dragCanvas;
        }
        
        // Fallback to finding by name
        dragCanvas = GameObject.Find("DragCanvas")?.GetComponent<Canvas>();
        if (dragCanvas != null) 
        {
            Debug.Log($"[DraggableFileIcon] Found drag canvas by name: {dragCanvas.name}");
            return dragCanvas;
        }
        
        // Try to find Desktop canvas
        dragCanvas = GameObject.Find("Desktop")?.GetComponent<Canvas>();
        if (dragCanvas != null)
        {
            Debug.Log($"[DraggableFileIcon] Found Desktop canvas: {dragCanvas.name}");
            return dragCanvas;
        }
        
        // Fallback to the current canvas (this will limit dragging to the current window)
        Canvas currentCanvas = GetComponentInParent<Canvas>();
        if (currentCanvas != null)
        {
            Debug.LogWarning($"[DraggableFileIcon] Using current canvas as fallback: {currentCanvas.name} - dragging will be limited to this window!");
            return currentCanvas;
        }
        
        Debug.LogError("[DraggableFileIcon] No canvas found!");
        return null;
    }
    
    private CitizenDatabaseDropZone GetDropZoneUnderPointer(PointerEventData eventData)
    {
        // Raycast to find drop zones
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        
        Debug.Log($"[DraggableFileIcon] Raycast found {raycastResults.Count} objects under pointer");
        
        foreach (var result in raycastResults)
        {
            Debug.Log($"[DraggableFileIcon] Checking object: {result.gameObject.name}");
            
            var dropZone = result.gameObject.GetComponent<CitizenDatabaseDropZone>();
            if (dropZone != null)
            {
                Debug.Log($"[DraggableFileIcon] Found drop zone: {dropZone.name}, initialized: {dropZone.IsInitialized()}");
                
                // Check if this drop zone can accept our file type
                if (file != null && dropZone.CanAcceptFile(file))
                {
                    Debug.Log($"[DraggableFileIcon] Drop zone {dropZone.name} can accept file {file.fileName}");
                    return dropZone;
                }
                else
                {
                    Debug.Log($"[DraggableFileIcon] Drop zone {dropZone.name} cannot accept file {file?.fileName}");
                }
            }
            
            // Also check parent objects
            dropZone = result.gameObject.GetComponentInParent<CitizenDatabaseDropZone>();
            if (dropZone != null)
            {
                Debug.Log($"[DraggableFileIcon] Found drop zone in parent: {dropZone.name}, initialized: {dropZone.IsInitialized()}");
                
                // Check if this drop zone can accept our file type
                if (file != null && dropZone.CanAcceptFile(file))
                {
                    Debug.Log($"[DraggableFileIcon] Parent drop zone {dropZone.name} can accept file {file.fileName}");
                    return dropZone;
                }
                else
                {
                    Debug.Log($"[DraggableFileIcon] Parent drop zone {dropZone.name} cannot accept file {file?.fileName}");
                }
            }
        }
        
        Debug.Log("[DraggableFileIcon] No drop zone found under pointer");
        return null;
    }
    
    private CitizenDatabaseDropZone lastHoveredDropZone;
    
    private void CheckDropZoneHover(PointerEventData eventData)
    {
        CitizenDatabaseDropZone currentDropZone = GetDropZoneUnderPointerForHover(eventData);
        
        // If we're hovering over a different drop zone
        if (currentDropZone != lastHoveredDropZone)
        {
            // Clear feedback from previous drop zone
            if (lastHoveredDropZone != null)
            {
                lastHoveredDropZone.OnFileDragExit();
            }
            
            // Show feedback for new drop zone
            if (currentDropZone != null && file != null)
            {
                currentDropZone.OnFileDragHover(file);
            }
            
            lastHoveredDropZone = currentDropZone;
        }
        // If we're still hovering over the same drop zone, ensure feedback is active
        else if (currentDropZone != null && file != null)
        {
            // Force refresh the hover feedback to ensure it's visible
            currentDropZone.OnFileDragHover(file);
        }
    }
    
    /// <summary>
    /// Find any drop zone under pointer for hover feedback (including invalid ones)
    /// </summary>
    private CitizenDatabaseDropZone GetDropZoneUnderPointerForHover(PointerEventData eventData)
    {
        // Raycast to find drop zones
        var raycastResults = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        
        foreach (var result in raycastResults)
        {
            var dropZone = result.gameObject.GetComponent<CitizenDatabaseDropZone>();
            if (dropZone != null)
            {
                return dropZone; // Return any drop zone, regardless of file acceptance
            }
            
            // Also check parent objects
            dropZone = result.gameObject.GetComponentInParent<CitizenDatabaseDropZone>();
            if (dropZone != null)
            {
                return dropZone; // Return any drop zone, regardless of file acceptance
            }
        }
        
        return null;
    }
    
    private void AnimateToDropZone(CitizenDatabaseDropZone dropZone)
    {
        if (dragCopy != null && dropZone != null)
        {
            Vector3 targetPosition = dropZone.transform.position;
            
            // Animate drag copy to drop zone
            dragCopy.transform.DOMove(targetPosition, snapAnimationDuration)
                .SetEase(snapEase)
                .OnComplete(() => {
                    // Notify the drop zone
                    dropZone.OnFileDropped(file, dragCopy);
                    
                    // Clean up drag copy
                    CleanupDragCopy();
                    
                    // Restore original
                    RestoreOriginal();
                });
        }
        else
        {
            CleanupDragCopy();
            RestoreOriginal();
        }
    }
    
    private void ReturnToOriginalPosition()
    {
        // Clean up drag copy
        CleanupDragCopy();
        
        // Restore original
        RestoreOriginal();
    }
    
    private void CleanupDragCopy()
    {
        if (dragCopy != null)
        {
            DOTween.Kill(dragCopy, true);
            Destroy(dragCopy);
            dragCopy = null;
        }
    }
    
    private void RestoreOriginal()
    {
        // Restore original state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f; // Force full opacity
            canvasGroup.blocksRaycasts = true;
        }
        
        Debug.Log("[DraggableFileIcon] Restored original icon");
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionOverlay != null)
        {
            selectionOverlay.SetActive(selected);
        }
    }
    
    /// <summary>
    /// Animate the icon fading in
    /// </summary>
    public void AnimateIn(float delay = 0f)
    {
        if (canvasGroup == null) return;
        
        // Start invisible
        canvasGroup.alpha = 0f;
        
        // Animate fade in with delay
        canvasGroup.DOFade(1f, FileIconManager.Settings?.iconFadeInDuration ?? 0.3f)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Animate the icon fading out
    /// </summary>
    public void AnimateOut(float delay = 0f)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.DOFade(0f, FileIconManager.Settings?.iconFadeInDuration ?? 0.3f)
            .SetDelay(delay)
            .SetEase(Ease.InQuad);
    }
    
    /// <summary>
    /// Update the file icon's display based on the file data
    /// </summary>
    public void RefreshDisplay()
    {
        if (file == null) return;
        
        // Update icon
        if (iconImage != null)
        {
            Sprite displayIcon = file.GetDisplayIcon(iconSettings);
            if (displayIcon != null)
            {
                iconImage.sprite = displayIcon;
            }
        }
        
        // Update name and color
        if (nameText != null)
        {
            nameText.text = file.fileName;
            nameText.color = file.fileNameColor;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up drag copy
        if (dragCopy != null)
        {
            DOTween.Kill(dragCopy, true);
            Destroy(dragCopy);
        }
        
        // Kill all tweens targeting this object
        DOTween.Kill(this, true);
        DOTween.Kill(transform, true);
        DOTween.Kill(gameObject, true);
        
        if (canvasGroup != null)
            DOTween.Kill(canvasGroup, true);
    }
} 