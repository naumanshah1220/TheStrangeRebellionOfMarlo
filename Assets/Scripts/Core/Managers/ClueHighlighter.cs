using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles highlighting of clues when they are re-clicked
/// </summary>
public class ClueHighlighter : MonoBehaviour
{
    [Header("Highlight Prefabs")]
    public GameObject underlineHighlightPrefab;
    public GameObject circleHighlightPrefab;
    public GameObject squareHighlightPrefab;

    [Header("Highlight Settings")]
    public float highlightFadeDuration = 10f; // How long highlights last
    public float highlightFadeInDuration = 0.3f; // How fast highlights appear

    // Highlight tracking
    private Dictionary<GameObject, HighlightInfo> activeHighlights = new Dictionary<GameObject, HighlightInfo>();

    public enum HighlightType
    {
        Underline,
        Circle,
        Square
    }

    [System.Serializable]
    public class HighlightInfo
    {
        public GameObject highlightObject;
        public HighlightType type;
        public Coroutine fadeCoroutine;
        public float timeRemaining;
        
        public HighlightInfo(GameObject highlight, HighlightType highlightType)
        {
            highlightObject = highlight;
            type = highlightType;
            fadeCoroutine = null;
            timeRemaining = 0f;
        }
    }

    /// <summary>
    /// Create highlight on a note object
    /// </summary>
    public void CreateHighlightOnNote(GameObject noteObject)
    {
        if (noteObject == null) return;

        // Clear existing highlight if any
        if (activeHighlights.ContainsKey(noteObject))
        {
            ClearHighlight(noteObject);
        }

        // Cycle through highlight types
        HighlightType highlightType = GetNextHighlightType(noteObject);
        GameObject highlightPrefab = GetHighlightPrefab(highlightType);
        
        if (highlightPrefab == null)
        {
            Debug.LogWarning($"[ClueHighlighter] No prefab assigned for highlight type: {highlightType}");
            return;
        }

        // Instantiate highlight
        GameObject highlightObj = Instantiate(highlightPrefab, noteObject.transform);
        
        // Position highlight to cover the note
        RectTransform highlightRect = highlightObj.GetComponent<RectTransform>();
        if (highlightRect != null)
        {
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.offsetMin = Vector2.zero;
            highlightRect.offsetMax = Vector2.zero;
            highlightRect.anchoredPosition = Vector2.zero;
        }

        // Create highlight info and start fade sequence
        HighlightInfo highlightInfo = new HighlightInfo(highlightObj, highlightType);
        activeHighlights[noteObject] = highlightInfo;
        
        Debug.Log($"[ClueHighlighter] Created {highlightType} highlight on note");

        // Start the fade sequence
        highlightInfo.fadeCoroutine = StartCoroutine(HighlightFadeSequence(noteObject, highlightInfo));
    }

    /// <summary>
    /// Refresh an existing highlight (reset its timer)
    /// </summary>
    public void RefreshHighlight(GameObject noteObject)
    {
        if (!activeHighlights.ContainsKey(noteObject)) return;

        HighlightInfo highlightInfo = activeHighlights[noteObject];
        
        // Stop existing fade coroutine
        if (highlightInfo.fadeCoroutine != null)
        {
            StopCoroutine(highlightInfo.fadeCoroutine);
        }

        // Reset alpha to full
        CanvasGroup canvasGroup = highlightInfo.highlightObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        Debug.Log($"[ClueHighlighter] Refreshed highlight on note");

        // Restart fade sequence
        highlightInfo.fadeCoroutine = StartCoroutine(HighlightFadeSequence(noteObject, highlightInfo));
    }

    private HighlightType GetNextHighlightType(GameObject noteObject)
    {
        // If no previous highlight, start with underline
        if (!activeHighlights.ContainsKey(noteObject))
        {
            return HighlightType.Underline;
        }

        // Cycle through highlight types
        HighlightType currentType = activeHighlights[noteObject].type;
        switch (currentType)
        {
            case HighlightType.Underline:
                return HighlightType.Circle;
            case HighlightType.Circle:
                return HighlightType.Square;
            case HighlightType.Square:
                return HighlightType.Underline;
            default:
                return HighlightType.Underline;
        }
    }

    private GameObject GetHighlightPrefab(HighlightType type)
    {
        switch (type)
        {
            case HighlightType.Underline:
                return underlineHighlightPrefab;
            case HighlightType.Circle:
                return circleHighlightPrefab;
            case HighlightType.Square:
                return squareHighlightPrefab;
            default:
                return underlineHighlightPrefab;
        }
    }

    private IEnumerator HighlightFadeSequence(GameObject noteObject, HighlightInfo highlightInfo)
    {
        if (highlightInfo.highlightObject == null) yield break;

        // Get or add CanvasGroup for alpha control
        CanvasGroup canvasGroup = highlightInfo.highlightObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = highlightInfo.highlightObject.AddComponent<CanvasGroup>();
        }

        // Fade in quickly
        canvasGroup.alpha = 0f;
        float fadeInTime = 0f;
        while (fadeInTime < highlightFadeInDuration)
        {
            fadeInTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, fadeInTime / highlightFadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Stay visible for the main duration
        yield return new WaitForSeconds(highlightFadeDuration);

        // Fade out slowly
        float fadeOutDuration = 2f;
        float fadeOutTime = 0f;
        while (fadeOutTime < fadeOutDuration)
        {
            fadeOutTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOutTime / fadeOutDuration);
            yield return null;
        }

        // Clean up
        ClearHighlight(noteObject);
        
        Debug.Log("[ClueHighlighter] Highlight fade sequence completed");
    }

    /// <summary>
    /// Clear highlight from a specific note
    /// </summary>
    public void ClearHighlight(GameObject noteObject)
    {
        if (!activeHighlights.ContainsKey(noteObject)) return;

        HighlightInfo highlightInfo = activeHighlights[noteObject];
        
        // Stop fade coroutine
        if (highlightInfo.fadeCoroutine != null)
        {
            StopCoroutine(highlightInfo.fadeCoroutine);
        }

        // Destroy highlight object
        if (highlightInfo.highlightObject != null)
        {
            Destroy(highlightInfo.highlightObject);
        }

        // Remove from tracking
        activeHighlights.Remove(noteObject);
    }

    /// <summary>
    /// Clear all active highlights
    /// </summary>
    public void ClearAllHighlights()
    {
        foreach (var kvp in activeHighlights)
        {
            HighlightInfo highlightInfo = kvp.Value;
            
            // Stop fade coroutine
            if (highlightInfo.fadeCoroutine != null)
            {
                StopCoroutine(highlightInfo.fadeCoroutine);
            }

            // Destroy highlight object
            if (highlightInfo.highlightObject != null)
            {
                Destroy(highlightInfo.highlightObject);
            }
        }

        activeHighlights.Clear();
        Debug.Log("[ClueHighlighter] Cleared all highlights");
    }

    /// <summary>
    /// Check if a note has an active highlight
    /// </summary>
    public bool HasHighlight(GameObject noteObject)
    {
        return activeHighlights.ContainsKey(noteObject);
    }

    /// <summary>
    /// Get the current highlight type for a note
    /// </summary>
    public HighlightType? GetHighlightType(GameObject noteObject)
    {
        if (activeHighlights.ContainsKey(noteObject))
        {
            return activeHighlights[noteObject].type;
        }
        return null;
    }

    private void OnDestroy()
    {
        ClearAllHighlights();
    }
} 