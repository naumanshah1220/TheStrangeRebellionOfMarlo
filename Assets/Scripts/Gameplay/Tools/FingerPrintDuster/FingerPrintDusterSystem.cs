using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Tool))]
public class FingerPrintDusterSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HorizontalCardHolder cardSlot;
    [SerializeField] private Transform brushTransform;
    [SerializeField] private Transform brushHead; // The tip/head of the brush that touches the mask
    [SerializeField] private Button brushButton;
    [SerializeField] private Transform brushDragLayer; // Optional: layer to place brush while dragging for topmost rendering
    [SerializeField] private Button scanButton; // SCAN button to unlock extra evidence
    [SerializeField] private LcdDisplayController lcdDisplay; // LCD display for retro messaging
    
    [Header("Flip Switch Settings")]
    [SerializeField] private Button flipSwitchButton;
    [SerializeField] private UnityEngine.Rendering.Universal.Light2D flipSwitchLight;
    [SerializeField] private Sprite flipSwitchOffSprite;
    [SerializeField] private Sprite flipSwitchOnSprite;
    [SerializeField] private float lightOffValue = 0f;
    [SerializeField] private float lightOnValue = 1f;
    [SerializeField] private float lightTransitionDuration = 0.3f;
    
    [Header("Brush Settings")]
    [SerializeField] private float brushSnapBackDuration = 0.3f;
    [SerializeField] private DG.Tweening.Ease snapBackEase = DG.Tweening.Ease.OutBack;
    [SerializeField] private Vector3 brushOriginalPosition = Vector3.zero;
    [SerializeField] private Vector3 brushOriginalRotation = Vector3.zero;
    
    [Header("Brush Settings")]
    [SerializeField] private float brushRadius = 30f;
    [SerializeField] [Range(0.1f, 5f)] private float brushSensitivity = 1f; // How responsive the brush is to mouse movement (higher = more responsive)
    [SerializeField] [Range(0f, 1f)] private float powderFlow = 1f; // How much powder is applied per stroke
    [SerializeField] [Range(0f, 1f)] private float brushHardness = 0.5f; // 0 = soft edges, 1 = hard edges
    
    [Header("Powder Material Settings")]
    [SerializeField] private Color powderTintColor = Color.white;
    [SerializeField] private Color powderEmissionColor = Color.black;
    
    [Header("Fingerprint Material Settings")]
    [SerializeField] private Color fingerprintTintColor = Color.white;
    [SerializeField] private Color fingerprintEmissionColor = Color.black;

    [Header("Fingerprint Glow Settings")]
    [SerializeField] private float fingerprintGlowIntensity = 10f; // Emission intensity when light is off
    [SerializeField] private float fingerprintNormalIntensity = 1f; // Normal emission intensity when light is on

    [Header("Powder Particle Settings")]
    [SerializeField] private Color powderParticleColor1 = Color.white;
    [SerializeField] private Color powderParticleColor2 = Color.white;
    [SerializeField] private Color powderParticleEmissionColor = Color.white;
    [SerializeField] [Range(0f, 20f)] private float powderParticleEmissionIntensity = 10f; // used when light is OFF; when ON we use 0
    
    [Header("Scanning Animation Settings")]
    [SerializeField] private GameObject scanBar; // The vertical scanning bar sprite
    [SerializeField] private float scanDuration = 1.5f; // How long the scan animation takes
    [SerializeField] private DG.Tweening.Ease scanEase = DG.Tweening.Ease.InOutSine; // Easing for scan animation
    [SerializeField] private float scanBarAlpha = 0.8f; // Transparency of the scanning bar
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
        
    // Internal state
    private Tool tool;
    private Card currentCard;
    private HorizontalCardHolder sourceHand;
    private Vector3 sourcePosition;
    private bool isBrushDragging = false;
    private Vector2 brushDragOffset;
    private Fingerprint currentFingerprint;
    private Camera mainCamera;
    private Canvas parentCanvas;
    private int previousPageIndex = -1; // Track page changes to prevent LCD glitches (root controller mode)
    private int previousBigCardPageIndex = -1; // Track BigCardVisual page index for page-level Fingerprints
    
    // Brush layering state
    private Transform brushOriginalParent;
    private int brushOriginalSiblingIndex;
    
    // Flip switch state
    private bool isFlipSwitchOn = true;
    private Image flipSwitchButtonImage;
    
    // Scanning animation state
    private bool isScanning = false;
    private Vector3 scanBarOriginalPosition;
    private Vector3 scanBarEndPosition;
    private Image scanBarImage;
    private RectTransform scanBarRectTransform;
    
    // Public accessor for current card
    public Card CurrentCard => currentCard;
    
    // Public accessor for card slot
    public HorizontalCardHolder CardSlot => cardSlot;

    // Exposed getters for other systems
    public Color GetPowderParticleColor1() => powderParticleColor1;
    public Color GetPowderParticleColor2() => powderParticleColor2;
    public Color GetPowderParticleEmissionColor() => powderParticleEmissionColor;
    public float GetPowderParticleEmissionIntensity() => powderParticleEmissionIntensity;
    
    private void Start()
    {
        tool = GetComponent<Tool>();
        mainCamera = Camera.main;
        parentCanvas = GetComponentInParent<Canvas>();
        
        if (cardSlot == null || brushTransform == null || tool == null)
        {
            Debug.LogError("FingerPrintDusterSystem: Missing required references!");
            enabled = false;
            return;
        }
    
        
        // Ensure card slot is configured as FingerPrintDuster type, Evidence mode
        if (cardSlot != null)
        {
            cardSlot.purpose = HolderPurpose.FingerPrintDuster;
            cardSlot.acceptedCardTypes = AcceptedCardTypes.Evidence;
        }
        
        // Set up brush button
        if (brushButton != null)
        {
            brushButton.onClick.AddListener(OnBrushClicked);
        }

        // Set up SCAN button - always show but start disabled
        if (scanButton != null)
        {
            scanButton.onClick.AddListener(OnScanClicked);
            scanButton.gameObject.SetActive(true);
            scanButton.interactable = false; // Start disabled
        }
        
        // Set up flip switch button
        if (flipSwitchButton != null)
        {
            flipSwitchButton.onClick.AddListener(OnFlipSwitchClicked);
            flipSwitchButtonImage = flipSwitchButton.GetComponent<Image>();
            
            // Initialize flip switch state
            UpdateFlipSwitchVisuals();
        }
        
        // Set up brush drag events
        if (brushTransform != null)
        {
            var brushEventTrigger = brushTransform.gameObject.GetComponent<EventTrigger>();
            if (brushEventTrigger == null)
            {
                brushEventTrigger = brushTransform.gameObject.AddComponent<EventTrigger>();
            }
            
            // Add begin drag event
            EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener((data) => { OnBrushBeginDrag((PointerEventData)data); });
            brushEventTrigger.triggers.Add(beginDragEntry);
            
            // Add drag event
            EventTrigger.Entry dragEntry = new EventTrigger.Entry();
            dragEntry.eventID = EventTriggerType.Drag;
            dragEntry.callback.AddListener((data) => { OnBrushDrag((PointerEventData)data); });
            brushEventTrigger.triggers.Add(dragEntry);
            
            // Add end drag event
            EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener((data) => { OnBrushEndDrag((PointerEventData)data); });
            brushEventTrigger.triggers.Add(endDragEntry);
        }
        
        // Store original brush position
        if (brushTransform != null)
        {
            brushOriginalPosition = brushTransform.localPosition;
            brushOriginalRotation = brushTransform.localEulerAngles;
        }
        

        
        // Initialize LCD display
        Debug.Log($"[FingerPrintDusterSystem] LCD Display assigned: {(lcdDisplay != null ? "YES" : "NO")}");
        InitializeLcdDisplay();
        
        // Initialize scan bar
        InitializeScanBar();
        
        // Set initial LCD state
        Debug.Log("[FingerPrintDusterSystem] Setting initial LCD state...");
        UpdateLcdState();
    }
    
    #region Card Handling
    
    /// <summary>
    /// Check if the duster can accept a specific card
    /// </summary>
    public bool CanAcceptCard(Card card)
    {
        try
        {
            // Must be evidence card
            if (card.mode != CardMode.Evidence) 
            {
                return false;
            }
            
            // Check if card slot is assigned
            if (cardSlot == null)
            {
                Debug.LogError($"[FingerPrintDusterSystem] cardSlot is null! Please assign it in the inspector.");
                return false;
            }
            
            // Check if this is a torn card with EnhancedCardVisual
            EnhancedCardVisual enhancedCard = null;
            if (card.bigCardVisual != null)
            {
                enhancedCard = card.bigCardVisual.GetComponent<EnhancedCardVisual>();
            }
            
            bool isTornCard = enhancedCard != null && enhancedCard.cardType == EnhancedCardType.Torn;
            
            // For torn cards, allow them even if duster has cards (they'll create multiple pieces)
            if (isTornCard)
            {
                return true;
            }
            
            // For normal cards, check if card slot already has a card
            if (cardSlot.Cards.Count > 0)
            {
                return false; // Already has a card
            }
            
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FingerPrintDusterSystem] Exception in CanAcceptCard: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// Handle card dropped on duster
    /// </summary>
    public void HandleCardDropped(Card card)
    {
        // Check if this is a torn card with EnhancedCardVisual
        EnhancedCardVisual enhancedCard = null;
        if (card.bigCardVisual != null)
        {
            enhancedCard = card.bigCardVisual.GetComponent<EnhancedCardVisual>();
        }
        
        bool isTornCard = enhancedCard != null && enhancedCard.cardType == EnhancedCardType.Torn;
        
        if (isTornCard)
        {
            HandleTornCardDropped(card, enhancedCard);
            return;
        }
        
        // Normal card handling
        if (!CanAcceptCard(card)) 
        {
            return;
        }
        
        if (cardSlot == null)
        {
            Debug.LogError("[FingerPrintDusterSystem] cardSlot is null! Please assign it in the inspector.");
            return;
        }
        
        // Store source hand and position for removal
        sourceHand = card.parentHolder;
        
        // Store the cardSlot's original position (parent transform)
        if (card.transform.parent != null)
        {
            sourcePosition = card.transform.parent.localPosition;
        }
        else
        {
            sourcePosition = card.transform.localPosition;
        }
        
        // Remove from current hand
        if (card.parentHolder != null)
        {
            card.parentHolder.RemoveCard(card);
        }
        
        // Add to card slot (FingerPrintDuster type, Evidence mode)  
        if (cardSlot != null)
        {
            cardSlot.AddCardToHand(card);
            
            // Force visual update for duster (should show bigCardVisual like a Mat)
            if (card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
            {
                card.cardLocation = CardLocation.Mat; // Duster acts like Mat - shows bigCardVisual
            }
        }
        
        // Store references
        currentCard = card;
        
        // Initialize page tracking
        previousPageIndex = -1; // Reset page tracking for new card
        
        // Update LCD state when card is loaded
        Debug.Log($"[FingerPrintDusterSystem] Card loaded: {card.name}, isBrushDragging: {isBrushDragging}");
        UpdateLcdState();
        
        // Update scan button state when card is loaded
        UpdateScanButtonState();
        
        // Find Fingerprint component on the card's bigCardVisual
        if (card.bigCardVisual != null)
        {
            currentFingerprint = card.bigCardVisual.GetComponent<Fingerprint>();
            if (currentFingerprint != null)
            {
                AttachAndInitializeFingerprint(currentFingerprint);
            }
            else
            {
                // Fallback: use the active page's Fingerprint component (per-page mode)
                AttachFingerprintForActivePage(card.bigCardVisual);
            }
        }
        
        // Progress will be shown by LCD when brushing actually starts
    }
    
    /// <summary>
    /// Handle torn card dropped on duster - create pieces in duster's cardSlot
    /// </summary>
    private void HandleTornCardDropped(Card card, EnhancedCardVisual enhancedCard)
    {
        if (cardSlot == null)
        {
            Debug.LogError("[FingerPrintDusterSystem] cardSlot is null! Please assign it in the inspector.");
            return;
        }
        
        // Check if pieces already exist (from previous dragging)
        if (enhancedCard.activePieceCards.Count > 0)
        {
            MoveExistingPiecesToDuster(card, enhancedCard);
            return;
        }
        
        // Store source hand and position for removal
        sourceHand = card.parentHolder;
        
        // Store the cardSlot's original position (parent transform)
        if (card.transform.parent != null)
        {
            sourcePosition = card.transform.parent.localPosition;
        }
        else
        {
            sourcePosition = card.transform.localPosition;
        }
        
        // Remove from current hand
        if (card.parentHolder != null)
        {
            card.parentHolder.RemoveCard(card);
        }
        
        // Temporarily allow multiple cards in the duster for torn pieces
        // We'll create pieces and add them to the duster's cardSlot
        StartCoroutine(CreateTornPiecesInDuster(card, enhancedCard));
    }
    
    /// <summary>
    /// Create torn pieces in the duster's cardSlot instead of matHand
    /// </summary>
    private IEnumerator CreateTornPiecesInDuster(Card originalCard, EnhancedCardVisual enhancedCard)
    {
        // Check if puzzle has been completed before
        if (enhancedCard.isPuzzleCompleted)
        {
            // Just add to duster normally, don't break into pieces
            cardSlot.AddCardToHand(originalCard);
            currentCard = originalCard;
            yield break;
        }
        
        // Ensure neighbor data is calculated before creating pieces
        if (enhancedCard.PieceGridPositions.Count == 0 || enhancedCard.PieceNeighbors.Count == 0)
        {
            enhancedCard.CalculatePieceNeighbors();
        }
        
        // Get the drop position (use the duster's center position)
        Vector2 dropPosition = cardSlot.transform.position;
        
        // Create a randomized list of piece indices to randomize the order
        List<int> randomizedIndices = new List<int>();
        for (int i = 0; i < enhancedCard.piecePrefabs.Count; i++)
        {
            randomizedIndices.Add(i);
        }
        
        // Shuffle the indices to randomize piece order
        for (int i = 0; i < randomizedIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, randomizedIndices.Count);
            int temp = randomizedIndices[i];
            randomizedIndices[i] = randomizedIndices[randomIndex];
            randomizedIndices[randomIndex] = temp;
        }
        
        // Create pieces and add them to the duster's cardSlot in randomized order
        for (int i = 0; i < enhancedCard.piecePrefabs.Count; i++)
        {
            int pieceIndex = randomizedIndices[i];
            yield return StartCoroutine(CreatePieceCardInDuster(pieceIndex, dropPosition, enhancedCard));
        }
        
        // Hide original card after creation
        originalCard.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Create a single piece card in the duster's cardSlot
    /// </summary>
    private IEnumerator CreatePieceCardInDuster(int pieceIndex, Vector2 centerPosition, EnhancedCardVisual enhancedCard)
    {
        // STEP 1: Create slot (same as HorizontalCardHolder does)
        GameObject slot = Instantiate(cardSlot.slotPrefab, cardSlot.transform);
        slot.transform.localPosition = Vector3.zero;
        
        // STEP 2: Get the Card from the slot
        Card pieceCard = slot.GetComponentInChildren<Card>();
        if (pieceCard == null)
        {
            Debug.LogError($"[FingerPrintDusterSystem] No Card found in slot prefab for piece {pieceIndex}");
            Destroy(slot);
            yield break;
        }
        
        // STEP 3: Setup basic card properties
        pieceCard.name = $"{enhancedCard.originalCard.name}_Piece_{pieceIndex}_Name{pieceIndex+1}"; // Index 0-8, Name 1-9
        pieceCard.parentHolder = cardSlot;
        
        // Add event listeners (same as HorizontalCardHolder does)
        pieceCard.PointerEnterEvent.AddListener(cardSlot.CardPointerEnter);
        pieceCard.PointerExitEvent.AddListener(cardSlot.CardPointerExit);
        pieceCard.BeginDragEvent.AddListener(cardSlot.BeginDrag);
        pieceCard.EndDragEvent.AddListener(cardSlot.EndDrag);
        
        // STEP 4: Create piece data for initialization
        PuzzlePieceData pieceData = new PuzzlePieceData
        {
            pieceIndex = pieceIndex,
            originalCard = enhancedCard.originalCard,
            smallSprite = enhancedCard.small_PieceCard, // Use the new piece-specific sprite
            bigSprite = null, // Will be set via BigCardVisual prefab
            cardMode = enhancedCard.originalCard.mode
        };
        
        // STEP 5: Initialize the card with piece data and visual handlers
        pieceCard.Initialize(pieceData, cardSlot.visualHandler, cardSlot.bigVisualHandler != null ? cardSlot.bigVisualHandler : cardSlot.visualHandler);
        
        // STEP 5.5: Explicitly set the cardVisual sprite (in case initialization didn't apply it)
        if (pieceCard.cardVisual != null && enhancedCard.small_PieceCard != null)
        {
            var cardImage = pieceCard.cardVisual.GetCardImage();
            if (cardImage != null)
            {
                cardImage.sprite = enhancedCard.small_PieceCard;
            }
            else
            {
                Debug.LogWarning($"[FingerPrintDusterSystem] Could not get cardImage for piece {pieceIndex}");
            }
        }
        else if (enhancedCard.small_PieceCard == null)
        {
            Debug.LogWarning($"[FingerPrintDusterSystem] small_PieceCard is null! Please assign it in the inspector.");
        }
        
        // STEP 6: Create BigCardVisual from assigned prefab
        if (pieceCard.bigCardVisual == null && pieceIndex < enhancedCard.piecePrefabs.Count && enhancedCard.piecePrefabs[pieceIndex] != null)
        {
            // Use the assigned prefab for this piece
            GameObject bigCardPrefab = enhancedCard.piecePrefabs[pieceIndex];
            
            // Create the BigCardVisual instance
            Transform bigVisualHandler = cardSlot.bigVisualHandler != null ? cardSlot.bigVisualHandler : cardSlot.visualHandler;
            GameObject bigCardInstance = Instantiate(bigCardPrefab, bigVisualHandler);
            pieceCard.bigCardVisual = bigCardInstance.GetComponent<BigCardVisual>();
            
            if (pieceCard.bigCardVisual != null)
            {
                pieceCard.bigCardVisual.ShowCard(false);
                bigCardInstance.transform.localRotation = Quaternion.identity;
                bigCardInstance.transform.localScale = Vector3.one;
                
                pieceCard.bigCardVisual.Initialize(pieceCard);
            }
            else
            {
                Debug.LogError($"[FingerPrintDusterSystem] Prefab {bigCardPrefab.name} does not have a BigCardVisual component!");
            }
        }
        else if (pieceIndex >= enhancedCard.piecePrefabs.Count || enhancedCard.piecePrefabs[pieceIndex] == null)
        {
            Debug.LogError($"[FingerPrintDusterSystem] No prefab assigned for piece {pieceIndex}! Please assign BigCardVisual prefabs in the piecePrefabs list.");
        }
        
        // STEP 7: Size the slot for big card (same as HorizontalCardHolder does for mat)
        if (cardSlot.purpose == HolderPurpose.FingerPrintDuster)
        {
            // Size slot appropriately
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect != null && pieceCard.bigCardVisual != null)
            {
                RectTransform bigCardRect = pieceCard.bigCardVisual.GetComponent<RectTransform>();
                if (bigCardRect != null)
                {
                    slotRect.sizeDelta = bigCardRect.sizeDelta;
                }
            }
        }
        
        // STEP 8: Get or add EnhancedCardVisual component to the bigCardVisual (not the card)
        if (pieceCard.bigCardVisual != null)
        {
            // Check if EnhancedCardVisual already exists (prefab might have it)
            EnhancedCardVisual pieceEnhanced = pieceCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
            if (pieceEnhanced == null)
            {
                // Add component if it doesn't exist
                pieceEnhanced = pieceCard.bigCardVisual.gameObject.AddComponent<EnhancedCardVisual>();
            }
            
            // Configure the component
            pieceEnhanced.cardType = EnhancedCardType.Piece;
            pieceEnhanced.originalCard = enhancedCard.originalCard; // Reference back to original
            pieceEnhanced.puzzleSettings = enhancedCard.puzzleSettings;
            
            // Setup piece-specific data (ensure dictionaries exist)
            if (enhancedCard.PieceGridPositions.ContainsKey(pieceIndex) && enhancedCard.PieceNeighbors.ContainsKey(pieceIndex))
            {
                pieceEnhanced.SetupAsPiece(pieceIndex, enhancedCard.PieceGridPositions[pieceIndex], enhancedCard.PieceNeighbors[pieceIndex]);
            }
        }
        
        // STEP 9: Position piece at scattered location within duster area
        // Get scattered position in world space using normalized method
        Vector2 scatteredWorldPosition = GetScatteredPositionInDuster(centerPosition, pieceIndex);

        // Convert to screen space for cardSlot
        Vector2 scatteredScreenPosition = Camera.main.WorldToScreenPoint(scatteredWorldPosition);
        
        // Add to cardSlot at specific position using screen coordinates
        cardSlot.AddCardToHandAtPosition(pieceCard, scatteredScreenPosition);
        
        // Add to enhanced card's tracking list
        enhancedCard.activePieceCards.Add(pieceCard);
        
        // Add our own listener for when dragging ends to check piece position
        pieceCard.EndDragEvent.AddListener(OnTornPieceDropped);
        
        yield return new WaitForEndOfFrame();
    }
    
    /// <summary>
    /// Get scattered position within the duster area for piece placement
    /// </summary>
    private Vector2 GetScatteredPositionInDuster(Vector2 center, int pieceIndex)
    {
        // Generate random position within unit circle (0-1 radius)
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float radius = Random.Range(0.2f, 1f); // Minimum 0.2 to avoid clustering at center
        
        // Convert to world units (adjust multiplier based on your duster size)
        float scatterRadius = 100f; // Smaller radius for duster area
        
        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * radius * scatterRadius,
            Mathf.Sin(angle) * radius * scatterRadius
        );
        
        Vector2 scatteredPos = center + offset;
        
        return scatteredPos;
    }
    
    /// <summary>
    /// Called when a torn piece is dropped in the duster - checks position and puzzle completion
    /// </summary>
    private void OnTornPieceDropped(Card droppedPiece)
    {
        // Find the EnhancedCardVisual component on the dropped piece's BigCardVisual
        if (droppedPiece?.bigCardVisual != null)
        {
            var pieceEnhanced = droppedPiece.bigCardVisual.GetComponent<EnhancedCardVisual>();
            if (pieceEnhanced != null && pieceEnhanced.cardType == EnhancedCardType.Piece)
            {
                // Check this piece's position
                pieceEnhanced.CheckPiecePosition();
                
                // Check if puzzle is complete (call on original card)
                if (pieceEnhanced.originalCard?.bigCardVisual != null)
                {
                    var originalEnhanced = pieceEnhanced.originalCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
                    if (originalEnhanced != null)
                    {
                        // Small delay to ensure all position updates are complete
                        originalEnhanced.StartCoroutine(originalEnhanced.DelayedPuzzleCompletionCheck());
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Handle card hovering over duster
    /// </summary>
    public void HandleCardHoverStart(Card card)
    {
        if (CanAcceptCard(card))
        {
            // Add visual feedback here (e.g., highlight duster border)
        }
    }
    
    /// <summary>
    /// Handle card stopping hover over duster
    /// </summary>
    public void HandleCardHoverEnd(Card card)
    {
        // Remove visual feedback here
    }
    
    /// <summary>
    /// Monitor the card slot for changes (in case cards are manually moved)
    /// </summary>
    private void MonitorCardSlot()
    {
        if (cardSlot == null) return;
        
        // Check if card was removed externally
        if (currentCard != null && !cardSlot.Cards.Contains(currentCard))
        {
            // Card was removed externally - clean up
            OnCardRemoved();
        }
        
        // Update currentCard reference if it's null but we have cards in the slot
        if (currentCard == null && cardSlot.Cards.Count > 0)
        {
            currentCard = cardSlot.Cards[0];
            
            // Find Fingerprint component on the card's bigCardVisual
            if (currentCard.bigCardVisual != null)
            {
                currentFingerprint = currentCard.bigCardVisual.GetComponent<Fingerprint>();
                if (currentFingerprint != null)
                {
                    AttachAndInitializeFingerprint(currentFingerprint);
                }
                else
                {
                    // Fallback: use the active page's Fingerprint component (per-page mode)
                    AttachFingerprintForActivePage(currentCard.bigCardVisual);
                }
            }
            

            
            // Update progress squares with current page progress immediately
            if (currentFingerprint != null)
            {
                float currentProgress = currentFingerprint.CurrentPageRevealPercentage;
                UpdateProgressDisplay(currentProgress);
            }
            
            // Update progress squares with current page progress (after material states are restored)
            if (currentFingerprint != null)
            {
                // Use a coroutine to delay the progress update to ensure material states are restored
                StartCoroutine(DelayedProgressUpdate());
            }
        }
        
        // Detect page flips and switch per-page Fingerprint if needed
        if (currentCard != null && currentCard.bigCardVisual != null)
        {
            int bigIndex = currentCard.bigCardVisual.currentPageIndex;
            if (bigIndex != previousBigCardPageIndex)
            {
                previousBigCardPageIndex = bigIndex;
                // If using per-page Fingerprints (no root controller), switch the currentFingerprint
                if (currentCard.bigCardVisual.GetComponent<Fingerprint>() == null)
                {
                    AttachFingerprintForActivePage(currentCard.bigCardVisual);
                }
                // Update LCD immediately on page change
                UpdateLcdStateForce();
            }
        }

        // Enforce single card rule for FingerPrintDuster type hands (but allow torn pieces)
        if (cardSlot.Cards.Count > 1)
        {
            // Check if any of the cards are torn pieces
            bool hasTornPieces = false;
            foreach (var card in cardSlot.Cards)
            {
                if (card?.bigCardVisual != null)
                {
                    var enhancedCard = card.bigCardVisual.GetComponent<EnhancedCardVisual>();
                    if (enhancedCard != null && enhancedCard.cardType == EnhancedCardType.Piece)
                    {
                        hasTornPieces = true;
                        break;
                    }
                }
            }
            
            if (hasTornPieces)
            {
                // Don't remove torn pieces, they belong together
            }
            else
            {
                Debug.LogWarning("[FingerPrintDusterSystem] Multiple cards detected - removing extras");
                
                // Keep only the first card, remove others
                for (int i = cardSlot.Cards.Count - 1; i > 0; i--)
                {
                    Card extraCard = cardSlot.Cards[i];
                    cardSlot.RemoveCard(extraCard);
                    
                    // Return to appropriate hand
                    if (CardTypeManager.Instance != null)
                    {
                        CardTypeManager.Instance.AddCardToAppropriateHand(extraCard);
                    }
                }
            }
        }
        
        // Enforce evidence-only rule for FingerPrintDuster type hands
        foreach (Card card in cardSlot.Cards.ToArray()) // ToArray to avoid modification during iteration
        {
            if (card.mode != CardMode.Evidence)
            {
                Debug.LogWarning($"[FingerPrintDusterSystem] Non-evidence card '{card.name}' detected in duster slot - removing");
                cardSlot.RemoveCard(card);
                
                // Return to appropriate hand
                if (CardTypeManager.Instance != null)
                {
                    CardTypeManager.Instance.AddCardToAppropriateHand(card);
                }
            }
        }
    }

    private void AttachAndInitializeFingerprint(Fingerprint fp)
    {
        // Detach previous if different
        if (currentFingerprint != null && currentFingerprint != fp)
        {
            currentFingerprint.OnCardRemovedFromDuster();
            currentFingerprint.OnProgressChanged -= OnFingerprintProgressChanged;
            currentFingerprint.OnPageFingerprintCompleted -= OnPageFingerprintCompleted;
            currentFingerprint.OnAllFingerprintsCompleted -= OnAllFingerprintsCompleted;
        }

        currentFingerprint = fp;
        // Subscribe
        currentFingerprint.OnProgressChanged += OnFingerprintProgressChanged;
        currentFingerprint.OnPageFingerprintCompleted += OnPageFingerprintCompleted;
        currentFingerprint.OnAllFingerprintsCompleted += OnAllFingerprintsCompleted;

        // Notify load
        currentFingerprint.OnCardLoadedOnDuster();

        // Restore materials and apply settings
        RestoreFingerprintMaterialStates();
        ApplyBrushSettingsToFingerprint();

        // Apply light/particle state
        ApplyFingerprintLightState(
            fingerprintTintColor,
            fingerprintEmissionColor,
            isFlipSwitchOn ? fingerprintNormalIntensity : fingerprintGlowIntensity
        );
        currentFingerprint.SetParticleEmissionIntensity(isFlipSwitchOn ? 0f : powderParticleEmissionIntensity);
    }

    private void AttachFingerprintForActivePage(BigCardVisual big)
    {
        var activePage = big != null ? big.GetActivePageObject() : null;
        var pageFingerprint = activePage != null ? activePage.GetComponentInChildren<Fingerprint>(true) : null;
        if (pageFingerprint == null)
        {
            // If no page-level fingerprint, clear current so brush won't try to paint
            currentFingerprint = null;
            return;
        }
        AttachAndInitializeFingerprint(pageFingerprint);
    }
    
    /// <summary>
    /// Handle card removal cleanup
    /// </summary>
    private void OnCardRemoved()
    {
        // Unsubscribe from fingerprint events
        if (currentFingerprint != null)
        {
            // Notify that card is removed from duster
            currentFingerprint.OnCardRemovedFromDuster();
            
            currentFingerprint.OnProgressChanged -= OnFingerprintProgressChanged;
            currentFingerprint.OnPageFingerprintCompleted -= OnPageFingerprintCompleted;
            currentFingerprint.OnAllFingerprintsCompleted -= OnAllFingerprintsCompleted;
            
            // Page changes are handled automatically, no unsubscription needed
            
            currentFingerprint = null;
        }
        

        

        
        currentCard = null;
        sourceHand = null;
        sourcePosition = Vector3.zero;
        previousPageIndex = -1; // Reset page tracking
        
        // Update LCD state when card is removed
        UpdateLcdState();
        
        // Update scan button state when card is removed
        UpdateScanButtonState();
        
    }
    
    /// <summary>
    /// Remove the current card and return it to source hand
    /// </summary>
    public void RemoveCard()
    {
        if (currentCard == null) 
        {
            return;
        }
        
        // Restore material states before removing the card
        RestoreFingerprintMaterialStates();
        
        // Remove from card slot
        if (cardSlot != null)
        {
            cardSlot.RemoveCard(currentCard);
        }
        
        // Return to source hand
        if (sourceHand != null)
        {
            // Special handling based on source hand type
            if (sourceHand.purpose == HolderPurpose.Mat && sourceHand.enableFreeFormPlacement)
            {
                // Add card back to hand first
                sourceHand.AddCardToHand(currentCard);
                
                // Then restore original position
                Transform returnedCardSlot = currentCard.transform.parent;
                if (returnedCardSlot != null)
                {
                    returnedCardSlot.localPosition = sourcePosition;
                }
                else
                {
                    currentCard.transform.localPosition = sourcePosition;
                }
            }
            else
            {
                // For regular hands (evidenceHand, etc.), use standard add
                sourceHand.AddCardToHand(currentCard);
            }
            
            // Force visual update based on new parent holder type
            if (currentCard.parentHolder != null)
            {
                // Set appropriate card location based on holder type to trigger visual switch
                switch (currentCard.parentHolder.purpose)
                {
                    case HolderPurpose.Hand:
                        currentCard.cardLocation = CardLocation.Hand; // Shows cardVisual
                        break;
                    case HolderPurpose.Mat:
                        currentCard.cardLocation = CardLocation.Mat; // Shows bigCardVisual
                        break;
                    case HolderPurpose.Computer:
                        currentCard.cardLocation = CardLocation.Mat; // Computer acts like Mat
                        break;
                    case HolderPurpose.FingerPrintDuster:
                        currentCard.cardLocation = CardLocation.Mat; // Duster acts like Mat
                        break;
                }
            }
        }
        else if (CardTypeManager.Instance != null)
        {
            // Fallback: return to appropriate hand
            CardTypeManager.Instance.AddCardToAppropriateHand(currentCard);
            
            // Force visual update for fallback case too
            if (currentCard.parentHolder != null)
            {
                switch (currentCard.parentHolder.purpose)
                {
                    case HolderPurpose.Hand:
                        currentCard.cardLocation = CardLocation.Hand;
                        break;
                    case HolderPurpose.Mat:
                        currentCard.cardLocation = CardLocation.Mat;
                        break;
                    case HolderPurpose.Computer:
                        currentCard.cardLocation = CardLocation.Mat;
                        break;
                    case HolderPurpose.FingerPrintDuster:
                        currentCard.cardLocation = CardLocation.Mat;
                        break;
                }
            }
        }
        
        // Clean up
        OnCardRemoved();
    }
    
    #endregion
    
    #region Material State Management
    
    /// <summary>
    /// Restore material states for all fingerprint brushes on the current card
    /// This ensures the reveal effects persist when cards move between holders
    /// </summary>
    private void RestoreFingerprintMaterialStates()
    {
        if (currentFingerprint == null) return;
        
        // Use the public method from Fingerprint class
        currentFingerprint.RestoreAllMaterialStates();
        
    }
    
    /// <summary>
    /// Apply current brush settings to all fingerprint brushes on the current card
    /// </summary>
    private void ApplyBrushSettingsToFingerprint()
    {
        if (currentFingerprint == null) return;
        
        // Apply brush settings to all pages
        currentFingerprint.ApplyBrushSettings(brushRadius, powderTintColor, powderEmissionColor, powderFlow, brushHardness);
        
    }
    
    /// <summary>
    /// Apply brush settings immediately to the current fingerprint
    /// This allows real-time changes without reloading the card
    /// </summary>
    private void ApplyBrushSettingsImmediately()
    {
        if (currentFingerprint == null) return;
        
        // Apply brush settings to all pages immediately
        currentFingerprint.ApplyBrushSettings(brushRadius, powderTintColor, powderEmissionColor, powderFlow, brushHardness);
    }
    
    /// <summary>
    /// Set brush radius and apply to current fingerprint
    /// </summary>
    public void SetBrushRadius(float radius)
    {
        brushRadius = Mathf.Max(1f, radius);
        ApplyBrushSettingsImmediately();
    }
    
    /// <summary>
    /// Set powder tint color and apply to current fingerprint
    /// </summary>
    public void SetPowderTintColor(Color color)
    {
        powderTintColor = color;
        ApplyBrushSettingsImmediately();
    }
    
    /// <summary>
    /// Set powder emission color and apply to current fingerprint
    /// </summary>
    public void SetPowderEmissionColor(Color color)
    {
        powderEmissionColor = color;
        ApplyBrushSettingsImmediately();
    }
    
    /// <summary>
    /// Set powder flow and apply to current fingerprint
    /// </summary>
    public void SetPowderFlow(float flow)
    {
        powderFlow = Mathf.Clamp01(flow);
        ApplyBrushSettingsImmediately();
    }
    
    /// <summary>
    /// Set brush hardness and apply to current fingerprint
    /// </summary>
    public void SetBrushHardness(float hardness)
    {
        brushHardness = Mathf.Clamp01(hardness);
        ApplyBrushSettingsImmediately();
    }
    
    /// <summary>
    /// Set brush sensitivity for mouse movement responsiveness
    /// </summary>
    public void SetBrushSensitivity(float sensitivity)
    {
        brushSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
    }
    
    #region Brush Settings Getters
    
    /// <summary>
    /// Get current brush radius
    /// </summary>
    public float GetBrushRadius() => brushRadius;
    
    /// <summary>
    /// Get current powder tint color
    /// </summary>
    public Color GetPowderTintColor() => powderTintColor;
    
    /// <summary>
    /// Get current powder emission color
    /// </summary>
    public Color GetPowderEmissionColor() => powderEmissionColor;
    
    /// <summary>
    /// Get current powder flow
    /// </summary>
    public float GetPowderFlow() => powderFlow;
    
    /// <summary>
    /// Get current brush hardness
    /// </summary>
    public float GetBrushHardness() => brushHardness;
    
    /// <summary>
    /// Get current brush sensitivity
    /// </summary>
    public float GetBrushSensitivity() => brushSensitivity;
    
    /// <summary>
    /// Get current fingerprint tint color
    /// </summary>
    public Color GetFingerprintTintColor() => fingerprintTintColor;
    
    /// <summary>
    /// Get current fingerprint emission color
    /// </summary>
    public Color GetFingerprintEmissionColor() => fingerprintEmissionColor;
    
    #endregion
    
    #endregion
    
    #region Fingerprint Event Handlers
    
    /// <summary>
    /// Called when fingerprint progress changes
    /// </summary>
    private void OnFingerprintProgressChanged(float progress)
    {
        // Check if this is a page change (progress change due to page switch)
        bool isPageChange = false;
        if (currentFingerprint != null)
        {
            int currentPageIndex = currentFingerprint.CurrentPageIndex;
            if (currentPageIndex != previousPageIndex)
            {
                isPageChange = true;
                previousPageIndex = currentPageIndex;
                Debug.Log($"[FingerPrintDusterSystem] Page change detected: {previousPageIndex} -> {currentPageIndex}");
            }
        }
        
        // Update progress display (LCD or traditional squares)
        UpdateProgressDisplay(progress);

        // Update LCD state (use ForceSetState if page changed to prevent glitches)
        if (isPageChange)
        {
            UpdateLcdStateForce();
        }
        else
        {
            UpdateLcdState();
        }
        
        // Legacy scan button state (replaced by LCD-based logic)
        UpdateScanButtonState();
    }
    
    /// <summary>
    /// Called when a page's fingerprint is completed
    /// </summary>
    private void OnPageFingerprintCompleted(int pageNumber)
    {
        // Optional: Add completion effects here for individual pages
        // - Play sound
        // - Show page completion message
        // - etc.
        UpdateScanButtonState();
    }
    
    /// <summary>
    /// Called when all fingerprints are completed
    /// </summary>
    private void OnAllFingerprintsCompleted()
    {
        // Optional: Add completion effects here for all pages
        // - Play special sound
        // - Show completion message
        // - Highlight the card
        // - Mark evidence as fully processed
        // - etc.
        UpdateScanButtonState();
    }

    private void UpdateScanButtonState()
    {
        if (scanButton == null) return;
        
        // Always show the scan button when a card is loaded
        bool shouldShow = currentCard != null;
        scanButton.gameObject.SetActive(shouldShow);
        
        if (!shouldShow) return;

        // Get current page info
        var page = currentFingerprint?.GetCurrentPage();
        float progressPercent = currentFingerprint?.CurrentPageRevealPercentage ?? 0f;
        bool alreadyScanned = page?.extraEvidenceUnlocked ?? false;

        // Use the new CheckProgressComplete logic instead of hardcoded threshold
        bool thresholdReached = CheckProgressComplete(progressPercent);

        // Update label and interactable
        var tmp = scanButton.GetComponentInChildren<TMP_Text>(true);
        var uiText = tmp == null ? scanButton.GetComponentInChildren<UnityEngine.UI.Text>(true) : null;

        if (alreadyScanned)
        {
            if (tmp != null) tmp.text = "Scanned"; else if (uiText != null) uiText.text = "Scanned";
            scanButton.interactable = false;
        }
        else if (thresholdReached)
        {
            if (tmp != null) tmp.text = "Scan"; else if (uiText != null) uiText.text = "Scan";
            scanButton.interactable = true;
        }
        else
        {
            if (tmp != null) tmp.text = "Scan"; else if (uiText != null) uiText.text = "Scan";
            scanButton.interactable = false;
        }
        
        Debug.Log($"[FingerPrintDusterSystem] UpdateScanButtonState - Card loaded: {shouldShow}, Progress: {progressPercent}%, Threshold reached: {thresholdReached}, Already scanned: {alreadyScanned}");
    }

    private void OnScanClicked()
    {
        // Check if scan button should be enabled (5 squares filled)
        if (scanButton != null && !scanButton.interactable)
        {
            return; // Don't process if button shouldn't be active
        }

        // Check if already scanning
        if (isScanning)
        {
            return; // Don't allow multiple scans at once
        }

        // Start the scanning animation
        StartScanAnimation();
        
        // The actual evidence processing will happen after the animation completes
        // via the ScanAnimationSequence coroutine
    }
    
    #endregion
    
    #region Brush Interaction
    
    private void OnBrushClicked()
    {
        // Optional: Add click feedback
    }
    
    private void OnFlipSwitchClicked()
    {
        isFlipSwitchOn = !isFlipSwitchOn;
        UpdateFlipSwitchVisuals();
        // Optional: Add flip switch sound effect
    }
    
    private void OnBrushBeginDrag(PointerEventData eventData)
    {
        if (currentCard == null || currentFingerprint == null) return;
        
        isBrushDragging = true;
        
        // Notify fingerprint that brush started dragging
        currentFingerprint.OnBrushStartDrag();
        
        // Calculate drag offset
        Vector3 brushScreenPos = mainCamera.WorldToScreenPoint(brushTransform.position);
        brushDragOffset = eventData.position - (Vector2)brushScreenPos;

        // Bring brush in front of the hand (and other UI) while dragging
        if (brushTransform != null)
        {
            brushOriginalParent = brushTransform.parent;
            brushOriginalSiblingIndex = brushTransform.GetSiblingIndex();
            if (brushDragLayer != null && brushTransform.parent != brushDragLayer)
            {
                // Reparent but keep world position to avoid visual jump
                brushTransform.SetParent(brushDragLayer, true);
                brushTransform.SetAsLastSibling();
            }
            else
            {
                brushTransform.SetAsLastSibling();
            }
        }
    }
    
    private void OnBrushDrag(PointerEventData eventData)
    {
        if (!isBrushDragging || currentCard == null || currentFingerprint == null) return;

        // Update brush position with sensitivity
        Vector3 brushScreenPos = mainCamera.WorldToScreenPoint(brushTransform.position);
        Vector2 targetScreenPos2D = eventData.position - (Vector2)brushScreenPos;
        Vector3 targetScreenPos = new Vector3(eventData.position.x - brushDragOffset.x, eventData.position.y - brushDragOffset.y, brushScreenPos.z);
        Vector3 targetWorldPos = mainCamera.ScreenToWorldPoint(targetScreenPos);
        
        // Apply sensitivity to brush movement
        Vector3 currentPos = brushTransform.position;
        Vector3 newPos = Vector3.Lerp(currentPos, targetWorldPos, brushSensitivity * Time.deltaTime * 10f);
        brushTransform.position = newPos;
        
        // Check if brush head is touching any fingerprint masks
        CheckBrushHeadCollision();
    }
    
    /// <summary>
    /// Check if the brush head is touching any fingerprint masks and paint them
    /// </summary>
    private void CheckBrushHeadCollision()
    {
        if (brushHead == null || currentFingerprint == null) return;
        
        Vector3 brushHeadWorldPos = brushHead.position;
        
        // Get the current active page from the fingerprint
        var currentPage = currentFingerprint.GetCurrentPage();
        if (currentPage == null) 
        {
            return;
        }
        
        bool paintedSomething = false;
        
        // Check powder brush (now handles both powder and fingerprints)
        if (currentPage.powderBrush != null)
        {
            var powderBrushRect = currentPage.powderBrush.GetComponent<RectTransform>();
            if (powderBrushRect != null)
            {
                bool isTouching = IsPointInRectTransform(brushHeadWorldPos, powderBrushRect);
                if (isTouching)
                {
                    currentPage.powderBrush.PaintAtWorldPosition(brushHeadWorldPos);
                    paintedSomething = true;
                }
            }
        }
        
        // Update progress in real-time if we painted something
        if (paintedSomething && currentFingerprint != null)
        {
            currentFingerprint.UpdateCurrentPageProgress();
        }
    }
    
    /// <summary>
    /// Check if a world position is within a RectTransform
    /// </summary>
    private bool IsPointInRectTransform(Vector3 worldPosition, RectTransform rectTransform)
    {
        Vector2 screenPoint = mainCamera.WorldToScreenPoint(worldPosition);
        bool isInside = RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, mainCamera);
        
        return isInside;
    }
    
    private void OnBrushEndDrag(PointerEventData eventData)
    {
        if (!isBrushDragging) return;
        
        isBrushDragging = false;
        
        // Notify fingerprint that brush stopped dragging
        if (currentFingerprint != null)
        {
            currentFingerprint.OnBrushEndDrag();
            
            // Update progress for the current page's brush
            var currentPage = currentFingerprint.GetCurrentPage();
            if (currentPage != null)
            {
                if (currentPage.powderBrush != null)
                {
                    currentPage.powderBrush.UpdateProgress();
                }
            }
        }

        // Restore brush to its original layering before snapping back
        if (brushTransform != null)
        {
            if (brushOriginalParent != null && brushTransform.parent != brushOriginalParent)
            {
                brushTransform.SetParent(brushOriginalParent, true);
            }
            // Guard originalSiblingIndex
            if (brushOriginalSiblingIndex >= 0)
            {
                brushTransform.SetSiblingIndex(Mathf.Min(brushOriginalSiblingIndex, brushTransform.parent.childCount - 1));
            }
        }
        
        // Snap brush back to original position
        SnapBrushBack();
    }
    
    private void SnapBrushBack()
    {
        if (brushTransform == null) return;
        
        // Animate brush back to original position
        brushTransform.DOLocalMove(brushOriginalPosition, brushSnapBackDuration)
            .SetEase(snapBackEase);
        
        brushTransform.DOLocalRotate(brushOriginalRotation, brushSnapBackDuration)
            .SetEase(snapBackEase);
    }
    
    #endregion
    
    #region Progress Bar
    
    /// <summary>

    

    
    #endregion
    
    private void Update()
    {
        // Monitor card slot for changes
        MonitorCardSlot();
        
        // Custom hover detection (similar to ComputerSystem)
        // This ensures hover detection works even when cards are being dragged
        bool isMouseOverDuster = IsMouseOverDuster();
        
        if (tool.isHovering != isMouseOverDuster)
        {
            tool.isHovering = isMouseOverDuster;

        }
    }
    
    /// <summary>
    /// Check if mouse is over the duster area (similar to ComputerSystem.IsMouseOverDesktop)
    /// </summary>
    private bool IsMouseOverDuster()
    {
        if (tool == null) return false;
        
        Vector2 mousePosition = Input.mousePosition;
        // Use Canvas event camera if not overlay
        var canvas = GetComponentInParent<Canvas>();
        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera != null ? canvas.worldCamera : mainCamera;
        }
         
         // Check if mouse is over the duster's RectTransform
         var dusterRect = tool.GetComponent<RectTransform>();
        if (dusterRect != null && RectTransformUtility.RectangleContainsScreenPoint(dusterRect, mousePosition, eventCamera))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Coroutine to delay progress update to ensure material states are restored
    /// </summary>
    private System.Collections.IEnumerator DelayedProgressUpdate()
    {
        // Wait for end of frame to ensure all material states are restored
        yield return new WaitForEndOfFrame();
        
        if (currentFingerprint != null)
        {
            currentFingerprint.UpdateCurrentPageProgress();
        }
    }

    /// <summary>
    /// Move existing torn pieces from their current hand to the duster's cardSlot
    /// </summary>
    private void MoveExistingPiecesToDuster(Card originalCard, EnhancedCardVisual enhancedCard)
    {
        // Store source hand and position for removal
        sourceHand = originalCard.parentHolder;
        
        // Store the cardSlot's original position (parent transform)
        if (originalCard.transform.parent != null)
        {
            sourcePosition = originalCard.transform.parent.localPosition;
        }
        else
        {
            sourcePosition = originalCard.transform.localPosition;
        }
        
        // Remove original card from current hand
        if (originalCard.parentHolder != null)
        {
            originalCard.parentHolder.RemoveCard(originalCard);
        }
        
        // Hide the original card
        originalCard.gameObject.SetActive(false);
        
        // Get the duster's center position for scattering
        Vector2 dusterCenter = cardSlot.transform.position;
        
        // Create a randomized list of piece indices to randomize the order
        List<int> randomizedIndices = new List<int>();
        for (int i = 0; i < enhancedCard.activePieceCards.Count; i++)
        {
            randomizedIndices.Add(i);
        }
        
        // Shuffle the indices to randomize piece order
        for (int i = 0; i < randomizedIndices.Count; i++)
        {
            int randomIndex = Random.Range(i, randomizedIndices.Count);
            int temp = randomizedIndices[i];
            randomizedIndices[i] = randomizedIndices[randomIndex];
            randomizedIndices[randomIndex] = temp;
        }
        
        // Move each existing piece to the duster's cardSlot in randomized order
        for (int i = 0; i < enhancedCard.activePieceCards.Count; i++)
        {
            int pieceIndex = randomizedIndices[i];
            var pieceCard = enhancedCard.activePieceCards[pieceIndex];
            if (pieceCard != null)
            {
                // Remove from current hand
                if (pieceCard.parentHolder != null)
                {
                    pieceCard.parentHolder.RemoveCard(pieceCard);
                }
                
                // Add to duster's cardSlot
                cardSlot.AddCardToHand(pieceCard);
                
                // Set card location to Mat for bigCardVisual display
                pieceCard.cardLocation = CardLocation.Mat;
                
                // Calculate scattered position within duster area using the randomized index
                Vector2 scatteredPosition = GetScatteredPositionInDuster(dusterCenter, i);
                
                // Move piece to scattered position
                if (pieceCard.transform.parent != null)
                {
                    pieceCard.transform.parent.position = scatteredPosition;
                }
                
                // Make piece visible
                if (pieceCard.bigCardVisual != null)
                {
                    pieceCard.bigCardVisual.ShowCard(true);
                }
            }
        }
        
        // Store reference to original card for tracking
        currentCard = originalCard;
    }

    /// <summary>
    /// Update the flip switch visuals (button image and light)
    /// </summary>
    private void UpdateFlipSwitchVisuals()
    {
        if (flipSwitchButtonImage == null) return;

        if (isFlipSwitchOn)
        {
            flipSwitchButtonImage.sprite = flipSwitchOnSprite;
            if (flipSwitchLight != null)
            {
                DOTween.To(() => flipSwitchLight.intensity, x => flipSwitchLight.intensity = x, lightOnValue, lightTransitionDuration);
            }
            // Light ON: set base tint to configured fingerprint tint; emission to normal
            ApplyFingerprintLightState(fingerprintTintColor, fingerprintEmissionColor, fingerprintNormalIntensity);
            // Light ON: particle system emission intensity -> 0
            if (currentFingerprint != null) currentFingerprint.SetParticleEmissionIntensity(0f);
        }
        else
        {
            flipSwitchButtonImage.sprite = flipSwitchOffSprite;
            if (flipSwitchLight != null)
            {
                DOTween.To(() => flipSwitchLight.intensity, x => flipSwitchLight.intensity = x, lightOffValue, lightTransitionDuration);
            }
            // Light OFF: keep tint as configured fingerprint tint; emission boosted
            ApplyFingerprintLightState(fingerprintTintColor, fingerprintEmissionColor, fingerprintGlowIntensity);
            // Light OFF: particle system emission intensity -> configured value
            if (currentFingerprint != null) currentFingerprint.SetParticleEmissionIntensity(powderParticleEmissionIntensity);
        }
    }

    private void ApplyFingerprintLightState(Color baseTint, Color emissionColor, float emissionIntensity)
    {
        if (currentFingerprint == null) return;
        var currentPage = currentFingerprint.GetCurrentPage();
        if (currentPage?.powderBrush == null) return;
        currentPage.powderBrush.SetFingerprintLightState(baseTint, emissionColor, emissionIntensity);
    }
    
    #region Flip Switch Public Methods
    
    /// <summary>
    /// Get the current state of the flip switch
    /// </summary>
    public bool IsFlipSwitchOn => isFlipSwitchOn;
    
    /// <summary>
    /// Set the flip switch state programmatically
    /// </summary>
    public void SetFlipSwitchState(bool isOn)
    {
        isFlipSwitchOn = isOn;
        UpdateFlipSwitchVisuals();
    }
    
    /// <summary>
    /// Toggle the flip switch state programmatically
    /// </summary>
    public void ToggleFlipSwitch()
    {
        isFlipSwitchOn = !isFlipSwitchOn;
        UpdateFlipSwitchVisuals();
    }
    
    /// <summary>
    /// Turn the flip switch on
    /// </summary>
    public void TurnFlipSwitchOn()
    {
        SetFlipSwitchState(true);
    }
    
    /// <summary>
    /// Turn the flip switch off
    /// </summary>
    public void TurnFlipSwitchOff()
    {
        SetFlipSwitchState(false);
    }
    
    #endregion
    
    #region LCD Display Management
    
    /// <summary>
    /// Initialize the LCD display
    /// </summary>
    private void InitializeLcdDisplay()
    {
        if (lcdDisplay == null)
        {
            Debug.LogWarning("[FingerPrintDusterSystem] No LCD display assigned. Falling back to traditional progress squares.");
            return;
        }
        

        
        // Don't set initial state here - let UpdateLcdState() handle it
        // This ensures the state is set based on current conditions
    }
    
    /// <summary>
    /// Update progress on both LCD and traditional squares
    /// </summary>
    private void UpdateProgressDisplay(float progressPercent)
    {
        // Convert percentage to 0-5 scale for LCD squares using specific thresholds
        int lcdProgress = CalculateProgressSquares(progressPercent);
        lcdProgress = Mathf.Clamp(lcdProgress, 0, 5);
        
        if (lcdDisplay != null)
        {
            // Update LCD with progress squares
            lcdDisplay.UpdateProgress(lcdProgress);
        }

        // Check if progress meets the page's required reveal percentage
        bool progressComplete = CheckProgressComplete(progressPercent);
        UpdateScanButtonForProgress(progressComplete);
    }
    
    /// <summary>
    /// Calculate how many squares should be filled based on specific percentages
    /// </summary>
    private int CalculateProgressSquares(float progressPercent)
    {
        // Squares fill at: 20%, 40%, 60%, 75%, 90%
        if (progressPercent >= 90f) return 5;
        if (progressPercent >= 75f) return 4;
        if (progressPercent >= 60f) return 3;
        if (progressPercent >= 40f) return 2;
        if (progressPercent >= 20f) return 1;
        return 0;
    }
    
    /// <summary>
    /// Check if progress meets the page's required reveal percentage
    /// </summary>
    private bool CheckProgressComplete(float progressPercent)
    {
        if (currentFingerprint == null) return false;
        
        var page = currentFingerprint.GetCurrentPage();
        if (page == null) return false;
        
        // requiredRevealPercentage is stored as 0-100; progressPercent is also 0-100
        float requiredPercentage = Mathf.Clamp(page.requiredRevealPercentage, 0f, 100f);
        
        // Check if progress exceeds the page's required reveal percentage
        bool isComplete = progressPercent >= requiredPercentage;
        
        Debug.Log($"[FingerPrintDusterSystem] Progress check: {progressPercent:F1}% >= {requiredPercentage:F1}% = {isComplete}");
        
        return isComplete;
    }
    
    /// <summary>
    /// Update scan button state based on progress (not fingerprint presence)
    /// </summary>
    private void UpdateScanButtonForProgress(bool progressComplete)
    {
        if (scanButton != null)
        {
            // Always show the button, but enable/disable based on progress
            scanButton.gameObject.SetActive(true);
            scanButton.interactable = progressComplete;
            
            Debug.Log($"[FingerPrintDusterSystem] Scan button - Visible: true, Enabled: {progressComplete}");
        }
    }
    
    /// <summary>
    /// Update LCD state based on duster workflow
    /// </summary>
    private void UpdateLcdState()
    {
        
        if (lcdDisplay == null) 
        {
            Debug.LogWarning("[FingerPrintDusterSystem] LCD display is null!");
            return;
        }
        
        if (currentCard == null)
        {
            // No card loaded
            lcdDisplay.SetState(LcdDisplayController.LcdState.LoadEvidence);
        }
        else
        {
            // Card is loaded - show progress squares if there's progress, otherwise show brush message
            if (currentFingerprint != null)
            {
                float currentProgress = currentFingerprint.CurrentPageRevealPercentage;
                if (currentProgress > 0f)
                {
                    // Show progress squares if there's any progress (regardless of brushing state)
                    UpdateProgressDisplay(currentProgress);
                }
                else
                {
                    // Show brush message only when progress is 0
                    lcdDisplay.SetState(LcdDisplayController.LcdState.BrushForPrints);
                }
            }
            else
            {
                // Card loaded but no fingerprint component - still show brush message
                lcdDisplay.SetState(LcdDisplayController.LcdState.BrushForPrints);
            }
        }
    }
    
    /// <summary>
    /// Update LCD state with force (stops any ongoing animations to prevent glitches)
    /// </summary>
    private void UpdateLcdStateForce()
    {
        
        if (lcdDisplay == null) 
        {
            return;
        }
        
        if (currentCard == null)
        {
            // No card loaded
            lcdDisplay.ForceSetState(LcdDisplayController.LcdState.LoadEvidence);
        }
        else
        {
            // Card is loaded - show progress squares if there's progress, otherwise show brush message
            if (currentFingerprint != null)
            {
                float currentProgress = currentFingerprint.CurrentPageRevealPercentage;
                if (currentProgress > 0f)
                {
                    // Show progress squares if there's any progress (regardless of brushing state)
                    UpdateProgressDisplay(currentProgress);
                }
                else
                {
                    // Show brush message only when progress is 0
                    lcdDisplay.ForceSetState(LcdDisplayController.LcdState.BrushForPrints);
                }
            }
            else
            {
                // Card loaded but no fingerprint component - still show brush message
                lcdDisplay.ForceSetState(LcdDisplayController.LcdState.BrushForPrints);
            }
        }
    }
    
    #endregion
    
    #region Scanning Animation
    
    /// <summary>
    /// Initialize the scan bar for the scanning animation
    /// </summary>
    private void InitializeScanBar()
    {
        if (scanBar == null)
        {
            Debug.LogWarning("[FingerPrintDusterSystem] No scan bar assigned. Scanning animation will be disabled.");
            return;
        }
        
        // Get the Image component for color control
        scanBarImage = scanBar.GetComponent<Image>();
        if (scanBarImage == null)
        {
            Debug.LogError("[FingerPrintDusterSystem] Scan bar GameObject must have an Image component!");
            return;
        }
        
        // Get the RectTransform for positioning
        scanBarRectTransform = scanBar.GetComponent<RectTransform>();
        if (scanBarRectTransform == null)
        {
            Debug.LogError("[FingerPrintDusterSystem] Scan bar GameObject must have a RectTransform component!");
            return;
        }
        
        // Store original position (left side of the duster)
        scanBarOriginalPosition = scanBarRectTransform.localPosition;
        
        // Calculate end position (right side of the duster)
        // We'll use the cardSlot's width to determine the scan range
        if (cardSlot != null)
        {
            RectTransform cardSlotRect = cardSlot.GetComponent<RectTransform>();
            if (cardSlotRect != null)
            {
                float scanWidth = cardSlotRect.rect.width;
                scanBarEndPosition = scanBarOriginalPosition + Vector3.right * scanWidth;
            }
            else
            {
                // Fallback: use a fixed width
                scanBarEndPosition = scanBarOriginalPosition + Vector3.right * 200f;
            }
        }
        else
        {
            // Fallback: use a fixed width
            scanBarEndPosition = scanBarOriginalPosition + Vector3.right * 200f;
        }
        
        // Set initial alpha and hide the scan bar
        if (scanBarImage != null)
        {
            Color color = scanBarImage.color;
            color.a = 0f; // Start invisible
            scanBarImage.color = color;
        }
        
        // Ensure scan bar is hidden initially
        scanBar.SetActive(false);
        
        Debug.Log($"[FingerPrintDusterSystem] Scan bar initialized - Original: {scanBarOriginalPosition}, End: {scanBarEndPosition}");
    }
    
    /// <summary>
    /// Start the scanning animation
    /// </summary>
    private void StartScanAnimation()
    {
        if (scanBar == null || isScanning)
        {
            Debug.LogWarning("[FingerPrintDusterSystem] Cannot start scan animation - scan bar is null or already scanning");
            return;
        }
        
        isScanning = true;
        
        // Disable scan button during animation
        if (scanButton != null)
        {
            scanButton.interactable = false;
        }
        
        // Show the scan bar
        scanBar.SetActive(true);
        
        // Reset position to start
        scanBarRectTransform.localPosition = scanBarOriginalPosition;
        
        // Fade in the scan bar
        if (scanBarImage != null)
        {
            Color startColor = scanBarImage.color;
            startColor.a = 0f;
            scanBarImage.color = startColor;
            
            Color endColor = scanBarImage.color;
            endColor.a = scanBarAlpha;
            
            // Fade in quickly
            scanBarImage.DOColor(endColor, 0.1f).SetEase(DG.Tweening.Ease.OutQuad);
        }
        
        // Start the scanning animation sequence
        StartCoroutine(ScanAnimationSequence());
        
        Debug.Log("[FingerPrintDusterSystem] Scan animation started");
    }
    
    /// <summary>
    /// Coroutine that handles the complete scanning animation sequence
    /// </summary>
    private IEnumerator ScanAnimationSequence()
    {
        // Wait a moment for the fade-in to complete
        yield return new WaitForSeconds(0.1f);
        
        // Move scan bar to the right (scan forward)
        scanBarRectTransform.DOLocalMove(scanBarEndPosition, scanDuration * 0.5f)
            .SetEase(scanEase);
        
        // Wait for forward scan to complete
        yield return new WaitForSeconds(scanDuration * 0.5f);
        
        // Move scan bar back to the left (scan backward)
        scanBarRectTransform.DOLocalMove(scanBarOriginalPosition, scanDuration * 0.5f)
            .SetEase(scanEase);
        
        // Wait for backward scan to complete
        yield return new WaitForSeconds(scanDuration * 0.5f);
        
        // Process the evidence after the scan animation completes
        ProcessScannedEvidence();
        
        // Fade out the scan bar
        if (scanBarImage != null)
        {
            Color endColor = scanBarImage.color;
            endColor.a = 0f;
            scanBarImage.DOColor(endColor, 0.2f).SetEase(DG.Tweening.Ease.InQuad);
        }
        
        // Wait for fade out to complete
        yield return new WaitForSeconds(0.2f);
        
        // Hide the scan bar
        scanBar.SetActive(false);
        
        // Update scan button state (this will handle enabling/disabling based on scan status)
        UpdateScanButtonState();
        
        isScanning = false;
        
        Debug.Log("[FingerPrintDusterSystem] Scan animation completed");
    }
    
    /// <summary>
    /// Process the evidence after the scanning animation completes
    /// </summary>
    private void ProcessScannedEvidence()
    {
        bool hasFingerprint = false;
        bool foundEvidence = false;
        
        // Check if we have a fingerprint
        if (currentFingerprint != null)
        {
            var page = currentFingerprint.GetCurrentPage();
            if (page != null && page.hasFingerprint)
            {
                hasFingerprint = true;
                
                // Check if there's evidence to unlock
                if (!page.extraEvidenceUnlocked && !string.IsNullOrEmpty(page.associatedEvidenceId))
                {
                    // Try to unlock associated extra evidence by ID from current case
                    var gm = GameManager.Instance;
                    if (gm != null && gm.CurrentCase != null)
                    {
                        var evidenceId = page.associatedEvidenceId;
                        Evidence extra = gm.CurrentCase.extraEvidences?.Find(ev => ev != null && ev.id == evidenceId);
                        if (extra != null)
                        {
                            // Add without clearing existing hand
                            EvidenceManager.Instance?.LoadExtraEvidence(extra);
                            currentFingerprint.MarkCurrentPageExtraEvidenceUnlocked();
                            foundEvidence = true;
                        }
                    }
                }
            }
        }
        
        // Show appropriate LCD feedback
        if (lcdDisplay != null)
        {
            if (hasFingerprint)
            {
                lcdDisplay.ShowTemporaryMessage(LcdDisplayController.LcdState.ScanSaved);
            }
            else
            {
                lcdDisplay.ShowTemporaryMessage(LcdDisplayController.LcdState.NoPrintsFound);
            }
        }
        
        Debug.Log($"[FingerPrintDusterSystem] Evidence processed - Has fingerprint: {hasFingerprint}, Found evidence: {foundEvidence}");
        
        // Update button state (legacy)
        UpdateScanButtonState();
    }
    
    /// <summary>
    /// Check if the scanning animation is currently running
    /// </summary>
    public bool IsScanning => isScanning;
    
    #endregion
}