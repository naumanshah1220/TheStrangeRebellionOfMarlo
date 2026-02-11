using UnityEngine;

/// <summary>
/// Interface for all card data types (Case, Evidence, Phone, etc.)
/// This allows for a generic card system that can handle any card type
/// </summary>
public interface ICardData
{
    string GetCardID();
    string GetCardTitle();
    string GetCardDescription();
    Sprite GetCardImage();
    GameObject GetFullCardPrefab();
    CardMode GetCardMode();
}

/// <summary>
/// Base class for all card data types
/// </summary>
public abstract class BaseCardData : ScriptableObject, ICardData
{
    [Header("Basic Card Info")]
    public string cardID;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public Sprite cardImage;
    public GameObject fullCardPrefab;

    public virtual string GetCardID() => cardID;
    public virtual string GetCardTitle() => title;
    public virtual string GetCardDescription() => description;
    public virtual Sprite GetCardImage() => cardImage;
    public virtual GameObject GetFullCardPrefab() => fullCardPrefab;
    public abstract CardMode GetCardMode();
} 