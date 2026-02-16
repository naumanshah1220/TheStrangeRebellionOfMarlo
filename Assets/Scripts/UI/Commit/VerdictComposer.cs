using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Detective.UI.Commit;

namespace Detective.UI.Commit
{
    public class VerdictComposer : MonoBehaviour
    {
        public event System.Action OnVerdictStateChanged;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        [Header("UI References")]
        public Transform slotsContainer;
        public TMPro.TextMeshProUGUI fullSentencePreview;
        
        [Header("Slot Components")]
        [Tooltip("The slot for the suspect portrait scroller.")]
        public SuspectVerdictSlot suspectPortraitSlot;
        [Tooltip("The slot for the suspect name popup selector.")]
        public PopupVerdictSlot suspectNameSlot;
        [Tooltip("The drop zone for justification clue tags.")]
        public JustificationDropZone justificationDropZone;

        [Header("Slot Prefabs")]
        [Tooltip("Default prefab for all other slot types (e.g., Motive, Method).")]
        public GameObject genericSlotPrefab;

        private Case currentCase;
        private VerdictSchema schema;
        private System.Collections.Generic.List<IVerdictSlot> allSlots = new System.Collections.Generic.List<IVerdictSlot>();
        private bool isSyncingSuspects = false; // Prevents infinite loops
        private bool isBuilt = false;
        private bool isRestoringState = false;

        // Tracks option lists for FromDiscoveredTags slots so new clues can inject options
        private Dictionary<string, List<VerdictOption>> discoveredTagSlotOptions = new Dictionary<string, List<VerdictOption>>();

        public IEnumerator BuildFromCase(Case caseData, CaseVerdict draftVerdict)
        {
            if (isBuilt)
            {
                if(showDebugLogs) Debug.LogWarning("[VerdictComposer] BuildFromCase was called, but composer is already built. Clearing and rebuilding.");
                Clear();
            }

            currentCase = caseData;
            if (currentCase == null)
            {
                if(showDebugLogs) Debug.LogError("[VerdictComposer] BuildFromCase was called with a NULL case!");
                yield break;
            }

            if(showDebugLogs) Debug.Log($"[VerdictComposer] Building UI for case: '{currentCase.caseID}'");

            schema = currentCase.verdictSchema;
            if (schema == null) 
            {
                if(showDebugLogs) Debug.LogError($"[VerdictComposer] Case '{currentCase.caseID}' is missing a Verdict Schema. Assign one in the Case asset.");
                yield break;
            }

            Clear();
            var suspectsAsOptions = currentCase.suspects.Select(s => new VerdictOption { id = s.citizenID, label = s.FullName, type = VerdictSlotType.Suspect }).ToList();
            if(showDebugLogs) Debug.Log($"[VerdictComposer] Found {suspectsAsOptions.Count} suspects to use as options.");

            if (schema.slots == null || schema.slots.Count == 0)
            {
                if(showDebugLogs) Debug.LogWarning($"[VerdictComposer] The Verdict Schema '{schema.name}' has no slots defined in its 'Slots' list.");
                yield break;
            }

            if(showDebugLogs) Debug.Log($"[VerdictComposer] Schema has {schema.slots.Count} slots to create.");

            foreach (var def in schema.slots)
            {
                IVerdictSlot slot = null;
                if (def.type == VerdictSlotType.Suspect)
                {
                    if(showDebugLogs) Debug.Log($"[VerdictComposer] Processing 'Suspect' slot type.");
                    // This slot is handled by the dedicated suspectNameSlot
                    if (suspectNameSlot != null)
                    {
                        suspectNameSlot.Populate(def, suspectsAsOptions);
                        slot = suspectNameSlot;
                        suspectNameSlot.gameObject.SetActive(true);
                    }
                    else
                    {
                        if(showDebugLogs) Debug.LogWarning("[VerdictComposer] Suspect slot definition found in schema, but 'Suspect Name Slot' is not assigned in the Inspector.");
                    }
                }
                else
                {
                    if(showDebugLogs) Debug.Log($"[VerdictComposer] Processing generic slot type '{def.displayLabel}' ({def.type}).");
                    slot = CreateSlotUI(def);
                }

                if (slot != null)
                {
                    allSlots.Add(slot);
                    slot.OnSlotChanged += RefreshSentence;
                    slot.OnSlotChanged += HandleSlotChange;
                }
            }
            
            // Setup the portrait scroller separately as it's a special case
            if (suspectPortraitSlot != null)
            {
                // This ID MUST match the slotId defined in the VerdictSchema for the suspect.
                var portraitSlotDef = new VerdictSlotDefinition { slotId = "suspect", type = VerdictSlotType.Suspect };
                suspectPortraitSlot.Populate(portraitSlotDef, suspectsAsOptions);
                suspectPortraitSlot.gameObject.SetActive(true);
                suspectPortraitSlot.OnSlotChanged += HandleSlotChange;
            }

            // Hook up the two-way binding
            if (suspectNameSlot != null)
            {
                suspectNameSlot.OnSlotChanged += SyncSuspectPortrait;
                suspectNameSlot.OnSlotChanged += HandleSlotChange;
            }
            if (suspectPortraitSlot != null)
            {
                suspectPortraitSlot.OnSuspectChanged += SyncSuspectName; // Switched to the new event
                suspectPortraitSlot.OnSlotChanged += HandleSlotChange;
            }

            // Asynchronously restore the previous state AFTER setting up.
            yield return StartCoroutine(RestoreVerdictState(draftVerdict));

        // Manually set initial state to the first suspect to ensure consistency,
        // but only if a suspect wasn't already loaded in the draft.
        if (suspectPortraitSlot != null && (draftVerdict == null || draftVerdict.selections.All(s => s.slotId != suspectPortraitSlot.SlotId)))
        {
            suspectPortraitSlot.GoToPanel(0);
        }

            // Subscribe to clue discovery so FromDiscoveredTags slots update dynamically
            GameEvents.OnClueDiscovered += OnClueDiscovered;

            isBuilt = true;
            OnVerdictStateChanged?.Invoke(); // Let controller know we have an initial state
        }

        private void SyncSuspectPortrait()
        {
            if (isSyncingSuspects || isRestoringState) return;
            isSyncingSuspects = true;

            if (suspectNameSlot.TryGetSelection(out var selection, out _))
            {
                if (selection != null)
                {
                    int suspectIndex = currentCase.suspects.FindIndex(s => s.citizenID == selection.optionId);
                    suspectPortraitSlot.GoToPanel(suspectIndex);
                }
            }
            isSyncingSuspects = false;
            RefreshSentence();
        }

        private void SyncSuspectName(int panelIndex)
        {
            if (isSyncingSuspects || isRestoringState) return;
            isSyncingSuspects = true;

            if (showDebugLogs) Debug.Log($"[VerdictComposer] Syncing name to portrait panel: {panelIndex}");

            if (suspectNameSlot != null)
            {
                // We need to find the correct VerdictOption that corresponds to the panelIndex
                // and tell the popup slot to update.
                if (suspectPortraitSlot.TryGetOptions(out var suspectOptions) && panelIndex >= 0 && panelIndex < suspectOptions.Count)
                {
                    var selectedOption = suspectOptions[panelIndex];
                    suspectNameSlot.UpdateSelection(selectedOption);
                }
            }

            isSyncingSuspects = false;
            // We invoke the general state change handler AFTER the sync is complete.
            OnVerdictStateChanged?.Invoke();
        }

        private void SyncSuspectName()
        {
            if (isSyncingSuspects || isRestoringState) return;
            isSyncingSuspects = true;

            if (suspectPortraitSlot.TryGetSelection(out var selection, out _))
            {
                var option = new VerdictOption { id = selection.optionId, label = currentCase.suspects.Find(s => s.citizenID == selection.optionId).FullName };
                suspectNameSlot.UpdateSelection(option);
            }
            isSyncingSuspects = false;
            RefreshSentence();
        }

        public bool TryCompose(out CaseVerdict verdict, out string error)
        {
            verdict = new CaseVerdict();
            error = string.Empty;

            if (!isBuilt)
            {
                error = "Verdict composer has not been initialized with case data.";
                return false; 
            }

            verdict.caseID = currentCase.caseID;

            // Step 1: Gather all selections from the UI into a definitive list.
            var allSelections = new List<CaseVerdict.SlotSelection>();
            foreach (var slot in allSlots)
            {
                if (slot.TryGetSelection(out var sel, out _))
                {
                    if (sel != null) allSelections.Add(sel);
                }
            }
            
            // Step 2: Ensure the portrait scroller's selection is the authoritative one for the suspect.
            if (suspectPortraitSlot.TryGetSelection(out var suspectSel, out _))
            {
                // The schema requires a slot with id "suspect". The portrait scroller's selection must be mapped to that.
                // This is now redundant since the source ID is correct, but it provides good safety.
                suspectSel.slotId = "suspect"; 
                
                // Remove any suspect selection that might have come from the name slot
                allSelections.RemoveAll(s => s.slotId == "suspect");
                // Add the definitive selection from the portrait scroller
                allSelections.Add(suspectSel);
            }

            // Step 3: Get justification tags.
            if (justificationDropZone != null)
            {
                verdict.justificationTagIds = justificationDropZone.GetAttachedClueIds();
            }

            // Step 4: NOW, save the complete current state to the verdict object.
            verdict.selections = allSelections;
            verdict.computedConfidence = VerdictEvaluator.ComputeConfidence(currentCase, verdict);

            // Step 5: Perform completeness checks on the now-saved state.
            foreach (var def in schema.slots)
            {
                if (def.required && !verdict.selections.Any(s => s.slotId == def.slotId))
                {
                    error = $"The '{def.displayLabel}' field must be filled.";
                    return false; // Verdict is incomplete, but the partial draft is correct.
                }
            }

            if (schema.justification.required && verdict.justificationTagIds.Count == 0)
            {
                error = "At least one piece of evidence must be provided as justification.";
                return false;
            }

            return true;
        }

        private IVerdictSlot CreateSlotUI(VerdictSlotDefinition def)
        {
            if (genericSlotPrefab == null)
            {
                if (showDebugLogs) Debug.LogError("[VerdictComposer] Cannot create slot UI because 'Generic Slot Prefab' is not assigned in the Inspector.");
                return null;
            }

            GameObject slotGO = Instantiate(genericSlotPrefab, slotsContainer);
            var slotUI = slotGO.GetComponent<IVerdictSlot>();
            var options = new List<VerdictOption>();

            if (def.optionSource == OptionSource.FromDiscoveredTags)
            {
                // Build options only from clues the player has already discovered
                options = BuildOptionsFromDiscoveredClues(def);
                // Track this list so OnClueDiscovered can add to it later
                discoveredTagSlotOptions[def.slotId] = options;
                if (showDebugLogs) Debug.Log($"[VerdictComposer] FromDiscoveredTags slot '{def.displayLabel}': {options.Count} option(s) from already-discovered clues.");
            }
            else
            {
                if (schema.globalPools != null)
                {
                    foreach(var pool in schema.globalPools)
                    {
                        if (pool != null && pool.options != null)
                        {
                            options.AddRange(pool.options.Where(opt => opt.type == def.type));
                        }
                    }
                }

                // Fallback: if no options found from globalPools, extract from case solutions
                if (options.Count == 0 && currentCase.solutions != null)
                {
                    var seen = new HashSet<string>();
                    foreach (var solution in currentCase.solutions)
                    {
                        if (solution.answers == null) continue;
                        var slotAnswer = solution.answers.Find(a => a.slotId == def.slotId);
                        if (slotAnswer == null || slotAnswer.acceptedOptionIds == null) continue;

                        foreach (string optionId in slotAnswer.acceptedOptionIds)
                        {
                            if (seen.Add(optionId))
                            {
                                options.Add(new VerdictOption
                                {
                                    id = optionId,
                                    label = FormatOptionLabel(optionId),
                                    type = def.type
                                });
                            }
                        }
                    }
                    if (showDebugLogs && options.Count > 0)
                        Debug.Log($"[VerdictComposer] Extracted {options.Count} option(s) from case solutions for slot '{def.displayLabel}'.");
                }

                if (options.Count == 0 && showDebugLogs)
                    Debug.LogWarning($"[VerdictComposer] No options found for slot '{def.displayLabel}' ({def.type}). Check globalPools or case solutions.");
            }

            if (showDebugLogs) Debug.Log($"[VerdictComposer] Found {options.Count} options for slot type '{def.type}'. Populating slot '{def.displayLabel}'.");
            slotUI.Populate(def, options);
            return slotUI;
        }

        /// <summary>
        /// Builds verdict options from clueVerdictMappings for clues the player has already discovered.
        /// </summary>
        private List<VerdictOption> BuildOptionsFromDiscoveredClues(VerdictSlotDefinition def)
        {
            var options = new List<VerdictOption>();
            if (currentCase.clueVerdictMappings == null) return options;

            var cluesManager = CluesManager.Instance;
            var seen = new HashSet<string>();

            foreach (var mapping in currentCase.clueVerdictMappings)
            {
                if (mapping.slotId != def.slotId) continue;
                if (seen.Contains(mapping.optionId)) continue;
                if (cluesManager != null && cluesManager.IsClueFound(mapping.clueId))
                {
                    seen.Add(mapping.optionId);
                    options.Add(new VerdictOption
                    {
                        id = mapping.optionId,
                        label = mapping.label,
                        type = def.type
                    });
                }
            }
            return options;
        }

        /// <summary>
        /// Called when a clue is discovered. Adds new verdict options to FromDiscoveredTags slots.
        /// </summary>
        private void OnClueDiscovered(string clueId)
        {
            if (currentCase?.clueVerdictMappings == null) return;

            foreach (var mapping in currentCase.clueVerdictMappings)
            {
                if (mapping.clueId != clueId) continue;
                if (!discoveredTagSlotOptions.TryGetValue(mapping.slotId, out var optionsList)) continue;

                // Don't add duplicate options
                if (optionsList.Any(o => o.id == mapping.optionId)) continue;

                // Find the slot definition to get the type
                var slotDef = schema.slots.Find(s => s.slotId == mapping.slotId);
                var slotType = slotDef != null ? slotDef.type : VerdictSlotType.Violation;

                optionsList.Add(new VerdictOption
                {
                    id = mapping.optionId,
                    label = mapping.label,
                    type = slotType
                });

                if (showDebugLogs) Debug.Log($"[VerdictComposer] Clue '{clueId}' revealed verdict option '{mapping.label}' for slot '{mapping.slotId}'.");
            }
        }

        /// <summary>
        /// Converts an option ID like "obstruction_of_records" into a readable label "Obstruction Of Records".
        /// </summary>
        private static string FormatOptionLabel(string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) return optionId;
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(optionId.Replace('_', ' '));
        }

        public void RefreshSentence()
        {
            if (schema == null) return;
            var s = schema.sentenceTemplate;
            
            // Handle normal slots
            foreach (var slot in allSlots)
            {
                 s = s.Replace("{" + slot.SlotId + "}", $"<b>{slot.CurrentLabelOrBlank()}</b>");
            }
            // Handle suspect slot specifically, as it's not in the main list
            if (suspectPortraitSlot != null)
            {
                s = s.Replace("{suspect}", $"<b>{suspectPortraitSlot.CurrentLabelOrBlank()}</b>");
            }

            if (fullSentencePreview != null) fullSentencePreview.text = s;
        }

        public void Clear()
        {
            isBuilt = false;
            if (justificationDropZone != null) justificationDropZone.Clear();

            // Unsubscribe from clue discovery events
            GameEvents.OnClueDiscovered -= OnClueDiscovered;
            discoveredTagSlotOptions.Clear();

            // --- IMPORTANT: Unsubscribe from all events before destroying objects ---

            // Unsubscribe from the portrait scroller (which is not in the main slots list)
            if (suspectPortraitSlot != null)
            {
                suspectPortraitSlot.OnSuspectChanged -= SyncSuspectName; // Unsubscribe from the new event
                suspectPortraitSlot.OnSlotChanged -= HandleSlotChange;
            }

            // Unsubscribe from and destroy all dynamically created slots
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    slot.OnSlotChanged -= RefreshSentence;
                    slot.OnSlotChanged -= HandleSlotChange;

                    // The suspect name slot has an extra event listener
                    if (slot == suspectNameSlot)
                    {
                        slot.OnSlotChanged -= SyncSuspectPortrait;
                    }

                    // Destroy the slot's GameObject ONLY if it's not one of the persistent scene references
                    if (slot is MonoBehaviour mb && mb.gameObject != suspectNameSlot.gameObject)
                    {
                        Destroy(mb.gameObject);
                    }
                }
            }
            allSlots.Clear();
            
            // Hide the persistent slots
            if (suspectNameSlot != null) suspectNameSlot.gameObject.SetActive(false);
            if (suspectPortraitSlot != null) suspectPortraitSlot.gameObject.SetActive(false);
        }

        private IEnumerator RestoreVerdictState(CaseVerdict draftVerdict)
        {
            if (draftVerdict == null) yield break;

            // Temporarily detach the two-way binding to prevent race conditions during restore
            if (suspectNameSlot != null) suspectNameSlot.OnSlotChanged -= SyncSuspectPortrait;
            if (suspectPortraitSlot != null) suspectPortraitSlot.OnSuspectChanged -= SyncSuspectName;

            // Restore all "normal" slots from the main list (including the suspect name)
            foreach (var savedSelection in draftVerdict.selections)
            {
                var slot = allSlots.FirstOrDefault(s => s.SlotId == savedSelection.slotId);
                if (slot == null) continue;

                if (slot is PopupVerdictSlot popupSlot && slot.TryGetOptions(out var options))
                {
                    var optionToSelect = options.FirstOrDefault(o => o.id == savedSelection.optionId);
                    if (optionToSelect != null)
                    {
                        popupSlot.UpdateSelection(optionToSelect);
                    }
                }
            }

            // Separately, restore the suspect portrait scroller, which is not in the main list
            var suspectSelection = draftVerdict.selections.FirstOrDefault(s => s.slotId == "suspect");
            if (suspectSelection != null && suspectPortraitSlot != null)
            {
                if (suspectPortraitSlot.TryGetOptions(out var options))
                {
                    int index = options.FindIndex(o => o.id == suspectSelection.optionId);
                    if (index != -1)
                    {
                        suspectPortraitSlot.GoToPanel(index);
                    }
                }
            }

            // IMPORTANT: Wait one frame for UI animations (like the scroller) to settle.
            yield return null;

            // Re-attach the two-way binding
            if (suspectNameSlot != null) suspectNameSlot.OnSlotChanged += SyncSuspectPortrait;
            if (suspectPortraitSlot != null) suspectPortraitSlot.OnSuspectChanged += SyncSuspectName;
            
            // One final sync from PORTRAIT to NAME to ensure consistency after restore.
            if (suspectPortraitSlot != null)
            {
                SyncSuspectName(suspectPortraitSlot.GetCenteredPanel());
            }
        }

        private void HandleSlotChange()
        {
            OnVerdictStateChanged?.Invoke();
        }

        private void OnDestroy()
        {
            GameEvents.OnClueDiscovered -= OnClueDiscovered;
        }
    }
}

