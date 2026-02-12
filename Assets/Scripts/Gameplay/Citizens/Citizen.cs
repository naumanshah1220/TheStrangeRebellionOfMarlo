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
    
    /// <summary>
    /// Get response for a specific tag using the new TagInteraction system
    /// </summary>
    public TagResponse GetResponseForTag(string tagId)
    {
        Debug.Log($"[Citizen] {FullName} - GetResponseForTag: '{tagId}'");

        // Find tag interaction
        TagInteraction tagInteraction = GetTagInteraction(tagId);
        
        if (tagInteraction != null)
        {
            Debug.Log($"[Citizen] {FullName} - Found specific interaction for tag: '{tagId}'");
            
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
                
                // Apply nervousness modifications
                response = ApplyNervousnessModifications(response, GetRuntimeTimesAsked(tagId));
                
                // Asking this tag might also unlock truth for others (self-unlock allowed)
                ApplyTruthUnlocks(tagInteraction);
                
                return response;
            }
            else
            {
                // Truth not unlocked yet; proceed with normal specific responses
                int currentTimesAsked = GetRuntimeTimesAsked(tagId);
                Debug.Log($"[Citizen] {FullName} - Times asked for '{tagId}': {currentTimesAsked}");
                
                TagResponse baseResponse = GetResponseForTimesAsked(tagInteraction, currentTimesAsked);
                Debug.Log($"[Citizen] {FullName} - Using specific response: '{baseResponse.GetCombinedResponse()}'");
                
                // Track denial if the chosen response is a lie
                if (baseResponse != null && baseResponse.isLie)
                {
                    runtimeDeniedTags.Add(tagId);
                }
                
                // Increment for next time
                SetRuntimeTimesAsked(tagId, currentTimesAsked + 1);
                
                // Apply nervousness modifications
                TagResponse responseMod = ApplyNervousnessModifications(baseResponse, currentTimesAsked + 1);
                
                // Asking this tag might also unlock truth for others
                ApplyTruthUnlocks(tagInteraction);
                
                return responseMod;
            }
        }
        else
        {
            Debug.Log($"[Citizen] {FullName} - No specific interaction found for tag: '{tagId}', using generic response");
            // Suspect doesn't know about this tag - use generic response
            return GetGenericResponse();
        }
    }
    
    /// <summary>
    /// Get response for a specific number of times asked
    /// </summary>
    private TagResponse GetResponseForTimesAsked(TagInteraction interaction, int timesAsked)
    {
        if (interaction.responses.Length == 0) return new TagResponse();
        
        // If asked more times than we have responses, use the last response
        int responseIndex = Mathf.Min(timesAsked, interaction.responses.Length - 1);
        return interaction.responses[responseIndex];
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
    /// Get the question text for a specific tag (for dynamic prompt updates)
    /// </summary>
    public string GetQuestionForTag(string tagId, string tagType)
    {
        TagInteraction interaction = GetTagInteraction(tagId);
        
        if (interaction != null)
        {
            // Use specific question
            return interaction.GetFormattedQuestion();
        }
        else
        {
            // Use generic question for this tag type
            return GetGenericQuestion(tagId, tagType);
        }
    }
    
    /// <summary>
    /// Get a generic question for a tag type
    /// </summary>
    public string GetGenericQuestion(string tagId, string tagType)
    {
        foreach (var genericQuestion in genericQuestions)
        {
            if (genericQuestion.tagType.Equals(tagType, System.StringComparison.OrdinalIgnoreCase))
            {
                return genericQuestion.GetRandomQuestion(tagId);
            }
        }
        
        // Fallback if tag type not found
        return $"Tell me about '{tagId}'";
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
                Debug.Log($"[Citizen] {FullName} - Truth unlocked for tag '{targetTagId}' by asking '{interaction.tagId}'");
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