using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BillsDeskPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private BillDocument billDocument;
    [SerializeField] private BillStamp stamp;
    [SerializeField] private RectTransform billArea;

    [Header("Buttons")]
    [SerializeField] private Button setAsideButton;
    [SerializeField] private Button goHomeButton;

    [Header("Text Displays")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private TMP_Text summaryBalanceText;
    [SerializeField] private TMP_Text summaryHungerText;

    [Header("Groups")]
    [SerializeField] private GameObject billGroup;
    [SerializeField] private GameObject summaryGroup;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float billSlideDistance = 1200f;

    private List<BillInfo> bills;
    private NightSummaryData nightData;
    private Action onGoHome;
    private int currentBillIndex;
    private float runningBalance;
    private bool isProcessing;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (setAsideButton) setAsideButton.onClick.AddListener(OnSetAside);
        if (goHomeButton) goHomeButton.onClick.AddListener(OnGoHome);

        if (stamp) stamp.OnStampApplied += OnStampDroppedAtPosition;
    }

    public void Show(List<BillInfo> billList, NightSummaryData data, Action onGoHomePressed)
    {
        bills = billList;
        nightData = data;
        onGoHome = onGoHomePressed;
        currentBillIndex = 0;
        runningBalance = data.totalEarnings;
        isProcessing = false;

        gameObject.SetActive(true);

        // Initial state
        if (billGroup) billGroup.SetActive(true);
        if (summaryGroup) summaryGroup.SetActive(false);
        if (warningText) warningText.gameObject.SetActive(false);
        if (setAsideButton) setAsideButton.interactable = true;

        UpdateBalanceDisplay();

        if (stamp) stamp.ResetToHolder();

        // Fade in panel
        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad)
            .OnStart(() => canvasGroup.blocksRaycasts = true)
            .OnComplete(() =>
            {
                if (bills.Count > 0)
                    ShowBill(0, fromRight: true);
                else
                    ShowSummary();
            });
    }

    public void Hide(Action onHidden = null)
    {
        canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                onHidden?.Invoke();
            });
    }

    private void ShowBill(int index, bool fromRight)
    {
        if (index >= bills.Count)
        {
            ShowSummary();
            return;
        }

        currentBillIndex = index;
        var bill = bills[index];
        billDocument.Setup(bill);

        // Slide bill in
        var billRect = billDocument.GetComponent<RectTransform>();
        if (fromRight)
        {
            float startX = billSlideDistance;
            billRect.anchoredPosition = new Vector2(startX, billRect.anchoredPosition.y);
            billRect.DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack);
        }

        // Enable stamp interaction
        if (setAsideButton) setAsideButton.interactable = true;
        if (warningText) warningText.gameObject.SetActive(false);
    }

    private void OnStampDroppedAtPosition(Vector2 screenPoint)
    {
        if (isProcessing) return;
        if (currentBillIndex >= bills.Count) return;

        Camera cam = parentCanvas != null ? parentCanvas.worldCamera : null;

        if (billDocument.IsOverStampZone(screenPoint, cam))
        {
            isProcessing = true;
            ApproveBill();
        }
        else
        {
            stamp.SnapBackToHolder();
        }
    }

    private Canvas parentCanvas
    {
        get
        {
            if (_parentCanvas == null)
                _parentCanvas = GetComponentInParent<Canvas>();
            return _parentCanvas;
        }
    }
    private Canvas _parentCanvas;

    private void ApproveBill()
    {
        var bill = bills[currentBillIndex];
        bill.wasApproved = true;

        var stampZone = billDocument.GetStampZone();
        Vector3 stampTarget = stampZone != null ? stampZone.position : billDocument.transform.position;

        // 1. Snap stamp to bill's stamp zone
        stamp.MoveToTarget(stampTarget, () =>
        {
            // 2. THUNK — stamp impact
            stamp.PlayStampImpact(() =>
            {
                // 3. Squish the bill document
                var billRect = billDocument.GetComponent<RectTransform>();
                billRect.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6);

                // 4. Show ink mark
                billDocument.ShowStampMark();

                // 5. Deduct from balance
                runningBalance -= bill.amount;
                UpdateBalanceDisplay();

                // 6. Pause, then return stamp and slide bill away
                StartCoroutine(FinishStampSequence());
            });
        });
    }

    private IEnumerator FinishStampSequence()
    {
        // Pause to let player see the result
        yield return new WaitForSeconds(0.4f);

        // Return stamp to holder
        stamp.SnapBackToHolder();

        // Slide bill out to the left
        var billRect = billDocument.GetComponent<RectTransform>();
        billRect.DOAnchorPosX(-billSlideDistance, 0.4f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                isProcessing = false;
                ShowBill(currentBillIndex + 1, fromRight: true);
            });
    }

    private void OnSetAside()
    {
        if (isProcessing) return;
        if (currentBillIndex >= bills.Count) return;

        isProcessing = true;
        var bill = bills[currentBillIndex];
        bill.wasApproved = false;

        // Flash skip warning
        if (warningText && !string.IsNullOrEmpty(bill.skipWarning))
        {
            warningText.gameObject.SetActive(true);
            warningText.text = bill.skipWarning;

            // Flash effect — fade in then out
            var warnCg = warningText.GetComponent<CanvasGroup>();
            if (warnCg == null) warnCg = warningText.gameObject.AddComponent<CanvasGroup>();
            warnCg.alpha = 0f;

            var seq = DOTween.Sequence();
            seq.Append(warnCg.DOFade(1f, 0.15f));
            seq.AppendInterval(0.6f);
            seq.Append(warnCg.DOFade(0f, 0.3f));
        }

        // Slide bill out
        var billRect = billDocument.GetComponent<RectTransform>();
        billRect.DOAnchorPosX(-billSlideDistance, 0.4f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                isProcessing = false;
                ShowBill(currentBillIndex + 1, fromRight: true);
            });
    }

    private void ShowSummary()
    {
        if (billGroup) billGroup.SetActive(false);
        if (summaryGroup) summaryGroup.SetActive(true);

        float totalExpenses = 0f;
        bool paidFood = false;
        foreach (var bill in bills)
        {
            if (bill.wasApproved)
            {
                totalExpenses += bill.amount;
                if (bill.category == "food") paidFood = true;
            }
        }

        float netEarnings = nightData.totalEarnings - totalExpenses;
        float projectedSavings = nightData.currentSavings + netEarnings;

        if (summaryBalanceText)
            summaryBalanceText.text = $"Savings: ${projectedSavings:F0}";

        if (summaryHungerText)
        {
            if (paidFood)
                summaryHungerText.text = $"Family fed tonight";
            else
                summaryHungerText.text = $"Your family will go hungry tonight";
        }
    }

    private void UpdateBalanceDisplay()
    {
        if (balanceText)
            balanceText.text = $"Balance: ${runningBalance:F0}";
    }

    private void OnGoHome()
    {
        // Compute net earnings (total - approved expenses)
        float totalExpenses = 0f;
        foreach (var bill in bills)
        {
            if (bill.wasApproved)
                totalExpenses += bill.amount;
        }
        float netEarnings = nightData.totalEarnings - totalExpenses;

        // Process day end with final financials
        var progress = PlayerProgressManager.Instance;
        if (progress != null)
        {
            progress.ProcessDayEnd(netEarnings);
        }

        Hide(() => onGoHome?.Invoke());
    }

    private void OnDestroy()
    {
        if (stamp) stamp.OnStampApplied -= OnStampDroppedAtPosition;
        DOTween.Kill(canvasGroup);
    }
}
