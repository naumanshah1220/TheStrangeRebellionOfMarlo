# Suspect Discovery System Documentation

## Overview

The Suspect Discovery System is a progressive investigation feature that allows players to gradually discover and piece together information about suspects through interactive clues. Instead of having all suspects pre-loaded, players build their suspects list by discovering individual pieces of information (name, ID, portrait, etc.) through gameplay.

## Key Features

### 🔍 **Progressive Discovery**
- Players discover suspects piece by piece
- Each suspect has 4 fields: Portrait, Citizen ID, First Name, Last Name
- Visual completion tracking with filled/unfilled squares
- Automatic portrait loading from database when ID is discovered

### 📝 **Notebook Integration**
- Dedicated "Suspects" tab in notebook (alongside "Clues" tab)
- File tab-like interface for switching between modes
- Real-time count badges showing suspect progress
- Animated transitions between modes

### 🎯 **Interactive Tags**
- New suspect tags: `<suspect_fname>`, `<suspect_lname>`, `<suspect_id>`, `<suspect_portrait>`
- Clicking suspect tags adds information to suspects list
- Visual feedback for discovered information
- Automatic notebook entry creation

### 🖱️ **Drag & Drop Assignment**
- Complete suspect profiles become draggable `<suspect>` tags
- Drag complete suspects to monitors for interrogation
- Visual feedback during drag operations
- Automatic citizen assignment to monitors

## System Components

### Core Classes

#### **SuspectEntry**
- Represents a single suspect in the suspects list
- Manages 4 data fields with completion tracking
- Creates draggable complete suspect tags when all fields are filled
- Handles visual updates and data validation

#### **SuspectsListManager**
- Singleton manager for the suspects list
- Handles suspect discovery and data updates
- Manages pre-assigned suspects from cases
- Integrates with notebook system for clue generation

#### **NotebookTabManager**
- Manages notebook tabs (Suspects/Clues)
- Animated tab switching with visual feedback
- Real-time count badges
- Content area management

#### **SuspectTag**
- Draggable component for complete suspect profiles
- Handles drag-and-drop to monitors
- Visual feedback during drag operations
- Integration with SuspectManager for monitor assignment

#### **SuspectDraggableTag**
- Special tag component for individual suspect information
- Handles click events for discovery
- Visual state management (normal/discovered)
- Integration with SuspectsListManager

### Integration Points

#### **ClueTextProcessor** (Modified)
- Added support for new suspect tags
- Automatic suspect discovery on tag creation
- Enhanced regex pattern matching
- Special handling for suspect-related content

#### **SuspectManager** (Modified)
- Added `AssignCitizenToMonitor()` method
- Added `RemoveCitizenFromMonitor()` method
- Enhanced monitor management for discovered suspects

## Usage Instructions

### For Designers

#### **Setting Up Suspect Discovery**
1. **Create Clues with Suspect Tags**:
   ```
   "The witness saw <suspect_fname>James</suspect_fname> near the scene."
   "Security footage shows citizen <suspect_id>CIT001</suspect_id> at 3:00 PM."
   "The suspect's last name is <suspect_lname>Johnson</suspect_lname>."
   ```

2. **Configure Prefabs**:
   - Create prefabs for each suspect tag type
   - Assign them to ClueTextProcessor
   - Set up visual styling (colors, fonts, etc.)

3. **Set Up Notebook**:
   - Configure NotebookTabManager with tab buttons
   - Set up suspects content area
   - Configure suspect entry prefab

#### **Case Configuration**
- Cases can still have pre-assigned suspects (auto-complete all fields)
- Mix pre-assigned and discoverable suspects
- Use `suspects` array in case data for pre-assigned suspects

### For Developers

#### **Adding New Suspect Information**
```csharp
// Programmatically add suspect information
SuspectsListManager.Instance.DiscoverSuspectInfo("suspect_fname", "James");
SuspectsListManager.Instance.DiscoverSuspectInfo("suspect_lname", "Johnson");
SuspectsListManager.Instance.DiscoverSuspectInfo("suspect_id", "CIT001");
```

#### **Checking Suspect Status**
```csharp
// Get suspect counts
int totalSuspects = SuspectsListManager.Instance.GetSuspectCount();
int completedSuspects = SuspectsListManager.Instance.GetCompletedSuspectCount();

// Get completed suspects
List<SuspectEntry> completed = SuspectsListManager.Instance.GetCompletedSuspects();
```

#### **Custom Tag Handling**
```csharp
// Check if a tag is a suspect tag
bool isSuspectTag = ClueTextProcessor.IsSuspectTag(tagType);

// Custom suspect discovery
SuspectsListManager.Instance.DiscoverSuspectInfo(fieldType, value, portraitSprite);
```

## Technical Details

### Data Flow
1. **Discovery**: Player clicks suspect tag in clue
2. **Processing**: ClueTextProcessor triggers suspect discovery
3. **Management**: SuspectsListManager updates suspect data
4. **Visual**: SuspectEntry updates UI and completion status
5. **Completion**: Complete suspects create draggable tags
6. **Assignment**: Draggable tags can be assigned to monitors

### Performance Considerations
- Suspect entries are pooled and reused
- Efficient lookup using dictionaries
- Lazy loading of portraits from database
- Minimal Update() calls with event-driven updates

### Error Handling
- Graceful handling of missing citizen data
- Fallback for missing portraits
- Duplicate suspect prevention
- Null reference protection

## Prefab Requirements

### Suspect Entry Prefab
- **SuspectEntry** component
- Portrait Image component
- Text components for ID, first name, last name
- 4 completion square Image components
- Complete suspect tag container

### Suspect Tag Prefabs
- **SuspectDraggableTag** component
- TextMeshProUGUI for content
- Image for background
- CanvasGroup for fade effects

### Complete Suspect Tag Prefab
- **SuspectTag** component
- TextMeshProUGUI for full name
- Image for background
- CanvasGroup for drag effects

## Best Practices

### Design
- Use consistent visual styling across suspect tags
- Provide clear feedback for discovered information
- Make completion progress easily visible
- Use intuitive drag-and-drop interactions

### Performance
- Limit suspect discoveries per frame
- Use object pooling for frequent instantiation
- Cache citizen database lookups
- Minimize string operations in Update()

### User Experience
- Show clear progress indicators
- Provide satisfying discovery feedback
- Make draggable elements obvious
- Handle edge cases gracefully

## Future Enhancements

### Planned Features
- **Suspect Relationships**: Link suspects to each other
- **Evidence Linking**: Connect suspects to specific evidence
- **Timeline Integration**: Show suspect activities over time
- **Confidence Levels**: Indicate certainty of suspect information
- **Collaborative Discovery**: Team-based suspect building

### Technical Improvements
- **Event System**: Replace polling with event-driven updates
- **Serialization**: Save/load suspect discovery progress
- **Networking**: Multiplayer suspect discovery
- **AI Integration**: Automatic suspect suggestion

## Troubleshooting

### Common Issues
1. **Suspect not appearing**: Check CitizenDatabase for matching ID
2. **Tags not clickable**: Verify SuspectDraggableTag component
3. **Monitor assignment failing**: Check SuspectManager integration
4. **Progress not updating**: Ensure proper event subscriptions

### Debug Tools
- Enable `debugSuspectInfo` in SuspectsListManager
- Check Unity console for detailed logs
- Use Unity Profiler for performance analysis
- Inspect suspect data in runtime

## Conclusion

The Suspect Discovery System provides a rich, interactive way for players to gradually build their understanding of a case's suspects. By combining progressive discovery with intuitive UI elements, it creates an engaging investigative experience that encourages thorough exploration of clues and evidence.

The system is designed to be extensible, allowing for future enhancements while maintaining performance and usability. The modular architecture ensures easy integration with existing systems while providing clear separation of concerns. 