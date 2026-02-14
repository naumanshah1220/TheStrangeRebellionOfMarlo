using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class DayBriefingPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform newspaperRect;
    [SerializeField] private TMP_Text dayTitleText;
    [SerializeField] private TMP_Text headlineText;
    [SerializeField] private TMP_Text introText;
    [SerializeField] private Button startDayButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float newspaperSlideDistance = 600f;
    [SerializeField] private float newspaperSlideDuration = 0.6f;

    private Action onStartDayPressed;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (startDayButton) startDayButton.onClick.AddListener(OnStartDay);
    }

    public void Show(int dayNumber, string headline, Action onStartDay)
    {
        onStartDayPressed = onStartDay;
        gameObject.SetActive(true);

        if (dayTitleText) dayTitleText.text = $"Day {dayNumber}";

        bool isFirstDay = dayNumber <= 1;

        // Newspaper only on day 2+
        if (newspaperRect)
        {
            newspaperRect.gameObject.SetActive(!isFirstDay);
            if (!isFirstDay)
            {
                if (headlineText) headlineText.text = headline ?? "No news today.";
                // Start newspaper off-screen, then slide in
                var startPos = newspaperRect.anchoredPosition;
                newspaperRect.anchoredPosition = new Vector2(startPos.x, startPos.y - newspaperSlideDistance);
                newspaperRect.DOAnchorPosY(startPos.y, newspaperSlideDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(fadeInDuration);
            }
        }

        // Intro text for day 1
        if (introText)
        {
            introText.gameObject.SetActive(isFirstDay);
            if (isFirstDay) introText.text = "Welcome to your first day at The Bureau, Analyst Marlo.\nYour cases await.";
        }

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

    private void OnStartDay()
    {
        Hide(() => onStartDayPressed?.Invoke());
    }
}
