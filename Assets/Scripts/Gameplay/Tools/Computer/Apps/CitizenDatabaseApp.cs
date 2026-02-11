using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Citizen Database app for searching and viewing citizen records
/// </summary>
public class CitizenDatabaseApp : ComputerApp
{
    [Header("UI Panels")]
    [SerializeField] private GameObject searchPanel;
    [SerializeField] private GameObject multipleResultsPanel;
    [SerializeField] private GameObject resultsPanel;
    
    [Header("Search Fields")]
    [SerializeField] private CitizenDatabaseDropZone firstNameDropZone;
    [SerializeField] private CitizenDatabaseDropZone lastNameDropZone;
    [SerializeField] private CitizenDatabaseDropZone citizenIdDropZone;
    [SerializeField] private CitizenDatabaseDropZone facialRecognitionDropZone;
    [SerializeField] private CitizenDatabaseDropZone biometricsDropZone;
    
    [Header("Search Controls")]
    [SerializeField] private Button searchButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button backButton;
    
    [Header("Progress Bar")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text progressText;
    
    [Header("Multiple Results Display")]
    [SerializeField] private TMP_Text resultsCountText;
    [SerializeField] private Transform citizenListContainer;
    [SerializeField] private GameObject citizenListItemPrefab;
    [SerializeField] private Button backFromResultsButton;
    
    [Header("Results Display")]
    [SerializeField] private GameObject detailsGroup;
    [SerializeField] private TMP_Text noResultsText;
    [SerializeField] private Image citizenPortrait;
    [SerializeField] private TMP_Text citizenNameText;
    [SerializeField] private TMP_Text citizenIdText;
    [SerializeField] private TMP_Text citizenAddressText;
    [SerializeField] private TMP_Text citizenOccupationText;
    [SerializeField] private TMP_Text citizenDobText;
    [SerializeField] private TMP_Text citizenCriminalHistoryText;

    [Header("Add to Suspects")]
    [SerializeField] private Button addToSuspectsButton;
    [SerializeField] private TMP_Text addToSuspectsButtonText;
    [SerializeField] private Color buttonNormalColor = Color.white;
    [SerializeField] private Color buttonHighlightColor = Color.yellow;
    [SerializeField] private float highlightDuration = 1f;
    
    [Header("Fingerprint Display")]
    [SerializeField] private Image fingerprint1Image;
    [SerializeField] private Image fingerprint2Image;
    [SerializeField] private Image fingerprint3Image;
    [SerializeField] private Image fingerprint4Image;
    [SerializeField] private Image fingerprint5Image;
    
    [Header("Settings")]
    [SerializeField] private float searchDuration = 2f;
    [SerializeField] private float progressUpdateInterval = 0.1f;
    
    // Search data
    private Dictionary<string, string> searchCriteria = new Dictionary<string, string>();
    private List<DatabaseCitizen> searchResults = new List<DatabaseCitizen>();
    private DatabaseCitizen currentCitizen;
    
    // Navigation state
    private bool cameFromMultipleResults = false;
    
    // Property to track flag changes with logging
    private bool CameFromMultipleResults
    {
        get { return cameFromMultipleResults; }
        set 
        { 
            if (cameFromMultipleResults != value)
            {
                Debug.Log($"[CitizenDatabaseApp] cameFromMultipleResults changed from {cameFromMultipleResults} to {value}");
                cameFromMultipleResults = value;
            }
        }
    }
    

    
    // Components
    private CitizenDatabaseManager databaseManager;
    private RetroComputerEffects retroEffects;
    
    public override void Initialize(AppConfig appConfig)
    {
        base.Initialize(appConfig);
        
        // Try to find the database manager
        FindDatabaseManager();
        retroEffects = RetroComputerEffects.Instance;
        
        // Button events will be manually wired through inspector
        
        // Initialize drop zones FIRST
        InitializeDropZones();
        
        // Show search panel initially
        GotoSearchPanel();
        
        // Set initial button states (disabled)
        UpdateButtonStates();
        
        // Initialize results panel state
        InitializeResultsPanelState();
        
        // Force UI update to ensure proper rendering
        ForceUIUpdate();
    }
    
    private void FindDatabaseManager()
    {
        // Try multiple methods to find the database manager
        databaseManager = FindFirstObjectByType<CitizenDatabaseManager>();
        
        
        if (databaseManager == null)
        {
            // Try finding in parent objects
            var parent = transform.parent;
            while (parent != null && databaseManager == null)
            {
                databaseManager = parent.GetComponent<CitizenDatabaseManager>();
                if (databaseManager == null)
                {
                    databaseManager = parent.GetComponentInChildren<CitizenDatabaseManager>();
                }
                parent = parent.parent;
            }
            
            if (databaseManager != null)
            {
                Debug.Log($"[CitizenDatabaseApp] Found CitizenDatabaseManager in parent hierarchy: {databaseManager.name}");
            }
        }
        
        if (databaseManager == null)
        {
            Debug.LogError("[CitizenDatabaseApp] Could not find CitizenDatabaseManager! Please ensure one exists in the scene.");
        }
        else
        {
            Debug.Log($"[CitizenDatabaseApp] Successfully found CitizenDatabaseManager: {databaseManager.name}");
        }
    }
    
    private void InitializeResultsPanelState()
    {
        // Set initial state: hide details, show no results text
        if (detailsGroup != null)
        {
            detailsGroup.SetActive(false);
        }
        
        if (noResultsText != null)
        {
            noResultsText.gameObject.SetActive(true);
        }
    }
    
    private void ValidateUIReferences()
    {
        if ((searchPanel == null || searchButton == null || clearButton == null) && 
            gameObject.activeInHierarchy && 
            Time.time > 1.0f)
        {
            TryFindMissingUIComponents();
        }
    }
    

    
    /// <summary>
    /// Force UI update to ensure proper rendering of all elements
    /// </summary>
    private void ForceUIUpdate()
    {
        Debug.Log("[CitizenDatabaseApp] Forcing UI update for proper rendering");
        
        // Force canvas update
        Canvas.ForceUpdateCanvases();
        
        // Force layout rebuild on all panels
        if (searchPanel != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(searchPanel.GetComponent<RectTransform>());
        }
        
        if (multipleResultsPanel != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(multipleResultsPanel.GetComponent<RectTransform>());
        }
        
        if (resultsPanel != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(resultsPanel.GetComponent<RectTransform>());
        }
        
        // Force update on all drop zones to ensure they're visible
        var dropZones = new CitizenDatabaseDropZone[] 
        {
            firstNameDropZone, lastNameDropZone, citizenIdDropZone, 
            facialRecognitionDropZone, biometricsDropZone
        };
        
        foreach (var dropZone in dropZones)
        {
            if (dropZone != null)
            {
                dropZone.gameObject.SetActive(false);
                dropZone.gameObject.SetActive(true);
                
                // Force layout update on the drop zone
                var rectTransform = dropZone.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
                }
            }
        }
        
        Debug.Log("[CitizenDatabaseApp] UI update completed");
    }
    
    private void InitializeDropZones()
    {
        Debug.Log("[CitizenDatabaseApp] Initializing drop zones");
        
        // Try to find drop zones if not assigned
        if (firstNameDropZone == null) firstNameDropZone = FindDropZoneByName("firstName", "First Name");
        if (lastNameDropZone == null) lastNameDropZone = FindDropZoneByName("lastName", "Last Name");
        if (citizenIdDropZone == null) citizenIdDropZone = FindDropZoneByName("citizenId", "Citizen ID");
        if (facialRecognitionDropZone == null) facialRecognitionDropZone = FindDropZoneByName("facialRecognition", "Facial Recognition");
        if (biometricsDropZone == null) biometricsDropZone = FindDropZoneByName("biometrics", "Biometrics");
        
        // Initialize drop zones
        if (firstNameDropZone != null)
        {
            firstNameDropZone.Initialize("firstName", "First Name");
            Debug.Log("[CitizenDatabaseApp] Initialized firstNameDropZone");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] firstNameDropZone is null!");
        
        if (lastNameDropZone != null)
        {
            lastNameDropZone.Initialize("lastName", "Last Name");
            Debug.Log("[CitizenDatabaseApp] Initialized lastNameDropZone");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] lastNameDropZone is null!");
        
        if (citizenIdDropZone != null)
        {
            citizenIdDropZone.Initialize("citizenId", "Citizen ID");
            Debug.Log("[CitizenDatabaseApp] Initialized citizenIdDropZone");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] citizenIdDropZone is null!");
        
        if (facialRecognitionDropZone != null)
        {
            facialRecognitionDropZone.Initialize("facialRecognition", "Facial Recognition");
            // Configure facial recognition to only accept photos
            facialRecognitionDropZone.SetAcceptedFileTypes(new FileType[] { FileType.Photo });
            Debug.Log("[CitizenDatabaseApp] Initialized facialRecognitionDropZone (Photo only)");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] facialRecognitionDropZone is null!");
        
        if (biometricsDropZone != null)
        {
            biometricsDropZone.Initialize("biometrics", "Biometrics");
            // Configure biometrics to only accept fingerprint scans
            biometricsDropZone.SetAcceptedFileTypes(new FileType[] { FileType.FingerprintScan });
            Debug.Log("[CitizenDatabaseApp] Initialized biometricsDropZone (FingerprintScan only)");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] biometricsDropZone is null!");
        
        // Wire up file drop events for facial recognition and biometrics
        if (facialRecognitionDropZone != null)
        {
            facialRecognitionDropZone.OnFileDroppedEvent += OnFacialRecognitionFileDropped;
        }
        
        if (biometricsDropZone != null)
        {
            biometricsDropZone.OnFileDroppedEvent += OnBiometricsFileDropped;
        }
        
        // Initialize AddToSuspects button
        InitializeAddToSuspectsButton();
        
        Debug.Log("[CitizenDatabaseApp] Drop zone initialization complete");
    }
    
    /// <summary>
    /// Initialize the Add to Suspects button
    /// </summary>
    private void InitializeAddToSuspectsButton()
    {
        if (addToSuspectsButton != null)
        {
            // Set initial text (will be updated dynamically when citizen is displayed)
            if (addToSuspectsButtonText != null)
            {
                addToSuspectsButtonText.text = "Add To Suspects List";
            }
            
            // Initially hide the button (only show when displaying results)
            addToSuspectsButton.gameObject.SetActive(false);
            
            Debug.Log("[CitizenDatabaseApp] Initialized AddToSuspects button");
        }
        else
        {
            Debug.LogWarning("[CitizenDatabaseApp] addToSuspectsButton is null!");
        }
    }
    
    /// <summary>
    /// Find drop zone by name pattern
    /// </summary>
    private CitizenDatabaseDropZone FindDropZoneByName(string type, string label)
    {
        var dropZones = GetComponentsInChildren<CitizenDatabaseDropZone>(true);
        foreach (var dropZone in dropZones)
        {
            string name = dropZone.name.ToLower();
            if (name.Contains(type.ToLower()) || name.Contains(label.ToLower().Replace(" ", "")))
            {
                Debug.Log($"[CitizenDatabaseApp] Found drop zone for {type}: {dropZone.name}");
                return dropZone;
            }
        }
        Debug.LogWarning($"[CitizenDatabaseApp] Could not find drop zone for {type}");
        return null;
    }
    
    /// <summary>
    /// Handle file dropped on facial recognition drop zone
    /// </summary>
    private void OnFacialRecognitionFileDropped(string fieldType, DiscFile file)
    {
        Debug.Log($"[CitizenDatabaseApp] Facial recognition file dropped: {file.fileName} (Type: {file.fileType})");
        if (!string.IsNullOrEmpty(file.associatedCitizenId))
        {
            Debug.Log($"[CitizenDatabaseApp] File associated with citizen: {file.associatedCitizenId}");
        }
        
        // Force button state update
        UpdateButtonStates();
        
        // Also notify that content changed
        OnDropZoneContentChanged();
    }
    
    /// <summary>
    /// Handle file dropped on biometrics drop zone
    /// </summary>
    private void OnBiometricsFileDropped(string fieldType, DiscFile file)
    {
        Debug.Log($"[CitizenDatabaseApp] Biometrics file dropped: {file.fileName} (Type: {file.fileType})");
        if (!string.IsNullOrEmpty(file.associatedCitizenId))
        {
            Debug.Log($"[CitizenDatabaseApp] File associated with citizen: {file.associatedCitizenId}");
        }
        
        // Force button state update
        UpdateButtonStates();
        
        // Also notify that content changed
        OnDropZoneContentChanged();
    }
    
    private void UpdateButtonStates()
    {
        Debug.Log("[CitizenDatabaseApp] Updating button states...");
        
        // Check if any drop zones have content
        bool hasContent = false;
        
        if (firstNameDropZone != null && firstNameDropZone.HasDroppedItem()) 
        {
            hasContent = true;
            Debug.Log("[CitizenDatabaseApp] firstNameDropZone has content");
        }
        if (lastNameDropZone != null && lastNameDropZone.HasDroppedItem()) 
        {
            hasContent = true;
            Debug.Log("[CitizenDatabaseApp] lastNameDropZone has content");
        }
        if (citizenIdDropZone != null && citizenIdDropZone.HasDroppedItem()) 
        {
            hasContent = true;
            Debug.Log("[CitizenDatabaseApp] citizenIdDropZone has content");
        }
        if (facialRecognitionDropZone != null && facialRecognitionDropZone.HasDroppedItem()) 
        {
            hasContent = true;
            Debug.Log("[CitizenDatabaseApp] facialRecognitionDropZone has content");
        }
        if (biometricsDropZone != null && biometricsDropZone.HasDroppedItem()) 
        {
            hasContent = true;
            Debug.Log("[CitizenDatabaseApp] biometricsDropZone has content");
        }
        
        // Update button states - enable if there's any content
        if (searchButton != null) 
        {
            searchButton.interactable = hasContent;
            Debug.Log($"[CitizenDatabaseApp] Search button interactable: {hasContent}");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] searchButton is null!");
        
        if (clearButton != null)
        {
            clearButton.interactable = hasContent;
            Debug.Log($"[CitizenDatabaseApp] Clear button interactable: {hasContent}");
        }
        else Debug.LogWarning("[CitizenDatabaseApp] clearButton is null!");
        
        Debug.Log($"[CitizenDatabaseApp] Button states updated - hasContent: {hasContent}");
    }
    
    public void OnDropZoneContentChanged()
    {
        Debug.Log("[CitizenDatabaseApp] OnDropZoneContentChanged called");
        
        // Debug: Check all drop zone states
        Debug.Log("[CitizenDatabaseApp] Current drop zone states:");
        if (firstNameDropZone != null) Debug.Log($"  firstNameDropZone: {firstNameDropZone.HasDroppedItem()}");
        if (lastNameDropZone != null) Debug.Log($"  lastNameDropZone: {lastNameDropZone.HasDroppedItem()}");
        if (citizenIdDropZone != null) Debug.Log($"  citizenIdDropZone: {citizenIdDropZone.HasDroppedItem()}");
        if (facialRecognitionDropZone != null) Debug.Log($"  facialRecognitionDropZone: {facialRecognitionDropZone.HasDroppedItem()}");
        if (biometricsDropZone != null) Debug.Log($"  biometricsDropZone: {biometricsDropZone.HasDroppedItem()}");
        
        UpdateButtonStates();
    }
    
    /// <summary>
    /// Try to find missing UI components dynamically by searching the hierarchy
    /// </summary>
    private void TryFindMissingUIComponents()
    {
        Debug.Log("[CitizenDatabaseApp] Attempting to find missing UI components dynamically");
        
        if (searchPanel == null)
        {
            // Try to find search panel by name
            var allObjects = GetComponentsInChildren<Transform>(true);
            foreach (var obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("search") && (name.Contains("panel") || name.Contains("page")))
                {
                    searchPanel = obj.gameObject;
                    Debug.Log($"[CitizenDatabaseApp] Found search panel dynamically: {obj.name}");
                    break;
                }
            }
            
            // If still not found, just use the first child that might be the search area
            if (searchPanel == null && transform.childCount > 0)
            {
                searchPanel = transform.GetChild(0).gameObject;
                Debug.Log($"[CitizenDatabaseApp] Using first child as search panel: {searchPanel.name}");
            }
        }
        
        if (searchButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                string name = button.name.ToLower();
                if (name.Contains("search") && !name.Contains("clear"))
                {
                    searchButton = button;
                    Debug.Log($"[CitizenDatabaseApp] Found search button dynamically: {button.name}");
                    break;
                }
            }
        }
        
        if (clearButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.name.ToLower().Contains("clear"))
                {
                    clearButton = button;
                    Debug.Log($"[CitizenDatabaseApp] Found clear button dynamically: {button.name}");
                    break;
                }
            }
        }
        
        // Log final status
        Debug.Log($"[CitizenDatabaseApp] Auto-find results: searchPanel={searchPanel?.name}, searchButton={searchButton?.name}, clearButton={clearButton?.name}");
    }
    
    public void Search()
    {
        if (searchCriteria == null)
        {
            searchCriteria = new Dictionary<string, string>();
        }
        
        GatherSearchCriteriaFromUI();
        
        if (searchCriteria.Count == 0) 
        {
            Debug.Log("No search criteria found");
            return;
        }
        
        StartCoroutine(PerformSearch());
    }
    
    private void GatherSearchCriteriaFromUI()
    {
        searchCriteria.Clear();
        
        // Read from first name field
        if (firstNameDropZone != null && firstNameDropZone.HasDroppedItem())
        {
            string content = firstNameDropZone.GetDroppedContent();
            if (!string.IsNullOrEmpty(content))
            {
                searchCriteria["firstName"] = content;
                Debug.Log($"CitizenDatabase.FNameDropZone.Content: {content}");
            }
        }
        
        // Read from last name field
        if (lastNameDropZone != null && lastNameDropZone.HasDroppedItem())
        {
            string content = lastNameDropZone.GetDroppedContent();
            if (!string.IsNullOrEmpty(content))
            {
                searchCriteria["lastName"] = content;
                Debug.Log($"CitizenDatabase.LNameDropZone.Content: {content}");
            }
        }
        
        // Read from citizen ID field
        if (citizenIdDropZone != null && citizenIdDropZone.HasDroppedItem())
        {
            string content = citizenIdDropZone.GetDroppedContent();
            if (!string.IsNullOrEmpty(content))
            {
                searchCriteria["citizenId"] = content;
                Debug.Log($"CitizenDatabase.CitizenIdDropZone.Content: {content}");
            }
        }
        
        // Read from facial recognition field (file-based)
        if (facialRecognitionDropZone != null && facialRecognitionDropZone.HasDroppedItem())
        {
            var file = facialRecognitionDropZone.GetDroppedFile();
            if (file != null)
            {
                // Use associatedCitizenId if available, otherwise fall back to filename
                if (!string.IsNullOrEmpty(file.associatedCitizenId))
                {
                    searchCriteria["facialRecognition"] = file.associatedCitizenId;
                    Debug.Log($"CitizenDatabase.FacialRecognitionDropZone.File: {file.fileName} -> Citizen ID: {file.associatedCitizenId}");
                }
                else
                {
                    searchCriteria["facialRecognition"] = file.fileName;
                    Debug.Log($"CitizenDatabase.FacialRecognitionDropZone.File: {file.fileName} (no associated citizen ID)");
                }
            }
        }
        
        // Read from biometrics field (file-based)
        if (biometricsDropZone != null && biometricsDropZone.HasDroppedItem())
        {
            var file = biometricsDropZone.GetDroppedFile();
            if (file != null)
            {
                // Use associatedCitizenId if available, otherwise fall back to filename
                if (!string.IsNullOrEmpty(file.associatedCitizenId))
                {
                    searchCriteria["biometrics"] = file.associatedCitizenId;
                    Debug.Log($"CitizenDatabase.BiometricsDropZone.File: {file.fileName} -> Citizen ID: {file.associatedCitizenId}");
                }
                else
                {
                    searchCriteria["biometrics"] = file.fileName;
                    Debug.Log($"CitizenDatabase.BiometricsDropZone.File: {file.fileName} (no associated citizen ID)");
                }
            }
        }
    }
    
    private IEnumerator PerformSearch()
    {
        Debug.Log("[CitizenDatabaseApp] PerformSearch started");
        
        // Disable search button
        if (searchButton != null)
        {
            searchButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("[CitizenDatabaseApp] searchButton is null");
        }
        
        // Show progress bar
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
            Debug.Log("[CitizenDatabaseApp] Progress bar activated");
        }
        else
        {
            Debug.LogWarning("[CitizenDatabaseApp] progressBar is null");
        }
        
        if (progressText != null)
        {
            progressText.text = "Searching database...";
        }
        else
        {
            Debug.LogWarning("[CitizenDatabaseApp] progressText is null");
        }
        
        // Show wait cursor
        if (retroEffects != null)
        {
            retroEffects.ShowHourglassCursor();
        }
        
        // Animate progress bar
        float elapsedTime = 0f;
        Debug.Log($"[CitizenDatabaseApp] Starting progress animation for {searchDuration} seconds");
        
        while (elapsedTime < searchDuration)
        {
            elapsedTime += progressUpdateInterval;
            float progress = elapsedTime / searchDuration;
            
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            
            if (progressText != null)
            {
                progressText.text = $"Searching database... {Mathf.RoundToInt(progress * 100)}%";
            }
            
            yield return new WaitForSeconds(progressUpdateInterval);
        }
        
        Debug.Log("[CitizenDatabaseApp] Progress animation completed");
        
        // Complete progress bar
        if (progressBar != null)
        {
            progressBar.value = 1f;
        }
        
        if (progressText != null)
        {
            progressText.text = "Search complete";
        }
        
        // Perform actual search
        Debug.Log("[CitizenDatabaseApp] Performing database search");
        PerformDatabaseSearch();
        
        // Wait a moment to show completion
        yield return new WaitForSeconds(0.5f);
        
        // Hide progress bar
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
            Debug.Log("[CitizenDatabaseApp] Progress bar hidden");
        }
        
        // Restore cursor
        if (retroEffects != null)
        {
            retroEffects.RestoreCursor();
        }
        
        // Show results
        Debug.Log("[CitizenDatabaseApp] Determining which panel to show based on results");
        ShowAppropriateResultsPanel();
    }
    
    private void PerformDatabaseSearch()
    {
        Debug.Log("[CitizenDatabaseApp] PerformDatabaseSearch started");
        searchResults.Clear();
        
        if (databaseManager == null)
        {
            Debug.LogWarning("[CitizenDatabaseApp] No CitizenDatabaseManager found! Trying to find it again...");
            FindDatabaseManager();
            
            if (databaseManager == null)
            {
                Debug.LogError("[CitizenDatabaseApp] Still no CitizenDatabaseManager found! Search cannot proceed.");
            return;
            }
        }
        
        Debug.Log($"[CitizenDatabaseApp] Searching with {searchCriteria.Count} criteria:");
        foreach (var criterion in searchCriteria)
        {
            Debug.Log($"  - {criterion.Key}: {criterion.Value}");
        }
        
        // Get all citizens from the database
        var allCitizens = databaseManager.GetAllCitizens();
        Debug.Log($"[CitizenDatabaseApp] Found {allCitizens.Count} citizens to search through");
        
        foreach (var citizen in allCitizens)
        {
            if (citizen == null) continue;
            
            Debug.Log($"[CitizenDatabaseApp] Checking citizen: {citizen.firstName} {citizen.lastName} ({citizen.citizenID})");
            
            bool matches = true;
            
            // Check each search criterion
            foreach (var criterion in searchCriteria)
            {
                if (!CitizenMatchesCriterion(citizen, criterion.Key, criterion.Value))
                {
                    matches = false;
                    Debug.Log($"[CitizenDatabaseApp] Citizen {citizen.firstName} {citizen.lastName} failed on criterion {criterion.Key}: {criterion.Value}");
                    break;
                }
            }
            
            if (matches)
            {
                searchResults.Add(citizen);
                Debug.Log($"[CitizenDatabaseApp] Found matching citizen: {citizen.firstName} {citizen.lastName} ({citizen.citizenID})");
            }
        }
        
        Debug.Log($"[CitizenDatabaseApp] Search completed. Found {searchResults.Count} matching citizens");
        
        if (searchResults.Count == 0)
        {
            Debug.LogWarning("[CitizenDatabaseApp] No citizens found matching the search criteria!");
        }
    }
    
    private bool CitizenMatchesCriterion(DatabaseCitizen citizen, string fieldType, string value)
    {
        if (string.IsNullOrEmpty(value)) return true;
        
        bool matches = false;
        
        switch (fieldType)
        {
            case "firstName":
                // Allow partial matching for names to be more user-friendly
                matches = citizen.firstName.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
                Debug.Log($"[CitizenDatabaseApp] Checking firstName: '{citizen.firstName}' contains '{value}' = {matches}");
                return matches;
                
            case "lastName":
                // Allow partial matching for names to be more user-friendly
                matches = citizen.lastName.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
                Debug.Log($"[CitizenDatabaseApp] Checking lastName: '{citizen.lastName}' contains '{value}' = {matches}");
                return matches;
                
            case "citizenId":
                // Exact match for citizen ID
                matches = citizen.citizenID.Equals(value, System.StringComparison.OrdinalIgnoreCase);
                Debug.Log($"[CitizenDatabaseApp] Checking citizenId: '{citizen.citizenID}' vs '{value}' = {matches}");
                return matches;
                
            case "facialRecognition":
                // For facial recognition, check if the citizen ID matches the associated citizen ID from the file
                // If no associated citizen ID, fall back to checking if citizen has a portrait
                if (!string.IsNullOrEmpty(value) && value.Length > 0)
                {
                    // Try to match by citizen ID first (if the value looks like a citizen ID)
                    if (value.StartsWith("CIT") || value.Length <= 10)
                    {
                        matches = citizen.citizenID.Equals(value, System.StringComparison.OrdinalIgnoreCase);
                        Debug.Log($"[CitizenDatabaseApp] Checking facialRecognition by citizen ID: '{citizen.citizenID}' vs '{value}' = {matches}");
                    }
                    else
                    {
                        // Fall back to checking if citizen has a portrait
                matches = citizen.picture != null;
                Debug.Log($"[CitizenDatabaseApp] Checking facialRecognition: citizen has picture = {matches}");
                    }
                }
                else
                {
                    // No value provided, check if citizen has a portrait
                    matches = citizen.picture != null;
                    Debug.Log($"[CitizenDatabaseApp] Checking facialRecognition: citizen has picture = {matches}");
                }
                return matches;
                
            case "biometrics":
                // For biometrics, check if the citizen ID matches the associated citizen ID from the file
                // If no associated citizen ID, fall back to checking if citizen has fingerprints
                if (!string.IsNullOrEmpty(value) && value.Length > 0)
                {
                    // Try to match by citizen ID first (if the value looks like a citizen ID)
                    if (value.StartsWith("CIT") || value.Length <= 10)
                    {
                        matches = citizen.citizenID.Equals(value, System.StringComparison.OrdinalIgnoreCase);
                        Debug.Log($"[CitizenDatabaseApp] Checking biometrics by citizen ID: '{citizen.citizenID}' vs '{value}' = {matches}");
                    }
                    else
                    {
                        // Fall back to checking if citizen has fingerprints
                matches = citizen.fingerprints != null && citizen.fingerprints.Count > 0;
                        Debug.Log($"[CitizenDatabaseApp] Checking biometrics: citizen has {citizen.fingerprints?.Count ?? 0} fingerprints = {matches}");
                    }
                }
                else
                {
                    // No value provided, check if citizen has fingerprints
                    matches = citizen.fingerprints != null && citizen.fingerprints.Count > 0;
                    Debug.Log($"[CitizenDatabaseApp] Checking biometrics: citizen has {citizen.fingerprints?.Count ?? 0} fingerprints = {matches}");
                }
                return matches;
                
            default:
                Debug.LogWarning($"[CitizenDatabaseApp] Unknown field type: {fieldType}");
                return false;
        }
    }
    
    public void GotoSearchPanel()
    {
        if (multipleResultsPanel != null) multipleResultsPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        if (searchPanel != null) searchPanel.SetActive(true);
        
        // Clear progress text when returning to search panel
        if (progressText != null)
        {
            progressText.text = "";
        }
        
        // Hide progress bar when returning to search panel
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }

        // Update button states when returning to search panel
        UpdateButtonStates();
    }
    
    public void GotoMultipleResultsPanel()
    {
        if (searchResults.Count > 1)
        {
            ShowMultipleResultsPanel();
        }
        else
        {
            GotoSearchPanel();
        }
    }
    
    /// <summary>
    /// Public method for the back button on Multiple Results Panel
    /// Always goes back to Search Panel
    /// </summary>
    public void BackFromMultipleResults()
    {
        Debug.Log("[CitizenDatabaseApp] BackFromMultipleResults called - going to SearchPanel");
        GotoSearchPanel();
    }
    
    /// <summary>
    /// Public method specifically for the Results Panel back button
    /// This ensures the button is calling the right method
    /// </summary>
    public void BackFromResults()
    {
        Debug.Log("[CitizenDatabaseApp] BackFromResults called - delegating to GoBack()");
        GoBack();
    }
    
    public void GoBack()
    {
        Debug.Log($"[CitizenDatabaseApp] GoBack called. cameFromMultipleResults: {CameFromMultipleResults}");
        Debug.Log($"[CitizenDatabaseApp] Current active panels - Search: {searchPanel?.activeInHierarchy}, Multiple: {multipleResultsPanel?.activeInHierarchy}, Results: {resultsPanel?.activeInHierarchy}");
        
        // Check which panel is currently active
        if (resultsPanel != null && resultsPanel.activeInHierarchy)
        {
            // We're on the results panel, check where we came from
            Debug.Log($"[CitizenDatabaseApp] Currently on Results Panel. cameFromMultipleResults = {CameFromMultipleResults}");
            
            if (CameFromMultipleResults)
            {
                // Go back to multiple results panel
                Debug.Log("[CitizenDatabaseApp] Going back to MultipleResultsPanel from ResultsPanel");
                ShowMultipleResultsPanel();
            }
            else
            {
                // Go back to search panel (single result)
                Debug.Log("[CitizenDatabaseApp] Going back to SearchPanel from ResultsPanel (single result)");
                GotoSearchPanel();
            }
        }
        else if (multipleResultsPanel != null && multipleResultsPanel.activeInHierarchy)
        {
            // We're on the multiple results panel, go back to search
            Debug.Log("[CitizenDatabaseApp] Going back to SearchPanel from MultipleResultsPanel");
            GotoSearchPanel();
        }
        else
        {
            // We're on the search panel, this shouldn't happen but fallback to search
            Debug.Log("[CitizenDatabaseApp] Already on SearchPanel, no navigation needed");
        }
    }
    
    /// <summary>
    /// Determine which results panel to show based on number of results
    /// </summary>
    private void ShowAppropriateResultsPanel()
    {
        Debug.Log($"[CitizenDatabaseApp] ShowAppropriateResultsPanel: {searchResults.Count} results");
        
        if (searchResults.Count == 0)
        {
            // No results - show results panel with "no results" message
            currentCitizen = null;
            CameFromMultipleResults = false;
            Debug.Log("[CitizenDatabaseApp] No results: Set cameFromMultipleResults = false");
            ShowResultsPanel();
        }
        else if (searchResults.Count == 1)
        {
            // Single result - go directly to detailed view
            currentCitizen = searchResults[0];
            CameFromMultipleResults = false;
            Debug.Log("[CitizenDatabaseApp] Single result: Set cameFromMultipleResults = false");
            ShowResultsPanel();
        }
        else
        {
            // Multiple results - show selection panel
            Debug.Log("[CitizenDatabaseApp] Multiple results: Showing MultipleResultsPanel");
            ShowMultipleResultsPanel();
        }
    }
    
    /// <summary>
    /// Show the multiple results selection panel
    /// </summary>
    private void ShowMultipleResultsPanel()
    {
        // Hide other panels
        if (searchPanel != null) searchPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);
        
        // Show multiple results panel
        if (multipleResultsPanel != null)
        {
            multipleResultsPanel.SetActive(true);
            DisplayMultipleResults();
        }
        else
        {
            // Fallback to showing first result if multiple results panel is missing
            currentCitizen = searchResults.Count > 0 ? searchResults[0] : null;
            DisplayCitizenResults(currentCitizen);
        }
    }
    
    /// <summary>
    /// Display the list of multiple search results
    /// </summary>
    private void DisplayMultipleResults()
    {
        // Update results count text
        if (resultsCountText != null)
        {
            resultsCountText.text = $"Found {searchResults.Count} results";
        }
        
        // Clear existing citizen list items
        if (citizenListContainer != null)
        {
            for (int i = citizenListContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(citizenListContainer.GetChild(i).gameObject);
            }
            
            // Create list items for each citizen
            for (int i = 0; i < searchResults.Count; i++)
            {
                var citizen = searchResults[i];
                CreateCitizenListItem(citizen, i);
            }
        }
    }
    
    /// <summary>
    /// Create a list item for a citizen in the multiple results view
    /// </summary>
    private void CreateCitizenListItem(DatabaseCitizen citizen, int index)
    {
        if (citizenListItemPrefab == null || citizenListContainer == null) return;
        
        // Create the list item
        GameObject listItem = Instantiate(citizenListItemPrefab, citizenListContainer);
        
        // Find components based on actual prefab structure
        Image portraitImage = listItem.transform.Find("Portrait")?.GetComponent<Image>();
        TMP_Text nameText = listItem.transform.Find("Name/NameText")?.GetComponent<TMP_Text>();
        TMP_Text idText = listItem.transform.Find("CitizenID/CitizenIDText")?.GetComponent<TMP_Text>();
        Button viewButton = listItem.transform.Find("ViewButton")?.GetComponent<Button>();
        
        // Set the citizen information
        if (nameText != null)
        {
            nameText.text = $"{citizen.firstName} {citizen.lastName}";
        }
        
        if (idText != null)
        {
            idText.text = citizen.citizenID;
        }
        
        if (portraitImage != null && citizen.picture != null)
        {
            portraitImage.sprite = citizen.picture;
        }
        
        // Set up the view button
        if (viewButton != null)
        {
            int citizenIndex = index; // Capture for closure
            viewButton.onClick.AddListener(() => OnCitizenSelected(citizenIndex));
        }
    }
    
    /// <summary>
    /// Called when a citizen is selected from the multiple results list
    /// </summary>
    private void OnCitizenSelected(int citizenIndex)
    {
        if (citizenIndex >= 0 && citizenIndex < searchResults.Count)
        {
            currentCitizen = searchResults[citizenIndex];
            CameFromMultipleResults = true;
            Debug.Log($"[CitizenDatabaseApp] OnCitizenSelected: Set cameFromMultipleResults = true for citizen {citizenIndex}");
            ShowResultsPanel();
        }
    }
    
    private void DisplayCitizenResults(DatabaseCitizen citizen)
    {
        currentCitizen = citizen;
        
        // Show details group and hide no results text
        if (detailsGroup != null)
        {
            detailsGroup.SetActive(true);
        }
        
        if (noResultsText != null)
        {
            noResultsText.gameObject.SetActive(false);
        }
        
        // Display citizen information
        if (citizenPortrait != null)
        {
            citizenPortrait.sprite = citizen.picture;
        }
        
        if (citizenNameText != null)
        {
            citizenNameText.text = citizen.FullName;
        }
        
        if (citizenIdText != null)
        {
            citizenIdText.text = citizen.citizenID;
        }
        
        if (citizenAddressText != null)
        {
            citizenAddressText.text = citizen.address;
        }
        
        if (citizenOccupationText != null)
        {
            citizenOccupationText.text = citizen.occupation;
        }
        
        if (citizenDobText != null)
        {
            citizenDobText.text = citizen.dateOfBirth;
        }
        
        // Display criminal history
        DisplayCriminalHistory();
        
        // Display fingerprints
        DisplayFingerprints();
        
        // Show Add to Suspects button
        if (addToSuspectsButton != null)
        {
            addToSuspectsButton.gameObject.SetActive(true);
        }
        
        // Update the button text based on whether this suspect already exists
        UpdateAddToSuspectsButtonText();
        
        Debug.Log($"[CitizenDatabaseApp] Displayed citizen results for: {citizen.FullName}");
    }
    
    private void DisplayNoResults()
    {
        currentCitizen = null;
        
        // Hide details group and show no results text
        if (detailsGroup != null)
        {
            detailsGroup.SetActive(false);
        }
        
        if (noResultsText != null)
        {
            noResultsText.gameObject.SetActive(true);
        }
        
        // Hide Add to Suspects button when no results
        if (addToSuspectsButton != null)
        {
            addToSuspectsButton.gameObject.SetActive(false);
        }
        
        Debug.Log("[CitizenDatabaseApp] Displayed no results message");
    }
    
    /// <summary>
    /// Display criminal history for the current citizen
    /// </summary>
    private void DisplayCriminalHistory()
    {
        if (citizenCriminalHistoryText != null && currentCitizen != null)
        {
            citizenCriminalHistoryText.text = FormatCriminalHistory(currentCitizen.criminalHistory);
        }
    }
    
    /// <summary>
    /// Display fingerprints for the current citizen
    /// </summary>
    private void DisplayFingerprints()
    {
        if (currentCitizen == null) return;
        
        // Get all fingerprint images
        Image[] fingerprintImages = { fingerprint1Image, fingerprint2Image, fingerprint3Image, fingerprint4Image, fingerprint5Image };
        
        // Display up to 5 fingerprints
        for (int i = 0; i < fingerprintImages.Length; i++)
        {
            if (fingerprintImages[i] != null)
            {
                if (i < currentCitizen.fingerprints.Count && currentCitizen.fingerprints[i] != null)
                {
                    fingerprintImages[i].sprite = currentCitizen.fingerprints[i];
                    fingerprintImages[i].gameObject.SetActive(true);
                }
                else
                {
                    fingerprintImages[i].gameObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Format criminal history for display
    /// </summary>
    private string FormatCriminalHistory(List<CriminalRecord> criminalHistory)
    {
        if (criminalHistory == null || criminalHistory.Count == 0)
        {
            return "No criminal record";
        }
        
        var formattedRecords = new List<string>();
        foreach (var record in criminalHistory)
        {
            if (record != null)
            {
                formattedRecords.Add($"• {record.offense} ({record.date})");
            }
        }
        
        return string.Join("\n", formattedRecords);
    }
    
    /// <summary>
    /// Show the results panel and display appropriate content
    /// </summary>
    private void ShowResultsPanel()
    {
        Debug.Log("[CitizenDatabaseApp] ShowResultsPanel called");
        
        // Hide other panels
        if (searchPanel != null)
        {
            searchPanel.SetActive(false);
            Debug.Log("[CitizenDatabaseApp] Search panel deactivated");
        }
        
        if (multipleResultsPanel != null)
        {
            multipleResultsPanel.SetActive(false);
            Debug.Log("[CitizenDatabaseApp] Multiple results panel deactivated");
        }
        
        // Show results panel
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
            Debug.Log("[CitizenDatabaseApp] Results panel activated");
            
            // Display appropriate content based on current citizen
            if (currentCitizen != null)
            {
                DisplayCitizenResults(currentCitizen);
            }
            else
            {
                DisplayNoResults();
            }
        }
        else
        {
            Debug.LogWarning("[CitizenDatabaseApp] resultsPanel is null");
        }
    }
    
    private void ClearFingerprintDisplay()
    {
        // Get all fingerprint images and clear them
        Image[] fingerprintImages = new Image[] 
        {
            fingerprint1Image,
            fingerprint2Image,
            fingerprint3Image,
            fingerprint4Image,
            fingerprint5Image
        };
        
        for (int i = 0; i < fingerprintImages.Length; i++)
        {
            if (fingerprintImages[i] != null)
            {
                fingerprintImages[i].sprite = null;
                fingerprintImages[i].gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Clear all search fields and reset the app
    /// </summary>
    public void Clear()
    {
        Debug.Log("[CitizenDatabaseApp] Clear called");
        
        // Clear all drop zones
        if (firstNameDropZone != null) firstNameDropZone.ClearDroppedItem();
        if (lastNameDropZone != null) lastNameDropZone.ClearDroppedItem();
        if (citizenIdDropZone != null) citizenIdDropZone.ClearDroppedItem();
        if (facialRecognitionDropZone != null) facialRecognitionDropZone.ClearDroppedItem();
        if (biometricsDropZone != null) biometricsDropZone.ClearDroppedItem();
        
        // Clear search criteria
        searchCriteria.Clear();
        
        // Clear progress text
        if (progressText != null)
        {
            progressText.text = "";
        }
        
        // Hide progress bar
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }
        
        // Update button states
        UpdateButtonStates();
        
        Debug.Log("[CitizenDatabaseApp] Clear completed");
    }
    
    /// <summary>
    /// Add the currently displayed citizen to the suspects list in the notebook
    /// </summary>
    public void AddToSuspectsList()
    {
        if (currentCitizen == null)
        {
            Debug.LogWarning("[CitizenDatabaseApp] No citizen to add to suspects list");
            return;
        }
        
        Debug.Log($"[CitizenDatabaseApp] Adding citizen to suspects list: {currentCitizen.FullName} (ID: {currentCitizen.citizenID})");
        
        // Get the SuspectsListManager to add the suspect
        var suspectsManager = SuspectsListManager.Instance;
        if (suspectsManager == null)
        {
            Debug.LogError("[CitizenDatabaseApp] SuspectsListManager not found!");
            return;
        }
        
        Debug.Log($"[CitizenDatabaseApp] Found SuspectsListManager: {suspectsManager.name}");
        
        // Check if suspect already exists in the notebook using more specific matching
        var existingSuspects = suspectsManager.GetAllSuspects();
        Debug.Log($"[CitizenDatabaseApp] Found {existingSuspects.Count} existing suspects in notebook");
        
        SuspectEntry existingEntry = null;
        
        foreach (var suspect in existingSuspects)
        {
            if (suspect != null && suspect.GetSuspectData() != null)
            {
                var suspectData = suspect.GetSuspectData();
                
                // Priority 1: Match by citizen ID (most reliable)
                if (suspectData.hasCitizenId && suspectData.citizenId == currentCitizen.citizenID)
                {
                    existingEntry = suspect;
                    Debug.Log($"[CitizenDatabaseApp] Found existing suspect by citizen ID: {currentCitizen.citizenID}");
                    break;
                }
                
                // Priority 2: Match by complete name (both first and last name)
                if (suspectData.hasFirstName && suspectData.hasLastName &&
                    suspectData.firstName.Equals(currentCitizen.firstName, System.StringComparison.OrdinalIgnoreCase) &&
                    suspectData.lastName.Equals(currentCitizen.lastName, System.StringComparison.OrdinalIgnoreCase))
                {
                    existingEntry = suspect;
                    Debug.Log($"[CitizenDatabaseApp] Found existing suspect by complete name: {currentCitizen.FullName}");
                    break;
                }
            }
        }
        
        if (existingEntry != null)
        {
            Debug.Log($"[CitizenDatabaseApp] Suspect {currentCitizen.FullName} already exists in notebook - updating with missing info");
            
            // Update the existing suspect with any missing information
            bool wasUpdated = UpdateExistingSuspect(existingEntry);
            
            if (wasUpdated)
            {
                Debug.Log($"[CitizenDatabaseApp] Successfully updated {currentCitizen.FullName} with missing information");
                
                // Update button text temporarily
                if (addToSuspectsButtonText != null)
                {
                    addToSuspectsButtonText.text = "Updated!";
                    StartCoroutine(ResetButtonTextAfterDelay(2f));
                }
                
                // Change button color to indicate success
                if (addToSuspectsButton != null)
                {
                    var buttonImage = addToSuspectsButton.GetComponent<Image>();
                    if (buttonImage != null)
                    {
                        buttonImage.color = Color.green;
                        StartCoroutine(ResetButtonColorAfterDelay(2f));
                    }
                }
                
                // Highlight the updated suspect entry
                HighlightExistingSuspect(existingEntry);
            }
            else
            {
                Debug.Log($"[CitizenDatabaseApp] No new information to add for {currentCitizen.FullName}");
                
                // Update button text temporarily
                if (addToSuspectsButtonText != null)
                {
                    addToSuspectsButtonText.text = "Already Complete!";
                    StartCoroutine(ResetButtonTextAfterDelay(2f));
                }
                
                // Highlight the existing suspect entry
                HighlightExistingSuspect(existingEntry);
            }
            
            return;
        }
        
        Debug.Log($"[CitizenDatabaseApp] No existing suspect found - adding new suspect: {currentCitizen.FullName}");
        
        // Add the suspect to the notebook with proper field parsing
        bool wasAdded = AddNewSuspectToNotebook();
        
        if (wasAdded)
        {
            Debug.Log($"[CitizenDatabaseApp] Successfully added {currentCitizen.FullName} to suspects list");
            
            // Update button text temporarily
            if (addToSuspectsButtonText != null)
            {
                addToSuspectsButtonText.text = "Added!";
                StartCoroutine(ResetButtonTextAfterDelay(2f));
            }
            
            // Change button color to indicate success
            if (addToSuspectsButton != null)
            {
                var buttonImage = addToSuspectsButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = Color.yellow;
                    StartCoroutine(ResetButtonColorAfterDelay(2f));
                }
            }
        }
        else
        {
            Debug.LogError($"[CitizenDatabaseApp] Failed to add {currentCitizen.FullName} to suspects list");
        }
    }
    
    /// <summary>
    /// Update an existing suspect with missing information from the database
    /// </summary>
    private bool UpdateExistingSuspect(SuspectEntry existingEntry)
    {
        if (existingEntry == null || currentCitizen == null) return false;
        
        var suspectData = existingEntry.GetSuspectData();
        if (suspectData == null) return false;
        
        bool wasUpdated = false;
        
        // Update citizen ID if missing
        if (!suspectData.hasCitizenId && !string.IsNullOrEmpty(currentCitizen.citizenID))
        {
            existingEntry.UpdateField("suspect_id", currentCitizen.citizenID);
            wasUpdated = true;
            Debug.Log($"[CitizenDatabaseApp] Added missing citizen ID: {currentCitizen.citizenID}");
        }
        
        // Update first name if missing
        if (!suspectData.hasFirstName && !string.IsNullOrEmpty(currentCitizen.firstName))
        {
            existingEntry.UpdateField("suspect_fname", currentCitizen.firstName);
            wasUpdated = true;
            Debug.Log($"[CitizenDatabaseApp] Added missing first name: {currentCitizen.firstName}");
        }
        
        // Update last name if missing
        if (!suspectData.hasLastName && !string.IsNullOrEmpty(currentCitizen.lastName))
        {
            existingEntry.UpdateField("suspect_lname", currentCitizen.lastName);
            wasUpdated = true;
            Debug.Log($"[CitizenDatabaseApp] Added missing last name: {currentCitizen.lastName}");
        }
        
        // Update portrait if missing
        if (!suspectData.hasPortrait && currentCitizen.picture != null)
        {
            existingEntry.UpdateField("suspect_portrait", currentCitizen.citizenID, currentCitizen.picture);
            wasUpdated = true;
            Debug.Log($"[CitizenDatabaseApp] Added missing portrait for citizen ID: {currentCitizen.citizenID}");
        }
        
        return wasUpdated;
    }
    
    /// <summary>
    /// Update the button text based on whether the current citizen is already in the suspects list
    /// </summary>
    public void UpdateAddToSuspectsButtonText()
    {
        if (currentCitizen == null || addToSuspectsButtonText == null) return;
        
        var suspectsManager = SuspectsListManager.Instance;
        if (suspectsManager == null) return;
        
        // Check if suspect already exists
        var existingSuspects = suspectsManager.GetAllSuspects();
        bool suspectExists = false;
        
        foreach (var suspect in existingSuspects)
        {
            if (suspect != null && suspect.GetSuspectData() != null)
            {
                var suspectData = suspect.GetSuspectData();
                
                // Check by citizen ID or complete name
                if ((suspectData.hasCitizenId && suspectData.citizenId == currentCitizen.citizenID) ||
                    (suspectData.hasFirstName && suspectData.hasLastName &&
                     suspectData.firstName.Equals(currentCitizen.firstName, System.StringComparison.OrdinalIgnoreCase) &&
                     suspectData.lastName.Equals(currentCitizen.lastName, System.StringComparison.OrdinalIgnoreCase)))
                {
                    suspectExists = true;
                    break;
                }
            }
        }
        
        // Update button text based on whether suspect exists
        if (suspectExists)
        {
            addToSuspectsButtonText.text = "Update Suspect Info";
        }
        else
        {
            addToSuspectsButtonText.text = "Add To Suspects List";
        }
    }
    
    /// <summary>
    /// Add a new suspect to the notebook using the direct database method
    /// </summary>
    private bool AddNewSuspectToNotebook()
    {
        Debug.Log($"[CitizenDatabaseApp] AddNewSuspectToNotebook called for: {currentCitizen?.FullName}");
        
        var suspectsManager = SuspectsListManager.Instance;
        if (suspectsManager == null) 
        {
            Debug.LogError("[CitizenDatabaseApp] SuspectsListManager is null in AddNewSuspectToNotebook");
            return false;
        }
        
        Debug.Log($"[CitizenDatabaseApp] Found SuspectsListManager: {suspectsManager.name}");
        
        // Use the direct method to add the citizen from database
        try
        {
            suspectsManager.AddSuspectFromDatabase(currentCitizen);
            Debug.Log($"[CitizenDatabaseApp] Successfully called AddSuspectFromDatabase for: {currentCitizen.FullName}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CitizenDatabaseApp] Exception in AddSuspectFromDatabase: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Highlight an existing suspect entry to show it's already in the notebook
    /// </summary>
    private void HighlightExistingSuspect(SuspectEntry suspectEntry)
    {
        if (suspectEntry == null) return;
        
        // Add a temporary highlight effect
        StartCoroutine(FlashSuspectEntry(suspectEntry));
    }
    
    /// <summary>
    /// Flash a suspect entry to highlight it
    /// </summary>
    private IEnumerator FlashSuspectEntry(SuspectEntry suspectEntry)
    {
        if (suspectEntry == null) yield break;
        
        var canvasGroup = suspectEntry.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;
        
        // Flash the entry by temporarily changing its alpha
        float originalAlpha = canvasGroup.alpha;
        
        // Flash to yellow tint
        canvasGroup.alpha = 0.5f;
        yield return new WaitForSeconds(0.2f);
        
        // Return to normal
        canvasGroup.alpha = originalAlpha;
        yield return new WaitForSeconds(0.2f);
        
        // Flash again
        canvasGroup.alpha = 0.5f;
        yield return new WaitForSeconds(0.2f);
        
        // Return to normal
        canvasGroup.alpha = originalAlpha;
    }
    
    /// <summary>
    /// Reset button text after a delay
    /// </summary>
    private IEnumerator ResetButtonTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (addToSuspectsButtonText != null)
        {
            addToSuspectsButtonText.text = "Add To Suspects List";
        }
    }
    
    /// <summary>
    /// Reset button color after a delay
    /// </summary>
    private IEnumerator ResetButtonColorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (addToSuspectsButton != null)
        {
            var buttonImage = addToSuspectsButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.white;
            }
        }
    }
    
    /// <summary>
    /// Show success feedback when adding a new suspect
    /// </summary>
    private IEnumerator ShowAddSuccessFeedback()
    {
        if (addToSuspectsButton != null && addToSuspectsButtonText != null)
        {
            // Change button text temporarily
            string originalText = addToSuspectsButtonText.text;
            addToSuspectsButtonText.text = "Added!";
            
            // Change button color
            var buttonImage = addToSuspectsButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color originalColor = buttonImage.color;
                buttonImage.color = buttonHighlightColor;
                
                // Wait for highlight duration
                yield return new WaitForSeconds(highlightDuration);
                
                // Restore original color and text
                buttonImage.color = originalColor;
                addToSuspectsButtonText.text = originalText;
            }
            else
            {
                // If no image component, just wait and restore text
                yield return new WaitForSeconds(highlightDuration);
                addToSuspectsButtonText.text = originalText;
            }
        }
        
        Debug.Log("[CitizenDatabaseApp] Showed add success feedback");
    }
    
    /// <summary>
    /// Public method to force clear search for debugging
    /// </summary>
    [ContextMenu("Force Clear Search")]
    public void ForceClearSearch()
    {
        Debug.Log("[CitizenDatabaseApp] ForceClearSearch called via context menu");
        Clear();
    }
    
    /// <summary>
    /// Public method to force search for debugging
    /// </summary>
    [ContextMenu("Force Start Search")]
    public void ForceStartSearch()
    {
        Debug.Log("[CitizenDatabaseApp] ForceStartSearch called via context menu");
        Search();
    }
    
    /// <summary>
    /// Clear only the search criteria dictionary without affecting drop zones
    /// Used when we want to clear the internal state but preserve visual state
    /// </summary>
    private void ClearSearchCriteriaOnly()
    {
        Debug.Log("[CitizenDatabaseApp] ClearSearchCriteriaOnly called - preserving drop zone state");
        
        if (searchCriteria == null)
        {
            searchCriteria = new Dictionary<string, string>();
        }
        
        searchCriteria.Clear();
        UpdateButtonStates();
    }
    
    public override void OnAppOpen()
    {
        base.OnAppOpen();
        GotoSearchPanel();
        
        if (searchCriteria == null)
        {
            searchCriteria = new Dictionary<string, string>();
        }
    }
    

} 