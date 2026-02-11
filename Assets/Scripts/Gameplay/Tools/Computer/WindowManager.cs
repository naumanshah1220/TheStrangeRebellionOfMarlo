using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;

public class WindowManager : MonoBehaviour
{
    [SerializeField] private GameObject windowPrefab;
    [SerializeField] private RectTransform windowContainer;
    
    [Header("Animation Settings")]
    [SerializeField] private float windowOpenDuration = 0.3f;
    [SerializeField] private Ease windowOpenEase = Ease.OutBack;
    
    private List<AppWindow> openWindows = new List<AppWindow>();
    private AppWindow focusedWindow;
    private RetroComputerEffects retroEffects;
    
    public bool HasFocusedWindow => focusedWindow != null;
    public bool HasOpenWindows => openWindows.Count > 0;
    
    private void Start()
    {
        retroEffects = FindFirstObjectByType<RetroComputerEffects>();
        
        // Debug logging for component assignment
        Debug.Log($"[WindowManager] Start - windowPrefab: {(windowPrefab != null ? windowPrefab.name : "NULL")}");
        Debug.Log($"[WindowManager] Start - windowContainer: {(windowContainer != null ? windowContainer.name : "NULL")}");
        
        if (windowPrefab == null)
        {
            Debug.LogError("[WindowManager] windowPrefab is not assigned in the inspector!");
        }
        
        if (windowContainer == null)
        {
            Debug.LogError("[WindowManager] windowContainer is not assigned in the inspector!");
        }
    }
    
    public void Initialize(RectTransform container)
    {
        windowContainer = container;
    }
    
    public void OpenApp(AppConfig appConfig)
    {
        OpenApp(appConfig, null);
    }
    
    public void OpenApp(AppConfig appConfig, AppIcon sourceIcon = null)
    {
        // Show hourglass cursor
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        StartCoroutine(OpenAppWithDelay(appConfig, sourceIcon));
    }
    
    /// <summary>
    /// Open an app with a specific file
    /// </summary>
    public void OpenAppWithFile(AppConfig appConfig, DiscFile file)
    {
        Debug.Log($"[WindowManager] OpenAppWithFile called with app: {appConfig?.AppName}, file: {file?.fileName}");
        
        // Validate parameters
        if (appConfig == null)
        {
            Debug.LogError("[WindowManager] OpenAppWithFile: appConfig is null!");
            return;
        }
        
        if (file == null)
        {
            Debug.LogError("[WindowManager] OpenAppWithFile: file is null!");
            return;
        }
        
        // Show hourglass cursor
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        Debug.Log($"[WindowManager] Starting OpenAppWithFileDelay coroutine...");
        StartCoroutine(OpenAppWithFileDelay(appConfig, file));
    }
    
    private IEnumerator OpenAppWithDelay(AppConfig appConfig, AppIcon sourceIcon)
    {
        // Simulate loading delay
        yield return new WaitForSeconds(0.45f);
        
        // Check if we can open multiple instances
        if (!appConfig.AllowMultipleInstances)
        {
            // Check if app is already open
            var existingWindow = openWindows.Find(w => w.AppConfig == appConfig);
            if (existingWindow != null)
            {
                existingWindow.Focus();
                yield break;
            }
        }
        
        // Check if we have the required components
        if (windowPrefab == null)
        {
            Debug.LogError("[WindowManager] Window prefab is null! Cannot open app.");
            yield break;
        }
        
        if (windowContainer == null)
        {
            Debug.LogError("[WindowManager] Window container is null! Cannot open app.");
            yield break;
        }
        
        // Create new window
        var windowObj = Instantiate(windowPrefab, windowContainer);
        var window = windowObj.GetComponent<AppWindow>();
        
        if (window != null)
        {
            window.Initialize(appConfig, OnWindowClosed);
            openWindows.Add(window);
            focusedWindow = window;
            
            // Animate window opening
            AnimateWindowOpen(window, sourceIcon);
        }
        else
        {
            Debug.LogError("[WindowManager] AppWindow component not found on window prefab!");
        }
    }
    
    private IEnumerator OpenAppWithFileDelay(AppConfig appConfig, DiscFile file)
    {
        Debug.Log($"[WindowManager] OpenAppWithFileDelay started for app: {appConfig?.AppName}");
        
        // Simulate loading delay
        yield return new WaitForSeconds(0.45f);
        
        Debug.Log($"[WindowManager] Loading delay finished, checking app config...");
        
        // Check if we can open multiple instances
        if (!appConfig.AllowMultipleInstances)
        {
            Debug.Log($"[WindowManager] Checking for existing windows...");
            // Check if app is already open
            var existingWindow = openWindows.Find(w => w.AppConfig == appConfig);
            if (existingWindow != null)
            {
                Debug.Log($"[WindowManager] Found existing window, focusing and loading file...");
                existingWindow.Focus();
                // Pass the file to the existing window
                existingWindow.LoadFile(file);
                yield break;
            }
        }
        
        Debug.Log($"[WindowManager] No existing window found, checking required components...");
        
        // Check if we have the required components
        if (windowPrefab == null)
        {
            Debug.LogError("[WindowManager] Window prefab is null! Cannot open app with file.");
            yield break;
        }
        
        if (windowContainer == null)
        {
            Debug.LogError("[WindowManager] Window container is null! Cannot open app with file.");
            yield break;
        }
        
        Debug.Log($"[WindowManager] Required components found, creating new window...");
        
        // Create new window
        var windowObj = Instantiate(windowPrefab, windowContainer);
        var window = windowObj.GetComponent<AppWindow>();
        
        if (window != null)
        {
            Debug.Log($"[WindowManager] AppWindow component found, initializing...");
            window.Initialize(appConfig, OnWindowClosed);
            openWindows.Add(window);
            focusedWindow = window;
            
            Debug.Log($"[WindowManager] Loading file into window...");
            // Load the file into the window
            window.LoadFile(file);
            
            Debug.Log($"[WindowManager] Animating window opening...");
            // Animate window opening
            AnimateWindowOpen(window, null);
        }
        else
        {
            Debug.LogError("[WindowManager] AppWindow component not found on window prefab!");
        }
    }
    
    private void AnimateWindowOpen(AppWindow window, AppIcon sourceIcon)
    {
        var windowRect = window.GetComponent<RectTransform>();
        
        // Store final position and scale
        Vector3 finalPosition = windowRect.anchoredPosition;
        Vector3 finalScale = windowRect.localScale;
        
        // Set initial state
        if (sourceIcon != null)
        {
            // Start from icon position
            var iconRect = sourceIcon.GetComponent<RectTransform>();
            Vector2 iconScreenPos = RectTransformUtility.WorldToScreenPoint(
                null, iconRect.position);
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                windowContainer, iconScreenPos, null, out Vector2 localPos))
            {
                windowRect.anchoredPosition = localPos;
            }
            
            // Start very small
            windowRect.localScale = Vector3.one * 0.1f;
        }
        else
        {
            // Start from center, small
            windowRect.localScale = Vector3.one * 0.1f;
        }
        
        // Animate to final state
        var sequence = DOTween.Sequence();
        sequence.Append(windowRect.DOAnchorPos(finalPosition, windowOpenDuration).SetEase(windowOpenEase));
        sequence.Join(windowRect.DOScale(finalScale, windowOpenDuration).SetEase(windowOpenEase));
        
        // Apply wipe effect after animation
        sequence.OnComplete(() => 
        {
            if (retroEffects != null)
            {
                retroEffects.ApplyWipeEffect(windowRect);
            }
        });
    }
    
    private void OnWindowClosed(AppWindow window)
    {
        // Force restore cursor to prevent it from getting stuck
        if (window != null)
        {
            window.ForceRestoreCursor();
            window.OnFocusLost();
        }
        
        // Show hourglass cursor for closing
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        StartCoroutine(CloseWindowWithDelay(window));
    }
    
    private IEnumerator CloseWindowWithDelay(AppWindow window)
    {
        // Animate window closing
        var windowRect = window.GetComponent<RectTransform>();
        windowRect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        
        yield return new WaitForSeconds(0.45f);
        
        if (focusedWindow == window)
        {
            focusedWindow = null;
        }
        
        openWindows.Remove(window);
        Destroy(window.gameObject);
    }
    
    public void CloseFocusedWindow()
    {
        if (focusedWindow != null)
        {
            OnWindowClosed(focusedWindow);
        }
    }
    
    public void CloseAllWindows()
    {
        // Force restore cursor on all windows to prevent it from getting stuck
        foreach (var window in openWindows)
        {
            if (window != null)
            {
                window.ForceRestoreCursor();
                window.OnFocusLost();
            }
        }
        
        // Show hourglass cursor
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        // Create a copy of the list since we'll be modifying it while iterating
        var windowsToClose = new List<AppWindow>(openWindows);
        foreach (var window in windowsToClose)
        {
            OnWindowClosed(window);
        }
    }
    
    public void SetFocusedWindow(AppWindow window)
    {
        focusedWindow = window;
    }
    
    /// <summary>
    /// Get the currently focused window
    /// </summary>
    public AppWindow GetFocusedWindow()
    {
        return focusedWindow;
    }
} 