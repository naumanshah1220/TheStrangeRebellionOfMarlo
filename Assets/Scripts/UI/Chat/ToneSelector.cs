using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple 3-button tone selector for interrogation.
/// Sets the citizen's InterrogationTone which affects stress direction.
/// </summary>
public class ToneSelector : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button calmButton;
    [SerializeField] private Button neutralButton;
    [SerializeField] private Button firmButton;

    [Header("Visual Feedback")]
    [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);

    private InterrogationTone currentTone = InterrogationTone.Neutral;

    private void OnEnable()
    {
        if (calmButton != null) calmButton.onClick.AddListener(() => SetTone(InterrogationTone.Calm));
        if (neutralButton != null) neutralButton.onClick.AddListener(() => SetTone(InterrogationTone.Neutral));
        if (firmButton != null) firmButton.onClick.AddListener(() => SetTone(InterrogationTone.Firm));

        if (InterrogationManager.Instance != null)
            InterrogationManager.Instance.OnSuspectChanged += OnSuspectChanged;

        SetTone(InterrogationTone.Neutral);
    }

    private void OnDisable()
    {
        if (calmButton != null) calmButton.onClick.RemoveAllListeners();
        if (neutralButton != null) neutralButton.onClick.RemoveAllListeners();
        if (firmButton != null) firmButton.onClick.RemoveAllListeners();

        if (InterrogationManager.Instance != null)
            InterrogationManager.Instance.OnSuspectChanged -= OnSuspectChanged;
    }

    private void SetTone(InterrogationTone tone)
    {
        currentTone = tone;

        var conversation = InterrogationManager.Instance != null
            ? InterrogationManager.Instance.GetCurrentConversation()
            : null;

        if (conversation != null && conversation.citizen != null)
            conversation.citizen.SetInterrogationTone(tone);

        UpdateVisuals();
    }

    private void OnSuspectChanged(string suspectId)
    {
        SetTone(InterrogationTone.Neutral);
    }

    private void UpdateVisuals()
    {
        SetButtonColor(calmButton, currentTone == InterrogationTone.Calm);
        SetButtonColor(neutralButton, currentTone == InterrogationTone.Neutral);
        SetButtonColor(firmButton, currentTone == InterrogationTone.Firm);
    }

    private void SetButtonColor(Button button, bool active)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null)
            image.color = active ? activeColor : inactiveColor;
    }
}
