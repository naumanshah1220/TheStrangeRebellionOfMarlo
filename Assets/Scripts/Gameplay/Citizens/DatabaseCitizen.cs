using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DatabaseCitizen
{
    [Header("Identification")]
    public string citizenID;
    public string firstName;
    public string lastName;
    public bool deceased = false;
    
    [Header("Personal Information")]
    public string dateOfBirth; // Format: MM/DD/YYYY
    public Gender gender = Gender.Male;
    public Ethnicity ethnicity = Ethnicity.Caucasian;
    public MaritalStatus maritalStatus = MaritalStatus.Single;
    public string address;
    public string occupation = "";
    
    [Header("Criminal Record")]
    public List<CriminalRecord> criminalHistory = new List<CriminalRecord>();
    
    [Header("Interrogation Data")]
    [Range(0f, 1f)]
    public float nervousnessLevel = 0.3f;
    
    // Runtime data - loaded from Resources
    [System.NonSerialized]
    public Sprite picture;
    [System.NonSerialized]
    public List<Sprite> fingerprints = new List<Sprite>();
    
    // Runtime conversation state
    [System.NonSerialized]
    private Dictionary<string, int> runtimeTimesAsked = new Dictionary<string, int>();
    [System.NonSerialized]
    private int genericQuestionCount = 0;
    
    // Helper properties
    public string FullName => $"{firstName} {lastName}";
    public bool HasCriminalRecord => criminalHistory.Count > 0;
    
    /// <summary>
    /// Convert DatabaseCitizen to a runtime Citizen for interrogation
    /// </summary>
    public Citizen ToRuntimeCitizen(GenericQuestion[] genericQuestions, TagResponse[] genericResponses)
    {
        // Create a new Citizen ScriptableObject instance (runtime only)
        Citizen citizen = ScriptableObject.CreateInstance<Citizen>();
        
        // Copy all data
        citizen.citizenID = this.citizenID;
        citizen.firstName = this.firstName;
        citizen.lastName = this.lastName;
        citizen.dateOfBirth = this.dateOfBirth;
        citizen.gender = this.gender;
        citizen.ethnicity = this.ethnicity;
        citizen.maritalStatus = this.maritalStatus;
        citizen.address = this.address;
        citizen.occupation = this.occupation;
        citizen.criminalHistory = new List<CriminalRecord>(this.criminalHistory);
        citizen.nervousnessLevel = this.nervousnessLevel;
        citizen.picture = this.picture;
        citizen.fingerprints = new List<Sprite>(this.fingerprints);
        
        // Use database-wide generic questions and responses
        citizen.genericQuestions = genericQuestions;
        citizen.genericResponses = genericResponses;
        
        // Empty tag interactions (database citizens don't have specific interactions)
        citizen.tagInteractions = new TagInteraction[0];
        
        return citizen;
    }
    
    /// <summary>
    /// Load portrait from Resources/Portraits folder
    /// </summary>
    public void LoadPortrait()
    {
        if (!string.IsNullOrEmpty(citizenID))
        {
            picture = Resources.Load<Sprite>($"Portraits/{citizenID}");
            if (picture == null)
            {
                Debug.LogWarning($"[DatabaseCitizen] Portrait not found for citizen {citizenID} at Resources/Portraits/{citizenID}");
            }
        }
    }
    
    /// <summary>
    /// Load fingerprints from Resources/Fingerprints folder
    /// </summary>
    public void LoadFingerprints()
    {
        fingerprints.Clear();
        
        if (!string.IsNullOrEmpty(citizenID))
        {
            // Try to load 5 fingerprints (numbered 1-5)
            for (int i = 1; i <= 5; i++)
            {
                Sprite fingerprint = Resources.Load<Sprite>($"Fingerprints/{citizenID}_finger_{i}");
                if (fingerprint != null)
                {
                    fingerprints.Add(fingerprint);
                }
            }
            
            if (fingerprints.Count == 0)
            {
                Debug.LogWarning($"[DatabaseCitizen] No fingerprints found for citizen {citizenID} at Resources/Fingerprints/{citizenID}_finger_[1-5]");
            }
        }
    }
    
    /// <summary>
    /// Reset conversation state
    /// </summary>
    public void ResetConversationState()
    {
        if (runtimeTimesAsked == null)
            runtimeTimesAsked = new Dictionary<string, int>();
        else
            runtimeTimesAsked.Clear();
            
        genericQuestionCount = 0;
    }
} 