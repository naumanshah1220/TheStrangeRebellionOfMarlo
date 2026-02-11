using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Clickable clue component for chat messages - similar to notebook tags
/// </summary>
[RequireComponent(typeof(Button))]
public class ClickableClue : MonoBehaviour, IPointerClickHandler
{
    [Header("Clue Settings")]
    public string clueText;
    public string noteText; // The text to add to notebook
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;
    public Color clickedColor = Color.green;
    public float fadeInDuration = 0.3f;
    
    private Button button;
    private TextMeshProUGUI textComponent;
    private NotebookManager notebook;
    private bool hasBeenClicked = false;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        textComponent = GetComponent<TextMeshProUGUI>();
        notebook = FindFirstObjectByType<NotebookManager>();
        
        if (!notebook)
        {
            Debug.LogError("[ClickableClue] NotebookManager not found in scene!");
        }
        
        // Setup visual state
        if (textComponent != null)
        {
            textComponent.color = normalColor;
        }
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasBeenClicked)
        {
            ActivateClue();
        }
        else
        {
            // Clue already activated, highlight it in the notebook
            if (notebook != null)
            {
                notebook.AddClueNote(noteText, clueText);
            }
        }
    }
    
    private void ActivateClue()
    {
        hasBeenClicked = true;
        
        // Visual feedback
        if (textComponent != null)
        {
            textComponent.color = clickedColor;
        }
        
        // Add to notebook WITHOUT auto-closing if in interrogation mode
        if (notebook != null)
        {
            // Check if we're in interrogation mode
            bool isInInterrogationMode = InterrogationManager.Instance != null && 
                                        !string.IsNullOrEmpty(InterrogationManager.Instance.CurrentSuspectId);
            
            if (isInInterrogationMode)
            {
                // In interrogation mode - add clue but don't auto-close
                notebook.AddClueNoteWithoutClosing(noteText, clueText);
            }
            else
            {
                // Not in interrogation mode - use normal behavior
                notebook.AddClueNote(noteText, clueText);
            }
        }
        
        Debug.Log($"[ClickableClue] Activated clue: {clueText}");
    }
    
    /// <summary>
    /// Setup the clue with text and note content
    /// </summary>
    public void SetupClue(string text, string note)
    {
        clueText = text;
        noteText = note;
        
        if (textComponent != null)
        {
            textComponent.text = text;
        }
    }
    
    /// <summary>
    /// Check if this clue has been activated
    /// </summary>
    public bool IsActivated => hasBeenClicked;
    
    /// <summary>
    /// Reset the clue state (for testing or new cases)
    /// </summary>
    public void Reset()
    {
        hasBeenClicked = false;
        if (textComponent != null)
        {
            textComponent.color = normalColor;
        }
    }
} 