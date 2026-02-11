using UnityEngine;

/// <summary>
/// Data class for puzzle piece cards
/// Implements ICardData to work with the card initialization system
/// </summary>
[System.Serializable]
public class PuzzlePieceData : ICardData
{
    public int pieceIndex;
    public Card originalCard;
    public Sprite smallSprite;
    public Sprite bigSprite;
    public CardMode cardMode;
    
    public string GetCardID()
    {
        return $"piece_{pieceIndex}_{originalCard?.name ?? "unknown"}";
    }
    
    public string GetCardTitle()
    {
        return $"Piece {pieceIndex + 1}";
    }
    
    public string GetCardDescription()
    {
        return $"Puzzle piece {pieceIndex + 1} from {originalCard?.name ?? "Unknown Card"}";
    }
    
    public CardMode GetCardMode()
    {
        return cardMode;
    }
    
    public Sprite GetCardImage()
    {
        return smallSprite;
    }
    
    public GameObject GetFullCardPrefab()
    {
        // Puzzle pieces use the standard BigCardVisual system, not custom prefabs
        return null;
    }
} 