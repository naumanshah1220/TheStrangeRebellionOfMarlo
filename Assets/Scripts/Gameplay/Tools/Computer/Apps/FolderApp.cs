using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FolderApp : ComputerApp, IPointerClickHandler
{
    [Header("Folder App References")]
    [SerializeField] private Transform fileIconContainer;
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private FileIconSettings fileIconSettings;
    
    private AppConfig currentAppConfig;
    private List<FileIcon> activeFileIcons = new List<FileIcon>();
    private List<DraggableFileIcon> activeDraggableFileIcons = new List<DraggableFileIcon>();
    private FileIcon selectedFileIcon;
    private DraggableFileIcon selectedDraggableFileIcon;
    private Evidence sourceEvidence;
    private Coroutine loadingCoroutine;
    private Image backgroundImage; // For raycast detection
    
    public FileIcon SelectedFile => selectedFileIcon;
    public AppConfig CurrentAppConfig => currentAppConfig;
    
    private void Awake()
    {
        // Ensure we have a background Image component for raycast detection
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
        
        // Make background transparent but still raycast target
        backgroundImage.color = new Color(0, 0, 0, 0.01f); // Nearly transparent but still detectable
        backgroundImage.raycastTarget = true;
        
        Debug.Log($"[FolderApp] Background image setup complete. Color: {backgroundImage.color}, Raycast: {backgroundImage.raycastTarget}");
    }
    
    public override void Initialize(AppConfig appConfig)
    {
        base.Initialize(appConfig);
                
        // Set up initial UI state
        if (statusText != null)
        {
            statusText.text = "Ready";
        }
        
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
        
        // Load the app config if it's a folder type
        if (appConfig != null && appConfig.IsFolder)
        {
            LoadFolderApp(appConfig);
        }
        else
        {
            // Try to get from current disc evidence as fallback
            LoadCurrentDiscApp();
        }
    }
    
    public override void OnAppOpen()
    {
        base.OnAppOpen();
        
        // Refresh the app when it opens
        LoadCurrentDiscApp();
    }
    
    /// <summary>
    /// Handle clicks on empty space to deselect files
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;
        
        // Check if the clicked object is a FileIcon or child of a FileIcon
        FileIcon fileIcon = clickedObject?.GetComponentInParent<FileIcon>();
        
        // Also check if it's a Button (which might be part of the FileIcon)
        Button clickedButton = clickedObject?.GetComponent<Button>();
        
        if (fileIcon == null && (clickedButton == null || clickedButton.transform.parent?.GetComponent<FileIcon>() == null))
        {
            // No FileIcon found and no Button that's part of a FileIcon - this is a background click
            Debug.Log($"[FolderApp] Background click on '{clickedObject?.name}', deselecting files");
            DeselectAllFiles();
        }
        else
        {
            Debug.Log($"[FolderApp] FileIcon or FileIcon button click detected: {clickedObject?.name} - ignoring background click");
        }
    }
    
    /// <summary>
    /// Called by FileIcon when it's clicked (similar to DesktopManager.OnIconClicked)
    /// </summary>
    public void OnFileIconClicked(FileIcon clickedIcon)
    {
        Debug.Log($"[FolderApp] OnFileIconClicked: {clickedIcon.File?.fileName}");
        
        // First, deselect all desktop icons to ensure only one item is selected globally
        DeselectAllDesktopIcons();
        
        // Deselect previous file selection
        if (selectedFileIcon != null)
        {
            selectedFileIcon.SetSelected(false);
        }
        
        // Select new file
        selectedFileIcon = clickedIcon;
        clickedIcon.SetSelected(true);
        
        // Update status
        if (statusText != null)
        {
            statusText.text = $"Selected: {clickedIcon.File.fileName}";
        }
    }
    
    /// <summary>
    /// Deselect all files
    /// </summary>
    public void DeselectAllFiles()
    {
        if (selectedFileIcon != null)
        {
            Debug.Log($"[FolderApp] Deselecting file: {selectedFileIcon.File?.fileName}");
            selectedFileIcon.SetSelected(false);
            selectedFileIcon = null;
        }
        
        if (selectedDraggableFileIcon != null)
        {
            Debug.Log($"[FolderApp] Deselecting draggable file: {selectedDraggableFileIcon.File?.fileName}");
            selectedDraggableFileIcon.SetSelected(false);
            selectedDraggableFileIcon = null;
        }
            
            // Update status
            if (statusText != null)
            {
            int totalFiles = activeFileIcons.Count + activeDraggableFileIcons.Count;
            statusText.text = $"{totalFiles} files";
        }
    }
    
    /// <summary>
    /// Load the app from the currently inserted disc
    /// </summary>
    private void LoadCurrentDiscApp()
    {
        Debug.Log("[FolderApp] LoadCurrentDiscApp called");
        
        // Find the computer system to get current disc
        ComputerSystem computerSystem = FindFirstObjectByType<ComputerSystem>();
        if (computerSystem == null)
        {
            ShowError("No computer system found");
            return;
        }
        
        // Get the current disc evidence
        if (computerSystem.CurrentDiscCard != null)
        {
            Evidence evidence = computerSystem.CurrentDiscCard.GetEvidenceData();
            if (evidence != null && evidence.HasAssociatedApp)
            {
                LoadFolderApp(evidence.AppConfig, evidence);
            }
            else
            {
                ShowError("Disc has no associated app");
            }
        }
        else
        {
            ShowError("No disc inserted");
        }
    }
    
    /// <summary>
    /// Load a specific folder app
    /// </summary>
    public void LoadFolderApp(AppConfig appConfig, Evidence evidence = null)
    {
        if (appConfig == null)
        {
            ShowError("App config is null");
            return;
        }
        
        Debug.Log($"[FolderApp] Loading folder app: {appConfig.AppName}, IsFolder: {appConfig.IsFolder}, Files: {appConfig.files.Count}");
        
        currentAppConfig = appConfig;
        sourceEvidence = evidence;
        
        // Clear existing icons
        ClearFileIcons();
        
        // Configure grid layout based on app config settings
        ConfigureGridLayout();
        
        // Start loading files with animation
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
        }
        loadingCoroutine = StartCoroutine(LoadFilesWithAnimation());
    }
    
    /// <summary>
    /// Configure the grid layout based on app config settings
    /// </summary>
    private void ConfigureGridLayout()
    {
        if (gridLayout == null || currentAppConfig == null) return;
        
        // Keep the prefab's cell size, only update spacing and constraint
        gridLayout.spacing = new Vector2(currentAppConfig.iconSpacing, currentAppConfig.iconSpacing);
        gridLayout.constraintCount = currentAppConfig.iconsPerRow;
    }
    
    /// <summary>
    /// Load files with staggered animation
    /// </summary>
    private IEnumerator LoadFilesWithAnimation()
    {
        Debug.Log("[FolderApp] Starting LoadFilesWithAnimation");
        
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            loadingText.text = "Loading files...";
        }
        
        if (statusText != null)
        {
            statusText.text = "Loading...";
        }
        
        // Get visible files
        List<DiscFile> visibleFiles = currentAppConfig.GetVisibleFiles();
        Debug.Log($"[FolderApp] Found {visibleFiles.Count} visible files");
        
        // Wait a moment before starting
        yield return new WaitForSeconds(0.3f);
        
        // Hide loading text
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
        
        // Create and animate file icons one by one
        for (int i = 0; i < visibleFiles.Count; i++)
        {
            DiscFile file = visibleFiles[i];
            Debug.Log($"[FolderApp] Creating icon for file: {file.fileName}");
            CreateFileIcon(file, i * 0.2f); // Fixed delay instead of relying on Settings
            
            // Update status
            if (statusText != null)
            {
                statusText.text = $"Loading {i + 1}/{visibleFiles.Count}";
            }
            
            // Wait between file loads
            yield return new WaitForSeconds(0.2f);
        }
        
        // Update final status
        if (statusText != null)
        {
            int totalFiles = activeFileIcons.Count + activeDraggableFileIcons.Count;
            statusText.text = $"{totalFiles} files";
        }
        
        Debug.Log($"[FolderApp] Finished loading {visibleFiles.Count} files");
        loadingCoroutine = null;
    }
    
    /// <summary>
    /// Create a file icon for a specific file
    /// </summary>
    private void CreateFileIcon(DiscFile file, float animationDelay)
    {
        if (file == null)
        {
            Debug.LogError("[FolderApp] File is null, cannot create icon");
            return;
        }
        
        if (fileIconContainer == null)
        {
            Debug.LogError("[FolderApp] fileIconContainer is null, cannot create icon");
            return;
        }
        
        Debug.Log($"[FolderApp] Creating icon for file: {file.fileName}, container: {fileIconContainer.name}");
        
        // Get the file icon prefab from direct reference
        GameObject iconPrefab = null;
        
        if (fileIconSettings != null)
        {
            iconPrefab = fileIconSettings.fileIconPrefab;
            Debug.Log($"[FolderApp] Using FileIconSettings: {fileIconSettings.name}");
        }
        else
        {
            Debug.LogWarning("[FolderApp] FileIconSettings not assigned! Trying fallback methods...");
            
            // Fallback 1: Try FileIconManager
            iconPrefab = FileIconManager.GetFileIconPrefab();
            if (iconPrefab != null)
            {
                Debug.Log("[FolderApp] Using FileIconManager fallback");
            }
            else
            {
                // Fallback 2: Try to find AppIcon prefab
                AppRegistry appRegistry = FindFirstObjectByType<AppRegistry>();
                if (appRegistry != null)
                {
                    // Use reflection to get the appIconPrefab field
                    var field = typeof(AppRegistry).GetField("appIconPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        iconPrefab = field.GetValue(appRegistry) as GameObject;
                        Debug.Log($"[FolderApp] Using AppIcon prefab as fallback: {iconPrefab?.name}");
                    }
                }
            }
        }
        
        if (iconPrefab == null)
        {
            Debug.LogError("[FolderApp] No icon prefab found at all! Cannot create file icon.");
            return;
        }
        
        // Instantiate the icon
        GameObject iconObj = Instantiate(iconPrefab, fileIconContainer);
        Debug.Log($"[FolderApp] Instantiated icon object: {iconObj.name}");
        
        // First try to get DraggableFileIcon (preferred for drag-and-drop functionality)
        DraggableFileIcon draggableFileIcon = iconObj.GetComponent<DraggableFileIcon>();
        if (draggableFileIcon != null)
        {
            Debug.Log($"[FolderApp] DraggableFileIcon component found, initializing...");
            // Initialize the draggable icon
            draggableFileIcon.Initialize(file, OnDraggableFileSingleClicked, OnDraggableFileDoubleClicked, fileIconSettings);
            
            // Add to active draggable icons list
            activeDraggableFileIcons.Add(draggableFileIcon);
            
            // Animate in with delay
            draggableFileIcon.AnimateIn(animationDelay);
        }
        else
        {
            // Fallback to regular FileIcon
        FileIcon fileIcon = iconObj.GetComponent<FileIcon>();
        
        if (fileIcon != null)
        {
            Debug.Log($"[FolderApp] FileIcon component found, initializing...");
            // Initialize the icon - only need double-click callback now since single clicks go through OnFileIconClicked
            fileIcon.Initialize(file, null, OnFileDoubleClicked, fileIconSettings);
            
            // Add to active icons list
            activeFileIcons.Add(fileIcon);
            
            // Animate in with delay
            fileIcon.AnimateIn(animationDelay);
        }
        else
        {
                Debug.LogWarning($"[FolderApp] No FileIcon or DraggableFileIcon component found on {iconObj.name}, trying AppIcon instead...");
            
            // If no FileIcon component, maybe it's an AppIcon prefab - try to adapt it
            AppIcon appIcon = iconObj.GetComponent<AppIcon>();
            if (appIcon != null)
            {
                Debug.Log("[FolderApp] Found AppIcon component, will adapt it for file display");
                // For now, just show it as a basic icon
                // TODO: We could create a wrapper or adapter here if needed
            }
            else
            {
                    Debug.LogError($"[FolderApp] No FileIcon, DraggableFileIcon, or AppIcon component found on {iconObj.name}!");
                Destroy(iconObj);
                }
            }
        }
    }
    
    /// <summary>
    /// Handle file opening (double click) for regular FileIcon
    /// </summary>
    private void OnFileDoubleClicked(FileIcon fileIcon)
    {
        if (fileIcon?.File == null) return;
        
        DiscFile file = fileIcon.File;
        Debug.Log($"[FolderApp] Double-clicked file: {file.fileName} (Type: {file.fileType})");
        
        // Open the file with the appropriate viewer app
        OpenFileWithViewer(file);
    }
    
    /// <summary>
    /// Handle single click for DraggableFileIcon
    /// </summary>
    private void OnDraggableFileSingleClicked(DraggableFileIcon fileIcon)
    {
        if (fileIcon?.File == null) return;
        
        Debug.Log($"[FolderApp] Single-clicked draggable file: {fileIcon.File.fileName}");
        
        // Handle single click (selection) - similar to OnFileIconClicked
        DeselectAllDesktopIcons();
        
        // Deselect previous file selections
        if (selectedFileIcon != null)
        {
            selectedFileIcon.SetSelected(false);
            selectedFileIcon = null;
        }
        
        if (selectedDraggableFileIcon != null)
        {
            selectedDraggableFileIcon.SetSelected(false);
        }
        
        // Select new draggable file
        selectedDraggableFileIcon = fileIcon;
        fileIcon.SetSelected(true);
        
        // Update status
        if (statusText != null)
        {
            statusText.text = $"Selected: {fileIcon.File.fileName}";
        }
    }
    
    /// <summary>
    /// Handle file opening (double click) for DraggableFileIcon
    /// </summary>
    private void OnDraggableFileDoubleClicked(DraggableFileIcon fileIcon)
    {
        if (fileIcon?.File == null) return;
        
        DiscFile file = fileIcon.File;
        Debug.Log($"[FolderApp] Double-clicked draggable file: {file.fileName} (Type: {file.fileType})");
        
        // Open the file with the appropriate viewer app
        OpenFileWithViewer(file);
    }
    
    /// <summary>
    /// Open the appropriate viewer app for a file
    /// </summary>
    private void OpenFileWithViewer(DiscFile file)
    {
        Debug.Log($"[FolderApp] Opening file with viewer: {file.fileName} (Type: {file.fileType})");
        
        // Get the viewer app for this file
        AppConfig viewerApp = file.GetViewerApp();
        Debug.Log($"[FolderApp] File's specific viewer app: {(viewerApp != null ? viewerApp.AppName : "None")}");
        
        // If no specific viewer app is set, try to get default from AppRegistry
        if (viewerApp == null)
        {
            var appRegistry = FindFirstObjectByType<AppRegistry>();
            if (appRegistry != null)
            {
                viewerApp = appRegistry.GetViewerApp(file.fileType);
                Debug.Log($"[FolderApp] Default viewer app from registry: {(viewerApp != null ? viewerApp.AppName : "None")}");
            }
            else
            {
                Debug.LogWarning("[FolderApp] AppRegistry not found!");
            }
        }
        
        if (viewerApp == null)
        {
            Debug.LogError($"[FolderApp] No viewer app found for file type: {file.fileType}");
            if (statusText != null)
            {
                statusText.text = $"No viewer available for {file.fileName}";
            }
            return;
        }
        
        Debug.Log($"[FolderApp] Using viewer app: {viewerApp.AppName} (Hidden: {viewerApp.IsHidden}, AppPrefab: {viewerApp.AppPrefab != null})");
        
        // Open the viewer app with the file
        var windowManager = FindFirstObjectByType<WindowManager>();
        if (windowManager != null)
        {
            Debug.Log($"[FolderApp] WindowManager found, calling OpenAppWithFile with app: {viewerApp.AppName} and file: {file.fileName}");
            // Pass the file data to the window manager
            windowManager.OpenAppWithFile(viewerApp, file);
        }
        else
        {
            Debug.LogError("[FolderApp] WindowManager not found!");
        }
        
        if (statusText != null)
        {
            statusText.text = $"Opening {file.fileName} with {viewerApp.AppName}";
        }
    }
    
    /// <summary>
    /// Clear all file icons
    /// </summary>
    private void ClearFileIcons()
    {
        foreach (FileIcon icon in activeFileIcons)
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }
        
        activeFileIcons.Clear();
        selectedFileIcon = null;
    }
    
    /// <summary>
    /// Show an error message
    /// </summary>
    private void ShowError(string message)
    {
        Debug.LogError($"[FolderApp] {message}");
        
        if (statusText != null)
        {
            statusText.text = $"Error: {message}";
        }
        
        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(false);
        }
    }
    
    public override void OnAppClose()
    {
        // Stop any ongoing loading
        if (loadingCoroutine != null)
        {
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        // Clear file icons
        ClearFileIcons();
        
        base.OnAppClose();
    }

    /// <summary>
    /// Get the currently selected file (for AppMenu integration)
    /// </summary>
    public DiscFile GetSelectedFile()
    {
        return selectedFileIcon?.File;
    }
    
    /// <summary>
    /// Check if there's a selected file (for AppMenu integration)
    /// </summary>
    public bool HasSelectedFile => selectedFileIcon != null;
    
    /// <summary>
    /// Open the currently selected file (for AppMenu integration)
    /// </summary>
    public void OpenSelectedFile()
    {
        if (selectedFileIcon != null)
        {
            OnFileDoubleClicked(selectedFileIcon);
        }
    }

    /// <summary>
    /// Deselect all desktop icons when selecting a file icon
    /// </summary>
    private void DeselectAllDesktopIcons()
    {
        DesktopManager desktopManager = FindFirstObjectByType<DesktopManager>();
        if (desktopManager != null)
        {
            desktopManager.DeselectAllIconsPublic();
            Debug.Log("[FolderApp] Deselected all desktop icons");
        }
    }
} 