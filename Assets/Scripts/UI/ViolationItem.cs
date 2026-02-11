using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ViolationItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Components")]
    public TextMeshProUGUI violationNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI severityText;
    public Button selectButton;
    public Image backgroundImage;
    
    [Header("Visual States")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.cyan;
    public Color hoverColor = Color.gray;
    
    // State
    private CriminalViolation violation;
    private bool isSelected = false;
    private System.Action<ViolationItem, CriminalViolation> onSelected;
    
    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(OnItemClicked);
    }
    
    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
    }
    
    /// <summary>
    /// Setup the violation item with data and callback
    /// </summary>
    public void Setup(CriminalViolation violationData, System.Action<ViolationItem, CriminalViolation> selectionCallback)
    {
        violation = violationData;
        onSelected = selectionCallback;
        
        // Update UI
        if (violationNameText != null)
            violationNameText.text = violation.violationName;
            
        if (descriptionText != null)
            descriptionText.text = violation.description;
            
        if (severityText != null)
        {
            severityText.text = violation.severity.ToString();
            
            // Color code by severity
            switch (violation.severity)
            {
                case CrimeSeverity.Minor:
                    severityText.color = Color.green;
                    break;
                case CrimeSeverity.Moderate:
                    severityText.color = Color.yellow;
                    break;
                case CrimeSeverity.Major:
                    severityText.color = Color.cyan;
                    break;
                case CrimeSeverity.Severe:
                    severityText.color = Color.red;
                    break;
            }
        }
        
        // Set initial state
        SetSelected(false);
    }
    
    /// <summary>
    /// Set the selected state of this item
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        
        if (backgroundImage != null)
        {
            backgroundImage.color = isSelected ? selectedColor : normalColor;
        }
        
        // Optional: Add scale animation for selection
        if (isSelected)
        {
            transform.localScale = Vector3.one * 1.05f;
        }
        else
        {
            transform.localScale = Vector3.one;
        }
    }
    
    /// <summary>
    /// Called when item is clicked
    /// </summary>
    private void OnItemClicked()
    {
        onSelected?.Invoke(this, violation);
    }
    
    /// <summary>
    /// Handle pointer enter for hover effect
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && backgroundImage != null)
        {
            backgroundImage.color = hoverColor;
        }
    }
    
    /// <summary>
    /// Handle pointer exit for hover effect
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isSelected && backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }
    }
} 