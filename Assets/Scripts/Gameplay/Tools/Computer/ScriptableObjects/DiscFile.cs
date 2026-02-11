using UnityEngine;

[System.Serializable]
public enum FileType
{
    Document,
    Photo, 
    Video,
    Audio,
    Folder,
    FingerprintScan
}

[CreateAssetMenu(fileName = "New Disc File", menuName = "Computer/Disc File")]
public class DiscFile : ScriptableObject
{
    [Header("File Identity")]
    public string fileName;
    public FileType fileType;
    public Sprite fileIcon; // Custom icon for this specific file (optional)
    
    [Header("File Content")]
    [TextArea(3, 5)]
    public string fileDescription; // For document content or file description
    
    [Header("Content Prefab")]
    [Tooltip("Prefab that will be instantiated in the viewer app's content area")]
    public GameObject contentPrefab;
    
    [Header("Viewer App")]
    [Tooltip("App that should open when this file is double-clicked")]
    public AppConfig viewerApp;
    
    [Header("Citizen Database Integration")]
    [Tooltip("Citizen ID this file is associated with (for photos and fingerprint scans)")]
    public string associatedCitizenId;
    
    /// <summary>
    /// Check if this file can be used for citizen database searches
    /// </summary>
    public bool CanBeUsedForCitizenSearch()
    {
        return (fileType == FileType.Photo || fileType == FileType.FingerprintScan) && 
               !string.IsNullOrEmpty(associatedCitizenId);
    }
    
    [Header("Visual Settings")]
    public Color fileNameColor = Color.white;
    
    /// <summary>
    /// Get the appropriate default icon for this file type
    /// </summary>
    public Sprite GetDisplayIcon(FileIconSettings settings = null)
    {
        if (fileIcon != null)
            return fileIcon;
            
        // Try to get icon from provided settings first
        if (settings != null)
        {
            Sprite defaultIcon = GetDefaultIconFromSettings(settings);
            if (defaultIcon != null)
                return defaultIcon;
        }
            
        // Fallback to FileIconManager
        return FileIconManager.GetDefaultIcon(fileType);
    }
    
    /// <summary>
    /// Get default icon from specific settings
    /// </summary>
    private Sprite GetDefaultIconFromSettings(FileIconSettings settings)
    {
        switch (fileType)
        {
            case FileType.Document:
                return settings.documentIcon;
            case FileType.Photo:
                return settings.photoIcon;
            case FileType.Video:
                return settings.videoIcon;
            case FileType.Audio:
                return settings.audioIcon;
            case FileType.Folder:
                return settings.folderIcon;
            default:
                return settings.unknownIcon;
        }
    }
    
    /// <summary>
    /// Check if this file has content that can be opened
    /// </summary>
    public bool HasOpenableContent()
    {
        // Check if we have a content prefab (new system)
        if (contentPrefab != null)
            return true;
            
        // For now, only content prefabs are supported
                return false;
    }
    
    /// <summary>
    /// Get the viewer app for this file type
    /// </summary>
    public AppConfig GetViewerApp()
    {
        if (viewerApp != null)
            return viewerApp;
            
        // Fallback to default viewer apps based on file type
        return GetDefaultViewerApp();
    }
    
    /// <summary>
    /// Get the default viewer app for this file type
    /// </summary>
    private AppConfig GetDefaultViewerApp()
    {
        // Try to get from AppRegistry
        var appRegistry = FindFirstObjectByType<AppRegistry>();
        if (appRegistry != null)
        {
            return appRegistry.GetViewerApp(fileType);
        }
        
        return null;
    }
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(fileName))
        {
            fileName = name;
        }
    }
} 