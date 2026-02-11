using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Animates thinking dots (. .. ... .. .) for chat messages
/// </summary>
public class ThinkingDotsAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    public float dotInterval = 0.5f;
    public int maxDots = 3;
    public string baseText = "";
    
    private TextMeshProUGUI textComponent;
    private Coroutine animationCoroutine;
    private bool isAnimating = false;
    
    private void Awake()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        if (textComponent == null)
            textComponent = GetComponentInChildren<TextMeshProUGUI>();
    }
    
    /// <summary>
    /// Start the thinking dots animation
    /// </summary>
    public void StartThinkingAnimation(string text = "")
    {
        if (isAnimating) return;
        
        baseText = text;
        isAnimating = true;
        
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
        
        animationCoroutine = StartCoroutine(AnimateThinkingDots());
    }
    
    /// <summary>
    /// Stop the thinking dots animation
    /// </summary>
    public void StopThinkingAnimation()
    {
        isAnimating = false;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }
    
    /// <summary>
    /// Animate the thinking dots
    /// </summary>
    private IEnumerator AnimateThinkingDots()
    {
        int currentDots = 0;
        
        // Don't disable ContentSizeFitter - instead use consistent character width
        // The issue was that disabling ContentSizeFitter was causing layout problems
        
        while (isAnimating && textComponent != null && this != null)
        {
            // Always use exactly 3 characters to maintain consistent width
            string dotsText = baseText;
            switch (currentDots)
            {
                case 0: dotsText += "   "; break; // 3 spaces
                case 1: dotsText += ".  "; break; // 1 dot + 2 spaces
                case 2: dotsText += ".. "; break; // 2 dots + 1 space
                case 3: dotsText += "..."; break; // 3 dots
            }
            
            // Update text - add null check
            if (textComponent != null && this != null)
                textComponent.text = dotsText;
            
            // Wait for next frame
            yield return new WaitForSeconds(dotInterval);
            
            // Cycle through dot count (0, 1, 2, 3, repeat)
            currentDots = (currentDots + 1) % 4;
        }
        
        animationCoroutine = null;
    }
    
    /// <summary>
    /// Check if animation is currently running
    /// </summary>
    public bool IsAnimating => isAnimating;
    
    private void OnDestroy()
    {
        StopThinkingAnimation();
    }
} 