using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Manages different card types and their associated hands
/// Makes it easy to add new card types and hands to the game
/// </summary>
public class CardTypeManager : SingletonMonoBehaviour<CardTypeManager>
{

    [Header("Hands")]
    [Tooltip("All hands in the scene that can hold cards")]
    public List<HorizontalCardHolder> allHands = new List<HorizontalCardHolder>();

    [Header("Default Hands")]
    public HorizontalCardHolder defaultHand;
    public HorizontalCardHolder matHand;
    
    [Header("Animation Settings")]
    [Range(0.1f, 2f)]
    public float cardReturnAnimationDuration = 0.5f;
    [Range(0.1f, 1f)]
    public float cardReturnDelayBetweenCards = 0.2f;
    [Header("Animation Easing")]
    public DG.Tweening.Ease cardReturnEase = DG.Tweening.Ease.OutBack;

    protected override void OnSingletonAwake() { }

    /// <summary>
    /// Get the appropriate hand for a specific card mode
    /// </summary>
    public HorizontalCardHolder GetHandForCardMode(CardMode mode)
    {
        Debug.Log($"[CardTypeManager] Looking for a hand for card mode: {mode}");

        // First pass: Look for a hand specifically configured for this single card type.
        foreach (var hand in allHands)
        {
            if (hand.purpose == HolderPurpose.Hand && hand.acceptedCardTypes == GetAcceptedTypeForSingleMode(mode))
            {
                Debug.Log($"[CardTypeManager] Found a SPECIFIC hand for {mode}: {hand.name}");
                return hand;
            }
        }
        
        // Second pass: If no specific hand is found, look for a general hand (like Mixed or All) that can accept the card.
        foreach (var hand in allHands)
        {
            if (hand.purpose == HolderPurpose.Hand && hand.CanAcceptCardType(mode))
            {
                Debug.Log($"[CardTypeManager] Found a GENERAL hand for {mode}: {hand.name}. This is a fallback.");
                return hand;
            }
        }

        Debug.LogWarning($"[CardTypeManager] No suitable hand found for {mode}. Returning default hand.");
        return defaultHand;
    }

    /// <summary>
    /// Helper to convert a CardMode to its specific AcceptedCardTypes equivalent.
    /// </summary>
    private AcceptedCardTypes GetAcceptedTypeForSingleMode(CardMode mode)
    {
        switch (mode)
        {
            case CardMode.Book: return AcceptedCardTypes.Books;
            case CardMode.Evidence: return AcceptedCardTypes.Evidence;
            case CardMode.Case: return AcceptedCardTypes.Cases;
            case CardMode.Report: return AcceptedCardTypes.Reports;
            case CardMode.Phone: return AcceptedCardTypes.Phones;
            default: return AcceptedCardTypes.All; // Should not happen for specific types
        }
    }

    /// <summary>
    /// Check if a card type uses free-form placement
    /// </summary>
    public bool UsesFreeFormPlacement(CardMode mode)
    {
        // This concept might need to be moved to the HorizontalCardHolder itself
        // For now, let's find the hand and check its properties
        var hand = GetHandForCardMode(mode);
        if (hand != null)
        {
            return hand.enableFreeFormPlacement;
        }
        return false;
    }

    /// <summary>
    /// Add a card to its appropriate hand
    /// </summary>
    public void AddCardToAppropriateHand(Card card, int index = -1)
    {
        if (card == null) return;

        HorizontalCardHolder targetHand = GetHandForCardMode(card.mode);
        if (targetHand != null)
        {
            targetHand.AddCardToHand(card, index);
        }
    }

    /// <summary>
    /// Add a card to its appropriate hand at a specific position (for free-form)
    /// </summary>
    public void AddCardToAppropriateHandAtPosition(Card card, Vector2 screenPosition)
    {
        if (card == null) return;

        HorizontalCardHolder targetHand = GetHandForCardMode(card.mode);
        if (targetHand != null && targetHand.enableFreeFormPlacement)
        {
            targetHand.AddCardToHandAtPosition(card, screenPosition);
        }
        else
        {
            // Fallback to regular placement
            AddCardToAppropriateHand(card);
        }
    }

    /// <summary>
    /// Move a card to the mat with appropriate placement style
    /// </summary>
    public void MoveCardToMat(Card card, Vector2? screenPosition = null)
    {
        if (card == null || matHand == null) return;

        // Remove from current hand
        if (card.parentHolder != null)
            card.parentHolder.RemoveCard(card);

        // Add to mat
        if (screenPosition.HasValue && matHand.enableFreeFormPlacement)
        {
            matHand.AddCardToHandAtPosition(card, screenPosition.Value);
        }
        else
        {
            matHand.AddCardToHand(card);
        }
    }

    /// <summary>
    /// Return a card from the mat to its original hand with smooth animation
    /// </summary>
    public void ReturnCardFromMat(Card card)
    {
        if (card == null) return;

        // Return to appropriate hand based on card type
        HorizontalCardHolder targetHand = GetReturnHandForCard(card);
        if (targetHand != null)
        {
            StartCoroutine(AnimateCardReturn(card, targetHand));
        }
    }

    /// <summary>
    /// Simply move a card from mat to its target hand - let visuals follow automatically
    /// </summary>
    private System.Collections.IEnumerator AnimateCardReturn(Card card, HorizontalCardHolder targetHand)
    {
        // Remove from mat first
        if (matHand != null)
        {
            matHand.RemoveCard(card);
        }

        // Simply add to target hand - let the system handle positioning and visuals
        targetHand.AddCardToHand(card);
        
        
        
        // Small delay just for visual pacing between card movements
        yield return new WaitForSeconds(cardReturnDelayBetweenCards);
    }

    /// <summary>
    /// Get all cards of a specific type
    /// </summary>
    public List<Card> GetCardsOfType(CardMode mode)
    {
        List<Card> cardsOfType = new List<Card>();
        
        HorizontalCardHolder hand = GetHandForCardMode(mode);
        if (hand != null)
        {
            foreach (Card card in hand.cards)
            {
                if (card.mode == mode)
                    cardsOfType.Add(card);
            }
        }

        // Also check mat
        if (matHand != null)
        {
            foreach (Card card in matHand.cards)
            {
                if (card.mode == mode)
                    cardsOfType.Add(card);
            }
        }

        return cardsOfType;
    }

    /// <summary>
    /// Load cards of a specific type into their appropriate hand
    /// </summary>
    public void LoadCardsOfType<T>(List<T> cardDataList, CardMode targetMode) where T : ICardData
    {
        HorizontalCardHolder targetHand = GetHandForCardMode(targetMode);
        if (targetHand != null)
        {
            // Convert to object list for the generic LoadCardsFromData method
            List<object> objectList = new List<object>();
            foreach (T cardData in cardDataList)
            {
                objectList.Add(cardData);
            }

            // Load using reflection or create cards manually
            StartCoroutine(LoadCardsCoroutine(objectList, targetHand));
        }
    }

    private System.Collections.IEnumerator LoadCardsCoroutine(List<object> cardDataList, HorizontalCardHolder targetHand)
    {
        foreach (object cardData in cardDataList)
        {
            if (cardData is ICardData)
            {
                // Create card instance and load data
                // This would need to be implemented based on your card instantiation system
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Return all cards from the mat to their appropriate original hands
    /// Used during case closing routine
    /// </summary>
    public System.Collections.IEnumerator ReturnAllCardsFromMatToHands()
    {
        if (matHand == null) yield break;

        var matCardsToMove = new List<Card>(matHand.cards); // Copy to avoid modification during iteration
        
        // Handle mat cards first
        foreach (Card card in matCardsToMove)
        {
            if (card != null) // Check if card still exists
            {
                ReturnCardFromMat(card);
                yield return new WaitForSeconds(cardReturnDelayBetweenCards);
            }
        }
        
        // Handle duster cards separately
        if (ToolsManager.Instance != null)
        {
            foreach (var tool in ToolsManager.Instance.tools)
            {
                if (tool != null && tool.toolId == "FingerPrintDuster")
                {
                    var dusterSystem = tool.GetComponent<FingerPrintDusterSystem>();
                    if (dusterSystem != null && dusterSystem.CardSlot != null)
                    {
                        var dusterCards = new List<Card>(dusterSystem.CardSlot.cards);
                        foreach (var card in dusterCards)
                        {
                            if (card != null) // Check if card still exists
                            {
                                Debug.Log($"[CardTypeManager] Found card '{card.name}' in FingerPrintDuster - returning to evidence hand");
                                ReturnCardFromDuster(card, dusterSystem);
                                yield return new WaitForSeconds(cardReturnDelayBetweenCards);
                            }
                        }
                    }
                }
            }
        }
        
        // Wait for the last card animation to complete
        yield return new WaitForSeconds(cardReturnAnimationDuration);
    }
    
    /// <summary>
    /// Return a card from the duster to its appropriate hand
    /// </summary>
    private void ReturnCardFromDuster(Card card, FingerPrintDusterSystem dusterSystem)
    {
        if (card == null) return;

        // Return to appropriate hand based on card type
        HorizontalCardHolder targetHand = GetReturnHandForCard(card);
        if (targetHand != null)
        {
            StartCoroutine(AnimateCardReturnFromDuster(card, targetHand, dusterSystem));
        }
    }
    
    /// <summary>
    /// Simply move a card from duster to its target hand
    /// </summary>
    private System.Collections.IEnumerator AnimateCardReturnFromDuster(Card card, HorizontalCardHolder targetHand, FingerPrintDusterSystem dusterSystem)
    {
        // Remove from duster first
        if (dusterSystem.CardSlot != null)
        {
            dusterSystem.CardSlot.RemoveCard(card);
        }

        // Simply add to target hand - let the system handle positioning and visuals
        targetHand.AddCardToHand(card);
        
        // Small delay just for visual pacing between card movements
        yield return new WaitForSeconds(cardReturnDelayBetweenCards);
    }

    /// <summary>
    /// Get the appropriate hand for returning a card from the mat
    /// This ensures cards go back to their type-specific hands
    /// </summary>
    public HorizontalCardHolder GetReturnHandForCard(Card card)
    {
        if (card == null) 
        {
            return defaultHand;
        }
        
        // Cases should NEVER be on the mat - they should only be in CaseSlot
        if (card.mode == CardMode.Case)
        {
            HorizontalCardHolder caseHand = GetHandForCardMode(CardMode.Case);
            return caseHand; // Return to case hand as fallback
        }
        
        HorizontalCardHolder returnHand = GetHandForCardMode(card.mode);
        return returnHand;
    }

    /// <summary>
    /// Clean up orphaned visual objects from all managed card hands
    /// </summary>
    public void CleanupAllVisualHandlers()
    {
        var allHands = GetAllManagedHands();
        
        foreach (var hand in allHands)
        {
            if (hand != null && hand.bigVisualHandler != null)
            {
                CleanupVisualHandler(hand.name, hand.bigVisualHandler);
            }
        }
    }
    
    /// <summary>
    /// Clean up orphaned visuals in a specific visual handler
    /// </summary>
    private void CleanupVisualHandler(string handlerName, Transform visualHandler)
    {
        if (visualHandler == null) return;
        
        List<Transform> orphanedObjects = new List<Transform>();
        
        // Find children that look like BigCardVisual clones but don't have the script
        for (int i = 0; i < visualHandler.childCount; i++)
        {
            Transform child = visualHandler.GetChild(i);
            
            // Check if it's a BigCardVisual clone without the script
            if (child.name.Contains("BigCardVisual") && child.name.Contains("Clone"))
            {
                BigCardVisual bigCardScript = child.GetComponent<BigCardVisual>();
                if (bigCardScript == null)
                {
                    orphanedObjects.Add(child);
                }
            }
            // Also check for other common orphaned patterns
            else if (child.name.Contains("CardVisual") && child.name.Contains("Clone"))
            {
                CardVisual cardScript = child.GetComponent<CardVisual>();
                if (cardScript == null)
                {
                    orphanedObjects.Add(child);
                }
            }
        }
        
        // Destroy orphaned objects
        foreach (Transform orphan in orphanedObjects)
        {
            Destroy(orphan.gameObject);
        }
    }
    
    /// <summary>
    /// Get all hands managed by this CardTypeManager
    /// </summary>
    private List<HorizontalCardHolder> GetAllManagedHands()
    {
        List<HorizontalCardHolder> hands = new List<HorizontalCardHolder>();
        
        // Add the directly referenced hands
        if (defaultHand != null) hands.Add(defaultHand);
        if (matHand != null) hands.Add(matHand);
        
        // Add hands from the allHands list
        foreach (var hand in allHands)
        {
            if (hand != null && !hands.Contains(hand))
            {
                hands.Add(hand);
            }
        }
        
        return hands;
    }
} 