using System.Collections.Generic;

/// <summary>
/// Plain data struct passed from FlowController to DayBriefingPanel.
/// Built each day from JSON + runtime state.
/// </summary>
public struct DayBriefingData
{
    public int dayNumber;
    public string headline;
    public string subheadline;
    public string letterFrom;
    public string letterBody;
    public List<string> unlockNotices;
}
