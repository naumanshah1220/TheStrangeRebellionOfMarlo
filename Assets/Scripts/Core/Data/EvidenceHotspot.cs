/// <summary>
/// Runtime data for a clickable clue region on an evidence card.
/// Populated from JSON via ContentLoader.
/// </summary>
[System.Serializable]
public class EvidenceHotspot
{
    public string clueId;
    public string noteText;
    public int pageIndex;
    public float positionX = 0.5f;  // normalized 0-1 within evidence image
    public float positionY = 0.5f;
    public float width = 0.3f;
    public float height = 0.1f;
}
