using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class AppRegistry : MonoBehaviour
{
    [SerializeField] private List<AppConfig> defaultApps = new List<AppConfig>();
    [SerializeField] private GameObject appIconPrefab;
    [SerializeField] private Transform desktopIconsContainer;
    
    private Dictionary<string, AppConfig> registeredApps = new Dictionary<string, AppConfig>();
    private Dictionary<string, GameObject> appIcons = new Dictionary<string, GameObject>();
    private Dictionary<FileType, AppConfig> defaultViewerApps = new Dictionary<FileType, AppConfig>();
    private WindowManager windowManager;
    
    private void Awake()
    {
        windowManager = GetComponent<WindowManager>();
        if (windowManager == null)
        {
            Debug.LogError("AppRegistry: WindowManager component not found!");
            enabled = false;
            return;
        }
    }
    
    public void Initialize(GameObject iconPrefab, Transform iconsContainer)
    {
        appIconPrefab = iconPrefab;
        desktopIconsContainer = iconsContainer;
        
        if (appIconPrefab == null || desktopIconsContainer == null)
        {
            Debug.LogError("AppRegistry: Missing required references!");
            enabled = false;
            return;
        }
    }
    
    public void LoadDefaultApps()
    {
        foreach (var appConfig in defaultApps)
        {
            RegisterApp(appConfig);
        }
    }
    
    public void RegisterApp(AppConfig appConfig)
    {
        if (appConfig == null || string.IsNullOrEmpty(appConfig.AppId))
        {
            Debug.LogError("AppRegistry: Invalid app config!");
            return;
        }
        
        if (registeredApps.ContainsKey(appConfig.AppId))
        {
            Debug.LogWarning($"AppRegistry: App {appConfig.AppId} is already registered!");
            return;
        }
        
        registeredApps[appConfig.AppId] = appConfig;
        
        // Only create desktop icon if app should show on desktop
        if (appConfig.ShouldShowOnDesktop)
        {
            CreateDesktopIcon(appConfig);
        }
    }
    
    /// <summary>
    /// Register a viewer app for a specific file type
    /// </summary>
    public void RegisterViewerApp(FileType fileType, AppConfig viewerApp)
    {
        Debug.Log($"[AppRegistry] RegisterViewerApp called: {fileType} -> {viewerApp?.AppName}");
        
        if (viewerApp == null)
        {
            Debug.LogError($"AppRegistry: Cannot register null viewer app for {fileType}");
            return;
        }
        
        defaultViewerApps[fileType] = viewerApp;
        Debug.Log($"AppRegistry: Registered {viewerApp.AppName} as viewer for {fileType} files");
    }
    
    /// <summary>
    /// Get the default viewer app for a file type
    /// </summary>
    public AppConfig GetViewerApp(FileType fileType)
    {
        Debug.Log($"[AppRegistry] GetViewerApp called for file type: {fileType}");
        Debug.Log($"[AppRegistry] Registered viewer apps: {defaultViewerApps.Count}");
        
        foreach (var kvp in defaultViewerApps)
        {
            Debug.Log($"[AppRegistry] - {kvp.Key}: {kvp.Value?.AppName}");
        }
        
        if (defaultViewerApps.TryGetValue(fileType, out AppConfig viewerApp))
        {
            Debug.Log($"[AppRegistry] Found viewer app for {fileType}: {viewerApp.AppName}");
            return viewerApp;
        }
        
        Debug.LogWarning($"AppRegistry: No viewer app registered for {fileType} files");
        return null;
    }
    
    public void InstallApp(AppConfig appConfig)
    {
        RegisterApp(appConfig);
    }
    
    public void UninstallApp(string appId)
    {
        if (!registeredApps.ContainsKey(appId))
        {
            Debug.LogWarning($"AppRegistry: App {appId} is not installed.");
            return;
        }
        
        // Close any open windows
        if (windowManager != null)
        {
            windowManager.CloseAllWindows();
        }
        
        // Remove desktop icon
        if (appIcons.TryGetValue(appId, out GameObject icon))
        {
            Destroy(icon);
            appIcons.Remove(appId);
        }
        
        registeredApps.Remove(appId);
    }
    
    public IEnumerable<AppConfig> GetRegisteredApps()
    {
        return registeredApps.Values;
    }
    
    /// <summary>
    /// Get all apps that should be visible somewhere (desktop or menu)
    /// </summary>
    public IEnumerable<AppConfig> GetVisibleApps()
    {
        List<AppConfig> visibleApps = new List<AppConfig>();
        foreach (var app in registeredApps.Values)
        {
            if (app.ShouldShowOnDesktop || app.ShouldShowInMenu)
            {
                visibleApps.Add(app);
            }
        }
        return visibleApps;
    }
    
    /// <summary>
    /// Get all apps that should appear on desktop
    /// </summary>
    public IEnumerable<AppConfig> GetDesktopApps()
    {
        List<AppConfig> desktopApps = new List<AppConfig>();
        foreach (var app in registeredApps.Values)
        {
            if (app.ShouldShowOnDesktop)
            {
                desktopApps.Add(app);
            }
        }
        return desktopApps;
    }
    
    /// <summary>
    /// Get all apps that should appear in the Apple menu
    /// </summary>
    public IEnumerable<AppConfig> GetMenuApps()
    {
        List<AppConfig> menuApps = new List<AppConfig>();
        foreach (var app in registeredApps.Values)
        {
            if (app.ShouldShowInMenu)
            {
                menuApps.Add(app);
            }
        }
        return menuApps;
    }
    
    private void CreateDesktopIcon(AppConfig appConfig)
    {
        if (appIconPrefab == null || desktopIconsContainer == null) return;
        
        var iconInstance = Instantiate(appIconPrefab, desktopIconsContainer);
        var appIcon = iconInstance.GetComponent<AppIcon>();
        
        if (appIcon != null)
        {
            appIcon.Initialize(appConfig, OnAppIconDoubleClicked);
            appIcons[appConfig.AppId] = iconInstance;
        }
    }
    
    private void OnAppIconDoubleClicked(AppConfig appConfig)
    {
        if (windowManager != null)
        {
            windowManager.OpenApp(appConfig);
        }
        else
        {
            Debug.LogError("AppRegistry: WindowManager is null!");
        }
    }
} 