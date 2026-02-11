using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to set up header text elements in the notebook
/// This can be used to automatically create header text components
/// </summary>
public class NotebookHeaderSetup : MonoBehaviour
{
    [Header("Header Setup")]
    public bool autoSetupHeaders = true;
    public string headerFontAssetPath = "Fonts & Materials/LiberationSans SDF";
    public float headerFontSize = 28f;
    public float subHeaderFontSize = 18f;
    public Color headerColor = Color.black;
    public Color subHeaderColor = Color.gray;
    
    [Header("Header Positioning")]
    public Vector2 leftHeaderOffset = new Vector2(0, -20);
    public Vector2 rightHeaderOffset = new Vector2(0, -20);
    public float headerSpacing = 5f;
    
    private NotebookManager notebookManager;
    
    private void Awake()
    {
        if (autoSetupHeaders)
        {
            SetupHeaders();
        }
    }
    
    /// <summary>
    /// Set up header text elements for the notebook
    /// </summary>
    [ContextMenu("Setup Headers")]
    public void SetupHeaders()
    {
        notebookManager = GetComponent<NotebookManager>();
        if (notebookManager == null)
        {
            Debug.LogError("[NotebookHeaderSetup] NotebookManager component not found!");
            return;
        }
        
        // Create left page headers
        CreateHeaderText(notebookManager.leftPageParent, "LeftPageHeader", leftHeaderOffset, true);
        CreateHeaderText(notebookManager.leftPageParent, "LeftPageSubHeader", leftHeaderOffset + Vector2.down * (headerFontSize + headerSpacing), false);
        
        // Create right page headers
        CreateHeaderText(notebookManager.rightPageParent, "RightPageHeader", rightHeaderOffset, true);
        CreateHeaderText(notebookManager.rightPageParent, "RightPageSubHeader", rightHeaderOffset + Vector2.down * (headerFontSize + headerSpacing), false);
        
        Debug.Log("[NotebookHeaderSetup] Headers set up successfully!");
    }
    
    /// <summary>
    /// Create a header text element
    /// </summary>
    private void CreateHeaderText(RectTransform parent, string name, Vector2 offset, bool isMainHeader)
    {
        if (parent == null) return;
        
        // Create GameObject
        GameObject headerObj = new GameObject(name);
        headerObj.transform.SetParent(parent, false);
        
        // Add RectTransform
        RectTransform rectTransform = headerObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = offset;
        rectTransform.sizeDelta = new Vector2(0, isMainHeader ? headerFontSize + 10 : subHeaderFontSize + 10);
        
        // Add TextMeshProUGUI
        TextMeshProUGUI textComponent = headerObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = isMainHeader ? "Header" : "Sub Header";
        textComponent.fontSize = isMainHeader ? headerFontSize : subHeaderFontSize;
        textComponent.color = isMainHeader ? headerColor : subHeaderColor;
        textComponent.fontStyle = isMainHeader ? FontStyles.Bold : FontStyles.Normal;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.horizontalAlignment = HorizontalAlignmentOptions.Left;
        textComponent.verticalAlignment = VerticalAlignmentOptions.Top;
        
        // Try to load font asset
        TMP_FontAsset fontAsset = Resources.Load<TMP_FontAsset>(headerFontAssetPath);
        if (fontAsset != null)
        {
            textComponent.font = fontAsset;
        }
        
        // Add LayoutElement to ensure proper positioning
        LayoutElement layoutElement = headerObj.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true; // Don't let layout system affect positioning
        
        // Assign to NotebookManager
        if (isMainHeader)
        {
            if (name.Contains("Left"))
                notebookManager.leftPageHeaderText = textComponent;
            else
                notebookManager.rightPageHeaderText = textComponent;
        }
        else
        {
            if (name.Contains("Left"))
                notebookManager.leftPageSubHeaderText = textComponent;
            else
                notebookManager.rightPageSubHeaderText = textComponent;
        }
    }
    
    /// <summary>
    /// Remove all header text elements
    /// </summary>
    [ContextMenu("Remove Headers")]
    public void RemoveHeaders()
    {
        if (notebookManager == null)
            notebookManager = GetComponent<NotebookManager>();
            
        if (notebookManager == null) return;
        
        // Remove left page headers
        RemoveHeaderText(notebookManager.leftPageParent, "LeftPageHeader");
        RemoveHeaderText(notebookManager.leftPageParent, "LeftPageSubHeader");
        
        // Remove right page headers
        RemoveHeaderText(notebookManager.rightPageParent, "RightPageHeader");
        RemoveHeaderText(notebookManager.rightPageParent, "RightPageSubHeader");
        
        // Clear references
        notebookManager.leftPageHeaderText = null;
        notebookManager.leftPageSubHeaderText = null;
        notebookManager.rightPageHeaderText = null;
        notebookManager.rightPageSubHeaderText = null;
        
        Debug.Log("[NotebookHeaderSetup] Headers removed successfully!");
    }
    
    /// <summary>
    /// Remove a specific header text element
    /// </summary>
    private void RemoveHeaderText(RectTransform parent, string name)
    {
        if (parent == null) return;
        
        Transform headerTransform = parent.Find(name);
        if (headerTransform != null)
        {
            DestroyImmediate(headerTransform.gameObject);
        }
    }
} 