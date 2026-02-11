using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Speech bubble component that auto-scales with message content
/// Handles both player and suspect messages with proper alignment
/// </summary>
public class MessageBubble : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private RectTransform bubbleRect;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private float maxWidth = 220f;
    
    /// <summary>
    /// Setup the message bubble with content and alignment
    /// </summary>
    public void SetupMessage(string message, bool isPlayerMessage, string timestamp = "")
    {
        messageText.text = message;
        if (timestampText != null)
        {
            timestampText.text = timestamp;
        }

        // Force layout rebuild to ensure proper sizing
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
        
        // Let the LayoutElement handle the sizing, but ensure we have minimum dimensions
        if (bubbleRect != null)
        {
            // Get the LayoutElement component to set preferred width and height
            LayoutElement layoutElement = bubbleRect.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                float clampedWidth = Mathf.Min(messageText.preferredWidth + 20f, maxWidth); // +20 for padding
                float preferredHeight = Mathf.Max(messageText.preferredHeight + 10f, 40f); // +10 for padding, minimum 40
                
                layoutElement.preferredWidth = clampedWidth;
                layoutElement.preferredHeight = preferredHeight;
                
                Debug.Log($"[MessageBubble] Set preferred size to {clampedWidth}x{preferredHeight} for message: '{message.Substring(0, Mathf.Min(20, message.Length))}...'");
            }
            else
            {
                // Fallback to manual sizing if no LayoutElement
                float clampedWidth = Mathf.Min(messageText.preferredWidth, maxWidth);
                bubbleRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, clampedWidth);
            }
        }
        
        // Force another layout rebuild after setting preferred width
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleRect);
    }
    
       
    /// <summary>
    /// Set message color (for suspect messages with different response types)
    /// </summary>
    public void SetMessageColor(Color color)
    {
        if (messageText != null)
        {
            messageText.color = color;
        }
    }
} 