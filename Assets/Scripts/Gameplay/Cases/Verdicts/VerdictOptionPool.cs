using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "VerdictOptionPool", menuName = "Cases/Verdict Option Pool")]
public class VerdictOptionPool : ScriptableObject
{
    public List<VerdictOption> options = new List<VerdictOption>();
}
