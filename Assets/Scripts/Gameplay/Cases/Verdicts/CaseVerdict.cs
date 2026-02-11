using System.Collections.Generic;

[System.Serializable]
public class CaseVerdict
{
    [System.Serializable]
    public class SlotSelection
    {
        public string slotId;
        public string optionId;         // chosen option
        public List<string> attachedTagIds = new List<string>(); // justifications the player attached - THIS IS NOW DEPRECATED
    }

    public string caseID;
    public List<SlotSelection> selections = new List<SlotSelection>();
    public List<string> justificationTagIds = new List<string>(); // All justifications for the verdict are stored here
    public int computedConfidence;   // optional scoring output
}
