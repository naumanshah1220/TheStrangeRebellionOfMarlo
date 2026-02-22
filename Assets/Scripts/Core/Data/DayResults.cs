using System.Collections.Generic;

[System.Serializable]
public class DayResults
{
    public int dayNumber;
    public List<CaseResult> caseResults = new List<CaseResult>();
    public bool hadOvertime;
    public float overtimeHours;
    public int unfinishedCoreCount;
    public float totalBonusEarned;         // Sum of all CaseResult.reward

    public void FinalizeResults()
    {
        totalBonusEarned = 0f;
        unfinishedCoreCount = 0;
        foreach (var r in caseResults)
            totalBonusEarned += r.reward;
    }

    /// <summary>
    /// Average confidence across all submitted cases (0-100).
    /// </summary>
    public float AverageConfidence()
    {
        if (caseResults.Count == 0) return 0f;
        float sum = 0f;
        foreach (var r in caseResults)
            sum += r.confidenceScore;
        return sum / caseResults.Count;
    }
}
