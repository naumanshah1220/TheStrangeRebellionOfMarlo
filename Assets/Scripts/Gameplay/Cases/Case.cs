using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCase", menuName = "Cases/Case")]
public class Case : ScriptableObject, ICardData
{
    [Header("Identification")]
    public string caseID;
    public string title;
    public string description;
    public CaseType caseType = CaseType.Core;

    [Header("Progression")]
    public int firstAvailableDay;     // Day when this case first becomes available
    public int coreSequenceNumber;    // Order in the core case sequence (for Core cases only)
    public float reward = 50f;        // Base reward for solving the case

    [Header("Requirements")]
    public List<string> requiredPreviousCaseIds;  // Cases that must be solved before this one
    public List<string> unlocksNextCaseIds;       // Cases that this unlocks when solved

    [Header("Legal")]
    public string lawBroken;
    public float extraRewardForState; // Extra reward for helping state
    public float suspicionReduction;  // How much suspicion is reduced by helping state
    
    [Header("Resistance Plot")]
    public bool involvesResistance;  // Whether this case has resistance implications
    public string resistanceChoice;   // Description of how player can help resistance
    public string stateChoice;        // Description of how player can help the state

    [Header("Suspects")]
    public List<Citizen> suspects = new List<Citizen>(4); // Up to 4 suspects per case
    public Citizen culprit; // The actual guilty party (must be one of the suspects)
    
    [Header("Data")]
    public List<Evidence> evidences;
    public List<Evidence> extraEvidences;
    public List<CaseStep> steps;

    [Header("Verdict")]
    public VerdictSchema verdictSchema;
    public CaseSolution[] solutions; // allow variants
    public int minDiscoveredCluesToAllowCommit = 3;
    public bool allowCommitWithLowConfidence = true; // but penalize
    public List<ClueVerdictMapping> clueVerdictMappings = new List<ClueVerdictMapping>();

    [Header("Visuals")]
    public Sprite cardImage;
    public GameObject fullCardPrefab;

    [Header("State")]
    public bool isSolved = false;
    public Citizen convictedSuspect;
    public CriminalViolation conviction;

    // runtime
    [System.NonSerialized] public CaseVerdict draftVerdict = new CaseVerdict();

    public void ResetRuntimeState()
    {
        draftVerdict = new CaseVerdict();
    }

    // ICardData implementation
    public string GetCardID() => caseID;
    public string GetCardTitle() => title;
    public string GetCardDescription() => description;
    public Sprite GetCardImage() => cardImage;
    public GameObject GetFullCardPrefab() => fullCardPrefab;
    public CardMode GetCardMode() => CardMode.Case;

    public void MarkAsSolved(Citizen suspect, CriminalViolation crime)
    {
        isSolved = true;
        convictedSuspect = suspect;
        conviction = crime;
    }
}

public enum CaseType
{
    Core,
    Secondary
}



[System.Serializable]
public class CaseStep
{
    public string stepId;             // Unique identifier for this step
    public int stepNumber;
    public string description;
    public List<string> requiredClueIds;  // Clues needed to complete this step
    public List<string> unlockedClueIds;  // Clues unlocked by completing this step
}
