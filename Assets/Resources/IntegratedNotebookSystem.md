# Integrated Notebook System Documentation

## Overview

The Integrated Notebook System combines suspect discovery with a book-style notebook interface. The system has been redesigned to work like a real book with left and right pages, where the first page is reserved for suspects and subsequent pages are for clues.

## Key Changes from Previous System

### ✅ **Combined Management**
- **Removed**: Separate `NotebookTabManager`
- **Enhanced**: `NotebookManager` now handles both tabs and page management
- **Simplified**: Single source of truth for all notebook functionality

### 📖 **Book-Style Layout**
- **Left Page**: First page area for notes
- **Right Page**: Second page area for notes  
- **Page 1**: Reserved for suspects (left + right sides)
- **Page 2+**: For clues (fills left first, then right, then flips)

### 🔄 **Smart Page Management**
- Auto-flip when both sides of a page are full
- Tab navigation jumps to appropriate page type
- Navigation buttons for manual page browsing

## System Architecture

### **NotebookManager** (Enhanced)
```csharp
public class NotebookManager : MonoBehaviour
{
    // New References
    public RectTransform leftPageParent;   // Left page container  
    public RectTransform rightPageParent;  // Right page container
    
    // Tab Management
    public Button suspectsTab;
    public Button cluesTab;
    public TextMeshProUGUI suspectsTabText;
    public TextMeshProUGUI cluesTabText;
    public GameObject suspectsTabBadge;
    public GameObject cluesTabBadge;
    
    // Page Navigation
    public Button previousPageButton;
    public Button nextPageButton;
    public TextMeshProUGUI pageIndicatorText;
}
```

### **BookPage Class**
```csharp
public class BookPage
{
    public PageType pageType;                    // Suspects or Clues
    public List<GameObject> leftPageNotes;       // Notes on left side
    public List<GameObject> rightPageNotes;      // Notes on right side
    public int leftPageNoteCount;               // Count for left side
    public int rightPageNoteCount;              // Count for right side
}
```

### **Page Flow Logic**
1. **Suspects Page** (Page 1): Left side fills first, then right side
2. **Clues Pages** (Page 2+): Left side fills first, then right side, then auto-flip
3. **Tab Navigation**: 
   - Suspects tab → Jump to Page 1
   - Clues tab → Jump to last clues page

## Setup Instructions

### 1. **Notebook GameObject Setup**

```
NotebookPanel
├── LeftPageParent (RectTransform)
│   └── VerticalLayoutGroup
├── RightPageParent (RectTransform)  
│   └── VerticalLayoutGroup
├── TabsContainer
│   ├── SuspectsTab (Button)
│   │   ├── TabText (TextMeshProUGUI)
│   │   └── TabBadge (GameObject)
│   └── CluesTab (Button)
│       ├── TabText (TextMeshProUGUI)
│       └── TabBadge (GameObject)
└── NavigationContainer
    ├── PreviousButton (Button)
    ├── NextButton (Button)
    └── PageIndicator (TextMeshProUGUI)
```

### 2. **Component Configuration**

#### **NotebookManager Component**
- Assign `leftPageParent` and `rightPageParent`
- Set up tab button references
- Configure page navigation buttons
- Set `maxNotesPerPage` (default: 10 per side)

#### **VerticalLayoutGroup** (on both page parents)
```csharp
// Recommended settings
spacing = 10f
childControlWidth = true
childControlHeight = true  
childForceExpandWidth = true
childForceExpandHeight = false
```

### 3. **Prefab Requirements**

#### **Suspect Entry Prefab**
- Must have `SuspectEntry` component
- Portrait Image, Text fields, Completion squares
- Complete suspect tag container

#### **Clue Note Prefab**
- TextMeshProUGUI component
- LayoutElement for height management
- Proper RectTransform anchoring

## API Usage

### **Adding Suspects**
```csharp
// Automatic through SuspectsListManager
SuspectsListManager.Instance.DiscoverSuspectInfo("suspect_fname", "James");

// Direct addition (pre-assigned suspects)
notebookManager.AddSuspectEntry(suspectEntryGameObject);
```

### **Adding Clues**
```csharp
// Standard clue addition (auto-manages pages)
notebookManager.AddClueNote("Investigation note", "clue_001");

// Without auto-close (for interrogation)
notebookManager.AddClueNoteWithoutClosing("Interrogation note", "clue_002");
```

### **Tab Management**
```csharp
// Switch modes programmatically
notebookManager.SwitchToMode(NotebookManager.NotebookMode.Suspects);
notebookManager.SwitchToMode(NotebookManager.NotebookMode.Clues);
```

### **Page Navigation**
```csharp
// Manual navigation (also available via UI buttons)
notebookManager.PreviousPage();
notebookManager.NextPage();
```

## Visual Feedback

### **Tab Appearance**
- **Active Tab**: Scaled up, bright colors
- **Inactive Tab**: Normal scale, muted colors
- **Badges**: Show counts with completion status

### **Page Indicator**
- Shows: "Suspects Page 1 of 3" or "Clues Page 2 of 3"
- Updates automatically with navigation

### **Navigation Buttons**
- **Previous**: Disabled on first page
- **Next**: Disabled on last page
- Smooth transitions between pages

## Integration with Suspect System

### **Automatic Integration**
1. **SuspectsListManager** automatically uses NotebookManager
2. **Suspect discoveries** create entries on suspects page
3. **Complete suspects** generate draggable tags
4. **Tag counts** update in real-time

### **Data Flow**
```
Suspect Discovery → SuspectsListManager → NotebookManager → Suspects Page
```

## Best Practices

### **Performance**
- Use object pooling for frequent suspect creation
- Minimize layout rebuilds
- Cache references to avoid frequent lookups

### **UI Design**
- Keep page capacity reasonable (5-10 items per side)
- Ensure sufficient contrast for tab states
- Provide clear visual feedback for interactions

### **Content Management**
- Reserve suspects page for suspect entries only
- Use consistent spacing and alignment
- Handle page overflow gracefully

## Troubleshooting

### **Common Issues**

1. **Suspects not appearing**:
   - Check `leftPageParent` and `rightPageParent` assignments
   - Verify `SuspectsListManager` has `notebookManager` reference
   - Ensure suspect entry prefab has `SuspectEntry` component

2. **Pages not switching**:
   - Verify tab button assignments
   - Check `SwitchToMode()` method calls
   - Ensure page navigation buttons are connected

3. **Layout issues**:
   - Configure VerticalLayoutGroup on page parents
   - Set proper RectTransform anchoring
   - Check ContentSizeFitter settings

4. **Tab counts not updating**:
   - Verify `UpdateTabCounts()` is being called
   - Check `SuspectsListManager` singleton instance
   - Ensure proper event subscriptions

### **Debug Options**
```csharp
// Enable debug logging
notebookManager.debugLayoutInfo = true;
suspectsListManager.debugSuspectInfo = true;
```

## Migration from Old System

### **Required Changes**
1. **Remove**: `NotebookTabManager` component and scripts
2. **Update**: NotebookManager with new references
3. **Reconfigure**: UI hierarchy for left/right pages
4. **Update**: SuspectsListManager references

### **Compatibility**
- Existing clue system continues to work
- Old notebook methods remain functional
- Gradual migration possible

## Future Enhancements

### **Planned Features**
- **Page Templates**: Different layouts for different content types
- **Smart Pagination**: Content-aware page breaks
- **Animations**: Page flip animations and transitions
- **Bookmarks**: Quick navigation to important pages

### **Extensibility**
- Easy to add new page types
- Configurable page layouts
- Plugin system for custom content

This integrated system provides a cohesive, book-like notebook experience while maintaining the flexibility and power of the previous system with enhanced suspect discovery integration. 