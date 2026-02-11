using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Tool : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    public string toolId;       // Unique identifier ("Computer", "Phone", etc.)
    public string displayName;  // For UI/Debug
    [Tooltip("Optional: override hover rect for precise hover detection")] public RectTransform hoverRectOverride;

    [Header("States")]
    public bool isHovering = false; //if hovered over by mouse

    [Header("Card Acceptance")]
    public bool acceptsCards = false; // Does this tool accept dropped cards?
    public CardMode[] acceptedCardTypes = new CardMode[] { CardMode.Evidence }; // What types of cards can be dropped

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Tool click handling can be added here if needed
    }

    /// <summary>
    /// Check if this tool can accept a specific card type
    /// </summary>
    public bool CanAcceptCard(Card card)
    {
        
        if (!acceptsCards) 
        {
            Debug.Log($"[Tool] Tool '{displayName}' does not accept cards");
            return false;
        }
        
        // Check if card type is accepted
        bool cardTypeAccepted = false;
        foreach (CardMode acceptedType in acceptedCardTypes)
        {
            if (card.mode == acceptedType) 
            {
                cardTypeAccepted = true;
                break;
            }
        }
        
        if (!cardTypeAccepted)
        {
            Debug.Log($"[Tool] Tool '{displayName}' does not accept card mode {card.mode}");
            return false;
        }
        
        // For computer tool, delegate to ComputerSystem for more specific validation
        if (toolId == "Computer")
        {
            ComputerSystem computerSystem = GetComponent<ComputerSystem>();
            if (computerSystem != null)
            {
                Debug.Log($"[Tool] Delegating to ComputerSystem for detailed validation");
                return computerSystem.CanAcceptDisc(card);
            }
            else
            {
                Debug.LogError($"[Tool] Computer tool has no ComputerSystem component!");
                return false;
            }
        }
        
        // For fingerprint duster tool, delegate to FingerPrintDusterSystem for more specific validation
        if (toolId == "FingerPrintDuster")
        {
            var dusterSystem = GetComponent("FingerPrintDusterSystem");
            if (dusterSystem != null)
            {
                var canAcceptMethod = dusterSystem.GetType().GetMethod("CanAcceptCard");
                if (canAcceptMethod != null)
                {
                    return (bool)canAcceptMethod.Invoke(dusterSystem, new object[] { card });
                }
            }           
            return false;
           
        }
        
        // For spectrograph tool, delegate to SpectrographSystem for more specific validation
        if (toolId == "Spectrograph")
        {
            var specSystem = GetComponent("SpectrographSystem");
            if (specSystem != null)
            {
                var canAcceptMethod = specSystem.GetType().GetMethod("CanAcceptCard");
                if (canAcceptMethod != null)
                {
                    return (bool)canAcceptMethod.Invoke(specSystem, new object[] { card });
                }
            }
            return false;
        }
        return true;
    }

    /// <summary>
    /// Handle card dropped on this tool
    /// </summary>
    public void OnCardDropped(Card card)
    {
        
        // Delegate to specific system based on tool type
        if (toolId == "Computer")
        {
            ComputerSystem computerSystem = GetComponent<ComputerSystem>();
            if (computerSystem != null)
            {
                computerSystem.HandleDiscDropped(card);
                return;
            }
        }
        
        if (toolId == "FingerPrintDuster")
        {
            var dusterSystem = GetComponent("FingerPrintDusterSystem");
            if (dusterSystem != null)
            {
                var handleDropMethod = dusterSystem.GetType().GetMethod("HandleCardDropped");
                if (handleDropMethod != null)
                {
                    handleDropMethod.Invoke(dusterSystem, new object[] { card });
                    return;
                }
            }
        }
        
        if (toolId == "Spectrograph")
        {
            var specSystem = GetComponent("SpectrographSystem");
            if (specSystem != null)
            {
                var handleDropMethod = specSystem.GetType().GetMethod("HandleCardDropped");
                if (handleDropMethod != null)
                {
                    handleDropMethod.Invoke(specSystem, new object[] { card });
                    return;
                }
            }
        }
        
        // Add more tool types here as needed
        // if (toolId == "Phone") { ... }
    }

    /// <summary>
    /// Provide visual feedback when card is hovering over tool
    /// </summary>
    public void OnCardHoverStart(Card card)
    {
        // Mark hover state for drag-driven hover routing
        isHovering = true;
        // Delegate to specific system based on tool type
        if (toolId == "Computer")
        {
            ComputerSystem computerSystem = GetComponent<ComputerSystem>();
            if (computerSystem != null)
            {
                computerSystem.HandleCardHoverStart(card);
            }
        }
        
        if (toolId == "FingerPrintDuster")
        {
            var dusterSystem = GetComponent("FingerPrintDusterSystem");
            if (dusterSystem != null)
            {
                var hoverStartMethod = dusterSystem.GetType().GetMethod("HandleCardHoverStart");
                if (hoverStartMethod != null)
                {
                    hoverStartMethod.Invoke(dusterSystem, new object[] { card });
                }
            }
        }
        
        if (toolId == "Spectrograph")
        {
            var specSystem = GetComponent("SpectrographSystem");
            if (specSystem != null)
            {
                var hoverStartMethod = specSystem.GetType().GetMethod("HandleCardHoverStart");
                if (hoverStartMethod != null)
                {
                    hoverStartMethod.Invoke(specSystem, new object[] { card });
                }
            }
        }
    }

    /// <summary>
    /// Clear visual feedback when card stops hovering
    /// </summary>
    public void OnCardHoverEnd(Card card)
    {
        // Clear hover state for drag-driven hover routing
        isHovering = false;
        if (card == null)
        {
            Debug.Log($"[Tool] Card stopped hovering over tool '{displayName}' (card was null)");
            return;
        }
                
        // Delegate to specific system based on tool type
        if (toolId == "Computer")
        {
            ComputerSystem computerSystem = GetComponent<ComputerSystem>();
            if (computerSystem != null)
            {
                computerSystem.HandleCardHoverEnd(card);
            }
        }
        
        if (toolId == "FingerPrintDuster")
        {
            var dusterSystem = GetComponent("FingerPrintDusterSystem");
            if (dusterSystem != null)
            {
                var hoverEndMethod = dusterSystem.GetType().GetMethod("HandleCardHoverEnd");
                if (hoverEndMethod != null)
                {
                    hoverEndMethod.Invoke(dusterSystem, new object[] { card });
                }
            }
        }
        
        if (toolId == "Spectrograph")
        {
            var specSystem = GetComponent("SpectrographSystem");
            if (specSystem != null)
            {
                var hoverEndMethod = specSystem.GetType().GetMethod("HandleCardHoverEnd");
                if (hoverEndMethod != null)
                {
                    hoverEndMethod.Invoke(specSystem, new object[] { card });
                }
            }
        }
    }
}
