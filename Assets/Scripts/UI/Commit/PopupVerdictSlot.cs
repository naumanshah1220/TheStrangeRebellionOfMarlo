using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Detective.UI.Commit
{
    public class PopupVerdictSlot : MonoBehaviour, IVerdictSlot
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI selectedOptionText;
        [SerializeField] private Button mainButton;
        [SerializeField] private VerdictOptionPopup popupPrefab;

        private VerdictSlotDefinition slotDefinition;
        private List<VerdictOption> availableOptions;
        private VerdictOption selectedOption;

        public string SlotId => slotDefinition?.slotId;
        public event System.Action OnSlotChanged;

        private void Awake()
        {
            mainButton.onClick.AddListener(OpenPopup);
        }

        public void Populate(VerdictSlotDefinition definition, List<VerdictOption> options)
        {
            slotDefinition = definition;
            availableOptions = options;
            labelText.text = definition.displayLabel;
            UpdateSelection(null);
        }

        public bool TryGetSelection(out CaseVerdict.SlotSelection selection, out string error)
        {
            selection = null;
            error = null;

            if (slotDefinition.required && selectedOption == null)
            {
                error = $"The '{slotDefinition.displayLabel}' field must be filled.";
                return false;
            }

            if (selectedOption != null)
            {
                selection = new CaseVerdict.SlotSelection
                {
                    slotId = this.SlotId,
                    optionId = selectedOption.id
                    // Justification tags will be handled by a separate UI element
                };
            }
            return true;
        }

        public bool TryGetOptions(out System.Collections.Generic.List<VerdictOption> options)
        {
            options = availableOptions;
            return options != null;
        }

        public string CurrentLabelOrBlank()
        {
            return selectedOption != null ? selectedOption.label : "______";
        }

        private void OpenPopup()
        {
            if (popupPrefab == null)
            {
                Debug.LogError("PopupPrefab is not assigned in the inspector for this slot!", this);
                return;
            }
            var popup = Instantiate(popupPrefab, transform.root);
            
            if (GameManager.Instance != null) // A simple check to add a debug log.
            {
                Debug.Log($"[PopupVerdictSlot] Opening popup for '{slotDefinition.displayLabel}'. Sending {availableOptions.Count} options to the popup.", this);
            }

            popup.Show(slotDefinition.displayLabel, availableOptions, (chosenOption) => {
                // If "Unknown" is chosen, we treat it as no selection (null)
                if (chosenOption != null && chosenOption.id == "unknown")
                {
                    UpdateSelection(null);
                }
                else
                {
                    UpdateSelection(chosenOption);
                }
            }, selectedOption?.id);
        }

        public void UpdateSelection(VerdictOption option)
        {
            selectedOption = option;
            selectedOptionText.text = selectedOption != null ? selectedOption.label : "Unknown";
            OnSlotChanged?.Invoke();
        }
    }
}
