using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Drop zone for Citizen Database search fields
/// </summary>
public class CitizenDatabaseDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Visual Settings")]
    [SerializeField] private Image frameImage;
    [SerializeField] private TMP_Text fieldLabelText;
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] private Color normalFrameColor = Color.white;
    [SerializeField] private Color highlightFrameColor = Color.yellow;
    [SerializeField] private Color acceptFrameColor = Color.green;
    [SerializeField] private Color rejectFrameColor = Color.red;
    
    [Header("Animation Settings")]
    [SerializeField] private float hoverScaleFactor = 1.02f; // Reduced from 1.05f to prevent excessive scaling
    [SerializeField] private float hoverAnimationDuration = 0.2f;
    [SerializeField] private float colorTransitionDuration = 0.3f;
    [SerializeField] private Ease hoverEase = Ease.OutQuart;
    
    [Header("Drop Settings")]
    [SerializeField] private bool acceptTags = true;
    [SerializeField] private bool acceptFiles = true;
    [SerializeField] private FileType[] acceptedFileTypes = { FileType.Photo, FileType.FingerprintScan };
    
    // State
    private bool isHighlighted = false;
    private bool hasDroppedItem = false;
    private bool isInitialized = false;
    private bool isInFileDragMode = false;
    private Vector3 originalScale;
    private Color originalFrameColor;
    private string fieldType;
    private string fieldLabel;
    private string droppedContent = "";
    private string droppedType = "";
    private DiscFile droppedFile;
    
    // Events
    public System.Action<string, string, string> OnTagDroppedEvent; // fieldType, content, tagType
    public System.Action<string, DiscFile> OnFileDroppedEvent; // fieldType, file
    
    private void Awake()
    {
        // Store original state
        originalScale = transform.localScale;
        originalFrameColor = normalFrameColor; // Use the intended normal color, not current color
        
        // Set initial frame color to normal with full opacity
        if (frameImage != null)
        {
            Color normalColor = normalFrameColor;
            normalColor.a = 1f; // Force full opacity
            frameImage.color = normalColor;
        }
        
        // Validate setup (but don't add unnecessary components)
        ValidateSetup();
    }
    

    
    /// <summary>
    /// Validate that the drop zone is properly set up
    /// </summary>
    private void ValidateSetup()
    {
        if (frameImage == null)
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] frameImage is null on {gameObject.name}");
        }
        
        if (fieldLabelText == null)
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] fieldLabelText is null on {gameObject.name}");
        }
        
        if (placeholderText == null)
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] placeholderText is null on {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Initialize the drop zone with field information
    /// </summary>
    public void Initialize(string type, string label)
    {
        
        fieldType = type;
        fieldLabel = label;
        
        // Update UI
        if (fieldLabelText != null)
        {
            fieldLabelText.text = label;
            Debug.Log($"[CitizenDatabaseDropZone] Set field label text to: {label}");
        }
        else
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] fieldLabelText is null during initialization of {gameObject.name}");
        }
        
        if (placeholderText != null)
        {
            placeholderText.text = $"Drop {label.ToLower()} here";
            Debug.Log($"[CitizenDatabaseDropZone] Set placeholder text to: Drop {label.ToLower()} here");
        }
        else
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] placeholderText is null during initialization of {gameObject.name}");
        }
        
        // Ensure proper visual state
        SetInitialVisualState();
        
        isInitialized = true;
        Debug.Log($"[CitizenDatabaseDropZone] Successfully initialized {gameObject.name} with type: {type}, label: {label}, isInitialized: {isInitialized}");
    }
    
    /// <summary>
    /// Set the initial visual state to ensure proper colors and visibility
    /// </summary>
    private void SetInitialVisualState()
    {
        Debug.Log($"[CitizenDatabaseDropZone] Setting initial visual state for {gameObject.name}");
        
        // Ensure frame has the correct normal color AND full opacity
        if (frameImage != null)
        {
            Color normalColor = normalFrameColor;
            normalColor.a = 1f; // Force full opacity
            frameImage.color = normalColor;
            frameImage.raycastTarget = true; // Ensure it can receive raycasts
            Debug.Log($"[CitizenDatabaseDropZone] Set frame color to normal with full opacity: {normalColor}");
        }
        else
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] frameImage is null during SetInitialVisualState for {gameObject.name}");
        }
        
        // Check for and fix any CanvasGroup alpha issues
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            Debug.Log($"[CitizenDatabaseDropZone] Set CanvasGroup alpha to 1 for {gameObject.name}");
        }
        
        // Ensure the GameObject is active and visible
        gameObject.SetActive(true);
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
        
        Debug.Log($"[CitizenDatabaseDropZone] Initial visual state set for {gameObject.name}");
    }
    
    /// <summary>
    /// Get the initialization status (for debugging)
    /// </summary>
    public bool IsInitialized() => isInitialized;
    
    /// <summary>
    /// Force re-initialization if needed
    /// </summary>
    public void ForceInitialize(string type, string label)
    {
        Debug.Log($"[CitizenDatabaseDropZone] Force initializing {gameObject.name}");
        Initialize(type, label);
    }
    
    /// <summary>
    /// Set the accepted file types for this drop zone
    /// </summary>
    public void SetAcceptedFileTypes(FileType[] fileTypes)
    {
        acceptedFileTypes = fileTypes;
        Debug.Log($"[CitizenDatabaseDropZone] {gameObject.name} now accepts: [{string.Join(", ", acceptedFileTypes)}]");
    }
    
    /// <summary>
    /// Check if this drop zone can accept the given tag
    /// </summary>
    public bool CanAcceptTag(DraggableTag tag)
    {
        if (!acceptTags || tag == null) return false;
        return true; // Accept all tags for now
    }
    
    /// <summary>
    /// Check if this drop zone can accept the given file
    /// </summary>
    public bool CanAcceptFile(DiscFile file)
    {
        if (!acceptFiles || file == null) 
        {
            Debug.Log($"[CitizenDatabaseDropZone] CanAcceptFile: acceptFiles={acceptFiles}, file={file?.fileName}");
            return false;
        }
        
        Debug.Log($"[CitizenDatabaseDropZone] Checking if {gameObject.name} can accept {file.fileName} (type: {file.fileType})");
        Debug.Log($"[CitizenDatabaseDropZone] Accepted types: [{string.Join(", ", acceptedFileTypes)}]");
        
        foreach (var acceptedType in acceptedFileTypes)
        {
            if (file.fileType == acceptedType)
            {
                Debug.Log($"[CitizenDatabaseDropZone] {gameObject.name} ACCEPTS {file.fileName} (type: {file.fileType})");
                return true;
            }
        }
        
        Debug.Log($"[CitizenDatabaseDropZone] {gameObject.name} REJECTS {file.fileName} (type: {file.fileType})");
        return false;
    }
    
    /// <summary>
    /// Show hover feedback for file drag (called from DraggableFileIcon during drag)
    /// </summary>
    public void OnFileDragHover(DiscFile file)
    {
        if (!acceptFiles || file == null) return;
        
        // Set file drag mode to disable pointer hover
        isInFileDragMode = true;
        
        bool canAccept = CanAcceptFile(file);
        
        if (canAccept)
        {
            // Show accept feedback (green)
            if (frameImage != null)
            {
                // Kill any existing color animation to prevent flickering
                DOTween.Kill(frameImage);
                frameImage.DOColor(acceptFrameColor, 0.1f);
            }
        }
        else
        {
            // Show reject feedback (red)
            if (frameImage != null)
            {
                // Kill any existing color animation to prevent flickering
                DOTween.Kill(frameImage);
                frameImage.DOColor(rejectFrameColor, 0.1f);
            }
        }
    }
    
    /// <summary>
    /// Clear hover feedback when file drag exits
    /// </summary>
    public void OnFileDragExit()
    {
        // Clear file drag mode
        isInFileDragMode = false;
        
        if (frameImage != null)
        {
            // Kill any existing color animation to prevent flickering
            DOTween.Kill(frameImage);
            frameImage.DOColor(normalFrameColor, 0.1f);
        }
    }
    
    /// <summary>
    /// Handle invalid drop with visual feedback
    /// </summary>
    public void OnInvalidDrop(DiscFile file, GameObject draggedObject)
    {
        Debug.Log($"[CitizenDatabaseDropZone] Invalid drop on {gameObject.name}: {file?.fileName} (type: {file?.fileType})");
        
        // Clean up dragged object
        if (draggedObject != null)
        {
            DOTween.Kill(draggedObject, true);
            Destroy(draggedObject);
        }
        
        // Flash red to indicate invalid drop
        StartCoroutine(FlashRejectColor());
    }
    
    /// <summary>
    /// Flash the frame red to indicate invalid drop
    /// </summary>
    private IEnumerator FlashRejectColor()
    {
        if (frameImage == null) yield break;
        
        Color originalColor = frameImage.color;
        
        // Flash red
        frameImage.DOColor(rejectFrameColor, 0.1f);
        yield return new WaitForSeconds(0.2f);
        
        // Return to original color
        frameImage.DOColor(originalColor, 0.1f);
        
        Debug.Log($"[CitizenDatabaseDropZone] Flashed red on {gameObject.name} for invalid drop");
    }
    
    /// <summary>
    /// Called when a tag is successfully dropped on this zone
    /// </summary>
    public void OnTagDropped(DraggableTag tag, GameObject draggedObject)
    {
        try
        {
            
            // Check if we're initialized
            if (!isInitialized)
            {
                Debug.LogWarning($"[CitizenDatabaseDropZone] OnTagDropped called before initialization on {gameObject.name}");
                Debug.LogWarning($"[CitizenDatabaseDropZone] Current state - fieldType: '{fieldType}', fieldLabel: '{fieldLabel}'");
                
                // Try to find parent CitizenDatabaseApp and re-initialize
                var app = GetComponentInParent<CitizenDatabaseApp>();
                if (app != null)
                {
                    Debug.Log($"[CitizenDatabaseDropZone] Found parent app, attempting re-initialization");
                    
                    // Try to determine what this drop zone should be based on its name
                    string expectedType = "";
                    string expectedLabel = "";
                    
                    string objName = gameObject.name.ToLower();
                    if (objName.Contains("fname") || objName.Contains("firstname"))
                    {
                        expectedType = "firstName";
                        expectedLabel = "First Name";
                    }
                    else if (objName.Contains("lname") || objName.Contains("lastname"))
                    {
                        expectedType = "lastName";
                        expectedLabel = "Last Name";
                    }
                    else if (objName.Contains("citizenid") || objName.Contains("id"))
                    {
                        expectedType = "citizenId";
                        expectedLabel = "Citizen ID";
                    }
                    else if (objName.Contains("portrait") || objName.Contains("facial"))
                    {
                        expectedType = "facialRecognition";
                        expectedLabel = "Facial Recognition";
                    }
                    else if (objName.Contains("biometric") || objName.Contains("fingerprint"))
                    {
                        expectedType = "biometrics";
                        expectedLabel = "Biometrics";
                    }
                    
                    if (!string.IsNullOrEmpty(expectedType))
                    {
                        Debug.Log($"[CitizenDatabaseDropZone] Emergency initializing as {expectedType}");
                        ForceInitialize(expectedType, expectedLabel);
                    }
                    else
                    {
                        Debug.LogError($"[CitizenDatabaseDropZone] Could not determine drop zone type from name: {gameObject.name}");
                        return;
                    }
                }
                else
                {
                    Debug.LogError("[CitizenDatabaseDropZone] No parent CitizenDatabaseApp found, cannot re-initialize");
                    return;
                }
            }
            
            if (tag == null) return;
            
            // Store tag information
            droppedContent = tag.GetTagContent();
            droppedType = tag.GetTagType();
            droppedFile = null;
            hasDroppedItem = true;
            
            // Clean up dragged object
            if (draggedObject != null)
            {
                DOTween.Kill(draggedObject, true);
                Destroy(draggedObject);
            }
            
            // Update visual state - just change frame color and text
            SetFrameColor(acceptFrameColor);
            UpdatePlaceholderText();
            
            // Notify parent app that content changed
            var parentApp = GetComponentInParent<CitizenDatabaseApp>();
            if (parentApp != null) parentApp.OnDropZoneContentChanged();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in OnTagDropped: {e.Message}");
        }
    }
    
    /// <summary>
    /// Called when a file is successfully dropped on this zone
    /// </summary>
    public void OnFileDropped(DiscFile file, GameObject draggedObject)
    {
        Debug.Log($"[CitizenDatabaseDropZone] OnFileDropped called on {gameObject.name} - isInitialized: {isInitialized}, fieldType: {fieldType}");
        
        // Check if we're initialized
        if (!isInitialized)
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] OnFileDropped called before initialization on {gameObject.name}, attempting emergency initialization");
            
            // Try emergency initialization based on name
            string expectedType = "";
            string expectedLabel = "";
            
            string objName = gameObject.name.ToLower();
            if (objName.Contains("fname") || objName.Contains("firstname"))
            {
                expectedType = "firstName";
                expectedLabel = "First Name";
            }
            else if (objName.Contains("lname") || objName.Contains("lastname"))
            {
                expectedType = "lastName";
                expectedLabel = "Last Name";
            }
            else if (objName.Contains("citizenid") || objName.Contains("id"))
            {
                expectedType = "citizenId";
                expectedLabel = "Citizen ID";
            }
            else if (objName.Contains("portrait") || objName.Contains("facial"))
            {
                expectedType = "facialRecognition";
                expectedLabel = "Facial Recognition";
            }
            else if (objName.Contains("biometric") || objName.Contains("fingerprint"))
            {
                expectedType = "biometrics";
                expectedLabel = "Biometrics";
            }
            
            if (!string.IsNullOrEmpty(expectedType))
            {
                Debug.Log($"[CitizenDatabaseDropZone] Emergency initializing {gameObject.name} as {expectedType}");
                ForceInitialize(expectedType, expectedLabel);
            }
            else
            {
                Debug.LogError($"[CitizenDatabaseDropZone] Could not determine drop zone type from name: {gameObject.name}");
                return;
            }
        }
        
        if (file == null) return;
        
        // Store file information
        droppedContent = file.fileName;
        droppedType = "file";
        droppedFile = file;
        hasDroppedItem = true;
        
        // Clean up dragged object
        if (draggedObject != null)
        {
            DOTween.Kill(draggedObject, true);
            Destroy(draggedObject);
        }
        
        // Update visual state - just change frame color and text
        SetFrameColor(acceptFrameColor);
        UpdatePlaceholderText();
        
        // Trigger event
        OnFileDroppedEvent?.Invoke(fieldType, droppedFile);
        
        // Notify parent app that content changed (this should update button states)
        var parentApp = GetComponentInParent<CitizenDatabaseApp>();
        if (parentApp != null) 
        {
            Debug.Log($"[CitizenDatabaseDropZone] Notifying parent app of content change");
            parentApp.OnDropZoneContentChanged();
        }
        else
        {
            Debug.LogWarning($"[CitizenDatabaseDropZone] No parent CitizenDatabaseApp found!");
        }
    }
    

    
    /// <summary>
    /// Clear the current dropped item
    /// </summary>
    public void ClearDroppedItem()
    {
        hasDroppedItem = false;
        droppedContent = "";
        droppedType = "";
        droppedFile = null;
        
        SetFrameColor(originalFrameColor);
        
        // Reset placeholder text
        if (isInitialized && placeholderText != null)
        {
            placeholderText.text = $"Drop {fieldLabel.ToLower()} here";
        }
        
        // Notify parent app that content changed
        var parentApp = GetComponentInParent<CitizenDatabaseApp>();
        if (parentApp != null) parentApp.OnDropZoneContentChanged();
    }
    
    /// <summary>
    /// Get the currently dropped content
    /// </summary>
    public string GetDroppedContent() => droppedContent;
    
    /// <summary>
    /// Get the currently dropped file
    /// </summary>
    public DiscFile GetDroppedFile() => droppedFile;
    
    /// <summary>
    /// Check if an item has been dropped
    /// </summary>
    public bool HasDroppedItem() => hasDroppedItem;
    
    /// <summary>
    /// Get detailed state information for debugging
    /// </summary>
    public string GetStateDebugInfo()
    {
        return $"HasDroppedItem: {hasDroppedItem}, DroppedContent: '{droppedContent}', DroppedType: '{droppedType}', IsInitialized: {isInitialized}";
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Disable pointer hover during file drag mode
        if (isInFileDragMode) return;
        
        if (!isHighlighted)
        {
            isHighlighted = true;
            
            // Scale up animation - use a more conservative scale
            Vector3 targetScale = originalScale * hoverScaleFactor;
            transform.DOScale(targetScale, hoverAnimationDuration)
                .SetEase(hoverEase);
            
            // Color change animation (only if no item is dropped)
            if (!hasDroppedItem)
            {
                SetFrameColor(highlightFrameColor);
            }
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        // Disable pointer hover during file drag mode
        if (isInFileDragMode) return;
        
        if (isHighlighted)
        {
            isHighlighted = false;
            
            // Scale down animation
            transform.DOScale(originalScale, hoverAnimationDuration)
                .SetEase(hoverEase);
            
            // Color revert animation (only if no item is dropped)
            if (!hasDroppedItem)
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
    
    private void UpdatePlaceholderText()
    {
        if (placeholderText == null) return;
        
        if (hasDroppedItem)
        {
            AnimateTypewriterText(droppedContent);
        }
        else
        {
            placeholderText.text = $"Drop {fieldLabel.ToLower()} here";
        }
    }
    
    /// <summary>
    /// Animate text with typewriter effect
    /// </summary>
    private void AnimateTypewriterText(string targetText)
    {
        if (placeholderText == null || string.IsNullOrEmpty(targetText)) return;
        
        // Kill any existing typewriter animation
        DOTween.Kill(placeholderText);
        
        // Clear the text first
        placeholderText.text = "";
        
        // Animate the text character by character
        float delayPerChar = 0.05f; // Adjust this for typing speed
        float totalDuration = targetText.Length * delayPerChar;
        
        DOTween.To(() => 0, (int charIndex) => {
            if (placeholderText != null)
            {
                int safeIndex = Mathf.Min(charIndex, targetText.Length);
                placeholderText.text = targetText.Substring(0, safeIndex);
            }
        }, targetText.Length, totalDuration)
        .SetEase(Ease.Linear)
        .SetTarget(placeholderText)
        .OnComplete(() => {
            Debug.Log($"CitizenDatabase.{fieldType}DropZone.PlaceHolderText: {targetText}");
        });
    }
    
    private void OnDestroy()
    {
        // Kill all tweens targeting this object
        DOTween.Kill(this, true);
        DOTween.Kill(transform, true);
        DOTween.Kill(gameObject, true);
        
        if (frameImage != null)
            DOTween.Kill(frameImage, true);
    }
} 