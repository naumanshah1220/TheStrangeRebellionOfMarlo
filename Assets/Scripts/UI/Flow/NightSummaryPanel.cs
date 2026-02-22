using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[System.Serializable]
public class CaseEarning
{
    public string caseTitle;
    public float reward;     // -1 sentinel means "pending review"
}

[System.Serializable]
public class NightSummaryData
{
    public int dayNumber;
    public int casesSolved;
    public float baseSalary;
    public List<CaseEarning> caseEarnings = new List<CaseEarning>();
    public float totalEarnings;
    public float currentSavings;
    public float rent;
    public float foodCost;
    public float currentHunger;
    public bool hadUnsolvedCoreCases;
    public bool hadOvertime;
    public float overtimeHours;
}

public class NightSummaryPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text dayTitleText;
    [SerializeField] private TMP_Text casesSolvedText;
    [SerializeField] private TMP_Text baseSalaryText;
    [SerializeField] private TMP_Text totalEarningsText;
    [SerializeField] private TMP_Text penaltyText;
    [SerializeField] private TMP_Text reviewNoticeText;
    [SerializeField] private TMP_Text overtimeText;

    [Header("Earnings List")]
    [SerializeField] private Transform earningsContainer;
    [SerializeField] private CaseEarningRow earningRowPrefab;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Action onContinue;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        // Don't SetActive(false) here — if the object starts inactive in the scene,
        // Awake fires on first Show() and immediately re-deactivates, preventing display.
        // Panels are invisible via alpha=0 + blocksRaycasts=false.

        if (continueButton) continueButton.onClick.AddListener(OnContinuePressed);
    }

    public void Show(NightSummaryData data, Action onContinuePressed)
    {
        onContinue = onContinuePressed;

        gameObject.SetActive(true);
        PopulateUI(data);

        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad)
            .OnStart(() => canvasGroup.blocksRaycasts = true);
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

    private void PopulateUI(NightSummaryData data)
    {
        if (dayTitleText) dayTitleText.text = $"Day {data.dayNumber} Complete";
        if (casesSolvedText) casesSolvedText.text = $"Cases Solved: {data.casesSolved}";
        if (baseSalaryText) baseSalaryText.text = $"Base Salary: ${data.baseSalary:F0}";
        if (totalEarningsText) totalEarningsText.text = $"Total Earnings: ${data.totalEarnings:F0}";

        // Penalty for unsolved core cases
        if (penaltyText)
        {
            penaltyText.gameObject.SetActive(data.hadUnsolvedCoreCases);
            if (data.hadUnsolvedCoreCases)
                penaltyText.text = "PENALTY: Unsolved core cases \u2014 no pay today";
        }

        // Overtime notice
        if (overtimeText)
        {
            overtimeText.gameObject.SetActive(data.hadOvertime);
            if (data.hadOvertime)
                overtimeText.text = $"OVERTIME: {data.overtimeHours:F1} hours \u2014 penalty applied to bonus";
        }

        // Deferred reward notice
        if (reviewNoticeText)
        {
            bool hasCases = data.caseEarnings.Count > 0;
            reviewNoticeText.gameObject.SetActive(hasCases);
            if (hasCases)
                reviewNoticeText.text = "Results under review \u2014 bonuses delivered tomorrow";
        }

        PopulateEarningsRows(data);
    }

    private void PopulateEarningsRows(NightSummaryData data)
    {
        if (earningsContainer == null) return;

        foreach (Transform child in earningsContainer)
            Destroy(child.gameObject);

        if (earningRowPrefab == null) return;

        foreach (var earning in data.caseEarnings)
        {
            var row = Instantiate(earningRowPrefab, earningsContainer);
            if (earning.reward < 0f)
            {
                // Pending review — show case name but no reward
                row.Setup(earning.caseTitle, "Under Review");
            }
            else
            {
                row.Setup(earning.caseTitle, earning.reward);
            }
        }
    }

    private void OnContinuePressed()
    {
        Hide(() => onContinue?.Invoke());
    }
}
