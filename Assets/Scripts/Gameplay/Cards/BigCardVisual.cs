using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// CardVisual controls the floating/animated visuals of a Card, including tilt, fly-in, scaling, 
/// and highlighting for hand, drag, and selection states. 
/// This version is extended for Case/Evidence support and is fully compatible with the MixAndJam system.
/// </summary>
public interface IEvidenceDraggable {};

public class BigCardVisual : MonoBehaviour//, IEvidenceDraggable
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
    [SerializeField] private float shadowOffset = 20;
    private Vector2 shadowDistance;
    private Canvas shadowCanvas;

    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;

    [Header("Follow Parameters")]
    public bool isFlyingIn = false;
    [SerializeField] private float followSpeed = 30;
    [SerializeField] private float flyingSpeed = 4f; // Speed for deal-in animation

    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 20;
    [SerializeField] private float rotationSpeed = 20;
    [SerializeField] private float autoTiltAmount = 30;
    [SerializeField] private float manualTiltAmount = 20;
    [SerializeField] private float tiltSpeed = 20;
    [SerializeField] private float snapAngle = 5f;

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


    private Clue[] cluesOnCard;

    [Header("Pages (GameObject-based)")]
    public List<GameObject> pageObjects = new List<GameObject>();
    public UnityEngine.UI.Button leftButton;
    public UnityEngine.UI.Button rightButton;

    public int currentPageIndex = 0;


    void Awake()
    {
        cluesOnCard = GetComponentsInChildren<Clue>(true);
    }
    // === INITIALIZATION ===
    private void Start()
    {
        canvas.worldCamera = Camera.main;
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

        if (pageObjects == null || pageObjects.Count == 0)
        {
            Debug.LogError("EvidencePrefab: No pages assigned! Assign page GameObjects.");
            return;
        }

        // Only add button listeners for multi-page cards
        if (pageObjects.Count > 1)
        {
            if (leftButton != null) leftButton.onClick.AddListener(GoToPreviousPage);
            if (rightButton != null) rightButton.onClick.AddListener(GoToNextPage);
        }
        else
        {
            // For single-page cards, permanently hide the buttons
            if (leftButton != null) leftButton.gameObject.SetActive(false);
            if (rightButton != null) rightButton.gameObject.SetActive(false);
        }

        SetPage(0);

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

        SmoothFollow();

        if (parentCard.isDragging)
        {
            CardTilt();
            FollowRotation();
        }
    }

    private void SmoothFollow()
    {
        // Only follow if not placed on mat
        if (!initalize || parentCard == null)// || parentCard.isPlacedOnMat)
            return;

        if (isFlyingIn)
        {
            Vector3 targetPos = cardTransform.position + Vector3.up;
            Quaternion targetRot = Quaternion.LookRotation(Camera.main.transform.forward);

            transform.position = Vector3.Lerp(transform.position, targetPos, flyingSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, flyingSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                isFlyingIn = false;
            }
            return;
        }

        transform.position = Vector3.Lerp(transform.position, cardTransform.position, followSpeed * Time.deltaTime);
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
        float tiltZ = parentCard.isDragging ? tiltParent.localEulerAngles.z : 0;

        float lerpX = Mathf.LerpAngle(tiltParent.localEulerAngles.x, tiltX + (sine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.localEulerAngles.y, tiltY + (cosine * autoTiltAmount), tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.localEulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.localRotation = Quaternion.Euler(lerpX, lerpY, lerpZ);
    }

    public void LiftCardVisual(Card card, bool state)
    {
        DOTween.Kill(1, true);
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1).SetId(1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle / 2), hoverTransition, 20, 1).SetId(2);

        shadowCanvas.enabled = state;

        if (scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        if (!state)
            AnimateTilt(snapAngle, 0.3f);
    }

    public void Swap(float dir = 1)
    {
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation((Vector3.forward * swapRotationAngle) * dir, swapTransition, swapVibrato, 1).SetId(3);
    }

    private void BeginDrag(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);
    }

    private void EndDrag(Card card)
    {
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
        LiftCardVisual(card, false);
    }


    private void PointerDown(Card card)
    {
        if (scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        visualShadow.localPosition += (-Vector3.up * shadowOffset);
        LiftCardVisual(card, true);
    }

    /// <summary>
    /// Returns the main tilt transform for custom external use (e.g. Mat snap).
    /// </summary>
    public Transform GetTiltParent() => tiltParent;
    public Transform GetShakeParent() => shakeParent;
    
    /// <summary>
    /// Dynamically populates the fallback BigCardVisual with evidence data.
    /// Sets the main image on page 0 and title/description text if found.
    /// Called by Card.Initialize() when using the fallback prefab (no custom BigCardVisual).
    /// </summary>
    public void PopulateFromEvidence(Evidence evidence)
    {
        if (evidence == null) return;

        // Find the first page
        GameObject page0 = (pageObjects != null && pageObjects.Count > 0) ? pageObjects[0] : null;
        if (page0 == null) return;

        // Set the main image on page 0 (the page itself or a child named "EvidenceImage")
        Image evidenceImage = null;

        // First try a named child
        var namedChild = page0.transform.Find("EvidenceImage");
        if (namedChild != null)
            evidenceImage = namedChild.GetComponent<Image>();

        // Fall back to the Image on the page itself
        if (evidenceImage == null)
            evidenceImage = page0.GetComponent<Image>();

        if (evidenceImage != null && evidence.cardImage != null)
        {
            evidenceImage.sprite = evidence.cardImage;
            evidenceImage.preserveAspect = true;
        }

        // Try to set title/description text on page 0
        var textComponents = page0.GetComponentsInChildren<TMPro.TMP_Text>(true);
        if (textComponents.Length > 0)
        {
            // Use the first text component for title + description
            textComponents[0].text = $"<b>{evidence.title}</b>\n\n{evidence.description}";
        }

        Debug.Log($"[BigCardVisual] Populated fallback from evidence '{evidence.id}': {evidence.title}");
    }

    // Legacy API removed: GetDisplayImage()

    public void AnimateTilt(float zAngle, float duration = 0.3f)
    {
        if (tiltParent != null)
            tiltParent.DOLocalRotate(new Vector3(0, 0, zAngle), duration).SetEase(Ease.OutBack);
    }
    
    /// <summary>
    /// Get the current snap angle
    /// </summary>
    public float GetSnapAngle() => snapAngle;
    
    /// <summary>
    /// Set the snap angle (used for computer disc insertion)
    /// </summary>
    public void SetSnapAngle(float newSnapAngle)
    {
        snapAngle = newSnapAngle;
    }

    public void ShowCard(bool show)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        visualShadow.gameObject.SetActive(show);

        for (int i = 0; i < pageObjects.Count; i++)
        {
            var obj = pageObjects[i];
            if (obj != null) obj.SetActive(show && i == currentPageIndex);
        }
        
        int total = pageObjects.Count;
        if (show && total > 1)
        {
            leftButton.gameObject.SetActive(true);
            rightButton.gameObject.SetActive(true);
        }
        else if (!show)
        {
            if (total > 1)
            {
                leftButton.gameObject.SetActive(false);
                rightButton.gameObject.SetActive(false);
            }
        }
        // For single-page cards, buttons remain permanently hidden
    }

    public bool IsCardShown()
    {
        if (currentPageIndex >= 0 && currentPageIndex < pageObjects.Count && pageObjects[currentPageIndex] != null)
            return pageObjects[currentPageIndex].activeSelf;
        return false;
    }

    public void SetPage(int pageIndex)
    {
        int total = pageObjects.Count;
        Debug.Log($"[BigCardVisual] SetPage called with pageIndex={pageIndex}, currentPageIndex={currentPageIndex}, totalPages={total}");
        
        if (pageIndex < 0 || pageIndex >= total)
        {
            Debug.LogWarning($"[BigCardVisual] Invalid page index {pageIndex}. Valid range: 0-{Mathf.Max(0, total - 1)}");
            return;
        }

        currentPageIndex = pageIndex;
        for (int i = 0; i < pageObjects.Count; i++)
        {
            var obj = pageObjects[i];
            if (obj != null) obj.SetActive(i == pageIndex);
        }

        // Only manage button visibility/interactability for multi-page cards
        // Single-page cards have their buttons permanently hidden in Initialize()
        if (total > 1)
        {
            // Show buttons and set interactability for multi-page cards
            if (leftButton != null) 
            {
                leftButton.gameObject.SetActive(true);
                leftButton.interactable = pageIndex > 0;
            }
            if (rightButton != null) 
            {
                rightButton.gameObject.SetActive(true);
                rightButton.interactable = pageIndex < total - 1;
            }
        }

        // Clues are children of the active page; no manual toggling needed

        // Safety: ensure the active page object stays enabled
        var activePage = GetActivePageObject();
        if (activePage != null && !activePage.activeSelf)
        {
            activePage.SetActive(true);
        }

        // Notify Fingerprint script of page change (for multi-page fingerprint controller on root)
        var fingerprintScript = GetComponent<Fingerprint>();
        if (fingerprintScript != null)
        {
            fingerprintScript.SetActivePage(pageIndex);
        }
    }

    public void GoToNextPage() { int total = pageObjects.Count; if (currentPageIndex < total - 1) SetPage(currentPageIndex + 1); }
    public void GoToPreviousPage() { if (currentPageIndex > 0) SetPage(currentPageIndex - 1); }
    public GameObject GetActivePageObject() { return (currentPageIndex >= 0 && currentPageIndex < pageObjects.Count) ? pageObjects[currentPageIndex] : null; }
    
}
