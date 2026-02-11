using UnityEngine;

public class CaseSlot : MonoBehaviour
{
    public RectTransform slotTransform;    // Set to center of mat area in Canvas
    public Card currentCard;      // Only ever one!

    // public void PlaceCase(Card caseCard)
    // {
    //     currentCard = caseCard;
    //     currentCard.cardVisual.transform.SetParent(slotTransform, false);
    // }

    // public void ClearCase()
    // {
    //     if (currentCard != null)
    //     {
    //         currentCard = null;
    //     }
    // }
}
