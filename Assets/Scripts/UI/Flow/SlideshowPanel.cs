using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SlideshowPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float slideFadeDuration = 0.4f;

    private SlideshowData data;
    private int currentSlideIndex;
    private Action onComplete;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        if (nextButton) nextButton.onClick.AddListener(NextSlide);
        if (skipButton) skipButton.onClick.AddListener(Skip);
    }

    public void Show(SlideshowData slideshowData, Action onSlideshowComplete)
    {
        data = slideshowData;
        onComplete = onSlideshowComplete;
        currentSlideIndex = 0;
        isTransitioning = false;

        if (data == null || data.slides.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        gameObject.SetActive(true);
        DisplaySlide(data.slides[0]);
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

    private void DisplaySlide(SlideshowSlide slide)
    {
        if (titleText) titleText.text = slide.title ?? "";
        if (bodyText) bodyText.text = slide.bodyText ?? "";
        if (backgroundImage && slide.backgroundImage != null)
            backgroundImage.sprite = slide.backgroundImage;

        // Update button text on last slide
        if (nextButton != null)
        {
            var btnText = nextButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = currentSlideIndex >= data.slides.Count - 1 ? "Begin" : "Next";
        }
    }

    private bool isTransitioning;

    private void NextSlide()
    {
        if (isTransitioning) return;

        currentSlideIndex++;
        if (currentSlideIndex >= data.slides.Count)
        {
            Finish();
            return;
        }

        isTransitioning = true;
        int targetIndex = currentSlideIndex;

        // Crossfade to next slide
        canvasGroup.DOFade(0.3f, slideFadeDuration * 0.5f).SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                if (targetIndex < data.slides.Count)
                    DisplaySlide(data.slides[targetIndex]);
                canvasGroup.DOFade(1f, slideFadeDuration * 0.5f).SetEase(Ease.OutQuad)
                    .OnComplete(() => isTransitioning = false);
            });
    }

    private void Skip()
    {
        Finish();
    }

    private void Finish()
    {
        Hide(() => onComplete?.Invoke());
    }
}
