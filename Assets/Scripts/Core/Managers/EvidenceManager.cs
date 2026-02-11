using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvidenceManager : MonoBehaviour
{
    [SerializeField] public HorizontalCardHolder evidenceHand;
    [SerializeField] public HorizontalCardHolder matHand;

    public static EvidenceManager Instance { get; private set; }

    private Dictionary<Evidence, GameObject> evidenceToInstance = new Dictionary<Evidence, GameObject>();
    private Dictionary<string, Clue> currentCaseClues = new Dictionary<string, Clue>();



    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCaseOpened += RegisterAllCluesForCase;
    }

    public void LoadMainEvidences(List<Evidence> evidenceList)
    {
        StartCoroutine(LoadEvidenceCoroutine(evidenceList, true));
    }

    public void LoadAllExtraEvidences(List<Evidence> evidenceList)
    {
        Debug.Log("Extra evidence being loaded");
        StartCoroutine(LoadEvidenceCoroutine(evidenceList, false));
    }

    public void LoadExtraEvidence(Evidence extraEvidence)
    {
        if (extraEvidence == null) return;
        StartCoroutine(LoadSingleEvidenceCoroutine(extraEvidence));
    }

    private IEnumerator LoadSingleEvidenceCoroutine(Evidence extraEvidence)
    {
        // Load one extra evidence, append to hand (don't delete old)
        if (evidenceHand != null)
        {
            evidenceHand.LoadCardsFromData(new List<Evidence> { extraEvidence }, false); // deleteOld: false
        }
        yield return null;
    }


    public IEnumerator LoadEvidenceCoroutine(List<Evidence> evidenceList, bool deleteOld = true)
    {
        //Load Main Evidences of the Case
        if (evidenceHand != null)
        {
            evidenceHand.LoadCardsFromData(evidenceList, deleteOld);
        }
        yield return null;
    }

    public void RegisterEvidenceInstance(Evidence ev, GameObject go)
    {
        evidenceToInstance[ev] = go;
    }

    public IEnumerable<Clue> GetAllCluesOnMatForCase(Case c)
    {
        foreach (var ev in c.evidences)
        {
            if (evidenceToInstance.TryGetValue(ev, out var go) && go != null)
            {
                foreach (var clue in go.GetComponentsInChildren<Clue>(true))
                    yield return clue;
            }
        }
    }

    public void RegisterAllCluesForCase(Case currentCase)
    {
        currentCaseClues.Clear();
        foreach (var ev in currentCase.evidences)
        {
            RegisterCluesForEvidence(ev);
        }
        foreach (var ev in currentCase.extraEvidences)
        {
            RegisterCluesForEvidence(ev);
        }
    }

    // Helper: get all clues from prefab (even if not yet instantiated, by instantiating once and destroying, or by using a reference list in the SO)
    private void RegisterCluesForEvidence(Evidence ev)
    {
        if (ev == null || ev.fullCardPrefab == null) return;
        var go = Instantiate(ev.fullCardPrefab);
        var clues = go.GetComponentsInChildren<Clue>(true);
        foreach (var clue in clues)
        {
            if (!currentCaseClues.ContainsKey(clue.clueID))
                currentCaseClues.Add(clue.clueID, clue);
        }
        Destroy(go); // clean up temp instance
    }

    // To get all clues for current case:
    public IEnumerable<Clue> GetAllCluesForCurrentCase()
    {
        return currentCaseClues.Values;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnCaseOpened -= RegisterAllCluesForCase;
    }

}
