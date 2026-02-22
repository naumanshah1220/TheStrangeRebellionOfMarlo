using System.Collections.Generic;

[System.Serializable]
public class BonusLetterData
{
    public int forDay;                    // Which day's work this covers
    public float totalBonus;
    public string overseerMessage;        // Performance-based message
    public List<CaseResult> caseBreakdown;
    public bool hasPenalty;
    public float penaltyAmount;
    public string penaltyReason;          // overtime, wrong verdict, etc.

    /// <summary>
    /// Net bonus after penalties.
    /// </summary>
    public float NetBonus => UnityEngine.Mathf.Max(0f, totalBonus - penaltyAmount);
}
