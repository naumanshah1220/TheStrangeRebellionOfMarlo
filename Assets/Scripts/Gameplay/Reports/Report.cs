using UnityEngine;

[CreateAssetMenu(fileName = "NewReport", menuName = "Game/Report")]
public class Report : ScriptableObject, ICardData
{
    public string id;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public GameObject fullCardPrefab;
    public Sprite cardImage;
    
    [Header("Report Settings")]
    public string author;
    public string department;
    public string reportDate;
    public string caseNumber;
    public ReportType reportType;
    
    // ICardData implementation
    public string GetCardID() => id;
    public string GetCardTitle() => title;
    public string GetCardDescription() => description;
    public Sprite GetCardImage() => cardImage;
    public GameObject GetFullCardPrefab() => fullCardPrefab;
    public CardMode GetCardMode() => CardMode.Report;
}

public enum ReportType
{
    Police,
    Medical,
    Forensic,
    Witness,
    Expert,
    Administrative
}
