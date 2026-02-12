using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles retro computer effects including cursor management and window wipe effects
/// </summary>
public class RetroComputerEffects : SingletonMonoBehaviour<RetroComputerEffects>
{
    [Header("Cursor Settings")]
    [SerializeField] private Image computerCursor; // The custom computer cursor UI element
    [SerializeField] private Sprite defaultCursorSprite;
    [SerializeField] private Sprite hourglassCursorSprite;
    [SerializeField] private Sprite resizeCursorSprite;
    [SerializeField] private Sprite moveCursorSprite;
    [SerializeField] private Vector2 cursorOffset = Vector2.zero;

    private Coroutine hourglassCoroutine;
    private Canvas parentCanvas;
    private Camera mainCamera;
    private bool isComputerCursorActive = false;
    private Sprite originalCursorSprite; // Store original cursor sprite

    protected override void OnSingletonAwake()
    {
        // Get references
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        
        // Initialize computer cursor
        if (computerCursor != null)
        {
            computerCursor.enabled = false;
            computerCursor.raycastTarget = false;
            
            // Store original cursor sprite
            originalCursorSprite = computerCursor.sprite;
            if (originalCursorSprite == null)
            {
                originalCursorSprite = defaultCursorSprite;
            }
        }
    }
    
    private void Update()
    {
        UpdateComputerCursorPosition();
    }
    
    /// <summary>
    /// Shows the custom computer cursor and hides the system cursor
    /// </summary>
    public void ShowComputerCursor()
    {
        if (computerCursor != null)
        {
            computerCursor.enabled = true;
            Cursor.visible = false;
            isComputerCursorActive = true;
        }
    }
    
    /// <summary>
    /// Hides the custom computer cursor and shows the system cursor
    /// </summary>
    public void HideComputerCursor()
    {
        if (computerCursor != null)
        {
            computerCursor.enabled = false;
            Cursor.visible = true;
            isComputerCursorActive = false;
        }
    }
    
    /// <summary>
    /// Updates the position of the custom computer cursor
    /// </summary>
    private void UpdateComputerCursorPosition()
    {
        if (isComputerCursorActive && computerCursor != null)
        {
            Vector2 position = Input.mousePosition;
            
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
            {
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    parentCanvas.GetComponent<RectTransform>(),
                    position,
                    mainCamera,
                    out Vector3 worldPos
                );
                computerCursor.transform.position = worldPos;
            }
            else
            {
                computerCursor.transform.position = position;
            }
            
            // Apply offset
            if (computerCursor.rectTransform != null)
            {
                computerCursor.rectTransform.anchoredPosition += cursorOffset;
            }
        }
    }
    
    /// <summary>
    /// Shows hourglass cursor for specified duration
    /// </summary>
    public void ShowHourglassCursor(float duration = 0.45f)
    {
        if (hourglassCoroutine != null)
        {
            StopCoroutine(hourglassCoroutine);
        }
        hourglassCoroutine = StartCoroutine(HourglassCoroutine(duration));
    }
    
    /// <summary>
    /// Shows resize cursor (stays active until changed)
    /// </summary>
    public void ShowResizeCursor()
    {
        // Stop any active hourglass coroutine
        if (hourglassCoroutine != null)
        {
            StopCoroutine(hourglassCoroutine);
            hourglassCoroutine = null;
        }
        
        if (computerCursor != null && resizeCursorSprite != null)
        {
            computerCursor.sprite = resizeCursorSprite;
        }
    }
    
    /// <summary>
    /// Shows move cursor (stays active until changed)
    /// </summary>
    public void ShowMoveCursor()
    {
        // Stop any active hourglass coroutine
        if (hourglassCoroutine != null)
        {
            StopCoroutine(hourglassCoroutine);
            hourglassCoroutine = null;
        }
        
        if (computerCursor != null && moveCursorSprite != null)
        {
            computerCursor.sprite = moveCursorSprite;
        }
    }
    
    /// <summary>
    /// Restores the default cursor
    /// </summary>
    public void RestoreCursor()
    {
        // Stop any active hourglass coroutine
        if (hourglassCoroutine != null)
        {
            StopCoroutine(hourglassCoroutine);
            hourglassCoroutine = null;
        }
        
        if (computerCursor != null)
        {
            // Always use the original cursor sprite, fallback to default if needed
            Sprite targetSprite = originalCursorSprite != null ? originalCursorSprite : defaultCursorSprite;
            if (targetSprite != null && computerCursor.sprite != targetSprite)
            {
                computerCursor.sprite = targetSprite;
            }
        }
    }

    /// <summary>
    /// Force restore cursor and clear any stuck states
    /// </summary>
    public void ForceRestoreCursor()
    {
        // Stop any active hourglass coroutine
        if (hourglassCoroutine != null)
        {
            StopCoroutine(hourglassCoroutine);
            hourglassCoroutine = null;
        }
        
        if (computerCursor != null)
        {
            if (originalCursorSprite != null)
            {
                computerCursor.sprite = originalCursorSprite;
            }
            else if (defaultCursorSprite != null)
            {
                computerCursor.sprite = defaultCursorSprite;
            }
        }
    }
    
    /// <summary>
    /// Get the current cursor sprite for debugging
    /// </summary>
    public Sprite GetCurrentCursorSprite()
    {
        if (computerCursor != null)
        {
            return computerCursor.sprite;
        }
        return null;
    }
    
    /// <summary>
    /// Check if computer cursor is currently active
    /// </summary>
    public bool IsComputerCursorActive()
    {
        return isComputerCursorActive && computerCursor != null && computerCursor.enabled;
    }
    
    /// <summary>
    /// Get the resize cursor sprite
    /// </summary>
    public Sprite GetResizeCursorSprite()
    {
        return resizeCursorSprite;
    }
    
    /// <summary>
    /// Get the move cursor sprite
    /// </summary>
    public Sprite GetMoveCursorSprite()
    {
        return moveCursorSprite;
    }
    
    /// <summary>
    /// Get the default cursor sprite
    /// </summary>
    public Sprite GetDefaultCursorSprite()
    {
        return originalCursorSprite != null ? originalCursorSprite : defaultCursorSprite;
    }
    
    private IEnumerator HourglassCoroutine(float duration)
    {
        // Set hourglass cursor
        if (computerCursor != null && hourglassCursorSprite != null)
        {
            computerCursor.sprite = hourglassCursorSprite;
        }
        
        yield return new WaitForSeconds(duration);
        
        // Restore original cursor
        if (computerCursor != null && originalCursorSprite != null)
        {
            computerCursor.sprite = originalCursorSprite;
        }
        hourglassCoroutine = null;
    }
    
    /// <summary>
    /// Creates a top-to-bottom wipe effect for revealing window content
    /// </summary>
    public void ApplyWipeEffect(RectTransform windowRect, System.Action onComplete = null)
    {
        StartCoroutine(WipeEffectCoroutine(windowRect, onComplete));
    }
    
    private IEnumerator WipeEffectCoroutine(RectTransform windowRect, System.Action onComplete)
    {
        // Create a mask for the wipe effect
        GameObject maskObj = new GameObject("WipeMask");
        maskObj.transform.SetParent(windowRect, false);
        
        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        
        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.color = Color.black;
        
        Mask mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Initially hide everything
        maskRect.anchorMax = new Vector2(1, 0);
        
        // Animate the mask from top to bottom
        float duration = 0.1f; // Quick wipe
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            maskRect.anchorMax = new Vector2(1, t);
            yield return null;
        }
        
        maskRect.anchorMax = Vector2.one;
        
        // Clean up the mask
        Destroy(maskObj);
        
        onComplete?.Invoke();
    }
} 