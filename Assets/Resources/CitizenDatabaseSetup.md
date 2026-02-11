# Expanded Citizens Database System

This document explains how to use the expanded Citizens Database system that supports both CSV data and ScriptableObject citizens.

## System Overview

The new system supports two types of citizens:

1. **CSV Citizens** - Loaded from a CSV file with portraits and fingerprints from Resources folders
2. **ScriptableObject Citizens** - Traditional Unity ScriptableObjects with custom interrogation data

ScriptableObject citizens will override CSV citizens if they have the same CitizenID.

## Setup Instructions

### 1. CSV Database Setup

1. **Create/Edit the CSV file**:
   - Place your CSV file in the `Resources/` folder
   - Use the provided `citizens_database.csv` as a template
   - Follow the exact column header format

2. **Add Portrait Images**:
   - Create folder: `Resources/Portraits/`
   - Add portrait images named exactly as the CitizenID (e.g., `CIT001.png`)
   - Supported formats: PNG, JPEG

3. **Add Fingerprint Images**:
   - Create folder: `Resources/Fingerprints/`
   - Add fingerprint images named as `{CitizenID}_finger_{1-5}` (e.g., `CIT001_finger_1.png`)
   - You can add 1-5 fingerprints per citizen

4. **Configure the Database**:
   - Select your CitizenDatabase ScriptableObject in the Inspector
   - Assign the CSV file to the "Citizen CSV File" field
   - Enable "Load From CSV" checkbox
   - Configure the generic questions and responses as needed

### 2. ScriptableObject Citizens

1. **Create Individual Citizens**:
   - Create Citizen ScriptableObjects as before
   - Add them to the "Scriptable Object Citizens" list in the database
   - These citizens can have custom TagInteractions for specific interrogation responses

2. **Override CSV Citizens**:
   - If a ScriptableObject citizen has the same CitizenID as a CSV citizen, it will override the CSV data
   - This allows you to upgrade important characters with custom interrogation data

## CSV Format Specification

### Required Columns

```csv
CitizenID,FirstName,LastName,DOB,Gender,Ethnicity,MaritalStatus,Address,Occupation,CriminalRecord,NervousnessLevel,Deceased
```

### Column Descriptions

- **CitizenID**: Unique identifier (e.g., CIT001, CIT002)
- **FirstName**: First name of the citizen
- **LastName**: Last name of the citizen
- **DOB**: Date of birth in MM/DD/YYYY format
- **Gender**: Male, Female, or Other
- **Ethnicity**: Caucasian, AfricanAmerican, Hispanic, Asian, MiddleEastern, NativeAmerican, Mixed, Other
- **MaritalStatus**: Single, Married, Divorced, Widowed, Separated
- **Address**: Full address string (use quotes if contains commas)
- **Occupation**: Job title or profession
- **CriminalRecord**: See Criminal Record Format below
- **NervousnessLevel**: Float between 0.0 and 1.0 (0.0 = calm, 1.0 = very nervous)
- **Deceased**: TRUE/FALSE or Yes/No

### Criminal Record Format

Use this format for the CriminalRecord column:
```
"offense1|date1|description1|severity1;offense2|date2|description2|severity2"
```

**Example**:
```
"Theft|05/12/2015|Stole car parts from garage|Minor;Assault|03/20/2018|Bar fight|Moderate"
```

**Severity Options**: Minor, Moderate, Major, Severe

**Empty Criminal Record**: Leave the field empty with just `""`

## Generic Questions and Responses

### Database-Wide Generic Questions

The database includes generic questions that apply to all CSV citizens:

- **Person tags**: "Tell me about '{tag}'", "Do you know '{tag}'?", etc.
- **Location tags**: "Have you been to '{tag}'?", "What do you know about '{tag}'?", etc.
- **Item tags**: "Do you recognize '{tag}'?", "Tell me about '{tag}'", etc.
- **Date/Time tags**: "What happened on '{tag}'?", "What were you doing at '{tag}'?", etc.

### Database-Wide Generic Responses

When citizens don't know about a tag, they use these responses:
- "I don't know anything about that."
- "I already told you, I don't know."
- "Why do you keep asking me about things I don't know?"

## Database Management

### Inspector Context Menu Options

Right-click on the CitizenDatabase ScriptableObject to access these options:

- **Validate Database**: Check for duplicate IDs and missing data
- **Rebuild Database**: Reload CSV and rebuild the lookup dictionary
- **Reload CSV Data**: Reload only the CSV data (useful during development)

### Code API

```csharp
// Get citizen by ID
Citizen citizen = database.GetCitizenById("CIT001");

// Get all citizens
List<Citizen> allCitizens = database.GetAllCitizens();

// Get living citizens (not deceased)
List<Citizen> livingCitizens = database.GetLivingCitizens();

// Get deceased citizens
List<Citizen> deceasedCitizens = database.GetDeceasedCitizens();

// Search by name
List<Citizen> searchResults = database.SearchCitizensByName("John");

// Filter by criteria
List<Citizen> maleCitizens = database.GetCitizensByGender(Gender.Male);
List<Citizen> criminals = database.GetCitizensWithCriminalRecord();

// Get counts
int totalCount = database.GetCitizenCount();
int csvCount = database.GetCSVCitizenCount();
int scriptableCount = database.GetScriptableObjectCitizenCount();
```

## Best Practices

1. **Use consistent CitizenIDs**: Use a format like CIT001, CIT002, etc.
2. **Organize your images**: Keep portraits and fingerprints well-organized
3. **Test frequently**: Use the context menu options to validate your data
4. **ScriptableObject for important characters**: Use ScriptableObject citizens for key characters that need custom interrogation responses
5. **CSV for background population**: Use CSV for the general population of citizens

## Troubleshooting

### Common Issues

1. **Citizens not loading**: Check that the CSV file is in the Resources folder and assigned
2. **Images not loading**: Verify file names exactly match CitizenIDs
3. **Criminal records not parsing**: Check the pipe (|) and semicolon (;) format
4. **Duplicate IDs**: Use the "Validate Database" context menu option

### Debug Information

Enable "Show Debug Info" on the CitizenDatabase to see detailed loading information in the console.

## Migration from Old System

If you have existing Citizens in the old `allCitizens` list:

1. Move them to the `scriptableObjectCitizens` list
2. The old system will continue to work alongside the new CSV system
3. ScriptableObject citizens will override CSV citizens with matching IDs

## Performance Notes

- The system builds a lookup dictionary at startup for fast citizen retrieval
- Images are loaded once during database initialization
- The system is designed to handle hundreds of citizens efficiently 