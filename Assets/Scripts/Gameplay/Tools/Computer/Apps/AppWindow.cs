using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class AppWindow : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private Image fullscreenIcon;
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private RectTransform dragHandle;
    [SerializeField] private RectTransform windowContainer;
    [SerializeField] private RectTransform resizeHandle;
    
    [Header("Settings")]
    [SerializeField] private Vector2 defaultWindowSize = new Vector2(800, 600);
    [SerializeField] private Vector2 minWindowSize = new Vector2(200, 150);
    [SerializeField] private Sprite fullscreenSprite;
    [SerializeField] private Sprite restoreSprite;
    
    private AppConfig config;
    private DiscFile currentFile;
    private GameObject currentContent;
    private System.Action<AppWindow> onClose;
    private Vector2 dragOffset;
    private Canvas parentCanvas;
    private Camera mainCamera;
    private bool isResizing;
    private bool isFullscreen;
    private Vector2 lastWindowSize;
    private Vector2 lastWindowPosition;
    private bool isDragging;
    private Vector2 startDragPosition;
    private Vector2 startDragSize;
    private RetroComputerEffects retroEffects;
    
    public AppConfig AppConfig => config;
    public DiscFile CurrentFile => currentFile;
    
    private void Awake()
    {
        // Get references
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        
        // If references not assigned, try to find them in children
        if (titleText == null)
            titleText = GetComponentInChildren<TMP_Text>();
        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>();
        if (dragHandle == null)
            dragHandle = transform.Find("WindowHeader")?.GetComponent<RectTransform>();
        if (windowContainer == null)
            windowContainer = GetComponent<RectTransform>();
        if (fullscreenButton == null)
            fullscreenButton = transform.Find("WindowHeader/FullscreenButton")?.GetComponent<Button>();
        if (fullscreenIcon == null && fullscreenButton != null)
            fullscreenIcon = fullscreenButton.GetComponent<Image>();
        if (resizeHandle == null)
            resizeHandle = transform.Find("ResizeHandle")?.GetComponent<RectTransform>();

        // Set pivot to top-left
        if (windowContainer != null)
        {
            windowContainer.pivot = new Vector2(0, 1);
        }

        retroEffects = RetroComputerEffects.Instance;
    }

    private void Update()
    {
        // Ensure resize handle is in correct state
        if (resizeHandle != null && config != null)
        {
            bool shouldBeActive = config.IsResizable && !isFullscreen;
            if (resizeHandle.gameObject.activeSelf != shouldBeActive)
            {
                resizeHandle.gameObject.SetActive(shouldBeActive);
            }
        }
        
        // Validate cursor state when not dragging/resizing
        if (!isDragging && !isResizing && !isFullscreen && retroEffects != null)
        {
            ValidateCursorState();
        }
    }
    
    /// <summary>
    /// Validate that the cursor state matches the current mouse position
    /// </summary>
    private void ValidateCursorState()
    {
        Vector2 mousePosition = Input.mousePosition;
        Camera eventCamera = mainCamera;
        
        // Check if we should be showing resize cursor
        bool shouldShowResize = resizeHandle != null && config.IsResizable && 
            RectTransformUtility.RectangleContainsScreenPoint(resizeHandle, mousePosition, eventCamera);
            
        // Check if we should be showing move cursor
        bool shouldShowMove = dragHandle != null && 
            RectTransformUtility.RectangleContainsScreenPoint(dragHandle, mousePosition, eventCamera);
            
        // Get current cursor sprite
        Sprite currentSprite = retroEffects.GetCurrentCursorSprite();
        
        // Determine what cursor we should be showing
        Sprite expectedSprite = null;
        if (shouldShowResize)
        {
            expectedSprite = retroEffects.GetResizeCursorSprite();
        }
        else if (shouldShowMove)
        {
            expectedSprite = retroEffects.GetMoveCursorSprite();
        }
        else
        {
            expectedSprite = retroEffects.GetDefaultCursorSprite();
        }
        
        // If cursor doesn't match expected, fix it
        if (currentSprite != expectedSprite)
        {
            if (shouldShowResize)
            {
                retroEffects.ShowResizeCursor();
            }
            else if (shouldShowMove)
            {
                retroEffects.ShowMoveCursor();
            }
            else
            {
                retroEffects.RestoreCursor();
            }
        }
    }
    
    public void Initialize(AppConfig appConfig, System.Action<AppWindow> closeCallback)
    {
        config = appConfig;
        onClose = closeCallback;
        
        if (titleText != null)
        {
            titleText.text = appConfig.AppName;
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => onClose?.Invoke(this));
        }

        // Setup fullscreen button based on isResizable
        if (fullscreenButton != null)
        {
            fullscreenButton.gameObject.SetActive(config.IsResizable);
            fullscreenButton.onClick.AddListener(ToggleFullscreen);
            if (fullscreenIcon != null)
            {
                fullscreenIcon.sprite = restoreSprite; // Start with restore icon since we're in fullscreen
            }
        }

        // Setup resize handle based on isResizable - enable it when not in fullscreen
        if (resizeHandle != null)
        {
            resizeHandle.gameObject.SetActive(config.IsResizable && !isFullscreen);
        }

        // Set up window container
        if (windowContainer != null)
        {
            // Set proper anchoring for the window
            windowContainer.anchorMin = new Vector2(0, 1); // Top-left anchoring to match pivot
            windowContainer.anchorMax = new Vector2(0, 1);
            
            // Store default size
            startDragSize = defaultWindowSize;
            lastWindowSize = defaultWindowSize;
            windowContainer.sizeDelta = defaultWindowSize;
            
            // Position at top-left
            windowContainer.anchoredPosition = Vector2.zero;
        }
        
        // Instantiate app content (will be overridden if file is loaded)
        if (appConfig.AppPrefab != null && contentArea != null)
        {
            InstantiateAppContent(appConfig.AppPrefab);
        }
    }
    
    /// <summary>
    /// Load a file into this window
    /// </summary>
    public void LoadFile(DiscFile file)
    {
        if (file == null) return;
        
        currentFile = file;
        
        // Update window title to include file name
        if (titleText != null)
        {
            titleText.text = $"{config.AppName} - {file.fileName}";
        }
        
        // Clear existing content
        ClearContent();
        
        // Priority 1: Use the file's viewer app if specified
        if (file.viewerApp != null && file.viewerApp.AppPrefab != null && contentArea != null)
        {
            Debug.Log($"[AppWindow] Loading file with viewer app: {file.viewerApp.AppName}");
            InstantiateAppContent(file.viewerApp.AppPrefab);
            
            // Initialize the viewer app with file data if it implements IFileContent
            if (currentContent != null)
            {
                var fileContent = currentContent.GetComponent<IFileContent>();
                if (fileContent != null)
                {
                    fileContent.Initialize(currentFile);
                }
            }
        }
        // Priority 2: Use the file's content prefab directly
        else if (file.contentPrefab != null && contentArea != null)
        {
            Debug.Log("[AppWindow] Loading file content prefab directly");
            InstantiateFileContent(file.contentPrefab);
        }
        // Priority 3: Fallback to the window's app content
        else if (config.AppPrefab != null && contentArea != null)
        {
            Debug.Log("[AppWindow] Loading window's app content as fallback");
            InstantiateAppContent(config.AppPrefab);
            
            // Initialize the app content with file data if it implements IFileContent
            if (currentContent != null)
            {
                var fileContent = currentContent.GetComponent<IFileContent>();
                if (fileContent != null)
                {
                    fileContent.Initialize(currentFile);
                }
            }
        }
        else
        {
            Debug.LogWarning("[AppWindow] No content to display for file");
        }
    }
    
    /// <summary>
    /// Instantiate app content
    /// </summary>
    private void InstantiateAppContent(GameObject appPrefab)
    {
        if (currentContent != null)
        {
            Destroy(currentContent);
        }
        
        currentContent = Instantiate(appPrefab, contentArea);
        SetupContentRectTransform(currentContent);
    }
    
    /// <summary>
    /// Instantiate file content
    /// </summary>
    private void InstantiateFileContent(GameObject filePrefab)
    {
        if (currentContent != null)
        {
            Destroy(currentContent);
        }
        
        currentContent = Instantiate(filePrefab, contentArea);
        SetupContentRectTransform(currentContent);
        
        // Try to initialize the content with file data
        var fileContent = currentContent.GetComponent<IFileContent>();
        if (fileContent != null)
        {
            fileContent.Initialize(currentFile);
        }
    }
    
    /// <summary>
    /// Setup the rect transform for content
    /// </summary>
    private void SetupContentRectTransform(GameObject content)
    {
        var contentRT = content.GetComponent<RectTransform>();
        if (contentRT != null)
        {
            contentRT.anchorMin = Vector2.zero;
            contentRT.anchorMax = Vector2.one;
            contentRT.sizeDelta = Vector2.zero;
            contentRT.anchoredPosition = Vector2.zero;
        }
    }
    
    /// <summary>
    /// Clear current content
    /// </summary>
    private void ClearContent()
    {
        if (currentContent != null)
        {
            Destroy(currentContent);
            currentContent = null;
        }
    }
    
    public void Focus()
    {
        transform.SetAsLastSibling();
    }
    
    public void OnFocusLost()
    {
        // Force restore cursor when window loses focus
        if (retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
        isDragging = false;
        isResizing = false;
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isFullscreen) return;

        // Check if we're clicking the resize handle
        if (resizeHandle != null && config.IsResizable && 
            RectTransformUtility.RectangleContainsScreenPoint(resizeHandle, eventData.position, eventData.pressEventCamera))
        {
            isResizing = true;
            isDragging = false;
            
            // Show resize cursor
            if (retroEffects != null)
            {
                retroEffects.ShowResizeCursor();
            }
            
            // Get the window's position in screen space for the anchor point
            Vector3[] corners = new Vector3[4];
            windowContainer.GetWorldCorners(corners);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[1]);
            
            // Store the initial mouse offset from the current size
            dragOffset = eventData.position - topLeft;
            startDragSize = windowContainer.sizeDelta;
            return;
        }

        // Check if we're clicking the drag handle
        if (dragHandle != null && 
            RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
        {
            isDragging = true;
            isResizing = false;

            // Show move cursor
            if (retroEffects != null)
            {
                retroEffects.ShowMoveCursor();
            }

            // Get the mouse position in the parent's space
            Vector2 mousePositionInParent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                windowContainer.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out mousePositionInParent
            );

            // Calculate the offset from the mouse to the window's position
            dragOffset = mousePositionInParent - windowContainer.anchoredPosition;
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isFullscreen) return;

        // Check if cursor is within desktop bounds
        var parent = windowContainer.parent as RectTransform;
        if (parent == null) return;

        bool isInDesktopBounds = RectTransformUtility.RectangleContainsScreenPoint(parent, eventData.position, eventData.pressEventCamera);

        if (isResizing && isInDesktopBounds)
        {
            // Get the window's position in screen space
            Vector3[] corners = new Vector3[4];
            windowContainer.GetWorldCorners(corners);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, corners[1]);
            
            // Calculate size from the anchor point (top-left) to the mouse
            Vector2 currentMouseOffset = eventData.position - topLeft;
            Vector2 sizeDelta = currentMouseOffset - dragOffset;
            
            // Fix Y-axis direction (moving mouse down should increase height)
            sizeDelta.y = -sizeDelta.y;
            
            Vector2 newSize = startDragSize + sizeDelta;

            // Ensure minimum size
            newSize.x = Mathf.Max(newSize.x, minWindowSize.x);
            newSize.y = Mathf.Max(newSize.y, minWindowSize.y);

            // Apply the new size
            windowContainer.sizeDelta = newSize;
        }
        else if (isDragging && isInDesktopBounds)
        {
            // Get the current mouse position in parent space
            Vector2 mousePositionInParent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                windowContainer.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out mousePositionInParent))
            {
                // Calculate desired position
                Vector2 desiredPosition = mousePositionInParent - dragOffset;
                
                // Apply normal movement
                windowContainer.anchoredPosition = desiredPosition;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Always restore default cursor when drag ends
        if (retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
        
        isDragging = false;
        isResizing = false;
        
        // Force validate cursor state after a short delay
        StartCoroutine(ValidateCursorAfterDelay());
    }
    
    private System.Collections.IEnumerator ValidateCursorAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        if (retroEffects != null)
        {
            ValidateCursorState();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Focus();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isFullscreen) return;
        
        // Check if hovering over resize handle first (higher priority)
        if (resizeHandle != null && config.IsResizable && 
            RectTransformUtility.RectangleContainsScreenPoint(resizeHandle, eventData.position, eventData.pressEventCamera))
        {
            if (retroEffects != null)
            {
                retroEffects.ShowResizeCursor();
            }
            return; // Exit early to avoid checking drag handle
        }
        
        // Check if hovering over drag handle
        if (dragHandle != null && 
            RectTransformUtility.RectangleContainsScreenPoint(dragHandle, eventData.position, eventData.pressEventCamera))
        {
            if (retroEffects != null)
            {
                retroEffects.ShowMoveCursor();
            }
            return; // Exit early to avoid unnecessary checks
        }
        
        // If we reach here, we're not hovering over any interactive area, so restore cursor
        if (retroEffects != null && !isDragging && !isResizing)
        {
            retroEffects.RestoreCursor();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Always restore cursor when exiting the window area, unless we're actively dragging/resizing
        if (!isDragging && !isResizing && retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
    }

    /// <summary>
    /// Force restore cursor - call this when window is closed or deactivated
    /// </summary>
    public void ForceRestoreCursor()
    {
        if (retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
        isDragging = false;
        isResizing = false;
    }

    private void ToggleFullscreen()
    {
        if (!config.IsResizable) return;

        isFullscreen = !isFullscreen;
        var parent = windowContainer.parent as RectTransform;
        if (parent == null) return;

        if (isFullscreen)
        {
            // Store current window state
            lastWindowSize = windowContainer.sizeDelta;
            lastWindowPosition = windowContainer.anchoredPosition;

            // Set to full desktop size
            windowContainer.anchorMin = Vector2.zero;
            windowContainer.anchorMax = Vector2.one;
            windowContainer.sizeDelta = Vector2.zero;
            windowContainer.anchoredPosition = Vector2.zero;

            // Hide resize handle and update fullscreen icon
            if (resizeHandle != null)
                resizeHandle.gameObject.SetActive(false);
            if (fullscreenIcon != null)
                fullscreenIcon.sprite = restoreSprite;
        }
        else
        {
            // Restore window anchoring with top-left pivot
            windowContainer.anchorMin = new Vector2(0, 1);
            windowContainer.anchorMax = new Vector2(0, 1);
            windowContainer.pivot = new Vector2(0, 1);
            
            // Restore previous window state
            windowContainer.sizeDelta = lastWindowSize;
            windowContainer.anchoredPosition = lastWindowPosition;

            // Show resize handle and update fullscreen icon
            if (resizeHandle != null && config.IsResizable)
                resizeHandle.gameObject.SetActive(true);
            if (fullscreenIcon != null)
                fullscreenIcon.sprite = fullscreenSprite;
        }
    }
} 