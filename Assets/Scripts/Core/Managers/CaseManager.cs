using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[System.Serializable]
public class CaseStatus
{
    public bool isActive;             // Whether case is currently being worked on
    public bool isSolved;             // Whether case has been fully solved
    public int currentStepIndex;      // Current step in the investigation
    public HashSet<string> completedStepIds = new HashSet<string>();  // Steps that have been completed
    public HashSet<string> discoveredClueIds = new HashSet<string>();  // All discovered clues
    public HashSet<string> discoveredEvidenceIds = new HashSet<string>();  // All discovered evidence
    public List<string> interrogatedSuspectIds = new List<string>();   // Suspects who've been questioned

    public CaseStatus()
    {
        Reset();
    }

    public void Reset()
    {
        isActive = false;
        isSolved = false;
        currentStepIndex = 0;
        completedStepIds.Clear();
        discoveredClueIds.Clear();
        discoveredEvidenceIds.Clear();
        interrogatedSuspectIds.Clear();
    }
}

/// <summary>
/// Manages the pool of all cases in the game. Handles case progression and status tracking.
/// </summary>
public class CaseManager : SingletonMonoBehaviour<CaseManager>
{

    [Header("All Case Data")]
    public List<Case> allCases = new List<Case>();

    // Runtime state
    private Dictionary<string, CaseStatus> caseStatuses = new Dictionary<string, CaseStatus>();

    protected override void OnSingletonAwake()
    {
        InitializeCaseStatuses();
        ResetAllCaseDrafts();
    }

    private void ResetAllCaseDrafts()
    {
        foreach (var c in allCases)
        {
            if (c != null)
            {
                c.ResetRuntimeState();
            }
        }
    }

    private void InitializeCaseStatuses()
    {
        caseStatuses.Clear();
        foreach (var c in allCases)
        {
            caseStatuses[c.caseID] = new CaseStatus();
        }
    }

    /// <summary>
    /// Gets available cases for the current day, including carried over unsolved cases
    /// and new cases that meet their requirements.
    /// </summary>
    public List<Case> GetAvailableCasesForDay(int currentDay, int maxCases, int minSecondaryCases)
    {
        var availableCases = new List<Case>();

        // First, add all unsolved active cases (they carry over)
        availableCases.AddRange(
            allCases.Where(c => GetCaseStatus(c.caseID).isActive && !GetCaseStatus(c.caseID).isSolved)
        );

        // Then, find new eligible core cases
        var eligibleCoreCases = allCases.Where(c =>
            c.caseType == CaseType.Core &&
            !GetCaseStatus(c.caseID).isActive &&
            !GetCaseStatus(c.caseID).isSolved &&
            c.firstAvailableDay <= currentDay &&
            ArePrerequisitesClosed(c)
        ).OrderBy(c => c.coreSequenceNumber);

        availableCases.AddRange(eligibleCoreCases);

        // Finally, fill remaining slots with secondary cases
        int remainingSlots = maxCases - availableCases.Count;
        int secondaryCasesNeeded = Mathf.Max(minSecondaryCases, remainingSlots);

        if (secondaryCasesNeeded > 0)
        {
            var secondaryCases = allCases.Where(c =>
                c.caseType == CaseType.Secondary &&
                !GetCaseStatus(c.caseID).isActive &&
                !GetCaseStatus(c.caseID).isSolved &&
                c.firstAvailableDay <= currentDay
            ).Take(secondaryCasesNeeded);

            availableCases.AddRange(secondaryCases);
        }

        return availableCases;
    }

    private bool ArePrerequisitesClosed(Case caseData)
    {
        if (caseData.requiredPreviousCaseIds == null || 
            caseData.requiredPreviousCaseIds.Count == 0)
            return true;

        foreach (var requiredId in caseData.requiredPreviousCaseIds)
        {
            if (!GetCaseStatus(requiredId).isSolved)
                return false;
        }
        return true;
    }

    public void MarkCaseActive(string caseId)
    {
        var status = GetCaseStatus(caseId);
        if (status != null)
        {
            status.isActive = true;
        }
    }

    public void MarkCaseSolved(string caseId)
    {
        var status = GetCaseStatus(caseId);
        if (status != null)
        {
            status.isSolved = true;
            status.isActive = false;

            // Unlock next cases if this was a core case
            var caseData = GetCaseByID(caseId);
            if (caseData != null && 
                caseData.caseType == CaseType.Core && 
                caseData.unlocksNextCaseIds != null)
            {
                foreach (var nextCaseId in caseData.unlocksNextCaseIds)
                {
                    var nextCase = GetCaseByID(nextCaseId);
                    if (nextCase != null)
                    {
                        Debug.Log($"Case {caseId} unlocked case {nextCaseId}");
                    }
                }
            }
        }
    }

    public void UpdateCaseProgress(string caseId, string discoveredClueId = null, 
        string discoveredEvidenceId = null, string interrogatedSuspectId = null,
        string completedStepId = null)
    {
        var status = GetCaseStatus(caseId);
        var caseData = GetCaseByID(caseId);
        if (status == null || caseData == null) return;

        if (!string.IsNullOrEmpty(discoveredClueId))
            status.discoveredClueIds.Add(discoveredClueId);

        if (!string.IsNullOrEmpty(discoveredEvidenceId))
            status.discoveredEvidenceIds.Add(discoveredEvidenceId);

        if (!string.IsNullOrEmpty(interrogatedSuspectId) && 
            !status.interrogatedSuspectIds.Contains(interrogatedSuspectId))
            status.interrogatedSuspectIds.Add(interrogatedSuspectId);

        if (!string.IsNullOrEmpty(completedStepId))
        {
            status.completedStepIds.Add(completedStepId);
            
            // Update current step index
            if (caseData.steps != null)
            {
                var nextStep = caseData.steps.FirstOrDefault(s => 
                    !status.completedStepIds.Contains(s.stepId));
                
                if (nextStep != null)
                    status.currentStepIndex = nextStep.stepNumber - 1;
                else
                    status.currentStepIndex = caseData.steps.Count; // All steps completed
            }
        }
    }

    public bool CanCompleteStep(string caseId, string stepId)
    {
        var status = GetCaseStatus(caseId);
        var caseData = GetCaseByID(caseId);
        if (status == null || caseData == null) return false;

        var step = caseData.steps?.FirstOrDefault(s => s.stepId == stepId);
        if (step == null) return false;

        if (step.requiredClueIds == null || step.requiredClueIds.Count == 0)
            return true;

        foreach (var clueId in step.requiredClueIds)
        {
            if (!status.discoveredClueIds.Contains(clueId))
                return false;
        }
        return true;
    }

    public float GetCaseCompletion(string caseId)
    {
        var status = GetCaseStatus(caseId);
        var caseData = GetCaseByID(caseId);
        if (status == null || caseData == null) return 0f;

        if (status.isSolved) return 100f;
        if (!status.isActive) return 0f;

        float totalPoints = 0f;
        float earnedPoints = 0f;

        // Weight from steps
        if (caseData.steps != null && caseData.steps.Count > 0)
        {
            float pointsPerStep = 40f / caseData.steps.Count;
            totalPoints += 40f;
            earnedPoints += status.completedStepIds.Count * pointsPerStep;
        }

        // Weight from evidence
        if (caseData.evidences != null && caseData.evidences.Count > 0)
        {
            float pointsPerEvidence = 30f / caseData.evidences.Count;
            totalPoints += 30f;
            earnedPoints += status.discoveredEvidenceIds.Count * pointsPerEvidence;
        }

        // Weight from interrogations
        if (caseData.suspects != null && caseData.suspects.Count > 0)
        {
            float pointsPerSuspect = 30f / caseData.suspects.Count;
            totalPoints += 30f;
            earnedPoints += status.interrogatedSuspectIds.Count * pointsPerSuspect;
        }

        return totalPoints > 0 ? (earnedPoints / totalPoints) * 100f : 0f;
    }

    public CaseStatus GetCaseStatus(string caseId)
    {
        if (string.IsNullOrEmpty(caseId)) return null;
        return caseStatuses.TryGetValue(caseId, out var status) ? status : null;
    }

    /// <summary>
    /// Gets all core story cases in sequence order.
    /// </summary>
    public List<Case> GetCoreCasesInOrder()
    {
        return allCases
            .Where(c => c.caseType == CaseType.Core)
            .OrderBy(c => c.coreSequenceNumber)
            .ToList();
    }

    /// <summary>
    /// Gets all secondary/filler cases.
    /// </summary>
    public List<Case> GetSecondaryCases()
    {
        return allCases.FindAll(c => c.caseType == CaseType.Secondary);
    }

    /// <summary>
    /// Finds a specific case by ID.
    /// </summary>
    public Case GetCaseByID(string caseID)
    {
        return allCases.Find(c => c.caseID == caseID);
    }

    /// <summary>
    /// Resets all case statuses. Call this when starting a new game.
    /// </summary>
    public void ResetAllCases()
    {
        foreach (var status in caseStatuses.Values)
        {
            status.Reset();
        }
    }

    /// <summary>
    /// Gets all suspects for a specific case
    /// </summary>
    public List<Citizen> GetCaseSuspects(string caseId)
    {
        var caseData = GetCaseByID(caseId);
        return caseData?.suspects ?? new List<Citizen>();
    }

    /// <summary>
    /// Gets the culprit for a specific case
    /// </summary>
    public Citizen GetCaseCulprit(string caseId)
    {
        var caseData = GetCaseByID(caseId);
        return caseData?.culprit;
    }

    /// <summary>
    /// Checks if a suspect has been interrogated in a case
    /// </summary>
    public bool HasSuspectBeenInterrogated(string caseId, string citizenId)
    {
        var status = GetCaseStatus(caseId);
        return status?.interrogatedSuspectIds.Contains(citizenId) ?? false;
    }

    /// <summary>
    /// Marks a suspect as interrogated
    /// </summary>
    public void MarkSuspectInterrogated(string caseId, string citizenId)
    {
        UpdateCaseProgress(caseId, interrogatedSuspectId: citizenId);
    }

    /// <summary>
    /// Gets the percentage of suspects interrogated for a case
    /// </summary>
    public float GetInterrogationProgress(string caseId)
    {
        var status = GetCaseStatus(caseId);
        var caseData = GetCaseByID(caseId);
        
        if (status == null || caseData == null || caseData.suspects == null || caseData.suspects.Count == 0)
            return 0f;

        return (float)status.interrogatedSuspectIds.Count / caseData.suspects.Count * 100f;
    }

    /// <summary>
    /// Checks if all suspects have been interrogated
    /// </summary>
    public bool AreAllSuspectsInterrogated(string caseId)
    {
        var status = GetCaseStatus(caseId);
        var caseData = GetCaseByID(caseId);
        
        if (status == null || caseData == null || caseData.suspects == null)
            return false;

        // Check if all suspects have been interrogated
        foreach (var suspect in caseData.suspects)
        {
            if (suspect != null && !status.interrogatedSuspectIds.Contains(suspect.citizenID))
                return false;
        }
        
        return caseData.suspects.Count > 0; // Only return true if there are actually suspects
    }

    public void OnCaseSolved(Case solvedCase)
    {
        if (solvedCase == null) return;

        // Mark case as solved in status tracking
        MarkCaseSolved(solvedCase.caseID);

        // Emit event — subscribers (PlayerProgressManager, UIManager, etc.) handle their own logic
        GameEvents.RaiseCaseSolved(solvedCase);
    }
}
