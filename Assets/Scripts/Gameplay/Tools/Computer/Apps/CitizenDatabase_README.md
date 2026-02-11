# Citizen Database App

The Citizen Database app allows players to search for citizens using tags from the notebook and fingerprint/face images from computer files.

## Features

- **Text-based Search**: Drop tags from the notebook onto First Name, Last Name, and Citizen ID fields
- **Image-based Search**: Drop photo files onto Facial Recognition field and fingerprint files onto Biometrics field
- **Progress Animation**: Shows a progress bar with wait cursor during search
- **Results Display**: Shows citizen's full profile including portrait, personal info, and fingerprints

## Setup Instructions

### 1. Create the App Config

1. Create a new AppConfig ScriptableObject
2. Set AppType to "App"
3. Assign the CitizenDatabase prefab to AppPrefab
4. Set IsOnDesktop to true to show on desktop
5. Set IsOnAppMenu to true to show in menu

### 2. Create the Citizen Database Prefab

1. Create a new GameObject with the CitizenDatabaseApp script
2. Set up the UI panels:
   - **Search Panel**: Contains all search fields and controls
   - **Results Panel**: Contains citizen information display

### 3. Set up Search Fields

For each search field, create a CitizenDatabaseDropZone:

#### Text Fields (First Name, Last Name, Citizen ID)
- Set `acceptTags = true`
- Set `acceptFiles = false`
- Assign frame image, field label text, and placeholder text

#### Image Fields (Facial Recognition, Biometrics)
- Set `acceptTags = false`
- Set `acceptFiles = true`
- Set `acceptedFileTypes` to include Photo and FingerprintScan
- Assign frame image, field label text, and placeholder text

### 4. Set up Results Display

- **Citizen Portrait**: Image component for displaying citizen photo
- **Text Fields**: TMP_Text components for name, ID, address, occupation, DOB
- **Fingerprint Container**: Transform to hold fingerprint prefabs
- **Fingerprint Prefab**: Prefab with FingerprintDisplay component

### 5. Add to App Registry

1. Add the AppConfig to the AppRegistry's default apps list
2. Ensure the CitizenDatabaseManager component is in the scene
3. Assign the CitizenDatabase ScriptableObject to the manager

## File Types

The app supports two new file types:

### FingerprintScan
- Added to FileType enum
- Requires fingerprint icon in FileIconSettings
- Used for fingerprint image files

### Photo
- Existing file type
- Used for facial recognition images

## Usage

1. **Open the App**: Click the Citizen Database icon on desktop or menu
2. **Add Search Criteria**: Drag tags or files to the appropriate fields
3. **Search**: Click the Search button to start the search
4. **View Results**: The app shows the matching citizen's full profile
5. **Clear**: Use Clear button to reset search fields
6. **Back**: Use Back button to return to search panel

## Integration

### With Notebook Tags
- DraggableTag components automatically detect CitizenDatabaseDropZone
- Tags can be dropped on text fields for name/ID searches

### With Computer Files
- DraggableFileIcon components can be used instead of regular FileIcon
- Files can be dropped on image fields for facial/biometric searches

### With Database
- CitizenDatabaseManager provides access to citizen data
- Supports both CSV and ScriptableObject citizens
- Automatically loads portraits and fingerprints from Resources folders

## Customization

### Search Duration
- Adjust `searchDuration` in CitizenDatabaseApp script
- Controls how long the progress bar animation runs

### Visual Feedback
- Customize colors and animations in CitizenDatabaseDropZone
- Modify progress bar appearance and behavior

### Database Integration
- Extend CitizenMatchesCriterion method for custom search logic
- Add new search fields by creating additional drop zones

## Troubleshooting

### Tags Not Dropping
- Ensure DraggableTag components are properly set up
- Check that CitizenDatabaseDropZone has correct acceptTags setting

### Files Not Dropping
- Use DraggableFileIcon instead of regular FileIcon
- Verify file types are in acceptedFileTypes array

### No Search Results
- Check CitizenDatabaseManager is assigned
- Verify citizen database has data
- Ensure search criteria match citizen data format

### Missing Images
- Check Resources/Portraits and Resources/Fingerprints folders
- Verify file naming conventions match citizen IDs 