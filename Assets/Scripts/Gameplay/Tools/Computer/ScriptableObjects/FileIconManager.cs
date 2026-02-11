using UnityEngine;

/// <summary>
/// Manages default icons for different file types
/// </summary>
public static class FileIconManager
{
    private static FileIconSettings _settings;
    private static bool _settingsChecked = false;
    
    public static FileIconSettings Settings
    {
        get
        {
            if (!_settingsChecked)
            {
                _settingsChecked = true;
                _settings = Resources.Load<FileIconSettings>("FileIconSettings");
                
                if (_settings == null)
                {
                    Debug.LogWarning("[FileIconManager] No FileIconSettings found in Resources folder!");
                }
                else
                {
                    // Check if the asset is corrupted (missing script component)
                    if (_settings.GetType() != typeof(FileIconSettings))
                    {
                        Debug.LogError("[FileIconManager] FileIconSettings asset is corrupted! The script component is missing.");
                        _settings = null;
                    }
                    else
                    {
                        Debug.Log("[FileIconManager] FileIconSettings loaded successfully");
                    }
                }
            }
            return _settings;
        }
    }
    
    /// <summary>
    /// Get the default icon for a specific file type
    /// </summary>
    public static Sprite GetDefaultIcon(FileType fileType)
    {
        if (Settings == null) 
        {
            Debug.LogWarning($"[FileIconManager] Cannot get icon for {fileType} - Settings is null");
            return null;
        }
        
        Sprite icon = null;
        switch (fileType)
        {
            case FileType.Document:
                icon = Settings.documentIcon;
                break;
            case FileType.Photo:
                icon = Settings.photoIcon;
                break;
            case FileType.Video:
                icon = Settings.videoIcon;
                break;
            case FileType.Audio:
                icon = Settings.audioIcon;
                break;
            case FileType.Folder:
                icon = Settings.folderIcon;
                break;
            case FileType.FingerprintScan:
                icon = Settings.fingerprintIcon;
                break;
            default:
                icon = Settings.unknownIcon;
                break;
        }
        
        if (icon == null)
        {
            Debug.LogWarning($"[FileIconManager] No icon found for file type: {fileType}");
        }
        
        return icon;
    }
    
    /// <summary>
    /// Get the file icon prefab
    /// </summary>
    public static GameObject GetFileIconPrefab()
    {
        if (Settings == null)
        {
            Debug.LogWarning("[FileIconManager] Cannot get file icon prefab - Settings is null");
            return null;
        }
        
        if (Settings.fileIconPrefab == null)
        {
            Debug.LogWarning("[FileIconManager] File icon prefab is not assigned in FileIconSettings");
        }
        
        return Settings.fileIconPrefab;
    }
    
    /// <summary>
    /// Force reload settings (useful for testing)
    /// </summary>
    public static void ReloadSettings()
    {
        _settings = null;
        _settingsChecked = false;
        Debug.Log("[FileIconManager] Settings reloaded");
    }
} 