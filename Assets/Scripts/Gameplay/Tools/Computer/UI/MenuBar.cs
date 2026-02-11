using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;

public class MenuBar : MonoBehaviour
{
    [System.Serializable]
    public class MenuItemData
    {
        public Button button; // The button for this menu item
        public bool permanentlyDisabled = false; // If true, always disabled
        public bool conditionallyEnabled = false; // If true, enabled by condition
        public System.Func<bool> enableCondition; // Optional condition for enabling
        public System.Action onSelected; // Action to perform when selected
        public Image background; // Background image for color changes
        public TMP_Text label; // Text label for color changes
        public AppConfig appToOpen; 
    }

    [System.Serializable]
    public class MenuDropdown
    {
        public Button menuLabelButton; // The button in the menu bar
        public GameObject dropdownPanel; // The dropdown panel (VerticalLayoutGroup)
        public List<MenuItemData> menuItems; // The menu items in this dropdown
    }

    [Header("Menus")]
    public List<MenuDropdown> menus;

    [Header("Colors")]
    public Color menuLabelNormalBG = Color.clear;
    public Color menuLabelNormalText = Color.black;
    public Color menuLabelSelectedBG = Color.black;
    public Color menuLabelSelectedText = Color.white;
    public Color itemNormalBG = Color.clear;
    public Color itemNormalText = Color.black;
    public Color itemHoverBG = Color.black;
    public Color itemHoverText = Color.white;
    public Color itemDisabledBG = new Color(0.7f,0.7f,0.7f,1f);
    public Color itemDisabledText = new Color(0.5f,0.5f,0.5f,1f);

    private MenuDropdown activeMenu = null;
    private MenuItemData hoveredItem = null;
    private bool isPointerDown = false;
    private RetroComputerEffects retroEffects;

    public WindowManager windowManager;
    public DesktopManager desktopManager;
    public List<AppConfig> allAppConfigs;

    void Start()
    {
        retroEffects = RetroComputerEffects.Instance;

        foreach (var menu in menus)
        {
            // Hide all dropdowns at start
            menu.dropdownPanel.SetActive(false);
            SetMenuLabelVisual(menu, false);

            // Add pointer handlers to menu label
            var labelHandler = menu.menuLabelButton.gameObject.AddComponent<MenuBarLabelHandler>();
            labelHandler.Init(this, menu);

            // Add pointer handlers to each menu item
            foreach (var item in menu.menuItems)
            {
                var itemHandler = item.button.gameObject.AddComponent<MenuBarItemHandler>();
                itemHandler.Init(this, menu, item);
                SetMenuItemVisual(item, false, false);

                if (item.appToOpen != null)
                {
                    item.onSelected = () => {
                        if (windowManager != null && item.appToOpen != null)
                            windowManager.OpenApp(item.appToOpen);
                    };
                }
            }
        }

        // Assign enableCondition and onSelected for File menu ---
        var fileMenu = menus.Find(m => m.menuLabelButton.GetComponentInChildren<TMP_Text>().text == "File");
        if (fileMenu != null && fileMenu.menuItems.Count >= 6)
        {
            // [0] = New
            fileMenu.menuItems[0].permanentlyDisabled = true;

            // [1] = Open
            fileMenu.menuItems[1].enableCondition = () => HasSelectedItem();
            fileMenu.menuItems[1].onSelected = () => OpenSelectedItem();

            // [2] = Close
            fileMenu.menuItems[2].enableCondition = () => windowManager != null && windowManager.HasFocusedWindow;
            fileMenu.menuItems[2].onSelected = () => { if (windowManager != null) windowManager.CloseFocusedWindow(); };

            // [3] = Close All
            fileMenu.menuItems[3].enableCondition = () => windowManager != null && windowManager.HasOpenWindows;
            fileMenu.menuItems[3].onSelected = () => { if (windowManager != null) windowManager.CloseAllWindows(); };
            
            // [4] = Get Info
            fileMenu.menuItems[4].permanentlyDisabled = true;

            // [5] = Duplicate
            fileMenu.menuItems[5].permanentlyDisabled = true;
      
            // [6] = Putaway
            fileMenu.menuItems[6].permanentlyDisabled = true;
        }

        // Permanently disable Edit > Undo ---
        var editMenu = menus.Find(m => m.menuLabelButton.GetComponentInChildren<TMP_Text>().text == "Edit");
        if (editMenu != null && editMenu.menuItems.Count > 3)
        {
            // [0] = Undo
            editMenu.menuItems[0].permanentlyDisabled = true;

            // [1] = Undo
            editMenu.menuItems[1].permanentlyDisabled = true;

            // [2] = Undo
            editMenu.menuItems[2].permanentlyDisabled = true;

            // [3] = Undo
            editMenu.menuItems[3].permanentlyDisabled = true;

            // [4] = Undo
            editMenu.menuItems[4].permanentlyDisabled = true;
        }

        // --- Add visible apps to the Apple menu ---
        var appleMenu = menus.Find(m => m.menuLabelButton.GetComponentInChildren<TMP_Text>().text == ""); // Apple menu is usually blank label
        
        // Check if apple menu exists and has at least one menu item to clone from
        if (appleMenu == null || appleMenu.menuItems.Count == 0)
        {
            Debug.LogWarning("[MenuBar] Apple menu not found or has no menu items to clone from!");
            return;
        }
  
        // Try to get menu apps from AppRegistry first
        var appRegistry = FindFirstObjectByType<AppRegistry>();
        if (appRegistry != null)
        {
            foreach (var app in appRegistry.GetMenuApps())
            {
                var menuItemObj = Instantiate(appleMenu.menuItems[0].button.gameObject, appleMenu.dropdownPanel.transform); // Clone a template
                var menuItem = new MenuItemData
                {
                    button = menuItemObj.GetComponent<Button>(),
                    background = menuItemObj.GetComponent<Image>(),
                    label = menuItemObj.GetComponentInChildren<TMP_Text>(),
                    permanentlyDisabled = false,
                    enableCondition = null,
                    onSelected = () => { if (windowManager != null && app != null) windowManager.OpenApp(app); }
                };
                if (menuItem.label) menuItem.label.text = app.AppName;
                // Add pointer handler
                var itemHandler = menuItem.button.gameObject.AddComponent<MenuBarItemHandler>();
                itemHandler.Init(this, appleMenu, menuItem);
                SetMenuItemVisual(menuItem, false, false);
                appleMenu.menuItems.Add(menuItem);
            }
        }
        else
        {
            // Fallback to allAppConfigs if AppRegistry not found
            foreach (var app in allAppConfigs)
            {
                if (app.ShouldShowInMenu)
                {
                    var menuItemObj = Instantiate(appleMenu.menuItems[0].button.gameObject, appleMenu.dropdownPanel.transform); // Clone a template
                    var menuItem = new MenuItemData
                    {
                        button = menuItemObj.GetComponent<Button>(),
                        background = menuItemObj.GetComponent<Image>(),
                        label = menuItemObj.GetComponentInChildren<TMP_Text>(),
                        permanentlyDisabled = false,
                        enableCondition = null,
                        onSelected = () => { if (windowManager != null && app != null) windowManager.OpenApp(app); }
                    };
                    if (menuItem.label) menuItem.label.text = app.AppName;
                    // Add pointer handler
                    var itemHandler = menuItem.button.gameObject.AddComponent<MenuBarItemHandler>();
                    itemHandler.Init(this, appleMenu, menuItem);
                    SetMenuItemVisual(menuItem, false, false);
                    appleMenu.menuItems.Add(menuItem);
                }
            }
        }
    }

    void Update()
    {
        // If pointer is down and mouse is released, handle selection or close
        if (isPointerDown && Input.GetMouseButtonUp(0))
        {
            isPointerDown = false;
            if (activeMenu != null)
            {
                if (hoveredItem != null && !hoveredItem.permanentlyDisabled && (hoveredItem.enableCondition == null || hoveredItem.enableCondition()))
                {
                    // Show hourglass cursor before executing action
                    if (retroEffects != null)
                    {
                        retroEffects.ShowHourglassCursor();
                    }
                    
                    hoveredItem.onSelected?.Invoke();
                }
                CloseActiveMenu();
            }
        }
    }

    public void OnMenuLabelPointerDown(MenuDropdown menu)
    {
        isPointerDown = true;
        if (activeMenu != null && activeMenu != menu)
        {
            CloseActiveMenu();
        }
        OpenMenu(menu);
    }

    public void OnMenuLabelPointerEnter(MenuDropdown menu)
    {
        if (isPointerDown && activeMenu != menu)
        {
            if (activeMenu != null) CloseActiveMenu();
            OpenMenu(menu);
        }
    }

    public void OnMenuItemPointerEnter(MenuDropdown menu, MenuItemData item)
    {
        if (activeMenu == menu)
        {
            if (!IsItemEnabled(item)) return; // Don't allow hover if disabled

            if (hoveredItem != null) SetMenuItemVisual(hoveredItem, false, !IsItemEnabled(hoveredItem));
            hoveredItem = item;
            SetMenuItemVisual(item, true, false);
        }
    }

    public void OnMenuItemPointerExit(MenuDropdown menu, MenuItemData item)
    {
        if (activeMenu == menu && hoveredItem == item)
        {
            // Always restore correct color for enabled/disabled state
            SetMenuItemVisual(item, false, !IsItemEnabled(item));
            hoveredItem = null;
        }
    }

    private void OpenMenu(MenuDropdown menu)
    {
        activeMenu = menu;
        menu.dropdownPanel.SetActive(true);
        SetMenuLabelVisual(menu, true);
        // Update enable/disable state for all items
        foreach (var item in menu.menuItems)
        {
            bool enabled = !item.permanentlyDisabled && (item.enableCondition == null || item.enableCondition());
            item.button.interactable = enabled;
            SetMenuItemVisual(item, false, !enabled);
        }
    }

    private void CloseActiveMenu()
    {
        if (activeMenu == null || activeMenu.dropdownPanel == null)
            Debug.LogWarning("Tried to close a menu, but activeMenu or dropdownPanel was null!");
        else
        {
            if (activeMenu.dropdownPanel != null)
                activeMenu.dropdownPanel.SetActive(false);

            SetMenuLabelVisual(activeMenu, false);

            if (hoveredItem != null)
                SetMenuItemVisual(hoveredItem, false, !IsItemEnabled(hoveredItem));

            hoveredItem = null;
            activeMenu = null;
        }
    }

    private void SetMenuLabelVisual(MenuDropdown menu, bool selected)
    {
        var img = menu.menuLabelButton.GetComponent<Image>();
        var txt = menu.menuLabelButton.GetComponentInChildren<TMP_Text>();
        if (img) img.color = selected ? menuLabelSelectedBG : menuLabelNormalBG;
        if (txt) txt.color = selected ? menuLabelSelectedText : menuLabelNormalText;
    }

    private void SetMenuItemVisual(MenuItemData item, bool hovered, bool forceDisabled)
    {
        bool actuallyDisabled = forceDisabled || !IsItemEnabled(item);
        if (item.background) item.background.color = actuallyDisabled ? itemDisabledBG : (hovered ? itemHoverBG : itemNormalBG);
        if (item.label) item.label.color = actuallyDisabled ? itemDisabledText : (hovered ? itemHoverText : itemNormalText);
    }

    private bool IsItemEnabled(MenuItemData item)
    {
        return !item.permanentlyDisabled && (item.enableCondition == null || item.enableCondition());
    }

    /// <summary>
    /// Check if there's a selected item (desktop icon or file icon)
    /// </summary>
    private bool HasSelectedItem()
    {
        Debug.Log("[MenuBar] HasSelectedItem called");
        
        // Check for selected desktop icon
        if (desktopManager != null && desktopManager.HasSelectedIcon)
        {
            Debug.Log("[MenuBar] HasSelectedItem: Desktop icon selected");
            return true;
        }
            
        // Check for selected file in focused folder app
        var focusedFolderApp = GetFocusedFolderApp();
        if (focusedFolderApp != null && focusedFolderApp.HasSelectedFile)
        {
            Debug.Log("[MenuBar] HasSelectedItem: File selected in folder app");
            return true;
        }
            
        Debug.Log("[MenuBar] HasSelectedItem: No item selected");
        return false;
    }
    
    /// <summary>
    /// Open the selected item (desktop icon or file icon)
    /// </summary>
    private void OpenSelectedItem()
    {
        Debug.Log("[MenuBar] OpenSelectedItem called");
        
        // Check for selected desktop icon
        if (desktopManager != null && desktopManager.HasSelectedIcon)
        {
            Debug.Log("[MenuBar] Opening selected desktop icon");
            desktopManager.OpenSelectedApp();
            return;
        }
        
        // Check for selected file in focused folder app
        var focusedFolderApp = GetFocusedFolderApp();
        if (focusedFolderApp != null && focusedFolderApp.HasSelectedFile)
        {
            Debug.Log("[MenuBar] Opening selected file from folder app");
            focusedFolderApp.OpenSelectedFile();
            return;
        }
        
        Debug.LogWarning("[MenuBar] No selected item found to open");
    }
    
    /// <summary>
    /// Get the FolderApp from the currently focused window
    /// </summary>
    private FolderApp GetFocusedFolderApp()
    {
        Debug.Log("[MenuBar] GetFocusedFolderApp called");
        
        if (windowManager == null) 
        {
            Debug.Log("[MenuBar] GetFocusedFolderApp: WindowManager is null");
            return null;
        }
        
        // Try to get FolderApp from the currently focused window
        if (windowManager.HasFocusedWindow)
        {
            Debug.Log("[MenuBar] GetFocusedFolderApp: WindowManager has focused window");
            var focusedWindow = windowManager.GetFocusedWindow();
            if (focusedWindow != null)
            {
                Debug.Log($"[MenuBar] GetFocusedFolderApp: Focused window found: {focusedWindow.name}");
                // Look for FolderApp in the window's content
                var folderApp = focusedWindow.GetComponentInChildren<FolderApp>();
                if (folderApp != null)
                {
                    Debug.Log($"[MenuBar] GetFocusedFolderApp: Found FolderApp in focused window: {folderApp.name}");
                    return folderApp;
                }
                else
                {
                    Debug.Log("[MenuBar] GetFocusedFolderApp: No FolderApp found in focused window");
                }
            }
            else
            {
                Debug.Log("[MenuBar] GetFocusedFolderApp: Focused window is null");
            }
        }
        else
        {
            Debug.Log("[MenuBar] GetFocusedFolderApp: WindowManager has no focused window");
        }
        
        // Fallback: try to find any FolderApp in the scene
        Debug.Log("[MenuBar] GetFocusedFolderApp: Using fallback - searching for any FolderApp in scene");
        var fallbackFolderApp = FindFirstObjectByType<FolderApp>();
        if (fallbackFolderApp != null)
        {
            Debug.Log($"[MenuBar] GetFocusedFolderApp: Found fallback FolderApp: {fallbackFolderApp.name}");
        }
        else
        {
            Debug.Log("[MenuBar] GetFocusedFolderApp: No FolderApp found in scene");
        }
        return fallbackFolderApp;
    }

    // Helper classes for pointer events
    private class MenuBarLabelHandler : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler
    {
        private MenuBar bar;
        private MenuDropdown menu;
        public void Init(MenuBar bar, MenuDropdown menu) { this.bar = bar; this.menu = menu; }
        public void OnPointerDown(PointerEventData eventData) { bar.OnMenuLabelPointerDown(menu); }
        public void OnPointerEnter(PointerEventData eventData) { bar.OnMenuLabelPointerEnter(menu); }
    }
    private class MenuBarItemHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MenuBar bar;
        private MenuDropdown menu;
        private MenuItemData item;
        public void Init(MenuBar bar, MenuDropdown menu, MenuItemData item) { this.bar = bar; this.menu = menu; this.item = item; }
        public void OnPointerEnter(PointerEventData eventData) { bar.OnMenuItemPointerEnter(menu, item); }
        public void OnPointerExit(PointerEventData eventData) { bar.OnMenuItemPointerExit(menu, item); }
    }

    // Usage:
    // - Assign your hidden apps to menuOnlyApps in the Inspector.
    // - They will appear in the Apple menu and can be opened from there.
    // - To hide an app from the desktop, set its 'hidden' property (in AppConfig or similar) to true and skip it in your desktop icon population logic.
} 