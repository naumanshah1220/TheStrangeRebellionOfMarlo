using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [Header("Case List")]
    public HorizontalCardHolder caseHand;
    public RectTransform caseHolderPanel;      // animate this out

    [Header("Evidence List")]
    public RectTransform evidenceHolderPanel;  // animate this in
    public ScrollRect evidenceScroller;

    [Header("Submit Zone")]
    public RectTransform submitZonePanel;

    [Header("Commit UI")]
    public CommitOverlayController commitOverlay; // Direct reference to the scene object
    public Button commitButton; // The commit/indict button
    
    [Header("Hand Animation Positions")]
    [Space(5)]
    [Header("Case Hand")]
    public float caseHandShowY = -50f;
    public float caseHandHideY = -500f;
    public float caseHandAnimationDuration = 0.6f;
    
    [Header("Evidence Hand")]
    public float evidenceHandShowY = -50f;
    public float evidenceHandHideY = -500f;
    public float evidenceHandAnimationDuration = 0.6f;
    
    [Header("Submit Zone")]
    public float submitZoneShowY = -50f;
    public float submitZoneHideY = -500f;
    public float submitZoneAnimationDuration = 0.6f;
    
    protected override void OnSingletonAwake()
    {
        // Setup commit button
        if (commitButton != null)
        {
            commitButton.onClick.AddListener(OnCommitButtonPressed);
        }

        GameEvents.OnCaseSolved += HandleCaseSolved;
    }

    protected override void OnSingletonDestroy()
    {
        GameEvents.OnCaseSolved -= HandleCaseSolved;
    }

    private void HandleCaseSolved(Case solvedCase)
    {
        if (solvedCase != null)
        {
            ShowCaseSolvedNotification(solvedCase);
        }
    }

    public void NotifyNewCaseAssigned(Case newCase)
    {
        Debug.Log($"[UI] New case assigned: {newCase.title}");

        if (caseHand != null && newCase != null)
        {
            caseHand.LoadCardsFromData(new List<Case> { newCase }, false);
        }     
        
        // TODO: Show popup or toast if needed
    }

    /// Simulates or displays the newspaper headline at the start of a day.
    public IEnumerator ShowHeadlineForDay(int day)
    {
        Debug.Log($"[UI] Showing headline for Day {day}");

        // TODO: Replace this with your actual newspaper panel logic.
        // For now, simulate with a delay and a debug log.
        yield return new WaitForSeconds(1.5f);
    }

    //Animate the Evidence Hand In
    public void AnimateToEvidenceHand()
    {
        StartCoroutine(SwitchHandCoroutine());
    }

    //Switch hands from Case to Evidence
    private IEnumerator SwitchHandCoroutine()
    {
        // Animate out and fade
        if (caseHolderPanel != null)
        {
            caseHolderPanel.DOAnchorPosY(caseHandHideY, caseHandAnimationDuration).SetEase(Ease.InBack);
            var caseGroup = caseHolderPanel.GetComponent<CanvasGroup>();
            if (caseGroup != null)
                caseGroup.DOFade(0f, 0.25f);
        }

        evidenceScroller.gameObject.SetActive(true);

        // Animate in and fade
        if (evidenceHolderPanel != null)
        {
            evidenceHolderPanel.DOAnchorPosY(evidenceHandShowY, evidenceHandAnimationDuration).SetEase(Ease.OutBack);
            var evidenceGroup = evidenceHolderPanel.GetComponent<CanvasGroup>();
            if (evidenceGroup != null)
                evidenceGroup.DOFade(1f, 0.25f);
        }

        // Wait for slide duration
        yield return new WaitForSeconds(0.3f);
    }
    
    //Animate the Evidence Hand Out
    public void AnimateEvidenceHandOut()
    {
        if (evidenceHolderPanel != null)
        {
            // Slide the evidence hand off screen
            evidenceHolderPanel.DOAnchorPosY(evidenceHandHideY, evidenceHandAnimationDuration).SetEase(Ease.InBack);
            var group = evidenceHolderPanel.GetComponent<CanvasGroup>();
            if (group != null)
                group.DOFade(0f, 0.3f);

            evidenceScroller.gameObject.SetActive(false);
        }
    }

    public void AnimateSubmitZoneIn()
    {
        if (submitZonePanel != null)
        {
            submitZonePanel.gameObject.SetActive(true);
            submitZonePanel.DOAnchorPosY(submitZoneShowY, submitZoneAnimationDuration).SetEase(Ease.OutBack);
            var group = submitZonePanel.GetComponent<CanvasGroup>();
            if (group != null)
                group.DOFade(1f, 0.3f);
        }
    }

    public void AnimateSubmitZoneOut()
    {
        if (submitZonePanel != null)
        {
            submitZonePanel.DOAnchorPosY(submitZoneHideY, submitZoneAnimationDuration).SetEase(Ease.InBack)
                .OnComplete(() => submitZonePanel.gameObject.SetActive(false));
            var group = submitZonePanel.GetComponent<CanvasGroup>();
            if (group != null)
                group.DOFade(0f, 0.3f);
        }
    }

    public void AnimateCaseHandIn()
    {
        if (caseHolderPanel != null)
        {
            caseHolderPanel.gameObject.SetActive(true);
            caseHolderPanel.DOAnchorPosY(caseHandShowY, caseHandAnimationDuration).SetEase(Ease.OutBack);
            var group = caseHolderPanel.GetComponent<CanvasGroup>();
            if (group != null)
                group.DOFade(1f, 0.3f);
        }
        
        // Re-enable evidence scroller for mat area functionality
        if (evidenceScroller != null)
        {
            evidenceScroller.gameObject.SetActive(true);
            Debug.Log("[UIManager] Re-enabled evidenceScroller for mat area");
        }
    }

    public void OnCommitComplete(CaseVerdict verdict)
    {
        // Notify suspect manager (or a new VerdictManager)
        if (GameManager.Instance != null && GameManager.Instance.CurrentCase != null)
        {
            // The final verdict is composed and passed in.
            // We should store this in a dedicated field, NOT the draft field.
            // Let's assume a field `submittedVerdict` exists for the final, locked-in verdict.
            // If it doesn't, we need to add it to Case.cs
            // For now, let's re-use draftVerdict, but this is not ideal.
            GameManager.Instance.CurrentCase.draftVerdict = verdict;
            Debug.Log($"[UIManager] Verdict submitted for case '{GameManager.Instance.CurrentCase.caseID}' with confidence {verdict.computedConfidence}%.");
        }
        
        // Close the case since a verdict has been submitted
        if (GameManager.Instance != null)
        {
            Debug.Log("[UIManager] Verdict submitted, closing case.");
            GameManager.Instance.StartCoroutine(GameManager.Instance.CloseCaseRoutine());
        }
        
        // Restore the commit button's state
        if (commitButton != null)
        {
            commitButton.onClick.RemoveAllListeners();
            commitButton.onClick.AddListener(OnCommitButtonPressed);
            var tmp = commitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && commitOverlay != null) tmp.text = "Indict"; // Or some default text
        }
    }
    
    public void OnCommitCancelled()
    {
        // Handle commit cancellation if needed
        Debug.Log("[UIManager] Commit was cancelled");

        // Restore the commit button's state
        if (commitButton != null)
        {
            commitButton.onClick.RemoveAllListeners();
            commitButton.onClick.AddListener(OnCommitButtonPressed);
            var tmp = commitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null && commitOverlay != null) tmp.text = "Indict";
        }
    }
    
    public void OnCommitButtonPressed()
    {
        Debug.Log("[UIManager] Commit button pressed");
        
        if (commitOverlay == null)
        {
            Debug.LogError("[UIManager] CommitOverlay reference is null! Please assign it in the inspector.");
            return;
        }
        
        Case currentCase = GameManager.Instance?.CurrentCase;
        
        if (currentCase != null)
        {
            Debug.Log($"[UIManager] Showing commit UI for case: {currentCase.title}");
            
            // Setup event handlers
            commitOverlay.OnCommitCompleted -= OnCommitComplete; // Unsubscribe first to prevent duplicates
            commitOverlay.OnCommitCompleted += OnCommitComplete;
            
            commitOverlay.OnCommitCancelled -= OnCommitCancelled; // Unsubscribe first
            commitOverlay.OnCommitCancelled += OnCommitCancelled;

            // UIManager now controls the button's state change
            if (commitButton != null)
            {
                commitButton.onClick.RemoveAllListeners();
                commitButton.onClick.AddListener(commitOverlay.Hide);
                var tmp = commitButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = commitOverlay.cancelButtonText;
            }

            // Show the UI with the current case data
            commitOverlay.Show(currentCase);
        }
        else
        {
            Debug.LogWarning("[UIManager] No active case to commit.");
            // ShowNotification("No active case for indictment.", NotificationType.Warning);
        }
    }

    public void ShowCaseSolvedNotification(Case solvedCase)
    {
        // Show a notification that the case was solved
        string message = $"Case Solved: {solvedCase.title}\n" +
                        $"Suspect: {solvedCase.convictedSuspect.GetFullName()}\n" +
                        $"Crime: {solvedCase.conviction?.violationName ?? "Unknown"}";
        
        ShowNotification(message, NotificationType.Success);
    }

    private void ShowNotification(string message, NotificationType type)
    {
        // TODO: Implement notification system
        Debug.Log($"[UI] {type}: {message}");
    }
}

public enum NotificationType
{
    Success,
    Warning,
    Error,
    Info
}
