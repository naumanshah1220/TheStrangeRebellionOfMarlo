using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Orchestrates the stress bar, zone label, and heartbeat ECG line.
/// Polls Citizen.CurrentStress each frame for smooth interpolation.
/// </summary>
public class StressIndicatorPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeartbeatLine heartbeatLine;
    [SerializeField] private Image stressBarFill;
    [SerializeField] private TextMeshProUGUI zoneLabel;
    [SerializeField] private GameObject toneSelectorPanel;

    [Header("Bar Settings")]
    [SerializeField] private float barLerpSpeed = 5f;
    [SerializeField] private float colorTransitionDuration = 0.3f;

    [Header("Zone Colors")]
    [SerializeField] private Color lawyeredUpColor = new Color(0.6f, 0.85f, 1.0f);
    [SerializeField] private Color deflectingColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color sweetSpotColor = new Color(1.0f, 0.85f, 0.2f);
    [SerializeField] private Color rattledColor = new Color(1.0f, 0.55f, 0.1f);
    [SerializeField] private Color shutdownColor = new Color(0.9f, 0.15f, 0.15f);

    [Header("Zone Names")]
    [SerializeField] private string lawyeredUpName = "LAWYERED UP";
    [SerializeField] private string deflectingName = "DEFLECTING";
    [SerializeField] private string sweetSpotName = "SWEET SPOT";
    [SerializeField] private string rattledName = "RATTLED";
    [SerializeField] private string shutdownName = "SHUTDOWN";

    private float displayedFill;
    private StressZone currentZone = (StressZone)(-1); // force first update
    private Tweener barColorTween;

    private void OnEnable()
    {
        if (InterrogationManager.Instance != null)
            InterrogationManager.Instance.OnSuspectChanged += OnSuspectChanged;

        // Start hidden — will show when a suspect is actively interrogated
        wasActive = false;
        if (heartbeatLine != null) heartbeatLine.enabled = false;
        if (stressBarFill != null) stressBarFill.gameObject.SetActive(false);
        if (zoneLabel != null) zoneLabel.gameObject.SetActive(false);
        if (toneSelectorPanel != null) toneSelectorPanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (InterrogationManager.Instance != null)
            InterrogationManager.Instance.OnSuspectChanged -= OnSuspectChanged;

        barColorTween?.Kill();
    }

    private bool wasActive;

    private void Update()
    {
        var conversation = InterrogationManager.Instance != null
            ? InterrogationManager.Instance.GetCurrentConversation()
            : null;

        bool hasActiveSuspect = conversation != null && conversation.citizen != null;

        // Hide/show the entire panel based on whether we have an active suspect
        if (hasActiveSuspect != wasActive)
        {
            wasActive = hasActiveSuspect;
            if (heartbeatLine != null) heartbeatLine.enabled = hasActiveSuspect;
            if (stressBarFill != null) stressBarFill.gameObject.SetActive(hasActiveSuspect);
            if (zoneLabel != null) zoneLabel.gameObject.SetActive(hasActiveSuspect);
            if (toneSelectorPanel != null) toneSelectorPanel.SetActive(hasActiveSuspect);
        }

        if (!hasActiveSuspect) return;

        float stress = conversation.citizen.CurrentStress;
        StressZone zone = conversation.citizen.GetCurrentStressZone();

        // Smooth bar fill
        displayedFill = Mathf.Lerp(displayedFill, stress, barLerpSpeed * Time.deltaTime);
        if (stressBarFill != null)
            stressBarFill.fillAmount = displayedFill;

        // Update heartbeat
        if (heartbeatLine != null)
            heartbeatLine.SetStress(stress);

        // Zone change detection
        if (zone != currentZone)
            ApplyZone(zone, false);
    }

    private void OnSuspectChanged(string suspectId)
    {
        var conversation = InterrogationManager.Instance != null
            ? InterrogationManager.Instance.GetCurrentConversation()
            : null;

        if (conversation == null || conversation.citizen == null) return;

        float stress = conversation.citizen.CurrentStress;
        StressZone zone = conversation.citizen.GetCurrentStressZone();

        // Snap — don't lerp — on suspect switch
        displayedFill = stress;
        if (stressBarFill != null)
            stressBarFill.fillAmount = stress;

        ApplyZone(zone, true);
    }

    private void ApplyZone(StressZone zone, bool snap)
    {
        currentZone = zone;
        Color zoneColor = GetZoneColor(zone);

        // Label
        if (zoneLabel != null)
        {
            zoneLabel.text = GetZoneName(zone);
            zoneLabel.color = zoneColor;
        }

        // Bar color
        if (stressBarFill != null)
        {
            barColorTween?.Kill();
            if (snap)
            {
                stressBarFill.color = zoneColor;
            }
            else
            {
                barColorTween = stressBarFill.DOColor(zoneColor, colorTransitionDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        // Heartbeat color
        if (heartbeatLine != null)
            heartbeatLine.SetZone(zone);
    }

    private Color GetZoneColor(StressZone zone)
    {
        return zone switch
        {
            StressZone.LawyeredUp => lawyeredUpColor,
            StressZone.Deflecting => deflectingColor,
            StressZone.SweetSpot => sweetSpotColor,
            StressZone.Rattled => rattledColor,
            StressZone.Shutdown => shutdownColor,
            _ => sweetSpotColor
        };
    }

    private string GetZoneName(StressZone zone)
    {
        return zone switch
        {
            StressZone.LawyeredUp => lawyeredUpName,
            StressZone.Deflecting => deflectingName,
            StressZone.SweetSpot => sweetSpotName,
            StressZone.Rattled => rattledName,
            StressZone.Shutdown => shutdownName,
            _ => sweetSpotName
        };
    }
}
