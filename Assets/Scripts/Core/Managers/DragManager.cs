using UnityEngine;
using System.Collections;

public class DragManager : MonoBehaviour
{
    public static DragManager Instance;

    [Header("Refs")]
    public Camera mainCamera;
    public MatManager matManager2;
    public RectTransform matRect; // The mat area for detecting drags
    public RectTransform fingerPrintDusterRect; // The FingerPrintDuster area for detecting drags
    public RectTransform spectrographRect; // The Spectrograph area for detecting drags

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

    [Header("State")]
    public Card currentCard;
    public bool isDragging = false;
    public bool overMat = false;
    public bool overFingerPrintDuster = false;
    public bool overSpectrograph = false;
    public bool overSubmitZone = false;
    public bool droppedCardOnATool = false;
    private Vector3 dragOffset; // Store the offset from card center when dragging starts
    private bool lastCanBeSubmitted = false;
    private bool lastOverSubmitZone = false;

    void Awake()
    {
        Instance = this;
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        overMat = IsPointerOverArea(mousePos, matRect);
        overFingerPrintDuster = IsPointerOverArea(mousePos, fingerPrintDusterRect);
        overSpectrograph = IsPointerOverArea(mousePos, spectrographRect);

        // Handle case cards that can be submitted
        if (currentCard != null && currentCard.mode == CardMode.Case)
        {
            bool overSubmitZone = IsMouseOverSubmitZone();

            // Only update visual state if something changed to avoid spam
            if (currentCard.canBeSubmitted != lastCanBeSubmitted || overSubmitZone != lastOverSubmitZone)
            {
                lastCanBeSubmitted = currentCard.canBeSubmitted;
                lastOverSubmitZone = overSubmitZone;

                // Update visual state of case submission zone if available
                if (caseSubmissionZone != null)
                {
                    if (currentCard.canBeSubmitted && overSubmitZone)
                    {
                        caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.CanAccept);
                    }
                    else if (overSubmitZone)
                    {
                        caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.DragHover);
                    }
                    else
                    {
                        caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.Normal);
                    }
                }
            }
        }

        if (!isDragging || currentCard == null) return;

        // Toggle visual as you move between hand and mat
        // For case cards, also show big visual when over submit zone
        // Show big visual if over mat area, submit zone for cases, or FingerPrintDuster
        bool shouldShowBigVisual = overMat || overFingerPrintDuster || overSpectrograph;

        SetCardVisualForZone(shouldShowBigVisual);

        // Handle EnhancedCardVisual hover effects
        HandleEnhancedCardHover();


        if (overMat)
        {
            // Special case: If dragging a case card from caseSlot, don't move its visual to matHand
            if (currentCard.mode == CardMode.Case && currentCard.parentHolder == caseSlot)
            {
                // Keep case visual in its own caseSlot handler - don't move to matHand
                Transform targetBigHandler = currentCard.parentHolder.bigVisualHandler != null ?
                    currentCard.parentHolder.bigVisualHandler : currentCard.parentHolder.visualHandler;
                if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                    currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
            }
            else
            {
                // Put the card's big visual to the mat's big visual handler (for evidence, phone, etc.)
                Transform targetBigHandler = matHand.bigVisualHandler != null ? matHand.bigVisualHandler : matHand.visualHandler;
                if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                    currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
            }
        }
        else if (overFingerPrintDuster)
        {
            // Only show bigCardVisual for evidence cards, not case cards
            if (currentCard.mode == CardMode.Evidence)
            {
                // Find the FingerPrintDuster system through ToolsManager
                FingerPrintDusterSystem fingerPrintDusterSystem = null;
                if (ToolsManager.Instance != null)
                {
                    foreach (var tool in ToolsManager.Instance.tools)
                    {
                        if (tool != null && tool.toolId == "FingerPrintDuster")
                        {
                            fingerPrintDusterSystem = tool.GetComponent<FingerPrintDusterSystem>();
                            break;
                        }
                    }
                }
                
                // Use the FingerPrintDuster's cardSlot visual handlers if available
                if (fingerPrintDusterSystem != null && fingerPrintDusterSystem.CardSlot != null)
                {
                    Transform targetBigHandler = fingerPrintDusterSystem.CardSlot.bigVisualHandler != null ? 
                        fingerPrintDusterSystem.CardSlot.bigVisualHandler : fingerPrintDusterSystem.CardSlot.visualHandler;
                    if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                        currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
                }
                else
                {
                    // Fallback to matHand's visual handlers if FingerPrintDuster system not found
                    Transform targetBigHandler = matHand.bigVisualHandler != null ? matHand.bigVisualHandler : matHand.visualHandler;
                    if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                        currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
                }
            }
            else
            {
                // For non-evidence cards (like case cards), keep bigCardVisual in original hand
                Transform targetBigHandler = currentCard.parentHolder.bigVisualHandler != null ? 
                    currentCard.parentHolder.bigVisualHandler : currentCard.parentHolder.visualHandler;
                if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                    currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
            }
        }
        else if (overSpectrograph)
        {
            // Only show bigCardVisual for evidence cards, not case cards
            if (currentCard.mode == CardMode.Evidence)
            {
                // Find the Spectrograph system through ToolsManager via reflection
                HorizontalCardHolder spectroSlot = null;
                if (ToolsManager.Instance != null)
                {
                    foreach (var tool in ToolsManager.Instance.tools)
                    {
                        if (tool != null && tool.toolId == "Spectrograph")
                        {
                            var specComp = tool.GetComponent("SpectrographSystem");
                            if (specComp != null)
                            {
                                var cardSlotProp = specComp.GetType().GetProperty("CardSlot");
                                if (cardSlotProp != null)
                                {
                                    spectroSlot = cardSlotProp.GetValue(specComp) as HorizontalCardHolder;
                                }
                            }
                            break;
                        }
                    }
                }
                
                // Use the Spectrograph's cardSlot visual handlers if available
                if (spectroSlot != null)
                {
                    Transform targetBigHandler = spectroSlot.bigVisualHandler != null ? spectroSlot.bigVisualHandler : spectroSlot.visualHandler;
                    if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                        currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
                }
                else
                {
                    // Fallback to matHand's visual handlers if Spectrograph system not found
                    Transform targetBigHandler = matHand.bigVisualHandler != null ? matHand.bigVisualHandler : matHand.visualHandler;
                    if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                        currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
                }
            }
            else
            {
                // For non-evidence cards (like case cards), keep bigCardVisual in original hand
                Transform targetBigHandler = currentCard.parentHolder.bigVisualHandler != null ? 
                    currentCard.parentHolder.bigVisualHandler : currentCard.parentHolder.visualHandler;
                if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                    currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);
            }
        }
        else
        {
            // Bring the visuals back to the original hand's appropriate handlers
            Transform targetBigHandler = currentCard.parentHolder.bigVisualHandler != null ? currentCard.parentHolder.bigVisualHandler : currentCard.parentHolder.visualHandler;
            if (currentCard.bigCardVisual && currentCard.bigCardVisual.transform.parent != targetBigHandler)
                currentCard.bigCardVisual.transform.SetParent(targetBigHandler, true);

            if (currentCard.mode == CardMode.Case)
            {
                HorizontalCardHolder caseHand = currentCard.parentHolder;
                if (currentCard.cardVisual && currentCard.cardVisual.transform.parent != caseHand.visualHandler)
                    currentCard.cardVisual.transform.SetParent(caseHand.visualHandler, true);
            }
            else
            {
                if (currentCard.cardVisual && currentCard.cardVisual.transform.parent != currentCard.parentHolder.visualHandler)
                    currentCard.cardVisual.transform.SetParent(currentCard.parentHolder.visualHandler, true);
            }
        }

        // --- On Drop ---
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            currentCard.isDragging = false;

            // Prefer tool drops over mat, even if overlapping the mat area
            if (CheckIfToolAtDragEnd(currentCard))
            {
                currentCard = null;
                return;
            }

            if (overMat)
            {
                // Special handling for repositioning cards already on the mat
                if (currentCard.parentHolder == matHand && matHand.enableFreeFormPlacement)
                {
                    // Reposition card within the mat using adjusted screen position (accounting for offset)
                    Vector2 adjustedPosition = Input.mousePosition - dragOffset;
                    matHand.AddCardToHandAtPosition(currentCard, adjustedPosition);
                    currentCard = null;
                    return;
                }

                // Dragging to the same hand (non-repositioning case)?
                if (currentCard.parentHolder == caseSlot)
                {

                    currentCard = null;
                    return;
                }

                if (currentCard.parentHolder != null)
                    currentCard.parentHolder.RemoveCard(currentCard);

                if (currentCard.mode == CardMode.Case)
                {
                    // Cases should only go to CaseSlot, not MatHand
                    // Opening case
                    matManager2.PlaceCase(currentCard);

                    // Show the evidence hand
                    if (UIManager.Instance != null)
                        UIManager.Instance.AnimateToEvidenceHand();

                    // Load the case's evidences
                    if (EvidenceManager.Instance != null)
                    {
                        EvidenceManager.Instance.LoadMainEvidences(currentCard.GetCaseData().evidences);
                    }

                }
                else
                {
                    // Put card on Mat (Evidence, Phone, or other types - NOT Cases!)
                    if (currentCard.mode == CardMode.Case)
                    {

                        SendCardBackToHand(currentCard);
                        currentCard = null;
                        return;
                    }

                    // Handle EnhancedCardVisual drop logic
                    if (HandleEnhancedCardDrop())
                    {
                        currentCard = null;
                        return;
                    }

                    if (cardTypeManager != null)
                    {
                        // Use mouse position adjusted for drag offset
                        Vector2 adjustedPosition = Input.mousePosition - dragOffset;
                        cardTypeManager.MoveCardToMat(currentCard, adjustedPosition);
                    }
                    else
                    {
                        // Fallback to old system
                        if (currentCard.parentHolder != null)
                            currentCard.parentHolder.RemoveCard(currentCard);

                        // Use free-form placement if matHand supports it
                        if (matHand.enableFreeFormPlacement)
                        {
                            // Use mouse position adjusted for drag offset
                            Vector2 adjustedPosition = Input.mousePosition - dragOffset;
                            Vector2 localPoint;
                            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                matRect,
                                adjustedPosition,
                                mainCamera,
                                out localPoint))
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
                }
            }
            else if (overFingerPrintDuster)
            {
                // Special handling for repositioning cards already on the FingerPrintDuster
                if (currentCard.parentHolder != null && currentCard.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
                {
                    // Card is already on the duster - reposition it to center
                    Vector3 dusterCenter = currentCard.parentHolder.transform.position;
                    if (currentCard.transform.parent != null)
                    {
                        currentCard.transform.parent.position = dusterCenter;
                    }
                    
                    currentCard = null;
                    return;
                }
                
                // Handle dropping card on FingerPrintDuster
                // Check if the FingerPrintDuster can accept this card
                if (CheckIfToolAtDragEnd(currentCard))
                {
                    droppedCardOnATool = true;
                    currentCard = null;
                    return;
                }
                else
                {
                    // If FingerPrintDuster rejected the card, send it back to hand
                    SendCardBackToHand(currentCard);
                    currentCard = null;
                    return;
                }
            }
            else if (overSpectrograph)
            {
                // Special handling for repositioning cards already on the Spectrograph
                if (currentCard.parentHolder != null && currentCard.parentHolder.purpose == HolderPurpose.Spectrograph)
                {
                    // Card is already on the spectrograph - reposition it to center
                    Vector3 specCenter = currentCard.parentHolder.transform.position;
                    if (currentCard.transform.parent != null)
                    {
                        currentCard.transform.parent.position = specCenter;
                    }
                    
                    currentCard = null;
                    return;
                }
                
                // Handle dropping card on Spectrograph
                // Check if the Spectrograph can accept this card
                if (CheckIfToolAtDragEnd(currentCard))
                {
                    droppedCardOnATool = true;
                    currentCard = null;
                    return;
                }
                else
                {
                    // If Spectrograph rejected the card, send it back to hand
                    SendCardBackToHand(currentCard);
                    currentCard = null;
                    return;
                }
            }
            else // Drop back to hand (case or evidence)
            {
                // Check if dropping case card on submit zone
                if (currentCard.mode == CardMode.Case)
                {
                    bool isOverSubmitZone = false;
                    bool canSubmit = currentCard.canBeSubmitted;

                    // Try to detect submit zone in multiple ways
                    if (caseSubmissionZone != null)
                    {
                        isOverSubmitZone = caseSubmissionZone.IsPointerOverZone(Input.mousePosition, mainCamera);

                    }
                    else
                    {
                        // Fallback: try to find submit zone by name

                        GameObject submitZoneGO = GameObject.Find("SubmitCaseZone");
                        if (submitZoneGO != null)
                        {
                            RectTransform submitRect = submitZoneGO.GetComponent<RectTransform>();
                            if (submitRect != null)
                            {
                                isOverSubmitZone = IsPointerOverArea(Input.mousePosition, submitRect);

                            }
                        }
                    }



                    if (isOverSubmitZone && canSubmit)
                    {


                        // Submit the case
                        if (caseSubmissionZone != null)
                            caseSubmissionZone.OnCaseDropped(currentCard);

                        // Clean up the card
                        currentCard.parentHolder.DeleteCard(currentCard);
                        currentCard.canBeSubmitted = false;

                        // Notify GameManager
                        GameManager.Instance.OnCaseSubmitted(currentCard);

                        // Early return to prevent snap back
                        currentCard = null;
                        return;
                    }
                    else if (!isOverSubmitZone && canSubmit)
                    {
                        // Case submission failed - fall through to snap back
                        SetCardVisualForZone(true);
                    }
                }
                else if (CheckIfToolAtDragEnd(currentCard))
                {
                    SetCardVisualForZone(true);

                    // Reset card visual lift when drag ends
                    if (currentCard.cardVisual != null)
                        currentCard.cardVisual.LiftCardVisual(currentCard, false);
                    if (currentCard.bigCardVisual != null)
                        currentCard.bigCardVisual.LiftCardVisual(currentCard, false);

                    // Reset sorting order to normal after drag ends
                    if (currentCard.parentHolder != null)
                    {
                        currentCard.parentHolder.UpdateCardSortingOrder(currentCard);
                    }

                    // Reset submission zone state
                    if (caseSubmissionZone != null)
                    {
                        caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.Normal);
                    }

                    currentCard = null;
                    wasOverMat = false;
                    return; // Early return to prevent card from being sent back
                }
                        else
        {
            // Check if card is being repositioned within the same hand or duster
            if (currentCard.parentHolder != null && 
                ((currentCard.mode == CardMode.Evidence && currentCard.parentHolder == evidenceHand) ||
                 (currentCard.mode == CardMode.Case && currentCard.parentHolder == caseHand) ||
                 (currentCard.parentHolder.purpose == HolderPurpose.FingerPrintDuster) ||
                 (currentCard.parentHolder.purpose == HolderPurpose.Spectrograph)))
            {
                // Reposition card within the same hand/duster using adjusted screen position
                Vector2 adjustedPosition = Input.mousePosition - dragOffset;
                
                // Special handling for FingerPrintDuster - check if mouse is still over the duster tool
                if (currentCard.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
                {
                    // Find the FingerPrintDuster tool and check its isHovering flag
                    bool shouldStayOnDuster = false;
                    if (ToolsManager.Instance != null)
                    {
                        foreach (var tool in ToolsManager.Instance.tools)
                        {
                            if (tool != null && tool.toolId == "FingerPrintDuster")
                            {
                                shouldStayOnDuster = tool.isHovering;
                                Debug.Log($"[DragManager] FingerPrintDuster tool isHovering: {shouldStayOnDuster}");
                                break;
                            }
                        }
                    }
                    
                    if (shouldStayOnDuster)
                    {
                        // Mouse is over the duster tool - reposition card to center
                        Vector3 dusterCenter = currentCard.parentHolder.transform.position;
                        if (currentCard.transform.parent != null)
                        {
                            currentCard.transform.parent.position = dusterCenter;
                        }
                        Debug.Log($"[DragManager] Card repositioned within duster area");
                        currentCard = null;
                        return;
                    }
                    else
                    {
                        // Mouse is not over the duster tool - card should be removed from duster
                        Debug.Log($"[DragManager] Card dropped outside duster area - removing from duster");
                        // Don't return here - let it fall through to the "send back to hand" logic
                    }
                }
                else if (currentCard.parentHolder.purpose == HolderPurpose.Spectrograph)
                {
                    // Find the Spectrograph tool and check its isHovering flag
                    bool shouldStayOnSpectro = false;
                    if (ToolsManager.Instance != null)
                    {
                        foreach (var tool in ToolsManager.Instance.tools)
                        {
                            if (tool != null && tool.toolId == "Spectrograph")
                            {
                                shouldStayOnSpectro = tool.isHovering;
                                Debug.Log($"[DragManager] Spectrograph tool isHovering: {shouldStayOnSpectro}");
                                break;
                            }
                        }
                    }
                    
                    if (shouldStayOnSpectro)
                    {
                        // Mouse is over the spectro tool - reposition card to center
                        Vector3 specCenter = currentCard.parentHolder.transform.position;
                        if (currentCard.transform.parent != null)
                        {
                            currentCard.transform.parent.position = specCenter;
                        }
                        Debug.Log($"[DragManager] Card repositioned within spectrograph area");
                        currentCard = null;
                        return;
                    }
                    else
                    {
                        // Mouse is not over the spectro tool - card should be removed from spectro
                        Debug.Log($"[DragManager] Card dropped outside spectrograph area - removing from spectrograph");
                        // Don't return here - let it fall through to the "send back to hand" logic
                    }
                }
                else
                {
                    // For regular hands, use the standard repositioning method
                    currentCard.parentHolder.AddCardToHandAtPosition(currentCard, adjustedPosition);
                    currentCard = null;
                    return;
                }
            }
            
            // Send card back to its original hand (or caseSlot for case cards)
            if (currentCard.mode == CardMode.Case)
            {
                // Case cards should return to caseSlot, not caseHand
                if (caseSlot != null)
                {
                    if (currentCard.parentHolder != null)
                        currentCard.parentHolder.RemoveCard(currentCard);
                    caseSlot.AddCardToHand(currentCard);

                    // Explicitly ensure the card shows big visual when returned to case slot
                    Debug.Log($"[DragManager] Case card returned to caseSlot - ensuring big visual is shown");

                    // Don't manually call ToggleFullView - let the automatic cardLocation system handle it
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
            }

            // Reset card visual lift when drag ends
            if (currentCard != null)
            {
                if (currentCard.cardVisual != null)
                    currentCard.cardVisual.LiftCardVisual(currentCard, false);
                if (currentCard.bigCardVisual != null)
                    currentCard.bigCardVisual.LiftCardVisual(currentCard, false);

                // Reset sorting order to normal after drag ends
                if (currentCard.parentHolder != null)
                {
                    currentCard.parentHolder.UpdateCardSortingOrder(currentCard);
                }
            }

            // Reset submission zone state
            if (caseSubmissionZone != null)
            {
                caseSubmissionZone.SetHighlightState(CaseSubmissionZone.HighlightState.Normal);
            }

            currentCard = null;
            wasOverMat = false; // Reset mat hover state for next drag operation
        }
    }

    /// <summary>
    /// Call this from Card.OnBeginDrag!
    /// </summary>
    public void BeginDraggingCard(Card card)
    {
        if (isDragging) return;

        isDragging = true;
        currentCard = card;
        currentCard.isDragging = true;
        droppedCardOnATool = false;



        // Calculate drag offset to maintain grab position
        Vector2 mousePos = Input.mousePosition;
        Vector2 cardScreenPos = mainCamera.WorldToScreenPoint(card.transform.position);
        dragOffset = mousePos - cardScreenPos;



        overMat = IsPointerOverArea(mousePos, matRect);

        // Only force card to top layer if we're starting drag over the mat
        // and the mat supports free-form placement
        if (overMat && matHand.enableFreeFormPlacement)
        {
            matHand.ForceCardToTopLayer(currentCard);
        }

        // Lift card visual and bring to front
        if (currentCard.cardVisual != null)
            currentCard.cardVisual.LiftCardVisual(currentCard, true);
        if (currentCard.bigCardVisual != null)
            currentCard.bigCardVisual.LiftCardVisual(currentCard, true);

        // Bring correct visual to front and show it
        SetCardVisualForZone(overMat);
    }

    public bool CheckIfToolAtDragEnd(Card card)
    {
        Tool dropTarget = null;

        Debug.Log($"[DragManager] CheckIfToolAtDragEnd called for card '{card.name}'");
        Debug.Log($"[DragManager] ToolsManager has {ToolsManager.Instance.tools.Count} tools");

        // Find the tool currently hovered by the card
        foreach (var tool in ToolsManager.Instance.tools)
        {
            if (tool == null) continue;

            Debug.Log($"[DragManager] Checking tool '{tool.displayName}' (ID: {tool.toolId}) - isHovering: {tool.isHovering}");

            if (tool.isHovering)
            {
                Debug.Log($"[DragManager] Tool '{tool.displayName}' is hovering - checking if it can accept card");
                
                // Special handling for FingerPrintDuster: if card is already on the duster, don't treat it as a new drop
                if (tool.toolId == "FingerPrintDuster" && card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.FingerPrintDuster)
                {
                    Debug.Log($"[DragManager] Card is already on FingerPrintDuster - treating as repositioning, not new drop");
                    return false; // Don't treat as tool drop
                }
                // Special handling for Spectrograph: if card is already on the spectrograph, don't treat it as a new drop
                if (tool.toolId == "Spectrograph" && card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.Spectrograph)
                {
                    Debug.Log($"[DragManager] Card is already on Spectrograph - treating as repositioning, not new drop");
                    return false;
                }
                
                // Check if this tool can accept the card
                if (tool.CanAcceptCard(card))
                {
                    dropTarget = tool;
                    Debug.Log($"[DragManager] Tool '{tool.displayName}' can accept card - setting as drop target");
                    break;
                }
                else
                {
                    Debug.Log($"[DragManager] Tool '{tool.displayName}' cannot accept card");
                }
            }
        }

        if (dropTarget != null)
        {
            // Card dropped on a tool!
            Debug.Log($"[DragManager] Card '{card.name}' dropped on tool '{dropTarget.displayName}'");
            ToolsManager.Instance.OnEvidenceDroppedOnTool(dropTarget, card);
            droppedCardOnATool = true;
        }
        else
        {
            Debug.Log($"[DragManager] No tool found to drop card on");
        }

        return droppedCardOnATool;
    }

    private void SendCardBackToHand(Card card)
    {
        Debug.Log($"[DragManager] SendCardBackToHand called for card '{card.name}' with mode {card.mode}");
        
        // Always remove card from current holder first, regardless of card location
        // This ensures cards are properly removed from tools (like FingerPrintDuster) before being added back to hands
        if (card.parentHolder != null)
        {
            card.parentHolder.RemoveCard(card);
        }

        // 1. Prioritize returning the card to its designated "home" hand.
        if (card.homeHand != null)
        {
            Debug.Log($"[DragManager] Returning card '{card.name}' to its home hand: {card.homeHand.name}");
            card.homeHand.AddCardToHand(card);
            return;
        }

        // 2. If no home is set, use the CardTypeManager as a fallback.
        if (cardTypeManager != null)
        {
            Debug.Log($"[DragManager] Card '{card.name}' has no home. Routing via CardTypeManager.");
            cardTypeManager.AddCardToAppropriateHand(card);
        }
        else
        {
            Debug.LogError("[DragManager] CardTypeManager is null! Cannot route card back to hand.");
            // 3. As a last resort, add to the default evidence hand if it exists.
            if (evidenceHand != null)
            {
                evidenceHand.AddCardToHand(card);
            }
        }
    }

    /// Toggles between CardVisual (hand) and BigCardVisual (mat) as needed during dragging.
    private void SetCardVisualForZone(bool pointerOverMat)
    {
        if (currentCard == null) return;

        // Check if we're over the duster specifically
        if (overFingerPrintDuster)
        {
            // Only show bigCardVisual for evidence cards when over the duster
            if (currentCard.mode == CardMode.Evidence)
            {
                currentCard.ToggleFullView(true); // Show BigCardVisual
            }
            else
            {
                currentCard.ToggleFullView(false); // Show CardVisual
            }
        }
        else if (overSpectrograph)
        {
            // Only show bigCardVisual for evidence cards when over the spectrograph
            if (currentCard.mode == CardMode.Evidence)
            {
                currentCard.ToggleFullView(true); // Show BigCardVisual
            }
            else
            {
                currentCard.ToggleFullView(false); // Show CardVisual
            }
        }
        // Check if we're over the mat area (but not duster)
        else if (pointerOverMat)
        {
            currentCard.ToggleFullView(true); // Show BigCardVisual
        }
        else
        {
            currentCard.ToggleFullView(false); // Show CardVisual
        }
    }

    /// Checks if pointer is over a given RectTransform.
    private bool IsPointerOverArea(Vector2 screenPos, RectTransform area)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, mainCamera, out var localPoint);
        return area.rect.Contains(localPoint);
    }

    private bool IsMouseOverSubmitZone()
    {
        if (caseSubmissionZone != null)
        {
            return caseSubmissionZone.IsPointerOverZone(Input.mousePosition, mainCamera);
        }
        return false;
    }

    private bool wasOverMat = false; // Track previous mat hover state

    /// <summary>
    /// Handle hover effects for cards with EnhancedCardVisual
    /// </summary>
    private void HandleEnhancedCardHover()
    {
        if (currentCard == null) return;

        // Check for EnhancedCardVisual on the Card first, then on BigCardVisual
        EnhancedCardVisual enhancedCard = currentCard.GetComponent<EnhancedCardVisual>();
        if (enhancedCard == null && currentCard.bigCardVisual != null)
        {
            enhancedCard = currentCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
        }

        if (enhancedCard == null)
        {
            // No enhanced card visual - this is normal for most cards
            return;
        }

        // Handle entering mat area
        if (overMat && !wasOverMat && (enhancedCard.cardType == EnhancedCardType.Torn || enhancedCard.cardType == EnhancedCardType.Connectable))
        {

            enhancedCard.OnHoveringOverMat();
        }
        // Handle exiting mat area  
        else if (!overMat && wasOverMat && (enhancedCard.cardType == EnhancedCardType.Torn || enhancedCard.cardType == EnhancedCardType.Connectable))
        {

            enhancedCard.OnExitingMat();
        }

        // Update previous state
        wasOverMat = overMat;
    }

    /// <summary>
    /// Handle drop logic for cards with EnhancedCardVisual
    /// Returns true if the drop was handled by EnhancedCardVisual system
    /// </summary>
    private bool HandleEnhancedCardDrop()
    {
        if (currentCard == null) return false;

        // Check for EnhancedCardVisual on the Card first, then on BigCardVisual
        EnhancedCardVisual enhancedCard = currentCard.GetComponent<EnhancedCardVisual>();
        if (enhancedCard == null && currentCard.bigCardVisual != null)
        {
            enhancedCard = currentCard.bigCardVisual.GetComponent<EnhancedCardVisual>();
        }

        if (enhancedCard == null)
        {
            // No enhanced card visual - this is normal for most cards
            return false;
        }


        if (enhancedCard.cardType == EnhancedCardType.Torn || enhancedCard.cardType == EnhancedCardType.Connectable)
        {

            // Calculate drop position adjusted for drag offset
            Vector2 adjustedPosition = Input.mousePosition - dragOffset;
            Vector2 localPoint;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                matRect,
                adjustedPosition,
                mainCamera,
                out localPoint))
            {
                // Convert to world position for EnhancedCardVisual
                Vector3 worldPos = matRect.TransformPoint(localPoint);



                // Remove card from current holder before creating pieces
                if (currentCard.parentHolder != null)
                {
                    Debug.Log($"[DragManager] Removing card {currentCard.name} from holder {currentCard.parentHolder.name}");
                    currentCard.parentHolder.RemoveCard(currentCard);
                }

                // Trigger enhanced card drop logic
                enhancedCard.OnDroppedOnMat(worldPos);

                return true; // Handled by EnhancedCardVisual
            }
            else
            {
                Debug.Log($"[DragManager] Failed to convert screen position to local point");
            }
        }

        return false; // Not handled by EnhancedCardVisual
    }
}
