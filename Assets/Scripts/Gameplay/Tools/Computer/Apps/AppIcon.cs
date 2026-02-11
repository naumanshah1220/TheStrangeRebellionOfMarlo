using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;

[RequireComponent(typeof(Button))]
public class AppIcon : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject selectionOverlay;
    
    [Header("Settings")]
    [SerializeField] private float doubleClickTime = 0.5f;
    
    private AppConfig appConfig;
    private Action<AppConfig> onDoubleClick;
    private float lastClickTime;
    private bool isSelected;
    private DesktopManager desktopManager;
    
    public AppConfig AppConfig => appConfig;
    public bool IsSelected => isSelected;
    
    private void Awake()
    {
        // Ensure the selection overlay starts hidden
        if (selectionOverlay != null)
        {
            selectionOverlay.SetActive(false);
        }
        
        // Find the desktop manager
        desktopManager = GetComponentInParent<DesktopManager>();
        if (desktopManager == null)
        {
            Debug.LogWarning("AppIcon: No DesktopManager found in parent hierarchy!");
        }
    }
    
    public void Initialize(AppConfig config, Action<AppConfig> doubleClickCallback)
    {
        appConfig = config;
        onDoubleClick = doubleClickCallback;
        
        // Set icon
        if (iconImage != null && config.AppIcon != null)
        {
            iconImage.sprite = config.AppIcon;
        }
        
        // Set name
        if (nameText != null)
        {
            nameText.text = config.AppName;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        float timeSinceLastClick = Time.time - lastClickTime;
        
        if (timeSinceLastClick <= doubleClickTime)
        {
            // Double click
            onDoubleClick?.Invoke(appConfig);
        }
        else
        {
            // Single click - notify desktop manager
            desktopManager?.OnIconClicked(this);
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
} 