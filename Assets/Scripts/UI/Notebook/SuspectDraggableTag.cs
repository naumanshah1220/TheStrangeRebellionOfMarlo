using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Special draggable tag component for suspect information discovery
/// </summary>
public class SuspectDraggableTag : MonoBehaviour, IPointerClickHandler
{
    [Header("Suspect Data")]
    public string suspectValue;
    public string suspectFieldType; // suspect_fname, suspect_lname, suspect_id, suspect_portrait
    public Sprite suspectPortrait; // For portrait tags
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color highlightColor = Color.yellow;
    public Color discoveredColor = Color.green;
    
    // Components
    private Image backgroundImage;
    private TextMeshProUGUI textComponent;
    
    // State
    private bool isDiscovered = false;
    
    private void Awake()
    {
        backgroundImage = GetComponent<Image>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>();
        
        // Set initial color
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
    
    /// <summary>
    /// Initialize the suspect tag with data
    /// </summary>
    public void Initialize(string value, string fieldType, Sprite portrait = null)
    {
        suspectValue = value;
        suspectFieldType = fieldType;
        suspectPortrait = portrait;
        
        // Set text content
        if (textComponent != null)
        {
            textComponent.text = value;
        }
        
        // For portrait tags, we might want to show the image instead of text
        if (fieldType == "suspect_portrait" && portrait != null)
        {
            // If there's an image component, use it
            Image portraitImage = GetComponent<Image>();
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
            }
        }
    }
    
    /// <summary>
    /// Handle click events
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDiscovered) return; // Already discovered
        
        // Trigger suspect discovery
        DiscoverSuspectInfo();
        
        // Mark as discovered
        isDiscovered = true;
        
        // Update visual feedback
        if (backgroundImage != null)
        {
            backgroundImage.color = discoveredColor;
        }
        
        Debug.Log($"[SuspectDraggableTag] Discovered suspect info: {suspectFieldType} = {suspectValue}");
    }
    
    /// <summary>
    /// Trigger suspect discovery through the SuspectsListManager
    /// </summary>
    private void DiscoverSuspectInfo()
    {
        var suspectsManager = SuspectsListManager.Instance;
        if (suspectsManager != null)
        {
            suspectsManager.DiscoverSuspectInfo(suspectFieldType, suspectValue, suspectPortrait);
        }
        else
        {
            Debug.LogWarning("[SuspectDraggableTag] SuspectsListManager instance not found!");
        }
    }
    
    /// <summary>
    /// Handle mouse enter for visual feedback
    /// </summary>
    private void OnMouseEnter()
    {
        if (isDiscovered) return;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
        }
    }
    
    /// <summary>
    /// Handle mouse exit for visual feedback
    /// </summary>
    private void OnMouseExit()
    {
        if (isDiscovered) return;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
    
    /// <summary>
    /// Check if this tag has been discovered
    /// </summary>
    public bool IsDiscovered()
    {
        return isDiscovered;
    }
    
    /// <summary>
    /// Get the suspect field type
    /// </summary>
    public string GetSuspectFieldType()
    {
        return suspectFieldType;
    }
    
    /// <summary>
    /// Get the suspect value
    /// </summary>
    public string GetSuspectValue()
    {
        return suspectValue;
    }
    
    /// <summary>
    /// Get the suspect portrait (if applicable)
    /// </summary>
    public Sprite GetSuspectPortrait()
    {
        return suspectPortrait;
    }
} 