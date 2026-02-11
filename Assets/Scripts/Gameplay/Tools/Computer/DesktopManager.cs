using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DesktopManager : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform selectionBox;
    
    private AppIcon currentlySelectedIcon;
    private Vector2 dragStartPosition;
    private bool isDragging;
    private Canvas parentCanvas;
    private List<AppIcon> selectedIcons = new List<AppIcon>();
    private WindowManager windowManager;
    
    public bool HasSelectedIcon => currentlySelectedIcon != null;
    
    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        windowManager = GetComponentInParent<WindowManager>();
        
        if (selectionBox != null)
        {
            selectionBox.gameObject.SetActive(false);
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle clicks if we're not dragging
        if (!isDragging && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            DeselectAllIcons();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Only start drag if we're clicking the desktop directly
        if (eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            isDragging = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out dragStartPosition
            );
            
            if (selectionBox != null)
            {
                selectionBox.gameObject.SetActive(true);
                selectionBox.sizeDelta = Vector2.zero;
                selectionBox.anchoredPosition = dragStartPosition;
            }
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && selectionBox != null)
        {
            Vector2 localMousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                eventData.position,
                parentCanvas.worldCamera,
                out localMousePosition
            );
            
            // Calculate corners
            Vector2 min = Vector2.Min(dragStartPosition, localMousePosition);
            Vector2 max = Vector2.Max(dragStartPosition, localMousePosition);
            
            // Set position and size
            Vector2 size = max - min;
            selectionBox.anchoredPosition = min;
            selectionBox.sizeDelta = size;
            
            // Select icons in box
            SelectIconsInBox();
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isDragging && selectionBox != null)
        {
            isDragging = false;
            selectionBox.gameObject.SetActive(false);
        }
    }
    
    private void SelectIconsInBox()
    {
        if (selectionBox == null) return;
        
        Rect selectionRect = new Rect(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        var icons = GetComponentsInChildren<AppIcon>();
        
        // Clear previous selection
        DeselectAllIcons();
        selectedIcons.Clear();
        
        // Check each icon for intersection
        foreach (var icon in icons)
        {
            if (IconIntersectsWithSelection(icon, selectionRect))
            {
                icon.SetSelected(true);
                selectedIcons.Add(icon);
                if (currentlySelectedIcon == null)
                {
                    currentlySelectedIcon = icon;
                }
            }
        }
    }
    
    private bool IconIntersectsWithSelection(AppIcon icon, Rect selectionRect)
    {
        RectTransform iconRect = icon.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        iconRect.GetWorldCorners(corners);
        
        // Convert corners to local space
        for (int i = 0; i < 4; i++)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                transform as RectTransform,
                RectTransformUtility.WorldToScreenPoint(parentCanvas.worldCamera, corners[i]),
                parentCanvas.worldCamera,
                out localPoint
            );
            
            // If any corner is inside the selection rect, the icon intersects
            if (selectionRect.Contains(localPoint))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public void OnIconClicked(AppIcon clickedIcon)
    {
        // If not dragging, handle normal icon selection
        if (!isDragging)
        {
            // First, deselect all file icons to ensure only one item is selected globally
            DeselectAllFileIcons();
            
            DeselectAllIcons();
            currentlySelectedIcon = clickedIcon;
            clickedIcon.SetSelected(true);
            selectedIcons.Add(clickedIcon);
        }
    }
    
    /// <summary>
    /// Public method to deselect all desktop icons
    /// </summary>
    public void DeselectAllIconsPublic()
    {
        DeselectAllIcons();
    }
    
    /// <summary>
    /// Deselect all file icons when selecting a desktop icon
    /// </summary>
    private void DeselectAllFileIcons()
    {
        FolderApp folderApp = FindFirstObjectByType<FolderApp>();
        if (folderApp != null)
        {
            folderApp.DeselectAllFiles();
            Debug.Log("[DesktopManager] Deselected all file icons");
        }
    }
    
    private void DeselectAllIcons()
    {
        foreach (var icon in selectedIcons)
        {
            if (icon != null)
            {
                icon.SetSelected(false);
            }
        }
        selectedIcons.Clear();
        currentlySelectedIcon = null;
    }
    
    public void OpenSelectedApp()
    {
        if (currentlySelectedIcon != null)
        {
            windowManager.OpenApp(currentlySelectedIcon.AppConfig, currentlySelectedIcon);
        }
    }
} 