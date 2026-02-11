using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Helper script to create DraggableFileIcon prefab
/// </summary>
public class DraggableFileIconHelper : MonoBehaviour
{
    [ContextMenu("Create DraggableFileIcon Prefab")]
    private void CreateDraggableFileIconPrefab()
    {
        #if UNITY_EDITOR
        Debug.Log("=== Creating DraggableFileIcon Prefab ===");
        
        // Load the existing FileIcon prefab
        var fileIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tools/Computer/Disc/FileIcon.prefab");
        
        if (fileIconPrefab == null)
        {
            Debug.LogError("FileIcon prefab not found at Assets/Prefabs/Tools/Computer/Disc/FileIcon.prefab!");
            return;
        }
        
        // Create a copy of the prefab
        var draggableFileIconPrefab = Instantiate(fileIconPrefab);
        draggableFileIconPrefab.name = "DraggableFileIcon";
        
        // Remove the FileIcon component
        var fileIconComponent = draggableFileIconPrefab.GetComponent<FileIcon>();
        if (fileIconComponent != null)
        {
            DestroyImmediate(fileIconComponent);
        }
        
        // Add the DraggableFileIcon component
        var draggableFileIconComponent = draggableFileIconPrefab.AddComponent<DraggableFileIcon>();
        
        // Set up the references (same as FileIcon)
        var iconImage = draggableFileIconPrefab.transform.Find("DesktopIcon")?.GetComponent<Image>();
        var nameText = draggableFileIconPrefab.transform.Find("FileName")?.GetComponent<TMPro.TextMeshProUGUI>();
        var selectionOverlay = draggableFileIconPrefab.transform.Find("SelectionOverlay")?.gameObject;
        var canvasGroup = draggableFileIconPrefab.GetComponent<CanvasGroup>();
        
        // Use reflection to set the private fields
        var iconImageField = typeof(DraggableFileIcon).GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameTextField = typeof(DraggableFileIcon).GetField("nameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var selectionOverlayField = typeof(DraggableFileIcon).GetField("selectionOverlay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var canvasGroupField = typeof(DraggableFileIcon).GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (iconImageField != null) iconImageField.SetValue(draggableFileIconComponent, iconImage);
        if (nameTextField != null) nameTextField.SetValue(draggableFileIconComponent, nameText);
        if (selectionOverlayField != null) selectionOverlayField.SetValue(draggableFileIconComponent, selectionOverlay);
        if (canvasGroupField != null) canvasGroupField.SetValue(draggableFileIconComponent, canvasGroup);
        
        // Save the prefab
        string prefabPath = "Assets/Prefabs/Tools/Computer/Disc/DraggableFileIcon.prefab";
        bool success = PrefabUtility.SaveAsPrefabAsset(draggableFileIconPrefab, prefabPath);
        
        if (success)
        {
            Debug.Log($"DraggableFileIcon prefab created successfully at {prefabPath}");
            Debug.Log("Now you need to:");
            Debug.Log("1. Open the FileIconSettings asset");
            Debug.Log("2. Change the fileIconPrefab field to point to the new DraggableFileIcon prefab");
            Debug.Log("3. Or update the FolderApp to use the new prefab");
        }
        else
        {
            Debug.LogError("Failed to save DraggableFileIcon prefab!");
        }
        
        // Clean up the temporary object
        DestroyImmediate(draggableFileIconPrefab);
        
        #else
        Debug.LogWarning("This can only be run in the Unity Editor");
        #endif
    }
    
    [ContextMenu("Update FileIconSettings to Use DraggableFileIcon")]
    private void UpdateFileIconSettings()
    {
        #if UNITY_EDITOR
        Debug.Log("=== Updating FileIconSettings ===");
        
        // Load the FileIconSettings asset
        var settings = AssetDatabase.LoadAssetAtPath<FileIconSettings>("Assets/Prefabs/Tools/Computer/Disc/FileIconSettings.asset");
        
        if (settings == null)
        {
            Debug.LogError("FileIconSettings not found!");
            return;
        }
        
        // Load the DraggableFileIcon prefab
        var draggablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Tools/Computer/Disc/DraggableFileIcon.prefab");
        
        if (draggablePrefab == null)
        {
            Debug.LogError("DraggableFileIcon prefab not found! Run 'Create DraggableFileIcon Prefab' first.");
            return;
        }
        
        // Update the fileIconPrefab field
        settings.fileIconPrefab = draggablePrefab;
        
        // Mark the asset as dirty and save
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        
        Debug.Log("FileIconSettings updated to use DraggableFileIcon prefab!");
        
        #else
        Debug.LogWarning("This can only be run in the Unity Editor");
        #endif
    }
} 