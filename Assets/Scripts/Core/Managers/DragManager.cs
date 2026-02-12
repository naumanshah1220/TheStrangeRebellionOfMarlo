using UnityEngine;

public class DragManager : SingletonMonoBehaviour<DragManager>
{
    [Header("Refs")]
    public Camera mainCamera;
    public MatManager matManager2;
    public RectTransform matRect;
    public RectTransform fingerPrintDusterRect;
    public RectTransform spectrographRect;

    [Header("Hands")]
    public HorizontalCardHolder caseSlot;
    public HorizontalCardHolder caseHand;
    public HorizontalCardHolder evidenceHand;
    public HorizontalCardHolder matHand;
    public HorizontalCardHolder bookHand1;
    public HorizontalCardHolder bookHand2;
    public HorizontalCardHolder overseerHand;

    [Header("Card Type Management")]
    public CardTypeManager cardTypeManager;

    [Header("Case Submission")]
    public CaseSubmissionZone caseSubmissionZone;

    // Drag state
    private Card currentCard;
    public Card CurrentCard => currentCard;
    private bool isDragging;
    public bool IsDragging => isDragging;
    private bool overMat;
    private bool overFingerPrintDuster;
    private bool overSpectrograph;
    private bool droppedCardOnATool;
    private Vector3 dragOffset;
    private bool lastCanBeSubmitted;
    private bool lastOverSubmitZone;
    private bool wasOverMat;

    protected override void OnSingletonAwake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        overMat = IsPointerOverArea(mousePos, matRect);
        overFingerPrintDuster = IsPointerOverArea(mousePos, fingerPrintDusterRect);
        overSpectrograph = IsPointerOverArea(mousePos, spectrographRect);

        UpdateSubmitZoneHighlight();

        if (!isDragging || currentCard == null) return;

        bool shouldShowBigVisual = overMat || overFingerPrintDuster || overSpectrograph;
        SetCardVisualForZone(shouldShowBigVisual);
        HandleEnhancedCardHover();
        UpdateBigVisualParentDuringDrag();

        if (Input.GetMouseButtonUp(0))
            HandleDrop();
    }

    #region Drag Lifecycle

    /// <summary>
    /// Called from Card.OnBeginDrag to start a drag operation.
    /// </summary>
    public void BeginDraggingCard(Card card)
    {
        if (isDragging) return;

        isDragging = true;
        currentCard = card;
        currentCard.isDragging = true;
        droppedCardOnATool = false;

        Vector2 mousePos = Input.mousePosition;
        Vector2 cardScreenPos = mainCamera.WorldToScreenPoint(card.transform.position);
        dragOffset = mousePos - cardScreenPos;

        overMat = IsPointerOverArea(mousePos, matRect);

        if (overMat && matHand.enableFreeFormPlacement)
            matHand.ForceCardToTopLayer(currentCard);

        if (currentCard.cardVisual != null)
            currentCard.cardVisual.LiftCardVisual(currentCard, true);
        if (currentCard.bigCardVisual != null)
            currentCard.bigCardVisual.LiftCardVisual(currentCard, true);

        SetCardVisualForZone(overMat);
    }

    /// <summary>
    /// Handle the mouse-up drop event. Routes to zone-specific handlers.
    /// </summary>
    private void HandleDrop()
    {
        isDragging = false;
        currentCard.isDragging = false;

        // Prefer tool drops over zone drops, even if overlapping
        if (CheckIfToolAtDragEnd(currentCard))
        {
            currentCard = null;
            return;
        }

        if (overMat)
            HandleDropOnMat();
        else if (overFingerPrintDuster)
            HandleDropOnToolZone(HolderPurpose.FingerPrintDuster);
        else if (overSpectrograph)
            HandleDropOnToolZone(HolderPurpose.Spectrograph);
        else
            HandleDropOutsideZones();

        // Common cleanup for non-early-return paths
        ResetDragVisuals();
        ResetSubmitZoneState();
        currentCard = null;
        wasOverMat = false;
    }

    #endregion

    #region Drop Handlers

    private void HandleDropOnMat()
    {
        // Repositioning within the mat
        if (currentCard.parentHolder == matHand && matHand.enableFreeFormPlacement)
        {
            Vector2 adjustedPosition = Input.mousePosition - dragOffset;
            matHand.AddCardToHandAtPosition(currentCard, adjustedPosition);
            return;
        }

        // Case card already in caseSlot — ignore drop on mat
        if (currentCard.parentHolder == caseSlot)
            return;

        if (currentCard.parentHolder != null)
            currentCard.parentHolder.RemoveCard(currentCard);

        if (currentCard.mode == CardMode.Case)
        {
            // Cases go to CaseSlot (opening the case)
            matManager2.PlaceCase(currentCard);

            if (UIManager.Instance != null)
                UIManager.Instance.AnimateToEvidenceHand();

            if (EvidenceManager.Instance != null)
                EvidenceManager.Instance.LoadMainEvidences(currentCard.GetCaseData().evidences);
        }
        else
        {
            // Evidence / Phone / other types go on the mat
            if (HandleEnhancedCardDrop())
                return;

            if (cardTypeManager != null)
            {
                Vector2 adjustedPosition = Input.mousePosition - dragOffset;
                cardTypeManager.MoveCardToMat(currentCard, adjustedPosition);
            }
            else
            {
                DropCardOnMatFallback();
            }
        }
    }

    private void DropCardOnMatFallback()
    {
        if (currentCard.parentHolder != null)
            currentCard.parentHolder.RemoveCard(currentCard);

        if (matHand.enableFreeFormPlacement)
        {
            Vector2 adjustedPosition = Input.mousePosition - dragOffset;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                matRect, adjustedPosition, mainCamera, out _))
            {
                matHand.AddCardToHandAtPosition(currentCard, adjustedPosition);
            }
            else
            {
                matHand.AddCardToHand(currentCard);
            }
        }
        else
        {
            matHand.AddCardToHand(currentCard);
        }
    }

    /// <summary>
    /// Handle drop on a tool zone (FingerPrintDuster or Spectrograph).
    /// </summary>
    private void HandleDropOnToolZone(HolderPurpose toolPurpose)
    {
        // Card already on this tool — reposition to center
        if (currentCard.parentHolder != null && currentCard.parentHolder.purpose == toolPurpose)
        {
            Vector3 center = currentCard.parentHolder.transform.position;
            if (currentCard.transform.parent != null)
                currentCard.transform.parent.position = center;
            return;
        }

        // Try tool acceptance
        if (CheckIfToolAtDragEnd(currentCard))
        {
            droppedCardOnATool = true;
            return;
        }

        // Tool rejected card — send back
        SendCardBackToHand(currentCard);
    }

    private void HandleDropOutsideZones()
    {
        // Case card submission check
        if (currentCard.mode == CardMode.Case && TrySubmitCase())
            return;

        // Tool check (e.g. notebook, interrogation drop zones)
        if (CheckIfToolAtDragEnd(currentCard))
        {
            SetCardVisualForZone(true);
            ResetDragVisuals();
            ResetSubmitZoneState();
            currentCard = null;
            wasOverMat = false;
            return;
        }

        // Repositioning within same hand or tool
        if (TryRepositionInCurrentHolder())
            return;

        // Default: send card back to its home hand
        ReturnCardToHome();
    }

    private bool TrySubmitCase()
    {
        bool isOverSubmitZone = false;
        bool canSubmit = currentCard.canBeSubmitted;

        if (caseSubmissionZone != null)
        {
            isOverSubmitZone = caseSubmissionZone.IsPointerOverZone(Input.mousePosition, mainCamera);
        }
        else
        {
            GameObject submitZoneGO = GameObject.Find("SubmitCaseZone");
            if (submitZoneGO != null)
            {
                RectTransform submitRect = submitZoneGO.GetComponent<RectTransform>();
                if (submitRect != null)
                    isOverSubmitZone = IsPointerOverArea(Input.mousePosition, submitRect);
            }
        }

        if (isOverSubmitZone && canSubmit)
        {
            if (caseSubmissionZone != null)
                caseSubmissionZone.OnCaseDropped(currentCard);

            currentCard.parentHolder.DeleteCard(currentCard);
            currentCard.canBeSubmitted = false;
            GameManager.Instance.OnCaseSubmitted(currentCard);

            // Early cleanup — skip common HandleDrop cleanup
            currentCard = null;
            return true;
        }

        if (!isOverSubmitZone && canSubmit)
            SetCardVisualForZone(true);

        return false;
    }

    /// <summary>
    /// Check if the card should be repositioned within its current holder
    /// (e.g. dragging within the evidence hand, or within a tool slot).
    /// </summary>
    private bool TryRepositionInCurrentHolder()
    {
        if (currentCard.parentHolder == null) return false;

        bool isInSameHand =
            (currentCard.mode == CardMode.Evidence && currentCard.parentHolder == evidenceHand) ||
            (currentCard.mode == CardMode.Case && currentCard.parentHolder == caseHand);
        bool isOnTool =
            currentCard.parentHolder.purpose == HolderPurpose.FingerPrintDuster ||
            currentCard.parentHolder.purpose == HolderPurpose.Spectrograph;

        if (!isInSameHand && !isOnTool) return false;

        Vector2 adjustedPosition = Input.mousePosition - dragOffset;

        // Tool repositioning: check if mouse is still over the tool
        if (isOnTool)
        {
            bool shouldStay = IsToolStillHovered(currentCard.parentHolder.purpose);

            if (shouldStay)
            {
                // Reposition to tool center
                Vector3 center = currentCard.parentHolder.transform.position;
                if (currentCard.transform.parent != null)
                    currentCard.transform.parent.position = center;
                return true;
            }
            else
            {
                // Dropped outside tool — fall through to ReturnCardToHome
                return false;
            }
        }

        // Regular hand repositioning
        currentCard.parentHolder.AddCardToHandAtPosition(currentCard, adjustedPosition);
        return true;
    }

    private void ReturnCardToHome()
    {
        if (currentCard.mode == CardMode.Case)
        {
            if (caseSlot != null)
            {
                if (currentCard.parentHolder != null)
                    currentCard.parentHolder.RemoveCard(currentCard);
                caseSlot.AddCardToHand(currentCard);
            }
            else
            {
                SendCardBackToHand(currentCard);
            }
        }
        else
        {
            SendCardBackToHand(currentCard);
        }
    }

    #endregion

    #region Visual Parent Management During Drag

    /// <summary>
    /// Reparent the BigCardVisual to the correct handler based on which zone the card is over.
    /// This ensures the big visual appears within the correct masked viewport.
    /// </summary>
    private void UpdateBigVisualParentDuringDrag()
    {
        if (currentCard == null || currentCard.bigCardVisual == null) return;

        if (overMat)
        {
            ReparentBigVisualForMat();
        }
        else if (overFingerPrintDuster)
        {
            ReparentBigVisualForTool("FingerPrintDuster");
        }
        else if (overSpectrograph)
        {
            ReparentBigVisualForTool("Spectrograph");
        }
        else
        {
            ReparentBigVisualToOrigin();
        }
    }

    private void ReparentBigVisualForMat()
    {
        // Case cards in caseSlot keep their visual in caseSlot's handler
        if (currentCard.mode == CardMode.Case && currentCard.parentHolder == caseSlot)
        {
            ReparentBigVisualTo(currentCard.parentHolder);
        }
        else
        {
            ReparentBigVisualTo(matHand);
        }
    }

    private void ReparentBigVisualForTool(string toolId)
    {
        // Only evidence cards show big visual on tools
        if (currentCard.mode != CardMode.Evidence)
        {
            ReparentBigVisualTo(currentCard.parentHolder);
            return;
        }

        HorizontalCardHolder toolSlot = FindToolCardSlot(toolId);
        ReparentBigVisualTo(toolSlot != null ? toolSlot : matHand);
    }

    private void ReparentBigVisualToOrigin()
    {
        ReparentBigVisualTo(currentCard.parentHolder);

        // Also reparent small visual back to origin
        if (currentCard.cardVisual != null)
        {
            Transform targetHandler = currentCard.parentHolder.visualHandler;
            if (currentCard.cardVisual.transform.parent != targetHandler)
                currentCard.cardVisual.transform.SetParent(targetHandler, true);
        }
    }

    private void ReparentBigVisualTo(HorizontalCardHolder holder)
    {
        Transform targetBigHandler = holder.bigVisualHandler != null
            ? holder.bigVisualHandler : holder.visualHandler;

        if (currentCard.bigCardVisual.transform.parent != targetBigHandler)
            currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
    }

    #endregion

    #region Tool Lookup

    /// <summary>
    /// Find the card slot for a tool by its tool ID. Replaces scattered inline lookups and reflection.
    /// </summary>
    private HorizontalCardHolder FindToolCardSlot(string toolId)
    {
        if (ToolsManager.Instance == null) return null;

        foreach (var tool in ToolsManager.Instance.tools)
        {
            if (tool == null || tool.toolId != toolId) continue;

            if (toolId == "FingerPrintDuster")
            {
                var duster = tool.GetComponent<FingerPrintDusterSystem>();
                return duster != null ? duster.CardSlot : null;
            }

            if (toolId == "Spectrograph")
            {
                var spectro = tool.GetComponent<SpectrographSystem>();
                return spectro != null ? spectro.CardSlot : null;
            }
        }

        return null;
    }

    /// <summary>
    /// Check if a tool is still being hovered (for repositioning decisions).
    /// </summary>
    private bool IsToolStillHovered(HolderPurpose toolPurpose)
    {
        if (ToolsManager.Instance == null) return false;

        string toolId = toolPurpose == HolderPurpose.FingerPrintDuster ? "FingerPrintDuster" : "Spectrograph";

        foreach (var tool in ToolsManager.Instance.tools)
        {
            if (tool != null && tool.toolId == toolId)
                return tool.isHovering;
        }

        return false;
    }

    #endregion

    #region Tool Drop Check

    public bool CheckIfToolAtDragEnd(Card card)
    {
        Tool dropTarget = null;

        foreach (var tool in ToolsManager.Instance.tools)
        {
            if (tool == null || !tool.isHovering) continue;

            // Card already on this tool — treat as repositioning, not new drop
            if (tool.toolId == "FingerPrintDuster" && card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
                return false;
            if (tool.toolId == "Spectrograph" && card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.Spectrograph)
                return false;

            if (tool.CanAcceptCard(card))
            {
                dropTarget = tool;
                break;
            }
        }

        if (dropTarget != null)
        {
            Debug.Log($"[DragManager] Card '{card.name}' dropped on tool '{dropTarget.displayName}'");
            ToolsManager.Instance.OnEvidenceDroppedOnTool(dropTarget, card);
            droppedCardOnATool = true;
        }

        return droppedCardOnATool;
    }

    #endregion

    #region Card Routing

    private void SendCardBackToHand(Card card)
    {
        if (card.parentHolder != null)
            card.parentHolder.RemoveCard(card);

        if (card.homeHand != null)
        {
            card.homeHand.AddCardToHand(card);
            return;
        }

        if (cardTypeManager != null)
        {
            cardTypeManager.AddCardToAppropriateHand(card);
        }
        else
        {
            Debug.LogWarning("[DragManager] CardTypeManager is null — falling back to evidenceHand");
            if (evidenceHand != null)
                evidenceHand.AddCardToHand(card);
        }
    }

    #endregion

    #region Visual Helpers

    /// <summary>
    /// Toggle between CardVisual (small) and BigCardVisual (large) based on zone.
    /// </summary>
    private void SetCardVisualForZone(bool pointerOverMat)
    {
        if (currentCard == null) return;

        // Tools only show big visual for evidence cards
        if (overFingerPrintDuster || overSpectrograph)
        {
            currentCard.ToggleFullView(currentCard.mode == CardMode.Evidence);
        }
        else if (pointerOverMat)
        {
            currentCard.ToggleFullView(true);
        }
        else
        {
            currentCard.ToggleFullView(false);
        }
    }

    private void ResetDragVisuals()
    {
        if (currentCard == null) return;

        if (currentCard.cardVisual != null)
            currentCard.cardVisual.LiftCardVisual(currentCard, false);
        if (currentCard.bigCardVisual != null)
            currentCard.bigCardVisual.LiftCardVisual(currentCard, false);

        if (currentCard.parentHolder != null)
            currentCard.parentHolder.UpdateCardSortingOrder(currentCard);
    }

    private void ResetSubmitZoneState()
    {
        if (caseSubmissionZone != null)
            caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.Normal);
    }

    private void UpdateSubmitZoneHighlight()
    {
        if (currentCard == null || currentCard.mode != CardMode.Case) return;

        bool currentOverSubmitZone = IsMouseOverSubmitZone();

        if (currentCard.canBeSubmitted == lastCanBeSubmitted && currentOverSubmitZone == lastOverSubmitZone)
            return;

        lastCanBeSubmitted = currentCard.canBeSubmitted;
        lastOverSubmitZone = currentOverSubmitZone;

        if (caseSubmissionZone == null) return;

        if (currentCard.canBeSubmitted && currentOverSubmitZone)
            caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.CanAccept);
        else if (currentOverSubmitZone)
            caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.DragHover);
        else
            caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.Normal);
    }

    #endregion

    #region Enhanced Card Visual

    private void HandleEnhancedCardHover()
    {
        if (currentCard == null) return;

        EnhancedCardVisual enhancedCard = GetEnhancedCardVisual(currentCard);
        if (enhancedCard == null) return;

        bool isTornOrConnectable = enhancedCard.cardType == EnhancedCardType.Torn ||
                                    enhancedCard.cardType == EnhancedCardType.Connectable;
        if (!isTornOrConnectable) return;

        if (overMat && !wasOverMat)
            enhancedCard.OnHoveringOverMat();
        else if (!overMat && wasOverMat)
            enhancedCard.OnExitingMat();

        wasOverMat = overMat;
    }

    /// <summary>
    /// Handle drop logic for EnhancedCardVisual (torn/connectable cards).
    /// Returns true if the drop was consumed.
    /// </summary>
    private bool HandleEnhancedCardDrop()
    {
        if (currentCard == null) return false;

        EnhancedCardVisual enhancedCard = GetEnhancedCardVisual(currentCard);
        if (enhancedCard == null) return false;

        if (enhancedCard.cardType != EnhancedCardType.Torn && enhancedCard.cardType != EnhancedCardType.Connectable)
            return false;

        Vector2 adjustedPosition = Input.mousePosition - dragOffset;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            matRect, adjustedPosition, mainCamera, out Vector2 localPoint))
        {
            return false;
        }

        Vector3 worldPos = matRect.TransformPoint(localPoint);

        if (currentCard.parentHolder != null)
            currentCard.parentHolder.RemoveCard(currentCard);

        enhancedCard.OnDroppedOnMat(worldPos);
        return true;
    }

    private EnhancedCardVisual GetEnhancedCardVisual(Card card)
    {
        var enhanced = card.GetComponent<EnhancedCardVisual>();
        if (enhanced == null && card.bigCardVisual != null)
            enhanced = card.bigCardVisual.GetComponent<EnhancedCardVisual>();
        return enhanced;
    }

    #endregion

    #region Utility

    private bool IsPointerOverArea(Vector2 screenPos, RectTransform area)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, mainCamera, out var localPoint);
        return area.rect.Contains(localPoint);
    }

    private bool IsMouseOverSubmitZone()
    {
        return caseSubmissionZone != null && caseSubmissionZone.IsPointerOverZone(Input.mousePosition, mainCamera);
    }

    #endregion
}
