using System.Collections.Generic;

[System.Serializable]
public class NewspaperArticle
{
    public string headline;
    public string body;
    public string category;       // "case_outcome", "world_event", "regime_propaganda"
    public int priority;
}

[System.Serializable]
public class NewspaperData
{
    public int dayNumber;
    public string mainHeadline;
    public List<NewspaperArticle> articles = new List<NewspaperArticle>();
}
