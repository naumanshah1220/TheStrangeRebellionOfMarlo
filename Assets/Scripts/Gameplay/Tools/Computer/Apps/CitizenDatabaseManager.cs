using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager component for Citizen Database app to access citizen data
/// </summary>
public class CitizenDatabaseManager : MonoBehaviour
{
    [Header("Database Reference")]
    [SerializeField] private CitizenDatabase citizenDatabase;
    
    private void Awake()
    {
        Debug.Log("[CitizenDatabaseManager] Awake called");
        
        // Try to find the database if not assigned
        if (citizenDatabase == null)
        {
            Debug.Log("[CitizenDatabaseManager] No database assigned, searching for one...");
            citizenDatabase = FindFirstObjectByType<CitizenDatabase>();
            if (citizenDatabase == null)
            {
                Debug.Log("[CitizenDatabaseManager] No CitizenDatabase in scene, trying Resources...");
                // Try to load from Resources
                citizenDatabase = Resources.Load<CitizenDatabase>("CitizenDatabase");
            }
        }
        
        if (citizenDatabase == null)
        {
            Debug.LogError("[CitizenDatabaseManager] No CitizenDatabase found! Please assign one or ensure it exists in Resources.");
        }
        else
        {
            Debug.Log($"[CitizenDatabaseManager] Found CitizenDatabase: {citizenDatabase.name}");
            
            // Force reload the database to ensure CSV is loaded fresh
            citizenDatabase.ForceReloadDatabase();
        }
    }
    
    private void Start()
    {
        Debug.Log("[CitizenDatabaseManager] Start called");
        
        // Ensure database is properly initialized
        if (citizenDatabase != null)
        {
            Debug.Log($"[CitizenDatabaseManager] Ensuring database is initialized. Current citizen count: {citizenDatabase.GetCitizenCount()}");
        }
    }
    
    /// <summary>
    /// Get all citizens from the database
    /// </summary>
    public List<DatabaseCitizen> GetAllCitizens()
    {
        if (citizenDatabase == null) 
        {
            Debug.LogWarning("[CitizenDatabaseManager] citizenDatabase is null in GetAllCitizens");
            return new List<DatabaseCitizen>();
        }
        
        Debug.Log($"[CitizenDatabaseManager] Getting all citizens from database: {citizenDatabase.name}");
        
        // Convert Citizen objects to DatabaseCitizen objects
        var citizens = citizenDatabase.GetAllCitizens();
        Debug.Log($"[CitizenDatabaseManager] Found {citizens.Count} citizens in database");
        
        var databaseCitizens = new List<DatabaseCitizen>();
        
        foreach (var citizen in citizens)
        {
            if (citizen != null)
            {
                var dbCitizen = new DatabaseCitizen
                {
                    citizenID = citizen.citizenID,
                    firstName = citizen.firstName,
                    lastName = citizen.lastName,
                    dateOfBirth = citizen.dateOfBirth,
                    gender = citizen.gender,
                    ethnicity = citizen.ethnicity,
                    maritalStatus = citizen.maritalStatus,
                    address = citizen.address,
                    occupation = citizen.occupation,
                    criminalHistory = new List<CriminalRecord>(citizen.criminalHistory),
                    nervousnessLevel = citizen.nervousnessLevel,
                    picture = citizen.picture,
                    fingerprints = new List<Sprite>(citizen.fingerprints)
                };
                
                databaseCitizens.Add(dbCitizen);
            }
        }
        
        Debug.Log($"[CitizenDatabaseManager] Converted {databaseCitizens.Count} citizens to DatabaseCitizen format");
        return databaseCitizens;
    }
    
    /// <summary>
    /// Get citizen by ID
    /// </summary>
    public DatabaseCitizen GetCitizenById(string citizenId)
    {
        if (citizenDatabase == null) return null;
        
        var citizen = citizenDatabase.GetCitizenById(citizenId);
        if (citizen == null) return null;
        
        return new DatabaseCitizen
        {
            citizenID = citizen.citizenID,
            firstName = citizen.firstName,
            lastName = citizen.lastName,
            dateOfBirth = citizen.dateOfBirth,
            gender = citizen.gender,
            ethnicity = citizen.ethnicity,
            maritalStatus = citizen.maritalStatus,
            address = citizen.address,
            occupation = citizen.occupation,
            criminalHistory = new List<CriminalRecord>(citizen.criminalHistory),
            nervousnessLevel = citizen.nervousnessLevel,
            picture = citizen.picture,
            fingerprints = new List<Sprite>(citizen.fingerprints)
        };
    }
    
    /// <summary>
    /// Search citizens by name
    /// </summary>
    public List<DatabaseCitizen> SearchCitizensByName(string searchTerm)
    {
        if (citizenDatabase == null) return new List<DatabaseCitizen>();
        
        var citizens = citizenDatabase.SearchCitizensByName(searchTerm);
        var databaseCitizens = new List<DatabaseCitizen>();
        
        foreach (var citizen in citizens)
        {
            if (citizen != null)
            {
                var dbCitizen = new DatabaseCitizen
                {
                    citizenID = citizen.citizenID,
                    firstName = citizen.firstName,
                    lastName = citizen.lastName,
                    dateOfBirth = citizen.dateOfBirth,
                    gender = citizen.gender,
                    ethnicity = citizen.ethnicity,
                    maritalStatus = citizen.maritalStatus,
                    address = citizen.address,
                    occupation = citizen.occupation,
                    criminalHistory = new List<CriminalRecord>(citizen.criminalHistory),
                    nervousnessLevel = citizen.nervousnessLevel,
                    picture = citizen.picture,
                    fingerprints = new List<Sprite>(citizen.fingerprints)
                };
                
                databaseCitizens.Add(dbCitizen);
            }
        }
        
        return databaseCitizens;
    }
    
    /// <summary>
    /// Get total number of citizens
    /// </summary>
    public int GetCitizenCount()
    {
        return citizenDatabase != null ? citizenDatabase.GetCitizenCount() : 0;
    }
} 