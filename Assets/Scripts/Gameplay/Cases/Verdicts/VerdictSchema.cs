using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "VerdictSchema", menuName = "Cases/Verdict Schema")]
public class VerdictSchema : ScriptableObject
{
    [TextArea(2, 4)]
    public string sentenceTemplate = "I accuse {suspect} of {violation} by {method} at {location} because {motive}.";
    public List<VerdictSlotDefinition> slots = new List<VerdictSlotDefinition>();
    public VerdictOptionPool[] globalPools; // optional global pools (methods, motives...)
}
