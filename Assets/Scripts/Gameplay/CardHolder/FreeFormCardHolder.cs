// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using DG.Tweening;
// using System.Linq;

// /// <summary>
// /// Free-form card holder that allows cards to be placed anywhere on the surface
// /// Similar to Papers Please gameplay
// /// </summary>
// public class FreeFormCardHolder : MonoBehaviour
// {
//     [Header("Card Mode")]
//     public HolderType type = HolderType.Mat;
//     public CardMode mode = CardMode.Evidence;

//     [Header("References")]
//     public Transform visualHandler;
//     [SerializeField] private GameObject slotPrefab;

//     [Header("Free-Form Settings")]
//     public bool enableFreeForm = true;
//     public float snapDistance = 50f;
//     public LayerMask cardLayerMask = -1;

//     [Header("Spawn Settings")]
//     public List<Card> cards = new List<Card>();
//     [SerializeField] private float fallback_slotSizeModifier = 2;

//     [Header("Card Deal Animation")]
//     public Transform cardStartPoint;
//     public float dealDelay = 0.1f;
//     public float dealFlyTime = 0.3f;

//     private RectTransform rect;
//     private Camera mainCamera;

//     void Start()
//     {
//         mainCamera = Camera.main;
//         rect = GetComponent<RectTransform>();
//     }

//     void Update()
//     {
//         if (enableFreeForm)
//         {
//             HandleFreeFormInteractions();
//         }
//     }

//     private void HandleFreeFormInteractions()
//     {
//         // Sort cards by their visual order (last moved/dropped should be on top)
//         UpdateCardSortingOrder();
//     }

//     private void UpdateCardSortingOrder()
//     {
//         // Sort cards by their last interaction time or position
//         for (int i = 0; i < cards.Count; i++)
//         {
//             if (cards[i].bigCardVisual != null)
//             {
//                 var canvas = cards[i].bigCardVisual.GetComponent<Canvas>();
//                 if (canvas != null)
//                 {
//                     canvas.sortingOrder = i;
//                 }
//             }
//         }
//     }

//     public void AddCardToHand(Card card, Vector3? worldPosition = null)
//     {
//         if (card == null) return;

//         // Create slot if needed
//         if (card.transform.parent == null)
//         {
//             GameObject slot = Instantiate(slotPrefab, transform);
//             card.transform.SetParent(slot.transform);
//         }

//         // Reactivate the slot and card
//         if (card.transform.parent != null)
//             card.transform.parent.gameObject.SetActive(true);

//         card.gameObject.SetActive(true);
//         card.parentHolder = null; // Remove reference to old holder since this is free-form

//         // Set card properties
//         card.cardLocation = CardLocation.Mat;
//         card.transform.localScale = new Vector3(fallback_slotSizeModifier, fallback_slotSizeModifier, fallback_slotSizeModifier);

//         // Position the card
//         if (worldPosition.HasValue && enableFreeForm)
//         {
//             PositionCardAtWorldPoint(card, worldPosition.Value);
//         }
//         else
//         {
//             // Default positioning (center of mat)
//             card.transform.position = transform.position;
//         }

//         // Set up visuals
//         if (card.cardVisual != null)
//             card.cardVisual.transform.SetParent(visualHandler);
//         if (card.bigCardVisual != null)
//             card.bigCardVisual.transform.SetParent(visualHandler);

//         // Add to cards list
//         if (!cards.Contains(card))
//         {
//             cards.Add(card);
//         }

//         // Show big card visual on mat
//         card.ToggleFullView(true);

//         // Update sorting order
//         UpdateCardSortingOrder();
//     }

//     public void AddCardToHandAtPosition(Card card, Vector2 screenPosition)
//     {
//         Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
//         AddCardToHand(card, worldPos);
//     }

//     private void PositionCardAtWorldPoint(Card card, Vector3 worldPosition)
//     {
//         if (enableFreeForm)
//         {
//             // Convert world position to local position within the mat bounds
//             Vector3 localPos = transform.InverseTransformPoint(worldPosition);
            
//             // Clamp to mat bounds
//             RectTransform matRect = GetComponent<RectTransform>();
//             if (matRect != null)
//             {
//                 Vector2 matSize = matRect.sizeDelta;
//                 localPos.x = Mathf.Clamp(localPos.x, -matSize.x / 2, matSize.x / 2);
//                 localPos.y = Mathf.Clamp(localPos.y, -matSize.y / 2, matSize.y / 2);
//                 localPos.z = 0;
//             }

//             card.transform.localPosition = localPos;
//         }
//     }

//     public void RemoveCard(Card card)
//     {
//         if (cards.Contains(card))
//         {
//             cards.Remove(card);
//             card.gameObject.SetActive(false);

//             if (card.transform.parent != null)
//                 card.transform.parent.gameObject.SetActive(false);
//         }
//     }

//     public Vector3 GetValidDropPosition(Vector2 screenPosition)
//     {
//         // Convert screen position to world position
//         Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        
//         // Check if position is within mat bounds
//         if (IsPositionWithinBounds(worldPos))
//         {
//             return worldPos;
//         }

//         // Return center if outside bounds
//         return transform.position;
//     }

//     private bool IsPositionWithinBounds(Vector3 worldPosition)
//     {
//         RectTransform matRect = GetComponent<RectTransform>();
//         if (matRect == null) return true;

//         Vector2 localPoint;
//         RectTransformUtility.ScreenPointToLocalPointInRectangle(
//             matRect, 
//             mainCamera.WorldToScreenPoint(worldPosition), 
//             mainCamera, 
//             out localPoint
//         );

//         return matRect.rect.Contains(localPoint);
//     }

//     public void MoveCardToPosition(Card card, Vector3 worldPosition)
//     {
//         if (!cards.Contains(card)) return;

//         PositionCardAtWorldPoint(card, worldPosition);
        
//         // Bring moved card to front
//         cards.Remove(card);
//         cards.Add(card);
//         UpdateCardSortingOrder();
//     }

//     public Card GetTopCardAtPosition(Vector2 screenPosition)
//     {
//         // Return the topmost card at the given screen position
//         // This is useful for detecting which card the player is interacting with
//         Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        
//         // Check cards in reverse order (topmost first)
//         for (int i = cards.Count - 1; i >= 0; i--)
//         {
//             Card card = cards[i];
//             if (card.bigCardVisual != null)
//             {
//                 RectTransform cardRect = card.bigCardVisual.GetComponent<RectTransform>();
//                 if (cardRect != null && RectTransformUtility.RectangleContainsScreenPoint(cardRect, screenPosition, mainCamera))
//                 {
//                     return card;
//                 }
//             }
//         }

//         return null;
//     }

//     public void LoadCardsFromData<T>(List<T> dataList, bool deleteOld = true) where T : ICardData
//     {
//         StartCoroutine(SetupCardsAndDealVisuals(dataList, deleteOld));
//     }

//     private IEnumerator SetupCardsAndDealVisuals<T>(List<T> dataList, bool deleteOld) where T : ICardData
//     {
//         if (deleteOld)
//         {
//             // Clear old slots/cards
//             foreach (Transform child in transform)
//                 Destroy(child.gameObject);
//             cards.Clear();
//         }

//         // Create cards with free-form positioning
//         for (int i = 0; i < dataList.Count; i++)
//         {
//             GameObject slot = Instantiate(slotPrefab, transform);
//             Card card = slot.GetComponentInChildren<Card>();
//             if (card == null)
//             {
//                 Debug.LogError($"No Card found in slot prefab at index {i}");
//                 continue;
//             }

//             card.name = i.ToString();
//             card.Initialize(dataList[i], visualHandler);
            
//             // Position cards in a slight offset pattern for free-form
//             Vector3 offset = new Vector3((i % 3) * 100f - 100f, (i / 3) * 120f - 60f, 0);
//             card.transform.localPosition = offset;

//             // Hide visual for "deal in" animation
//             if (card.cardVisual != null)
//                 card.cardVisual.gameObject.SetActive(false);

//             cards.Add(card);

//             yield return new WaitForSeconds(dealDelay);

//             // Deal in animation
//             if (card.cardVisual != null)
//             {
//                 card.cardVisual.gameObject.SetActive(true);
//                 card.cardVisual.isFlyingIn = true;
//                 if (cardStartPoint != null)
//                 {
//                     card.cardVisual.transform.position = cardStartPoint.position;
//                     card.cardVisual.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
//                 }
//             }
//         }
//     }
// } 