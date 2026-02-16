/// <summary>
/// Maps a discovered clue to a verdict option it reveals.
/// When a player discovers a clue, any verdict slot using FromDiscoveredTags
/// will show the mapped option.
/// </summary>
[System.Serializable]
public class ClueVerdictMapping
{
    public string clueId;     // e.g. "clue_access_logs"
    public string slotId;     // e.g. "violation"
    public string optionId;   // e.g. "obstruction_of_records"
    public string label;      // e.g. "Obstruction of Records"
}
