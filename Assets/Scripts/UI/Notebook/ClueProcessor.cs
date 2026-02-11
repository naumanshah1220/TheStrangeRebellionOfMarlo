using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections.Generic;

/// <summary>
/// Processes chat messages to make extractable clues clickable
/// </summary>
public class ClueProcessor : MonoBehaviour
{
    [Header("Clue Prefab")]
    public GameObject clickableClueTagPrefab; // Prefab for clickable clue tags
    
    [Header("Visual Settings")]
    public Color clueHighlightColor = Color.yellow;
    public Font clueFont;
    public int clueFontSize = 16;
    
    /// <summary>
    /// Process a message text and make extractable clues clickable
    /// </summary>
    public void ProcessCluesInMessage(TextMeshProUGUI messageText, string[] extractableClues)
    {
        if (extractableClues == null || extractableClues.Length == 0)
            return;
            
        string originalText = messageText.text;
        
        // Process each extractable clue
        foreach (string clue in extractableClues)
        {
            if (string.IsNullOrEmpty(clue)) continue;
            
            // Find the clue word in the text and make it clickable
            ProcessClueInText(messageText, clue, originalText);
        }
    }
    
    /// <summary>
    /// Process a specific clue in the message text
    /// </summary>
    private void ProcessClueInText(TextMeshProUGUI messageText, string clue, string originalText)
    {
        // For now, we'll use a simple approach: create a clickable tag overlay
        // In a more advanced implementation, we could use TextMeshPro's rich text features
        
        Transform messageContainer = messageText.transform.parent;
        if (messageContainer == null) return;
        
        // Check if the clue word exists in the text (case-insensitive)
        int clueIndex = originalText.IndexOf(clue, System.StringComparison.OrdinalIgnoreCase);
        if (clueIndex == -1) return;
        
        // Create a clickable tag for this clue
        CreateClickableClueTag(messageContainer, clue, GetClueNoteText(clue));
    }
    
    /// <summary>
    /// Create a clickable tag for a clue
    /// </summary>
    private void CreateClickableClueTag(Transform parent, string clueText, string noteText)
    {
        if (clickableClueTagPrefab == null)
        {
            Debug.LogError("[ClueProcessor] No clickable clue tag prefab assigned!");
            return;
        }
        
        // Instantiate the clue tag
        GameObject clueTag = Instantiate(clickableClueTagPrefab, parent);
        
        // Setup the clickable clue component
        ClickableClue clickableClue = clueTag.GetComponent<ClickableClue>();
        if (clickableClue != null)
        {
            clickableClue.SetupClue(clueText, noteText);
        }
        
        // Position the tag (this is a simplified approach - in practice you'd want more precise positioning)
        RectTransform tagRect = clueTag.GetComponent<RectTransform>();
        if (tagRect != null)
        {
            tagRect.anchorMin = new Vector2(0, 0);
            tagRect.anchorMax = new Vector2(0, 0);
            tagRect.anchoredPosition = new Vector2(10, -30); // Simple offset positioning
        }
        
        Debug.Log($"[ClueProcessor] Created clickable clue tag for: {clueText}");
    }
    
    /// <summary>
    /// Get the note text for a clue (can be customized based on clue type)
    /// </summary>
    private string GetClueNoteText(string clue)
    {
        // For now, return a simple note format
        // In practice, you might want to have more sophisticated note generation
        return $"Found clue: {clue}";
    }
    
    /// <summary>
    /// Alternative approach: Use TextMeshPro rich text to highlight clues
    /// </summary>
    public void HighlightCluesInText(TextMeshProUGUI messageText, string[] extractableClues)
    {
        if (extractableClues == null || extractableClues.Length == 0)
            return;
            
        string text = messageText.text;
        
        // Process each clue and add rich text formatting
        foreach (string clue in extractableClues)
        {
            if (string.IsNullOrEmpty(clue)) continue;
            
            // Use case-insensitive replacement with rich text formatting
            string colorHex = ColorUtility.ToHtmlStringRGB(clueHighlightColor);
            string highlightedClue = $"<color=#{colorHex}><u>{clue}</u></color>";
            
            // Replace the clue with highlighted version (case-insensitive)
            text = Regex.Replace(text, Regex.Escape(clue), highlightedClue, RegexOptions.IgnoreCase);
        }
        
        messageText.text = text;
    }
    
    /// <summary>
    /// Process clues using link system (advanced TextMeshPro feature)
    /// </summary>
    public void ProcessCluesAsLinks(TextMeshProUGUI messageText, string[] extractableClues)
    {
        if (extractableClues == null || extractableClues.Length == 0)
            return;
            
        string text = messageText.text;
        
        // Process each clue and add link formatting
        for (int i = 0; i < extractableClues.Length; i++)
        {
            string clue = extractableClues[i];
            if (string.IsNullOrEmpty(clue)) continue;
            
            // Create a link with unique ID
            string linkId = $"clue_{i}";
            string linkedClue = $"<link=\"{linkId}\">{clue}</link>";
            
            // Replace the clue with linked version (case-insensitive)
            text = Regex.Replace(text, Regex.Escape(clue), linkedClue, RegexOptions.IgnoreCase);
        }
        
        messageText.text = text;
        
        // You would need to handle link clicks in the ChatMessage component
        // by implementing TMP_LinkInfo processing
    }
} 