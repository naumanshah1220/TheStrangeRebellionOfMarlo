using UnityEngine;
using System;

[CreateAssetMenu(fileName = "NewOverseerNote", menuName = "Game/OverseerNote")]
public class OverseerNotes : ScriptableObject, ICardData
{
    public string id;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public GameObject fullCardPrefab;
    public Sprite cardImage;
    
    [Header("Note Settings")]
    public string sender = "Overseer";
    public DateTime timestamp = DateTime.Now;
    
    [Header("Trigger Settings")]
    public NoteTrigger trigger = NoteTrigger.StartOfDay;
    public int triggerDay = 1; // For day-specific triggers
    public string triggerCaseId = ""; // For case-specific triggers
    public float triggerHoursAfterStart = 0f; // For DayXAfterYHours trigger - hours after day start
    
    // ICardData implementation
    public string GetCardID() => id;
    public string GetCardTitle() => title;
    public string GetCardDescription() => description;
    public Sprite GetCardImage() => cardImage;
    public GameObject GetFullCardPrefab() => fullCardPrefab;
    public CardMode GetCardMode() => CardMode.OverseerNote;
}

public enum NoteTrigger
{
    StartOfDay,           // Triggered at the start of a specific day
    DayXAfterYHours,      // Triggered X hours after day start on day Y
    CaseXOpen,           // Triggered when a specific case is opened
    CaseXSubmission,     // Triggered when a specific case is submitted
    AllCasesSolved       // Triggered when all cases for the day are solved
}
