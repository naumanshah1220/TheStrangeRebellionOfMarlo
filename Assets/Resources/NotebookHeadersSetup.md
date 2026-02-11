# Notebook Headers Setup Guide

## Overview

The notebook system now supports header text elements that display contextual information based on the current page type:

- **Suspects Page**: Shows "Suspects" as header and "Case: [Case Name]" as sub-header
- **Clues Page**: Shows "Clues" as header and "Leads gathered so far:" as sub-header

## Setup Instructions

### Method 1: Automatic Setup (Recommended)

1. **Add the NotebookHeaderSetup Component**:
   - Select your NotebookManager GameObject in the scene
   - Add the `NotebookHeaderSetup` component
   - The headers will be automatically created on Awake

2. **Configure Header Settings**:
   - **Header Font Size**: Size of the main header text (default: 28)
   - **Sub Header Font Size**: Size of the sub-header text (default: 18)
   - **Header Color**: Color of the main header (default: Black)
   - **Sub Header Color**: Color of the sub-header (default: Gray)
   - **Header Positioning**: Adjust offset and spacing as needed

3. **Manual Setup** (if needed):
   - Right-click on the NotebookHeaderSetup component
   - Select "Setup Headers" from the context menu

### Method 2: Manual Setup

1. **Create Header Text Elements**:
   - In your notebook prefab, add TextMeshProUGUI components to both `leftPageParent` and `rightPageParent`
   - Name them appropriately (e.g., "LeftPageHeader", "LeftPageSubHeader", etc.)

2. **Assign References**:
   - In the NotebookManager component, assign the TextMeshProUGUI components to:
     - `leftPageHeaderText`
     - `leftPageSubHeaderText`
     - `rightPageHeaderText`
     - `rightPageSubHeaderText`

3. **Position the Headers**:
   - Set the headers to anchor to the top of their respective page parents
   - Position them at the top with appropriate spacing

## Header Content

### Suspects Page
- **Header**: "Suspects"
- **Sub-header**: "Case: [Current Case Name]"
  - Automatically updates when cases are opened/closed
  - Falls back to "Unknown Case" if no case is active

### Clues Page
- **Header**: "Clues"
- **Sub-header**: "Leads gathered so far:"

## Technical Details

### Automatic Updates
Headers are automatically updated when:
- Switching between suspects and clues modes
- Navigating between pages
- Opening/closing the notebook

### Manual Refresh
You can manually refresh headers by calling:
```csharp
notebookManager.RefreshPageHeaders();
```

### Case Name Resolution
The system tries to get the current case name from:
1. `SuspectManager.Instance.GetCurrentCase()`
2. `CaseManager.Instance.GetCurrentCase()`
3. Falls back to "Unknown Case"

## Customization

### Font and Styling
- Headers use the default TextMeshPro font asset
- Can be customized through the NotebookHeaderSetup component
- Supports rich text formatting

### Positioning
- Headers are positioned at the top of each page
- Use `ignoreLayout = true` to prevent layout system interference
- Can be adjusted through offset settings

### Content
- Header content is determined by the current page type
- Sub-header content varies based on page type and case information
- Can be extended to support additional page types

## Troubleshooting

### Headers Not Appearing
1. Check that TextMeshProUGUI components are assigned in NotebookManager
2. Verify that the header GameObjects are active
3. Ensure proper anchoring and positioning

### Case Name Not Updating
1. Verify that a case is currently active
2. Check that SuspectManager or CaseManager has the current case set
3. Call `RefreshPageHeaders()` manually if needed

### Layout Issues
1. Ensure headers have `ignoreLayout = true` on their LayoutElement
2. Check anchoring and positioning settings
3. Verify that headers don't interfere with content layout

## Integration Notes

- Headers work seamlessly with the existing notebook system
- No changes required to existing suspect or clue functionality
- Headers are automatically managed by the NotebookManager
- Supports the book-style page layout system 