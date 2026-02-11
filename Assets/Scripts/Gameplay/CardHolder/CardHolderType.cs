/// <summary>
/// Defines the visual behavior of a card holder
/// </summary>
public enum VisualMode
{
    SmallCards,  // Shows small card visuals (like in hands)
    BigCards     // Shows big card visuals (like on mat)
}

/// <summary>
/// Defines what types of cards a holder can accept
/// </summary>
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

/// <summary>
/// Defines the functional purpose of a card holder
/// </summary>
public enum HolderPurpose
{
    Hand,               // Regular card hand
    Mat,                // Investigation mat
    Computer,           // Computer disc slot
    FingerPrintDuster,  // Fingerprint duster tool
    Spectrograph,       // Spectrograph analysis tool
    BookShelf,          // Book storage
    ReportFile          // Report storage
}
