using System.Collections.Generic;

[System.Serializable]
public class CaseVerdict
{
    [System.Serializable]
    public class SlotSelection
    {
        public string slotId;
        public string optionId;
    }

    public string caseID;
    public List<SlotSelection> selections = new List<SlotSelection>();
    public int computedConfidence;
}
