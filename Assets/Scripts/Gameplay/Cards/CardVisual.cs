using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// CardVisual controls the floating/animated visuals of a Card, including tilt, fly-in, scaling, 
/// and highlighting for hand, drag, and selection states. 
/// This version is extended for Case/Evidence support and is fully compatible with the MixAndJam system.
/// </summary>
public class CardVisual : MonoBehaviour
{
    // MixAndJam initialization
    private bool initalize = false;

    [Header("Card Link")]
    public Card parentCard;
    private Transform cardTransform;
    private Vector3 rotationDelta;
    private int savedIndex;
    Vector3 movementDelta;
    private Canvas canvas;

    [Header("References")]
    public Transform visualShadow;
    private float shadowOffset = 20;
    private Vector2 shadowDistance;
    private Canvas shadowCanvas;

    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;

    [SerializeField] private TMP_Text titleText;           // For title (case/evidence)
    [SerializeField] private Image cardImage;        // For case/evidence icon

    [Header("Follow Parameters")]
    public bool isFlyingIn = false;
    [SerializeField] private float followSpeed = 30;
    [SerializeField] private float flyingSpeed = 4f; // Speed for deal-in animation
    private float dealFlyTime = 0.3f; // Duration for deal-in animation (set by card holder)

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;
    [SerializeField] private float rotationSpeed = 20;
    [SerializeField] private float autoTiltAmount = 30;
    [SerializeField] private float manualTiltAmount = 20;
    [SerializeField] private float tiltSpeed = 20;

    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;
    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] private float scaleTransition = .15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;

    [Header("Hover Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;
    [SerializeField] private float hoverTransition = .15f;

    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;
    [SerializeField] private float swapRotationAngle = 30;
    [SerializeField] private float swapTransition = .15f;
    [SerializeField] private int swapVibrato = 5;

    [Header("Curve")]
    [SerializeField] private CurveParameters curve;

    private float curveYOffset;
    private float curveRotationOffset;

    // === INITIALIZATION ===
    private void Start()
    {
        shadowDistance = visualShadow.localPosition;
    }

    /// <summary>
    /// Initialize the visual for a Card, with full event binding. Called from Card.Initialize().
    /// </summary>
    public void Initialize(Card target, int index = 0)
    {
        // Link to card
        parentCard = target;
        cardTransform = target.transform;
        canvas = GetComponent<Canvas>();
        shadowCanvas = visualShadow.GetComponent<Canvas>();

        // Listen to parentCard events for hover, drag, select, etc
        parentCard.PointerEnterEvent.AddListener(PointerEnter);
        parentCard.PointerExitEvent.AddListener(PointerExit);
        parentCard.BeginDragEvent.AddListener(BeginDrag);
        parentCard.EndDragEvent.AddListener(EndDrag);
        parentCard.PointerDownEvent.AddListener(PointerDown);
        parentCard.PointerUpEvent.AddListener(PointerUp);

        initalize = true;
    }

    /// <summary>
    /// Sync visual sibling index with card slot index (hand/fan).
    /// </summary>
    public void UpdateIndex(int length)
    {
        transform.SetSiblingIndex(parentCard.transform.parent.GetSiblingIndex());
    }

    void Update()
    {
        if (!initalize || parentCard == null) return;

        HandPositioning();
        SmoothFollow();
        FollowRotation();
        CardTilt();
    }

    // === HAND VISUALS & FAN POSITIONING ===

    private void HandPositioning()
    {
        // Only apply curve if the hand has it enabled
        if (parentCard.parentHolder != null && parentCard.parentHolder.enableFanningAndCurve)
        {
            // Offset for hand curve/fan effect
            curveYOffset = (curve.positioning.Evaluate(parentCard.NormalizedPosition()) * curve.positioningInfluence) * parentCard.SiblingAmount();
            curveYOffset = parentCard.SiblingAmount() < 3 ? 0 : curveYOffset;
            curveRotationOffset = curve.rotation.Evaluate(parentCard.NormalizedPosition());
        }
        else
        {
            // Reset offsets if fanning is disabled
            curveYOffset = 0;
            curveRotationOffset = 0;
        }
    }

    private void SmoothFollow()
    {
        // Only follow if not placed on mat
        if (!initalize || parentCard == null)// || parentCard.isPlacedOnMat)
            return;

        if (isFlyingIn)
        {
            Vector3 targetPos = cardTransform.position + Vector3.up * curveYOffset;
            Quaternion targetRot = Quaternion.LookRotation(Camera.main.transform.forward);

            // Use DOTween for duration-based animation instead of speed-based
            if (!DOTween.IsTweening(transform))
            {
                transform.DOMove(targetPos, dealFlyTime).SetEase(Ease.OutBack);
                transform.DORotate(targetRot.eulerAngles, dealFlyTime).SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        isFlyingIn = false;
                    });
            }
            return;
        }

        Vector3 verticalOffset = Vector3.up * (parentCard.isDragging ? 0 : curveYOffset);
        transform.position = Vector3.Lerp(transform.position, cardTransform.position + verticalOffset, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, cardTransform.rotation, followSpeed * Time.deltaTime);
    }

    private void FollowRotation()
    {
        Vector3 movement = (transform.position - cardTransform.position);
        movementDelta = Vector3.Lerp(movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = (parentCard.isDragging ? movementDelta : movement) * rotationAmount;
        rotationDelta = Vector3.Lerp(rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Mathf.Clamp(rotationDelta.x, -60, 60));
    }

    private void CardTilt()
    {
        savedIndex = parentCard.isDragging ? savedIndex : parentCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + savedIndex) * (parentCard.isHovering ? 0.2f : 1);
        float cosine = Mathf.Cos(Time.time + savedIndex) * (parentCard.isHovering ? 0.2f : 1);

        Vector3 worldMouse = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 1f));
        Vector3 direction = (transform.position - worldMouse).normalized;
        Vector3 localDirection = transform.InverseTransformDirection(direction);

        float tiltX = parentCard.isHovering ? (localDirection.y * manualTiltAmount) : 0;
        float tiltY = parentCard.isHovering ? (localDirection.x * manualTiltAmount) : 0;
        
        // Only apply curve rotation if the hand has it enabled
        float curveRotation = (parentCard.parentHolder != null && parentCard.parentHolder.enableFanningAndCurve)
            ? (curveRotationOffset * (curve.rotationInfluence * parentCard.SiblingAmount()))
            : 0;
            
        float tiltZ = parentCard.isDragging ? tiltParent.localEulerAngles.z : curveRotation;

        float lerpX = Mathf.LerpAngle(tiltParent.localEulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.localEulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.localEulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.localRotation = Quaternion.Euler(lerpX, lerpY, lerpZ);
    }

    // === CARD VISUAL UPDATES (CASE/EVIDENCE, ETC) ===

    /// <summary>
    /// Set visuals for Case card (called by Card.cs).
    /// </summary>
    public void SetFromCase(Case caseData)
    {


        titleText.text = caseData.title;
        cardImage.sprite = caseData.cardImage;
        // Optionally update iconImage if you wish
    }

    /// <summary>
    /// Set visuals for Evidence card (called by Card.cs).
    /// </summary>
    public void SetFromEvidence(Evidence evidenceData)
    {
        titleText.text = evidenceData.title;
        cardImage.sprite = evidenceData.cardImage;

    }

    /// <summary>
    /// Set visuals for generic card types (Phone, Tool, etc.)
    /// </summary>
    public void SetFromGenericCard(ICardData cardData)
    {
        titleText.text = cardData.GetCardTitle();
        cardImage.sprite = cardData.GetCardImage();
    }

    // === ANIMATIONS & EFFECTS ===

    public void LiftCardVisual(Card card, bool state)
    {
        DOTween.Kill(1, true);
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1).SetId(1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20, 1).SetId(2);

        // Removed canvas.overrideSorting to maintain viewport masking

        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);
    }

    public void Swap(float dir = 1)
    {
        if (!swapAnimations)
            return;

        // Kill any existing swap animations to prevent overlapping
        DOTween.Kill(3, true);
        
        // Store the current rotation to ensure we return to it
        Vector3 originalRotation = shakeParent.localEulerAngles;
        
        // Create the punch rotation with proper completion callback
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1)
            .SetId(3)
            .OnComplete(() => {
                // Ensure rotation is properly reset after animation
                shakeParent.localRotation = Quaternion.Euler(originalRotation);
            });
    }

    private void BeginDrag(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        // Removed canvas.overrideSorting to maintain viewport masking
    }

    private void EndDrag(Card card)
    {
        // Removed canvas.overrideSorting to maintain viewport masking
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerEnter(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    private void PointerExit(Card card)
    {
        if (!parentCard.wasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void PointerUp(Card card, bool longPress)
    {
        if (scaleAnimations)
            transform.DOScale(longPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition = shadowDistance;
        // Removed shadowCanvas.overrideSorting to maintain viewport masking
        LiftCardVisual(card, false);
    }

    private void PointerDown(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += (-Vector3.up * shadowOffset);
        // Removed shadowCanvas.overrideSorting to maintain viewport masking
        LiftCardVisual(card, true);
    }

    /// <summary>
    /// Returns the main tilt transform for custom external use (e.g. Mat snap).
    /// </summary>
    public Transform GetTiltParent() => tiltParent;
    public Transform GetShakeParent() => shakeParent;
    
    /// <summary>
    /// Get the card image component for sprite changes
    /// </summary>
    public Image GetCardImage() => cardImage;

    /// <summary>
    /// Set the deal fly time for the flying animation
    /// </summary>
    public void SetDealFlyTime(float duration)
    {
        dealFlyTime = duration;
    }

    public void AnimateTilt(float zAngle, float duration = 0.3f)
    {
        if (tiltParent != null)
            tiltParent.DOLocalRotate(new Vector3(0, 0, zAngle), duration).SetEase(Ease.OutBack);
    }

    public void ShowCard(bool show)
    {
        visualShadow.gameObject.SetActive(show);
        cardImage.gameObject.SetActive(show);
        titleText.gameObject.SetActive(show);

    }

    public bool IsCardShown()
    {
        return cardImage.gameObject.activeSelf;
    }
}
