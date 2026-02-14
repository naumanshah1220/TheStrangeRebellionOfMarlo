using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class TopBarController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private Button endDayButton;
    [SerializeField] private TMP_Text endDayButtonText;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.3f;

    private bool isVisible;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (endDayButton) endDayButton.onClick.AddListener(OnEndDayPressed);
    }

    private void Update()
    {
        if (!isVisible) return;

        var days = DaysManager.Instance;
        if (days == null || !days.IsDayActive()) return;

        if (timeText) timeText.text = days.GetTimeString();

        // Disable End Day if core cases remain
        if (endDayButton)
        {
            bool hasCorePending = false;
            foreach (var c in days.todaysPendingCases)
            {
                if (c.caseType == CaseType.Core)
                {
                    hasCorePending = true;
                    break;
                }
            }
            endDayButton.interactable = !hasCorePending;
            if (endDayButtonText)
                endDayButtonText.text = hasCorePending ? "End Day (Core pending)" : "End Day";
        }
    }

    public void Show(int dayNumber)
    {
        gameObject.SetActive(true);
        isVisible = true;
        if (dayText) dayText.text = $"Day {dayNumber}";
        canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.OutQuad)
            .OnStart(() => canvasGroup.blocksRaycasts = true);
    }

    public void Hide()
    {
        isVisible = false;
        canvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            });
    }

    private void OnEndDayPressed()
    {
        var days = DaysManager.Instance;
        if (days != null) days.EndDay();
    }
}
