using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SpectrographSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private ForeignSubstanceDatabase database;

    [Header("Card Slot (Evidence only)")]
    [SerializeField] private HorizontalCardHolder cardSlot; // visualMode: BigCards, accepted: Evidence, purpose: Spectrograph

    [Header("Spectrum Bands (fixed ROYGBIV order)")]
    [Tooltip("Assign 7 band Images in order: Red, Orange, Yellow, Green, Blue, Indigo, Violet")] 
    [SerializeField] private Image[] spectrumBands = new Image[7];
    [SerializeField, Range(1f, 50f)] private float minBandWidth = 6f;
    [SerializeField, Range(1f, 100f)] private float maxBandWidth = 28f;

    [Header("Debug")] 
    [SerializeField] private bool debugLogs = false;

    private Card currentCard;
    private RectTransform[] bandRects = new RectTransform[7];
    private float[] bandBaselineWidths = new float[7];
    private Sprite whiteSprite;

    public HorizontalCardHolder CardSlot => cardSlot;

    private void Awake()
    {
        // Ensure we have a drawable white sprite for bands with no sprite assigned
        if (whiteSprite == null)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        // Cache rects and baseline widths
        for (int i = 0; i < spectrumBands.Length; i++)
        {
            if (spectrumBands[i] == null) continue;
            if (spectrumBands[i].sprite == null)
            {
                spectrumBands[i].sprite = whiteSprite;
                spectrumBands[i].type = Image.Type.Simple;
                spectrumBands[i].preserveAspect = false;
            }
            var rt = spectrumBands[i].GetComponent<RectTransform>();
            bandRects[i] = rt;
            if (rt != null) bandBaselineWidths[i] = rt.sizeDelta.x;
            // Start hidden until a card is applied
            spectrumBands[i].gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        MonitorCardSlot();
    }

    public bool CanAcceptCard(Card card)
    {
        if (card == null) return false;
        if (card.mode != CardMode.Evidence) return false;
        if (cardSlot == null) { Debug.LogError("[SpectrographSystem] cardSlot not assigned"); return false; }
        // Only allow if slot empty (single card)
        if (cardSlot.Cards.Count > 0) return false;
        return true;
    }

    public void HandleCardDropped(Card card)
    {
        if (!CanAcceptCard(card)) return;
        if (cardSlot == null) return;

        // Remove from previous holder
        if (card.parentHolder != null)
        {
            card.parentHolder.RemoveCard(card);
        }

        // Add to spectrograph slot and force big visual
        cardSlot.AddCardToHand(card);
        if (card.parentHolder != null)
        {
            card.cardLocation = CardLocation.Mat; // show big
        }

        currentCard = card;
        RefreshForCurrentCard();
    }

    public void HandleCardHoverStart(Card card) { }
    public void HandleCardHoverEnd(Card card) { }

    private void MonitorCardSlot()
    {
        if (cardSlot == null) return;
        if (currentCard != null && !cardSlot.Cards.Contains(currentCard))
        {
            OnCardRemoved();
        }
        if (currentCard == null && cardSlot.Cards.Count > 0)
        {
            currentCard = cardSlot.Cards[0];
            RefreshForCurrentCard();
        }
    }

    private void OnCardRemoved()
    {
        currentCard = null;
        // Hide all bands
        for (int i = 0; i < spectrumBands.Length; i++)
        {
            if (spectrumBands[i] != null) spectrumBands[i].gameObject.SetActive(false);
        }
    }

    private void RefreshForCurrentCard()
    {
        if (currentCard == null || database == null)
        {
            OnCardRemoved();
            return;
        }
        var ev = currentCard.GetEvidenceData();
        if (ev == null || ev.foreignSubstance == ForeignSubstanceType.None)
        {
            OnCardRemoved();
            return;
        }
        if (!ValidateBandsAssigned())
        {
            Debug.LogWarning("[SpectrographSystem] spectrumBands not assigned correctly (need 7 Images)");
            OnCardRemoved();
            return;
        }

        if (database.TryGetBandPattern(ev.foreignSubstance, out var pattern))
        {
            ApplyBandPattern(pattern);
        }
        else
        {
            if (debugLogs) Debug.LogWarning($"[SpectrographSystem] No band pattern found for {ev.foreignSubstance}");
            OnCardRemoved();
        }
    }

    private void ApplyBandPattern(ForeignSubstanceDatabase.BandPattern pattern)
    {
        if (pattern.enabled == null || pattern.widths == null || pattern.enabled.Length != 7 || pattern.widths.Length != 7)
        {
            if (debugLogs) Debug.LogWarning("[SpectrographSystem] Invalid band pattern sizes");
            OnCardRemoved();
            return;
        }

        for (int i = 0; i < 7; i++)
        {
            var img = spectrumBands[i];
            if (img == null) continue;
            img.gameObject.SetActive(pattern.enabled[i]);

            var rt = bandRects[i];
            if (rt != null)
            {
                var sd = rt.sizeDelta;
                var minW = Mathf.Min(minBandWidth, maxBandWidth);
                var maxW = Mathf.Max(minBandWidth, maxBandWidth);
                // Remap database width (canonical 6..28) into current min/max range
                const float dbMin = 6f;
                const float dbMax = 28f;
                float t = Mathf.InverseLerp(dbMin, dbMax, pattern.widths[i]);
                sd.x = Mathf.Lerp(minW, maxW, t);
                rt.sizeDelta = sd;
            }
            // Preserve the band's preset alpha; do not override color.a
        }
    }

    private bool ValidateBandsAssigned()
    {
        if (spectrumBands == null || spectrumBands.Length != 7) return false;
        for (int i = 0; i < 7; i++) if (spectrumBands[i] == null) return false;
        return true;
    }
}


