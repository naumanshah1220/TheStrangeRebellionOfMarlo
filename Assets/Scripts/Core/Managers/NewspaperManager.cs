using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CaseOutcomeHeadline
{
    public string caseId;
    public string headlineIfSolved;
    public string headlineIfUnsolved;
    public int priority = 1; // Higher priority headlines take precedence
}

[System.Serializable]
public class DayHeadline
{
    public int day;
    public string defaultHeadline;
    public List<CaseOutcomeHeadline> caseOutcomes = new List<CaseOutcomeHeadline>();
}

public class NewspaperManager : SingletonMonoBehaviour<NewspaperManager>
{
    public List<DayHeadline> dayHeadlines = new List<DayHeadline>();
    
    private CaseManager caseManager;

    private void Start()
    {
        caseManager = FindFirstObjectByType<CaseManager>();
        if (!caseManager)
            Debug.LogError("CaseManager not found in NewspaperManager!");
    }

    /// <summary>
    /// Loads day headlines from JSON data. Replaces inspector-configured data.
    /// Inspector data is kept as fallback if no JSON is provided.
    /// </summary>
    public void LoadFromJson(DaysBriefingDataJson data)
    {
        if (data == null || data.days == null) return;

        var jsonHeadlines = new List<DayHeadline>();
        foreach (var dayJson in data.days)
        {
            var dh = new DayHeadline
            {
                day = dayJson.day,
                defaultHeadline = dayJson.headline ?? "",
                caseOutcomes = new List<CaseOutcomeHeadline>()
            };

            if (dayJson.caseOutcomeHeadlines != null)
            {
                foreach (var co in dayJson.caseOutcomeHeadlines)
                {
                    dh.caseOutcomes.Add(new CaseOutcomeHeadline
                    {
                        caseId = co.caseId,
                        headlineIfSolved = co.headlineIfSolved,
                        headlineIfUnsolved = co.headlineIfUnsolved,
                        priority = co.priority
                    });
                }
            }

            jsonHeadlines.Add(dh);
        }

        dayHeadlines = jsonHeadlines;
        Debug.Log($"[NewspaperManager] Loaded {jsonHeadlines.Count} day headline(s) from JSON.");
    }

    public string GetHeadlineForDay(int day, List<Case> yesterdaysCases)
    {
        var dayHeadline = dayHeadlines.Find(d => d.day == day);
        if (dayHeadline == null) return string.Empty;

        // Start with default headline
        string headline = dayHeadline.defaultHeadline;
        int highestPriority = -1;

        // Check case outcomes and use the highest priority headline
        foreach (var outcome in dayHeadline.caseOutcomes)
        {
            if (outcome.priority <= highestPriority) continue;

            var matchingCase = yesterdaysCases.Find(c => c.caseID == outcome.caseId);
            if (matchingCase != null)
            {
                var status = caseManager.GetCaseStatus(matchingCase.caseID);
                if (status != null)
                {
                    string newHeadline = status.isSolved ? outcome.headlineIfSolved : outcome.headlineIfUnsolved;
                    if (!string.IsNullOrEmpty(newHeadline))
                    {
                        headline = newHeadline;
                        highestPriority = outcome.priority;
                    }
                }
            }
        }

        return headline;
    }
} 