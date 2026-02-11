using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class BookTestHelper : MonoBehaviour
{
    [ContextMenu("Create Sample Books")]
    public void CreateSampleBooks()
    {
        // Create sample books for testing
        CreateBook("book_001", "The Art of Detection", "A comprehensive guide to modern detective work, covering forensic techniques, witness interviewing, and case analysis methods.", "Dr. Sarah Watson", "Detective Press", 2023, "978-1-234-56789-0");
        CreateBook("book_002", "Forensic Science Handbook", "Detailed manual covering DNA analysis, fingerprint identification, and crime scene investigation procedures.", "Prof. Michael Johnson", "Forensic Publications", 2022, "978-1-234-56789-1");
        CreateBook("book_003", "Criminal Psychology", "Understanding the criminal mind and behavioral patterns in investigation and profiling.", "Dr. Emily Rodriguez", "Psychology Press", 2024, "978-1-234-56789-2");
        CreateBook("book_004", "Evidence Collection Manual", "Step-by-step guide to proper evidence collection, preservation, and documentation.", "Detective Robert Chen", "Law Enforcement Press", 2023, "978-1-234-56789-3");
        
        Debug.Log("Sample books created! Check the Resources/Books folder.");
    }

    private void CreateBook(string id, string title, string description, string author, string publisher, int year, string isbn)
    {
        Book book = ScriptableObject.CreateInstance<Book>();
        book.id = id;
        book.title = title;
        book.description = description;
        book.author = author;
        book.publisher = publisher;
        book.publicationYear = year;
        book.isbn = isbn;
        
        // Create the directory if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Books"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Books");
        }
        
        AssetDatabase.CreateAsset(book, $"Assets/Resources/Books/{id}.asset");
    }
}
#endif
