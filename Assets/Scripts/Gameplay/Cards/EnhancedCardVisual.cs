using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public enum EnhancedCardType
{
    Torn,
    Connectable,
    Piece
}

[System.Serializable]
public class PuzzleGridSettings
{
    public int gridSize = 3; // 3x3, 4x4, or 5x5
    public float pieceSnapDistance = 180f;  // Increased from 120f to 180f for easier snapping
    public float directionalSnapMultiplier = 0.75f; // New field to control how strict the directional snapping is
    public float glowDuration = 1f;
    public float glowHoldDuration = 0.5f;
    public float glowDownDuration = 0.5f;
    public Color glowColor = Color.yellow;
    public float glowAlpha = 0.5f;
    public float effectDistance = 0.1f;
    public float glowUpDuration = 0.6f; // Duration of glow fade in
    public float glowStartAlpha = 0.5f; // Starting alpha for glow
    public float glowMaxAlpha = 1f; // Maximum alpha for glow effect
    public Vector2 glowEffectDistance = new Vector2(3, 3); // Outline effect distance
}

/// <summary>
/// Enhanced card visual system for torn puzzle cards and connectable cards
/// </summary>
public class EnhancedCardVisual : MonoBehaviour
{
    [Header("Enhanced Card Settings")]
    public EnhancedCardType cardType = EnhancedCardType.Torn;
    
    [Header("Enhanced Sprites")]
    [Tooltip("Sprite shown when hovering torn card over mat (before breaking)")]
    public Sprite big_TornCardWhenHovering;   // Shown in BigCardVisual when hovering over mat

    [Tooltip("Sprite shown in small card view when card is torn but not completed")]
    public Sprite small_TornCard;             // Small card sprite before completion

    [Tooltip("Sprite shown in small card view for individual pieces")]
    public Sprite small_PieceCard;            // Small card sprite for pieces

    [Tooltip("Sprite shown in small card view after puzzle is completed")]
    public Sprite small_JoinedCard;           // Small card sprite after completion
    
    [Header("Puzzle Pieces")]
    public List<GameObject> piecePrefabs = new List<GameObject>(); // BigCardVisual prefabs for each piece
    
    [Header("Puzzle Settings")]
    public PuzzleGridSettings puzzleSettings = new PuzzleGridSettings();
    
    [Header("Piece Management")]
    public List<Card> activePieceCards = new List<Card>(); // Created piece cards
    public Card originalCard; // Reference to the original card
    
    // Piece positioning data
    private Dictionary<int, Vector2Int> pieceGridPositions = new Dictionary<int, Vector2Int>();
    private Dictionary<int, List<int>> pieceNeighbors = new Dictionary<int, List<int>>();
    private Vector2 originalDropPosition;
    private bool isPuzzleComplete = false;
    
    // Piece-specific properties
    [Header("Piece Properties")]
    public int pieceIndex = -1;
    public Vector2Int gridPosition;
    public List<int> neighborIndices = new List<int>();
    public bool isInCorrectPosition = false;
    
    [Header("Puzzle State")]
    public bool isPuzzleCompleted = false;
    private Sprite originalCardVisualSprite; // Store original sprite to restore later
    private bool hasAppliedHoverEffect = false; // Track if hover effect has been applied
    
    // Store original big card visual sprite for restoration
    private Sprite originalBigCardVisualSprite;
    
    // Public accessors for piece data
    public Dictionary<int, Vector2Int> PieceGridPositions => pieceGridPositions;
    public Dictionary<int, List<int>> PieceNeighbors => pieceNeighbors;
    
    private bool isBeingMoved = false;
    private bool isCompletionSequenceRunning = false; // Flag to prevent multiple completion sequences
    private bool isPrecreatingPieces = false;
    private Coroutine precreatePiecesCoroutine;
    private bool isUnhidingPieces = false;
    private Vector3? completionTargetWorldPosition = null;
    
    [Header("Scatter Settings")]
    public bool clampScatterToMat = true; // Ensure scattered pieces stay within mat bounds
    public float scatterMarginPixels = 40f; // Inset from mat edges when clamping
    
    private void Awake()
    {
        // Try to find the Card reference
        FindCardReference();
    }
    
    private void Start()
    {
        // Try again in Start() in case parentCard wasn't set during Awake()
        if (originalCard == null)
        {
            FindCardReference();
        }
        
        if (originalCard == null)
        {
            Debug.LogError("[EnhancedCardVisual] Could not find Card reference! Make sure this component is on a BigCardVisual with parentCard set, or on a Card directly.");
        }
        
        if (cardType == EnhancedCardType.Torn && originalCard != null)
        {
            CalculatePieceNeighbors();
        }
    }
    
    private void FindCardReference()
    {
        
        // If this is on a BigCardVisual, get the Card from the BigCardVisual's parentCard
        BigCardVisual bigCardVisual = GetComponent<BigCardVisual>();
        if (bigCardVisual != null)
        {
            if (bigCardVisual.parentCard != null)
        {
            originalCard = bigCardVisual.parentCard;
            Debug.Log($"[EnhancedCardVisual] Found Card reference from BigCardVisual.parentCard: {originalCard.name}");
                return;
        }
            else
        {
                Debug.Log("[EnhancedCardVisual] BigCardVisual found but parentCard is null");
            }
        }
        else
        {
            Debug.Log($"[EnhancedCardVisual] No BigCardVisual component found on {gameObject.name}");
        }
        
            // Fallback: try to get Card component directly (for cases where it's on the Card itself)
        Card cardComponent = GetComponent<Card>();
        if (cardComponent != null)
            {
            originalCard = cardComponent;
                Debug.Log($"[EnhancedCardVisual] Found Card reference directly: {originalCard.name}");
            return;
        }
        else
        {
            Debug.Log($"[EnhancedCardVisual] No Card component found on {gameObject.name}");
        }
        
        // Additional fallback: try to find Card component in parent
        Card parentCard = GetComponentInParent<Card>();
        if (parentCard != null)
        {
            originalCard = parentCard;
            Debug.Log($"[EnhancedCardVisual] Found Card reference in parent: {originalCard.name}");
            return;
        }
        
        Debug.LogWarning($"[EnhancedCardVisual] Could not find any Card reference for {gameObject.name}");
    }
    
    private Vector3 lastPosition;
    private float positionCheckDelay = 0.1f;
    private float lastPositionCheckTime = 0f;
    
    private void Update()
    {
        // Track if piece is being moved for visual feedback (gizmos, etc.)
        if (cardType == EnhancedCardType.Piece && transform.hasChanged)
        {
            isBeingMoved = true;
            transform.hasChanged = false;
            
            // Start a coroutine to reset isBeingMoved after a short delay
            StartCoroutine(ResetMovingState());
        }
    }
    
    private IEnumerator ResetMovingState()
    {
        yield return new WaitForSeconds(0.1f);
        isBeingMoved = false;
    }

    /// <summary>
    /// Disable interaction on a piece card (no drag, no raycasts)
    /// </summary>
    private void DisablePieceInteraction(Card pieceCard)
    {
        if (pieceCard == null) return;
        pieceCard.isDragLocked = true;
        var img = pieceCard.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        var cg = pieceCard.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = pieceCard.gameObject.AddComponent<CanvasGroup>();
        }
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    /// <summary>
    /// Enable interaction on a piece card (allow drag and raycasts)
    /// </summary>
    private void EnablePieceInteraction(Card pieceCard)
    {
        if (pieceCard == null) return;
        pieceCard.isDragLocked = false;
        var img = pieceCard.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
        var cg = pieceCard.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    /// <summary>
    /// Disable interaction for the original joined card during completion.
    /// </summary>
    private void DisableOriginalCardInteraction()
    {
        if (originalCard == null) return;
        originalCard.isDragLocked = true;
        var img = originalCard.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        var cg = originalCard.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = originalCard.gameObject.AddComponent<CanvasGroup>();
        }
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    /// <summary>
    /// Re-enable interaction for the original joined card after completion fades in.
    /// </summary>
    private void EnableOriginalCardInteraction()
    {
        if (originalCard == null) return;
        originalCard.isDragLocked = false;
        var img = originalCard.GetComponent<Image>();
        if (img != null) img.raycastTarget = true;
        var cg = originalCard.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }
    }

    /// <summary>
    /// Place a card's slot under a holder at a specific world position.
    /// </summary>
    private void PlaceCardSlotAtWorldPosition(HorizontalCardHolder holder, Card card, Vector3 worldPosition)
    {
        if (holder == null || card == null) return;
        RectTransform holderRect = holder.GetComponent<RectTransform>();
        if (holderRect == null) return;
        Transform target = card.transform.parent != null ? card.transform.parent : card.transform;
        RectTransform targetRect = target as RectTransform;
        Vector3 local = holderRect.InverseTransformPoint(worldPosition);
        target.localPosition = new Vector3(local.x, local.y, 0);
        // Do not also move bigCardVisual in world space; it should follow the slot locally
        if (card.bigCardVisual != null)
        {
            var bigRt = card.bigCardVisual.GetComponent<RectTransform>();
            if (bigRt != null)
            {
                bigRt.anchoredPosition3D = Vector3.zero;
                bigRt.localPosition = Vector3.zero;
            }
        }
        // Keep correct sizing when on mat showing big cards
        if (holder.ShowsBigCards() && targetRect != null && card.bigCardVisual != null)
        {
            RectTransform bigRect = card.bigCardVisual.GetComponent<RectTransform>();
            if (bigRect != null)
            {
                targetRect.sizeDelta = bigRect.sizeDelta;
            }
        }
        // Bring to front for input consistency
        target.SetAsLastSibling();
        if (card.bigCardVisual != null) card.bigCardVisual.transform.SetAsLastSibling();
        if (card.cardVisual != null) card.cardVisual.transform.SetAsLastSibling();
    }
    
    private void OnDrawGizmos()
    {
        // Only draw for piece cards that are being moved
        if (cardType != EnhancedCardType.Piece || !isBeingMoved) return;
        
        // Draw snap distance circle
        float worldSnapDistance = puzzleSettings.pieceSnapDistance;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            worldSnapDistance *= canvas.transform.localScale.x;
        }

        // Draw filled circle with transparency
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f); // Very transparent cyan for fill
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = transform.position;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float nextAngle = (i + 1) * angleStep * Mathf.Deg2Rad;
            Vector3 point1 = transform.position + new Vector3(Mathf.Cos(angle) * worldSnapDistance, Mathf.Sin(angle) * worldSnapDistance, 0);
            Vector3 point2 = transform.position + new Vector3(Mathf.Cos(nextAngle) * worldSnapDistance, Mathf.Sin(nextAngle) * worldSnapDistance, 0);
            Gizmos.DrawLine(transform.position, point1);
            Gizmos.DrawLine(point1, point2);
            prevPoint = point2;
        }

        // Draw outline circle with more opacity
        Gizmos.color = new Color(0f, 1f, 1f, 0.8f); // More solid cyan for outline
        prevPoint = transform.position + Vector3.right * worldSnapDistance;
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = transform.position + new Vector3(Mathf.Cos(angle) * worldSnapDistance, Mathf.Sin(angle) * worldSnapDistance, 0);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }
    
    /// <summary>
    /// Clamp a screen position to the bounds of the mat hand's RectTransform (with margin).
    /// </summary>
    private Vector2 ClampToMatScreenBounds(Vector2 screenPos)
    {
        var matHand = DragManager.Instance != null ? DragManager.Instance.matHand : null;
        RectTransform matRect = null;
        if (DragManager.Instance != null && DragManager.Instance.matRect != null)
        {
            matRect = DragManager.Instance.matRect;
        }
        else if (matHand != null)
        {
            matRect = matHand.GetComponent<RectTransform>();
        }
        if (matRect == null)
        {
            return screenPos;
        }
        Canvas canvas = matRect.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
        Vector3[] corners = new Vector3[4];
        matRect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // bottom-left
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // top-right
        min.x += scatterMarginPixels;
        min.y += scatterMarginPixels;
        max.x -= scatterMarginPixels;
        max.y -= scatterMarginPixels;
        return new Vector2(
            Mathf.Clamp(screenPos.x, min.x, max.x),
            Mathf.Clamp(screenPos.y, min.y, max.y)
        );
    }

    /// <summary>
    /// Clamp a world position to the mat holder's rect bounds (using screen-space clamp and convert back to world).
    /// </summary>
    private Vector3 ClampToMatWorldBounds(Vector3 worldPos)
    {
        var matHand = DragManager.Instance != null ? DragManager.Instance.matHand : null;
        RectTransform matRect = null;
        if (DragManager.Instance != null && DragManager.Instance.matRect != null)
        {
            matRect = DragManager.Instance.matRect;
        }
        else if (matHand != null)
        {
            matRect = matHand.GetComponent<RectTransform>();
        }
        if (matRect == null) return worldPos;

        Canvas canvas = matRect.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        // Convert world position to screen, clamp in screen space to rect bounds, then back to local and world
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        Vector3[] corners = new Vector3[4];
        matRect.GetWorldCorners(corners);
        Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // bottom-left
        Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // top-right
        min.x += scatterMarginPixels;
        min.y += scatterMarginPixels;
        max.x -= scatterMarginPixels;
        max.y -= scatterMarginPixels;
        screen.x = Mathf.Clamp(screen.x, min.x, max.x);
        screen.y = Mathf.Clamp(screen.y, min.y, max.y);

        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(matRect, screen, cam, out local))
        {
            return matRect.TransformPoint(local);
        }
        return worldPos;
    }
    
    /// <summary>
    /// Called when card is being dragged over the mat (before drop)
    /// </summary>
    public void OnHoveringOverMat()
    {
        Debug.Log($"[EnhancedCardVisual] OnHoveringOverMat called for card type: {cardType}, hasAppliedHoverEffect: {hasAppliedHoverEffect}");
        
        // Skip if hover effect already applied or puzzle was previously completed
        if (hasAppliedHoverEffect || isPuzzleCompleted)
        {
            Debug.Log($"[EnhancedCardVisual] Skipping hover effect - already applied: {hasAppliedHoverEffect}, completed: {isPuzzleCompleted}");
            return;
        }
        
        if (originalCard == null)
        {
            Debug.LogError("[EnhancedCardVisual] originalCard is null! Cannot process hover effect.");
            return;
        }
        
        if (cardType == EnhancedCardType.Torn || cardType == EnhancedCardType.Connectable)
        {
            Debug.Log($"[EnhancedCardVisual] Processing hover for enhanced card");
            
            // Store original sprites before changing them
            if (originalCard.cardVisual != null)
            {
                var image = originalCard.cardVisual.GetComponent<Image>();
                if (image != null)
                {
                    originalCardVisualSprite = image.sprite;
                }
            }
            // Legacy: previously stored bigCard display sprite; now pages are GameObjects.
            // If needed, capture first page sprite from an Image under page 0.
            if (originalCard.bigCardVisual != null)
            {
                var firstPage = originalCard.bigCardVisual.GetActivePageObject() ?? (originalCard.bigCardVisual.pageObjects.Count > 0 ? originalCard.bigCardVisual.pageObjects[0] : null);
                var img = firstPage != null ? firstPage.GetComponentInChildren<Image>(true) : null;
                if (img != null)
                {
                    originalBigCardVisualSprite = img.sprite;
                }
            }
            
            // Change bigCardVisual sprite and hide page buttons
            if (originalCard.bigCardVisual != null)
            {
                // Replace first page's main Image sprite when hovering
                var firstPage = originalCard.bigCardVisual.GetActivePageObject() ?? (originalCard.bigCardVisual.pageObjects.Count > 0 ? originalCard.bigCardVisual.pageObjects[0] : null);
                var img = firstPage != null ? firstPage.GetComponentInChildren<Image>(true) : null;
                if (img != null)
                {
                    Debug.Log($"[EnhancedCardVisual] Changing bigCard page[0] image from {img.sprite?.name} to {big_TornCardWhenHovering?.name}");
                    img.sprite = big_TornCardWhenHovering;
                }
                // Hide page turning buttons and other UI elements
                HideCardUIElements();
            }
            else
            {
                Debug.LogWarning($"[EnhancedCardVisual] originalCard.bigCardVisual is null!");
            }
            
            // Change small card visual sprite
            if (originalCard.cardVisual != null)
            {
                var image = originalCard.cardVisual.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = small_TornCard;
                }
            }
            
            // Pre-create puzzle pieces for torn cards (hidden)
            if (cardType == EnhancedCardType.Torn && activePieceCards.Count == 0 && !isPrecreatingPieces)
            {
                precreatePiecesCoroutine = StartCoroutine(PreCreatePuzzlePieces());
            }
            
            hasAppliedHoverEffect = true;
        }
        else if (cardType == EnhancedCardType.Piece)
        {
            Debug.Log($"[EnhancedCardVisual] Piece hover logic not implemented yet");
            // TODO: Implement piece-specific hover logic if needed
        }
        else if (cardType == EnhancedCardType.Connectable)
        {
            Debug.Log($"[EnhancedCardVisual] Connectable card logic not implemented yet");
            // TODO: Implement connectable logic later
        }
        else
        {
            Debug.LogWarning($"[EnhancedCardVisual] Unknown card type: {cardType}");
        }
    }
    
    /// <summary>
    /// Called when card exits the mat area during drag
    /// </summary>
    public void OnExitingMat()
    {
        Debug.Log($"[EnhancedCardVisual] OnExitingMat called, resetting hover effect flag");
        hasAppliedHoverEffect = false;
    }
    
    /// <summary>
    /// Called when card is dropped on the mat
    /// </summary>
    public void OnDroppedOnMat(Vector2 dropPosition)
    {
        Debug.Log($"[EnhancedCardVisual] OnDroppedOnMat called for card type: {cardType} at position: {dropPosition}");
        
        if (originalCard == null)
        {
            Debug.LogError("[EnhancedCardVisual] originalCard is null! Cannot process drop.");
            return;
        }
        
        if (cardType == EnhancedCardType.Torn)
        {
            // Step 7: Check if puzzle has been completed before
            if (isPuzzleCompleted)
            {
                Debug.Log($"[EnhancedCardVisual] Torn card already completed - treating as normal card");
                // Just add to mat normally, don't break into pieces
                if (DragManager.Instance?.matHand != null)
                {
                    // Single-step: add and place without intermediate activation to avoid one-frame flash
                    DragManager.Instance.matHand.AddCardToHandAtWorldPosition(originalCard, dropPosition);
                }
                return;
            }
            
            Debug.Log($"[EnhancedCardVisual] Processing torn card drop with {activePieceCards.Count} pre-created pieces");
            
            originalDropPosition = dropPosition;
            
            // If pieces were pre-created during hover, just unhide them
            if (activePieceCards.Count > 0)
            {
                Debug.Log($"[EnhancedCardVisual] Unhiding pre-created pieces");
                UnhidePuzzlePieces();
            }
            else
            {
                // Fallback: create pieces if they weren't pre-created
                Debug.Log($"[EnhancedCardVisual] No pre-created pieces found, creating them now");
                StartCoroutine(CreatePuzzlePieces(dropPosition));
            }
        }
        else if (cardType == EnhancedCardType.Connectable)
        {
            Debug.Log($"[EnhancedCardVisual] Connectable card logic not implemented yet");
            // TODO: Implement connectable logic later
        }
        else
        {
            Debug.LogWarning($"[EnhancedCardVisual] Unknown card type: {cardType}");
        }
    }
    
    /// <summary>
    /// Create individual piece cards when torn card is dropped (fallback if not pre-created)
    /// </summary>
    private IEnumerator CreatePuzzlePieces(Vector2 dropPosition)
    {
        Debug.Log($"[EnhancedCardVisual] Creating puzzle pieces at position: {dropPosition}");
        
        // Ensure neighbor data is calculated before creating pieces
        if (pieceGridPositions.Count == 0 || pieceNeighbors.Count == 0)
        {
            Debug.Log("[EnhancedCardVisual] Calculating piece neighbors before creation...");
            CalculatePieceNeighbors();
        }
        
        var dragManager = FindFirstObjectByType<DragManager>();
        if (dragManager?.matHand == null)
        {
            Debug.LogError("[EnhancedCardVisual] Could not find DragManager or matHand!");
            yield break;
        }
        
        // Create pieces visible (fallback behavior)
        for (int i = 0; i < piecePrefabs.Count; i++)
        {
            yield return StartCoroutine(CreatePieceCard(i, dropPosition, dragManager.matHand, false));
        }

        // Hide original card after creation
        originalCard.gameObject.SetActive(false);
        
        Debug.Log($"[EnhancedCardVisual] Created {activePieceCards.Count} puzzle pieces");
    }
    
    /// <summary>
    /// Create a single piece card using proper card creation process
    /// </summary>
    private IEnumerator CreatePieceCard(int pieceIndex, Vector2 centerPosition, HorizontalCardHolder matHand, bool createHidden = false)
    {
        Debug.Log($"[EnhancedCardVisual] Creating piece card {pieceIndex} using proper card creation process");
        
        // STEP 1: Create slot (same as HorizontalCardHolder does)
        GameObject slot = Instantiate(matHand.slotPrefab, matHand.transform);
        slot.transform.localPosition = Vector3.zero;
        
        // STEP 2: Get the Card from the slot
        Card pieceCard = slot.GetComponentInChildren<Card>();
        if (pieceCard == null)
        {
            Debug.LogError($"[EnhancedCardVisual] No Card found in slot prefab for piece {pieceIndex}");
            Destroy(slot);
            yield break;
        }
        
        // STEP 3: Setup basic card properties
        pieceCard.name = $"{originalCard.name}_Piece_{pieceIndex}_Name{pieceIndex+1}"; // Index 0-8, Name 1-9
        
        // Add event listeners (same as HorizontalCardHolder does)
        pieceCard.PointerEnterEvent.AddListener(matHand.CardPointerEnter);
        pieceCard.PointerExitEvent.AddListener(matHand.CardPointerExit);
        pieceCard.BeginDragEvent.AddListener(matHand.BeginDrag);
        pieceCard.EndDragEvent.AddListener(matHand.EndDrag);
        
        // Add our own listener for when dragging ends to check piece position
        pieceCard.EndDragEvent.AddListener(OnPieceDropped);
        
        Debug.Log($"[EnhancedCardVisual] Piece {pieceIndex} basic setup complete");
        
        // STEP 4: Create piece data for initialization
        PuzzlePieceData pieceData = new PuzzlePieceData
        {
            pieceIndex = pieceIndex,
            originalCard = this.originalCard,
            smallSprite = small_PieceCard, // Use the new piece-specific sprite
            bigSprite = null, // Will be set via BigCardVisual prefab
            cardMode = originalCard.mode
        };
        
        // STEP 5: Initialize the card with piece data and visual handlers
        pieceCard.Initialize(pieceData, matHand.visualHandler, matHand.bigVisualHandler != null ? matHand.bigVisualHandler : matHand.visualHandler);
        
        // STEP 5.5: Explicitly set the cardVisual sprite (in case initialization didn't apply it)
        if (pieceCard.cardVisual != null && small_PieceCard != null)
        {
            var cardImage = pieceCard.cardVisual.GetCardImage();
            if (cardImage != null)
            {
                cardImage.sprite = small_PieceCard;
                Debug.Log($"[EnhancedCardVisual] Set piece {pieceIndex} cardVisual sprite to: {small_PieceCard.name}");
            }
            else
            {
                Debug.LogWarning($"[EnhancedCardVisual] Could not get cardImage for piece {pieceIndex}");
            }
        }
        else if (small_PieceCard == null)
        {
            Debug.LogWarning($"[EnhancedCardVisual] small_PieceCard is null! Please assign it in the inspector.");
        }
        
        
        // STEP 6: Create BigCardVisual from assigned prefab
        if (pieceCard.bigCardVisual == null && pieceIndex < piecePrefabs.Count && piecePrefabs[pieceIndex] != null)
        {
            Debug.Log($"[EnhancedCardVisual] Creating BigCardVisual from prefab for piece {pieceIndex}");
            
            // Use the assigned prefab for this piece
            GameObject bigCardPrefab = piecePrefabs[pieceIndex];
            
            // Create the BigCardVisual instance
            Transform bigVisualHandler = matHand.bigVisualHandler != null ? matHand.bigVisualHandler : matHand.visualHandler;
            GameObject bigCardInstance = Instantiate(bigCardPrefab, bigVisualHandler);
            pieceCard.bigCardVisual = bigCardInstance.GetComponent<BigCardVisual>();
           
            
            if (pieceCard.bigCardVisual != null)
            {
                pieceCard.bigCardVisual.ShowCard(false);
                bigCardInstance.transform.localRotation = Quaternion.identity;
                bigCardInstance.transform.localScale = Vector3.one;
                
                pieceCard.bigCardVisual.Initialize(pieceCard);
                                
                Debug.Log($"[EnhancedCardVisual] Successfully created BigCardVisual for piece {pieceIndex} using prefab: {bigCardPrefab.name}");
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] Prefab {bigCardPrefab.name} does not have a BigCardVisual component!");
            }
        }
        else if (pieceIndex >= piecePrefabs.Count || piecePrefabs[pieceIndex] == null)
        {
            Debug.LogError($"[EnhancedCardVisual] No prefab assigned for piece {pieceIndex}! Please assign BigCardVisual prefabs in the piecePrefabs list.");
        }
        
        // STEP 7: Size the slot for big card (same as HorizontalCardHolder does for mat)
        if (matHand.purpose == HolderPurpose.Mat)
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
        

        // Disable interaction until torn card is actually dropped on the mat
        DisablePieceInteraction(pieceCard);
        
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
            pieceEnhanced.originalCard = this.originalCard; // Reference back to original
            pieceEnhanced.puzzleSettings = this.puzzleSettings;
            pieceEnhanced.clampScatterToMat = this.clampScatterToMat;
            pieceEnhanced.scatterMarginPixels = this.scatterMarginPixels;
            
            // Setup piece-specific data (ensure dictionaries exist)
            if (pieceGridPositions.ContainsKey(pieceIndex) && pieceNeighbors.ContainsKey(pieceIndex))
            {
            pieceEnhanced.SetupAsPiece(pieceIndex, pieceGridPositions[pieceIndex], pieceNeighbors[pieceIndex]);
            }

        }
        
        // STEP 9: Position piece at scattered location
        // Get scattered position in world space using normalized method
        Vector2 scatteredWorldPosition = GetScatteredPositionNormalized(centerPosition, pieceIndex);
        // Clamp to mat to avoid out-of-bounds placement
        {
            Vector3 clamped = ClampToMatWorldBounds(scatteredWorldPosition);
            scatteredWorldPosition = new Vector2(clamped.x, clamped.y);
        }


        // Convert to screen space for matHand
        Vector2 scatteredScreenPosition = Camera.main.WorldToScreenPoint(scatteredWorldPosition);
        if (clampScatterToMat)
        {
            scatteredScreenPosition = ClampToMatScreenBounds(scatteredScreenPosition);
        }
        
        // Add to matHand at specific position using screen coordinates
        // This will trigger the new visual switching system
        matHand.AddCardToHandAtPosition(pieceCard, scatteredScreenPosition);
        
        // Ensure the piece shows the correct visual for the mat (big visual)
        // The new system should handle this automatically, but let's make sure
        if (pieceCard.bigCardVisual != null && pieceCard.cardVisual != null)
        {
            // Force the correct visual state for pieces on the mat
            pieceCard.cardVisual.ShowCard(false);  // Hide small visual
            pieceCard.bigCardVisual.ShowCard(true); // Show big visual
        }
        
        // IMPORTANT: Hide BigCardVisual AFTER adding to mat (since adding to mat automatically shows it)
        if (createHidden && pieceCard.bigCardVisual != null)
        {
            pieceCard.bigCardVisual.ShowCard(false);
        }
        
        // Add to our tracking list
        activePieceCards.Add(pieceCard);
        
        
        yield return new WaitForEndOfFrame();
        
    }
    
    /// <summary>
    /// Setup this card as a puzzle piece
    /// </summary>
    public void SetupAsPiece(int pieceIndex, Vector2Int gridPos, List<int> neighbors)
    {
        this.pieceIndex = pieceIndex;
        this.gridPosition = gridPos;
        this.neighborIndices = new List<int>(neighbors);
        
        // Note: Outline component will be added dynamically when needed for glow effect
        // (since this component is on the bigCardVisual already)
        
        // Initialize position tracking
        lastPosition = transform.position;
        
    }
    
    /// <summary>
    /// Manual trigger for testing position checking (can be called from console or debug buttons)
    /// </summary>
    [ContextMenu("Test Position Check")]
    public void TestPositionCheck()
    {
        if (cardType == EnhancedCardType.Piece)
        {

            CheckPiecePosition();
        }

    }
    
    /// <summary>
    /// Test all pieces at once (call this from the original torn card)
    /// </summary>
    [ContextMenu("Test All Pieces")]
    public void TestAllPieces()
    {
        if (cardType == EnhancedCardType.Torn)
        {
            for (int i = 0; i < activePieceCards.Count; i++)
            {
                var pieceCard = activePieceCards[i];
                if (pieceCard?.bigCardVisual != null)
                {
                    var pieceEnhanced = pieceCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
                    if (pieceEnhanced != null)
                    {
                        Debug.Log($"[EnhancedCardVisual] Testing piece {i}: {pieceCard.name}");
                        pieceEnhanced.TestPositionCheck();
                    }
                }
            }
        }

    }
    
    /// <summary>
    /// Pre-create puzzle pieces during hover (hidden)
    /// </summary>
    private IEnumerator PreCreatePuzzlePieces()
    {
        if (isPrecreatingPieces)
        {
            yield break;
        }
        isPrecreatingPieces = true;
        
        if (piecePrefabs.Count == 0)
        {
            Debug.LogError("[EnhancedCardVisual] No piece prefabs configured!");
            yield break;
        }
        
        // Ensure neighbor data is calculated before creating pieces
        if (pieceGridPositions.Count == 0 || pieceNeighbors.Count == 0)
        {
            CalculatePieceNeighbors();
        }
        
        // Get mat hand for piece creation
        var dragManager = FindFirstObjectByType<DragManager>();
        if (dragManager?.matHand == null)
        {
            Debug.LogError("[EnhancedCardVisual] Could not find DragManager or matHand!");
            yield break;
        }
        
        HorizontalCardHolder matHand = dragManager.matHand;
        Vector2 centerPosition = originalCard.transform.position; // Use current card position
        
        // Create each piece (hidden)
        for (int i = 0; i < piecePrefabs.Count; i++)
        {
            yield return StartCoroutine(CreatePieceCard(i, centerPosition, matHand, true)); // true = hidden
        }
        
        // Keep all precreated pieces non-interactive until drop
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            DisablePieceInteraction(activePieceCards[i]);
        }

        Debug.Log($"[EnhancedCardVisual] Pre-created {activePieceCards.Count} hidden puzzle pieces");
        isPrecreatingPieces = false;
    }
    
    /// <summary>
    /// Unhide pre-created puzzle pieces and scatter them
    /// </summary>
    private void UnhidePuzzlePieces()
    {
        if (isUnhidingPieces)
        {
            return;
        }
        StartCoroutine(UnhidePuzzlePiecesCoroutine());
    }
    
    /// <summary>
    /// Coroutine version of UnhidePuzzlePieces with proper timing to prevent race conditions
    /// </summary>
    private IEnumerator UnhidePuzzlePiecesCoroutine()
    {
        isUnhidingPieces = true;
        
        // If precreation is still running, wait for it to finish (up to a short timeout)
        float waitStart = Time.time;
        while (isPrecreatingPieces && Time.time - waitStart < 0.5f)
        {
            yield return null;
        }

        // Validate all pieces exist before proceeding
        int validPieces = 0;
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            if (activePieceCards[i] != null)
            {
                validPieces++;
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] Piece {i} is null in activePieceCards!");
            }
        }
        
        if (validPieces != activePieceCards.Count)
        {
            yield break;
        }
        
        // Remove original card from current holder (evidenceHand) but DON'T add to mat hand
        // The pieces will be what's visible on the mat, not the original card
        if (originalCard.parentHolder != null)
        {
            originalCard.parentHolder.RemoveCard(originalCard);
        }
        
        // Hide original card's bigCardVisual and disable the card
        if (originalCard != null)
        {
            if (originalCard.bigCardVisual != null)
            {
                originalCard.bigCardVisual.ShowCard(false);
                Debug.Log($"[EnhancedCardVisual] Original card BigCardVisual hidden");
            }
            
            originalCard.gameObject.SetActive(false);
        }
        
        // Move pieces CardSlots and bigCardVisuals to drop point, scatter CardSlots, then unhide
        // Use the original drop position since the card is no longer in the scene
        Vector3 dropWorldPosition = originalDropPosition;
        
        // Step 1: Move pieces CardSlots and their bigCardVisuals to drop point directly
        int movedToDropPoint = 0;
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            Card pieceCard = activePieceCards[i];
            if (pieceCard != null)
            {
                // Move CardSlot (parent) to drop point
                if (pieceCard.transform.parent != null)
                {
                    // Clamp to mat
                    pieceCard.transform.parent.position = ClampToMatWorldBounds(dropWorldPosition);
                    movedToDropPoint++;
                }
                else
                {
                    Debug.LogWarning($"[EnhancedCardVisual] Piece {i} has no parent (CardSlot)!");
                }
                
                // Move bigCardVisual to drop point too
                if (pieceCard.bigCardVisual != null)
                {
                    pieceCard.bigCardVisual.transform.position = dropWorldPosition;
                }
                else
                {
                    Debug.LogWarning($"[EnhancedCardVisual] Piece {i} has no bigCardVisual!");
                }
                
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] Piece {i} is null during Step 1!");
            }
        }
        
        
        // Wait a frame to ensure all transforms are updated
        yield return null;
        
        // Step 2: Add scatter offset to CardSlots only
        int scatteredPieces = 0;
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            Card pieceCard = activePieceCards[i];
            if (pieceCard != null && pieceCard.transform.parent != null)
            {
                // Calculate scattered position for the CardSlot (1 unit radius max)
                float scatterX = Random.Range(-1f, 1f); // Simple -1 to 1 range, no multiplier
                float scatterY = Random.Range(-1f, 1f); // Simple -1 to 1 range, no multiplier
                Vector3 scatteredWorldPos = dropWorldPosition + new Vector3(scatterX, scatterY, 0);
                scatteredWorldPos = ClampToMatWorldBounds(scatteredWorldPos);
                
                // Move CardSlot (parent) to scattered position
                pieceCard.transform.parent.position = scatteredWorldPos;
                scatteredPieces++;
                
            }
            else
            {
                if (pieceCard == null)
                {
                    Debug.LogError($"[EnhancedCardVisual] Piece {i} is null during Step 2!");
                }
                else if (pieceCard.transform.parent == null)
                {
                    Debug.LogError($"[EnhancedCardVisual] Piece {i} has no parent during Step 2!");
                }
            }
        }
        
        
        // Wait another frame to ensure scatter positions are applied
        yield return null;
        
        // Step 3: Unhide the bigCardVisuals so they smoothly follow CardSlots to scattered positions
        int unhiddenPieces = 0;
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            Card pieceCard = activePieceCards[i];
            if (pieceCard?.bigCardVisual != null)
            {
                // Force correct visual state and enable
                pieceCard.cardVisual?.ShowCard(false);
                pieceCard.bigCardVisual.ShowCard(true);
                // Ensure CanvasGroup alpha is 1
                var cg = pieceCard.bigCardVisual.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
                // Now that they're visible on mat, enable interaction
                EnablePieceInteraction(pieceCard);
                unhiddenPieces++;
            }
            else
            {
                if (pieceCard == null)
                {
                    Debug.LogError($"[EnhancedCardVisual] Piece {i} is null during Step 3!");
                }
                else if (pieceCard.bigCardVisual == null)
                {
                    Debug.LogError($"[EnhancedCardVisual] Piece {i} has no bigCardVisual during Step 3!");
                }
            }
        }
        
        
        // Wait a couple frames to ensure UI sync, then validate visibility again
        yield return null;
        yield return null;

        for (int i = 0; i < activePieceCards.Count; i++)
        {
            var pieceCard = activePieceCards[i];
            if (pieceCard?.bigCardVisual != null && !pieceCard.bigCardVisual.isActiveAndEnabled)
            {
                pieceCard.bigCardVisual.ShowCard(true);
            }
        }
        
        // Final validation - check all pieces are in expected states
        int finalValidPieces = 0;
        for (int i = 0; i < activePieceCards.Count; i++)
        {
            Card pieceCard = activePieceCards[i];
            if (pieceCard != null && 
                pieceCard.transform.parent != null && 
                pieceCard.bigCardVisual != null)
            {
                finalValidPieces++;
                Vector3 finalPos = pieceCard.transform.parent.position;
                bool isVisible = pieceCard.bigCardVisual.isActiveAndEnabled;
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] Piece {i} failed final validation - missing components!");
                if (pieceCard == null) Debug.LogError($"  - pieceCard is null");
                else if (pieceCard.transform.parent == null) Debug.LogError($"  - parent is null");
                else if (pieceCard.bigCardVisual == null) Debug.LogError($"  - bigCardVisual is null");
            }
        }
        
        
        if (finalValidPieces != activePieceCards.Count)
        {
            Debug.LogError($"[EnhancedCardVisual] WARNING: Only {finalValidPieces}/{activePieceCards.Count} pieces were successfully processed!");
        }

        isUnhidingPieces = false;
    }
    
    /// <summary>
    /// Calculate piece neighbors based on grid size (0-based indexing for pieces 0-8)
    /// </summary>
    public void CalculatePieceNeighbors()
    {
        int gridSize = puzzleSettings.gridSize;
        pieceGridPositions.Clear();
        pieceNeighbors.Clear();
        
        // Calculate grid positions for each piece (0-based indexing)
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            // Calculate x and y to get:
            // 1 2 3    y=2
            // 4 5 6    y=1
            // 7 8 9    y=0
            int x = i % gridSize;
            // Invert y since Unity's Y is up
            int y = (gridSize - 1) - (i / gridSize);
            pieceGridPositions[i] = new Vector2Int(x, y);
        }
        
        // Calculate neighbors for each piece (0-based indexing)
        for (int i = 0; i < gridSize * gridSize; i++)
        {
            List<int> neighbors = new List<int>();
            Vector2Int pos = pieceGridPositions[i];
            
            // Check all 4 directions
            // Right
            if (pos.x < gridSize - 1)
                neighbors.Add(i + 1);
            // Left
            if (pos.x > 0)
                neighbors.Add(i - 1);
            // Top (add gridSize since y is inverted)
            if (pos.y < gridSize - 1)
                neighbors.Add(i - gridSize);
            // Bottom (subtract gridSize since y is inverted)
            if (pos.y > 0)
                neighbors.Add(i + gridSize);
            
            pieceNeighbors[i] = neighbors;
        }
        
    }
    
    /// <summary>
    /// Get scattered position for piece placement (legacy method)
    /// </summary>
    private Vector2 GetScatteredPosition(Vector2 center, int pieceIndex)
    {
        // Create larger random offsets to spread pieces out more
        float randomX = Random.Range(-100f, 100f);
        float randomY = Random.Range(-100f, 100f);
        
        Vector2 offset = new Vector2(randomX, randomY);
        Vector2 scatteredPos = center + offset;
        
        
        return scatteredPos;
    }
    
    /// <summary>
    /// Get scattered position with normalized 0-1 radius for piece BigCardVisuals
    /// </summary>
    private Vector2 GetScatteredPositionNormalized(Vector2 center, int pieceIndex)
    {
        // Generate random position within unit circle (0-1 radius)
        float angle = Random.Range(0f, 2f * Mathf.PI);
        float radius = Random.Range(0.2f, 1f); // Minimum 0.2 to avoid clustering at center
        
        // Convert to world units (adjust multiplier based on your game's scale)
        float scatterRadius = 200f; // Adjust this value based on your mat size
        
        Vector2 offset = new Vector2(
            Mathf.Cos(angle) * radius * scatterRadius,
            Mathf.Sin(angle) * radius * scatterRadius
        );
        
        Vector2 scatteredPos = center + offset;
        
        
        return scatteredPos;
    }
    
    /// <summary>
    /// Hide card UI elements (page buttons, clue buttons, etc.)
    /// </summary>
    private void HideCardUIElements()
    {
        // Hide page turning buttons
        var pageButtons = originalCard.bigCardVisual.GetComponentsInChildren<Button>();
        foreach (var button in pageButtons)
        {
            if (button.name.Contains("Page") || button.name.Contains("page"))
            {
                button.gameObject.SetActive(false);
            }
        }
        
        // Hide clue buttons
        var clueComponents = originalCard.bigCardVisual.GetComponentsInChildren<MonoBehaviour>();
        foreach (var component in clueComponents)
        {
            if (component.GetType().Name.Contains("Clue"))
            {
                component.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Called when a piece is dropped (drag ends) - checks position and puzzle completion
    /// </summary>
    private void OnPieceDropped(Card droppedPiece)
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
                if (originalCard?.bigCardVisual != null)
                {
                    var originalEnhanced = originalCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
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
    /// Delayed puzzle completion check to ensure all position updates are complete
    /// </summary>
    public IEnumerator DelayedPuzzleCompletionCheck()
    {
        yield return new WaitForSeconds(0.1f); // Small delay
        CheckPuzzleCompletion();
    }
    
    /// <summary>
    /// Check if piece is in correct position relative to neighbors (Step 5)
    /// </summary>
    public void CheckPiecePosition()
    {
        if (cardType != EnhancedCardType.Piece) return;
        
        bool wasInCorrectPosition = isInCorrectPosition;
        isInCorrectPosition = false; // Assume false until proven otherwise
        
        // Check neighbor positions
        int correctNeighbors = 0;
        int totalNeighbors = neighborIndices.Count;
        
        foreach (int neighborIndex in neighborIndices)
        {
            Card neighborCard = FindPieceCardByIndex(neighborIndex);
            if (neighborCard != null)
            {
                // Check if neighbor is in correct relative position
                bool neighborCorrect = IsNeighborInCorrectRelativePosition(neighborIndex, neighborCard);
                if (neighborCorrect)
                {
                    correctNeighbors++;
                }
            }
        }
        
        // Piece is correctly positioned if ALL its neighbors are in correct relative positions
        isInCorrectPosition = (correctNeighbors == totalNeighbors && totalNeighbors > 0);
        
    }
    
    /// <summary>
    /// Check if a neighbor piece is in the correct relative position (Step 5 detailed logic)
    /// </summary>
    private bool IsNeighborInCorrectRelativePosition(int neighborIndex, Card neighborCard)
    {
        if (neighborCard == null) return false;
        
        Vector2 myPosition = transform.position;
        Vector2 neighborPosition = neighborCard.transform.position;
        Vector2 offset = neighborPosition - myPosition;
        
        // Get expected offset based on grid positions - use original card's dictionaries
        Vector2Int myGridPos, neighborGridPos;
        
        // Get grid positions from original card's component (where they're properly calculated)
        if (originalCard?.bigCardVisual != null)
        {
            var originalEnhanced = originalCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
            if (originalEnhanced != null && 
                originalEnhanced.pieceGridPositions.ContainsKey(pieceIndex) && 
                originalEnhanced.pieceGridPositions.ContainsKey(neighborIndex))
            {
                myGridPos = originalEnhanced.pieceGridPositions[pieceIndex];
                neighborGridPos = originalEnhanced.pieceGridPositions[neighborIndex];
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] Could not find grid positions for pieces {pieceIndex} and {neighborIndex}");
                return false;
            }
        }
        else
        {
            Debug.LogError($"[EnhancedCardVisual] Could not access original card for grid positions");
            return false;
        }
        
        Vector2Int expectedGridOffset = neighborGridPos - myGridPos;
        
        // Check distance first
        float distance = offset.magnitude;
        bool withinDistance = distance <= puzzleSettings.pieceSnapDistance;
        
        if (!withinDistance)
        {
            return false;
        }
        
        bool correctDirection = false;
        
        // Check if neighbor is in correct direction
        // Right neighbor (expectedGridOffset.x == 1)
        if (expectedGridOffset.x == 1 && expectedGridOffset.y == 0)
        {
            correctDirection = offset.x > 0 && Mathf.Abs(offset.y) < puzzleSettings.pieceSnapDistance * 0.5f;
        }
        // Left neighbor (expectedGridOffset.x == -1)
        else if (expectedGridOffset.x == -1 && expectedGridOffset.y == 0)
        {
            correctDirection = offset.x < 0 && Mathf.Abs(offset.y) < puzzleSettings.pieceSnapDistance * 0.5f;
        }
        // Bottom neighbor (expectedGridOffset.y == 1)
        else if (expectedGridOffset.x == 0 && expectedGridOffset.y == 1)
        {
            correctDirection = offset.y > 0 && Mathf.Abs(offset.x) < puzzleSettings.pieceSnapDistance * 0.5f;
        }
        // Top neighbor (expectedGridOffset.y == -1)
        else if (expectedGridOffset.x == 0 && expectedGridOffset.y == -1)
        {
            correctDirection = offset.y < 0 && Mathf.Abs(offset.x) < puzzleSettings.pieceSnapDistance * 0.5f;
        }
                
        return correctDirection;
    }
    
    /// <summary>
    /// Find piece card by its index
    /// </summary>
    private Card FindPieceCardByIndex(int index)
    {
        // For piece cards, we need to find the original card that created the pieces
        if (cardType == EnhancedCardType.Piece && originalCard?.bigCardVisual != null)
        {
            var originalEnhanced = originalCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
            if (originalEnhanced != null)
            {
                foreach (var pieceCard in originalEnhanced.activePieceCards)
                {
                    if (pieceCard?.bigCardVisual != null)
                    {
                        var pieceEnhanced = pieceCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
                        if (pieceEnhanced != null && pieceEnhanced.pieceIndex == index)
                        {
                            return pieceCard;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get expected offset between two pieces
    /// </summary>
    private Vector2Int GetExpectedOffset(int fromIndex, int toIndex)
    {
        Vector2Int fromPos = pieceGridPositions[fromIndex];
        Vector2Int toPos = pieceGridPositions[toIndex];
        return toPos - fromPos;
    }
    
    /// <summary>
    /// Check if pieces are currently in the duster
    /// </summary>
    private bool ArePiecesInDuster()
    {
        if (activePieceCards.Count == 0) return false;
        
        // Check if any piece is in a duster hand
        foreach (var piece in activePieceCards)
        {
            if (piece?.parentHolder != null && piece.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get the duster system that contains the pieces
    /// </summary>
    private FingerPrintDusterSystem GetDusterSystem()
    {
        if (activePieceCards.Count == 0) return null;
        
        foreach (var piece in activePieceCards)
        {
            if (piece?.parentHolder != null && piece.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
            {
                // Find the duster system that owns this cardSlot
                var dusterSystems = FindObjectsByType<FingerPrintDusterSystem>(FindObjectsSortMode.None);
                foreach (var dusterSystem in dusterSystems)
                {
                    if (dusterSystem.CardSlot == piece.parentHolder)
                    {
                        return dusterSystem;
                    }
                }
            }
        }
        return null;
    }

    
    /// <summary>
    /// Check if all pieces are in correct positions and complete puzzle if so
    /// </summary>
    private void CheckPuzzleCompletion()
    {
        if (isPuzzleComplete || isCompletionSequenceRunning || cardType == EnhancedCardType.Piece) 
        {
            return;
        }

        // Check if all pieces are in correct positions
        bool allPiecesCorrect = true;
        int correctPieces = 0;
        
        foreach (var pieceCard in activePieceCards)
        {
            if (pieceCard?.bigCardVisual != null)
            {
                var pieceEnhanced = pieceCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
                if (pieceEnhanced != null)
                {
                    // Force a position check
                    pieceEnhanced.CheckPiecePosition();
                    
                    if (pieceEnhanced.isInCorrectPosition)
                    {
                        correctPieces++;
                    }
                    else
                    {
                        allPiecesCorrect = false;
                    }
                }
                else
                {
                    allPiecesCorrect = false;
                }
            }
            else
            {
                allPiecesCorrect = false;
            }
        }
        
        Debug.Log($"[PUZZLE] Completion check: {correctPieces}/{activePieceCards.Count} pieces correct");
        
        if (allPiecesCorrect && correctPieces == activePieceCards.Count && correctPieces > 0)
        {
            Debug.Log("[PUZZLE] ✨ All pieces correct - starting completion sequence!");
            isPuzzleComplete = true;
            isCompletionSequenceRunning = true;
            
            // Block dragging on all pieces
            foreach (var pieceCard in activePieceCards)
            {
                if (pieceCard != null)
                {
                    var imageComponent = pieceCard.GetComponent<Image>();
                    if (imageComponent != null)
                    {
                        imageComponent.raycastTarget = false;
                    }
                }
            }
            
            StartCoroutine(CompletePuzzle());
        }
        else
        {
            Debug.Log($"[PUZZLE] Not all pieces correct yet - allCorrect: {allPiecesCorrect}, count: {correctPieces}");
        }
    }
    
    /// <summary>
    /// Complete the puzzle - glow pieces then restore original card
    /// </summary>
    private IEnumerator CompletePuzzle()
    {
        Debug.Log("[PUZZLE] CompletePuzzle coroutine started!");
        
        // First position the original card and its visuals while they're still hidden
        if (originalCard != null)
        {
            // Keep everything disabled for now
            originalCard.gameObject.SetActive(false);
            if (originalCard.bigCardVisual != null)
            {
                originalCard.bigCardVisual.ShowCard(false);
            }
            
            // Find the center piece (index 4 in a 3x3 grid)
            Card centerPiece = null;
            foreach (var piece in activePieceCards)
            {
                if (piece?.bigCardVisual != null)
                {
                    var pieceEnhanced = piece.bigCardVisual.GetComponent<EnhancedCardVisual>();
                    if (pieceEnhanced != null && pieceEnhanced.pieceIndex == 4)
                    {
                        centerPiece = piece;
                        break;
                    }
                }
            }

            Vector3 targetPosition;
            if (centerPiece != null)
            {
                // Use center piece's exact position
                targetPosition = centerPiece.transform.position;
                Debug.Log($"[PUZZLE] Using center piece position for original card: {targetPosition}");
                completionTargetWorldPosition = targetPosition;
            }
            else
            {
                // Fallback: Calculate average position of all pieces
                Vector3 averagePosition = Vector3.zero;
                int validPieces = 0;
                foreach (var piece in activePieceCards)
                {
                    if (piece != null)
                    {
                        averagePosition += piece.transform.position;
                        validPieces++;
                    }
                }
                targetPosition = validPieces > 0 ? averagePosition / validPieces : Vector3.zero;
                Debug.Log($"[PUZZLE] Using average position for original card: {targetPosition}");
                completionTargetWorldPosition = targetPosition;
            }
            
            // Position both card slot and big card visual at target position
            if (originalCard.transform.parent != null)
            {
                originalCard.transform.parent.position = targetPosition;
                Debug.Log($"[PUZZLE] Set original card slot position to: {targetPosition}");
            }
            originalCard.transform.position = targetPosition;
            
            if (originalCard.bigCardVisual != null)
            {
                originalCard.bigCardVisual.transform.position = targetPosition;
                Debug.Log($"[PUZZLE] Set original card BigCardVisual position to: {targetPosition}");
            }
            
            // Move the original card to the mat hand BEFORE pieces are destroyed
            // This ensures it appears in the right place when pieces fade out
            if (ArePiecesInDuster())
            {
                // Pieces are in duster - add original card to duster
                var dusterSystem = GetDusterSystem();
                if (dusterSystem != null)
                {
                    Debug.Log("[PUZZLE] Moving original card to duster before completion");
                    
                    // Remove from any current holder
                    if (originalCard.parentHolder != null)
                    {
                        originalCard.parentHolder.RemoveCard(originalCard);
                    }
                    
                    // Add to duster's cardSlot
                    dusterSystem.CardSlot.AddCardToHand(originalCard);
                    
                    // Set card location to Mat for bigCardVisual display
                    originalCard.cardLocation = CardLocation.Mat;
                    
                    // Ensure the card stays hidden during animation
                    originalCard.gameObject.SetActive(false);
                    if (originalCard.bigCardVisual != null)
                    {
                        originalCard.bigCardVisual.ShowCard(false);
                    }
                }
            }
            else
            {
                // Pieces are in mat - add original card to mat hand
                if (DragManager.Instance?.matHand != null)
                {
                    Debug.Log("[PUZZLE] Moving original card to mat hand before completion");
                    
                    // Remove from any current holder
                    if (originalCard.parentHolder != null)
                    {
                        originalCard.parentHolder.RemoveCard(originalCard);
                    }
                    
                    // Add to mat hand at target position
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(completionTargetWorldPosition ?? targetPosition);
                    if (clampScatterToMat)
                    {
                        screenPos = ClampToMatScreenBounds(screenPos);
                    }
                    DragManager.Instance.matHand.AddCardToHandAtPosition(originalCard, screenPos);
                    // Immediately place the slot exactly at the center-piece world position to prevent mid-mat interaction
                    PlaceCardSlotAtWorldPosition(DragManager.Instance.matHand, originalCard, completionTargetWorldPosition ?? targetPosition);
                    // Disable interaction during transition
                    DisableOriginalCardInteraction();
                    
                    // Ensure the card stays hidden during animation
                    originalCard.gameObject.SetActive(false);
                    if (originalCard.bigCardVisual != null)
                    {
                        originalCard.bigCardVisual.ShowCard(false);
                    }
                }
            }
        }

        // Make all pieces glow and prepare for convergence
        List<Outline> outlines = new List<Outline>();
        Dictionary<Card, Vector3> originalLocalPositions = new Dictionary<Card, Vector3>();
        Card convergenceCenterPiece = null;  // Renamed from centerPiece
        Vector3 centerLocalPos = Vector3.zero;

        Debug.Log($"[PUZZLE] Starting completion animation with {activePieceCards.Count} pieces");
        
        if (activePieceCards == null || activePieceCards.Count == 0)
        {
            Debug.LogError("[PUZZLE] No active pieces found! Skipping animation.");
            yield break;
        }

        // Setup outlines and positions for each piece
        foreach (var piece in activePieceCards)
        {
            if (piece?.bigCardVisual == null) 
            {
                Debug.LogWarning($"[PUZZLE] Piece {piece?.name ?? "null"} has no bigCardVisual!");
                continue;
            }
                
            // Store original position
            RectTransform rectTransform = piece.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalLocalPositions[piece] = rectTransform.localPosition;
                Debug.Log($"[PUZZLE] Stored original position for piece {piece.name}: {rectTransform.localPosition}");
            }
            else
            {
                Debug.LogError($"[PUZZLE] No RectTransform found on piece {piece.name}!");
            }
            
            // Find center piece (index 4)
            var pieceEnhanced = piece.bigCardVisual.GetComponent<EnhancedCardVisual>();
            if (pieceEnhanced != null && pieceEnhanced.pieceIndex == 4)
            {
                convergenceCenterPiece = piece;  // Using renamed variable
                centerLocalPos = rectTransform?.localPosition ?? Vector3.zero;
                Debug.Log($"[PUZZLE] Found center piece (index 4): {piece.name} at WORLD {piece.transform.position} LOCAL {centerLocalPos}");
            }
            
            // Get existing outline from the Sprite (not Shadow)
            // Outline: use first Image under active page (default page 0)
            var pageObj = piece.bigCardVisual.GetActivePageObject() ?? (piece.bigCardVisual.pageObjects.Count > 0 ? piece.bigCardVisual.pageObjects[0] : null);
            var displayImage = pageObj != null ? pageObj.GetComponentInChildren<Image>(true) : null;
            if (displayImage != null)
            {
                var outline = displayImage.GetComponent<Outline>();
                if (outline != null)
                {
                    var initialColor = puzzleSettings.glowColor;
                    initialColor.a = 0f;
                    outline.effectColor = initialColor;
                    outline.effectDistance = puzzleSettings.glowEffectDistance;
                    outline.enabled = true;
                    outlines.Add(outline);
                }
            }
        }
        
        Debug.Log($"[PUZZLE] Initial setup complete - Found center piece: {(convergenceCenterPiece != null ? "Yes" : "No")}, Center position: {centerLocalPos}");

        // If no center piece found, use average position
        if (convergenceCenterPiece == null)
        {
            Debug.Log("[PUZZLE] No center piece found, calculating average position...");
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var pos in originalLocalPositions.Values)
            {
                sum += pos;
                count++;
            }
            centerLocalPos = count > 0 ? sum / count : Vector3.zero;
            Debug.Log($"[PUZZLE] Calculated average center position from {count} pieces: {centerLocalPos}");
        }

        // Calculate convergence target positions
        Dictionary<Card, Vector2> targetLocalPositions = new Dictionary<Card, Vector2>();
        int convergenceCount = 0;

        // First ensure original card is positioned at center piece
        Vector2 centerAnchoredPos = Vector2.zero;
        if (convergenceCenterPiece?.transform.parent != null)
        {
            RectTransform centerSlotRect = convergenceCenterPiece.transform.parent.GetComponent<RectTransform>();
            if (centerSlotRect != null)
            {
                centerAnchoredPos = centerSlotRect.anchoredPosition;
            }
        }

        // Get original card dimensions for alignment
        Vector2 originalCardSize = Vector2.zero;
        Vector2 pieceSize = Vector2.zero;
        if (originalCard?.bigCardVisual != null)
        {
            RectTransform originalCardRect = originalCard.bigCardVisual.GetComponent<RectTransform>();
            if (originalCardRect != null)
            {
                originalCardSize = originalCardRect.sizeDelta;
            }
        }

        // Get piece size from any piece (they should all be the same size)
        if (activePieceCards.Count > 0 && activePieceCards[0]?.bigCardVisual != null)
        {
            RectTransform pieceRect = activePieceCards[0].bigCardVisual.GetComponent<RectTransform>();
            if (pieceRect != null)
            {
                pieceSize = pieceRect.sizeDelta;
            }
        }

        // Scale factor for canvas
        float canvasScale = 1f;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasScale = canvas.transform.localScale.x;
        }

        // Calculate piece dimensions and spacing
        float pieceWidth = pieceSize.x * canvasScale;
        float pieceHeight = pieceSize.y * canvasScale;
        
        // Calculate grid cell size (original card size divided by 3 for 3x3 grid)
        float cellWidth = originalCardSize.x * canvasScale / 3f;
        float cellHeight = originalCardSize.y * canvasScale / 3f;

        // Calculate starting corner (top-left) of the original card relative to center
        Vector2 topLeftCorner = centerAnchoredPos + new Vector2(-originalCardSize.x * canvasScale / 2f, originalCardSize.y * canvasScale / 2f);

        // Calculate target positions for each piece based on grid position
        foreach (var piece in activePieceCards)
        {
            if (piece == null || piece.transform.parent == null) continue;

            var pieceEnhanced = piece.bigCardVisual?.GetComponent<EnhancedCardVisual>();
            if (pieceEnhanced == null) continue;

            RectTransform slotRect = piece.transform.parent.GetComponent<RectTransform>();
            if (slotRect == null) continue;

            // Skip center piece (piece 5 in 3x3 grid) as it's already aligned
            if (pieceEnhanced.pieceIndex == 4) continue;

            // Calculate grid position (0-2 for x and y)
            int gridX = pieceEnhanced.pieceIndex % 3;
            int gridY = pieceEnhanced.pieceIndex / 3;

            // Calculate target position for this grid cell
            Vector2 targetAnchoredPos = topLeftCorner + new Vector2(
                gridX * cellWidth + pieceWidth/2f,
                -gridY * cellHeight - pieceHeight/2f
            );

            targetLocalPositions[piece] = targetAnchoredPos;
            convergenceCount++;
        }

        // Run animation if we have outlines and positions
        if (outlines.Count > 0 && targetLocalPositions.Count > 0)
        {
            float duration = puzzleSettings.glowUpDuration;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float alpha = Mathf.Lerp(0f, puzzleSettings.glowMaxAlpha, progress);
                float moveProgress = Mathf.SmoothStep(0, 1, progress);
                
                // Update outline glow
                foreach (var outline in outlines)
                {
                    if (outline != null)
                    {
                        Color glowColor = puzzleSettings.glowColor;
                        glowColor.a = alpha;
                        outline.effectColor = glowColor;
                    }
                }
                
                // Update piece slot positions
                foreach (var kvp in targetLocalPositions)
                {
                    Card piece = kvp.Key;
                    Vector2 targetPos = kvp.Value;
                    
                    if (piece?.transform.parent == null) continue;
                    
                    RectTransform slotRect = piece.transform.parent.GetComponent<RectTransform>();
                    if (slotRect != null)
                    {
                        Vector2 startPos = slotRect.anchoredPosition;
                        Vector2 newPos = Vector2.Lerp(startPos, targetPos, moveProgress);
                        slotRect.anchoredPosition = newPos;
                    }
                }
                
                yield return null;
            }
            
            yield return new WaitForSeconds(puzzleSettings.glowHoldDuration);
        }
        else
        {
            Debug.LogError($"[PUZZLE] Animation skipped - Outlines: {outlines.Count}, Positions: {targetLocalPositions.Count}");
        }

        // Store final positions for fade out
        Dictionary<Card, Vector2> finalSlotPositions = new Dictionary<Card, Vector2>();
        foreach (var piece in activePieceCards)
        {
            if (piece?.transform.parent != null)
            {
                RectTransform slotRect = piece.transform.parent.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    finalSlotPositions[piece] = slotRect.anchoredPosition;
                }
            }
        }

        // Prepare original card for fade in
        if (originalCard != null)
        {
            // Restore original card visuals first (keep big visual hidden until fade-in)
            if (originalCard.bigCardVisual != null)
            {
                originalCard.bigCardVisual.ShowCard(false);
            }
            
            // Change the small card view to show the joined card sprite
            if (originalCard.cardVisual != null)
            {
                var cardImage = originalCard.cardVisual.GetCardImage();
                if (cardImage != null && small_JoinedCard != null)
                {
                    cardImage.sprite = small_JoinedCard;
                    Debug.Log("[EnhancedCardVisual] Changed small card view to joined card sprite");
                }
            }
            
            // Enable card slot first
            if (originalCard.transform.parent != null)
            {
                originalCard.transform.parent.gameObject.SetActive(true);
            }
            
            // Enable card but start fully transparent
            originalCard.gameObject.SetActive(true);
            
            // Add fade in effect to original card
            var originalCanvasGroup = originalCard.bigCardVisual.GetComponent<CanvasGroup>();
            if (originalCanvasGroup == null)
            {
                originalCanvasGroup = originalCard.bigCardVisual.gameObject.AddComponent<CanvasGroup>();
            }
            originalCanvasGroup.alpha = 0f;
            
            // Add fade out effect to pieces
            Dictionary<Card, CanvasGroup> pieceCanvasGroups = new Dictionary<Card, CanvasGroup>();
            foreach (var piece in activePieceCards)
            {
                if (piece?.bigCardVisual != null)
                {
                    var pieceCanvasGroup = piece.bigCardVisual.GetComponent<CanvasGroup>();
                    if (pieceCanvasGroup == null)
                    {
                        pieceCanvasGroup = piece.bigCardVisual.gameObject.AddComponent<CanvasGroup>();
                    }
                    pieceCanvasGroup.alpha = 1f;
                    pieceCanvasGroups[piece] = pieceCanvasGroup;
                }
            }
            
            // Fade out pieces while maintaining slot positions
            float elapsed = 0f;
            float transitionDuration = puzzleSettings.glowDownDuration;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = elapsed / transitionDuration;
                
                // Fade in original card
                if (originalCanvasGroup != null)
                {
                    originalCanvasGroup.alpha = alpha;
                }
                
                // Fade out pieces and maintain positions
                foreach (var kvp in pieceCanvasGroups)
                {
                    var piece = kvp.Key;
                    var pieceCanvasGroup = kvp.Value;
                    
                    if (piece != null && piece.transform.parent != null && pieceCanvasGroup != null)
                    {
                        // Maintain the slot position
                        if (finalSlotPositions.ContainsKey(piece))
                        {
                            RectTransform slotRect = piece.transform.parent.GetComponent<RectTransform>();
                            if (slotRect != null)
                            {
                                slotRect.anchoredPosition = finalSlotPositions[piece];
                            }
                        }
                        
                        pieceCanvasGroup.alpha = 1f - alpha;
                        
                        // Also fade out the glow
                        var spriteImage = piece.bigCardVisual.GetComponentInChildren<Image>();
                        if (spriteImage != null)
                        {
                            var outline = spriteImage.GetComponent<Outline>();
                            if (outline != null)
                            {
                                Color glowColor = puzzleSettings.glowColor;
                                glowColor.a = (1f - alpha) * puzzleSettings.glowMaxAlpha;
                                outline.effectColor = glowColor;
                            }
                        }
                    }
                }
                
                yield return null;
            }
            
            // Cleanup - first remove components we added
            foreach (var kvp in pieceCanvasGroups)
            {
                var piece = kvp.Key;
                var pieceCanvasGroup = kvp.Value;
                
                if (piece != null && piece.bigCardVisual != null)
                {
                    // Kill all DOTween animations on this piece
                    DOTween.Kill(piece.transform);
                    DOTween.Kill(piece.bigCardVisual.gameObject);
                    
                    // Find and remove outline from sprite image
                    var spriteImage = piece.bigCardVisual.GetComponentInChildren<Image>();
                    if (spriteImage != null)
                    {
                        var outline = spriteImage.GetComponent<Outline>();
                        if (outline != null)
                        {
                            Destroy(outline);
                        }
                    }
                    
                    if (pieceCanvasGroup != null)
                    {
                        Destroy(pieceCanvasGroup);
                    }
                }
            }
            
            // Now destroy the pieces
            foreach (var piece in activePieceCards)
            {
                if (piece != null)
                {
                    // Use DeleteCard to properly remove and destroy pieces from their hand
                    if (piece.parentHolder != null)
                    {
                        piece.parentHolder.DeleteCard(piece);
                    }
                    else
                    {
                        // Fallback: destroy directly if no parent holder
                        Destroy(piece.gameObject);
                    }
                }
            }
            activePieceCards.Clear();
            
            // Wait a frame to ensure all destroys are processed
            yield return null;
            
            // Now make the original card visible with fade-in effect AFTER the pieces are destroyed
            if (originalCard != null)
            {
                // Reinitialize the card to ensure it shows the correct data and first page
                ReinitializeCompletedCard();
                
                // Change the small card view to show the joined card sprite
                if (originalCard.cardVisual != null)
                {
                    var cardImage = originalCard.cardVisual.GetCardImage();
                    if (cardImage != null && small_JoinedCard != null)
                    {
                        cardImage.sprite = small_JoinedCard;
                    }
                }
                
                // Before fade-in, ensure original card slot and big visual are positioned exactly at completion target
                if (completionTargetWorldPosition.HasValue)
                {
                    Vector3 exactTarget = completionTargetWorldPosition.Value;
                    if (originalCard.transform.parent != null)
                    {
                        originalCard.transform.parent.position = exactTarget;
                    }
                    originalCard.transform.position = exactTarget;
                    if (originalCard.bigCardVisual != null)
                    {
                        originalCard.bigCardVisual.transform.position = exactTarget;
                    }
                }

                // Reuse the existing CanvasGroup for the final fade-in
                if (originalCanvasGroup != null)
                {
                    // Set alpha back to 0 for the fade-in
                    originalCanvasGroup.alpha = 0f;
                    
                    // Ensure small card is hidden to avoid flicker
                    if (originalCard.cardVisual != null)
                    {
                        originalCard.cardVisual.ShowCard(false);
                    }

                    // Show the card
                    originalCard.gameObject.SetActive(true);
                    if (originalCard.bigCardVisual != null)
                    {
                        originalCard.bigCardVisual.ShowCard(true);
                        
                    }
                    
                    // Fade in over time
                    float fadeDuration = puzzleSettings.glowUpDuration;
                    float fadeElapsed = 0f;
                    
                    while (fadeElapsed < fadeDuration)
                    {
                        fadeElapsed += Time.deltaTime;
                        float progress = fadeElapsed / fadeDuration;
                        originalCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
                        yield return null;
                    }
                    
                    // Ensure final alpha is 1
                    originalCanvasGroup.alpha = 1f;
                    // Re-enable interaction now that fade-in completed
                    EnableOriginalCardInteraction();
                    
                    // Now we can destroy the CanvasGroup
                    //Destroy(originalCanvasGroup);
                }
                else
                {
                    // Fallback if CanvasGroup was somehow lost
                    originalCard.gameObject.SetActive(true);
                    if (originalCard.bigCardVisual != null)
                    {
                        originalCard.bigCardVisual.ShowCard(true);
                    }
                }
                
            }
            
            // Now restore UI elements
            yield return StartCoroutine(RestoreCardUIElementsCoroutine());
        }
        
        // Mark the puzzle as completed
        isPuzzleCompleted = true;
        
    }
    
    /// <summary>
    /// Restore card UI elements (page buttons, clue buttons, etc.)
    /// </summary>
    private void RestoreCardUIElements()
    {
        StartCoroutine(RestoreCardUIElementsCoroutine());
    }
    
    /// <summary>
    /// Coroutine version of RestoreCardUIElements to handle timing issues
    /// </summary>
    private IEnumerator RestoreCardUIElementsCoroutine()
    {
        // Wait a frame to ensure any pending destroys are processed
        yield return null;
        
        if (originalCard == null || originalCard.bigCardVisual == null)
        {
            Debug.LogError("[EnhancedCardVisual] Cannot restore UI elements - original card or bigCardVisual is null");
            yield break;
        }
        
        try
    {
        // Restore page turning buttons
        var pageButtons = originalCard.bigCardVisual.GetComponentsInChildren<Button>(true);
            if (pageButtons != null)
            {
        foreach (var button in pageButtons)
        {
                    if (button != null && (button.name.Contains("Page") || button.name.Contains("page")))
            {
                button.gameObject.SetActive(true);
                    }
            }
        }
        
        // Restore clue buttons
        var clueComponents = originalCard.bigCardVisual.GetComponentsInChildren<MonoBehaviour>(true);
            if (clueComponents != null)
            {
        foreach (var component in clueComponents)
        {
                    if (component != null && component.GetType().Name.Contains("Clue"))
            {
                component.gameObject.SetActive(true);
            }
                }
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[EnhancedCardVisual] Error restoring UI elements: {e.Message}");
        }
    }
    
    /// <summary>
    /// Called when card is added to evidence hand - change visual if it's a torn card
    /// </summary>
    public void OnAddedToEvidenceHand()
    {
        
        if (cardType == EnhancedCardType.Torn && !isPuzzleCompleted)
        {
            
            // Try to find card reference if it's null (timing issue fix)
            if (originalCard == null)
            {
                FindCardReference();
            }
            
            // Debug card visual finding
            if (originalCard == null)
            {
                Debug.LogWarning($"[EnhancedCardVisual] originalCard is still null after FindCardReference(), trying with delay...");
                StartCoroutine(DelayedOnAddedToEvidenceHand());
                return;
            }
            
            if (originalCard.cardVisual == null)
            {
                Debug.LogError($"[EnhancedCardVisual] originalCard.cardVisual is null!");
                return;
            }
            
            Image cardImage = originalCard.cardVisual.GetCardImage();
            if (cardImage == null)
            {
                Debug.LogError($"[EnhancedCardVisual] No Image component found on cardVisual via GetCardImage()!");
                return;
            }
            
            // Store original sprite before changing
            originalCardVisualSprite = cardImage.sprite;
            
            // Change to enhanced small card sprite
            if (small_TornCard != null)
            {
                cardImage.sprite = small_TornCard;
            }
            else
            {
                Debug.LogError($"[EnhancedCardVisual] small_TornCard is null!");
            }
        }

    }
    
    /// <summary>
    /// Delayed version of OnAddedToEvidenceHand for timing issues
    /// </summary>
    private IEnumerator DelayedOnAddedToEvidenceHand()
    {
        
        // Wait a frame for initialization to complete
        yield return new WaitForEndOfFrame();
        
        // Try finding the reference again
        FindCardReference();
        
        if (originalCard == null)
        {
            Debug.LogError($"[EnhancedCardVisual] originalCard is still null even after delay! Cannot process torn card visual change.");
            yield break;
        }
        
        
        // Process the torn card visual change (copied from OnAddedToEvidenceHand)
        if (originalCard.cardVisual == null)
        {
            Debug.LogError($"[EnhancedCardVisual] originalCard.cardVisual is null!");
            yield break;
        }
        
        Image cardImage = originalCard.cardVisual.GetCardImage();
        if (cardImage == null)
        {
            Debug.LogError($"[EnhancedCardVisual] No Image component found on cardVisual via GetCardImage()!");
            yield break;
        }
        
        // Store original sprite before changing
        originalCardVisualSprite = cardImage.sprite;
        
        // Change to enhanced small card sprite
        if (small_TornCard != null)
        {
            cardImage.sprite = small_TornCard;
        }
        else
        {
            Debug.LogError($"[EnhancedCardVisual] small_TornCard is null!");
        }
    }
    
    /// <summary>
    /// Restore original card visual sprite (called when puzzle is completed)
    /// </summary>
    public void RestoreOriginalCardVisual()
    {
        if (originalCard?.cardVisual?.GetCardImage() != null && originalCardVisualSprite != null)
        {
            originalCard.cardVisual.GetCardImage().sprite = originalCardVisualSprite;
        }
    }
    
    /// <summary>
    /// Reinitialize the completed card to ensure it shows the correct data and first page
    /// </summary>
    private void ReinitializeCompletedCard()
    {
        if (originalCard == null)
        {
            Debug.LogError("[EnhancedCardVisual] Cannot reinitialize - originalCard is null");
            return;
        }
        
        
        // Get the original evidence data
        Evidence evidenceData = originalCard.GetEvidenceData();
        if (evidenceData == null)
        {
            Debug.LogError("[EnhancedCardVisual] Cannot reinitialize - no evidence data found");
            return;
        }
        
        // Reinitialize the card with the original evidence data
        // This ensures it shows the correct first page and data
        originalCard.Initialize(evidenceData, originalCard.transform.parent);
        
        // Ensure the big card visual is also reinitialized
        if (originalCard.bigCardVisual != null)
        {
            originalCard.bigCardVisual.Initialize(originalCard);
            
            // Force the big card visual to show page 0 (first page) for the fade-in
            // This ensures the correct page is shown during the animation
            if (originalCard.bigCardVisual is BigCardVisual bigCardVisual)
            {
                // Set to page 0 explicitly
                bigCardVisual.SetPage(0);
            }
            else
            {
                Debug.LogWarning("[EnhancedCardVisual] Big card visual is not a BigCardVisual component, cannot set page");
            }
        }
        
    }
} 

