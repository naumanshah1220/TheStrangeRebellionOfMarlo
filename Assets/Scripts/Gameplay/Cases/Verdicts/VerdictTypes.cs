using UnityEngine;
using System.Collections.Generic;

public enum VerdictSlotType { Suspect, Violation, Method, Motive, Location, Time, Item, Custom }

[System.Serializable]
public class VerdictSlotDefinition
{
    public string slotId;                 // e.g. "suspect", "method"
    public string displayLabel;           // e.g. "Who", "How"
    public VerdictSlotType type;
    public bool required = true;          // must be filled
    public int minJustificationTags = 0;  // tags player must attach to this slot
    public string[] tagTypesAccepted;     // e.g. ["person","item","location"] for drag behavior
    public OptionSource optionSource = OptionSource.CaseAndGlobal;
    public string customPoolId;           // for Custom
}

public enum OptionSource { CaseOnly, GlobalOnly, CaseAndGlobal, FromDiscoveredTags }

[System.Serializable]
public class VerdictOption
{
    public string id;         // global unique (e.g., "method_bludgeoning")
    public string label;      // UI text
    public VerdictSlotType type;
}

// Defines a correct combination for a case
[System.Serializable]
public class CaseSolution
{
    [System.Serializable]
    public class SlotAnswer
    {
        public string slotId;
        public string[] acceptedOptionIds; // support multiple acceptable values
    }

    public List<SlotAnswer> answers = new List<SlotAnswer>();
    public string[] requiredJustificationTagIds; // player must have discovered and/or attach to commit
    public string[] bonusJustificationTagIds; // optional "damning" clues for a score boost
    public int minConfidenceToApprove = 100;     // if you compute confidence
}

[System.Serializable]
public class JustificationDefinition
{
    public bool required = true;
    public int minRequired = 1;
}
