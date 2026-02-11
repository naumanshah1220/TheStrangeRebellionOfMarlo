using UnityEngine;
using System.Collections.Generic;

public enum BookShelf
{
    BookHand1,
    BookHand2
}

[CreateAssetMenu(fileName = "NewBook", menuName = "Game/Book")]
public class Book : ScriptableObject, ICardData
{
    public string id;
    public string title;
    [TextArea(3, 5)]
    public string description;
    public GameObject fullCardPrefab;
    public Sprite cardImage;
    
    [Header("Book Settings")]
    public string author;
    public string publisher;
    public int publicationYear;
    public string isbn;
    
    [Header("Availability")]
    public int availableFromDay = 1; // Day when this book becomes available
    public bool isTutorialBook = false; // Special books that are always available
    
    [Header("Placement")]
    public BookShelf targetShelf = BookShelf.BookHand1; // Which shelf this book should be placed on
    
    // ICardData implementation
    public string GetCardID() => id;
    public string GetCardTitle() => title;
    public string GetCardDescription() => description;
    public Sprite GetCardImage() => cardImage;
    public GameObject GetFullCardPrefab() => fullCardPrefab;
    public CardMode GetCardMode() => CardMode.Book;
}
