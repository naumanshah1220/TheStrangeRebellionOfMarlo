# Book and Report Hand Setup Guide

## Overview
This guide explains how to set up the new Book and Report card types and their corresponding hands in your detective game.

## New Card Types Added

### 1. Book Cards (CardMode.Book)
- **ScriptableObject**: `Book.cs`
- **Properties**: id, title, description, fullCardPrefab, cardImage, author, publisher, publicationYear, isbn
- **Hand Type**: `HolderType.Book`

### 2. Report Cards (CardMode.Report)
- **ScriptableObject**: `Report.cs`
- **Properties**: id, title, description, fullCardPrefab, cardImage, author, department, reportDate, caseNumber, reportType
- **Hand Type**: `HolderType.Overseer`

## Setup Instructions

### Step 1: Create Book Hands
1. Create 2 GameObject children under your main UI canvas
2. Add `HorizontalCardHolder` component to each
3. Set `HolderType` to `Book`
4. Set `CardMode` to `Book`
5. Configure visual handlers and slot prefabs (same as Evidence hands)

### Step 2: Create Overseer Hand
1. Create 1 GameObject child under your main UI canvas
2. Add `HorizontalCardHolder` component
3. Set `HolderType` to `Overseer`
4. Set `CardMode` to `Report`
5. Configure visual handlers and slot prefabs

### Step 3: Configure CardTypeManager
1. Find your CardTypeManager in the scene
2. Add new entries to `cardTypeMappings`:
   - **Book Mapping**: CardMode.Book → Book Hand 1
   - **Book Mapping**: CardMode.Book → Book Hand 2 (if you want separate hands)
   - **Report Mapping**: CardMode.Report → Overseer Hand

**Important**: The BookManager will automatically register Book card types with CardTypeManager when it starts up, but you should still configure the mappings manually in the inspector for better control.

### Step 4: Set up BookManager
1. Create a GameObject in your scene
2. Add the `BookManager` component
3. Assign your two book hands to `bookHand1` and `bookHand2`
4. Add Book assets to the `booksForHand1` and `booksForHand2` lists in the inspector

### Step 5: Configure GameManager Debug Menu
1. Find your GameManager in the scene
2. In the Debug Menu section, assign a Button to `debugAddBooksButton`
3. The button will automatically call `BookManager.AddAllBooksToHands()` when clicked

### Step 6: Create Sample Assets
1. Use the `BookTestHelper` script:
   - Add it to any GameObject in the scene
   - Right-click and select "Create Sample Books"
   - This creates 4 sample books in `Assets/Resources/Books/`
2. Or use the `ReportAssetCreator` script for reports
3. Drag the created assets into the BookManager's book lists

### Step 7: Test the System
1. Click the "Add Books" button in the debug menu
2. Books should appear in their respective hands
3. Test dragging books to the mat and back
4. Verify that books return to their original hands

## BookManager Features

### Methods Available:
- `AddAllBooksToHands()`: Loads all books from both lists into their hands
- `AddBooksToHand(int handIndex, List<Book> books)`: Add books to a specific hand
- `ClearAllBookHands()`: Remove all books from both hands
- `GetBookCounts()`: Returns the number of books in each hand
- `ValidateBookHands()`: Checks if hands are properly configured

### Editor Context Menus:
- Right-click on BookManager component for quick access to:
  - "Validate Book Hands"
  - "Add All Books to Hands"
  - "Clear All Book Hands"

## Behavior
- **In Hands**: Show small card visuals (like Evidence cards)
- **On Mat**: Show full card visuals when moved to the mat
- **MatMixed**: Both Book and Report cards can be placed on the mat
- **Return Logic**: Cards return to their original hands when removed from mat

## Integration
The new card types automatically integrate with:
- CardTypeManager routing system
- DragManager for card movement
- Mat system for investigation area
- Case closing routines
- Visual cleanup systems
- GameManager debug menu

## Notes
- Books and Reports follow the same pattern as Evidence cards
- They implement `ICardData` interface for consistency
- The system is extensible - you can easily add more card types following this pattern
- BookManager provides centralized management for book collections
