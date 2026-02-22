using System.Linq;

public static class VerdictEvaluator
{
    public static int ComputeConfidence(Case c, CaseVerdict v)
    {
        var best = MatchBestSolution(c, v, out int slotMatches);
        int conf = slotMatches * 20;
        return UnityEngine.Mathf.Clamp(conf, 0, 100);
    }

    public static bool IsFullyCorrect(Case c, CaseVerdict v)
    {
        return MatchBestSolution(c, v, out int slotMatches);
    }

    /// <summary>
    /// Full verdict evaluation returning a CaseResult with per-slot breakdown and computed reward.
    /// </summary>
    public static CaseResult EvaluateVerdict(Case c, CaseVerdict v)
    {
        var result = new CaseResult
        {
            caseId = c.caseID,
            caseTitle = c.title,
            caseType = c.caseType.ToString()
        };

        if (c.solutions == null || c.solutions.Length == 0)
        {
            result.confidenceScore = 0;
            result.reward = 0;
            return result;
        }

        // Find best matching solution with detailed breakdown
        int bestSlotMatches = 0;
        bool bestCorrectSuspect = false;
        bool bestCorrectViolation = false;

        foreach (var sol in c.solutions)
        {
            int matches = 0;
            bool suspectCorrect = false;
            bool violationCorrect = false;

            if (sol.answers != null)
            {
                foreach (var s in sol.answers)
                {
                    var sel = v.selections.FirstOrDefault(x => x.slotId == s.slotId);
                    if (sel != null && s.acceptedOptionIds.Contains(sel.optionId))
                    {
                        matches++;
                        if (s.slotId.ToLower().Contains("suspect"))
                            suspectCorrect = true;
                        if (s.slotId.ToLower().Contains("violation") || s.slotId.ToLower().Contains("crime"))
                            violationCorrect = true;
                    }
                }
            }

            if (matches > bestSlotMatches)
            {
                bestSlotMatches = matches;
                bestCorrectSuspect = suspectCorrect;
                bestCorrectViolation = violationCorrect;
            }
        }

        int totalSlots = c.verdictSchema != null && c.verdictSchema.slots != null
            ? c.verdictSchema.slots.Count
            : 0;

        result.slotsCorrect = bestSlotMatches;
        result.slotsTotal = totalSlots;
        result.correctSuspect = bestCorrectSuspect;
        result.correctViolation = bestCorrectViolation;
        result.isFullyCorrect = totalSlots > 0 && bestSlotMatches == totalSlots;
        result.confidenceScore = ComputeConfidence(c, v);

        // Compute reward
        result.ComputeReward(c.reward);

        return result;
    }

    private static bool MatchBestSolution(Case c, CaseVerdict v, out int slotMatches)
    {
        slotMatches = 0;
        bool any = false;

        if (c.solutions == null) return false;

        foreach (var sol in c.solutions)
        {
            int matches = 0;
            if (sol.answers != null)
            {
                foreach (var s in sol.answers)
                {
                    var sel = v.selections.FirstOrDefault(x => x.slotId == s.slotId);
                    if (sel != null && s.acceptedOptionIds.Contains(sel.optionId)) matches++;
                }
            }

            if (!any || matches > slotMatches)
            {
                slotMatches = matches; any = true;
            }
        }
        return any && c.verdictSchema != null && c.verdictSchema.slots != null && slotMatches == c.verdictSchema.slots.Count;
    }
}
