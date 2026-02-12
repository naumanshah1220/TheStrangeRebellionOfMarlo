using System;

/// <summary>
/// Central static event bus for decoupling cross-manager communication.
/// Managers raise events here instead of calling other managers directly.
/// </summary>
public static class GameEvents
{
    // Day lifecycle
    public static event Action<int> OnDayStarted;
    public static event Action OnDayEnded;

    // Case lifecycle
    public static event Action<string> OnCaseOpened;
    public static event Action<string> OnCaseClosed;
    public static event Action<Case> OnCaseSolved;

    // Clue discovery
    public static event Action<string> OnClueDiscovered;

    // --- Raise helpers ---

    public static void RaiseDayStarted(int dayNumber)
    {
        OnDayStarted?.Invoke(dayNumber);
    }

    public static void RaiseDayEnded()
    {
        OnDayEnded?.Invoke();
    }

    public static void RaiseCaseOpened(string caseId)
    {
        OnCaseOpened?.Invoke(caseId);
    }

    public static void RaiseCaseClosed(string caseId)
    {
        OnCaseClosed?.Invoke(caseId);
    }

    public static void RaiseCaseSolved(Case solvedCase)
    {
        OnCaseSolved?.Invoke(solvedCase);
    }

    public static void RaiseClueDiscovered(string clueId)
    {
        OnClueDiscovered?.Invoke(clueId);
    }

    /// <summary>
    /// Unsubscribe all listeners. Call on application quit or full reset.
    /// </summary>
    public static void ClearAll()
    {
        OnDayStarted = null;
        OnDayEnded = null;
        OnCaseOpened = null;
        OnCaseClosed = null;
        OnCaseSolved = null;
        OnClueDiscovered = null;
    }
}
