using System.Linq;
using System.Collections.Generic;

public static class VerdictEvaluator
{
    public static int ComputeConfidence(Case c, CaseVerdict v)
    {
        // naive: +20 per slot matched, +10 if all required justifications present
        var best = MatchBestSolution(c, v, out int slotMatches, out bool justOk);
        int conf = slotMatches * 20 + (justOk ? 10 : 0);
        return UnityEngine.Mathf.Clamp(conf, 0, 100);
    }

    public static bool IsFullyCorrect(Case c, CaseVerdict v)
    {
        var best = MatchBestSolution(c, v, out int slotMatches, out bool justOk);
        return best;
    }

    private static bool MatchBestSolution(Case c, CaseVerdict v, out int slotMatches, out bool justOk)
    {
        slotMatches = 0; justOk = false;
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


            bool justSatisfied = true;
            if (sol.requiredJustificationTagIds != null && sol.requiredJustificationTagIds.Length > 0)
            {
                var allAttached = new HashSet<string>(v.justificationTagIds);
                foreach (var req in sol.requiredJustificationTagIds)
                    if (!allAttached.Contains(req)) { justSatisfied = false; break; }
            }

            if (!any || matches > slotMatches || (matches == slotMatches && justSatisfied && !justOk))
            {
                slotMatches = matches; justOk = justSatisfied; any = true;
            }
        }
        return any && c.verdictSchema != null && c.verdictSchema.slots != null && slotMatches == c.verdictSchema.slots.Count && justOk;
    }
}
