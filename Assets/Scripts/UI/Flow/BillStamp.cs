using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class BillStamp : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [SerializeField] private RectTransform holderPosition;
    [SerializeField] private Canvas parentCanvas;

    [Header("Settings")]
    [SerializeField] private float dragScaleMultiplier = 1.1f;
    [SerializeField] private float snapBackDuration = 0.25f;

    public event Action<Vector2> OnStampApplied;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private bool isDragging;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        originalScale = transform.localScale;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isDragging) return;
        isDragging = true;

        // Raise above siblings
        transform.SetAsLastSibling();

        // Scale up
        transform.DOScale(originalScale * dragScaleMultiplier, 0.15f).SetEase(Ease.OutBack);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector2 localPoint;
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, eventData.position, parentCanvas.worldCamera, out localPoint))
        {
            rectTransform.localPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        canvasGroup.blocksRaycasts = true;
        transform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad);

        // Pass pointer screen position so BillsDeskPanel can hit-test the stamp zone
        OnStampApplied?.Invoke(eventData.position);
    }

    public void SnapBackToHolder()
    {
        if (holderPosition == null) return;
        rectTransform.DOMove(holderPosition.position, snapBackDuration).SetEase(Ease.OutBack);
    }

    public void MoveToTarget(Vector3 worldPos, Action onArrived)
    {
        rectTransform.DOMove(worldPos, 0.05f).OnComplete(() => onArrived?.Invoke());
    }

    public void PlayStampImpact(Action onComplete)
    {
        var seq = DOTween.Sequence();
        seq.Append(rectTransform.DOPunchPosition(Vector3.down * 20f, 0.35f, 10, 0.3f));
        seq.AppendCallback(() => onComplete?.Invoke());
    }

    public void ResetToHolder()
    {
        if (holderPosition == null) return;
        rectTransform.position = holderPosition.position;
        transform.localScale = originalScale;
    }

    private void OnDestroy()
    {
        DOTween.Kill(rectTransform);
        DOTween.Kill(transform);
    }
}
