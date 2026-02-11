using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug helper for the computer system
/// </summary>
public class ComputerDebugHelper : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private Button testImageViewerButton;
    [SerializeField] private Button testWindowManagerButton;
    [SerializeField] private Button testAppRegistryButton;
    
    private void Start()
    {
        if (testImageViewerButton != null)
        {
            testImageViewerButton.onClick.AddListener(TestImageViewer);
        }
        
        if (testWindowManagerButton != null)
        {
            testWindowManagerButton.onClick.AddListener(TestWindowManager);
        }
        
        if (testAppRegistryButton != null)
        {
            testAppRegistryButton.onClick.AddListener(TestAppRegistry);
        }
    }
    
    private void TestImageViewer()
    {
        Debug.Log("=== Testing ImageViewer ===");
        
        // Find ImageViewer app config
        var imageViewerConfig = Resources.Load<AppConfig>("ImageViewerAppConfig");
        if (imageViewerConfig != null)
        {
            Debug.Log($"Found ImageViewer config: {imageViewerConfig.AppName}");
            Debug.Log($"- AppPrefab: {(imageViewerConfig.AppPrefab != null ? imageViewerConfig.AppPrefab.name : "NULL")}");
            Debug.Log($"- IsHidden: {imageViewerConfig.IsHidden}");
            Debug.Log($"- ShouldShowOnDesktop: {imageViewerConfig.ShouldShowOnDesktop}");
            Debug.Log($"- ShouldShowInMenu: {imageViewerConfig.ShouldShowInMenu}");
        }
        else
        {
            Debug.LogError("ImageViewerAppConfig not found in Resources!");
        }
        
        // Try to find it in the scene
        var appRegistry = FindFirstObjectByType<AppRegistry>();
        if (appRegistry != null)
        {
            Debug.Log("=== Apps in Registry ===");
            foreach (var app in appRegistry.GetRegisteredApps())
            {
                Debug.Log($"- {app.AppName}: Desktop={app.ShouldShowOnDesktop}, Menu={app.ShouldShowInMenu}, Hidden={app.IsHidden}");
            }
            
            Debug.Log("=== Desktop Apps ===");
            foreach (var app in appRegistry.GetDesktopApps())
            {
                Debug.Log($"- {app.AppName}");
            }
            
            Debug.Log("=== Menu Apps ===");
            foreach (var app in appRegistry.GetMenuApps())
            {
                Debug.Log($"- {app.AppName}");
            }
        }
    }
    
    private void TestWindowManager()
    {
        Debug.Log("=== Testing WindowManager ===");
        
        var windowManager = FindFirstObjectByType<WindowManager>();
        if (windowManager != null)
        {
            Debug.Log("WindowManager found");
            
            // Use reflection to check private fields
            var windowPrefabField = typeof(WindowManager).GetField("windowPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var windowContainerField = typeof(WindowManager).GetField("windowContainer", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (windowPrefabField != null)
            {
                var windowPrefab = windowPrefabField.GetValue(windowManager) as GameObject;
                Debug.Log($"windowPrefab: {(windowPrefab != null ? windowPrefab.name : "NULL")}");
            }
            
            if (windowContainerField != null)
            {
                var windowContainer = windowContainerField.GetValue(windowManager) as RectTransform;
                Debug.Log($"windowContainer: {(windowContainer != null ? windowContainer.name : "NULL")}");
            }
        }
        else
        {
            Debug.LogError("WindowManager not found in scene!");
        }
    }
    
    private void TestAppRegistry()
    {
        Debug.Log("=== Testing AppRegistry ===");
        
        var appRegistry = FindFirstObjectByType<AppRegistry>();
        if (appRegistry != null)
        {
            Debug.Log("AppRegistry found");
            
            // Test viewer app registration
            var photoViewer = appRegistry.GetViewerApp(FileType.Photo);
            Debug.Log($"Photo viewer: {(photoViewer != null ? photoViewer.AppName : "NULL")}");
            
            var documentViewer = appRegistry.GetViewerApp(FileType.Document);
            Debug.Log($"Document viewer: {(documentViewer != null ? documentViewer.AppName : "NULL")}");
            
            var videoViewer = appRegistry.GetViewerApp(FileType.Video);
            Debug.Log($"Video viewer: {(videoViewer != null ? videoViewer.AppName : "NULL")}");
        }
        else
        {
            Debug.LogError("AppRegistry not found in scene!");
        }
    }
    
    [ContextMenu("Run All Tests")]
    private void RunAllTests()
    {
        TestImageViewer();
        TestWindowManager();
        TestAppRegistry();
    }
} 