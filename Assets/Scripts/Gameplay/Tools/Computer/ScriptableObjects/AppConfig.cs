using UnityEngine;
using System.Collections.Generic;

public enum AppType
{
    App,
    Folder
}

[CreateAssetMenu(fileName = "New App Config", menuName = "Computer/App Config")]
public class AppConfig : ScriptableObject
{
    [Header("App Identity")]
    public string AppId;
    public string AppName;
    public Sprite AppIcon;
    public AppType appType = AppType.App;
    
    [Header("App Content")]
    public GameObject AppPrefab;
    public bool SupportsScrolling;
    
    [Header("Folder Settings")]
    [Tooltip("Only used when AppType is Folder")]
    public List<DiscFile> files = new List<DiscFile>();
    [Range(2, 8)]
    public int iconsPerRow = 4;
    [Range(80, 200)]
    public float iconSize = 100f;
    [Range(10, 50)]
    public float iconSpacing = 20f;
    
    [Header("Window Settings")]
    public bool IsResizable = true;
    public bool AllowMultipleInstances = false;

    [Header("Visibility Settings")]
    [Tooltip("If checked, this app will appear on the desktop with an icon")]
    public bool IsOnDesktop = true;
    [Tooltip("If checked, this app will appear in the Apple menu")]
    public bool IsOnAppMenu = true;
    
    /// <summary>
    /// Get all files for display in folder apps
    /// Only applicable for Folder type apps
    /// </summary>
    public List<DiscFile> GetVisibleFiles()
    {
        if (appType != AppType.Folder) return new List<DiscFile>();
        
        List<DiscFile> visibleFiles = new List<DiscFile>();
        
        foreach (var file in files)
        {
            if (file != null)
            {
                visibleFiles.Add(file);
            }
        }
        
        return visibleFiles;
    }
    
    /// <summary>
    /// Check if this is a folder app
    /// </summary>
    public bool IsFolder => appType == AppType.Folder;
    
    /// <summary>
    /// Check if this app should be hidden from desktop and menu
    /// </summary>
    public bool IsHidden => !IsOnDesktop && !IsOnAppMenu;
    
    /// <summary>
    /// Check if this app should appear on desktop
    /// </summary>
    public bool ShouldShowOnDesktop => IsOnDesktop;
    
    /// <summary>
    /// Check if this app should appear in the Apple menu
    /// </summary>
    public bool ShouldShowInMenu => IsOnAppMenu;
    
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(AppId))
        {
            AppId = name;
        }
    }
} 