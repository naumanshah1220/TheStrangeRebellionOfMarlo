using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Manages the suspects list in the notebook
/// </summary>
public class SuspectsListManager : SingletonMonoBehaviour<SuspectsListManager>
{
    [Header("References")]
    public GameObject suspectEntryPrefab; // Prefab for suspect entries
    public NotebookManager notebookManager; // Reference to notebook manager

    [Header("Settings")]
    public bool debugSuspectInfo = false;

    // Data
    private List<SuspectEntry> suspectEntries = new List<SuspectEntry>();
    private Dictionary<string, SuspectEntry> suspectLookup = new Dictionary<string, SuspectEntry>();

    protected override void OnSingletonAwake()
    {
        // Find notebook manager if not assigned
        if (notebookManager == null)
        {
            notebookManager = FindFirstObjectByType<NotebookManager>();
            if (notebookManager == null)
            {
                Debug.LogError("[SuspectsListManager] No NotebookManager found in scene! Please ensure one exists.");
            }
            else
            {
                Debug.Log($"[SuspectsListManager] Found NotebookManager: {notebookManager.name}");
            }
        }
        else
        {
            Debug.Log($"[SuspectsListManager] NotebookManager already assigned: {notebookManager.name}");
        }
    }
    
    private void Start()
    {
        // Subscribe to case events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened += OnCaseOpened;
            GameManager.Instance.OnCaseClosed += OnCaseClosed;
        }
    }
    
    protected override void OnSingletonDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCaseOpened -= OnCaseOpened;
            GameManager.Instance.OnCaseClosed -= OnCaseClosed;
        }
    }
    
    /// <summary>
    /// Called when a case is opened
    /// </summary>
    private void OnCaseOpened(Case openedCase)
    {
        // Clear previous suspects
        ClearAllSuspects();
        
        // Load pre-assigned suspects from the case
        if (openedCase != null && openedCase.suspects != null)
        {
            foreach (var suspect in openedCase.suspects)
            {
                if (suspect != null)
                {
                    AddPreAssignedSuspect(suspect);
                }
            }
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Loaded {suspectEntries.Count} pre-assigned suspects for case: {openedCase?.title}");
    }
    
    /// <summary>
    /// Called when a case is closed
    /// </summary>
    private void OnCaseClosed(Case closedCase)
    {
        ClearAllSuspects();
        
        if (debugSuspectInfo)
            Debug.Log("[SuspectsListManager] Cleared suspects for case close");
    }
    
    /// <summary>
    /// Add a pre-assigned suspect from case data
    /// </summary>
    public void AddPreAssignedSuspect(Citizen citizen)
    {
        if (citizen == null) return;
        
        // Check if suspect already exists
        if (suspectLookup.ContainsKey(citizen.citizenID))
        {
            if (debugSuspectInfo)
                Debug.LogWarning($"[SuspectsListManager] Suspect {citizen.FullName} already exists in list");
            return;
        }
        
        // Create new suspect entry
        CreateSuspectEntry(citizen);
    }
    
    /// <summary>
    /// Discover suspect information from tags
    /// </summary>
    public void DiscoverSuspectInfo(string fieldType, string value, Sprite portraitSprite = null)
    {
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Discovering suspect info: {fieldType} = {value}");
        
        // Try to find existing suspect that matches this information
        SuspectEntry existingEntry = FindMatchingSuspect(fieldType, value);
        
        if (existingEntry != null)
        {
            // Update existing entry
            bool wasUpdated = existingEntry.UpdateField(fieldType, value, portraitSprite);
            if (wasUpdated && debugSuspectInfo)
            {
                Debug.Log($"[SuspectsListManager] Updated existing suspect with {fieldType}: {value}");
            }
        }
        else
        {
            // Create new suspect entry
            CreateNewSuspectFromDiscovery(fieldType, value, portraitSprite);
        }
        
        // Note: We don't add discovery clues here anymore as the NotebookManager handles the original clue
    }
    
    /// <summary>
    /// Discover grouped suspect information (for new nested format)
    /// </summary>
    public void DiscoverGroupedSuspectInfo(List<SuspectDiscoveryInfo> suspectInfo)
    {
        if (suspectInfo == null || suspectInfo.Count == 0) return;
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Discovering grouped suspect info with {suspectInfo.Count} fields");
        
        // Try to find existing suspect that matches any of this information
        SuspectEntry existingEntry = FindMatchingSuspectFromGroup(suspectInfo);
        
        if (existingEntry != null)
        {
            // Update existing entry with all new information
            bool wasUpdated = false;
            foreach (var info in suspectInfo)
            {
                Sprite portraitSprite = null;
                if (info.fieldType == "suspect_portrait")
                {
                    portraitSprite = GetPortraitSprite(info.value);
                }
                
                bool fieldUpdated = existingEntry.UpdateField(info.fieldType, info.value, portraitSprite);
                if (fieldUpdated) wasUpdated = true;
            }
            
            if (wasUpdated && debugSuspectInfo)
            {
                Debug.Log($"[SuspectsListManager] Updated existing suspect with grouped info");
            }
        }
        else
        {
            // Create new suspect entry with all information
            CreateNewSuspectFromGroupedDiscovery(suspectInfo);
        }
    }
    
    /// <summary>
    /// Find a suspect that matches any information in the group
    /// </summary>
    private SuspectEntry FindMatchingSuspectFromGroup(List<SuspectDiscoveryInfo> suspectInfo)
    {
        foreach (var info in suspectInfo)
        {
            var existingEntry = FindMatchingSuspect(info.fieldType, info.value);
            if (existingEntry != null)
            {
                return existingEntry;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Create a new suspect entry from grouped discovery information
    /// </summary>
    private void CreateNewSuspectFromGroupedDiscovery(List<SuspectDiscoveryInfo> suspectInfo)
    {
        if (suspectEntryPrefab == null)
        {
            Debug.LogError("[SuspectsListManager] Missing suspect entry prefab!");
            return;
        }
        
        // Create entry object without parent (NotebookManager will handle placement)
        GameObject entryObj = Instantiate(suspectEntryPrefab);
        SuspectEntry entry = entryObj.GetComponent<SuspectEntry>();
        
        if (entry == null)
        {
            Debug.LogError("[SuspectsListManager] Suspect entry prefab doesn't have SuspectEntry component!");
            Destroy(entryObj);
            return;
        }
        
        // Set up for fade-in animation (start invisible)
        var canvasGroup = entryObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Initialize with discovered information
        var suspectData = new SuspectEntry.SuspectData(System.Guid.NewGuid().ToString());
        entry.Initialize(suspectData);
        
        // Apply all discovered information
        foreach (var info in suspectInfo)
        {
            Sprite portraitSprite = null;
            if (info.fieldType == "suspect_portrait")
            {
                portraitSprite = GetPortraitSprite(info.value);
            }
            
            entry.UpdateField(info.fieldType, info.value, portraitSprite);
            
            // Add to lookup for each piece of information
            if (!suspectLookup.ContainsKey(info.value))
            {
                suspectLookup[info.value] = entry;
            }
        }
        
        // Add to tracking lists
        suspectEntries.Add(entry);
        
        // Add to notebook
        if (notebookManager != null)
        {
            notebookManager.AddSuspectEntry(entryObj);
            
            // Start fade-in animation for the new suspect entry
            var suspectEntryComponent = entryObj.GetComponent<SuspectEntry>();
            if (suspectEntryComponent != null)
            {
                suspectEntryComponent.StartFadeInAnimation();
            }
        }
        else
        {
            Debug.LogError("[SuspectsListManager] NotebookManager reference is missing!");
            Destroy(entryObj);
            return;
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Created new suspect entry from grouped discovery with {suspectInfo.Count} fields");
    }
    
    /// <summary>
    /// Get portrait sprite from citizen database
    /// </summary>
    private Sprite GetPortraitSprite(string citizenId)
    {
        var citizenDatabase = FindFirstObjectByType<CitizenDatabase>();
        if (citizenDatabase != null)
        {
            var citizen = citizenDatabase.GetCitizenById(citizenId);
            if (citizen != null)
            {
                return citizen.picture;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Data structure for suspect discovery information
    /// </summary>
    [System.Serializable]
    public class SuspectDiscoveryInfo
    {
        public string fieldType;
        public string value;
    }
    
    /// <summary>
    /// Find a suspect that might match the discovered information
    /// </summary>
    private SuspectEntry FindMatchingSuspect(string fieldType, string value)
    {
        foreach (var entry in suspectEntries)
        {
            if (entry.MatchesIdentifier(value))
            {
                return entry;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Create a new suspect entry from discovered information
    /// </summary>
    private void CreateNewSuspectFromDiscovery(string fieldType, string value, Sprite portraitSprite)
    {
        if (suspectEntryPrefab == null)
        {
            Debug.LogError("[SuspectsListManager] Missing suspect entry prefab!");
            return;
        }
        
        // Create entry object without parent (NotebookManager will handle placement)
        GameObject entryObj = Instantiate(suspectEntryPrefab);
        SuspectEntry entry = entryObj.GetComponent<SuspectEntry>();
        
        if (entry == null)
        {
            Debug.LogError("[SuspectsListManager] Suspect entry prefab doesn't have SuspectEntry component!");
            Destroy(entryObj);
            return;
        }
        
        // Set up for fade-in animation (start invisible)
        var canvasGroup = entryObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Initialize with discovered information
        var suspectData = new SuspectEntry.SuspectData(System.Guid.NewGuid().ToString());
        entry.Initialize(suspectData);
        entry.UpdateField(fieldType, value, portraitSprite);
        
        // Add to tracking lists
        suspectEntries.Add(entry);
        
        // Use value as key for lookup (might be name, ID, etc.)
        if (!suspectLookup.ContainsKey(value))
        {
            suspectLookup[value] = entry;
        }
        
        // Add to notebook
        if (notebookManager != null)
        {
            notebookManager.AddSuspectEntry(entryObj);
            
            // Start fade-in animation for the new suspect entry
            var suspectEntryComponent = entryObj.GetComponent<SuspectEntry>();
            if (suspectEntryComponent != null)
            {
                suspectEntryComponent.StartFadeInAnimation();
            }
        }
        else
        {
            Debug.LogError("[SuspectsListManager] NotebookManager reference is missing!");
            Destroy(entryObj);
            return;
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Created new suspect entry from {fieldType}: {value}");
    }
    
    /// <summary>
    /// Create suspect entry for pre-assigned citizen
    /// </summary>
    private void CreateSuspectEntry(Citizen citizen)
    {
        if (suspectEntryPrefab == null)
        {
            Debug.LogError("[SuspectsListManager] Missing suspect entry prefab!");
            return;
        }
        
        // Create entry object without parent (NotebookManager will handle placement)
        GameObject entryObj = Instantiate(suspectEntryPrefab);
        SuspectEntry entry = entryObj.GetComponent<SuspectEntry>();
        
        if (entry == null)
        {
            Debug.LogError("[SuspectsListManager] Suspect entry prefab doesn't have SuspectEntry component!");
            Destroy(entryObj);
            return;
        }
        
        // Initialize with citizen data
        entry.Initialize(citizen);
        
        // Ensure pre-assigned suspects are visible immediately (no fade-in)
        var canvasGroup = entryObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // Add to tracking lists
        suspectEntries.Add(entry);
        suspectLookup[citizen.citizenID] = entry;
        
        // Also add by name for lookup
        if (!string.IsNullOrEmpty(citizen.firstName))
        {
            suspectLookup[citizen.firstName] = entry;
        }
        if (!string.IsNullOrEmpty(citizen.lastName))
        {
            suspectLookup[citizen.lastName] = entry;
        }
        
        // Add to notebook
        if (notebookManager != null)
        {
            notebookManager.AddSuspectEntry(entryObj);
        }
        else
        {
            Debug.LogError("[SuspectsListManager] NotebookManager reference is missing!");
            Destroy(entryObj);
            return;
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Created suspect entry for: {citizen.FullName}");
    }
    
    /// <summary>
    /// Clear all suspect entries
    /// </summary>
    public void ClearAllSuspects()
    {
        // Destroy all entry objects
        foreach (var entry in suspectEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }
        
        // Clear tracking lists
        suspectEntries.Clear();
        suspectLookup.Clear();
        
        if (debugSuspectInfo)
            Debug.Log("[SuspectsListManager] Cleared all suspects");
    }
    
    /// <summary>
    /// Get all suspect entries
    /// </summary>
    public List<SuspectEntry> GetAllSuspects()
    {
        return new List<SuspectEntry>(suspectEntries);
    }
    
    /// <summary>
    /// Get completed suspects (all 4 fields filled)
    /// </summary>
    public List<SuspectEntry> GetCompletedSuspects()
    {
        List<SuspectEntry> completed = new List<SuspectEntry>();
        foreach (var entry in suspectEntries)
        {
            if (entry != null && entry.GetSuspectData().IsComplete())
            {
                completed.Add(entry);
            }
        }
        return completed;
    }
    
    /// <summary>
    /// Get completed suspects as Citizen objects (for CommitOverlay integration)
    /// </summary>
    public List<Citizen> GetCompletedSuspectsAsCitizens()
    {
        List<Citizen> completedCitizens = new List<Citizen>();
        foreach (var entry in suspectEntries)
        {
            if (entry != null && entry.GetSuspectData().IsComplete())
            {
                Citizen citizen = entry.GetCitizen();
                if (citizen != null)
                {
                    completedCitizens.Add(citizen);
                }
            }
        }
        return completedCitizens;
    }
    
    /// <summary>
    /// Get suspect count
    /// </summary>
    public int GetSuspectCount()
    {
        return suspectEntries.Count;
    }
    
    /// <summary>
    /// Get completed suspect count
    /// </summary>
    public int GetCompletedSuspectCount()
    {
        return GetCompletedSuspects().Count;
    }

    /// <summary>
    /// Add a citizen from the database to the suspects list
    /// </summary>
    public void AddSuspectFromDatabase(DatabaseCitizen citizen)
    {
        if (citizen == null) 
        {
            Debug.LogError("[SuspectsListManager] AddSuspectFromDatabase called with null citizen");
            return;
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Adding citizen from database: {citizen.FullName} (ID: {citizen.citizenID})");
        
        // Check if suspect already exists by citizen ID
        var existingEntry = FindSuspectByIdentifier(citizen.citizenID);
        
        if (existingEntry != null)
        {
            if (debugSuspectInfo)
                Debug.Log($"[SuspectsListManager] Citizen {citizen.FullName} already exists in suspects list");
            return;
        }
        
        Debug.Log($"[SuspectsListManager] Citizen {citizen.FullName} not found in existing suspects - creating new entry");
        
        // Create new suspect entry
        if (suspectEntryPrefab == null)
        {
            Debug.LogError("[SuspectsListManager] Missing suspect entry prefab!");
            return;
        }
        
        Debug.Log($"[SuspectsListManager] Creating new suspect entry object");
        
        // Create entry object without parent (NotebookManager will handle placement)
        GameObject entryObj = Instantiate(suspectEntryPrefab);
        SuspectEntry entry = entryObj.GetComponent<SuspectEntry>();
        
        if (entry == null)
        {
            Debug.LogError("[SuspectsListManager] Suspect entry prefab doesn't have SuspectEntry component!");
            Destroy(entryObj);
            return;
        }
        
        Debug.Log($"[SuspectsListManager] Successfully created SuspectEntry component");
        
        // Set up for fade-in animation (start invisible)
        var canvasGroup = entryObj.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Convert DatabaseCitizen to Citizen using the ToRuntimeCitizen method
        // We need to get the generic questions and responses from somewhere
        var citizenDatabase = FindFirstObjectByType<CitizenDatabase>();
        GenericQuestion[] genericQuestions = null;
        TagResponse[] genericResponses = null;
        
        if (citizenDatabase != null)
        {
            genericQuestions = citizenDatabase.databaseGenericQuestions;
            genericResponses = citizenDatabase.databaseGenericResponses;
            Debug.Log($"[SuspectsListManager] Found CitizenDatabase with {genericQuestions?.Length ?? 0} questions and {genericResponses?.Length ?? 0} responses");
        }
        else
        {
            Debug.LogWarning("[SuspectsListManager] CitizenDatabase not found, using fallback generic questions/responses");
            // Fallback to default generic questions and responses
            genericQuestions = new GenericQuestion[]
            {
                new GenericQuestion { tagType = "person", questions = new string[] { "Tell me about '{tag}'", "Do you know '{tag}'?", "What's your relationship with '{tag}'?" } },
                new GenericQuestion { tagType = "location", questions = new string[] { "Tell me about '{tag}'", "Have you been to '{tag}'?", "What do you know about '{tag}'?" } },
                new GenericQuestion { tagType = "item", questions = new string[] { "Tell me about '{tag}'", "Do you recognize '{tag}'?", "What do you know about '{tag}'?" } },
                new GenericQuestion { tagType = "date", questions = new string[] { "Tell me about '{tag}'", "What happened on '{tag}'?", "Do you remember '{tag}'?" } },
                new GenericQuestion { tagType = "time", questions = new string[] { "Tell me about '{tag}'", "What were you doing at '{tag}'?", "Do you remember '{tag}'?" } }
            };
            
            genericResponses = new TagResponse[]
            {
                new TagResponse { responseSequence = new string[] { "I don't know anything about that." } },
                new TagResponse { responseSequence = new string[] { "I already told you, I don't know." } },
                new TagResponse { responseSequence = new string[] { "Why do you keep asking me about things I don't know?" } }
            };
        }
        
        Debug.Log($"[SuspectsListManager] Converting DatabaseCitizen to Citizen");
        
        // Convert DatabaseCitizen to Citizen
        var runtimeCitizen = citizen.ToRuntimeCitizen(genericQuestions, genericResponses);
        
        Debug.Log($"[SuspectsListManager] Successfully converted to Citizen: {runtimeCitizen?.FullName}");
        
        // Initialize with the converted citizen data
        entry.Initialize(runtimeCitizen);
        
        Debug.Log($"[SuspectsListManager] Successfully initialized SuspectEntry");
        
        // Add to tracking lists
        suspectEntries.Add(entry);
        suspectLookup[citizen.citizenID] = entry;
        
        // Also add by name for lookup
        if (!string.IsNullOrEmpty(citizen.firstName))
        {
            suspectLookup[citizen.firstName] = entry;
        }
        if (!string.IsNullOrEmpty(citizen.lastName))
        {
            suspectLookup[citizen.lastName] = entry;
        }
        
        Debug.Log($"[SuspectsListManager] Added to tracking lists. Total suspects: {suspectEntries.Count}");
        
        // Add to notebook
        if (notebookManager != null)
        {
            Debug.Log($"[SuspectsListManager] Adding to NotebookManager");
            
            // Ensure the GameObject is active before adding to notebook
            if (entryObj != null && !entryObj.activeInHierarchy)
            {
                entryObj.SetActive(true);
                Debug.Log($"[SuspectsListManager] Activated suspect entry GameObject");
            }
            
            // Ensure the GameObject is active before starting coroutine
            if (entryObj != null && !entryObj.activeInHierarchy)
            {
                entryObj.SetActive(true);
                Debug.Log($"[SuspectsListManager] Activated suspect entry GameObject");
            }
            
            notebookManager.AddSuspectEntry(entryObj);
            
            // Start fade-in animation for the new suspect entry
            var suspectEntryComponent = entryObj.GetComponent<SuspectEntry>();
            if (suspectEntryComponent != null)
            {
                suspectEntryComponent.StartFadeInAnimation();
            }
            
            Debug.Log($"[SuspectsListManager] Successfully added to NotebookManager");
        }
        else
        {
            Debug.LogError("[SuspectsListManager] NotebookManager reference is missing!");
            Debug.LogError("[SuspectsListManager] Please ensure NotebookManager is assigned in the inspector or exists in the scene");
            Destroy(entryObj);
            return;
        }
        
        if (debugSuspectInfo)
            Debug.Log($"[SuspectsListManager] Successfully added citizen {citizen.FullName} to suspects list");
    }
    
    /// <summary>
    /// Find a suspect by a specific identifier (citizen ID, name, etc.)
    /// </summary>
    public SuspectEntry FindSuspectByIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return null;
        
        // First check the lookup dictionary
        if (suspectLookup.ContainsKey(identifier))
        {
            return suspectLookup[identifier];
        }
        
        // Fallback to checking all entries
        foreach (var entry in suspectEntries)
        {
            if (entry != null && entry.MatchesIdentifier(identifier))
            {
                return entry;
            }
        }
        
        return null;
    }
} 