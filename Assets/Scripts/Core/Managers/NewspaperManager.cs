using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    private DaysBriefingDataJson storedDaysData;

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

        storedDaysData = data;

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

    /// <summary>
    /// Build a multi-article newspaper for the given day using previous day's case results and JSON filler articles.
    /// </summary>
    public NewspaperData BuildNewspaper(int day, DayResults previousDayResults)
    {
        var newspaper = new NewspaperData { dayNumber = day };

        // 1. Get the main headline (existing logic)
        List<Case> yesterdaysClosed = new List<Case>();
        if (DaysManager.Instance != null)
            yesterdaysClosed = DaysManager.Instance.todaysClosedCases;

        newspaper.mainHeadline = GetHeadlineForDay(day, yesterdaysClosed);

        // 2. Generate case outcome articles from previous day's results
        if (previousDayResults != null)
        {
            foreach (var result in previousDayResults.caseResults)
            {
                string outcomeHeadline;
                string outcomeBody;

                if (result.isFullyCorrect)
                {
                    outcomeHeadline = $"CASE RESOLVED: {result.caseTitle.ToUpper()}";
                    outcomeBody = $"Bureau analysts successfully identified the perpetrator. The case has been closed with full confidence. The Council commends the thoroughness of the investigation.";
                }
                else if (result.slotsCorrect > 0)
                {
                    outcomeHeadline = $"CASE CLOSED WITH QUESTIONS: {result.caseTitle.ToUpper()}";
                    outcomeBody = $"The Bureau has filed its report on the case, though some details remain uncertain. Confidence in the verdict stands at {result.confidenceScore:F0}%.";
                }
                else
                {
                    outcomeHeadline = $"INVESTIGATION INCONCLUSIVE: {result.caseTitle.ToUpper()}";
                    outcomeBody = $"Despite Bureau efforts, the submitted verdict appears to lack substantiation. Internal review has been requested.";
                }

                newspaper.articles.Add(new NewspaperArticle
                {
                    headline = outcomeHeadline,
                    body = outcomeBody,
                    category = "case_outcome",
                    priority = result.caseType == "Core" ? 8 : 6
                });
            }

            // Add article about unfinished cases if any
            if (previousDayResults.unfinishedCoreCount > 0)
            {
                newspaper.articles.Add(new NewspaperArticle
                {
                    headline = "BUREAU BACKLOGS RAISE COUNCIL CONCERNS",
                    body = $"{previousDayResults.unfinishedCoreCount} core case(s) remain unresolved. The Council has expressed displeasure at the Bureau's pace.",
                    category = "case_outcome",
                    priority = 9
                });
            }
        }

        // 3. Add world/propaganda filler articles from JSON
        if (storedDaysData != null && storedDaysData.days != null)
        {
            var dayJson = storedDaysData.days.Find(d => d.day == day);
            if (dayJson?.newspaperArticles != null)
            {
                foreach (var articleJson in dayJson.newspaperArticles)
                {
                    newspaper.articles.Add(new NewspaperArticle
                    {
                        headline = articleJson.headline,
                        body = articleJson.body,
                        category = articleJson.category,
                        priority = articleJson.priority
                    });
                }
            }
        }

        // 4. Sort by priority (highest first)
        newspaper.articles.Sort((a, b) => b.priority.CompareTo(a.priority));

        // Use highest-priority article headline as main headline if no case-outcome headline was set
        if (string.IsNullOrEmpty(newspaper.mainHeadline) && newspaper.articles.Count > 0)
            newspaper.mainHeadline = newspaper.articles[0].headline;

        Debug.Log($"[NewspaperManager] Built newspaper for day {day}: {newspaper.articles.Count} articles, main: '{newspaper.mainHeadline}'");
        return newspaper;
    }
}
