using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DanielLochner.Assets.SimpleScrollSnap;
using System.Collections;
using System.Linq;

namespace Detective.UI.Commit
{
    public class SuspectVerdictSlot : MonoBehaviour, IVerdictSlot
    {
        [Header("System References")]
        [SerializeField] private SimpleScrollSnap suspectScrollSnap;
        [SerializeField] private GameObject suspectItemPrefab; // <-- ADD THIS FIELD

        public int GetCenteredPanel()
        {
            return suspectScrollSnap != null ? suspectScrollSnap.CenteredPanel : -1;
        }

        private VerdictSlotDefinition slotDefinition;
        private List<VerdictOption> availableOptions;

        public string SlotId => slotDefinition.slotId;
        public event System.Action OnSlotChanged;
        public event System.Action<int> OnSuspectChanged; // More specific event

        private void Awake()
        {
            if (suspectScrollSnap == null)
            {
                suspectScrollSnap = GetComponentInChildren<SimpleScrollSnap>();
            }
            suspectScrollSnap.OnPanelSelected.AddListener(HandlePanelSelected);
        }

        private void OnDestroy()
        {
            if (suspectScrollSnap != null)
            {
                suspectScrollSnap.OnPanelSelected.RemoveListener(HandlePanelSelected);
            }
        }

        private void HandlePanelSelected(int panelIndex)
        {
            // The SimpleScrollSnap event fires before its internal state updates.
            // We wait one frame to ensure we get the correct, new panel index.
            StartCoroutine(DelayedSync());
        }

        private IEnumerator DelayedSync()
        {
            yield return null; // Wait one frame
            int currentPanel = suspectScrollSnap.CenteredPanel;
            OnSlotChanged?.Invoke();
            OnSuspectChanged?.Invoke(currentPanel);
        }

        public bool TryGetOptions(out List<VerdictOption> options)
        {
            options = availableOptions;
            return options != null;
        }

        public void Populate(VerdictSlotDefinition definition, List<VerdictOption> options)
        {
            slotDefinition = definition;
            availableOptions = options;

            // Clear any existing suspects
            while (suspectScrollSnap.NumberOfPanels > 0)
            {
                suspectScrollSnap.RemoveFromBack();
            }

            if (suspectItemPrefab == null)
            {
                Debug.LogError("[SuspectVerdictSlot] Suspect Item Prefab is not assigned! Cannot populate suspects.", this);
                return;
            }

            // Populate with new suspects
            foreach (var option in availableOptions)
            {
                GameObject suspectGO = Instantiate(suspectItemPrefab);
                var suspectItem = suspectGO.GetComponent<SuspectItem>();
                if (suspectItem != null)
                {
                    // This is a bit of a workaround. SuspectItem expects a Citizen object.
                    // We need to find the full Citizen object from the GameManager or another source.
                    // For now, let's assume we can get it from a central place.
                    var citizen = GameManager.Instance.CurrentCase.suspects.Find(s => s.citizenID == option.id);
                    if (citizen != null)
                    {
                        suspectItem.Setup(citizen);
                        suspectScrollSnap.AddToBack(suspectGO);
                    }
                }
                // The scroll snap creates a copy, so we destroy our original instance.
                Destroy(suspectGO);
            }
        }

        public bool TryGetSelection(out CaseVerdict.SlotSelection selection, out string error)
        {
            selection = new CaseVerdict.SlotSelection { slotId = slotDefinition.slotId };
            error = string.Empty;

            if (suspectScrollSnap == null || availableOptions == null || availableOptions.Count == 0)
            {
                error = "Suspect slot is not properly initialized.";
                return false;
            }

            int currentPanel = suspectScrollSnap.CenteredPanel;
            if (currentPanel >= 0 && currentPanel < availableOptions.Count)
            {
                selection.optionId = availableOptions[currentPanel].id;
                return true;
            }
            
            error = "No suspect selected.";
            return false;
        }

        public string CurrentLabelOrBlank()
        {
            if (suspectScrollSnap == null || availableOptions == null || availableOptions.Count == 0)
            {
                return string.Empty;
            }

            int currentPanel = suspectScrollSnap.CenteredPanel;
            if (currentPanel >= 0 && currentPanel < availableOptions.Count)
            {
                return availableOptions[currentPanel].label;
            }
            
            return string.Empty;
        }
        
        public int GetCurrentPanel()
        {
            return suspectScrollSnap != null ? suspectScrollSnap.SelectedPanel : -1;
        }

        public void GoToPanel(int panelIndex)
        {
            if (suspectScrollSnap != null && panelIndex >= 0 && panelIndex < suspectScrollSnap.NumberOfPanels)
            {
                suspectScrollSnap.GoToPanel(panelIndex);
            }
        }
    }
}
