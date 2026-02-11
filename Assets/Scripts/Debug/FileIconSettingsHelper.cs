using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper script to manage FileIconSettings assets
/// </summary>
public class FileIconSettingsHelper : MonoBehaviour
{
    [ContextMenu("Check FileIconSettings")]
    private void CheckFileIconSettings()
    {
        #if UNITY_EDITOR
        Debug.Log("=== Checking FileIconSettings ===");
        
        // Try to load the settings
        var settings = Resources.Load<FileIconSettings>("FileIconSettings");
        
        if (settings == null)
        {
            Debug.LogError("FileIconSettings not found in Resources folder!");
            return;
        }
        
        // Check if the asset is corrupted
        if (settings.GetType() != typeof(FileIconSettings))
        {
            Debug.LogError("FileIconSettings asset is corrupted! The script component is missing.");
            Debug.Log($"Asset type: {settings.GetType()}");
            return;
        }
        
        Debug.Log("FileIconSettings asset is valid!");
        
        // Check all the icon references
        Debug.Log($"Document Icon: {(settings.documentIcon != null ? settings.documentIcon.name : "NULL")}");
        Debug.Log($"Photo Icon: {(settings.photoIcon != null ? settings.photoIcon.name : "NULL")}");
        Debug.Log($"Video Icon: {(settings.videoIcon != null ? settings.videoIcon.name : "NULL")}");
        Debug.Log($"Audio Icon: {(settings.audioIcon != null ? settings.audioIcon.name : "NULL")}");
        Debug.Log($"Folder Icon: {(settings.folderIcon != null ? settings.folderIcon.name : "NULL")}");
        Debug.Log($"Unknown Icon: {(settings.unknownIcon != null ? settings.unknownIcon.name : "NULL")}");
        
        // Check the file icon prefab
        Debug.Log($"File Icon Prefab: {(settings.fileIconPrefab != null ? settings.fileIconPrefab.name : "NULL")}");
        
        // Check animation settings
        Debug.Log($"Icon Load Delay: {settings.iconLoadDelay}");
        Debug.Log($"Icon Fade In Duration: {settings.iconFadeInDuration}");
        
        #else
        Debug.LogWarning("This can only be run in the Unity Editor");
        #endif
    }
    
    [ContextMenu("Recreate FileIconSettings")]
    private void RecreateFileIconSettings()
    {
        #if UNITY_EDITOR
        Debug.Log("=== Recreating FileIconSettings ===");
        
        // Delete existing asset if it exists
        var existingSettings = Resources.Load<FileIconSettings>("FileIconSettings");
        if (existingSettings != null)
        {
            Debug.Log("Deleting existing FileIconSettings asset from Resources...");
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(existingSettings));
        }
        
        // Create new asset
        var newSettings = ScriptableObject.CreateInstance<FileIconSettings>();
        
        // Create the target folder path
        string targetPath = "Assets/Prefabs/Tools/Computer/Disc/FileIconSettings.asset";
        
        // Ensure the folder structure exists
        string[] folders = targetPath.Split('/');
        string currentPath = "";
        for (int i = 0; i < folders.Length - 1; i++)
        {
            if (i == 0) currentPath = folders[i];
            else currentPath += "/" + folders[i];
            
            if (!AssetDatabase.IsValidFolder(currentPath))
            {
                string parentPath = "";
                for (int j = 0; j < i; j++)
                {
                    if (j == 0) parentPath = folders[j];
                    else parentPath += "/" + folders[j];
                }
                AssetDatabase.CreateFolder(parentPath, folders[i]);
            }
        }
        
        // Save the asset
        AssetDatabase.CreateAsset(newSettings, targetPath);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"New FileIconSettings asset created at {targetPath}");
        Debug.Log("Please assign the icon sprites and file icon prefab in the inspector.");
        Debug.Log("Then assign this asset to the FolderApp's fileIconSettings field.");
        
        #else
        Debug.LogWarning("This can only be run in the Unity Editor");
        #endif
    }
    
    [ContextMenu("Test FileIconManager")]
    private void TestFileIconManager()
    {
        Debug.Log("=== Testing FileIconManager ===");
        
        // Force reload settings
        FileIconManager.ReloadSettings();
        
        // Test getting settings
        var settings = FileIconManager.Settings;
        Debug.Log($"Settings loaded: {settings != null}");
        
        // Test getting icons for each file type
        foreach (FileType fileType in System.Enum.GetValues(typeof(FileType)))
        {
            var icon = FileIconManager.GetDefaultIcon(fileType);
            Debug.Log($"{fileType}: {(icon != null ? icon.name : "NULL")}");
        }
        
        // Test getting file icon prefab
        var prefab = FileIconManager.GetFileIconPrefab();
        Debug.Log($"File Icon Prefab: {(prefab != null ? prefab.name : "NULL")}");
    }
} 