using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Simple overlay for day start. Shows day number, intro text (day 1), and Start Day button.
/// Newspaper, letters, bonuses, and notices are delivered as Overseer Hand cards after the workday begins.
/// </summary>
public class DayBriefingPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text dayTitleText;
    [SerializeField] private TMP_Text introText;
    [SerializeField] private Button startDayButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private Action onStartDayPressed;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (startDayButton) startDayButton.onClick.AddListener(OnStartDay);
    }

    public void Show(DayBriefingData data, Action onStartDay)
    {
        onStartDayPressed = onStartDay;
        gameObject.SetActive(true);

        if (dayTitleText) dayTitleText.text = $"Day {data.dayNumber}";

        bool isFirstDay = data.dayNumber <= 1;
        if (introText)
        {
            introText.gameObject.SetActive(isFirstDay);
            if (isFirstDay)
                introText.text = "Welcome to your first day at The Bureau, Analyst Marlo.\nYour cases await.";
        }

        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad)
            .OnStart(() => canvasGroup.blocksRaycasts = true);
    }

    /// <summary>
    /// Legacy overload for backward compatibility.
    /// </summary>
    public void Show(int dayNumber, string headline, Action onStartDay)
    {
        Show(new DayBriefingData { dayNumber = dayNumber }, onStartDay);
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

    private void OnStartDay()
    {
        Hide(() => onStartDayPressed?.Invoke());
    }
}
