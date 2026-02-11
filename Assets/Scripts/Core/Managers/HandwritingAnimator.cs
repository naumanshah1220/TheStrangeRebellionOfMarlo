using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles handwriting animation for notebook text using sprite sheet animations
/// </summary>
public class HandwritingAnimator : MonoBehaviour
{
    [Header("Sprite Sheet Settings")]
    public Texture2D spriteSheet;
    public int gridCols = 32;
    public int gridRows = 32;
    public int cellSize = 64;
    private const int SPRITE_SHEET_FRAMES_PER_LETTER = 10;

    [Header("Animation Settings")]
    [Range(1, 10)]
    public int framesPerLetter = 10;
    public float frameDuration = 0.05f;
    public float caretScale = 1.0f;
    public Vector2 globalPositionOffset = Vector2.zero;
    public Color caretColor = Color.white;

    [Header("Character Animation")]
    public float fadeInDuration = 0.05f;
    public float fadeInDelay = 0.0f;
    [Range(0f, 1f)]
    public float fadeStartAlpha = 0f;

    [Header("Character Offsets")]
    public List<CharacterOffset> characterOffsets = new List<CharacterOffset>();

    // Character mapping for sprite sheet
    private string charOrder = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890.?!\"#$%&',:-+=<>()*_[]/--";
    
    // Animation state
    private Color32[][] originalVertexColors;
    private Color32[][] currentVertexColors;
    private bool needsVertexUpdate = false;
    private TextMeshProUGUI currentTargetTMP;
    private Image caretImage;

    [System.Serializable]
    public class CharacterOffset
    {
        public char character;
        public Vector2 offset;
        
        public CharacterOffset(char c)
        {
            character = c;
            offset = Vector2.zero;
        }
    }

    private void Awake()
    {
        if (spriteSheet != null)
            cellSize = spriteSheet.width / gridCols;
    }

    private void LateUpdate()
    {
        if (needsVertexUpdate && currentTargetTMP != null && currentVertexColors != null)
        {
            // Apply all pending vertex color changes at once
            for (int materialIndex = 0; materialIndex < currentVertexColors.Length; materialIndex++)
            {
                if (currentVertexColors[materialIndex] != null && 
                    materialIndex < currentTargetTMP.textInfo.meshInfo.Length)
                {
                    Color32[] meshColors = currentTargetTMP.textInfo.meshInfo[materialIndex].colors32;
                    if (meshColors != null)
                    {
                        for (int i = 0; i < currentVertexColors[materialIndex].Length && i < meshColors.Length; i++)
                        {
                            meshColors[i] = currentVertexColors[materialIndex][i];
                        }
                    }
                }
            }
            
            currentTargetTMP.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            needsVertexUpdate = false;
        }
    }

    /// <summary>
    /// Animate text with handwriting effect
    /// </summary>
    public IEnumerator AnimateText(string processedText, TextMeshProUGUI targetTMP, Color originalColor, 
        System.Func<int, bool> isTaggedCharacterFunc = null, 
        System.Action<int> onTagEndFunc = null)
    {
        currentTargetTMP = targetTMP;
        
        // Set the processed text
        currentTargetTMP.text = processedText;
        currentTargetTMP.ForceMeshUpdate();
        
        // Cache original vertex colors
        CacheOriginalVertexColors();
        
        // Create and show caret
        CreateCaretForText();
        if (caretImage != null)
            caretImage.enabled = true;

        for (int i = 0; i < processedText.Length; i++)
        {
            char c = processedText[i];
            
            // Check if this character is part of a tag
            bool isTaggedCharacter = isTaggedCharacterFunc?.Invoke(i) ?? false;
            
            int charIndex = charOrder.IndexOf(c);
            if (charIndex < 0)
            {
                // Character not in sprite sheet, just make it visible immediately
                SetCharacterAlphaWithColor(i, 255, originalColor);
                
                // If this is the last character of a tag, trigger callback
                if (isTaggedCharacter)
                {
                    onTagEndFunc?.Invoke(i);
                }
                continue;
            }

            if (caretImage != null)
            {
                // Animate caret
                yield return StartCoroutine(AnimateCaret(c, i));
            }

            // Wait for delay, then fade in the character
            if (fadeInDelay > 0)
                yield return new WaitForSeconds(fadeInDelay);
                
            StartCoroutine(FadeInCharacter(i, fadeInDuration, originalColor));
            
            // If this is the last character of a tag, trigger callback
            if (isTaggedCharacter)
            {
                onTagEndFunc?.Invoke(i);
            }
        }

        if (caretImage != null)
            caretImage.enabled = false;
        
        // Restore the text color to fully visible after animation completes
        currentTargetTMP.color = originalColor;
    }

    private IEnumerator AnimateCaret(char c, int charIndex)
    {
        // Set fixed caret size
        float caretSize = cellSize * caretScale;
        caretImage.rectTransform.sizeDelta = new Vector2(caretSize, caretSize);

        // Position caret at the exact character position
        Vector2 caretLocalPos = GetCharacterPosition(currentTargetTMP, charIndex);
        caretLocalPos += globalPositionOffset;
        
        // Apply per-character offset
        Vector2 characterOffset = GetCharacterOffset(c);
        caretLocalPos += characterOffset;
        
        caretImage.rectTransform.anchoredPosition = caretLocalPos;

        // Animate through frames for this character
        int spriteCharIndex = charOrder.IndexOf(c);
        for (int frame = 0; frame < framesPerLetter; frame++)
        {
            int spriteSheetFrame = Mathf.RoundToInt((float)frame * (SPRITE_SHEET_FRAMES_PER_LETTER - 1) / Mathf.Max(1, framesPerLetter - 1));
            SetCaretSprite(spriteCharIndex, spriteSheetFrame);
            caretImage.color = caretColor;
            yield return new WaitForSeconds(frameDuration);
        }
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

    private Vector2 GetCharacterOffset(char c)
    {
        foreach (var charOffset in characterOffsets)
        {
            if (charOffset.character == c)
            {
                return charOffset.offset;
            }
        }
        return Vector2.zero;
    }

    private void SetCaretSprite(int charIndex, int spriteSheetFrame)
    {
        if (caretImage == null || spriteSheet == null) return;

        int cellIndex = charIndex * SPRITE_SHEET_FRAMES_PER_LETTER + spriteSheetFrame;
        int col = cellIndex % gridCols;
        int row = cellIndex / gridCols;

        caretImage.sprite = Sprite.Create(
            spriteSheet,
            new Rect(col * cellSize, (gridRows - row - 1) * cellSize, cellSize, cellSize),
            new Vector2(0.5f, 0.5f)
        );
    }

    private void CacheOriginalVertexColors()
    {
        originalVertexColors = new Color32[currentTargetTMP.textInfo.materialCount][];
        currentVertexColors = new Color32[currentTargetTMP.textInfo.materialCount][];
        
        for (int i = 0; i < currentTargetTMP.textInfo.materialCount; i++)
        {
            var textInfo = currentTargetTMP.textInfo;
            var meshInfo = textInfo.meshInfo[i];
            
            originalVertexColors[i] = new Color32[meshInfo.vertexCount];
            currentVertexColors[i] = new Color32[meshInfo.vertexCount];
            
            for (int j = 0; j < meshInfo.vertexCount; j++)
            {
                originalVertexColors[i][j] = meshInfo.colors32[j];
                currentVertexColors[i][j] = meshInfo.colors32[j];
            }
        }
    }

    private IEnumerator FadeInCharacter(int charIndex, float duration, Color originalColor)
    {
        if (duration <= 0)
        {
            SetCharacterAlphaWithColor(charIndex, 255, originalColor);
            yield break;
        }
        
        float startTime = Time.time;
        byte startAlpha = (byte)(fadeStartAlpha * 255);
        byte endAlpha = 255;
        
        while (Time.time < startTime + duration)
        {
            float t = (Time.time - startTime) / duration;
            byte currentAlpha = (byte)Mathf.Lerp(startAlpha, endAlpha, t);
            SetCharacterAlphaWithColor(charIndex, currentAlpha, originalColor);
            yield return null;
        }
        
        SetCharacterAlphaWithColor(charIndex, endAlpha, originalColor);
    }

    private void SetCharacterAlphaWithColor(int charIndex, byte alpha, Color originalColor)
    {
        var textInfo = currentTargetTMP.textInfo;
        
        if (charIndex >= 0 && charIndex < textInfo.characterCount)
        {
            var charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) return;
            
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            
            // Safety checks
            if (currentVertexColors == null || 
                materialIndex < 0 || materialIndex >= currentVertexColors.Length ||
                currentVertexColors[materialIndex] == null ||
                vertexIndex + 3 >= currentVertexColors[materialIndex].Length)
            {
                return;
            }
            
            Color32 newColor = new Color32(
                (byte)(originalColor.r * 255),
                (byte)(originalColor.g * 255), 
                (byte)(originalColor.b * 255),
                alpha
            );
            
            currentVertexColors[materialIndex][vertexIndex + 0] = newColor;
            currentVertexColors[materialIndex][vertexIndex + 1] = newColor;
            currentVertexColors[materialIndex][vertexIndex + 2] = newColor;
            currentVertexColors[materialIndex][vertexIndex + 3] = newColor;
            
            needsVertexUpdate = true;
        }
    }

    private void CreateCaretForText()
    {
        if (currentTargetTMP == null) return;

        GameObject caretObj = new GameObject("Caret");
        caretObj.transform.SetParent(currentTargetTMP.transform, false);

        caretImage = caretObj.AddComponent<Image>();
        caretImage.color = caretColor;
        caretImage.enabled = false;

        RectTransform caretRect = caretObj.GetComponent<RectTransform>();
        caretRect.anchorMin = new Vector2(0.5f, 0.5f);
        caretRect.anchorMax = new Vector2(0.5f, 0.5f);
        caretRect.pivot = new Vector2(0.5f, 0.5f);
        caretRect.sizeDelta = new Vector2(cellSize, cellSize) * caretScale;
    }

    /// <summary>
    /// Hide specific characters in text (used for tag replacement)
    /// </summary>
    public void HideTextCharacters(int startIndex, int length, Color originalColor)
    {
        for (int i = startIndex; i < startIndex + length; i++)
        {
            SetCharacterAlphaWithColor(i, 0, originalColor);
        }
    }

    #region Context Menu Methods
    [ContextMenu("Initialize Character Offsets")]
    public void InitializeCharacterOffsets()
    {
        characterOffsets.Clear();
        
        for (int i = 0; i < charOrder.Length; i++)
        {
            characterOffsets.Add(new CharacterOffset(charOrder[i]));
        }
        
        Debug.Log($"[HandwritingAnimator] Initialized {characterOffsets.Count} character offsets");
    }

    [ContextMenu("Show Character Order")]
    public void ShowCharacterOrder()
    {
        Debug.Log($"[HandwritingAnimator] Character Order ({charOrder.Length} characters): {charOrder}");
    }

    [ContextMenu("Reset All Character Offsets")]
    public void ResetCharacterOffsets()
    {
        foreach (var charOffset in characterOffsets)
        {
            charOffset.offset = Vector2.zero;
        }
        Debug.Log("[HandwritingAnimator] Reset all character offsets to zero");
    }
    
    [ContextMenu("Show Frame Sampling")]
    public void ShowFrameSampling()
    {
        Debug.Log($"[HandwritingAnimator] Frame Sampling for {framesPerLetter} frames configured");
    }
    #endregion

    private void OnDisable()
    {
        currentTargetTMP = null;
        caretImage = null;
    }
} 