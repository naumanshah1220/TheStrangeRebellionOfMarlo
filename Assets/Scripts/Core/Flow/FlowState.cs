/// <summary>
/// High-level game flow states. FlowController owns transitions between these.
/// GameManager's internal states (CaseSelection, CaseActive, etc.) are sub-states of Workday.
/// </summary>
public enum FlowState
{
    None,
    MainMenu,
    LoreSlideshow,
    DayBriefing,
    Workday,
    NightSummary,
    BillsDesk,
    GameEnd
}
