using System.Collections.Generic;
using UnityEngine;
using Detective.Gameplay;

[CreateAssetMenu(fileName = "NewCitizen", menuName = "Citizens/Citizen")]
public class Citizen : ScriptableObject
{
    [Header("Identification")]
    public string citizenID;
    public string firstName;
    public string lastName;
    
    [Header("Personal Information")]
    public Sprite picture;
    public string dateOfBirth; // Format: MM/DD/YYYY
    public Gender gender = Gender.Male;
    public Ethnicity ethnicity = Ethnicity.Caucasian;
    public MaritalStatus maritalStatus = MaritalStatus.Single;
    public string address;
    public string occupation = ""; // Optional
    
    [Header("Criminal Record")]
    public List<CriminalRecord> criminalHistory = new List<CriminalRecord>();
    
    [Header("Biometric Data")]
    public List<Sprite> fingerprints = new List<Sprite>(5); // Exactly 5 fingerprints
    
    [Header("Interrogation Data")]
    [Tooltip("How nervous this suspect gets under pressure (affects response timing)")]
    [Range(0f, 1f)]
    public float nervousnessLevel = 0.3f;

    [Tooltip("Override starting stress (0-1). Negative means auto-calculate from nervousnessLevel.")]
    public float initialStress = -1f;

    [Tooltip("Whether this person is guilty in the current case")]
    public bool isGuilty = false;
    
    [Header("Tag Interactions")]
    [Tooltip("Specific interactions for tags this suspect knows about")]
    public TagInteraction[] tagInteractions = new TagInteraction[0];
    
    [Header("Generic Questions")]
    [Tooltip("Generic questions for different tag types when suspect doesn't have specific knowledge")]
    public GenericQuestion[] genericQuestions = new GenericQuestion[]
    {
        new GenericQuestion { tagType = "person", questions = new string[] { "Tell me about '{tag}'", "Do you know '{tag}'?", "What's your relationship with '{tag}'?" } },
        new GenericQuestion { tagType = "location", questions = new string[] { "Tell me about '{tag}'", "Have you been to '{tag}'?", "What do you know about '{tag}'?" } },
        new GenericQuestion { tagType = "item", questions = new string[] { "Tell me about '{tag}'", "Do you recognize '{tag}'?", "What do you know about '{tag}'?" } },
        new GenericQuestion { tagType = "date", questions = new string[] { "Tell me about '{tag}'", "What happened on '{tag}'?", "Do you remember '{tag}'?" } },
        new GenericQuestion { tagType = "time", questions = new string[] { "Tell me about '{tag}'", "What were you doing at '{tag}'?", "Do you remember '{tag}'?" } }
    };
    
    [Header("Generic Responses")]
    [Tooltip("Generic responses when suspect doesn't know about a tag")]
    public TagResponse[] genericResponses = new TagResponse[]
    {
        new TagResponse { responseSequence = new string[] { "I don't know anything about that." } },
        new TagResponse { responseSequence = new string[] { "I already told you, I don't know." } },
        new TagResponse { responseSequence = new string[] { "Why do you keep asking me about things I don't know?" } }
    };
    
    // Runtime conversation state (not saved in ScriptableObject)
    [System.NonSerialized]
    private Dictionary<string, int> runtimeTimesAsked = new Dictionary<string, int>();
    [System.NonSerialized]
    private int genericQuestionCount = 0;
    
    // Stress system
    [System.NonSerialized]
    private float runtimeStress = 0f;
    [System.NonSerialized]
    private HashSet<string> runtimeContradictionsUsed = new HashSet<string>();
    public float CurrentStress => runtimeStress;

    // Stress zone thresholds
    private const float ZONE_LAWYERED_UP_MAX = 0.2f;
    private const float ZONE_DEFLECTING_MAX = 0.4f;
    private const float ZONE_SWEET_SPOT_MAX = 0.7f;
    private const float ZONE_RATTLED_MAX = 0.8f;

    // Tone deltas
    private const float TONE_CALM_DELTA = -0.08f;
    private const float TONE_NEUTRAL_DELTA = 0.02f;
    private const float TONE_FIRM_DELTA = 0.10f;

    // Zone-specific fallback responses (populated from JSON)
    [System.NonSerialized]
    public string[] lawyeredUpResponses = new string[0];
    [System.NonSerialized]
    public string[] rattledResponses = new string[0];
    [System.NonSerialized]
    public string[] shutdownResponses = new string[0];

    // Tone runtime state
    [System.NonSerialized]
    private InterrogationTone runtimeTone = InterrogationTone.Neutral;
    [System.NonSerialized]
    private int lawyeredUpIndex = 0;
    [System.NonSerialized]
    private int rattledFallbackIndex = 0;
    [System.NonSerialized]
    private int shutdownIndex = 0;

    // Discrepancy / truth-unlock runtime state
    [System.NonSerialized]
    private HashSet<string> runtimeTruthUnlocked = new HashSet<string>();
    [System.NonSerialized]
    private HashSet<string> runtimeDeniedTags = new HashSet<string>();
    [System.NonSerialized]
    private HashSet<string> runtimeUsedUnlockedInitialFor = new HashSet<string>();
    [System.NonSerialized]
    private Dictionary<string, int> runtimeTimesAskedSinceUnlock = new Dictionary<string, int>();
    
    // Global call counter for debugging
    
    // Helper properties
    public string FullName => $"{firstName} {lastName}";
    public bool HasCriminalRecord => criminalHistory.Count > 0;
    
    // ── Stress Zone API ──────────────────────────────────────────

    public StressZone GetCurrentStressZone()
    {
        if (runtimeStress < ZONE_LAWYERED_UP_MAX) return StressZone.LawyeredUp;
        if (runtimeStress < ZONE_DEFLECTING_MAX) return StressZone.Deflecting;
        if (runtimeStress < ZONE_SWEET_SPOT_MAX) return StressZone.SweetSpot;
        if (runtimeStress < ZONE_RATTLED_MAX) return StressZone.Rattled;
        return StressZone.Shutdown;
    }

    public void SetInterrogationTone(InterrogationTone tone)
    {
        runtimeTone = tone;
        Debug.Log($"[Citizen] {FullName} - Tone set to {tone}");
    }

    public InterrogationTone GetInterrogationTone() => runtimeTone;

    private void ApplyToneDelta()
    {
        float delta;
        switch (runtimeTone)
        {
            case InterrogationTone.Calm: delta = TONE_CALM_DELTA; break;
            case InterrogationTone.Firm: delta = TONE_FIRM_DELTA; break;
            default: delta = TONE_NEUTRAL_DELTA; break;
        }
        float oldStress = runtimeStress;
        runtimeStress = Mathf.Clamp01(runtimeStress + delta);
        Debug.Log($"[Citizen] {FullName} - Tone {runtimeTone}: stress {oldStress:F2} → {runtimeStress:F2} ({delta:+0.00;-0.00})");
    }

    private TagResponse GetShutdownResponse()
    {
        if (shutdownResponses == null || shutdownResponses.Length == 0)
            return new TagResponse { responseSequence = new string[] { "..." }, isLie = false };
        string line = shutdownResponses[shutdownIndex % shutdownResponses.Length];
        shutdownIndex++;
        Debug.Log($"[Citizen] {FullName} - SHUTDOWN zone response: '{line}'");
        return new TagResponse { responseSequence = new string[] { line }, isLie = false };
    }

    private TagResponse GetLawyeredUpResponse()
    {
        if (lawyeredUpResponses == null || lawyeredUpResponses.Length == 0)
            return new TagResponse { responseSequence = new string[] { "I want to speak to a lawyer." }, isLie = false };
        string line = lawyeredUpResponses[lawyeredUpIndex % lawyeredUpResponses.Length];
        lawyeredUpIndex++;
        Debug.Log($"[Citizen] {FullName} - LAWYERED UP zone response: '{line}'");
        return new TagResponse { responseSequence = new string[] { line }, isLie = false };
    }

    private TagResponse GetRattledFallbackResponse()
    {
        if (rattledResponses == null || rattledResponses.Length == 0)
            return new TagResponse { responseSequence = new string[] { "I... I can't think straight." }, isLie = false };
        string line = rattledResponses[rattledFallbackIndex % rattledResponses.Length];
        rattledFallbackIndex++;
        Debug.Log($"[Citizen] {FullName} - RATTLED fallback response: '{line}'");
        return new TagResponse { responseSequence = new string[] { line }, isLie = false };
    }

    /// <summary>
    /// Get response for a specific tag using the new TagInteraction system
    /// </summary>
    public TagResponse GetResponseForTag(string tagId)
    {
        Debug.Log($"[Citizen] {FullName} - GetResponseForTag: '{tagId}'");

        // Apply tone stress delta before anything else
        ApplyToneDelta();

        // Determine current stress zone
        StressZone zone = GetCurrentStressZone();
        Debug.Log($"[Citizen] {FullName} - Stress: {runtimeStress:F2}, Zone: {zone}");

        // Shutdown: refuse to speak entirely
        if (zone == StressZone.Shutdown)
            return GetShutdownResponse();

        // Lawyered Up: demands lawyer, won't answer
        if (zone == StressZone.LawyeredUp)
            return GetLawyeredUpResponse();

        // Find tag interaction
        TagInteraction tagInteraction = GetTagInteraction(tagId);

        if (tagInteraction != null)
        {
            Debug.Log($"[Citizen] {FullName} - Found specific interaction for tag: '{tagId}'");

            // Check if this tag is evidence that contradicts another interaction's lies
            // Contradictions always fire regardless of stress zone (player earned the evidence)
            TagResponse contradictionResult = CheckIfEvidenceContradicts(tagId);
            if (contradictionResult != null)
            {
                Debug.Log($"[Citizen] {FullName} - Evidence contradiction triggered for tag: '{tagId}'");
                int timesAskedContra = GetRuntimeTimesAsked(tagId);
                SetRuntimeTimesAsked(tagId, timesAskedContra + 1);
                ApplyStressImpact(contradictionResult);
                return ApplyNervousnessModifications(contradictionResult, timesAskedContra + 1);
            }

            // Check truth unlock state for this tag
            bool truthUnlockedForThisTag = runtimeTruthUnlocked.Contains(tagId);
            
            TagResponse response;
            
            if (truthUnlockedForThisTag)
            {
                // If truth has been unlocked for this tag, choose unlocked initial or follow-ups
                bool usedInitial = runtimeUsedUnlockedInitialFor.Contains(tagId);
                if (!usedInitial)
                {
                    // First truthful reveal depending on whether they previously denied
                    bool previouslyDenied = runtimeDeniedTags.Contains(tagId);
                    if (previouslyDenied && tagInteraction.unlockedInitialResponseIfPreviouslyDenied != null &&
                        tagInteraction.unlockedInitialResponseIfPreviouslyDenied.responseSequence != null &&
                        tagInteraction.unlockedInitialResponseIfPreviouslyDenied.responseSequence.Length > 0)
                    {
                        response = tagInteraction.unlockedInitialResponseIfPreviouslyDenied;
                    }
                    else if (tagInteraction.unlockedInitialResponseIfNotDenied != null &&
                             tagInteraction.unlockedInitialResponseIfNotDenied.responseSequence != null &&
                             tagInteraction.unlockedInitialResponseIfNotDenied.responseSequence.Length > 0)
                    {
                        response = tagInteraction.unlockedInitialResponseIfNotDenied;
                    }
                    else
                    {
                        // Fallback to first follow-up if provided, else default specific response (forced truthful)
                        if (tagInteraction.unlockedFollowupResponses != null && tagInteraction.unlockedFollowupResponses.Length > 0)
                        {
                            response = tagInteraction.unlockedFollowupResponses[0];
                        }
                        else
                        {
                            int currentTimesAsked = GetRuntimeTimesAsked(tagId);
                            response = GetResponseForTimesAsked(tagInteraction, currentTimesAsked);
                        }
                    }
                    runtimeUsedUnlockedInitialFor.Add(tagId);
                    // Reset follow-up counter
                    runtimeTimesAskedSinceUnlock[tagId] = 0;
                }
                else
                {
                    // Subsequent truthful follow-ups
                    int followupIndex = 0;
                    if (!runtimeTimesAskedSinceUnlock.TryGetValue(tagId, out followupIndex))
                    {
                        followupIndex = 0;
                    }
                    if (tagInteraction.unlockedFollowupResponses != null && tagInteraction.unlockedFollowupResponses.Length > 0)
                    {
                        int idx = Mathf.Min(followupIndex, tagInteraction.unlockedFollowupResponses.Length - 1);
                        response = tagInteraction.unlockedFollowupResponses[idx];
                    }
                    else
                    {
                        // Fallback to specific response progression
                        int currentTimesAsked = GetRuntimeTimesAsked(tagId);
                        response = GetResponseForTimesAsked(tagInteraction, currentTimesAsked);
                    }
                    runtimeTimesAskedSinceUnlock[tagId] = followupIndex + 1;
                }
                
                // Force truthful flag on unlocked responses
                if (response != null)
                {
                    response.isLie = false;
                }
                
                // Maintain base times asked counter for compatibility
                int times = GetRuntimeTimesAsked(tagId);
                SetRuntimeTimesAsked(tagId, times + 1);

                // Apply stress
                ApplyStressImpact(response);
                ApplyRepeatedQuestionStress(times + 1);

                // Apply nervousness modifications
                response = ApplyNervousnessModifications(response, GetRuntimeTimesAsked(tagId));

                // Asking this tag might also unlock truth for others (self-unlock allowed)
                ApplyTruthUnlocks(tagInteraction);

                return response;
            }
            else
            {
                // Truth not unlocked yet; check for response variants first
                int currentTimesAsked = GetRuntimeTimesAsked(tagId);
                Debug.Log($"[Citizen] {FullName} - Times asked for '{tagId}': {currentTimesAsked}");

                TagResponse baseResponse;

                // Try variant selection if variants are authored
                bool usedVariant = false;
                var eligibleVariants = FilterEligibleVariants(tagInteraction.responseVariants);
                if (eligibleVariants.Count > 0)
                {
                    var selectedVariant = WeightedRandomSelect(eligibleVariants);
                    Debug.Log($"[Citizen] {FullName} - Using variant '{selectedVariant.variantId}' for tag '{tagId}'");
                    baseResponse = FindFirstEligibleResponse(selectedVariant.responses, currentTimesAsked);
                    usedVariant = true;
                }
                else
                {
                    baseResponse = GetResponseForTimesAsked(tagInteraction, currentTimesAsked);
                }

                // Rattled zone: if no per-tag variant was selected, use citizen-level rattled fallback
                if (zone == StressZone.Rattled && !usedVariant
                    && rattledResponses != null && rattledResponses.Length > 0)
                {
                    baseResponse = GetRattledFallbackResponse();
                }

                Debug.Log($"[Citizen] {FullName} - Using response: '{baseResponse.GetCombinedResponse()}'");
                
                // Track denial if the chosen response is a lie
                if (baseResponse != null && baseResponse.isLie)
                {
                    runtimeDeniedTags.Add(tagId);
                }
                
                // Increment for next time
                SetRuntimeTimesAsked(tagId, currentTimesAsked + 1);

                // Apply stress
                ApplyStressImpact(baseResponse);
                ApplyRepeatedQuestionStress(currentTimesAsked + 1);

                // Apply nervousness modifications
                TagResponse responseMod = ApplyNervousnessModifications(baseResponse, currentTimesAsked + 1);

                // Asking this tag might also unlock truth for others
                ApplyTruthUnlocks(tagInteraction);

                return responseMod;
            }
        }
        else
        {
            // Even without a specific interaction, this tag might be evidence contradicting another tag's lies
            TagResponse contradictionResult = CheckIfEvidenceContradicts(tagId);
            if (contradictionResult != null)
            {
                Debug.Log($"[Citizen] {FullName} - Evidence contradiction triggered for unknown tag: '{tagId}'");
                ApplyStressImpact(contradictionResult);
                return ApplyNervousnessModifications(contradictionResult, 1);
            }

            Debug.Log($"[Citizen] {FullName} - No specific interaction found for tag: '{tagId}', using generic response");
            // Suspect doesn't know about this tag - use generic response
            return GetGenericResponse();
        }
    }
    
    /// <summary>
    /// Get response for a specific number of times asked, respecting conditions
    /// </summary>
    private TagResponse GetResponseForTimesAsked(TagInteraction interaction, int timesAsked)
    {
        if (interaction.responses.Length == 0) return new TagResponse();

        return FindFirstEligibleResponse(interaction.responses, timesAsked);
    }
    
    /// <summary>
    /// Get runtime times asked for a tag
    /// </summary>
    private int GetRuntimeTimesAsked(string tagId)
    {
        if (runtimeTimesAsked == null)
            runtimeTimesAsked = new Dictionary<string, int>();
            
        int count = runtimeTimesAsked.ContainsKey(tagId) ? runtimeTimesAsked[tagId] : 0;
        Debug.Log($"[Citizen] {FullName} - GetRuntimeTimesAsked for '{tagId}': {count} (dictionary has {runtimeTimesAsked.Count} entries)");
        return count;
    }
    
    /// <summary>
    /// Set runtime times asked for a tag
    /// </summary>
    private void SetRuntimeTimesAsked(string tagId, int count)
    {
        if (runtimeTimesAsked == null)
            runtimeTimesAsked = new Dictionary<string, int>();
        
        Debug.Log($"[Citizen] {FullName} - Setting times asked for '{tagId}' to {count}");
        runtimeTimesAsked[tagId] = count;
    }
    
    /// <summary>
    /// Get a tag interaction by ID, or null if not found
    /// </summary>
    public TagInteraction GetTagInteraction(string tagId)
    {
        foreach (var interaction in tagInteractions)
        {
            if (interaction.tagId.Equals(tagId, System.StringComparison.OrdinalIgnoreCase))
            {
                return interaction;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if suspect has specific knowledge about a tag
    /// </summary>
    public bool HasKnowledgeAbout(string tagId)
    {
        return GetTagInteraction(tagId) != null;
    }
    
    /// <summary>
    /// Get the question text for a specific tag (for dynamic prompt updates).
    /// Uses tagId for interaction lookup but displayName for generic question templates.
    /// </summary>
    public string GetQuestionForTag(string tagId, string tagType, string displayName = null)
    {
        TagInteraction interaction = GetTagInteraction(tagId);

        if (interaction != null)
        {
            // Use specific question
            return interaction.GetFormattedQuestion();
        }
        else
        {
            // Use generic question — show the human-readable display name, not the raw ID
            return GetGenericQuestion(displayName ?? tagId, tagType);
        }
    }

    /// <summary>
    /// Get a generic question for a tag type
    /// </summary>
    public string GetGenericQuestion(string displayName, string tagType)
    {
        foreach (var genericQuestion in genericQuestions)
        {
            if (genericQuestion.tagType.Equals(tagType, System.StringComparison.OrdinalIgnoreCase))
            {
                return genericQuestion.GetRandomQuestion(displayName);
            }
        }

        // Fallback if tag type not found
        return $"Tell me about '{displayName}'";
    }
    
    /// <summary>
    /// Get a generic response when suspect doesn't know about the tag
    /// </summary>
    private TagResponse GetGenericResponse()
    {
        // Increment generic question count
        genericQuestionCount++;
        
        // Track how many times generic questions have been asked
        int genericIndex = Mathf.Min(genericQuestionCount - 1, genericResponses.Length - 1);
        TagResponse response = genericResponses[genericIndex];
        
        // Apply nervousness modifications
        return ApplyNervousnessModifications(response, genericQuestionCount);
    }
    
    /// <summary>
    /// Apply nervousness modifications to responses
    /// </summary>
    private TagResponse ApplyNervousnessModifications(TagResponse baseResponse, int timesAsked)
    {
        TagResponse modifiedResponse = new TagResponse
        {
            responseSequence = baseResponse.responseSequence,
            isLie = baseResponse.isLie,
            responseDelayOverride = baseResponse.responseDelayOverride,
            clickableClues = baseResponse.clickableClues
        };
        
        // Nervousness can still affect delay timing
        if (timesAsked > 3)
        {
            // Add extra delay when getting agitated with repeated questions
            if (nervousnessLevel > 0.7f)
            {
                modifiedResponse.responseDelayOverride = Mathf.Max(modifiedResponse.responseDelayOverride, 2.5f);
            }
            else
            {
                modifiedResponse.responseDelayOverride = Mathf.Max(modifiedResponse.responseDelayOverride, 1.8f);
            }
        }
        
        return modifiedResponse;
    }
    
    // ── Response Variants ─────────────────────────────────────

    /// <summary>
    /// Filter variant groups whose conditions are all met
    /// </summary>
    private List<ResponseVariantGroup> FilterEligibleVariants(ResponseVariantGroup[] variants)
    {
        var eligible = new List<ResponseVariantGroup>();
        if (variants == null) return eligible;

        for (int i = 0; i < variants.Length; i++)
        {
            if (AllConditionsMet(variants[i].conditions))
                eligible.Add(variants[i]);
        }
        return eligible;
    }

    /// <summary>
    /// Weighted random selection from eligible variant groups
    /// </summary>
    private ResponseVariantGroup WeightedRandomSelect(List<ResponseVariantGroup> eligible)
    {
        if (eligible == null || eligible.Count == 0) return null;
        if (eligible.Count == 1) return eligible[0];

        float totalWeight = 0f;
        for (int i = 0; i < eligible.Count; i++)
            totalWeight += Mathf.Max(eligible[i].weight, 0.01f);

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < eligible.Count; i++)
        {
            cumulative += Mathf.Max(eligible[i].weight, 0.01f);
            if (roll <= cumulative) return eligible[i];
        }

        return eligible[eligible.Count - 1];
    }

    // ── Evidence Contradiction ─────────────────────────────────

    /// <summary>
    /// Check if a presented tag is evidence that contradicts any of this citizen's lies.
    /// Returns a contradiction response if found, null otherwise.
    /// </summary>
    private TagResponse CheckIfEvidenceContradicts(string tagId)
    {
        if (tagInteractions == null) return null;
        if (runtimeContradictionsUsed == null) runtimeContradictionsUsed = new HashSet<string>();

        for (int i = 0; i < tagInteractions.Length; i++)
        {
            var interaction = tagInteractions[i];
            if (interaction.contradictedByEvidenceTagIds == null || interaction.contradictedByEvidenceTagIds.Length == 0)
                continue;

            for (int j = 0; j < interaction.contradictedByEvidenceTagIds.Length; j++)
            {
                if (string.Equals(interaction.contradictedByEvidenceTagIds[j], tagId, System.StringComparison.OrdinalIgnoreCase))
                {
                    // Check the interaction has lies and hasn't been contradicted yet
                    string contradictionKey = $"{interaction.tagId}:{tagId}";
                    if (runtimeContradictionsUsed.Contains(contradictionKey))
                        continue;

                    bool hasLies = false;
                    if (interaction.responses != null)
                    {
                        for (int r = 0; r < interaction.responses.Length; r++)
                        {
                            if (interaction.responses[r].isLie) { hasLies = true; break; }
                        }
                    }
                    if (!hasLies) continue;

                    return HandleEvidenceContradiction(interaction, tagId, contradictionKey);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Handle a confirmed evidence contradiction: mark used, unlock truth, bump stress, return response
    /// </summary>
    private TagResponse HandleEvidenceContradiction(TagInteraction interaction, string evidenceTagId, string contradictionKey)
    {
        runtimeContradictionsUsed.Add(contradictionKey);

        // Unlock truth for the contradicted tag
        if (!runtimeTruthUnlocked.Contains(interaction.tagId))
        {
            runtimeTruthUnlocked.Add(interaction.tagId);
            Debug.Log($"[Citizen] {FullName} - Evidence '{evidenceTagId}' contradicted '{interaction.tagId}' — truth unlocked!");
        }

        // Mark as previously denied (they were lying)
        runtimeDeniedTags.Add(interaction.tagId);

        // Stress bump for being caught
        float oldStress = runtimeStress;
        runtimeStress = Mathf.Clamp01(runtimeStress + 0.3f);
        Debug.Log($"[Citizen] {FullName} - Contradiction stress {oldStress:F2} → {runtimeStress:F2} (+0.30)");

        // Return contradictionResponse if authored, else fall through to unlockedInitialResponseIfPreviouslyDenied
        if (interaction.contradictionResponse != null &&
            interaction.contradictionResponse.responseSequence != null &&
            interaction.contradictionResponse.responseSequence.Length > 0)
        {
            return interaction.contradictionResponse;
        }

        // Fallback to the unlocked initial response for previously denied
        if (interaction.unlockedInitialResponseIfPreviouslyDenied != null &&
            interaction.unlockedInitialResponseIfPreviouslyDenied.responseSequence != null &&
            interaction.unlockedInitialResponseIfPreviouslyDenied.responseSequence.Length > 0)
        {
            return interaction.unlockedInitialResponseIfPreviouslyDenied;
        }

        // Ultimate fallback
        return new TagResponse
        {
            responseSequence = new string[] { "..." },
            isLie = false
        };
    }

    // ── Condition Evaluation ────────────────────────────────────

    /// <summary>
    /// Evaluate a single response condition against current runtime state
    /// </summary>
    private bool EvaluateCondition(ResponseCondition condition)
    {
        if (condition == null) return true;

        switch (condition.type)
        {
            case ResponseCondition.ConditionType.TagAsked:
                return GetRuntimeTimesAsked(condition.targetId) >= condition.minCount;

            case ResponseCondition.ConditionType.TagNotAsked:
                return GetRuntimeTimesAsked(condition.targetId) == 0;

            case ResponseCondition.ConditionType.ClueDiscovered:
                return CluesManager.Instance != null && CluesManager.Instance.IsClueFound(condition.targetId);

            case ResponseCondition.ConditionType.ClueNotDiscovered:
                return CluesManager.Instance == null || !CluesManager.Instance.IsClueFound(condition.targetId);

            case ResponseCondition.ConditionType.TruthUnlocked:
                return runtimeTruthUnlocked != null && runtimeTruthUnlocked.Contains(condition.targetId);

            case ResponseCondition.ConditionType.StressAbove:
                return runtimeStress >= condition.threshold;

            case ResponseCondition.ConditionType.StressBelow:
                return runtimeStress < condition.threshold;

            default:
                return true;
        }
    }

    /// <summary>
    /// Check if all conditions in an array are met (null/empty = always met)
    /// </summary>
    private bool AllConditionsMet(ResponseCondition[] conditions)
    {
        if (conditions == null || conditions.Length == 0) return true;
        for (int i = 0; i < conditions.Length; i++)
        {
            if (!EvaluateCondition(conditions[i])) return false;
        }
        return true;
    }

    /// <summary>
    /// Find the first eligible response starting from timesAsked index, checking conditions.
    /// Falls back to last response if no conditions match.
    /// </summary>
    private TagResponse FindFirstEligibleResponse(TagResponse[] responses, int timesAsked)
    {
        if (responses == null || responses.Length == 0) return new TagResponse();

        int startIndex = Mathf.Min(timesAsked, responses.Length - 1);

        // Try from startIndex forward to find one whose conditions are met
        for (int i = startIndex; i < responses.Length; i++)
        {
            if (AllConditionsMet(responses[i].conditions))
                return responses[i];
        }

        // Try from startIndex backward
        for (int i = startIndex - 1; i >= 0; i--)
        {
            if (AllConditionsMet(responses[i].conditions))
                return responses[i];
        }

        // Ultimate fallback: last response regardless of conditions
        return responses[responses.Length - 1];
    }

    /// <summary>
    /// Apply stress impact from a delivered response
    /// </summary>
    private void ApplyStressImpact(TagResponse response)
    {
        if (response == null) return;
        float oldStress = runtimeStress;
        runtimeStress = Mathf.Clamp01(runtimeStress + response.stressImpact);
        if (Mathf.Abs(response.stressImpact) > 0.001f)
            Debug.Log($"[Citizen] {FullName} - Stress {oldStress:F2} → {runtimeStress:F2} (impact: {response.stressImpact:+0.00;-0.00})");
    }

    /// <summary>
    /// Apply stress from repeated questioning (>2 asks for the same tag)
    /// </summary>
    private void ApplyRepeatedQuestionStress(int timesAsked)
    {
        if (timesAsked > 2)
        {
            float oldStress = runtimeStress;
            runtimeStress = Mathf.Clamp01(runtimeStress + 0.05f);
            Debug.Log($"[Citizen] {FullName} - Repeated question stress {oldStress:F2} → {runtimeStress:F2} (+0.05)");
        }
    }

    /// <summary>
    /// Reset conversation state (for new interrogation sessions)
    /// </summary>
    public void ResetConversationState()
    {
        // Reset runtime state (don't modify ScriptableObject)
        if (runtimeTimesAsked == null)
            runtimeTimesAsked = new Dictionary<string, int>();
        else
            runtimeTimesAsked.Clear();
            
        genericQuestionCount = 0;

        // Reset stress — use explicit initialStress if set, otherwise derive from nervousness
        runtimeStress = initialStress >= 0f
            ? Mathf.Clamp01(initialStress)
            : Mathf.Lerp(0.25f, 0.45f, nervousnessLevel);
        if (runtimeContradictionsUsed == null) runtimeContradictionsUsed = new HashSet<string>(); else runtimeContradictionsUsed.Clear();

        // Reset tone and cycling counters
        runtimeTone = InterrogationTone.Neutral;
        lawyeredUpIndex = 0;
        rattledFallbackIndex = 0;
        shutdownIndex = 0;

        // Reset discrepancy state
        if (runtimeTruthUnlocked == null) runtimeTruthUnlocked = new HashSet<string>(); else runtimeTruthUnlocked.Clear();
        if (runtimeDeniedTags == null) runtimeDeniedTags = new HashSet<string>(); else runtimeDeniedTags.Clear();
        if (runtimeUsedUnlockedInitialFor == null) runtimeUsedUnlockedInitialFor = new HashSet<string>(); else runtimeUsedUnlockedInitialFor.Clear();
        if (runtimeTimesAskedSinceUnlock == null) runtimeTimesAskedSinceUnlock = new Dictionary<string, int>(); else runtimeTimesAskedSinceUnlock.Clear();
    }

    /// <summary>
    /// Apply truth unlocks defined on the interaction just asked
    /// </summary>
    private void ApplyTruthUnlocks(TagInteraction interaction)
    {
        if (interaction == null || interaction.unlocksTruthForTagIds == null) return;
        for (int i = 0; i < interaction.unlocksTruthForTagIds.Length; i++)
        {
            string targetTagId = interaction.unlocksTruthForTagIds[i];
            if (string.IsNullOrEmpty(targetTagId)) continue;
            if (!runtimeTruthUnlocked.Contains(targetTagId))
            {
                runtimeTruthUnlocked.Add(targetTagId);
                runtimeStress = Mathf.Clamp01(runtimeStress + 0.2f);
                Debug.Log($"[Citizen] {FullName} - Truth unlocked for tag '{targetTagId}' by asking '{interaction.tagId}' (stress +0.2 → {runtimeStress:F2})");
            }
        }
    }

    public Sprite GetPortraitSprite()
    {
        return picture;
    }

    public string GetFullName()
    {
        return FullName;
    }

    public string GetCitizenId()
    {
        return citizenID;
    }

    public void AddViolation(CriminalViolation violation)
    {
        if (violation != null)
        {
            Debug.Log($"Adding violation {violation.violationName} to citizen {GetFullName()}");
            var record = new CriminalRecord
            {
                offense = violation.violationName,
                description = violation.description,
                severity = violation.severity,
                date = System.DateTime.Now.ToString("MM/dd/yyyy")
            };
            criminalHistory.Add(record);
        }
    }
}

[System.Serializable]
public class CriminalRecord
{
    public string offense;
    public string date; // Format: MM/DD/YYYY
    public string description;
    public CrimeSeverity severity = CrimeSeverity.Minor;
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum Ethnicity
{
    Caucasian,
    AfricanAmerican,
    Hispanic,
    Asian,
    MiddleEastern,
    NativeAmerican,
    Mixed,
    Other
}

public enum MaritalStatus
{
    Single,
    Married,
    Divorced,
    Widowed,
    Separated
}

public enum CrimeSeverity
{
    Minor,      // Traffic violations, petty theft
    Moderate,   // Burglary, assault
    Major,      // Armed robbery, serious assault
    Severe      // Murder, kidnapping
} 