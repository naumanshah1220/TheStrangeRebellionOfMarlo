using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using DG.Tweening;

[RequireComponent(typeof(Tool))]
public class ComputerSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup screenSaver;
    [SerializeField] private DesktopManager desktopManager;
    [SerializeField] private HorizontalCardHolder discSlot;
    [SerializeField] private Button ejectButton;
    
    [Header("Settings")]
    [SerializeField] private float screenSaverDelay = 10f;
    [SerializeField] private float fadeSpeed = 2f;
    
    [Header("Disc Animation")]
    [SerializeField] private float discInsertionDuration = 1f;
    [SerializeField] private float discInsertionDelay = 1f; // How long to wait before hiding disc
    [SerializeField] private DG.Tweening.Ease insertionEase = DG.Tweening.Ease.OutBack;
    
    private WindowManager windowManager;
    private AppRegistry appRegistry;
    private Tool tool;
    private bool isHovering;
    private float lastInteractionTime;
    private bool wasHovering;
    private Evidence currentDisk;
    private Card currentDiscCard;
    private HorizontalCardHolder sourceHand; // Store where the disc came from
    
    // Public accessor for current disc card
    public Card CurrentDiscCard => currentDiscCard;
    private Vector3 sourcePosition; // Store original position in source hand
    private Coroutine fadeCoroutine;
    private Canvas parentCanvas;
    private Camera mainCamera;
    private RetroComputerEffects retroEffects;
    private bool isDiscInserted = false; // True when disc is fully inserted and hidden
    private Coroutine insertionAnimationCoroutine;
    private float originalSnapAngle = 5f; // Store original snap angle for restoration
    
    private void Start()
    {
        windowManager = GetComponent<WindowManager>();
        appRegistry = GetComponent<AppRegistry>();
        tool = GetComponent<Tool>();
        parentCanvas = GetComponentInParent<Canvas>();
        mainCamera = Camera.main;
        retroEffects = RetroComputerEffects.Instance;

        // Set initial hover state based on actual mouse position
        wasHovering = IsMouseOverDesktop();
        isHovering = wasHovering;

        if (screenSaver == null || windowManager == null || 
            appRegistry == null || tool == null || desktopManager == null)
        {
            Debug.LogError("ComputerSystem: Missing required references!");
            enabled = false;
            return;
        }
        
        // Configure tool to accept cards
        tool.acceptsCards = true;
        tool.acceptedCardTypes = new CardMode[] { CardMode.Evidence };
        tool.toolId = "Computer";
        tool.displayName = "Computer";
        
        // Ensure disc slot is configured as Computer type, Evidence mode
        if (discSlot != null)
        {
            discSlot.purpose = HolderPurpose.Computer;
            discSlot.acceptedCardTypes = AcceptedCardTypes.Evidence;
        }
        
        // Set up eject button
        if (ejectButton != null)
        {
            ejectButton.onClick.AddListener(EjectDisc);
            ejectButton.gameObject.SetActive(false); // Hidden when no disc
        }
        
        InitializeSystem();

        // Force correct cursor state on first frame
        if (isHovering)
        {
            OnComputerEnter();
        }
        else
        {
            OnComputerExit();
        }
    }
    

    
    private void InitializeSystem()
    {
        // Initialize components
        windowManager.Initialize(desktopManager.GetComponent<RectTransform>());
        appRegistry.LoadDefaultApps();
        
        // Register default viewer apps
        RegisterDefaultViewerApps();
        
        // Setup initial states
        screenSaver.alpha = 1f;
        Cursor.visible = true;
    }
    
    /// <summary>
    /// Register default viewer apps for different file types
    /// </summary>
    private void RegisterDefaultViewerApps()
    {
        Debug.Log("[ComputerSystem] RegisterDefaultViewerApps called");
        
        // Find viewer apps in the default apps list
        foreach (var app in appRegistry.GetRegisteredApps())
        {
            Debug.Log($"[ComputerSystem] Checking app: {app.AppName} (Hidden: {app.IsHidden}, Desktop: {app.ShouldShowOnDesktop}, Menu: {app.ShouldShowInMenu})");
            
            if (app.IsHidden)
            {
                // Register based on app name (you can make this more sophisticated)
                if (app.AppName.Contains("Image") || app.AppName.Contains("Photo"))
                {
                    Debug.Log($"[ComputerSystem] Registering {app.AppName} as Photo viewer");
                    appRegistry.RegisterViewerApp(FileType.Photo, app);
                }
                else if (app.AppName.Contains("Document") || app.AppName.Contains("Text"))
                {
                    Debug.Log($"[ComputerSystem] Registering {app.AppName} as Document viewer");
                    appRegistry.RegisterViewerApp(FileType.Document, app);
                }
                else if (app.AppName.Contains("Video") || app.AppName.Contains("Animation"))
                {
                    Debug.Log($"[ComputerSystem] Registering {app.AppName} as Video viewer");
                    appRegistry.RegisterViewerApp(FileType.Video, app);
                }
            }
        }
        
        Debug.Log("[ComputerSystem] RegisterDefaultViewerApps completed");
    }
    
    private bool IsMouseOverDesktop()
    {
        if (desktopManager == null) return false;
        
        Vector2 mousePosition = Input.mousePosition;
        Camera eventCamera = parentCanvas.worldCamera;
        
        // Check if mouse is over desktop area
        var desktopRect = desktopManager.transform as RectTransform;
        if (desktopRect != null && RectTransformUtility.RectangleContainsScreenPoint(desktopRect, mousePosition, eventCamera))
        {
            return true;
        }
        
        // Check if mouse is over menu bar area
        var menuBar = FindFirstObjectByType<MenuBar>();
        if (menuBar != null)
        {
            var menuBarRect = menuBar.transform as RectTransform;
            if (menuBarRect != null && RectTransformUtility.RectangleContainsScreenPoint(menuBarRect, mousePosition, eventCamera))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private void Update()
    {
        // Use our own hover detection
        isHovering = IsMouseOverDesktop();

        if (wasHovering != isHovering)
        {
            wasHovering = isHovering;
            if (wasHovering)
            {
                OnComputerEnter();
            }
            else
            {
                OnComputerExit();
            }
        }

        // Update screensaver state
        if (!isHovering)
        {
            if (Time.time - lastInteractionTime >= screenSaverDelay)
            {
                FadeScreenSaver(1f);
            }
        }
        
        // Monitor disc slot for changes
        MonitorDiscSlot();
    }
    
    private void OnComputerEnter()
    {
        FadeScreenSaver(0f);
        if (retroEffects != null)
        {
            retroEffects.ShowComputerCursor();
        }
        lastInteractionTime = Time.time;
    }
    
    private void OnComputerExit()
    {
        if (retroEffects != null)
        {
            retroEffects.HideComputerCursor();
            // Also force restore cursor to prevent any stuck states
            retroEffects.ForceRestoreCursor();
        }
        lastInteractionTime = Time.time;
    }
    
    private void FadeScreenSaver(float targetAlpha)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeScreenSaverRoutine(targetAlpha));
    }
    
    private IEnumerator FadeScreenSaverRoutine(float targetAlpha)
    {
        float startAlpha = screenSaver.alpha;
        float elapsedTime = 0f;
        
        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime * fadeSpeed;
            screenSaver.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime);
            yield return null;
        }
        
        screenSaver.alpha = targetAlpha;
        fadeCoroutine = null;
    }
    
    public void InstallApp(AppConfig appConfig)
    {
        StartCoroutine(InstallAppWithDelay(appConfig));
    }
    
    /// <summary>
    /// Install app with delay and wait cursor
    /// </summary>
    private System.Collections.IEnumerator InstallAppWithDelay(AppConfig appConfig)
    {
        // Show wait cursor
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        // Wait for installation delay
        yield return new WaitForSeconds(1.5f); // Delay before app appears
        
        // Install the app
        appRegistry.InstallApp(appConfig);
        
        // Return to normal cursor after a short delay
        yield return new WaitForSeconds(0.5f);
        if (retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
    }
    
    public void UninstallApp(string appId)
    {
        appRegistry.UninstallApp(appId);
    }
    
    public void InsertDisc(Evidence evidence)
    {
        if (evidence != null && evidence.type == EvidenceType.Disc && evidence.HasAssociatedApp)
        {
            currentDisk = evidence;
            InstallApp(evidence.AppConfig);
        }
    }
    
    public void EnableScreenSaver()
    {
        FadeScreenSaver(1f);
    }
    
    public void DisableScreenSaver()
    {
        FadeScreenSaver(0f);
        lastInteractionTime = Time.time;
    }
    
    #region Card Handling
    
    /// <summary>
    /// Check if the computer can accept a specific card
    /// </summary>
    public bool CanAcceptDisc(Card card)
    {
        Debug.Log($"[ComputerSystem] CanAcceptDisc called for card '{card.name}' with mode {card.mode}");
        
        try
        {
            // Must be evidence card
            if (card.mode != CardMode.Evidence) 
            {
                Debug.Log($"[ComputerSystem] Rejected: Card mode is {card.mode}, not Evidence");
                return false;
            }
            
            Debug.Log($"[ComputerSystem] Step 1 passed: Card mode is Evidence");
            
            // Must be a disc
            Evidence evidence = card.GetEvidenceData();
            if (evidence == null) 
            {
                Debug.Log($"[ComputerSystem] Rejected: No evidence data found");
                return false;
            }
            
            Debug.Log($"[ComputerSystem] Step 2 passed: Evidence data found - title: '{evidence.title}', type: {evidence.type}");
            
            if (evidence.type != EvidenceType.Disc) 
            {
                Debug.Log($"[ComputerSystem] Rejected: Evidence type is {evidence.type}, not Disk");
                return false;
            }
            
            Debug.Log($"[ComputerSystem] Step 3 passed: Evidence type is Disk");
            
            // Check if disc slot already has a disc
            if (discSlot != null && discSlot.cards.Count > 0)
            {
                Debug.Log($"[ComputerSystem] Rejected: Disc slot already has {discSlot.cards.Count} cards");
                return false; // Already has a disc
            }
            
            Debug.Log($"[ComputerSystem] Step 4 passed: Disc slot is empty (discSlot: {discSlot != null}, cards: {(discSlot?.cards.Count ?? -1)})");
            
            // Check if this disc has an associated app
            bool hasAssociatedApp = evidence.HasAssociatedApp;
            Debug.Log($"[ComputerSystem] Step 5 check: evidence.HasAssociatedApp = {hasAssociatedApp}");
            
            if (!hasAssociatedApp)
            {
                Debug.LogWarning($"[ComputerSystem] Rejected: Disc '{evidence.title}' has no associated app");
                return false;
            }
            
            Debug.Log($"[ComputerSystem] All checks passed! Accepted: Disc '{evidence.title}' can be inserted");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ComputerSystem] Exception in CanAcceptDisc: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
    
    /// <summary>
    /// Handle disc dropped on computer
    /// </summary>
    public void HandleDiscDropped(Card card)
    {
        if (!CanAcceptDisc(card)) return;
        
        Evidence evidence = card.GetEvidenceData();
        if (evidence == null) return;
        
        // Store source hand and position for ejection
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
        
        // Add to disc slot (Computer type, Evidence mode)  
        if (discSlot != null)
        {
            discSlot.AddCardToHand(card);
            
            // Force visual update for computer (should show bigCardVisual like a Mat)
            if (card.parentHolder != null && card.parentHolder.purpose == HolderPurpose.Computer)
            {
                card.cardLocation = CardLocation.Mat; // Computer acts like Mat - shows bigCardVisual
            }
        }
        
        // Store references
        currentDiscCard = card;
        currentDisk = evidence;
        
        // Start insertion animation
        if (insertionAnimationCoroutine != null)
        {
            StopCoroutine(insertionAnimationCoroutine);
        }
        insertionAnimationCoroutine = StartCoroutine(AnimateDiscInsertion(card, evidence));
    }
    
    /// <summary>
    /// Animate disc insertion with rotation and hiding
    /// </summary>
    private IEnumerator AnimateDiscInsertion(Card card, Evidence evidence)
    {
        // Lock dragging during animation
        card.isDragLocked = true;
        
        // Step 1: Store original snapAngle and set to 0 (no tilting) via bigCardVisual
        if (card.bigCardVisual != null)
        {
            // Store original snap angle for restoration
            originalSnapAngle = card.bigCardVisual.GetSnapAngle();
            
            // Set snap angle to 0 so card won't tilt naturally
            card.bigCardVisual.SetSnapAngle(0f);
            
            var tiltParent = card.bigCardVisual.GetTiltParent();
            if (tiltParent != null)
            {
                // Animate tilt to 0 (no tilting)
                tiltParent.DOLocalRotate(Vector3.zero, discInsertionDuration * 0.3f).SetEase(insertionEase);
            }
        }
        
        // Step 2: Animate cardSlot to rotate to (90, 0, 0) - looks like going into slot
        Transform cardSlot = card.transform.parent != null ? card.transform.parent : card.transform;
        cardSlot.DOLocalRotate(new Vector3(90f, 0f, 0f), discInsertionDuration).SetEase(insertionEase);
        
        // Wait for rotation animation to complete
        yield return new WaitForSeconds(discInsertionDuration);
        
        // Step 3: Wait additional delay before hiding
        yield return new WaitForSeconds(discInsertionDelay);
        
        // Step 4: Hide the disc (it's now "inside" the computer)
        if (card.transform.parent != null)
        {
            card.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            card.gameObject.SetActive(false);
        }
        
        // Step 5: Install the associated app now that disc is fully inserted
        if (evidence.HasAssociatedApp)
        {
            InstallApp(evidence.AppConfig);
        }
        
        // Step 6: Update UI and set inserted state
        isDiscInserted = true;
        UpdateEjectButtonVisibility();
        
        insertionAnimationCoroutine = null;
    }
    
    /// <summary>
    /// Handle card hovering over computer
    /// </summary>
    public void HandleCardHoverStart(Card card)
    {
        if (CanAcceptDisc(card))
        {
            Debug.Log($"[ComputerSystem] Disc '{card.name}' hovering over computer - can accept");
            // Add visual feedback here (e.g., highlight computer border)
        }
    }
    
    /// <summary>
    /// Handle card stopping hover over computer
    /// </summary>
    public void HandleCardHoverEnd(Card card)
    {
        Debug.Log($"[ComputerSystem] Disc '{card.name}' stopped hovering over computer");
        // Remove visual feedback here
    }
    
    /// <summary>
    /// Monitor the disc slot for changes (in case cards are manually moved)
    /// </summary>
    private void MonitorDiscSlot()
    {
        if (discSlot == null) return;
        
        // Check if disc was removed externally
        if (currentDiscCard != null && !discSlot.cards.Contains(currentDiscCard))
        {
            // Disc was removed externally - clean up
            OnDiscRemoved();
        }
        
        // Enforce single disc rule for Computer type hands
        if (discSlot.cards.Count > 1)
        {
            Debug.LogWarning("[ComputerSystem] Multiple discs detected - removing extras");
            
            // Keep only the first disc, remove others
            for (int i = discSlot.cards.Count - 1; i > 0; i--)
            {
                Card extraCard = discSlot.cards[i];
                discSlot.RemoveCard(extraCard);
                
                // Return to appropriate hand
                if (CardTypeManager.Instance != null)
                {
                    CardTypeManager.Instance.AddCardToAppropriateHand(extraCard);
                }
            }
        }
        
        // Enforce disc-only rule for Computer type hands
        foreach (Card card in discSlot.cards.ToArray()) // ToArray to avoid modification during iteration
        {
            if (!IsValidDiscCard(card))
            {
                Debug.LogWarning($"[ComputerSystem] Non-disc card '{card.name}' detected in computer slot - removing");
                discSlot.RemoveCard(card);
                
                // Return to appropriate hand
                if (CardTypeManager.Instance != null)
                {
                    CardTypeManager.Instance.AddCardToAppropriateHand(card);
                }
            }
        }
    }
    
    /// <summary>
    /// Check if a card is a valid disc card for the computer
    /// </summary>
    private bool IsValidDiscCard(Card card)
    {
        if (card == null || card.mode != CardMode.Evidence) return false;
        
        Evidence evidence = card.GetEvidenceData();
        return evidence != null && evidence.type == EvidenceType.Disc;
    }
    
    /// <summary>
    /// Handle disc removal cleanup
    /// </summary>
    private void OnDiscRemoved()
    {
        if (currentDisk != null && currentDisk.HasAssociatedApp)
        {
            UninstallApp(currentDisk.AppConfig.AppId);
        }
        
        currentDisk = null;
        currentDiscCard = null;
        sourceHand = null;
        sourcePosition = Vector3.zero;
        isDiscInserted = false; // Reset insertion state
        originalSnapAngle = 5f; // Reset snap angle
        
        UpdateEjectButtonVisibility();
    }
    
    /// <summary>
    /// Update eject button visibility based on disc presence
    /// </summary>
    private void UpdateEjectButtonVisibility()
    {
        if (ejectButton != null)
        {
            bool hasDisc = currentDiscCard != null;
            ejectButton.gameObject.SetActive(hasDisc);
        }
    }
    
    /// <summary>
    /// Eject the current disc and return it to source hand with animation
    /// </summary>
    public void EjectDisc()
    {
        if (currentDiscCard == null) 
        {
            return;
        }
        
        // Stop any ongoing insertion animation
        if (insertionAnimationCoroutine != null)
        {
            StopCoroutine(insertionAnimationCoroutine);
            insertionAnimationCoroutine = null;
        }
        
        // Start ejection animation
        StartCoroutine(AnimateDiscEjection());
    }
    
    /// <summary>
    /// Force eject any inserted disc immediately for case closing (no animation)
    /// This ensures discs don't get stuck when cases are closed
    /// </summary>
    public void ForceEjectDiscForCaseClosing()
    {
        if (currentDiscCard == null || !isDiscInserted)
        {
            return; // No disc inserted, nothing to do
        }
        
        Debug.Log("[ComputerSystem] Force ejecting disc for case closing");
        
        Card card = currentDiscCard;
        
        // Stop any ongoing animations
        if (insertionAnimationCoroutine != null)
        {
            StopCoroutine(insertionAnimationCoroutine);
            insertionAnimationCoroutine = null;
        }
        
        // Show the disc if it was hidden
        if (card.transform.parent != null)
        {
            card.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            card.gameObject.SetActive(true);
        }
        
        // Reset rotations immediately (no animation during case closing)
        Transform cardSlot = card.transform.parent != null ? card.transform.parent : card.transform;
        cardSlot.localRotation = Quaternion.identity;
        
        // Restore snap angle and tilt immediately
        if (card.bigCardVisual != null)
        {
            card.bigCardVisual.SetSnapAngle(originalSnapAngle);
            var tiltParent = card.bigCardVisual.GetTiltParent();
            if (tiltParent != null)
            {
                tiltParent.localRotation = Quaternion.Euler(0, 0, originalSnapAngle);
            }
        }
        
        // Unlock dragging
        card.isDragLocked = false;
        
        // Remove from disc slot
        if (discSlot != null)
        {
            discSlot.RemoveCard(card);
        }
        
        // Return to evidence hand (where it should go during case closing)
        if (EvidenceManager.Instance?.evidenceHand != null)
        {
            EvidenceManager.Instance.evidenceHand.AddCardToHand(card);
            card.cardLocation = CardLocation.Hand; // Shows cardVisual
        }
        else if (sourceHand != null)
        {
            // Fallback to source hand if evidence manager not available
            sourceHand.AddCardToHand(card);
            
            // Set visual based on hand type
            switch (sourceHand.purpose)
            {
                case HolderPurpose.Hand:
                    card.cardLocation = CardLocation.Hand;
                    break;
                case HolderPurpose.Mat:
                    card.cardLocation = CardLocation.Mat;
                    break;
            }
        }
        
        // Clean up
        OnDiscRemoved();
    }
    
    /// <summary>
    /// Animate disc ejection - reverse of insertion animation
    /// </summary>
    private IEnumerator AnimateDiscEjection()
    {
        Card card = currentDiscCard;
        if (card == null) yield break;
        
        // Step 1: Show the disc again if it was hidden
        if (isDiscInserted)
        {
            if (card.transform.parent != null)
            {
                card.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                card.gameObject.SetActive(true);
            }
            isDiscInserted = false;
        }
        
        // Step 2: Animate cardSlot back from (90,0,0) to (0,0,0) - disc coming out of slot
        Transform cardSlot = card.transform.parent != null ? card.transform.parent : card.transform;
        cardSlot.DOLocalRotate(Vector3.zero, discInsertionDuration).SetEase(insertionEase);
        
        // Wait for ejection animation to complete
        yield return new WaitForSeconds(discInsertionDuration);
        
        // Step 3: Restore snapAngle and tilt parent rotation (AFTER animation completes)
        if (card.bigCardVisual != null)
        {
            // Restore original snap angle so card will tilt naturally when back on mat
            card.bigCardVisual.SetSnapAngle(originalSnapAngle);
            
            var tiltParent = card.bigCardVisual.GetTiltParent();
            if (tiltParent != null)
            {
                // Restore natural tilt quickly so it finishes before card reaches mat hand
                tiltParent.DOLocalRotate(new Vector3(0, 0, originalSnapAngle), 0.2f).SetEase(insertionEase);
            }
        }
        
        // Step 4: Unlock dragging
        card.isDragLocked = false;
        
        // Step 5: Remove from disc slot
        if (discSlot != null)
        {
            discSlot.RemoveCard(card);
        }
        
        // Step 6: Return to source hand
        if (sourceHand != null)
        {
            // Special handling based on source hand type
            if (sourceHand.purpose == HolderPurpose.Mat && sourceHand.enableFreeFormPlacement)
            {
                // Add card back to hand first
                sourceHand.AddCardToHand(card);
                
                // Then restore original position
                Transform returnedCardSlot = card.transform.parent;
                if (returnedCardSlot != null)
                {
                    returnedCardSlot.localPosition = sourcePosition;
                }
                else
                {
                    card.transform.localPosition = sourcePosition;
                }
            }
            else
            {
                // For regular hands (evidenceHand, etc.), use standard add
                sourceHand.AddCardToHand(card);
            }
            
            // Force visual update based on new parent holder type
            if (card.parentHolder != null)
            {
                // Set appropriate card location based on holder type to trigger visual switch
                switch (card.parentHolder.purpose)
                {
                    case HolderPurpose.Hand:
                        card.cardLocation = CardLocation.Hand; // Shows cardVisual
                        break;
                    case HolderPurpose.Mat:
                        card.cardLocation = CardLocation.Mat; // Shows bigCardVisual
                        break;
                    case HolderPurpose.Computer:
                        card.cardLocation = CardLocation.Mat; // Computer acts like Mat
                        break;
                }
            }
        }
        else if (CardTypeManager.Instance != null)
        {
            // Fallback: return to appropriate hand
            CardTypeManager.Instance.AddCardToAppropriateHand(card);
            
            // Force visual update for fallback case too
            if (card.parentHolder != null)
            {
                switch (card.parentHolder.purpose)
                {
                    case HolderPurpose.Hand:
                        card.cardLocation = CardLocation.Hand;
                        break;
                    case HolderPurpose.Mat:
                        card.cardLocation = CardLocation.Mat;
                        break;
                    case HolderPurpose.Computer:
                        card.cardLocation = CardLocation.Mat;
                        break;
                }
            }
        }
        
        // Step 7: Clean up
        OnDiscRemoved();
    }
    
    #endregion
} 