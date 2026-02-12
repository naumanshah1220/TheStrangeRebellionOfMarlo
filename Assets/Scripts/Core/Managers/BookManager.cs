using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages book hands and their associated book collections
/// Allows for easy testing and management of books in the detective game
/// </summary>
public class BookManager : SingletonMonoBehaviour<BookManager>
{
    
    [Header("Book Hands")]
    public HorizontalCardHolder bookHand1;
    public HorizontalCardHolder bookHand2;
    
    [Header("Book Collection")]
    public List<Book> allBooks = new List<Book>();

    protected override void OnSingletonAwake()
    {
        SetupEventListeners();
    }

    protected override void OnSingletonDestroy()
    {
        RemoveEventListeners();
    }
    
    private void SetupEventListeners()
    {
        // Listen to day start events
        if (DaysManager.Instance != null)
        {
            DaysManager.Instance.onDayStart.AddListener(OnDayStart);
        }
    }
    
    private void Start()
    {
        // Wait for DaysManager to be ready, then set up event listener
        StartCoroutine(WaitForDaysManager());
    }
    
    private System.Collections.IEnumerator WaitForDaysManager()
    {
        // Wait until DaysManager.Instance is available
        while (DaysManager.Instance == null)
        {
            yield return null;
        }
        
        // Set up the event listener
        DaysManager.Instance.onDayStart.AddListener(OnDayStart);
        Debug.Log("[BookManager] Event listener connected");
    }
    
    private void RemoveEventListeners()
    {
        if (DaysManager.Instance != null)
        {
            DaysManager.Instance.onDayStart.RemoveListener(OnDayStart);
        }
    }
    
    /// <summary>
    /// Called when a day starts - auto-load available books
    /// </summary>
    private void OnDayStart()
    {
        int currentDay = DaysManager.Instance?.GetCurrentDay() ?? 1;
        LoadBooksForDay(currentDay);
    }

    public void OpenBook(Card card)
    {
        if (card == null || card.mode != CardMode.Book) 
            return;

        if (bookHand1.CanAcceptCardType(CardMode.Book) && bookHand1.Cards.Count == 0)
        {
            MoveCardToHand(card, bookHand1);
        }
        else if (bookHand2.CanAcceptCardType(CardMode.Book) && bookHand2.Cards.Count == 0)
        {
            MoveCardToHand(card, bookHand2);
        }
    }

    public void CloseAllBooks()
    {
        CloseBook(bookHand1);
        CloseBook(bookHand2);
    }

    public void CloseBook(HorizontalCardHolder bookHand)
    {
        if (bookHand.Cards.Count > 0)
        {
            Card card = bookHand.Cards.FirstOrDefault();
            if (card == null || card.mode != CardMode.Book) return;

            // Return card to its original hand
            CardTypeManager.Instance.AddCardToAppropriateHand(card);
        }
    }
    
    private void MoveCardToHand(Card card, HorizontalCardHolder targetHand)
    {
        if (card.parentHolder != null)
        {
            card.parentHolder.RemoveCard(card);
        }
        targetHand.AddCardToHand(card);
    }
    
    /// <summary>
    /// Gets all books available for a specific day
    /// </summary>
    public List<Book> GetBooksForDay(int dayNumber)
    {
        return allBooks.Where(book => 
            book.isTutorialBook || book.availableFromDay <= dayNumber
        ).ToList();
    }
    
    /// <summary>
    /// Gets books that become newly available on a specific day
    /// </summary>
    public List<Book> GetNewBooksForDay(int dayNumber)
    {
        return allBooks.Where(book => 
            !book.isTutorialBook && book.availableFromDay == dayNumber
        ).ToList();
    }
    
    /// <summary>
    /// Gets tutorial books that should be available from the start
    /// </summary>
    public List<Book> GetTutorialBooks()
    {
        return allBooks.Where(book => book.isTutorialBook).ToList();
    }
    
    /// <summary>
    /// Public method to manually trigger book loading (for testing/debugging)
    /// </summary>
    public void ManualLoadBooksForCurrentDay()
    {
        int currentDay = DaysManager.Instance?.GetCurrentDay() ?? 1;
        LoadBooksForDay(currentDay);
    }
    
    /// <summary>
    /// Loads books for a specific day into their appropriate shelves
    /// </summary>
    public void LoadBooksForDay(int dayNumber)
    {
        var availableBooks = GetBooksForDay(dayNumber);
        
        if (availableBooks.Count == 0)
        {
            Debug.LogWarning($"[BookManager] No books available for Day {dayNumber}");
            return;
        }
        
        Debug.Log($"[BookManager] Loading {availableBooks.Count} books for Day {dayNumber}");
        
        foreach (var book in availableBooks)
        {
            LoadBookToShelf(book);
        }
    }
    
    /// <summary>
    /// Loads a specific book to its designated shelf
    /// </summary>
    private void LoadBookToShelf(Book book)
    {
        if (book == null) return;
        
        // Get the appropriate shelf based on book's targetShelf
        HorizontalCardHolder targetShelf = GetShelfForBook(book);
        
        if (targetShelf == null)
        {
            Debug.LogWarning($"[BookManager] No shelf found for book '{book.title}' with targetShelf '{book.targetShelf}'");
            return;
        }
        
        // Check if book is already loaded in any shelf
        if (IsBookAlreadyLoaded(book))
        {
            return;
        }
        
        // Use the proper LoadCardsFromData method to load the book
        targetShelf.LoadCardsFromData(new List<Book> { book }, false);
        Debug.Log($"[BookManager] Loaded '{book.title}' to {book.targetShelf}");
    }
    
    /// <summary>
    /// Gets the appropriate shelf for a book based on its targetShelf setting
    /// </summary>
    private HorizontalCardHolder GetShelfForBook(Book book)
    {
        switch (book.targetShelf)
        {
            case BookShelf.BookHand1:
                return bookHand1;
            case BookShelf.BookHand2:
                return bookHand2;
            default:
                return bookHand1; // Default to BookHand1
        }
    }
    
    /// <summary>
    /// Checks if a book is already loaded in any shelf
    /// </summary>
    private bool IsBookAlreadyLoaded(Book book)
    {
        // Check Shelf1
        if (bookHand1 != null)
        {
            foreach (Card card in bookHand1.Cards)
            {
                if (card.GetCardData() is Book bookData && bookData.id == book.id)
                    return true;
            }
        }
        
        // Check Shelf2
        if (bookHand2 != null)
        {
            foreach (Card card in bookHand2.Cards)
            {
                if (card.GetCardData() is Book bookData && bookData.id == book.id)
                    return true;
            }
        }
        
        return false;
    }
    
}
