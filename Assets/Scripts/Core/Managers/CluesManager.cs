using System.Collections.Generic;
using UnityEngine;

public class CluesManager : SingletonMonoBehaviour<CluesManager>
{
    private HashSet<string> foundClueIDs = new HashSet<string>();

    public void OnClueFound(Clue clue)
    {
        if (clue == null || string.IsNullOrEmpty(clue.clueID)) return;

        if (foundClueIDs.Contains(clue.clueID))
        {
            Debug.Log($"[CluesManager] Clue already found: {clue.clueID}");
            return;
        }

        foundClueIDs.Add(clue.clueID);
        Debug.Log($"[CluesManager] Clue found: {clue.clueID} - {clue.noteText} ({foundClueIDs.Count} total)");

        GameEvents.RaiseClueDiscovered(clue.clueID);
    }

    public bool IsClueFound(string clueID) => foundClueIDs.Contains(clueID);

    public Clue GetClueByID(string clueID)
    {
        if (string.IsNullOrEmpty(clueID)) return null;

        // Delegate to EvidenceManager which maintains the case clue registry
        if (EvidenceManager.Instance != null)
        {
            return EvidenceManager.Instance.GetClueByID(clueID);
        }

        return null;
    }

    public int FoundClueCount => foundClueIDs.Count;

    public void ClearForNewCase()
    {
        foundClueIDs.Clear();
        Debug.Log("[CluesManager] Cleared found clues for new case");
    }
}
