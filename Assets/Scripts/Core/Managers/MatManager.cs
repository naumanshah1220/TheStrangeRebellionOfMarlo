using DG.Tweening;
using UnityEngine;

public class MatManager : SingletonMonoBehaviour<MatManager>
{

    [Header("References")]
    public HorizontalCardHolder matHand;
    public HorizontalCardHolder caseHand; // The evidence area on the mat
    public HorizontalCardHolder caseSlot;

    private GameManager gM;

    public Card currentCaseCard;

    protected override void OnSingletonAwake() { }

    void Start()
    {
        gM = GameManager.Instance;
        currentCaseCard = null;
    }

    // 1. Place the case in the case slot
    public void PlaceCase(Card caseCard)
    {

        if (gM.CurrentCase != null && gM.CurrentCase != caseCard)
        {
            gM.CurrentCase = null;
        }

        currentCaseCard = caseCard;
        caseHand.RemoveCard(caseCard);
        caseSlot.AddCardToHand(caseCard);

        caseCard.cardLocation = CardLocation.Slot;
        caseCard.canBeSubmitted = false;
        
        // Ensure evidenceScroller is active so mat area is usable
        if (UIManager.Instance != null && UIManager.Instance.evidenceScroller != null)
        {
            UIManager.Instance.evidenceScroller.gameObject.SetActive(true);
            Debug.Log("[MatManager] Enabled evidenceScroller for mat area accessibility");
        }

        if (caseCard != null && caseCard.mode == CardMode.Case)
        {
            var caseData = caseCard.GetCaseData();
            if (caseData != null && GameManager.Instance.CurrentCase != caseData)
                GameManager.Instance.OpenCase(caseData);
        }

    }

    // 2. Place an evidence card into the mat hand
    public void PlaceEvidence(Card evidenceCard)
    {
        matHand.AddCardToHand(evidenceCard);
    }

    // 3. Remove an evidence card from the mat hand
    public void RemoveEvidence(Card evidenceCard)
    {
        matHand.RemoveCard(evidenceCard);
    }

    public void EnableCaseSubmission()
    {
        if (gM.CurrentCase != null && caseSlot.Cards.Count > 0)
        {
            caseSlot.Cards[0].canBeSubmitted = true;
        }
    }
    
}
