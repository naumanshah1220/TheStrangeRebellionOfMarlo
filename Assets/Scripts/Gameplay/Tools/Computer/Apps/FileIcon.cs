using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class FileIcon : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectionOverlay;
    [SerializeField] private CanvasGroup canvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float doubleClickTime = 0.5f;
    
    private DiscFile file;
    private Action<FileIcon> onSingleClick;
    private Action<FileIcon> onDoubleClick;
    private float lastClickTime;
    private bool isSelected;
    private FolderApp parentFolder;
    private FileIconSettings iconSettings;
    
    public DiscFile File => file;
    public bool IsSelected => isSelected;
    
    private void Awake()
    {
        // Ensure the selection overlay starts hidden
        if (selectionOverlay != null)
        {
            selectionOverlay.SetActive(false);
        }
        
        // Get canvas group for fade animations
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Start invisible for fade-in animation
        canvasGroup.alpha = 0f;
        
        // Find the parent folder app
        parentFolder = GetComponentInParent<FolderApp>();
        if (parentFolder == null)
        {
            Debug.LogWarning("FileIcon: No FolderApp found in parent hierarchy!");
        }
    }
    
    public void Initialize(DiscFile discFile, Action<FileIcon> singleClickCallback, Action<FileIcon> doubleClickCallback, FileIconSettings settings = null)
    {
        file = discFile;
        onSingleClick = singleClickCallback;
        onDoubleClick = doubleClickCallback;
        iconSettings = settings;
        
        // Set icon
        if (iconImage != null && discFile != null)
        {
            Sprite displayIcon = discFile.GetDisplayIcon(iconSettings);
            if (displayIcon != null)
            {
                iconImage.sprite = displayIcon;
            }
        }
        
        // Set name with color
        if (nameText != null && discFile != null)
        {
            nameText.text = discFile.fileName;
            nameText.color = discFile.fileNameColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (file == null) return;
        
        Debug.Log($"[FileIcon] OnPointerClick - File: {file.fileName}, HasOpenableContent: {file.HasOpenableContent()}");
        
        float timeSinceLastClick = Time.time - lastClickTime;
        Debug.Log($"[FileIcon] Time since last click: {timeSinceLastClick}, Double click threshold: {doubleClickTime}");
        
        if (timeSinceLastClick <= doubleClickTime)
        {
            // Double click - only if file has openable content
            if (file.HasOpenableContent())
            {
                Debug.Log($"[FileIcon] Double click detected - opening {file.fileName}");
                onDoubleClick?.Invoke(this);
            }
            else
            {
                Debug.Log($"[FileIcon] Double click detected but file has no openable content: {file.fileName}");
            }
        }
        else
        {
            // Single click - notify parent folder directly (like AppIcon does with DesktopManager)
            Debug.Log($"[FileIcon] Single click - notifying FolderApp about {file.fileName}");
            if (parentFolder != null)
            {
                parentFolder.OnFileIconClicked(this);
            }
            else
            {
                // Fallback to callback if no parent folder found
                onSingleClick?.Invoke(this);
            }
        }
        
        lastClickTime = Time.time;
    }
    
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionOverlay != null)
        {
            selectionOverlay.SetActive(selected);
        }
    }
    
    /// <summary>
    /// Animate the icon fading in
    /// </summary>
    public void AnimateIn(float delay = 0f)
    {
        if (canvasGroup == null) return;
        
        // Start invisible
        canvasGroup.alpha = 0f;
        
        // Animate fade in with delay
        canvasGroup.DOFade(1f, FileIconManager.Settings?.iconFadeInDuration ?? 0.3f)
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);
    }
    
    /// <summary>
    /// Animate the icon fading out
    /// </summary>
    public void AnimateOut(float delay = 0f)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.DOFade(0f, FileIconManager.Settings?.iconFadeInDuration ?? 0.3f)
            .SetDelay(delay)
            .SetEase(Ease.InQuad);
    }
    
    /// <summary>
    /// Update the file icon's display based on the file data
    /// </summary>
    public void RefreshDisplay()
    {
        if (file == null) return;
        
        // Update icon
        if (iconImage != null)
        {
            Sprite displayIcon = file.GetDisplayIcon(iconSettings);
            if (displayIcon != null)
            {
                iconImage.sprite = displayIcon;
            }
        }
        
        // Update name and color
        if (nameText != null)
        {
            nameText.text = file.fileName;
            nameText.color = file.fileNameColor;
        }
        
        // Files are always interactable (removed locked state functionality)
    }
} 