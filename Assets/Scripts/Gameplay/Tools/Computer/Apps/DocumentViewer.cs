using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Document viewer app for displaying text files
/// </summary>
public class DocumentViewer : ComputerApp, IFileContent
{
    [Header("Document Viewer References")]
    [SerializeField] private TMP_Text documentText;
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private ScrollRect documentScrollView;
    [SerializeField] private Button findButton;
    [SerializeField] private Button printButton;
    [SerializeField] private TMP_InputField searchInput;
    
    [Header("Settings")]
    [SerializeField] private float fontSize = 14f;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color backgroundColor = Color.white;
    
    private DiscFile currentFile;
    private string searchTerm = "";
    private int currentSearchIndex = -1;
    
    public override void Initialize(AppConfig appConfig)
    {
        base.Initialize(appConfig);
        
        // Set up scroll view
        if (documentScrollView != null)
        {
            documentScrollView.horizontal = false;
            documentScrollView.vertical = true;
        }
        
        // Set up buttons
        if (findButton != null)
        {
            findButton.onClick.AddListener(FindNext);
        }
        
        if (printButton != null)
        {
            printButton.onClick.AddListener(PrintDocument);
        }
        
        // Set up search input
        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(OnSearchTermChanged);
            searchInput.onSubmit.AddListener(OnSearchSubmitted);
        }
    }
    
    public void Initialize(DiscFile file)
    {
        currentFile = file;
        
        if (file == null) return;
        
        // Set file name
        if (fileNameText != null)
        {
            fileNameText.text = file.fileName;
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = file.fileDescription;
        }
        
        // Load document content
        LoadDocumentContent();
    }
    
    private void LoadDocumentContent()
    {
        if (currentFile == null || documentText == null) return;
        
        // Try to get text from content prefab first (new system)
        if (currentFile.contentPrefab != null)
        {
            // The content prefab should contain the text
            // This will be handled by the prefab itself
            Debug.Log($"[DocumentViewer] Using content prefab for file: {currentFile.fileName}");
            return;
        }
        
        // Use file description as fallback content
        if (!string.IsNullOrEmpty(currentFile.fileDescription))
        {
            documentText.text = currentFile.fileDescription;
            documentText.fontSize = fontSize;
            documentText.color = textColor;
            
            // Set background color
            var background = GetComponent<Image>();
            if (background != null)
            {
                background.color = backgroundColor;
            }
        }
        else
        {
            Debug.LogWarning($"[DocumentViewer] No document content found for file: {currentFile.fileName}");
            if (documentText != null)
            {
                documentText.text = "No document content available";
            }
        }
    }
    
    public void FindNext()
    {
        if (string.IsNullOrEmpty(searchTerm) || documentText == null) return;
        
        string text = documentText.text;
        int startIndex = currentSearchIndex + 1;
        
        int foundIndex = text.IndexOf(searchTerm, startIndex, System.StringComparison.OrdinalIgnoreCase);
        
        if (foundIndex == -1 && startIndex > 0)
        {
            // Wrap around to beginning
            foundIndex = text.IndexOf(searchTerm, 0, System.StringComparison.OrdinalIgnoreCase);
        }
        
        if (foundIndex != -1)
        {
            currentSearchIndex = foundIndex;
            HighlightSearchResult(foundIndex, searchTerm.Length);
        }
        else
        {
            Debug.Log($"[DocumentViewer] No more occurrences of '{searchTerm}' found");
        }
    }
    
    private void HighlightSearchResult(int startIndex, int length)
    {
        // For now, just scroll to the position
        // In a more advanced implementation, you could highlight the text
        if (documentScrollView != null && documentText != null)
        {
            // Calculate the position to scroll to
            // This is a simplified approach - you'd need more complex logic for proper text positioning
            float normalizedPosition = (float)startIndex / documentText.text.Length;
            documentScrollView.verticalNormalizedPosition = 1f - normalizedPosition;
        }
    }
    
    private void OnSearchTermChanged(string newTerm)
    {
        searchTerm = newTerm;
        currentSearchIndex = -1; // Reset search position
    }
    
    private void OnSearchSubmitted(string term)
    {
        FindNext();
    }
    
    public void PrintDocument()
    {
        // In a real implementation, this would open a print dialog
        Debug.Log($"[DocumentViewer] Print requested for: {currentFile?.fileName}");
        
        // For now, just log the action
        if (currentFile != null)
        {
            Debug.Log($"[DocumentViewer] Would print document: {currentFile.fileName}");
            Debug.Log($"[DocumentViewer] Content length: {currentFile.fileDescription?.Length ?? 0} characters");
        }
    }
    
    public override void OnAppOpen()
    {
        base.OnAppOpen();
        
        // Reset search
        searchTerm = "";
        currentSearchIndex = -1;
        
        if (searchInput != null)
        {
            searchInput.text = "";
        }
        
        // Scroll to top
        if (documentScrollView != null)
        {
            documentScrollView.verticalNormalizedPosition = 1f;
        }
    }
} 