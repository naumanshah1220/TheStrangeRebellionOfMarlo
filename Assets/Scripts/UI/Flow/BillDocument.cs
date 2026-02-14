using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BillDocument : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image inkMark;
    [SerializeField] private RectTransform stampZone;

    private CanvasGroup inkMarkCanvasGroup;

    private void Awake()
    {
        if (inkMark != null)
        {
            inkMarkCanvasGroup = inkMark.GetComponent<CanvasGroup>();
            if (inkMarkCanvasGroup == null)
                inkMarkCanvasGroup = inkMark.gameObject.AddComponent<CanvasGroup>();
            inkMarkCanvasGroup.alpha = 0f;
        }
    }

    public void Setup(BillInfo bill)
    {
        if (titleText) titleText.text = bill.title;
        if (amountText) amountText.text = $"${bill.amount:F0}";
        if (descriptionText) descriptionText.text = bill.description;

        // Reset ink mark
        if (inkMarkCanvasGroup) inkMarkCanvasGroup.alpha = 0f;
    }

    public void ShowStampMark()
    {
        if (inkMarkCanvasGroup != null)
            inkMarkCanvasGroup.DOFade(1f, 0.1f);
    }

    public RectTransform GetStampZone()
    {
        return stampZone;
    }

    public bool IsOverStampZone(Vector2 screenPoint, Camera cam)
    {
        if (stampZone == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(stampZone, screenPoint, cam);
    }
}
