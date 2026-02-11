using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CitizenDatabase", menuName = "Citizens/Citizen Database")]
public class CitizenDatabase : ScriptableObject
{
    [Header("CSV Database")]
    [SerializeField] private bool loadFromCSV = true;
    
    [Header("ScriptableObject Citizens")]
    [Tooltip("Citizens created as ScriptableObjects - these override CSV citizens with matching IDs")]
    public List<Citizen> scriptableObjectCitizens = new List<Citizen>();
    
    [Header("Generic Questions & Responses")]
    [Tooltip("Generic questions used for all database citizens")]
    public GenericQuestion[] databaseGenericQuestions = new GenericQuestion[]
    {
        new GenericQuestion { tagType = "person", questions = new string[] { "Tell me about '{tag}'", "Do you know '{tag}'?", "What's your relationship with '{tag}'?" } },
        new GenericQuestion { tagType = "location", questions = new string[] { "Tell me about '{tag}'", "Have you been to '{tag}'?", "What do you know about '{tag}'?" } },
        new GenericQuestion { tagType = "item", questions = new string[] { "Tell me about '{tag}'", "Do you recognize '{tag}'?", "What do you know about '{tag}'?" } },
        new GenericQuestion { tagType = "date", questions = new string[] { "Tell me about '{tag}'", "What happened on '{tag}'?", "Do you remember '{tag}'?" } },
        new GenericQuestion { tagType = "time", questions = new string[] { "Tell me about '{tag}'", "What were you doing at '{tag}'?", "Do you remember '{tag}'?" } }
    };
    
    [Tooltip("Generic responses used for all database citizens")]
    public TagResponse[] databaseGenericResponses = new TagResponse[]
    {
        new TagResponse { responseSequence = new string[] { "I don't know anything about that." } },
        new TagResponse { responseSequence = new string[] { "I already told you, I don't know." } },
        new TagResponse { responseSequence = new string[] { "Why do you keep asking me about things I don't know?" } }
    };
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    
    // Runtime data
    private Dictionary<string, Citizen> citizenLookup;
    private List<DatabaseCitizen> databaseCitizens = new List<DatabaseCitizen>();
    private bool isInitialized = false;
    
    private void OnEnable()
    {
        InitializeDatabase();
    }
    
    private void OnValidate()
    {
        InitializeDatabase();
        ValidateDatabase();
    }
    
    /// <summary>
    /// Initialize the database by loading CSV data and building lookup dictionary
    /// </summary>
    private void InitializeDatabase()
    {
        if (showDebugInfo)
            Debug.Log("[CitizenDatabase] Initializing database...");
            
        if (loadFromCSV)
        {
            LoadCitizenDataFromCSV();
        }
        BuildLookupDictionary();
        isInitialized = true;
        
        if (showDebugInfo)
            Debug.Log($"[CitizenDatabase] Database initialization complete. Total citizens: {GetCitizenCount()} ({GetCSVCitizenCount()} CSV, {GetScriptableObjectCitizenCount()} ScriptableObject)");
    }
    
    /// <summary>
    /// Load citizen data from CSV file
    /// </summary>
    private void LoadCitizenDataFromCSV()
    {
        // Manually load CSV from Resources folder
        TextAsset csvFile = Resources.Load<TextAsset>("citizens_database");
        
        if (csvFile == null)
        {
            Debug.LogError("[CitizenDatabase] Could not load CSV file from Resources/citizens_database.csv. Make sure the file exists at Assets/Resources/citizens_database.csv");
            return;
        }
        
        try
        {
            var csvData = CSVReader.Read(csvFile);
            databaseCitizens.Clear();
            
            foreach (var row in csvData)
            {
                var citizen = ParseCitizenFromCSVRow(row);
                if (citizen != null)
                {
                    databaseCitizens.Add(citizen);
                }
            }
            
            if (showDebugInfo)
                Debug.Log($"[CitizenDatabase] Loaded {databaseCitizens.Count} citizens from CSV");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CitizenDatabase] Error loading CSV data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Parse a citizen from a CSV row
    /// </summary>
    private DatabaseCitizen ParseCitizenFromCSVRow(Dictionary<string, object> row)
    {
        try
        {
            var citizen = new DatabaseCitizen
            {
                citizenID = row.ContainsKey("CitizenID") ? row["CitizenID"].ToString() : "",
                firstName = row.ContainsKey("FirstName") ? row["FirstName"].ToString() : "",
                lastName = row.ContainsKey("LastName") ? row["LastName"].ToString() : "",
                dateOfBirth = row.ContainsKey("DOB") ? row["DOB"].ToString() : "",
                address = row.ContainsKey("Address") ? row["Address"].ToString() : "",
                occupation = row.ContainsKey("Occupation") ? row["Occupation"].ToString() : "",
                nervousnessLevel = row.ContainsKey("NervousnessLevel") ? CSVReader.ParseFloat(row["NervousnessLevel"].ToString(), 0.3f) : 0.3f,
                deceased = row.ContainsKey("Deceased") ? CSVReader.ParseBool(row["Deceased"].ToString()) : false
            };
            
            // Parse enums
            if (row.ContainsKey("Gender"))
                citizen.gender = CSVReader.ParseEnum<Gender>(row["Gender"].ToString(), Gender.Male);
            if (row.ContainsKey("Ethnicity"))
                citizen.ethnicity = CSVReader.ParseEnum<Ethnicity>(row["Ethnicity"].ToString(), Ethnicity.Caucasian);
            if (row.ContainsKey("MaritalStatus"))
                citizen.maritalStatus = CSVReader.ParseEnum<MaritalStatus>(row["MaritalStatus"].ToString(), MaritalStatus.Single);
            
            // Parse criminal records
            if (row.ContainsKey("CriminalRecord"))
            {
                citizen.criminalHistory = CSVReader.ParseCriminalRecords(row["CriminalRecord"].ToString());
            }
            
            // Load portrait and fingerprints
            citizen.LoadPortrait();
            citizen.LoadFingerprints();
            
            return citizen;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[CitizenDatabase] Error parsing citizen row: {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Builds the lookup dictionary for fast citizen retrieval by ID
    /// </summary>
    private void BuildLookupDictionary()
    {
        citizenLookup = new Dictionary<string, Citizen>();
        
        // First add database citizens
        foreach (var dbCitizen in databaseCitizens)
        {
            if (dbCitizen != null && !string.IsNullOrEmpty(dbCitizen.citizenID))
            {
                var runtimeCitizen = dbCitizen.ToRuntimeCitizen(databaseGenericQuestions, databaseGenericResponses);
                citizenLookup[dbCitizen.citizenID] = runtimeCitizen;
            }
        }
        
        // Then add/override with ScriptableObject citizens
        foreach (var citizen in scriptableObjectCitizens)
        {
            if (citizen != null && !string.IsNullOrEmpty(citizen.citizenID))
            {
                if (citizenLookup.ContainsKey(citizen.citizenID))
                {
                    if (showDebugInfo)
                        Debug.Log($"[CitizenDatabase] ScriptableObject citizen {citizen.FullName} overriding CSV citizen with ID: {citizen.citizenID}");
                }
                citizenLookup[citizen.citizenID] = citizen;
            }
        }
        
        if (showDebugInfo)
            Debug.Log($"[CitizenDatabase] Built lookup dictionary with {citizenLookup.Count} citizens");
    }
    
    /// <summary>
    /// Validates the database for common issues
    /// </summary>
    private void ValidateDatabase()
    {
        var allCitizens = GetAllCitizens();
        
        var duplicateIds = allCitizens
            .Where(c => c != null && !string.IsNullOrEmpty(c.citizenID))
            .GroupBy(c => c.citizenID)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
            
        foreach (var duplicateId in duplicateIds)
        {
            Debug.LogError($"[CitizenDatabase] Duplicate citizen ID: {duplicateId}");
        }
        
        var missingIds = allCitizens
            .Where(c => c != null && string.IsNullOrEmpty(c.citizenID))
            .ToList();
            
        if (missingIds.Count > 0)
        {
            Debug.LogWarning($"[CitizenDatabase] {missingIds.Count} citizens have missing IDs");
        }
    }
    
    /// <summary>
    /// Get citizen by ID
    /// </summary>
    public Citizen GetCitizenById(string citizenId)
    {
        if (!isInitialized)
            InitializeDatabase();
            
        return citizenLookup.TryGetValue(citizenId, out Citizen citizen) ? citizen : null;
    }
    
    /// <summary>
    /// Get multiple citizens by their IDs
    /// </summary>
    public List<Citizen> GetCitizensByIds(List<string> citizenIds)
    {
        List<Citizen> result = new List<Citizen>();
        
        foreach (string id in citizenIds)
        {
            Citizen citizen = GetCitizenById(id);
            if (citizen != null)
            {
                result.Add(citizen);
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"[CitizenDatabase] Citizen not found with ID: {id}");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Get all citizens (both CSV and ScriptableObject)
    /// </summary>
    public List<Citizen> GetAllCitizens()
    {
        if (!isInitialized)
            InitializeDatabase();
            
        return citizenLookup.Values.ToList();
    }
    
    /// <summary>
    /// Get citizens by gender
    /// </summary>
    public List<Citizen> GetCitizensByGender(Gender gender)
    {
        return GetAllCitizens().Where(c => c != null && c.gender == gender).ToList();
    }
    
    /// <summary>
    /// Get citizens with criminal records
    /// </summary>
    public List<Citizen> GetCitizensWithCriminalRecord()
    {
        return GetAllCitizens().Where(c => c != null && c.HasCriminalRecord).ToList();
    }
    
    /// <summary>
    /// Get citizens by ethnicity
    /// </summary>
    public List<Citizen> GetCitizensByEthnicity(Ethnicity ethnicity)
    {
        return GetAllCitizens().Where(c => c != null && c.ethnicity == ethnicity).ToList();
    }
    
    /// <summary>
    /// Get living citizens (not deceased)
    /// </summary>
    public List<Citizen> GetLivingCitizens()
    {
        return GetAllCitizens().Where(c => c != null && !IsDeceased(c.citizenID)).ToList();
    }
    
    /// <summary>
    /// Get deceased citizens
    /// </summary>
    public List<Citizen> GetDeceasedCitizens()
    {
        return GetAllCitizens().Where(c => c != null && IsDeceased(c.citizenID)).ToList();
    }
    
    /// <summary>
    /// Check if a citizen is deceased
    /// </summary>
    public bool IsDeceased(string citizenId)
    {
        var dbCitizen = databaseCitizens.FirstOrDefault(c => c.citizenID == citizenId);
        return dbCitizen?.deceased ?? false;
    }
    
    /// <summary>
    /// Search citizens by name (partial matches)
    /// </summary>
    public List<Citizen> SearchCitizensByName(string searchTerm)
    {
        string lowerSearchTerm = searchTerm.ToLower();
        return GetAllCitizens().Where(c => c != null && 
            (c.firstName.ToLower().Contains(lowerSearchTerm) || 
             c.lastName.ToLower().Contains(lowerSearchTerm) ||
             c.FullName.ToLower().Contains(lowerSearchTerm))).ToList();
    }
    
    /// <summary>
    /// Get random citizens (useful for testing)
    /// </summary>
    public List<Citizen> GetRandomCitizens(int count)
    {
        var shuffled = GetAllCitizens().Where(c => c != null).OrderBy(x => Random.value).ToList();
        return shuffled.Take(count).ToList();
    }
    
    /// <summary>
    /// Add a new ScriptableObject citizen to the database
    /// </summary>
    public void AddScriptableObjectCitizen(Citizen citizen)
    {
        if (citizen == null)
        {
            Debug.LogError("[CitizenDatabase] Cannot add null citizen");
            return;
        }
        
        if (string.IsNullOrEmpty(citizen.citizenID))
        {
            Debug.LogError("[CitizenDatabase] Cannot add citizen without ID");
            return;
        }
        
        // Check if citizen already exists in ScriptableObject list
        if (scriptableObjectCitizens.Any(c => c.citizenID == citizen.citizenID))
        {
            Debug.LogError($"[CitizenDatabase] ScriptableObject citizen with ID {citizen.citizenID} already exists");
            return;
        }
        
        scriptableObjectCitizens.Add(citizen);
        BuildLookupDictionary();
        
        if (showDebugInfo)
            Debug.Log($"[CitizenDatabase] Added ScriptableObject citizen: {citizen.FullName} ({citizen.citizenID})");
    }
    
    /// <summary>
    /// Remove a ScriptableObject citizen from the database
    /// </summary>
    public bool RemoveScriptableObjectCitizen(string citizenId)
    {
        var citizen = scriptableObjectCitizens.FirstOrDefault(c => c.citizenID == citizenId);
        if (citizen != null)
        {
            scriptableObjectCitizens.Remove(citizen);
            BuildLookupDictionary();
            
            if (showDebugInfo)
                Debug.Log($"[CitizenDatabase] Removed ScriptableObject citizen: {citizen.FullName} ({citizenId})");
            return true;
        }
        
        if (showDebugInfo)
            Debug.LogWarning($"[CitizenDatabase] Could not find ScriptableObject citizen to remove: {citizenId}");
        return false;
    }
    
    /// <summary>
    /// Get total number of citizens
    /// </summary>
    public int GetCitizenCount()
    {
        return GetAllCitizens().Count;
    }
    
    /// <summary>
    /// Get number of CSV citizens
    /// </summary>
    public int GetCSVCitizenCount()
    {
        return databaseCitizens.Count;
    }
    
    /// <summary>
    /// Get number of ScriptableObject citizens
    /// </summary>
    public int GetScriptableObjectCitizenCount()
    {
        return scriptableObjectCitizens.Count;
    }
    
    /// <summary>
    /// Reload CSV data (useful for development)
    /// </summary>
    public void ReloadCSVData()
    {
        if (showDebugInfo)
            Debug.Log("[CitizenDatabase] Reloading CSV data...");
            
        if (loadFromCSV)
        {
            LoadCitizenDataFromCSV();
            BuildLookupDictionary();
            
            if (showDebugInfo)
                Debug.Log($"[CitizenDatabase] CSV data reloaded. Total citizens: {GetCitizenCount()} ({GetCSVCitizenCount()} CSV, {GetScriptableObjectCitizenCount()} ScriptableObject)");
        }
        else
        {
            if (showDebugInfo)
                Debug.Log("[CitizenDatabase] CSV loading is disabled");
        }
    }
    
    [ContextMenu("Validate Database")]
    private void ValidateDatabaseMenu()
    {
        ValidateDatabase();
        Debug.Log($"[CitizenDatabase] Validation complete. Total citizens: {GetCitizenCount()}");
    }
    
    [ContextMenu("Rebuild Database")]
    private void RebuildDatabaseMenu()
    {
        InitializeDatabase();
        Debug.Log($"[CitizenDatabase] Database rebuilt. {GetCitizenCount()} total citizens ({GetCSVCitizenCount()} CSV, {GetScriptableObjectCitizenCount()} ScriptableObject)");
    }
    
    [ContextMenu("Reload CSV Data")]
    private void ReloadCSVDataMenu()
    {
        ReloadCSVData();
    }
    
    /// <summary>
    /// Public method to force reload the entire database
    /// Useful for debugging and ensuring fresh data is loaded
    /// </summary>
    public void ForceReloadDatabase()
    {
        if (showDebugInfo)
            Debug.Log("[CitizenDatabase] Force reloading database...");
            
        isInitialized = false;
        InitializeDatabase();
    }
} 