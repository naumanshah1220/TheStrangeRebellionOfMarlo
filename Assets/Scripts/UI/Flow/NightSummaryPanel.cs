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
    public float reward;
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

        if (penaltyText)
        {
            penaltyText.gameObject.SetActive(data.hadUnsolvedCoreCases);
            if (data.hadUnsolvedCoreCases)
                penaltyText.text = "PENALTY: Unsolved core cases — no pay today";
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
            row.Setup(earning.caseTitle, earning.reward);
        }
    }

    private void OnContinuePressed()
    {
        Hide(() => onContinue?.Invoke());
    }
}
