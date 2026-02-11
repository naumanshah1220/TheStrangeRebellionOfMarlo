using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Detective.UI.Commit;

public class CluesManager : MonoBehaviour
{
    public static CluesManager Instance { get; private set; }
    
    // Track found clues to prevent duplicates
    private HashSet<string> foundClueIDs = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void OnClueFound(Clue clue)
    {
        Debug.Log($"Clue Found: {clue.clueID} - {clue.noteText}");
        // Here you would typically notify other systems, like the CaseManager
        // that a new clue has been discovered.
        // For example: CaseManager.Instance.UpdateCaseProgressWithClue(clue.clueID);
    }
    
    public bool IsClueFound(string clueID) => foundClueIDs.Contains(clueID);

    // This is a placeholder. You would need a robust system to manage all clues in the game.
    // This could be loading them from a database (ScriptableObject) or having them registered.
    public Clue GetClueByID(string clueID)
    {
        // In a real implementation, you would search a collection of all clues.
        // For now, we'll just return null as this is a conceptual placeholder.
        Debug.LogWarning($"GetClueByID is not fully implemented. Could not retrieve clue '{clueID}'.");
        return null;
    }
}
