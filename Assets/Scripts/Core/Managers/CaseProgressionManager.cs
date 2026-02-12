using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class CaseSlotIncrease
{
    public string caseId;
    public int slotIncrease = 1;
}

public class CaseProgressionManager : SingletonMonoBehaviour<CaseProgressionManager>
{
    [Header("Base Settings")]
    public int baseMaxSimultaneousCases = 3;

    [Header("Progression")]
    public List<CaseSlotIncrease> slotIncreases = new List<CaseSlotIncrease>();

    [Header("Events")]
    public UnityEvent<int> onMaxSimultaneousCasesChanged;

    private CaseManager caseManager;
    private int currentMaxSimultaneousCases;
    private HashSet<string> processedCases = new HashSet<string>();

    private void Start()
    {
        caseManager = FindFirstObjectByType<CaseManager>();
        if (!caseManager)
            Debug.LogError("CaseManager not found in CaseProgressionManager!");

        currentMaxSimultaneousCases = baseMaxSimultaneousCases;
        
        // Process any already completed cases (for save game loading)
        foreach (var increase in slotIncreases)
        {
            var status = caseManager.GetCaseStatus(increase.caseId);
            if (status != null && status.isSolved)
            {
                ProcessCaseCompletion(increase.caseId);
            }
        }
    }

    public void ProcessCaseCompletion(string caseId)
    {
        if (processedCases.Contains(caseId)) return;

        var increase = slotIncreases.Find(i => i.caseId == caseId);
        if (increase != null)
        {
            currentMaxSimultaneousCases += increase.slotIncrease;
            processedCases.Add(caseId);
            onMaxSimultaneousCasesChanged?.Invoke(currentMaxSimultaneousCases);
            Debug.Log($"Max simultaneous cases increased to {currentMaxSimultaneousCases} after completing case {caseId}");
        }
    }

    public int GetCurrentMaxSimultaneousCases() => currentMaxSimultaneousCases;
} 