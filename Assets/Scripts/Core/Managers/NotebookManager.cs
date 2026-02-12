using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Main notebook manager that coordinates handwriting animation, text processing, paging, and highlighting
/// </summary>
public class NotebookManager : SingletonMonoBehaviour<NotebookManager>
{
    [Header("References")]
    public RectTransform notebookPanel;
    public RectTransform leftPageParent;   // Left page container
    public RectTransform rightPageParent;  // Right page container (was clueNoteParent)
    public Button toggleNotebookButton;
    public GameObject clueNotePrefab;
    public GameObject suspectEntryPrefab;
    
    [Header("Tab Management")]
    public Button suspectsTab;
    public Button cluesTab;
    public TextMeshProUGUI suspectsTabText;
    public TextMeshProUGUI cluesTabText;
    public GameObject suspectsTabBadge;
    public GameObject cluesTabBadge;
    public TextMeshProUGUI suspectsCountText;
    public TextMeshProUGUI cluesCountText;
    
    [Header("Page Navigation")]
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageIndicatorText;
    
    [Header("Page Headers")]
    public TextMeshProUGUI leftPageHeaderText;
    public TextMeshProUGUI leftPageSubHeaderText;
    public TextMeshProUGUI rightPageHeaderText;
    public TextMeshProUGUI rightPageSubHeaderText;

    [Header("Components")]
    public HandwritingAnimator handwritingAnimator;
    public ClueTextProcessor textProcessor;
    public ClueHighlighter highlighter;

    [Header("Text Settings")]
    public float fontSize = 24f;
    public bool useCaretColorForText = true;

    [Header("Layout Settings")]
    public float minimumNoteHeight = 30f;
    public bool debugLayoutInfo = false;
    public int maxNotesPerPage = 10; // Maximum notes per page side (left or right)
    public int maxSuspectsPerPage = 4; // Maximum suspects per page (2 left + 2 right)
    
    [Header("Content Type Layout Settings")]
    public LayoutConfig suspectsLayoutConfig = new LayoutConfig
    {
        spacing = 120f,          // Large spacing between suspect entries
        topPadding = 0f,         // No top padding
        bottomPadding = 20f,     // Bottom padding only
        leftPadding = 0f,        // No left padding
        rightPadding = 0f,       // No right padding
        childForceExpandWidth = true,
        childForceExpandHeight = false,
        childControlWidth = true,
        childControlHeight = true
    };
    
    public LayoutConfig cluesLayoutConfig = new LayoutConfig
    {
        spacing = 8f,            // Tighter spacing for text-based clues
        topPadding = 15f,        // Standard padding
        bottomPadding = 15f,     // Standard padding
        leftPadding = 10f,       // Standard padding
        rightPadding = 10f,      // Standard padding
        childForceExpandWidth = true,
        childForceExpandHeight = false,
        childControlWidth = true,
        childControlHeight = true
    };
    
    [Header("Tab Visual Settings")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = Color.gray;
    public Color activeTextColor = Color.black;
    public Color inactiveTextColor = Color.white;
    public float tabSwitchDuration = 0.3f;
    public float tabScaleOnActive = 1.1f;
    public float tabScaleOnInactive = 1.0f;

    [Header("Positioning")]
    public Vector2 closedPosition = new Vector2(900, 0);
    public Vector2 openPosition = new Vector2(300, 0);
    public float slideDuration = 0.5f;
    public Ease openEase = Ease.OutBack;
    public Ease closeEase = Ease.InBack;

    // State
    private bool isOpen = false;
    private Dictionary<string, ClueNoteInfo> addedClues = new Dictionary<string, ClueNoteInfo>();
    
    // Book-style page management
    private List<BookPage> bookPages = new List<BookPage>();
    private int currentBookPageIndex = 0;
    private NotebookMode currentMode = NotebookMode.Suspects;
    private int lastCluesPageIndex = 0; // Track where clues were last added
    
    // Suspects management
    private List<GameObject> suspectEntries = new List<GameObject>();
    private SuspectsListManager suspectsManager;
    
    public enum NotebookMode
    {
        Suspects,
        Clues
    }
    
    public enum PageType
    {
        Suspects,
        Clues
    }
    
    [System.Serializable]
    public class BookPage
    {
        public PageType pageType;
        public List<GameObject> leftPageNotes = new List<GameObject>();
        public List<GameObject> rightPageNotes = new List<GameObject>();
        public int leftPageNoteCount = 0;
        public int rightPageNoteCount = 0;
        
        public BookPage(PageType type)
        {
            pageType = type;
        }
        
        public bool IsPageFull(int maxPerSide)
        {
            return leftPageNoteCount >= maxPerSide && rightPageNoteCount >= maxPerSide;
        }

        public bool CanAddNote(int maxPerSide)
        {
            return leftPageNoteCount < maxPerSide || rightPageNoteCount < maxPerSide;
        }

        public bool ShouldAddToLeftPage(int maxPerSide)
        {
            return leftPageNoteCount < maxPerSide;
        }
    }

    [System.Serializable]
    public class ClueNoteInfo
    {
        public string clueId;
        public GameObject noteObject;
        public int pageIndex;
        public string clueText;
        
        public ClueNoteInfo(string id, GameObject note, int page, string text)
        {
            clueId = id;
            noteObject = note;
            pageIndex = page;
            clueText = text;
        }
    }

    [System.Serializable]
    public class LayoutConfig
    {
        public float spacing = 10f;
        public float topPadding = 10f;
        public float bottomPadding = 10f;
        public float leftPadding = 10f;
        public float rightPadding = 10f;
        public bool childForceExpandWidth = true;
        public bool childForceExpandHeight = false;
        public bool childControlWidth = true;
        public bool childControlHeight = true;
    }

    protected override void OnSingletonAwake()
    {
        // Verify components are assigned
        if (handwritingAnimator == null) handwritingAnimator = GetComponent<HandwritingAnimator>();
        if (textProcessor == null) textProcessor = GetComponent<ClueTextProcessor>();
        if (highlighter == null) highlighter = GetComponent<ClueHighlighter>();

        // Initialize notebook position and button
        notebookPanel.anchoredPosition = closedPosition;
        if (toggleNotebookButton != null)
            toggleNotebookButton.onClick.AddListener(ToggleNotebook);

        // Initialize book-style pages
        InitializeBookPages();

        // Initialize tab management
        InitializeTabManagement();

        // Initialize page navigation
        InitializePageNavigation();

        // Initialize layout configuration
        InitializeLayoutConfiguration();

        // Get suspects manager reference
        suspectsManager = FindFirstObjectByType<SuspectsListManager>();
    }

    protected override void OnSingletonDestroy()
    {
        if (toggleNotebookButton != null)
            toggleNotebookButton.onClick.RemoveListener(ToggleNotebook);

        // Clean up tab buttons
        if (suspectsTab != null)
            suspectsTab.onClick.RemoveListener(() => SwitchToMode(NotebookMode.Suspects));
        if (cluesTab != null)
            cluesTab.onClick.RemoveListener(() => SwitchToMode(NotebookMode.Clues));

        // Clean up page navigation buttons
        if (previousPageButton != null)
            previousPageButton.onClick.RemoveListener(PreviousPage);
        if (nextPageButton != null)
            nextPageButton.onClick.RemoveListener(NextPage);
    }

    #region Public Interface

    /// <summary>
    /// Add a clue note to the notebook
    /// </summary>
    public void AddClueNote(string clueText)
    {
        AddClueNote(clueText, clueText); // Use clue text as ID if no ID provided
    }

    /// <summary>
    /// Add a clue note to the notebook with specific ID
    /// </summary>
    public void AddClueNote(string clueText, string clueId)
    {
        if (string.IsNullOrEmpty(clueText)) return;
        
        // Check if this clue contains suspect tags
        if (SuspectTagParser.ContainsSuspectTags(clueText))
        {
            // Handle suspect clues with special behavior
            StartCoroutine(AddSuspectClueRoutine(clueText, clueId, true));
        }
        else
        {
            // Handle regular clues
            if (addedClues.ContainsKey(clueId))
            {
                StartCoroutine(HighlightExistingClue(clueId));
            }
            else
            {
                StartCoroutine(AddClueNoteRoutine(clueText, clueId, true));
            }
        }
    }

    /// <summary>
    /// Add a clue note without auto-closing (for interrogation mode)
    /// </summary>
    public void AddClueNoteWithoutClosing(string clueText, string clueId)
    {
        // Check if this clue has already been added
        if (addedClues.ContainsKey(clueId))
        {
            // Clue already exists, highlight it instead
            StartCoroutine(HighlightExistingClue(clueId, false));
            return;
        }

        StartCoroutine(AddClueNoteRoutine(clueText, clueId, false));
    }

    /// <summary>
    /// Toggle notebook open/closed
    /// </summary>
    public void ToggleNotebook()
    {
        isOpen = !isOpen;
        notebookPanel.DOAnchorPos(
            isOpen ? openPosition : closedPosition,
            slideDuration
        ).SetEase(isOpen ? openEase : closeEase);
    }

    /// <summary>
    /// Open the notebook
    /// </summary>
    public void OpenNotebook()
    {
        if (!isOpen)
            ToggleNotebook();
    }

    /// <summary>
    /// Close the notebook
    /// </summary>
    public void CloseNotebook()
    {
        if (isOpen)
            ToggleNotebook();
    }

    /// <summary>
    /// Clear all pages and reset notebook state
    /// </summary>
    public void ClearAllPages()
    {
        // Clear all clues
        foreach (var clueInfo in addedClues.Values)
        {
            if (clueInfo.noteObject != null)
            {
                Destroy(clueInfo.noteObject);
            }
        }
        addedClues.Clear();
        
        // Clear all suspect entries
        ClearSuspectEntries();
        
        // Clear all book pages
        ClearAllBookPages();
        
        // Reinitialize with fresh suspects page
        InitializeBookPages();
        
        // Reset to suspects mode
        currentMode = NotebookMode.Suspects;
        currentBookPageIndex = 0;
        lastCluesPageIndex = 0;
        
        // Update display
        UpdatePageDisplay();
        UpdateTabCounts();
        
        if (debugLayoutInfo)
            Debug.Log("[NotebookManager] Cleared all pages and reset notebook state");
    }

    /// <summary>
    /// Clear only clues pages while keeping suspects (useful when case is closed)
    /// </summary>
    public void ClearCluesPages()
    {
        // Clear all clues
        foreach (var clueInfo in addedClues.Values)
        {
            if (clueInfo.noteObject != null)
            {
                Destroy(clueInfo.noteObject);
            }
        }
        addedClues.Clear();
        
        // Remove clues pages from book pages (keep suspects pages)
        for (int i = bookPages.Count - 1; i >= 0; i--)
        {
            if (bookPages[i].pageType == PageType.Clues)
            {
                bookPages.RemoveAt(i);
            }
        }
        
        // Reset clues tracking
        lastCluesPageIndex = 0;
        
        // If we're currently viewing a clues page, switch to suspects
        if (currentMode == NotebookMode.Clues)
        {
            SwitchToMode(NotebookMode.Suspects);
        }
        
        // Update display
        UpdatePageDisplay();
        UpdateTabCounts();
        
        if (debugLayoutInfo)
            Debug.Log("[NotebookManager] Cleared clues pages while keeping suspects");
    }

    /// <summary>
    /// Add a suspect entry to the suspects page
    /// </summary>
    public void AddSuspectEntry(GameObject suspectEntry)
    {
        if (suspectEntry == null)
        {
            Debug.LogError("[NotebookManager] Attempted to add null suspect entry!");
            return;
        }
        
        // Find or create a suspects page that can accommodate this entry
        int targetPageIndex = FindOrCreateSuspectsPage();
        
        var suspectsPage = bookPages[targetPageIndex];
        
        // Add to left page first, then right page
        if (suspectsPage.ShouldAddToLeftPage(maxSuspectsPerPage))
        {
            Debug.Log($"[NotebookManager] Adding suspect to left page {targetPageIndex}");
            suspectsPage.leftPageNotes.Add(suspectEntry);
            suspectsPage.leftPageNoteCount++;
            suspectEntry.transform.SetParent(leftPageParent, false);
        }
        else if (suspectsPage.rightPageNoteCount < maxSuspectsPerPage)
        {
            Debug.Log($"[NotebookManager] Adding suspect to right page {targetPageIndex}");
            suspectsPage.rightPageNotes.Add(suspectEntry);
            suspectsPage.rightPageNoteCount++;
            suspectEntry.transform.SetParent(rightPageParent, false);
        }
        else
        {
            Debug.LogWarning("[NotebookManager] Something went wrong - page should have space!");
            Destroy(suspectEntry);
            return;
        }
        
        suspectEntries.Add(suspectEntry);
        
        Debug.Log($"[NotebookManager] Added suspect entry to page {targetPageIndex}. Left: {suspectsPage.leftPageNoteCount}, Right: {suspectsPage.rightPageNoteCount}, Total: {suspectEntries.Count}");
            
        UpdateTabCounts();
        UpdatePageDisplay();
    }
    
    /// <summary>
    /// Switch notebook mode between suspects and clues
    /// </summary>
    public void SwitchToMode(NotebookMode mode)
    {
        if (currentMode == mode) return;
        
        // Cancel any active drag operations before switching modes
        DragManager.Instance.CancelAllActiveDrags();
        
        currentMode = mode;
        
        // Find first page of requested type
        int targetPage = 0;
        if (mode == NotebookMode.Suspects)
        {
            // Find first suspects page (should be 0)
            targetPage = 0;
        }
        else
        {
            // Find first clues page
            for (int i = 0; i < bookPages.Count; i++)
            {
                if (bookPages[i].pageType == PageType.Clues)
                {
                    targetPage = i;
                    break;
                }
            }
            
            // Create clues page if none exists
            if (targetPage == 0)
            {
                EnsureCluesPagesExist();
                targetPage = lastCluesPageIndex;
            }
        }
        
        // Navigate to target page
        currentBookPageIndex = targetPage;
        UpdateTabVisuals();
        UpdatePageDisplay();
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Switched to {mode} mode, navigated to page {targetPage}");
    }

    /// <summary>
    /// Refresh page headers (useful when case information changes)
    /// </summary>
    public void RefreshPageHeaders()
    {
        UpdatePageHeaders();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Add clue note with animation routine
    /// </summary>
    private IEnumerator AddClueNoteRoutine(string clueText, string clueId, bool autoClose)
    {
        OpenNotebook();

        // Ensure we have clues pages
        EnsureCluesPagesExist();
        
        // Find or create a page that can accommodate this clue
        int targetPageIndex = FindOrCreateCluesPage();
        
        // Create the note object
        GameObject noteObj = null;
        yield return CreateNoteObject(clueText, clueId, obj => noteObj = obj);
        if (noteObj == null) yield break;
        
        // Add note to the appropriate page and side
        AddNoteToBookPage(noteObj, targetPageIndex);
        
        // Update last clues page index
        lastCluesPageIndex = targetPageIndex;
        
        // Switch to clues mode and navigate to the target page
        if (currentMode != NotebookMode.Clues || currentBookPageIndex != targetPageIndex)
        {
            currentMode = NotebookMode.Clues;
            currentBookPageIndex = targetPageIndex;
            UpdateTabVisuals();
            UpdatePageDisplay();
            
            if (debugLayoutInfo)
                Debug.Log($"[NotebookManager] Switched to clues mode and navigated to page {targetPageIndex}");
        }

        // Get the TextMeshPro component
        TextMeshProUGUI targetTMP = noteObj.GetComponentInChildren<TextMeshProUGUI>();
        if (targetTMP == null)
        {
            Debug.LogError("[NotebookManager] ClueNotePrefab doesn't have a TextMeshProUGUI component in its children!");
            Destroy(noteObj);
            yield break;
        }

        // Process the text and get original color
        string processedText = textProcessor != null ? textProcessor.ProcessTagsInText(clueText) : clueText;
        Color originalColor = targetTMP.color;

        // Make text invisible for animation
        targetTMP.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        targetTMP.text = processedText;
        targetTMP.ForceMeshUpdate();

        // Calculate proper height and add to paging system
        yield return StartCoroutine(CalculateAndSetProperHeight(noteObj, processedText));
        
        // Track this clue
        ClueNoteInfo clueInfo = new ClueNoteInfo(clueId, noteObj, targetPageIndex, clueText);
        addedClues[clueId] = clueInfo;

        // Animate the text
        if (handwritingAnimator != null)
        {
            yield return StartCoroutine(handwritingAnimator.AnimateText(
                processedText, 
                targetTMP, 
                originalColor,
                (pos) => textProcessor?.GetTagAtPosition(pos) != null,
                (pos) => {
                    var tag = textProcessor?.GetTagEndingAtPosition(pos);
                    if (tag != null)
                    {
                        StartCoroutine(textProcessor.ReplaceTagWithPrefab(
                            tag, originalColor, targetTMP, handwritingAnimator.HideTextCharacters, clueId)); // Pass clueId
                    }
                }
            ));
        }
        else
        {
            // Fallback: just show the text immediately
            targetTMP.color = originalColor;
        }

        yield return new WaitForSeconds(0.5f);
        
        if (autoClose)
            CloseNotebook();
    }

    /// <summary>
    /// Create note object with proper configuration
    /// </summary>
    private IEnumerator CreateNoteObject(string clueText, string clueId, System.Action<GameObject> onCreated)
    {
        if (clueNotePrefab == null)
        {
            Debug.LogError("[NotebookManager] ClueNotePrefab is not assigned!");
            yield return null;
        }

        GameObject noteObj = Instantiate(clueNotePrefab);
        
        // Configure RectTransform for layout
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        if (noteRect != null)
        {
            noteRect.anchorMin = new Vector2(0, 1);
            noteRect.anchorMax = new Vector2(1, 1);
            noteRect.pivot = new Vector2(0.5f, 1);
            noteRect.anchoredPosition = Vector2.zero;
            noteRect.offsetMin = new Vector2(0, noteRect.offsetMin.y);
            noteRect.offsetMax = new Vector2(0, noteRect.offsetMax.y);
        }
        
        // Remove ContentSizeFitter since we're manually calculating height
        ContentSizeFitter sizeFitter = noteObj.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            DestroyImmediate(sizeFitter);
        }
        
        // Add LayoutElement for better layout control
        LayoutElement layoutElement = noteObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = noteObj.AddComponent<LayoutElement>();
        }
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = minimumNoteHeight;
        
        // Layout configuration is now handled in UpdatePageDisplay()

        // Configure text properties
        TextMeshProUGUI targetTMP = noteObj.GetComponentInChildren<TextMeshProUGUI>();
        if (targetTMP != null)
        {
            targetTMP.fontSize = fontSize;
            if (useCaretColorForText && handwritingAnimator != null)
            {
                targetTMP.color = handwritingAnimator.caretColor;
            }
            
            targetTMP.textWrappingMode = TextWrappingModes.PreserveWhitespace;
            targetTMP.overflowMode = TextOverflowModes.Overflow;
            
            RectTransform textRect = targetTMP.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
        }

        yield return noteObj;
        onCreated(noteObj);
    }

    /// <summary>
    /// Calculate and set proper height for note object
    /// </summary>
    private IEnumerator CalculateAndSetProperHeight(GameObject noteObj, string textContent)
    {
        RectTransform noteRect = noteObj.GetComponent<RectTransform>();
        TextMeshProUGUI targetTMP = noteObj.GetComponentInChildren<TextMeshProUGUI>();
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Initial note size: {noteRect.sizeDelta}");
        
        // Wait a frame for the text to be processed
        yield return null;
        
        // Calculate the preferred height
        float preferredHeight = LayoutUtility.GetPreferredHeight(targetTMP.rectTransform);
        float finalHeight = Mathf.Max(preferredHeight, minimumNoteHeight);
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Text preferred height: {preferredHeight}");
        
        // Set the height on the LayoutElement
        LayoutElement layoutElement = noteObj.GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.preferredHeight = finalHeight;
        }
        
        // Also set the RectTransform height as fallback
        noteRect.sizeDelta = new Vector2(noteRect.sizeDelta.x, finalHeight);
        
        // Force layout rebuild
        if (leftPageParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(leftPageParent);
        }
        if (rightPageParent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rightPageParent);
        }
        
        yield return null;
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Final note height set to: {finalHeight}");
    }

    /// <summary>
    /// Initialize book pages
    /// </summary>
    private void InitializeBookPages()
    {
        bookPages.Clear();
        
        // Create the first page for suspects
        bookPages.Add(new BookPage(PageType.Suspects));
        currentBookPageIndex = 0;
        
        if (debugLayoutInfo)
            Debug.Log("[NotebookManager] Initialized book pages with suspects page");
    }
    
    /// <summary>
    /// Initialize tab management
    /// </summary>
    private void InitializeTabManagement()
    {
        // Set up tab button events
        if (suspectsTab != null)
            suspectsTab.onClick.AddListener(() => SwitchToMode(NotebookMode.Suspects));
            
        if (cluesTab != null)
            cluesTab.onClick.AddListener(() => SwitchToMode(NotebookMode.Clues));
        
        // Initialize tab visuals
        UpdateTabVisuals();
        UpdateTabCounts();
    }
    
    /// <summary>
    /// Initialize page navigation
    /// </summary>
    private void InitializePageNavigation()
    {
        if (previousPageButton != null)
            previousPageButton.onClick.AddListener(PreviousPage);
            
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
            
        UpdatePageNavigation();
    }
    
    /// <summary>
    /// Initialize layout configuration
    /// </summary>
    private void InitializeLayoutConfiguration()
    {
        // Apply initial layout configuration for suspects (default page)
        ApplyLayoutConfiguration(PageType.Suspects);
        
        if (debugLayoutInfo)
            Debug.Log("[NotebookManager] Initialized layout configuration");
    }
    
    /// <summary>
    /// Ensure clues pages exist
    /// </summary>
    private void EnsureCluesPagesExist()
    {
        // If we only have the suspects page, create the first clues page
        if (bookPages.Count == 1 && bookPages[0].pageType == PageType.Suspects)
        {
            bookPages.Add(new BookPage(PageType.Clues));
            lastCluesPageIndex = 1;
            
            if (debugLayoutInfo)
                Debug.Log("[NotebookManager] Created first clues page");
        }
    }
    
    /// <summary>
    /// Find or create clues page
    /// </summary>
    private int FindOrCreateCluesPage()
    {
        // Start searching from the last clues page
        for (int i = lastCluesPageIndex; i < bookPages.Count; i++)
        {
            if (bookPages[i].pageType == PageType.Clues && bookPages[i].CanAddNote(maxNotesPerPage))
            {
                return i;
            }
        }
        
        // No existing page can accommodate the note, create a new one
        bookPages.Add(new BookPage(PageType.Clues));
        return bookPages.Count - 1;
    }
    
    /// <summary>
    /// Add note to book page
    /// </summary>
    private void AddNoteToBookPage(GameObject noteObj, int pageIndex)
    {
        if (pageIndex < 0 || pageIndex >= bookPages.Count) return;
        
        var page = bookPages[pageIndex];
        
        // Add to left page first, then right page
        if (page.ShouldAddToLeftPage(maxNotesPerPage))
        {
            page.leftPageNotes.Add(noteObj);
            page.leftPageNoteCount++;
            noteObj.transform.SetParent(leftPageParent, false);
        }
        else if (page.rightPageNoteCount < maxNotesPerPage)
        {
            page.rightPageNotes.Add(noteObj);
            page.rightPageNoteCount++;
            noteObj.transform.SetParent(rightPageParent, false);
        }
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Added note to page {pageIndex}. Left: {page.leftPageNoteCount}, Right: {page.rightPageNoteCount}");
        
        UpdateTabCounts();
    }
    
    /// <summary>
    /// Update the display of the current page
    /// </summary>
    private void UpdatePageDisplay()
    {
        if (bookPages.Count == 0) return;
        
        // Clear current display
        ClearCurrentPageDisplay();
        
        // Get current page
        var currentPage = bookPages[currentBookPageIndex];
        
        // Apply appropriate layout configuration
        ApplyLayoutConfiguration(currentPage.pageType);
        
        // Display notes for current page
        DisplayPageNotes(currentPage);
        
        // Update navigation
        UpdatePageNavigation();
        
        // Update page headers
        UpdatePageHeaders();
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Updated page display for page {currentBookPageIndex} ({currentPage.pageType})");
    }
    
    /// <summary>
    /// Clear the current page display
    /// </summary>
    private void ClearCurrentPageDisplay()
    {
        // Hide all notes by setting them inactive
        foreach (Transform child in leftPageParent)
        {
            child.gameObject.SetActive(false);
        }
        
        foreach (Transform child in rightPageParent)
        {
            child.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Display notes for the specified page
    /// </summary>
    private void DisplayPageNotes(BookPage page)
    {
        // Display left page notes
        for (int i = 0; i < page.leftPageNotes.Count; i++)
        {
            var note = page.leftPageNotes[i];
            if (note != null)
            {
                note.SetActive(true);
                // Ensure it's parented to the correct side
                if (note.transform.parent != leftPageParent)
                    note.transform.SetParent(leftPageParent, false);
            }
        }
        
        // Display right page notes
        for (int i = 0; i < page.rightPageNotes.Count; i++)
        {
            var note = page.rightPageNotes[i];
            if (note != null)
            {
                note.SetActive(true);
                // Ensure it's parented to the correct side
                if (note.transform.parent != rightPageParent)
                    note.transform.SetParent(rightPageParent, false);
            }
        }
    }
    
    /// <summary>
    /// Apply layout configuration based on content type
    /// </summary>
    private void ApplyLayoutConfiguration(PageType pageType)
    {
        LayoutConfig config = pageType == PageType.Suspects ? suspectsLayoutConfig : cluesLayoutConfig;
        
        // Apply to left page
        ApplyLayoutToParent(leftPageParent, config);
        
        // Apply to right page
        ApplyLayoutToParent(rightPageParent, config);
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Applied {pageType} layout configuration");
    }
    
    /// <summary>
    /// Apply layout configuration to a specific parent
    /// </summary>
    private void ApplyLayoutToParent(RectTransform parent, LayoutConfig config)
    {
        // Get or add VerticalLayoutGroup
        VerticalLayoutGroup vlg = parent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        
        // Apply spacing
        vlg.spacing = config.spacing;
        
        // Apply padding
        vlg.padding.top = Mathf.RoundToInt(config.topPadding);
        vlg.padding.bottom = Mathf.RoundToInt(config.bottomPadding);
        vlg.padding.left = Mathf.RoundToInt(config.leftPadding);
        vlg.padding.right = Mathf.RoundToInt(config.rightPadding);
        
        // Apply child control settings
        vlg.childForceExpandWidth = config.childForceExpandWidth;
        vlg.childForceExpandHeight = config.childForceExpandHeight;
        vlg.childControlWidth = config.childControlWidth;
        vlg.childControlHeight = config.childControlHeight;
        
        // Apply alignment
        vlg.childAlignment = TextAnchor.UpperCenter;
        
        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
    }
    
    /// <summary>
    /// Update tab visuals
    /// </summary>
    private void UpdateTabVisuals()
    {
        if (suspectsTab != null)
        {
            bool isActive = currentMode == NotebookMode.Suspects;
            UpdateTabAppearance(suspectsTab, suspectsTabText, isActive);
        }
        
        if (cluesTab != null)
        {
            bool isActive = currentMode == NotebookMode.Clues;
            UpdateTabAppearance(cluesTab, cluesTabText, isActive);
        }
    }
    
    /// <summary>
    /// Update individual tab appearance
    /// </summary>
    private void UpdateTabAppearance(Button tab, TextMeshProUGUI tabText, bool isActive)
    {
        if (tab == null) return;
        
        // Update colors
        Image tabImage = tab.GetComponent<Image>();
        if (tabImage != null)
        {
            tabImage.DOColor(isActive ? activeTabColor : inactiveTabColor, tabSwitchDuration);
        }
        
        // Update text color
        if (tabText != null)
        {
            tabText.DOColor(isActive ? activeTextColor : inactiveTextColor, tabSwitchDuration);
        }
        
        // Update scale
        float targetScale = isActive ? tabScaleOnActive : tabScaleOnInactive;
        tab.transform.DOScale(targetScale, tabSwitchDuration);
    }
    
    /// <summary>
    /// Update tab counts/badges
    /// </summary>
    private void UpdateTabCounts()
    {
        // Update suspects count
        if (suspectsCountText != null)
        {
            int suspectCount = suspectEntries.Count;
            int completedCount = suspectsManager != null ? suspectsManager.GetCompletedSuspectCount() : 0;
            
            suspectsCountText.text = completedCount > 0 ? $"{completedCount}/{suspectCount}" : suspectCount.ToString();
            
            if (suspectsTabBadge != null)
                suspectsTabBadge.SetActive(suspectCount > 0);
        }
        
        // Update clues count
        if (cluesCountText != null)
        {
            int clueCount = addedClues.Count;
            cluesCountText.text = clueCount.ToString();
            
            if (cluesTabBadge != null)
                cluesTabBadge.SetActive(clueCount > 0);
        }
    }
    
    /// <summary>
    /// Update page navigation buttons and indicator
    /// </summary>
    private void UpdatePageNavigation()
    {
        // Enable/disable navigation buttons based on total pages
        if (previousPageButton != null)
            previousPageButton.interactable = currentBookPageIndex > 0;
            
        if (nextPageButton != null)
            nextPageButton.interactable = currentBookPageIndex < bookPages.Count - 1;
            
        if (pageIndicatorText != null)
        {
            // Get current page type
            string pageTypeName = bookPages[currentBookPageIndex].pageType.ToString();
            
            // Count total pages of current type
            int typePageCount = 0;
            int currentTypePageNumber = 1;
            
            for (int i = 0; i < bookPages.Count; i++)
            {
                if (bookPages[i].pageType == bookPages[currentBookPageIndex].pageType)
                {
                    typePageCount++;
                    if (i < currentBookPageIndex)
                        currentTypePageNumber++;
                }
            }
            
            pageIndicatorText.text = $"{pageTypeName} Page {currentTypePageNumber} of {typePageCount}";
        }
        
        // Update current mode based on current page type
        NotebookMode newMode = bookPages[currentBookPageIndex].pageType == PageType.Suspects ? 
            NotebookMode.Suspects : NotebookMode.Clues;
            
        if (currentMode != newMode)
        {
            currentMode = newMode;
            UpdateTabVisuals();
        }
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Updated navigation. Page {currentBookPageIndex} ({bookPages[currentBookPageIndex].pageType})");
    }
    
    /// <summary>
    /// Navigate to previous page
    /// </summary>
    private void PreviousPage()
    {
        if (currentBookPageIndex > 0)
        {
            // Cancel any active drag operations before page navigation
            DragManager.Instance.CancelAllActiveDrags();
            
            currentBookPageIndex--;
            UpdatePageDisplay();
            UpdatePageNavigation();
            
            if (debugLayoutInfo)
                Debug.Log($"[NotebookManager] Navigated to previous page: {currentBookPageIndex}");
        }
    }
    
    /// <summary>
    /// Navigate to next page
    /// </summary>
    private void NextPage()
    {
        if (currentBookPageIndex < bookPages.Count - 1)
        {
            // Cancel any active drag operations before page navigation
            DragManager.Instance.CancelAllActiveDrags();
            
            currentBookPageIndex++;
            UpdatePageDisplay();
            UpdatePageNavigation();
            
            if (debugLayoutInfo)
                Debug.Log($"[NotebookManager] Navigated to next page: {currentBookPageIndex}");
        }
    }
    
    /// <summary>
    /// Clear all book pages
    /// </summary>
    private void ClearAllBookPages()
    {
        // Destroy all note objects
        foreach (var page in bookPages)
        {
            foreach (var note in page.leftPageNotes)
            {
                if (note != null) Destroy(note);
            }
            foreach (var note in page.rightPageNotes)
            {
                if (note != null) Destroy(note);
            }
        }
        
        // Reinitialize pages
        InitializeBookPages();
    }
    
    /// <summary>
    /// Clear suspect entries
    /// </summary>
    private void ClearSuspectEntries()
    {
        foreach (var entry in suspectEntries)
        {
            if (entry != null) Destroy(entry);
        }
        
        suspectEntries.Clear();
        UpdateTabCounts();
    }

    /// <summary>
    /// Highlights an existing clue in the notebook
    /// </summary>
    private IEnumerator HighlightExistingClue(string clueId, bool autoClose = true)
    {
        if (!addedClues.ContainsKey(clueId))
        {
            Debug.LogWarning($"[NotebookManager] Attempted to highlight non-existent clue: {clueId}");
            yield break;
        }

        ClueNoteInfo clueInfo = addedClues[clueId];
        
        // Navigate to the page containing this clue
        if (clueInfo.pageIndex != currentBookPageIndex)
        {
            currentBookPageIndex = clueInfo.pageIndex;
            UpdatePageDisplay();
        }
        
        // Switch to clues mode if not already there
        if (currentMode != NotebookMode.Clues)
        {
            SwitchToMode(NotebookMode.Clues);
        }

        // Open notebook to show the highlight
        OpenNotebook();
        
        // Wait a moment for notebook to open and page to display
        yield return new WaitForSeconds(0.5f);
        
        // Highlight the clue using the highlighter
        if (highlighter != null && clueInfo.noteObject != null)
        {
            if (highlighter.HasHighlight(clueInfo.noteObject))
            {
                highlighter.RefreshHighlight(clueInfo.noteObject);
            }
            else
            {
                highlighter.CreateHighlightOnNote(clueInfo.noteObject);
            }
        }
        
        // Auto-close after highlighting if requested
        if (autoClose)
        {
            yield return new WaitForSeconds(2f);
            CloseNotebook();
        }
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Highlighted existing clue: {clueId}");
    }

    /// <summary>
    /// Find or create a suspects page that can accommodate a new entry
    /// </summary>
    private int FindOrCreateSuspectsPage()
    {
        // First ensure we have at least one suspects page
        if (bookPages.Count == 0 || bookPages[0].pageType != PageType.Suspects)
        {
            Debug.Log("[NotebookManager] Initializing first suspects page");
            InitializeBookPages();
        }
        
        // Look for a suspects page with space (max 8 suspects per page: 4 left + 4 right)
        for (int i = 0; i < bookPages.Count; i++)
        {
            if (bookPages[i].pageType == PageType.Suspects && bookPages[i].CanAddNote(maxSuspectsPerPage))
            {
                return i;
            }
        }
        
        // No existing page has space, create a new suspects page
        int newPageIndex = 0;
        // Find where suspects pages end
        for (int i = 0; i < bookPages.Count; i++)
        {
            if (bookPages[i].pageType == PageType.Clues)
            {
                newPageIndex = i;
                break;
            }
            newPageIndex = i + 1;
        }
        
        // Insert new suspects page
        bookPages.Insert(newPageIndex, new BookPage(PageType.Suspects));
        Debug.Log($"[NotebookManager] Created new suspects page at index {newPageIndex}");
        
        return newPageIndex;
    }

    /// <summary>
    /// Handle suspect clues with special discovery behavior
    /// </summary>
    private IEnumerator AddSuspectClueRoutine(string clueText, string clueId, bool autoClose)
    {
        // Step 1: Cancel any active drag operations before page flip
        DragManager.Instance.CancelAllActiveDrags();
        
        // Step 2: Add the clue to notebook first (with suspect tags)
        OpenNotebook();
        
        // Process suspect tags - convert nested tags to individual tags and remove portrait tags
        string processedClueText = SuspectTagParser.ProcessSuspectTags(clueText);
        
        // Only add the clue if it has visible content after processing
        if (!string.IsNullOrWhiteSpace(processedClueText.Trim()))
        {
            // Add the clue note normally (this will show the tags)
            yield return StartCoroutine(AddClueNoteRoutine(processedClueText, clueId, false));
        }
        
        // Step 3: Switch to suspects page
        yield return new WaitForSeconds(0.5f); // Brief pause after clue animation
        
        SwitchToMode(NotebookMode.Suspects);
        yield return new WaitForSeconds(0.3f); // Wait for page switch animation
        
        // Step 4: Process suspect discovery
        yield return StartCoroutine(ProcessSuspectDiscovery(clueText));
        
        // Step 5: Auto-close if requested
        if (autoClose)
        {
            yield return new WaitForSeconds(1.5f);
            CloseNotebook();
        }
        
        if (debugLayoutInfo)
            Debug.Log($"[NotebookManager] Completed suspect clue routine for: {clueId}");
    }
    
    /// <summary>
    /// Process suspect discovery from clue text
    /// </summary>
    private IEnumerator ProcessSuspectDiscovery(string originalClueText)
    {
        var suspectGroups = SuspectTagParser.ExtractGroupedSuspectInformation(originalClueText);

        if (suspectGroups.Count == 0)
            yield break;

        foreach (var suspectData in suspectGroups)
        {
            yield return StartCoroutine(DiscoverSuspectInformation(suspectData));
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// Discover information about a suspect
    /// </summary>
    private IEnumerator DiscoverSuspectInformation(List<SuspectsListManager.SuspectDiscoveryInfo> suspectData)
    {
        var suspectsManager = SuspectsListManager.Instance;
        if (suspectsManager == null)
        {
            Debug.LogError("[NotebookManager] SuspectsListManager not found!");
            yield break;
        }

        suspectsManager.DiscoverGroupedSuspectInfo(suspectData);
        yield return new WaitForSeconds(1.0f);
    }

    /// <summary>
    /// Update page headers based on current page type and case
    /// </summary>
    private void UpdatePageHeaders()
    {
        if (bookPages.Count == 0) return;
        
        var currentPage = bookPages[currentBookPageIndex];
        string headerText = "";
        string subHeaderText = "";
        
        switch (currentPage.pageType)
        {
            case PageType.Suspects:
                headerText = "Suspects";
                // Get current case name from GameManager or CaseManager
                string caseName = GetCurrentCaseName();
                subHeaderText = $"Case: {caseName}";
                break;
                
            case PageType.Clues:
                headerText = "Clues";
                subHeaderText = "Leads gathered so far:";
                break;
        }
        
        // Update left page headers
        if (leftPageHeaderText != null)
            leftPageHeaderText.text = headerText;
        if (leftPageSubHeaderText != null)
            leftPageSubHeaderText.text = subHeaderText;
            
        // Update right page headers (same content for now, could be different in future)
        if (rightPageHeaderText != null)
            rightPageHeaderText.text = headerText;
        if (rightPageSubHeaderText != null)
            rightPageSubHeaderText.text = subHeaderText;
    }
    
    /// <summary>
    /// Get the current case name
    /// </summary>
    private string GetCurrentCaseName()
    {
        // Try to get from SuspectManager first
        if (SuspectManager.Instance != null)
        {
            var currentCase = SuspectManager.Instance.GetCurrentCase();
            if (currentCase != null && !string.IsNullOrEmpty(currentCase.title))
            {
                return currentCase.title;
            }
        }
        
        // Fallback to GameManager
        if (GameManager.Instance != null && GameManager.Instance.CurrentCase != null)
        {
            return GameManager.Instance.CurrentCase.title;
        }
        
        // Default fallback
        return "Unknown Case";
    }

    #endregion
} 