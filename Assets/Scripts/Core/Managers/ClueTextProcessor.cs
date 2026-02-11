using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Handles detection and processing of detective tags in clue text
/// </summary>
public class ClueTextProcessor : MonoBehaviour
{
    [Header("Detective Tag Prefabs")]
    public GameObject personTagPrefab;
    public GameObject locationTagPrefab;
    public GameObject dateTagPrefab;
    public GameObject timeTagPrefab;
    public GameObject itemTagPrefab;
    public GameObject valueTagPrefab;
    public GameObject objectTagPrefab;
    
    [Header("Suspect Tag Prefabs")]
    public GameObject suspectFnameTagPrefab;
    public GameObject suspectLnameTagPrefab;
    public GameObject suspectIdTagPrefab;
    public GameObject suspectPortraitTagPrefab;
    public GameObject suspectCompleteTagPrefab; // For complete suspect tags

    [Header("Tag Positioning")]
    public Vector2 globalPositionOffset = Vector2.zero;
    public Vector2 tagPositionOffset = Vector2.zero;
    public char frameSpacingChar = ' ';
    public int frameSpacingCount = 1;

    [Header("Tag Animation")]
    public float tagFadeInDuration = 0.1f;
    public float tagFadeInDelay = 0.05f;

    // Tag detection and processing
    private List<TagInfo> detectedTags = new List<TagInfo>();

    [System.Serializable]
    public class TagInfo
    {
        public string tagType;      // person, location, date, time, item, value, object
        public string content;      // The actual word (James, gym, 3:00 pm)
        public int startIndex;      // Position in processed text where spacing starts
        public int length;          // Number of spacing characters used
        public GameObject prefab;   // Which prefab to instantiate
        public GameObject instance; // The instantiated prefab
        
        public TagInfo(string type, string text, int start, int len, GameObject prefabRef)
        {
            tagType = type;
            content = text;
            startIndex = start;
            length = len;
            prefab = prefabRef;
            instance = null;
        }
    }

    /// <summary>
    /// Process tags in text and replace them with spacing characters
    /// </summary>
    public string ProcessTagsInText(string originalText)
    {
        detectedTags.Clear();
        
        // Regex pattern to match tags like <person>James</person>
        string pattern = @"<(person|location|date|time|item|value|object|suspect_fname|suspect_lname|suspect_id|suspect_portrait|suspect)>(.*?)</\1>";
        
        string result = originalText;
        int offset = 0; // Track position changes due to replacements
        
        foreach (Match match in Regex.Matches(originalText, pattern))
        {
            string tagType = match.Groups[1].Value;
            string content = match.Groups[2].Value;
            int originalStart = match.Index;
            int adjustedStart = originalStart - offset;
            
            // Get the appropriate prefab
            GameObject prefab = GetPrefabForTagType(tagType);
            if (prefab == null)
            {
                Debug.LogWarning($"[ClueTextProcessor] No prefab assigned for tag type: {tagType}");
                continue;
            }
            
            // Create spacing for frame around the tagged word
            string spacing = new string(frameSpacingChar, frameSpacingCount);
            
            // Check if this tag is at the very beginning of the text (after any whitespace)
            bool isFirstTag = IsTagAtBeginning(originalText, originalStart);
            
            // For first tag, don't add leading space; for others, add both leading and trailing space
            string spacedContent;
            int contentStartIndex;
            
            if (isFirstTag)
            {
                spacedContent = content + spacing; // Only trailing space
                contentStartIndex = adjustedStart; // Content starts immediately, no leading space
            }
            else
            {
                spacedContent = spacing + content + spacing; // Both leading and trailing space
                contentStartIndex = adjustedStart + frameSpacingCount; // Content starts after leading space
            }
            
            // Replace the tag with spaced content
            string fullTag = match.Value;
            result = result.Substring(0, adjustedStart) + spacedContent + result.Substring(adjustedStart + fullTag.Length);
            
            // Store tag info
            TagInfo tagInfo = new TagInfo(tagType, content, contentStartIndex, content.Length, prefab);
            detectedTags.Add(tagInfo);
            
            // Update offset for next replacements
            offset += fullTag.Length - spacedContent.Length;
        }
        
        return result;
    }

    /// <summary>
    /// Check if a character position is part of a tag
    /// </summary>
    public TagInfo GetTagAtPosition(int position)
    {
        foreach (var tag in detectedTags)
        {
            if (tag.startIndex <= position && position < tag.startIndex + tag.length)
            {
                return tag;
            }
        }
        return null;
    }

    /// <summary>
    /// Check if character is the last character of any tag
    /// </summary>
    public TagInfo GetTagEndingAtPosition(int position)
    {
        foreach (var tag in detectedTags)
        {
            if (position == tag.startIndex + tag.length - 1)
            {
                return tag;
            }
        }
        return null;
    }

    /// <summary>
    /// Replace a detected tag with its visual prefab
    /// </summary>
    public IEnumerator ReplaceTagWithPrefab(TagInfo tag, Color originalColor, TextMeshProUGUI targetTMP, 
        System.Action<int, int, Color> hideCharactersFunc, string clueId = null)
    {
        // Wait for the character fade to complete, then add tag-specific delay
        yield return new WaitForSeconds(tagFadeInDelay);
        
        // Hide the original text characters for this tag
        hideCharactersFunc?.Invoke(tag.startIndex, tag.length, originalColor);
        
        // Instantiate the tag prefab
        if (tag.prefab != null)
        {
            tag.instance = Instantiate(tag.prefab, targetTMP.transform.parent);
            
            // Find the TMPro component in the prefab and set the text
            TextMeshProUGUI tagText = tag.instance.GetComponentInChildren<TextMeshProUGUI>();
            if (tagText != null)
            {
                tagText.text = tag.content;
                tagText.color = originalColor; // Match the original text color
            }
            else
            {
                Debug.LogWarning($"[ClueTextProcessor] Tag prefab for {tag.tagType} doesn't have a TextMeshProUGUI component in children!");
            }
            
            // Position the tag at the first character position
            RectTransform tagRect = tag.instance.GetComponent<RectTransform>();
            if (tagRect != null)
            {
                // Get more precise positioning using character bounds
                Vector2 precisePosition = GetPreciseTagPosition(tag, targetTMP);
                precisePosition += globalPositionOffset;
                precisePosition += tagPositionOffset; // Apply manual tag offset
                
                tagRect.anchoredPosition = precisePosition;
                
                // Get the CanvasGroup (should be pre-added to prefab)
                CanvasGroup canvasGroup = tag.instance.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    Debug.LogWarning($"[ClueTextProcessor] Tag prefab for {tag.tagType} should have a CanvasGroup component!");
                    canvasGroup = tag.instance.AddComponent<CanvasGroup>();
                }
                
                // Add DraggableTag component for interrogation system
                DraggableTag draggableComponent = tag.instance.GetComponent<DraggableTag>();
                if (draggableComponent == null)
                {
                    draggableComponent = tag.instance.AddComponent<DraggableTag>();
                }
                
                // Initialize the draggable tag with content and type
                draggableComponent.Initialize(tag.content, tag.tagType, clueId);
                
                canvasGroup.alpha = 0f;
                
                // Fade in the tag
                StartCoroutine(FadeInTag(canvasGroup, tagFadeInDuration));
            }
        }
    }

    private bool IsTagAtBeginning(string text, int tagStartIndex)
    {
        // Check if there are only whitespace characters before this tag
        for (int i = 0; i < tagStartIndex; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
            {
                return false; // Found non-whitespace character before tag
            }
        }
        return true; // Only whitespace (or nothing) before this tag
    }

    private GameObject GetPrefabForTagType(string tagType)
    {
        switch (tagType.ToLower())
        {
            case "person": return personTagPrefab;
            case "location": return locationTagPrefab;
            case "date": return dateTagPrefab;
            case "time": return timeTagPrefab;
            case "item": return itemTagPrefab;
            case "value": return valueTagPrefab;
            case "object": return objectTagPrefab;
            case "suspect_fname": return suspectFnameTagPrefab;
            case "suspect_lname": return suspectLnameTagPrefab;
            case "suspect_id": return suspectIdTagPrefab;
            case "suspect_portrait": return suspectPortraitTagPrefab;
            case "suspect": return suspectCompleteTagPrefab;
            default: return null;
        }
    }

    private Vector2 GetPreciseTagPosition(TagInfo tag, TextMeshProUGUI targetTMP)
    {
        targetTMP.ForceMeshUpdate();
        var textInfo = targetTMP.textInfo;
        
        // Calculate word bounds for the tagged content
        if (tag.startIndex >= 0 && tag.startIndex + tag.length <= textInfo.characterCount)
        {
            Vector3 bottomLeft = Vector3.zero;
            Vector3 topRight = Vector3.zero;
            float maxAscender = -Mathf.Infinity;
            float minDescender = Mathf.Infinity;
            
            bool hasValidChar = false;
            
            // Iterate through each character of the tagged word
            for (int i = 0; i < tag.length; i++)
            {
                int characterIndex = tag.startIndex + i;
                if (characterIndex >= textInfo.characterCount) break;
                
                var charInfo = textInfo.characterInfo[characterIndex];
                if (!charInfo.isVisible) continue;
                
                // Track bounds
                maxAscender = Mathf.Max(maxAscender, charInfo.ascender);
                minDescender = Mathf.Min(minDescender, charInfo.descender);
                
                if (!hasValidChar)
                {
                    // First visible character - set initial bounds
                    bottomLeft = new Vector3(charInfo.bottomLeft.x, minDescender, 0);
                    hasValidChar = true;
                }
                
                // Update right bound to last character's right edge
                topRight = new Vector3(charInfo.topRight.x, maxAscender, 0);
            }
            
            if (hasValidChar)
            {
                // Use center of the word bounds for positioning
                Vector3 wordCenter = new Vector3(
                    (bottomLeft.x + topRight.x) * 0.5f,
                    (minDescender + maxAscender) * 0.5f,
                    0
                );
                
                // Convert to UI space
                Vector3 worldPos = targetTMP.transform.TransformPoint(wordCenter);
                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(targetTMP.canvas.worldCamera, worldPos);
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    targetTMP.rectTransform, screenPoint, targetTMP.canvas.worldCamera, out localPoint);

                return localPoint;
            }
        }
        
        // Fallback to basic character position
        return GetCharacterPosition(targetTMP, tag.startIndex);
    }

    private Vector2 GetCharacterPosition(TextMeshProUGUI tmp, int charIndex)
    {
        tmp.ForceMeshUpdate();
        var textInfo = tmp.textInfo;

        if (charIndex >= 0 && charIndex < textInfo.characterCount)
        {
            var charInfo = textInfo.characterInfo[charIndex];
            Vector3 worldPos = charInfo.bottomLeft;
            
            Vector3 world = tmp.transform.TransformPoint(worldPos);
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(tmp.canvas.worldCamera, world);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                tmp.rectTransform, screenPoint, tmp.canvas.worldCamera, out localPoint);

            return localPoint;
        }
        
        return Vector2.zero;
    }

    private IEnumerator FadeInTag(CanvasGroup canvasGroup, float duration)
    {
        if (duration <= 0)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }
        
        float startTime = Time.time;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            canvasGroup.alpha = t;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Clear all detected tags
    /// </summary>
    public void ClearTags()
    {
        detectedTags.Clear();
    }

    /// <summary>
    /// Get all detected tags
    /// </summary>
    public List<TagInfo> GetDetectedTags()
    {
        return new List<TagInfo>(detectedTags);
    }
} 