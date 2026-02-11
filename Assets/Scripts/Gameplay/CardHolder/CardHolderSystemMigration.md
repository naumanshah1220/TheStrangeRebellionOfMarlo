# Card Holder System Migration Guide

## Current Confusion
The current system uses confusing naming:
- `HolderType` mixes visual behavior with functional purpose
- `CardMode` is used for both card types and holder behavior
- `CardTypeManager` adds a third layer of complexity

## Proposed New System

### 1. **VisualMode** - How cards are displayed
```csharp
public enum VisualMode
{
    SmallCards,  // Shows small card visuals (like in hands)
    BigCards     // Shows big card visuals (like on mat)
}
```

### 2. **AcceptedCardTypes** - What cards can be placed here
```csharp
public enum AcceptedCardTypes
{
    Cases,           // Only case cards
    Evidence,        // Only evidence cards
    Books,           // Only book cards
    Reports,         // Only report cards
    Phones,          // Only phone cards
    Mixed,           // Evidence + Books + Reports + Phones (but NOT cases)
    All              // All card types
}
```

### 3. **HolderPurpose** - What this holder is for
```csharp
public enum HolderPurpose
{
    Hand,               // Regular card hand
    Mat,                // Investigation mat
    Computer,           // Computer disc slot
    FingerPrintDuster,  // Fingerprint duster tool
    BookShelf,          // Book storage
    ReportFile          // Report storage
}
```

## Migration Examples

### **Current → New**

| Current Setup | New Setup |
|---------------|-----------|
| `type = HolderType.Hand, mode = CardMode.Case` | `visualMode = SmallCards, acceptedTypes = Cases, purpose = Hand` |
| `type = HolderType.Hand, mode = CardMode.Evidence` | `visualMode = SmallCards, acceptedTypes = Evidence, purpose = Hand` |
| `type = HolderType.Mat, mode = CardMode.MatMixed` | `visualMode = BigCards, acceptedTypes = Mixed, purpose = Mat` |
| `type = HolderType.Book, mode = CardMode.Book` | `visualMode = SmallCards, acceptedTypes = Books, purpose = BookShelf` |
| `type = HolderType.Computer, mode = CardMode.Evidence` | `visualMode = BigCards, acceptedTypes = Evidence, purpose = Computer` |

## Benefits

### **1. Clear Intent**
- **VisualMode**: Immediately tells you how cards will look
- **AcceptedCardTypes**: Clearly states what can go here
- **HolderPurpose**: Explains the functional role

### **2. Easier Configuration**
```csharp
// Instead of guessing what combination works:
// type = HolderType.Hand, mode = CardMode.Book

// You explicitly set each aspect:
visualMode = VisualMode.SmallCards;      // Cards will be small
acceptedTypes = AcceptedCardTypes.Books;  // Only books allowed
purpose = HolderPurpose.BookShelf;        // This is for storing books
```

### **3. Better Validation**
```csharp
// Easy to validate configurations:
if (purpose == HolderPurpose.BookShelf && acceptedTypes != AcceptedCardTypes.Books)
{
    Debug.LogError("BookShelf should only accept Books!");
}
```

### **4. Simplified Logic**
```csharp
// Instead of complex type checking:
if (type == HolderType.Hand && mode == CardMode.Evidence)

// Simple, clear checks:
if (visualMode == VisualMode.SmallCards && acceptedTypes == AcceptedCardTypes.Evidence)
```

## Implementation Status

### **✅ Phase 1: Add New Properties (COMPLETED)**
- ✅ Added new enums to HorizontalCardHolder
- ✅ Kept old properties for backward compatibility
- ✅ Added validation to ensure consistency
- ✅ Added helper methods for the new system

### **🔄 Phase 2: Update Logic (IN PROGRESS)**
- ✅ Updated LoadCasesOnDayStart() to use new system
- ✅ Updated evidence hand setup logic
- ✅ Updated mat placement logic
- ✅ Updated EnsureProperVisualForHolder() method
- ✅ Updated AddCardToHand() method
- ⏳ Update CardTypeManager to use new enums
- ⏳ Update DragManager routing logic

### **⏳ Phase 3: Remove Old System (PENDING)**
- ⏳ Remove old HolderType and CardMode properties
- ⏳ Clean up all references
- ⏳ Update documentation

## Current Implementation Details

### **New Properties Added to HorizontalCardHolder:**
```csharp
[Header("New Card Holder System")]
[Tooltip("How cards are displayed in this holder")]
public VisualMode visualMode = VisualMode.SmallCards;

[Tooltip("What types of cards this holder can accept")]
public AcceptedCardTypes acceptedCardTypes = AcceptedCardTypes.Cases;

[Tooltip("The functional purpose of this holder")]
public HolderPurpose purpose = HolderPurpose.Hand;
```

### **Helper Methods Added:**
- `CanAcceptCardType(CardMode cardMode)` - Check if holder accepts a card type
- `ShowsBigCards()` - Check if holder shows big visuals
- `ShowsSmallCards()` - Check if holder shows small visuals
- `GetVisualModeDescription()` - Get human-readable description
- `GetAcceptedCardTypesDescription()` - Get human-readable description
- `GetPurposeDescription()` - Get human-readable description

### **Validation Added:**
- Automatic validation on Start() to catch configuration errors
- Warnings for logical inconsistencies (e.g., BookShelf not accepting Books)

### **Test Script Added:**
- `CardHolderSystemTest.cs` - Comprehensive test script to validate configurations

## Questions for Consideration

1. **Should we keep CardTypeManager?** Or can we simplify routing with the new system?
2. **Should we add more AcceptedCardTypes?** Like `CasesAndEvidence` for specific combinations?
3. **Should we add more VisualModes?** Like `MediumCards` for special cases?

This new system would make the codebase much more intuitive for new developers and easier to maintain!
