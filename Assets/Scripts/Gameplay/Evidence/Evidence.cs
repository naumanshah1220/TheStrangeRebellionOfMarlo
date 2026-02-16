using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEvidence", menuName = "Game/Evidence")]
public class Evidence : ScriptableObject, ICardData
{
    public string id;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public EvidenceType type;
    public GameObject fullCardPrefab;

    public Sprite cardImage;
    
    [Header("Spectrograph")]
    [SerializeField] public ForeignSubstanceType foreignSubstance = ForeignSubstanceType.None;
    
    [Header("Hotspots (JSON-driven clue regions)")]
    public List<EvidenceHotspot> hotspots = new List<EvidenceHotspot>();

    [Header("Disk Evidence Settings")]
    [SerializeField] private AppConfig associatedApp;
    public bool HasAssociatedApp => type == EvidenceType.Disc && associatedApp != null;
    public AppConfig AppConfig => associatedApp;

    // ICardData implementation
    public string GetCardID() => id;
    public string GetCardTitle() => title;
    public string GetCardDescription() => description;
    public Sprite GetCardImage() => cardImage;
    public GameObject GetFullCardPrefab() => fullCardPrefab;
    public CardMode GetCardMode() => CardMode.Evidence;
}

public enum EvidenceType
{
    Document,
    Photo,
    Disc,
    Item
}
