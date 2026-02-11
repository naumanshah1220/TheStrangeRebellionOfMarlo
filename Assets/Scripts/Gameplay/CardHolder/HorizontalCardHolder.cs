using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;
using System.Linq;

/// <summary>
/// HorizontalCardHolder manages a 'hand' or row of cards with fanned layout,
/// MixAndJam-style animation, and your added support for evidence/case/data modes.
/// </summary>

public enum HolderType { Hand, Mat, Computer, FingerPrintDuster, Spectrograph, Book, Overseer }

public class HorizontalCardHolder : MonoBehaviour
{
    [Header("New Card Holder System")]
    [Tooltip("How cards are displayed in this holder")]
    public VisualMode visualMode = VisualMode.SmallCards;
    
    [Tooltip("What types of cards this holder can accept")]
    public AcceptedCardTypes acceptedCardTypes = AcceptedCardTypes.Cases;
    
    [Tooltip("The functional purpose of this holder")]
    public HolderPurpose purpose = HolderPurpose.Hand;

    [Header("Layout Settings")]
    [Tooltip("Enable the fanned/curved layout for cards in this hand.")]
    public bool enableFanningAndCurve = true;
    public bool enableFreeFormPlacement = false; // For Papers Please style placement
    
    [Header("Layout Adaptive Squeeze")]
    [Tooltip("Enable to only squeeze cards when count >= minCardsToSqueeze")]
    public bool enableAdaptiveSqueeze = true;
    [Tooltip("Start squeezing only when this many cards are present")]
    public int minCardsToSqueeze = 4;
    [Tooltip("Preferred slot width when not squeezing (≤ threshold)")]
    public float minSlotPreferredWidth = 160f;
    [Tooltip("Minimum per-slot width while squeezing (prevents over-squeeze)")]
    public float minSqueezedSlotWidth = 80f;

    // Slot baseline size (auto-detected from slotPrefab at runtime). Not exposed in Inspector.
    private float baselineSlotWidth = -1f;
    private float baselineSlotHeight = -1f;

    [Header("References")]
    public Transform visualHandler;  // For small card visuals
    public Transform bigVisualHandler;  // For big card visuals  
    [SerializeField] public GameObject slotPrefab;

    [SerializeField] private Card selectedCard;
    [SerializeField] private Card hoveredCard;

    [Header("Spawn Settings")]
    public List<Card> cards = new List<Card>();
    [SerializeField] private float fallback_slotSizeModifier = 2;


    [Header("Card Deal Animation")]
    public Transform cardStartPoint; // Assign in world/canvas space
    public float dealDelay = 0.1f;
    public float dealFlyTime = 0.3f;

    private RectTransform rect;
    private bool isCrossing = false;
    private bool isDealing = false; // Track if cards are currently being dealt
    private int cardsDealingCount = 0; // Track how many cards are still being dealt

    private DaysManager daysManager;


    // 🆕 Setup: Load data, initialize visuals, deal in with MixAndJam-style fly-in
    void Start()
    {
        // Initialize rect to prevent NullReferenceException in EndDrag
        rect = GetComponent<RectTransform>();
        
        daysManager = FindFirstObjectByType<DaysManager>();
        
        // Only subscribe to day start event if this holder should load cases
        // Use legacy system check to maintain original behavior
        if (purpose == HolderPurpose.Hand && acceptedCardTypes == AcceptedCardTypes.Cases)
        {
            Debug.Log($"[{gameObject.name}] Subscribing to day start event (Case Hand)");
            daysManager.onDayStart.AddListener(LoadCasesOnDayStart);
        }
        else
        {
            Debug.Log($"[{gameObject.name}] NOT subscribing to day start event (purpose: {purpose}, acceptedTypes: {acceptedCardTypes})");
        }
        
        // Validate new card holder system configuration
        ValidateCardHolderConfiguration();

        // Capture baseline slot size from prefab (once)
        if (slotPrefab != null)
        {
            var prefabRect = slotPrefab.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                baselineSlotWidth = prefabRect.sizeDelta.x;
                baselineSlotHeight = prefabRect.sizeDelta.y;
            }
        }
    }

    /// <summary>
    /// Validates that the new card holder system is properly configured
    /// </summary>
    private void ValidateCardHolderConfiguration()
    {
        // Check for logical inconsistencies
        if (purpose == HolderPurpose.BookShelf && acceptedCardTypes != AcceptedCardTypes.Books)
        {
            Debug.LogWarning($"[{gameObject.name}] BookShelf should only accept Books! Current: {acceptedCardTypes}");
        }
        
        if (purpose == HolderPurpose.ReportFile && acceptedCardTypes != AcceptedCardTypes.Reports)
        {
            Debug.LogWarning($"[{gameObject.name}] ReportFile should only accept Reports! Current: {acceptedCardTypes}");
        }
        
        // Validate visual mode consistency
        if (purpose == HolderPurpose.Mat && visualMode != VisualMode.BigCards)
        {
            Debug.LogWarning($"[{gameObject.name}] Mat should use BigCards visual mode! Current: {visualMode}");
        }
        
        if (purpose == HolderPurpose.Computer && visualMode != VisualMode.BigCards)
        {
            Debug.LogWarning($"[{gameObject.name}] Computer should use BigCards visual mode! Current: {visualMode}");
        }
        
        if (purpose == HolderPurpose.FingerPrintDuster && visualMode != VisualMode.BigCards)
        {
            Debug.LogWarning($"[{gameObject.name}] FingerPrintDuster should use BigCards visual mode! Current: {visualMode}");
        }
    }

    public void LoadCasesOnDayStart()
    {
        var pendingCases = daysManager.GetCasesForToday();
        // Use legacy system check to maintain original behavior
        // Only load cases if this is actually a case hand
        if (purpose == HolderPurpose.Hand && acceptedCardTypes == AcceptedCardTypes.Cases)
        {
            Debug.Log($"[{gameObject.name}] Loading {pendingCases.Count} cases for day start");
            LoadCardsFromData(pendingCases, true);
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Skipping case loading - not a case hand (purpose: {purpose}, acceptedTypes: {acceptedCardTypes})");
        }
    }

    /// <summary>
    /// Loads and deals cards from a data list of cases/evidences (or any custom Card data)
    /// </summary>
    public void LoadCardsFromData<T>(List<T> dataList, bool deleteOld = true)
    {
        StopAllCoroutines();
        if (dataList != null)
            StartCoroutine(SetupCardsAndDealVisuals(dataList, deleteOld));
    }

    private IEnumerator SetupCardsAndDealVisuals<T>(List<T> dataList, bool deleteOld)
    {
        Debug.Log($"[{gameObject.name}] Starting SetupCardsAndDealVisuals with {dataList.Count} items. deleteOld is {deleteOld}.");
        if (deleteOld)
        {
            // Clear old slots/cards
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            cards.Clear();
        }

        rect = GetComponent<RectTransform>();

        // STEP 1: Instantiate all slots immediately
        int startingIndex = deleteOld ? 0 : cards.Count;
        List<Card> newlyAddedCards = new List<Card>();
        for (int i = 0; i < dataList.Count; i++)
        {
            Debug.Log($"[{gameObject.name}] Creating card for item {i} of type {typeof(T)}.");
            GameObject slot = Instantiate(slotPrefab, transform);
            slot.transform.localPosition = Vector3.zero;
            // Note: We'll size the slot after the card is initialized, not here


            Card card = slot.GetComponentInChildren<Card>();
            if (card == null)
            {
                Debug.LogError($"No Card found in slot prefab at index {i}");
                continue;
            }

            // 🟢 Event & selection setup for each card
            card.name = (startingIndex + i).ToString();
            card.parentHolder = this;
            card.homeHand = this; // Set the initial home hand
            card.PointerEnterEvent.AddListener(CardPointerEnter);
            card.PointerExitEvent.AddListener(CardPointerExit);
            card.BeginDragEvent.AddListener(BeginDrag);
            card.EndDragEvent.AddListener(EndDrag);

            // 🟢 Initialize the visuals/data with your own method
            card.Initialize(dataList[i], visualHandler, bigVisualHandler);
            
            // Set the deal fly time for the card visual
            if (card.cardVisual != null)
            {
                card.cardVisual.SetDealFlyTime(dealFlyTime);
            }
            
            // Step 1: Check for enhanced card visual and handle evidence hand setup
            // Use new system: Check if this holder accepts evidence and has hand purpose
            if (purpose == HolderPurpose.Hand && acceptedCardTypes == AcceptedCardTypes.Evidence)
            {
                var enhancedCardVisual = card.GetComponent<EnhancedCardVisual>();
                
                if (enhancedCardVisual == null && card.bigCardVisual != null)
                {
                    enhancedCardVisual = card.bigCardVisual.GetComponent<EnhancedCardVisual>();
                }
                
                if (enhancedCardVisual != null)
                {
                    Debug.Log($"[HorizontalCardHolder] Calling OnAddedToEvidenceHand on {enhancedCardVisual.name}");
                    enhancedCardVisual.OnAddedToEvidenceHand();
                }
                else
                {
                    Debug.Log($"[HorizontalCardHolder] No EnhancedCardVisual found on card {card.name}");
                }
            }
            
            // Ensure slot width is reset to baseline on creation (prevents inherited large sizes)
            // Only when adaptive squeeze is enabled (otherwise keep HLG-authored behavior)
            if (enableAdaptiveSqueeze)
            {
                var slotRectInit = slot.GetComponent<RectTransform>();
                if (slotRectInit != null)
                {
                    float targetW = baselineSlotWidth > 0 ? baselineSlotWidth : (minSlotPreferredWidth > 0 ? minSlotPreferredWidth : slotRectInit.sizeDelta.x);
                    slotRectInit.sizeDelta = new Vector2(targetW, slotRectInit.sizeDelta.y);
                }
            }

            // Size the slot appropriately for mat placement (only if this holder is Mat)
            if (purpose == HolderPurpose.Mat)
            {
                SizeSlotForBigCard(card, slot);
            }
            
            // Hide visual for "deal in" animation
            if (card.cardVisual != null)
                card.cardVisual.gameObject.SetActive(false);
            cards.Add(card);
            newlyAddedCards.Add(card);
        }

        yield return new WaitForEndOfFrame();

        // STEP 2: Deal visuals in one by one (MixAndJam style)
        var cardsToDeal = deleteOld ? cards : newlyAddedCards;
        isDealing = true; // Start dealing
        cardsDealingCount = cardsToDeal.Count; // Track only the new cards being dealt

        for (int i = 0; i < cardsToDeal.Count; i++)
        {
            Card card = cardsToDeal[i];
            if (card.cardVisual != null)
            {
                card.cardVisual.gameObject.SetActive(true);
                card.cardVisual.isFlyingIn = true;
                card.cardVisual.transform.position = cardStartPoint.position;
                card.cardVisual.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
                
                // Start monitoring this card's animation completion
                StartCoroutine(MonitorCardAnimation(card));
            }
            
            // Only add delay between cards, not before the first one
            if (i < cardsToDeal.Count - 1)
            {
                yield return new WaitForSeconds(dealDelay);
            }
        }
        
        // Wait for all dealt cards to finish their animations
        yield return StartCoroutine(WaitForAllCardsToSettle());
        
        isDealing = false; // Dealing complete for this batch
        cardsDealingCount = 0;

        // Apply adaptive layout after cards are fully dealt
        ApplyAdaptiveLayout();
    }
    
    /// <summary>
    /// Monitor when a card's flying animation is complete
    /// </summary>
    private IEnumerator MonitorCardAnimation(Card card)
    {
        if (card.cardVisual == null) yield break;
        
        // Wait until the card is no longer flying in
        while (card.cardVisual.isFlyingIn)
        {
            yield return null;
        }
        
        // Decrement the dealing counter when this card is done
        cardsDealingCount--;
    }
    
    /// <summary>
    /// Wait for all cards to finish their deal animations
    /// </summary>
    private IEnumerator WaitForAllCardsToSettle()
    {
        // Wait until all cards have finished their animations
        while (cardsDealingCount > 0)
        {
            yield return null;
        }
        
        // Additional small delay to ensure all animations are truly complete
        yield return new WaitForSeconds(0.05f);
    }

    /// <summary>
    /// Loads and deals a single card from a data list of cases/evidences (or any custom Card data)
    /// </summary>
    public void LoadCardsFromData(Case caseData)
    {
        StopAllCoroutines();
        if (caseData != null)
            StartCoroutine(SetupACardAndDealVisuals(caseData));
    }

    private IEnumerator SetupACardAndDealVisuals(Case caseData)
    {
        rect = GetComponent<RectTransform>();

        // STEP 1: Instantiate all slots immediately

        GameObject slot = Instantiate(slotPrefab, transform);
        slot.transform.localPosition = Vector3.zero;
        // Note: We'll size the slot after the card is initialized, not here


        Card card = slot.GetComponentInChildren<Card>();
        if (card == null)
        {
            Debug.LogError($"No Card found in slot prefab at index");
        }

        // 🟢 Event & selection setup for each card
        card.name = cards.Count.ToString();
        card.parentHolder = this;
        card.homeHand = this; // Set the initial home hand
        card.PointerEnterEvent.AddListener(CardPointerEnter);
        card.PointerExitEvent.AddListener(CardPointerExit);
        card.BeginDragEvent.AddListener(BeginDrag);
        card.EndDragEvent.AddListener(EndDrag);

        // 🟢 Initialize the visuals/data with your own method
        card.Initialize(caseData, visualHandler, bigVisualHandler);
        
        // Set the deal fly time for the card visual
        if (card.cardVisual != null)
        {
            card.cardVisual.SetDealFlyTime(dealFlyTime);
        }
        
        // Size the slot appropriately for mat placement
        if (purpose == HolderPurpose.Mat)
        {
            SizeSlotForBigCard(card, slot);
        }
        
        // Hide visual for "deal in" animation
        if (card.cardVisual != null)
            card.cardVisual.gameObject.SetActive(false);
        cards.Add(card);

        yield return new WaitForSeconds(dealDelay);

        // STEP 2: Deal visuals in one by one (MixAndJam style)
        isDealing = true; // Start dealing
        cardsDealingCount = 1; // Track the single card being dealt
        
        card.cardVisual.gameObject.SetActive(true);
        card.cardVisual.isFlyingIn = true;
        card.cardVisual.transform.position = cardStartPoint.position;
        card.cardVisual.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        
        // Start monitoring this card's animation completion
        yield return StartCoroutine(MonitorCardAnimation(card));
        
        isDealing = false; // Dealing complete
        cardsDealingCount = 0;
    }
    


    /// <summary>
    /// Handle selection visuals and card offset (MixAndJam style)
    /// </summary>
    public void SelectCard(Card card)
    {
        if (!cards.Contains(card))
            return;

        if (card != null && selectedCard != card)
        {
            selectedCard = card;
            
            // In free-form mode, bring selected card to front for better interaction
            if (enableFreeFormPlacement)
            {
        
                BringCardToFront(card);
            }
        }
    }

    public void DeselectCard()
    {
        if (selectedCard != null)
        {
            selectedCard = null;
        }
    }
    public void BeginDrag(Card card)
    {
        // Prevent dragging while cards are being dealt
        if (isDealing) return;
        
        SelectCard(card);
        
        // In free-form mode, immediately bring card to front when drag starts
        if (enableFreeFormPlacement)
        {
    
            BringCardToFront(card);
        }
    }

    public void EndDrag(Card card)
    {
        if (selectedCard == null)
            return;

        rect.sizeDelta += Vector2.right;
        rect.sizeDelta -= Vector2.right;

        // Visual switching is handled automatically by cardLocation property setter

        selectedCard = null;
    }

    public void CardPointerEnter(Card card)
    {
        hoveredCard = card;
    }

    public void CardPointerExit(Card card)
    {
        hoveredCard = null;
    }

    void Update()
    {
        // Simple "delete card" for debug
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            if (hoveredCard != null)
            {
                Destroy(hoveredCard.transform.parent.gameObject);
                cards.Remove(hoveredCard);
            }
        }

        // --- Drag-to-Swap Logic (MixAndJam feel) ---
        // Skip swap logic in free-form mode
        if (enableFreeFormPlacement)
            return;
            
        if (selectedCard == null)
            return;

        if (isCrossing)
            return;

        for (int i = 0; i < cards.Count; i++)
        {
            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }

            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Swaps the slots of two cards for MixAndJam smooth hand motion and fanning.
    /// </summary>
    void Swap(int index)
    {
        isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;

        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].selected ? new Vector3(0, cards[index].selectionOffset, 0) : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        isCrossing = false;

        if (cards[index].cardVisual == null)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);

        // Updated visual indexes for all cards (for proper hand fanning)
        foreach (Card card in cards)
            card.cardVisual.UpdateIndex(transform.childCount);
    }

    public void RemoveCard(Card card)
    {
        if (cards.Contains(card))
        {
            card.PointerEnterEvent.RemoveAllListeners();
            cards.Remove(card);
            card.parentHolder = null;
            card.gameObject.SetActive(false);

            if (card.transform.parent != null)
                card.transform.parent.gameObject.SetActive(false);
        }

        // Update adaptive layout when a card is removed
        ApplyAdaptiveLayout();
    }

    public void AddCardToHand(Card card, int index = -1)
    {
        // Reactivate the slot (parent of Card) and CardVisual
        if (card.transform.parent != null)
            card.transform.parent.gameObject.SetActive(true);
        card.transform.parent.SetParent(transform);
        
        // When a card is added to a new hand, update its parent and, if it's a true "Hand", its home.
        card.parentHolder = this;
        if (purpose == HolderPurpose.Hand)
        {
            card.homeHand = this;
        }

        // Set visuals to appropriate handlers
        if (card.cardVisual != null)
            card.cardVisual.transform.SetParent(visualHandler);
        if (card.bigCardVisual != null)
            card.bigCardVisual.transform.SetParent(bigVisualHandler != null ? bigVisualHandler : visualHandler);
        
        card.PointerEnterEvent.AddListener(CardPointerEnter);
        card.PointerExitEvent.AddListener(CardPointerExit);
        card.BeginDragEvent.AddListener(BeginDrag);
        card.EndDragEvent.AddListener(EndDrag);


        // Determine card location and scale based on new holder system
        if (card.parentHolder.ShowsSmallCards())
        {
            // Cards in hands show small visuals
            card.cardLocation = CardLocation.Hand;
            card.transform.localScale = new Vector3(1, 1, 1);
            
            // When returning from a mat, scale down the slot to its original size
            if (card.transform.parent != null)
            {
                ResetSlotSize(card.transform.parent.gameObject);
            }
        }
        else if (card.parentHolder.ShowsBigCards())
        {
            // Cards in big visual holders show big visuals
            if (card.parentHolder.purpose == HolderPurpose.Mat && card.mode == CardMode.Case)
            {
                card.cardLocation = CardLocation.Slot;
            }
            else
            {
                card.cardLocation = CardLocation.Mat;
            }
            
            Vector3 bigCardScale = GetBigCardScale(card);
            card.transform.localScale = bigCardScale;
            
            // Resize the slot for big cards
            if (card.transform.parent != null)
            {
                SizeSlotForBigCard(card, card.transform.parent.gameObject);
            }
        }

        // Let the automatic cardLocation system handle visual switching
        // The cardLocation property setter will automatically call UpdateVisualBasedOnLocation()

        card.gameObject.SetActive(true);

        // Set the deal fly time for the card visual
        if (card.cardVisual != null)
        {
            card.cardVisual.SetDealFlyTime(dealFlyTime);
        }

        if (!cards.Contains(card))
        {
            // Insert at specified index, or add to end
            if (index < 0 || index > cards.Count)
                cards.Add(card);
            else
                cards.Insert(index, card);

            // For free-form placement, disable automatic layout positioning
            if (!enableFreeFormPlacement)
            {
                // Set sibling index for layout
                if (card.transform.parent != null)
                    card.transform.parent.SetSiblingIndex(index < 0 ? cards.Count - 1 : index);

                if (card.cardVisual.transform != null)
                {
            
                    card.cardVisual.transform.SetSiblingIndex(index < 0 ? cards.Count - 1 : index);
                }
            }
            else
            {
                // For free-form, disable any Layout Group components
                DisableLayoutGroups();
            }
        }

        // Update adaptive layout when a card is added/repositioned (layout-only; avoid moving free-form slots)
        ApplyAdaptiveLayout();
    }

    // Note: Visual switching is now handled automatically by the cardLocation property setter
    // which calls UpdateVisualBasedOnLocation() when the location changes

    public void AddCardToHandAtPosition(Card card, Vector2 screenPosition)
    {
        if (!enableFreeFormPlacement) 
        {
            AddCardToHand(card);
            return;
        }

        // Check if card is already in this hand (repositioning)
        bool isRepositioning = cards.Contains(card);
        
        if (!isRepositioning)
        {
            AddCardToHand(card);
        }
        
        // Position card at specific screen position for free-form placement
        // Use RectTransformUtility for proper UI coordinate conversion
        Vector2 localPoint;
        RectTransform holderRect = GetComponent<RectTransform>();
        
        // Determine the correct camera based on canvas render mode
        Canvas canvas = holderRect.GetComponentInParent<Canvas>();
        Camera canvasCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera ?? Camera.main;
        }
        
        if (holderRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            holderRect, 
            screenPosition, 
            canvasCamera, 
            out localPoint))
        {
            // Work directly in local space for UI elements - no world conversion needed
            PositionCardAtLocalPoint(card, localPoint);
        }
        
        // Bring card to front
        BringCardToFront(card);
    }

    /// <summary>
    /// Add a card to this holder and place its slot at an exact world position in a single step.
    /// Avoids one-frame flashes at a default position for free-form mats.
    /// </summary>
    public void AddCardToHandAtWorldPosition(Card card, Vector3 worldPosition)
    {
        if (!enableFreeFormPlacement)
        {
            AddCardToHand(card);
            return;
        }

        // Reactivate slot (parent of Card) and reparent to this holder, but keep card inactive until after placement
        if (card.transform.parent != null)
        {
            card.transform.parent.gameObject.SetActive(true);
            card.transform.parent.SetParent(transform);
        }

        // Update parent references
        card.parentHolder = this;
        if (purpose == HolderPurpose.Hand)
        {
            card.homeHand = this;
        }

        // Set visual parents
        if (card.cardVisual != null)
            card.cardVisual.transform.SetParent(visualHandler);
        if (card.bigCardVisual != null)
            card.bigCardVisual.transform.SetParent(bigVisualHandler != null ? bigVisualHandler : visualHandler);

        // Hook listeners (same as AddCardToHand)
        card.PointerEnterEvent.AddListener(CardPointerEnter);
        card.PointerExitEvent.AddListener(CardPointerExit);
        card.BeginDragEvent.AddListener(BeginDrag);
        card.EndDragEvent.AddListener(EndDrag);

        // Determine visuals (big-visual mats vs hands)
        if (ShowsSmallCards())
        {
            card.cardLocation = CardLocation.Hand;
            card.transform.localScale = new Vector3(1, 1, 1);
            if (card.transform.parent != null)
            {
                ResetSlotSize(card.transform.parent.gameObject);
            }
        }
        else
        {
            // Big visual holder (mat/tools)
            card.cardLocation = (purpose == HolderPurpose.Mat && card.mode == CardMode.Case) ? CardLocation.Slot : CardLocation.Mat;
            Vector3 bigCardScale = GetBigCardScale(card);
            card.transform.localScale = bigCardScale;
            if (card.transform.parent != null)
            {
                SizeSlotForBigCard(card, card.transform.parent.gameObject);
            }
        }

        // Ensure card present in list
        if (!cards.Contains(card))
        {
            cards.Add(card);
            if (!enableFreeFormPlacement)
            {
                if (card.transform.parent != null)
                    card.transform.parent.SetSiblingIndex(cards.Count - 1);
                if (card.cardVisual.transform != null)
                    card.cardVisual.transform.SetSiblingIndex(cards.Count - 1);
            }
            else
            {
                DisableLayoutGroups();
            }
        }

        // Compute and set exact local position for the slot
        RectTransform holderRect = GetComponent<RectTransform>();
        if (holderRect != null)
        {
            Vector3 local = holderRect.InverseTransformPoint(worldPosition);
            Transform target = card.transform.parent != null ? card.transform.parent : card.transform;
            target.localPosition = new Vector3(local.x, local.y, 0);
        }

        // Activate after placement to avoid one-frame flash at default position
        card.gameObject.SetActive(true);

        // Ensure correct sizing (again) and bring to front
        if (card.transform.parent != null)
        {
            if (!ShowsSmallCards()) SizeSlotForBigCard(card, card.transform.parent.gameObject);
        }
        BringCardToFront(card);

        // Deal fly time remains as configured
        if (card.cardVisual != null)
        {
            card.cardVisual.SetDealFlyTime(dealFlyTime);
        }

        ApplyAdaptiveLayout();
    }

    private void PositionCardAtLocalPoint(Card card, Vector2 localPoint)
    {
        if (!enableFreeFormPlacement) return;

        Vector3 localPos = new Vector3(localPoint.x, localPoint.y, 0);
        // Position the card slot (parent of the card)
        Transform targetTransform = card.transform.parent != null ? card.transform.parent : card.transform;
        Vector3 oldPosition = targetTransform.localPosition;
        
        targetTransform.localPosition = localPos;
    }

    private void PositionCardAtWorldPoint(Card card, Vector3 worldPosition)
    {
        if (!enableFreeFormPlacement) return;

        // Convert world position to local position within bounds
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        
        // Clamp to bounds
        RectTransform holderRect = GetComponent<RectTransform>();
        if (holderRect != null)
        {
            Vector2 holderSize = holderRect.sizeDelta;
            localPos.x = Mathf.Clamp(localPos.x, -holderSize.x / 2, holderSize.x / 2);
            localPos.y = Mathf.Clamp(localPos.y, -holderSize.y / 2, holderSize.y / 2);
            localPos.z = 0;
        }

        // Position the card slot (parent of the card)
        if (card.transform.parent != null)
        {
            card.transform.parent.localPosition = localPos;
        }
        else
        {
            card.transform.localPosition = localPos;
        }
    }

    private void BringCardToFront(Card card)
    {
        if (!enableFreeFormPlacement) return;

        // Move card to end of list (visual front)
        if (cards.Contains(card))
        {
            cards.Remove(card);
            cards.Add(card);
            
            // IMPORTANT: Move the card slot to last sibling for proper hit detection
            if (card.transform.parent != null)
            {
                card.transform.parent.SetAsLastSibling();
            }
            
            // Use sibling indices for layering instead of Canvas sorting
            if (card.bigCardVisual != null)
            {
                card.bigCardVisual.transform.SetAsLastSibling();
            }
            if (card.cardVisual != null)
            {
                card.cardVisual.transform.SetAsLastSibling();
            }
        }
    }

    private void UpdateCardSortingOrder()
    {
        if (!enableFreeFormPlacement) return;

        // Update sibling indices based on cards list order
        for (int i = 0; i < cards.Count; i++)
        {
            // Update card slot sibling index for hit detection
            if (cards[i].transform.parent != null)
            {
                cards[i].transform.parent.SetSiblingIndex(i);
            }
            
            // Update big card visual sibling index
            if (cards[i].bigCardVisual != null)
            {
                cards[i].bigCardVisual.transform.SetSiblingIndex(i);
            }
            
            // Update small card visual sibling index
            if (cards[i].cardVisual != null)
            {
                cards[i].cardVisual.transform.SetSiblingIndex(i);
            }
        }
    }

    public void UpdateCardSortingOrder(Card card)
    {
        if (!enableFreeFormPlacement || card == null) return;

        // Reset the card's sibling order to its position in the cards list
        int cardIndex = cards.IndexOf(card);
        if (cardIndex >= 0)
        {
            // Update card slot sibling index for hit detection
            if (card.transform.parent != null)
            {
                card.transform.parent.SetSiblingIndex(cardIndex);
            }
            
            // Update big card visual sibling index
            if (card.bigCardVisual != null)
            {
                card.bigCardVisual.transform.SetSiblingIndex(cardIndex);
            }
            
            // Update small card visual sibling index
            if (card.cardVisual != null)
            {
                card.cardVisual.transform.SetSiblingIndex(cardIndex);
            }
        }
        else
        {
            Debug.LogWarning($"[HorizontalCardHolder] Card {card.name} not found in this holder for sibling order reset");
        }
    }

    public void MoveCardToPosition(Card card, Vector3 worldPosition)
    {
        if (!enableFreeFormPlacement || !cards.Contains(card)) return;

        // Convert world position to local for UI positioning
        Vector3 localPos = transform.InverseTransformPoint(worldPosition);
        PositionCardAtLocalPoint(card, new Vector2(localPos.x, localPos.y));
        
        // Bring moved card to front
        BringCardToFront(card);
    }

    /// <summary>
    /// Gets the appropriate scale for a card based on its bigCardVisual size
    /// </summary>
    private Vector3 GetBigCardScale(Card card)
    {
        if (card.bigCardVisual != null)
        {
            // Get the big card visual's rect transform
            RectTransform bigCardRect = card.bigCardVisual.GetComponent<RectTransform>();
            if (bigCardRect != null)
            {
                // Get the small card's rect transform for comparison
                RectTransform cardRect = card.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    // Calculate scale based on the size difference
                    Vector2 bigCardSize = bigCardRect.sizeDelta;
                    Vector2 smallCardSize = cardRect.sizeDelta;
                    
                    if (smallCardSize.x > 0 && smallCardSize.y > 0)
                    {
                        float scaleX = bigCardSize.x / smallCardSize.x;
                        float scaleY = bigCardSize.y / smallCardSize.y;
                        
                        // Use the smaller scale to maintain aspect ratio, or average them
                        float uniformScale = Mathf.Max(scaleX, scaleY);
                        
                        return new Vector3(uniformScale, uniformScale, uniformScale);
                    }
                }
            }
        }
        
        // Fallback to the original bigSlotSize if calculation fails
        return new Vector3(fallback_slotSizeModifier, fallback_slotSizeModifier, fallback_slotSizeModifier);
    }

    /// <summary>
    /// Sizes the card slot based on the bigCardVisual size
    /// </summary>
    private void SizeSlotForBigCard(Card card, GameObject slot)
    {
        if (card.bigCardVisual != null)
        {
            RectTransform bigCardRect = card.bigCardVisual.GetComponent<RectTransform>();
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            
            if (bigCardRect != null && slotRect != null)
            {
                // Set slot size to match the big card visual size
                slotRect.sizeDelta = bigCardRect.sizeDelta;
                return;
            }
        }
        
        // Fallback to original behavior
        RectTransform cardRect = slot.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.sizeDelta = cardRect.sizeDelta * fallback_slotSizeModifier;
        }
    }

    /// <summary>
    /// Resets the card slot to its original baseline size, captured from the prefab.
    /// </summary>
    private void ResetSlotSize(GameObject slot)
    {
        if (slot == null) return;

        RectTransform slotRect = slot.GetComponent<RectTransform>();
        // Use baseline size if available, otherwise do nothing
        if (slotRect != null && baselineSlotWidth > 0 && baselineSlotHeight > 0)
        {
            slotRect.sizeDelta = new Vector2(baselineSlotWidth, baselineSlotHeight);
        }
    }

    /// <summary>
    /// Force bring card to front with maximum priority (called from DragManager)
    /// Uses sibling indices instead of Canvas sorting to maintain viewport masking
    /// </summary>
    public void ForceCardToTopLayer(Card card)
    {
        if (!enableFreeFormPlacement) return;

        // IMPORTANT: Move the card slot to last sibling for proper hit detection
        if (card.transform.parent != null)
        {
            card.transform.parent.SetAsLastSibling();
        }

        // Use sibling indices to bring to front - this respects Canvas hierarchy and viewport masking
        if (card.bigCardVisual != null)
        {
            card.bigCardVisual.transform.SetAsLastSibling();
        }

        if (card.cardVisual != null)
        {
            card.cardVisual.transform.SetAsLastSibling();
        }
    }

    private void DisableLayoutGroups()
    {
        // Disable any automatic layout components for free-form placement
        var layoutGroup = GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        var verticalLayoutGroup = GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
            verticalLayoutGroup.enabled = false;

        var gridLayoutGroup = GetComponent<UnityEngine.UI.GridLayoutGroup>();
        if (gridLayoutGroup != null)
            gridLayoutGroup.enabled = false;
    }

    /// <summary>
    /// Adapt HorizontalLayoutGroup so slots expand only when we have many cards.
    /// For small hands (<= threshold), keep slots at baseline width to avoid huge raycast areas.
    /// </summary>
    private void ApplyAdaptiveLayout()
    {
        if (enableFreeFormPlacement || !enableAdaptiveSqueeze)
            return; // free-form or disabled -> keep HLG-authored behavior untouched

        var hlg = GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        if (hlg == null)
            return;

        bool showSmall = ShowsSmallCards();
        bool shouldSqueeze = enableAdaptiveSqueeze && cards.Count >= minCardsToSqueeze;

        // Let the group compress only when many cards AND we're a small-visual hand
        bool allowGroupExpand = shouldSqueeze && showSmall;
        hlg.childControlWidth = allowGroupExpand;
        hlg.childForceExpandWidth = allowGroupExpand;

        for (int i = 0; i < transform.childCount; i++)
        {
            var slot = transform.GetChild(i) as RectTransform;
            if (slot == null) continue;

            var le = slot.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null)
                le = slot.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();

            if (!shouldSqueeze && showSmall)
            {
                // Only enforce fixed width when adaptive squeeze is enabled
                le.flexibleWidth = 0f;
                float baseW = baselineSlotWidth > 0 ? baselineSlotWidth : minSlotPreferredWidth;
                le.preferredWidth = baseW;
                le.minWidth = baseW;
                if (slot.sizeDelta.x != baseW)
                    slot.sizeDelta = new Vector2(baseW, slot.sizeDelta.y);
            }
            else
            {
                // Squeezing (small-visual hand) or big-visual mats/tools
                if (shouldSqueeze && showSmall)
                {
                    // Enforce a minimum squeezed width so slots don't become too tiny
                    le.minWidth = Mathf.Max(1f, minSqueezedSlotWidth);
                    le.preferredWidth = -1f; // let layout reduce until minWidth
                    le.flexibleWidth = 1f;
                }
                else
                {
                    // Big-visual holders rely on SizeSlotForBigCard; don't constrain here
                    le.minWidth = -1f;
                    le.preferredWidth = -1f;
                    le.flexibleWidth = 1f;
                }
                // When returning from Mat, ensure we don't carry over a huge size in RT when group is expanding
                if (!showSmall)
                {
                    // Big-visual holders rely on SizeSlotForBigCard; don't force here
                }
            }
        }
    }

    public void DeleteCard(Card card)
    {
        if (card == null) return;
        

        
        DOTween.KillAll(true);
      
        if (selectedCard == card)
            selectedCard = null;
        
        if (hoveredCard == card)
            hoveredCard = null;
        
        // Remove from cards list first
        if (cards.Contains(card))
            cards.Remove(card);
        
        // Clean up visual objects that might be in different handlers
        if (card.bigCardVisual != null)
        {

            Destroy(card.bigCardVisual.gameObject);
        }
        
        if (card.cardVisual != null)
        {

            Destroy(card.cardVisual.gameObject);
        }
        
        // Destroy the card slot (parent) if it exists
        if (card.transform.parent != null)
        {

            Destroy(card.transform.parent.gameObject);
        }
        else
        {
            // If no parent slot, destroy the card directly

            Destroy(card.gameObject);
        }
        

    }

    /// <summary>
    /// Check if cards are currently being dealt
    /// </summary>
    public bool IsDealing()
    {
        return isDealing;
    }
    
    /// <summary>
    /// Check if dealing is complete and cards are ready for interaction
    /// </summary>
    public bool IsDealingComplete()
    {
        return !isDealing;
    }

    #region New Card Holder System Helper Methods

    /// <summary>
    /// Check if this holder can accept the given card type
    /// </summary>
    public bool CanAcceptCardType(CardMode cardMode)
    {
        switch (acceptedCardTypes)
        {
            case AcceptedCardTypes.Cases:
                return cardMode == CardMode.Case;
            case AcceptedCardTypes.Evidence:
                return cardMode == CardMode.Evidence;
            case AcceptedCardTypes.Books:
                return cardMode == CardMode.Book;
            case AcceptedCardTypes.Reports:
                return cardMode == CardMode.Report;
            case AcceptedCardTypes.Phones:
                return cardMode == CardMode.Phone;
            case AcceptedCardTypes.Mixed:
                return cardMode == CardMode.Evidence || cardMode == CardMode.Book || 
                       cardMode == CardMode.Report || cardMode == CardMode.Phone;
            case AcceptedCardTypes.All:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Check if this holder shows big card visuals
    /// </summary>
    public bool ShowsBigCards()
    {
        // New system primary check
        if (visualMode == VisualMode.BigCards) return true;
        
        return false;
    }

    /// <summary>
    /// Check if this holder shows small card visuals
    /// </summary>
    public bool ShowsSmallCards()
    {
        // If it's not big-visual by new or legacy rules, it's small-visual
        return !ShowsBigCards();
    }

    /// <summary>
    /// Get the visual mode as a string for debugging
    /// </summary>
    public string GetVisualModeDescription()
    {
        return visualMode == VisualMode.SmallCards ? "Small Cards" : "Big Cards";
    }

    /// <summary>
    /// Get the accepted card types as a string for debugging
    /// </summary>
    public string GetAcceptedCardTypesDescription()
    {
        return acceptedCardTypes.ToString();
    }

    /// <summary>
    /// Get the holder purpose as a string for debugging
    /// </summary>
    public string GetPurposeDescription()
    {
        return purpose.ToString();
    }

    #endregion
}
