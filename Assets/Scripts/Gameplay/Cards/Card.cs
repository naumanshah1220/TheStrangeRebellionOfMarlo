using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
using System;
using DG.Tweening;



public enum CardLocation { Hand, Mat, Slot }

public class Card : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, 
    IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    private Canvas canvas;
    private Image imageComponent;
    [SerializeField] private bool instantiateVisual = true;
    private Vector3 offset;

    [Header("Movement")]
    [SerializeField] private float moveSpeedLimit = 50;

    [Header("Selection")]
    public bool selected;
    public float selectionOffset = 50;
    private float pointerDownTime;
    private float pointerUpTime;

    [Header("Visual")]
    [SerializeField] private GameObject cardVisualPrefab;
    [Tooltip("Used when ICardData.GetFullCardPrefab() is null (e.g. JSON-loaded evidence)")]
    [SerializeField] private GameObject fallbackBigCardPrefab;
    [HideInInspector] public CardVisual cardVisual;
    [HideInInspector] public BigCardVisual bigCardVisual;

    [Header("States")]
    public CardMode mode = CardMode.Case;
    public bool canBeSubmitted = false;
    [HideInInspector] public bool isDragLocked = false; // Prevents dragging when set to true

    public HorizontalCardHolder parentHolder;
    public HorizontalCardHolder homeHand; // The hand this card considers its "home"
    private CardLocation _cardLocation = CardLocation.Hand;
    public CardLocation cardLocation 
    { 
        get => _cardLocation; 
        set 
        { 
            if (_cardLocation != value)
            {
                _cardLocation = value;
                UpdateVisualBasedOnLocation();
            }
        } 
    }
    public bool isHovering;
    public bool isDragging;

    [HideInInspector] public bool wasDragged;

    public int preferredWidth = 1;
    public int preferredHeight = 1; 

    private Case caseInformation;     
    private Evidence evidenceInformation; 
    private PhoneCard phoneInformation;
    private ICardData cardData; // Generic card data for new card types 

    // --- Event Hooks ---
    [HideInInspector] public UnityEvent<Card> PointerEnterEvent;
    [HideInInspector] public UnityEvent<Card> PointerExitEvent;
    [HideInInspector] public UnityEvent<Card, bool> PointerUpEvent;
    [HideInInspector] public UnityEvent<Card> PointerDownEvent;
    [HideInInspector] public UnityEvent<Card> BeginDragEvent;
    [HideInInspector] public UnityEvent<Card> EndDragEvent;
    [HideInInspector] public UnityEvent<Card, bool> SelectEvent;

    void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        imageComponent = GetComponent<Image>();

        if (!instantiateVisual)
            return;
    }

    private void Update()
    {
        if (parentHolder.purpose == HolderPurpose.Hand)
            ClampPosition();

        if (isDragging)
        {
            Vector2 targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition) - offset;
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 velocity = direction * Mathf.Min(moveSpeedLimit, Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
            transform.Translate(velocity * Time.deltaTime);
        }
    }

    private void ClampPosition()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y -500f, screenBounds.y);
        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    // --- Initialize, Case/Evidence/Custom Data
    public void Initialize(object data, Transform smallVisualHandler, Transform bigVisualHandler = null)
    {
        // Use fallback if bigVisualHandler not provided (backward compatibility)
        if (bigVisualHandler == null)
            bigVisualHandler = smallVisualHandler;

        if (cardVisual == null && cardVisualPrefab != null)
        {
            GameObject visualInstance = Instantiate(cardVisualPrefab, smallVisualHandler ? smallVisualHandler.transform : canvas.transform);
            cardVisual = visualInstance.GetComponent<CardVisual>();
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;
            cardVisual.Initialize(this);
        }

        if (cardVisual == null)
            return;

        // Handle ICardData interface for new card types
        if (data is ICardData cardDataInterface)
        {
            cardData = cardDataInterface;
            mode = cardDataInterface.GetCardMode();

            // Set up card visual based on card mode
            switch (mode)
            {
                case CardMode.Case:
        if (data is Case caseData)
        {
            caseInformation = caseData;
            cardVisual.SetFromCase(caseData);
                    }
                    break;
                case CardMode.Evidence:
                    if (data is Evidence evidenceData)
                    {
                        evidenceInformation = evidenceData;
                        cardVisual.SetFromEvidence(evidenceData);
                    }
                    break;
                case CardMode.Phone:
                    if (data is PhoneCard phoneData)
                    {
                        phoneInformation = phoneData;
                        cardVisual.SetFromGenericCard(phoneData);
                    }
                    break;
                default:
                    // Handle other card types generically
                    cardVisual.SetFromGenericCard(cardDataInterface);
                    break;
            }

            // Create big card visual - use dedicated big visual handler
            // Prefer the card data's own prefab; fall back to the generic one on the slot prefab
            GameObject bigCardPrefab = cardDataInterface.GetFullCardPrefab() ?? fallbackBigCardPrefab;
            bool usingFallback = cardDataInterface.GetFullCardPrefab() == null && fallbackBigCardPrefab != null;
            if (bigCardVisual == null && bigCardPrefab != null)
            {
                GameObject fullCardInstance = Instantiate(bigCardPrefab, bigVisualHandler ? bigVisualHandler.transform : canvas.transform);
                bigCardVisual = fullCardInstance.GetComponent<BigCardVisual>();
                if (bigCardVisual != null)
                {
                    fullCardInstance.transform.localPosition = Vector3.zero;
                    fullCardInstance.transform.localRotation = Quaternion.identity;
                    fullCardInstance.transform.localScale = Vector3.one;
                    bigCardVisual.Initialize(this);

                    // Dynamically populate fallback BigCardVisual with evidence data
                    if (usingFallback && mode == CardMode.Evidence && evidenceInformation != null)
                        bigCardVisual.PopulateFromEvidence(evidenceInformation);

                    fullCardInstance.SetActive(false);
                }
            }
        }
        else
            Debug.LogWarning("[Card] Data does not implement ICardData interface");
        
    }

    // --- Full Card View (2D panel, not 3D) ---
   public void ToggleFullView(bool show)
    {
        if (bigCardVisual != null)
            bigCardVisual.ShowCard(show);

        if (cardVisual != null)
            cardVisual.ShowCard(!show);
    }
    
    /// <summary>
    /// Automatically update visuals based on card location
    /// </summary>
    private void UpdateVisualBasedOnLocation()
    {
        switch (cardLocation)
        {
            case CardLocation.Hand:
                // Show small visual, hide big visual
                if (cardVisual != null) cardVisual.ShowCard(true);
                if (bigCardVisual != null) bigCardVisual.ShowCard(false);
                break;
                
            case CardLocation.Mat:
            case CardLocation.Slot:
                // Show big visual, hide small visual  
                if (cardVisual != null) cardVisual.ShowCard(false);
                if (bigCardVisual != null) bigCardVisual.ShowCard(true);
                
                // Restore material states for fingerprint brushes when moving to Mat/Slot location
                // This ensures reveal effects persist when cards move between holders
                if (bigCardVisual != null)
                {
                    var fingerprint = bigCardVisual.GetComponent<Fingerprint>();
                    if (fingerprint != null)
                    {
                        fingerprint.RestoreAllMaterialStates();
                        Debug.Log($"[Card] Restored material states for fingerprint on card '{name}' when moving to {cardLocation}");
                    }
                }
                break;
        }
    }

    public Case GetCaseData() => (mode == CardMode.Case) ? caseInformation : null;
    public Evidence GetEvidenceData() => (mode == CardMode.Evidence) ? evidenceInformation : null;
    public PhoneCard GetPhoneData() => (mode == CardMode.Phone) ? phoneInformation : null;
    public ICardData GetCardData() => cardData;

    public bool IsFullViewActive()
    {
        return bigCardVisual != null && bigCardVisual.IsCardShown();
    }

    public bool IsCardVisualActive()
    {
        return cardVisual != null && cardVisual.IsCardShown();
    }
    
  public void SetFullViewParent(Transform newParent)
    {
        if (bigCardVisual != null)
        {
            var rect = bigCardVisual.GetComponent<RectTransform>();
            Vector3 worldPos = rect.position;
            Quaternion worldRot = rect.rotation;
            rect.SetParent(newParent, false);
            rect.position = worldPos;
            rect.rotation = worldRot;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardVisual.isFlyingIn)
            return;
        
        // Prevent dragging if card is locked (e.g., during disc insertion animation)
        if (isDragLocked)
            return;
            
        // Prevent dragging if parent holder is currently dealing cards
        if (parentHolder != null && parentHolder.IsDealing())
            return;
            
        isDragging = true;
        BeginDragEvent.Invoke(this);
        parentHolder.SelectCard(this);

        if (DragManager.Instance != null)
             DragManager.Instance.BeginDraggingCard(this);

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = mousePosition - (Vector2)transform.position;

        canvas.GetComponent<GraphicRaycaster>().enabled = false;
        imageComponent.raycastTarget = false;
        wasDragged = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // No need to put anything here! (handled by Update)
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragEvent.Invoke(this);
        parentHolder.DeselectCard();
        isDragging = false;
        canvas.GetComponent<GraphicRaycaster>().enabled = true;
        imageComponent.raycastTarget = true;

        StartCoroutine(FrameWait());
        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            wasDragged = false;
        }
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        PointerEnterEvent.Invoke(this);
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PointerExitEvent.Invoke(this);
        isHovering = false;
    }

    // private bool IsPointerOverOwnButton(PointerEventData eventData)
    // {
    //     if (eventData.pointerEnter == null) return false;
    //     // Check if it's inside our CardVisual or BigCardVisual
    //     bool inCardVisual = cardVisual && eventData.pointerEnter.transform.IsChildOf(cardVisual.transform);
    //     bool inBigCardVisual = bigCardVisual && eventData.pointerEnter.transform.IsChildOf(bigCardVisual.transform);

    //     if (!inCardVisual && !inBigCardVisual) return false;

    //     // Now, is it a Button or child of Button (up to the visual root)?
    //     if (eventData.pointerEnter.GetComponentInParent<Button>() != null)
    //         return true;

    //     return false;
    // }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        PointerDownEvent.Invoke(this);
        Select();
        pointerDownTime = Time.time;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        pointerUpTime = Time.time;
        PointerUpEvent.Invoke(this, pointerUpTime - pointerDownTime > .2f);
        Deselect();

        if (pointerUpTime - pointerDownTime > .2f)
            return;

        if (wasDragged)
            return;
    }

    public void Select()
    {
        if (!selected)
        {
            selected = true;
            parentHolder.SelectCard(this);
        }
    }

    public void Deselect()
    {
        if (selected)
        {
            selected = false;
            parentHolder.DeselectCard();
            transform.localPosition = Vector3.zero;
        }
    }

    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }

    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }

    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot") ? ExtensionMethods.Remap((float)ParentIndex(), 0, (float)(transform.parent.parent.childCount - 1), 0, 1) : 0;
    }

    private void OnDestroy()
    {
        DOTween.Kill(this.gameObject);        
        if (cardVisual != null)
            Destroy(cardVisual.gameObject);
        if (bigCardVisual != null)
            Destroy(bigCardVisual.gameObject);
    }
}
