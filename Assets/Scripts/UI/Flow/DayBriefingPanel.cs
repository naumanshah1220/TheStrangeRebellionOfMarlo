using System;
using System.Collections.Generic;
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
    [SerializeField] private TMP_Text subheadlineText;
    [SerializeField] private TMP_Text introText;
    [SerializeField] private Button startDayButton;

    [Header("Family Letter")]
    [SerializeField] private GameObject letterPanel;
    [SerializeField] private TMP_Text letterFromText;
    [SerializeField] private TMP_Text letterBodyText;

    [Header("Unlock Notices")]
    [SerializeField] private RectTransform noticesContainer;
    [SerializeField] private GameObject noticePrefab;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float newspaperSlideDistance = 600f;
    [SerializeField] private float newspaperSlideDuration = 0.6f;
    [SerializeField] private float letterDelayAfterNewspaper = 0.4f;
    [SerializeField] private float letterFadeDuration = 0.5f;
    [SerializeField] private float noticePopDelay = 0.3f;
    [SerializeField] private float noticePopDuration = 0.3f;

    private Action onStartDayPressed;
    private List<GameObject> spawnedNotices = new List<GameObject>();

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (startDayButton) startDayButton.onClick.AddListener(OnStartDay);
    }

    /// <summary>
    /// Enriched Show that accepts the full DayBriefingData struct.
    /// </summary>
    public void Show(DayBriefingData data, Action onStartDay)
    {
        onStartDayPressed = onStartDay;
        gameObject.SetActive(true);

        // Clean up any previously spawned notice items
        CleanupNotices();

        if (dayTitleText) dayTitleText.text = $"Day {data.dayNumber}";

        bool isFirstDay = data.dayNumber <= 1;
        bool hasHeadline = !string.IsNullOrEmpty(data.headline);

        // --- Newspaper (day 2+ or whenever there's a headline) ---
        if (newspaperRect)
        {
            bool showNewspaper = !isFirstDay && hasHeadline;
            newspaperRect.gameObject.SetActive(showNewspaper);
            if (showNewspaper)
            {
                if (headlineText) headlineText.text = data.headline;
                if (subheadlineText)
                {
                    subheadlineText.gameObject.SetActive(!string.IsNullOrEmpty(data.subheadline));
                    subheadlineText.text = data.subheadline ?? "";
                }

                // Start newspaper off-screen, then slide in
                var startPos = newspaperRect.anchoredPosition;
                newspaperRect.anchoredPosition = new Vector2(startPos.x, startPos.y - newspaperSlideDistance);
                newspaperRect.DOAnchorPosY(startPos.y, newspaperSlideDuration)
                    .SetEase(Ease.OutBack)
                    .SetDelay(fadeInDuration);
            }
        }

        // --- Intro text for day 1 ---
        if (introText)
        {
            introText.gameObject.SetActive(isFirstDay);
            if (isFirstDay) introText.text = "Welcome to your first day at The Bureau, Analyst Marlo.\nYour cases await.";
        }

        // --- Family letter ---
        bool hasLetter = !string.IsNullOrEmpty(data.letterBody);
        if (letterPanel)
        {
            letterPanel.SetActive(hasLetter);
            if (hasLetter)
            {
                if (letterFromText) letterFromText.text = !string.IsNullOrEmpty(data.letterFrom) ? $"From: {data.letterFrom}" : "";
                if (letterBodyText) letterBodyText.text = data.letterBody;

                // Animate: fade in letter after newspaper slides in (or after panel fade on day 1)
                var letterCG = letterPanel.GetComponent<CanvasGroup>();
                if (letterCG == null) letterCG = letterPanel.AddComponent<CanvasGroup>();
                letterCG.alpha = 0f;

                float letterDelay = isFirstDay
                    ? fadeInDuration + 0.2f
                    : fadeInDuration + newspaperSlideDuration + letterDelayAfterNewspaper;

                letterCG.DOFade(1f, letterFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetDelay(letterDelay);
            }
        }

        // --- Unlock notices ---
        if (noticesContainer)
        {
            bool hasNotices = data.unlockNotices != null && data.unlockNotices.Count > 0;
            noticesContainer.gameObject.SetActive(hasNotices);

            if (hasNotices)
            {
                float baseDelay = isFirstDay
                    ? fadeInDuration + 0.5f
                    : fadeInDuration + newspaperSlideDuration + letterDelayAfterNewspaper + letterFadeDuration + 0.2f;

                for (int i = 0; i < data.unlockNotices.Count; i++)
                {
                    SpawnNotice(data.unlockNotices[i], baseDelay + i * noticePopDelay);
                }
            }
        }

        canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad)
            .OnStart(() => canvasGroup.blocksRaycasts = true);
    }

    /// <summary>
    /// Legacy overload for backward compatibility.
    /// </summary>
    public void Show(int dayNumber, string headline, Action onStartDay)
    {
        Show(new DayBriefingData
        {
            dayNumber = dayNumber,
            headline = headline,
            subheadline = "",
            letterFrom = "",
            letterBody = "",
            unlockNotices = new List<string>()
        }, onStartDay);
    }

    public void Hide(Action onHidden = null)
    {
        canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                canvasGroup.blocksRaycasts = false;
                CleanupNotices();
                gameObject.SetActive(false);
                onHidden?.Invoke();
            });
    }

    private void OnStartDay()
    {
        Hide(() => onStartDayPressed?.Invoke());
    }

    private void SpawnNotice(string text, float delay)
    {
        if (noticePrefab == null || noticesContainer == null)
        {
            // No prefab assigned — try creating a text element dynamically
            var noticeGO = new GameObject("UnlockNotice", typeof(RectTransform), typeof(CanvasGroup));
            noticeGO.transform.SetParent(noticesContainer, false);

            var tmp = noticeGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.8f, 0.2f, 1f); // Gold/yellow for unlock notices

            var cg = noticeGO.GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            noticeGO.transform.localScale = Vector3.one * 0.5f;
            cg.DOFade(1f, noticePopDuration).SetDelay(delay).SetEase(Ease.OutQuad);
            noticeGO.transform.DOScale(Vector3.one, noticePopDuration).SetDelay(delay).SetEase(Ease.OutBack);

            spawnedNotices.Add(noticeGO);
            return;
        }

        var instance = Instantiate(noticePrefab, noticesContainer);
        instance.SetActive(true);

        var noticeText = instance.GetComponentInChildren<TMP_Text>();
        if (noticeText) noticeText.text = text;

        var canvasGroupComp = instance.GetComponent<CanvasGroup>();
        if (canvasGroupComp == null) canvasGroupComp = instance.AddComponent<CanvasGroup>();
        canvasGroupComp.alpha = 0f;

        instance.transform.localScale = Vector3.one * 0.5f;
        canvasGroupComp.DOFade(1f, noticePopDuration).SetDelay(delay).SetEase(Ease.OutQuad);
        instance.transform.DOScale(Vector3.one, noticePopDuration).SetDelay(delay).SetEase(Ease.OutBack);

        spawnedNotices.Add(instance);
    }

    private void CleanupNotices()
    {
        foreach (var notice in spawnedNotices)
        {
            if (notice != null)
                Destroy(notice);
        }
        spawnedNotices.Clear();
    }
}
