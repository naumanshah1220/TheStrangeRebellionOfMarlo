using System.Collections.Generic;

[System.Serializable]
public class CaseResult
{
    public string caseId;
    public string caseTitle;
    public string caseType;              // "Core" or "Secondary"
    public bool correctSuspect;
    public bool correctViolation;
    public int slotsCorrect;
    public int slotsTotal;
    public float confidenceScore;         // 0-100 from VerdictEvaluator
    public float reward;                  // Computed bonus amount
    public bool isFullyCorrect;
    public float baseReward;              // Original case reward before modifiers

    /// <summary>
    /// Compute the bonus reward based on verdict accuracy.
    /// Perfect verdict: 1.5x base reward. Partial: proportional to slots correct.
    /// </summary>
    public float ComputeReward(float caseBaseReward)
    {
        baseReward = caseBaseReward;

        if (slotsTotal == 0)
        {
            reward = 0f;
            return reward;
        }

        float accuracy = (float)slotsCorrect / slotsTotal;
        float multiplier = accuracy;

        if (isFullyCorrect)
            multiplier = 1.5f;

        reward = caseBaseReward * multiplier;
        return reward;
    }
}
