using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using Detective.UI.Commit;
using UnityEngine.EventSystems;
using DanielLochner.Assets.SimpleScrollSnap; // Required for getting suspect photo
using System.Collections; // <-- Added for IEnumerator
using TMPro; // <-- Added for TextMeshProUGUI

public class CommitOverlayController : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("The Verdict Composer that builds the charge sheet.")]
    public VerdictComposer verdictComposer;

    [Header("UI References")]
    public CanvasGroup overlayCanvasGroup;
    public RectTransform chargeSheetPanel; // Renamed from commitPanel for clarity
    public Button actionButton; // Renamed from incarcerateButton
    public RectTransform leverHandle;

    [Header("Animation Curves")]
    public AnimationCurve panelSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve leverSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Lever Logic")]
    [Range(0f, 1f)] public float chargeSheetFadeEnd = 0.7f;
    public float leverPullDistance = 200f;
    public float leverResistance = 2f;
    public AnimationCurve resistanceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Animation Timings")]
    public float panelSlideDuration = 0.6f;
    public float leverSlideDuration = 0.5f;
    
    [Header("Panel Positions")]
    public float panelStartY = 1200f;
    public float panelEndY = -100f;

    [Header("Lever Positions")]
    public float leverStartX = 500f; // Off-screen right
    public float leverEndX = -50f;   // On-screen

    [Header("Cinematic Sequence")]
    public Image flashImage;
    public Image spriteAnimationImage;
    public Sprite[] animationFrames;
    public float flashDuration = 0.1f;
    public float delayAfterFlash = 0.2f;
    public float animationFrameRate = 10f;
    public float finalFadeOutDelay = 1.0f;
    public float delayBeforeReset = 1.0f; // Added from old script logic
    
    [Header("Button Text")]
    public string incarcerateButtonText = "Incarcerate"; // Kept for legacy compatibility if needed
    public string cancelButtonText = "Cancel";
    
    [Header("Verdict Status Indicators")]
    public GameObject incompleteVerdictIndicator; // Red light
    public GameObject completeVerdictIndicator; // Green light
    
    // Data
    private Case currentCase;
    private bool isAnimating = false;
    private bool isPanelVisible = false;
    private bool isVerdictReady = false; // To control lever interaction
    
    // Animation state
    private GameObject extractedPhotoContainer;
    private Coroutine spriteAnimationCoroutine;
    private CanvasGroup commitPanelCanvasGroup;
    private bool isFadeSequenceActive = false;
    
    // Lever state
    private Vector2 leverStartPosition;
    private bool isDraggingLever = false;
    private bool leverActivated = false;
    private float currentLeverPull = 0f;
    
    // Events
    public System.Action<CaseVerdict> OnCommitCompleted;
    public System.Action OnCommitCancelled;
    
    private void Awake()
    {
        if (verdictComposer == null) verdictComposer = GetComponentInChildren<VerdictComposer>();

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0;
            overlayCanvasGroup.blocksRaycasts = false;
        }
        
        if (chargeSheetPanel != null)
        {
            commitPanelCanvasGroup = chargeSheetPanel.GetComponent<CanvasGroup>();
            if (commitPanelCanvasGroup == null)
            {
                commitPanelCanvasGroup = chargeSheetPanel.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (leverHandle != null)
        {
            leverStartPosition = leverHandle.anchoredPosition;
            var eventTrigger = leverHandle.gameObject.GetComponent<EventTrigger>() ?? leverHandle.gameObject.AddComponent<EventTrigger>();
            
            var beginDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(beginDragEntry);

            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(dragEntry);

            var endDragEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endDragEntry.callback.AddListener((data) => { OnEndDrag((PointerEventData)data); });
            eventTrigger.triggers.Add(endDragEntry);
        }
        
        // Button listener is now managed dynamically in Show/Hide
        
        // Set initial positions to be off-screen to prevent visibility on game start.
        chargeSheetPanel.anchoredPosition = new Vector2(chargeSheetPanel.anchoredPosition.x, panelStartY);
        var actionButtonRect = actionButton.GetComponent<RectTransform>();
        actionButtonRect.anchoredPosition = new Vector2(leverStartX, actionButtonRect.anchoredPosition.y);

        if(verdictComposer != null)
        {
            verdictComposer.OnVerdictStateChanged += UpdateVerdictStatusVisuals;
        }

        // Initial state setup
        ResetUIState();
    }

    private void OnDestroy()
    {
        if (verdictComposer != null)
        {
            verdictComposer.OnVerdictStateChanged -= UpdateVerdictStatusVisuals;
        }
    }

    public void Show(Case caseData)
    {
        if (isAnimating || isPanelVisible) return;
        
        isAnimating = true;
        currentCase = caseData; // Store the case reference

        ResetLever();
        ResetFadeSequence();

        StartCoroutine(verdictComposer.BuildFromCase(caseData, caseData.draftVerdict));

        overlayCanvasGroup.alpha = 0;
            overlayCanvasGroup.blocksRaycasts = true;

        chargeSheetPanel.anchoredPosition = new Vector2(chargeSheetPanel.anchoredPosition.x, panelStartY);
        
        var actionButtonRect = actionButton.GetComponent<RectTransform>();
        actionButtonRect.anchoredPosition = new Vector2(leverStartX, actionButtonRect.anchoredPosition.y);
        
        // Animate In
        overlayCanvasGroup.DOFade(0.4f, panelSlideDuration);
        chargeSheetPanel.DOAnchorPosY(panelEndY, panelSlideDuration).SetEase(panelSlideCurve);

        actionButtonRect.DOAnchorPosX(leverEndX, leverSlideDuration)
            .SetEase(leverSlideCurve)
            .SetDelay(0.2f); // Stagger the animation slightly

        // The UIManager now handles the button's state. This script no longer needs to.
        // SetButtonText(cancelButtonText);
        // if (actionButton != null)
        // {
        //     actionButton.onClick.RemoveAllListeners();
        //     actionButton.onClick.AddListener(Hide);
        // }

        DOVirtual.DelayedCall(Mathf.Max(panelSlideDuration, leverSlideDuration + 0.2f), () => {
        isAnimating = false;
            isPanelVisible = true;
            UpdateVerdictStatusVisuals(); // Set initial state
        });
    }
    
    public void Hide()
    {
        if (isAnimating || !isPanelVisible) return;

        // Find and close any active verdict option popup before animating the panel away.
        var openPopup = FindFirstObjectByType<VerdictOptionPopup>();
        if (openPopup != null)
        {
            openPopup.Close();
        }

        isAnimating = true;
        isPanelVisible = false;

        // Save the current state of the verdict before clearing the UI
        if (currentCase != null)
        {
            verdictComposer.TryCompose(out currentCase.draftVerdict, out _);
        }

        // Animate panel and button sliding out
        chargeSheetPanel.DOAnchorPosY(panelStartY, panelSlideDuration).SetEase(panelSlideCurve);
        actionButton.GetComponent<RectTransform>().DOAnchorPosX(leverStartX, panelSlideDuration).SetEase(leverSlideCurve)
            .SetDelay(0.2f);

        DOVirtual.DelayedCall(panelSlideDuration + 0.2f, () => {
            isPanelVisible = false;
            isAnimating = false;
            overlayCanvasGroup.blocksRaycasts = false;
            ResetUIState(); 
        OnCommitCancelled?.Invoke();
        });
    }

    // --- Animation Sequence ---
    
    private void StartFadeSequence()
    {
        if (isFadeSequenceActive) return;
        isFadeSequenceActive = true;
        ExtractSuspectPhoto();
    }
    
    private void UpdateFadeProgress(float progress)
    {
        if (!isFadeSequenceActive) return;
        float panelAlpha = Mathf.Clamp01(1f - (progress / chargeSheetFadeEnd));
        commitPanelCanvasGroup.alpha = panelAlpha;
        // Also fade the main overlay, starting from its base alpha
        overlayCanvasGroup.alpha = Mathf.Lerp(0.4f, 1f, progress);
    }
    
    private void ResetFadeSequence()
    {
        if (!isFadeSequenceActive) return;
        isFadeSequenceActive = false;
        UpdateFadeProgress(0f);
        if (extractedPhotoContainer != null) Destroy(extractedPhotoContainer);
    }
    
    private void CompleteFadeSequence()
    {
        isFadeSequenceActive = false;
        commitPanelCanvasGroup.alpha = 0;
        overlayCanvasGroup.alpha = 1f;
    }

    private IEnumerator PlayFinalSequence(CaseVerdict verdict)
    {
        isAnimating = true;
        CompleteFadeSequence();

        yield return StartCoroutine(PlayFlashAnimation());
        yield return new WaitForSeconds(delayAfterFlash);
        StartSpriteAnimation();
        yield return new WaitForSeconds(finalFadeOutDelay);

        if (extractedPhotoContainer != null)
        {
            extractedPhotoContainer.GetComponent<CanvasGroup>()?.DOFade(0f, 0.5f);
        }
        if (spriteAnimationImage != null)
        {
            spriteAnimationImage.GetComponent<CanvasGroup>()?.DOFade(0f, 0.5f);
        }
        overlayCanvasGroup.DOFade(0f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        
        ResetUIState();
        OnCommitCompleted?.Invoke(verdict);
    }

    private void ResetUIState()
    {
        isPanelVisible = false;
        isAnimating = false;
        leverActivated = false;

        // Reset visual components
        if (flashImage != null) flashImage.gameObject.SetActive(false);
        if (spriteAnimationImage != null) spriteAnimationImage.gameObject.SetActive(false);
        if (spriteAnimationCoroutine != null) StopCoroutine(spriteAnimationCoroutine);
        if (extractedPhotoContainer != null) Destroy(extractedPhotoContainer);

        // The UIManager now resets the button's text and listener.
        // SetButtonText(incarcerateButtonText);

        // Reset positions for next 'Show' animation
        if (chargeSheetPanel != null)
        {
            chargeSheetPanel.anchoredPosition = new Vector2(chargeSheetPanel.anchoredPosition.x, panelStartY);
        }
        if (actionButton != null)
        {
            var actionButtonRect = actionButton.GetComponent<RectTransform>();
            actionButtonRect.anchoredPosition = new Vector2(leverStartX, actionButtonRect.anchoredPosition.y);
        }
        
        // Reset alphas
        if(overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = false;
        }
        if(commitPanelCanvasGroup != null) commitPanelCanvasGroup.alpha = 1f;
        
        ResetLever();
        verdictComposer.Clear();
        UpdateVerdictStatusVisuals(); // Reset verdict indicators
    }
    
    private Image GetSelectedSuspectPhoto()
    {
        var scroller = verdictComposer?.suspectPortraitSlot;
        if (scroller == null) return null;
        
        var scrollSnap = scroller.GetComponentInChildren<SimpleScrollSnap>();
        if (scrollSnap == null || scrollSnap.NumberOfPanels == 0) return null;
        
        int selectedIndex = scrollSnap.SelectedPanel;
        if (selectedIndex < 0 || selectedIndex >= scrollSnap.NumberOfPanels) return null;

        RectTransform panelRect = scrollSnap.Panels[selectedIndex];
        return panelRect?.GetComponent<SuspectItem>()?.suspectPhoto;
    }
    
    private void ExtractSuspectPhoto()
    {
        if (extractedPhotoContainer != null) Destroy(extractedPhotoContainer);
        Image originalPhoto = GetSelectedSuspectPhoto();
        if (originalPhoto == null) return;

        extractedPhotoContainer = new GameObject("ExtractedSuspectPhoto");
        extractedPhotoContainer.transform.SetParent(transform, false);
        
        RectTransform extractedRect = extractedPhotoContainer.AddComponent<RectTransform>();
        RectTransform originalRect = originalPhoto.GetComponent<RectTransform>();
        
            Vector3[] corners = new Vector3[4];
            originalRect.GetWorldCorners(corners);
        extractedRect.position = (corners[0] + corners[2]) * 0.5f; // Center position
        extractedRect.sizeDelta = new Vector2(originalRect.rect.width, originalRect.rect.height);
        
        var image = extractedPhotoContainer.AddComponent<Image>();
        image.sprite = originalPhoto.sprite;
        image.color = originalPhoto.color;
        
        extractedPhotoContainer.AddComponent<CanvasGroup>();
        extractedPhotoContainer.transform.SetAsLastSibling();
    }
    
    private IEnumerator PlayFlashAnimation()
    {
        if (flashImage == null) yield break;
        var flashGroup = flashImage.GetComponent<CanvasGroup>() ?? flashImage.gameObject.AddComponent<CanvasGroup>();
        flashImage.gameObject.SetActive(true);
        
        flashGroup.alpha = 1f;
        yield return new WaitForSeconds(flashDuration);
        yield return flashGroup.DOFade(0.2f, 0.3f).WaitForCompletion();
        flashImage.gameObject.SetActive(false);
    }
    
    private void StartSpriteAnimation()
    {
        if (spriteAnimationImage != null && animationFrames != null && animationFrames.Length > 0)
        {
            // Ensure there's a CanvasGroup for alpha controls.
        CanvasGroup animGroup = spriteAnimationImage.GetComponent<CanvasGroup>();
        if (animGroup == null)
        {
            animGroup = spriteAnimationImage.gameObject.AddComponent<CanvasGroup>();
        }
        
        spriteAnimationImage.gameObject.SetActive(true);
            animGroup.alpha = 1;
            StartCoroutine(PlaySpriteAnimation());
        }
    }
    
    private IEnumerator PlaySpriteAnimation()
    {
        float frameTime = 1f / animationFrameRate;
        for (int i = 0; i < animationFrames.Length; i++)
        {
            spriteAnimationImage.sprite = animationFrames[i];
            yield return new WaitForSeconds(frameTime);
        }
    }

    // --- Lever Logic ---

    private IEnumerator HandleActivationSequence()
    {
        // 1. Lock in the verdict BEFORE animations change the UI state.
        bool isVerdictValid = verdictComposer.TryCompose(out CaseVerdict verdict, out string error);
        Debug.Log($"[CommitOverlay] Verdict composition result: IsValid = {isVerdictValid}. Error: {(string.IsNullOrEmpty(error) ? "None" : error)}");

        // 2. Play the full, unconditional cinematic sequence.
        CompleteFadeSequence();
        yield return StartCoroutine(PlayFlashAnimation());
        yield return new WaitForSeconds(delayAfterFlash);

        StartSpriteAnimation();

        // 3. WAIT for the door animation to actually finish playing.
        float animationDuration = 0f;
        if (animationFrames != null && animationFrames.Length > 0 && animationFrameRate > 0)
        {
            animationDuration = animationFrames.Length / animationFrameRate;
        }
        yield return new WaitForSeconds(animationDuration);

        // 4. Hold on the final frame for a moment.
        yield return new WaitForSeconds(finalFadeOutDelay);

        Debug.Log("[CommitOverlay] Door animation finished. Checking verdict validity to proceed.");
        // 5. Now, based on the stored verdict result, proceed.
        if (isVerdictValid)
        {
            Debug.Log("[CommitOverlay] Verdict is valid. Starting fade out sequence.");
            // SUCCESS: Animate everything out and submit the case.
            
            // Simultaneously fade elements and slide button out.
            if (extractedPhotoContainer != null) extractedPhotoContainer.GetComponent<CanvasGroup>()?.DOFade(0f, 0.5f);
            if (spriteAnimationImage != null) spriteAnimationImage.GetComponent<CanvasGroup>()?.DOFade(0f, 0.5f);
            overlayCanvasGroup.DOFade(0f, 0.5f);
            
            var actionButtonRect = actionButton.GetComponent<RectTransform>();
            actionButtonRect.DOAnchorPosX(leverStartX, leverSlideDuration).SetEase(leverSlideCurve);

            // Wait for the main fade to complete.
            yield return new WaitForSeconds(0.5f);
            
            // Wait a final moment before resetting the UI.
            yield return new WaitForSeconds(delayBeforeReset);

            // Reset the UI and notify that the commit is complete.
            // The UIManager will handle closing the case.
            ResetUIState();
            OnCommitCompleted?.Invoke(verdict);
        }
        else
        {
            // FAILURE: The animations have played. The lever is down.
            // The player sees the result and must manually cancel to try again.
            Debug.LogWarning($"[CommitOverlay] Verdict composition failed: {error}. Awaiting user cancellation.");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnimating || !isPanelVisible || leverActivated || !isVerdictReady) return;
            isDraggingLever = true;
            StartFadeSequence();
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggingLever || leverActivated || !isVerdictReady) return;
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            leverHandle.parent as RectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        
        // Corrected Drag Calculation: Allow pulling beyond the visual distance to overcome resistance
            float rawDeltaY = localPoint.y - leverStartPosition.y;
        rawDeltaY = Mathf.Clamp(rawDeltaY, -leverPullDistance * (1 + leverResistance), 0);
            
            float rawPullPercentage = Mathf.Abs(rawDeltaY) / leverPullDistance;
            float resistance = resistanceCurve.Evaluate(Mathf.Clamp01(rawPullPercentage)) * leverResistance;
        currentLeverPull = Mathf.Clamp01(rawPullPercentage / (1 + resistance));

        leverHandle.anchoredPosition = new Vector2(leverStartPosition.x, leverStartPosition.y - (currentLeverPull * leverPullDistance));
            UpdateFadeProgress(currentLeverPull);
            
        // If we pull it all the way down, activate.
            if (currentLeverPull >= 1.0f && !leverActivated)
            {
                leverActivated = true;
            isDraggingLever = false; // Stop further drag updates.
            StartCoroutine(HandleActivationSequence());
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        // If the lever has already been activated during the drag, or we weren't dragging, do nothing.
        if (!isDraggingLever || leverActivated || !isVerdictReady) return;
        
        isDraggingLever = false;
        
        // If the lever is released anywhere before the very end, spring it back.
        SpringLeverBack();
    }
    
    private void ResetLever()
    {
        leverActivated = false;
        currentLeverPull = 0f;
        if (leverHandle != null)
        {
            leverHandle.anchoredPosition = leverStartPosition;
        }
    }
    
    private void SpringLeverBack()
    {
        isFadeSequenceActive = true; 
        DOTween.To(() => currentLeverPull, x => currentLeverPull = x, 0f, 0.4f)
            .SetEase(Ease.OutBounce)
            .OnUpdate(() => {
                        if(this == null || leverHandle == null) return;
                        leverHandle.anchoredPosition = new Vector2(leverStartPosition.x, leverStartPosition.y - (currentLeverPull * leverPullDistance));
                        UpdateFadeProgress(currentLeverPull);
                    })
                    .OnComplete(() => {
                        if(this == null) return;
                        isFadeSequenceActive = false;
                        ResetLever();
                        // Restore base alpha on snapback complete
                        commitPanelCanvasGroup.alpha = 1f;
                        overlayCanvasGroup.alpha = 0.4f;
                    });
    }
    
    // This is no longer needed as UIManager controls the button text.
    // private void SetButtonText(string text)
    // {
    //     var tmp = actionButton.GetComponentInChildren<TextMeshProUGUI>();
    //     if (tmp != null)
    //     {
    //         tmp.text = text;
    //     }
    // }

    private void UpdateVerdictStatusVisuals()
    {
        string error = null;
        isVerdictReady = verdictComposer.TryCompose(out _, out error);

        if (!isVerdictReady && !string.IsNullOrEmpty(error))
        {
            Debug.Log($"[CommitOverlay] Verdict not ready: {error}");
        }
        
        if (incompleteVerdictIndicator != null)
        {
            incompleteVerdictIndicator.SetActive(!isVerdictReady);
        }
        if (completeVerdictIndicator != null)
        {
            completeVerdictIndicator.SetActive(isVerdictReady);
        }
    }
} 