/// <summary>
/// Static flags for passing data between scenes (MainMenu → Detective 6.0).
/// No DontDestroyOnLoad needed — just static fields.
/// </summary>
public static class FlowBootstrap
{
    public static bool WasInitialized { get; private set; }
    public static bool ShouldShowSlideshow { get; private set; }
    public static int ContinueFromDay { get; private set; }

    public static void SetNewGame()
    {
        WasInitialized = true;
        ShouldShowSlideshow = true;
        ContinueFromDay = 1;
    }

    public static void SetContinueGame(int day)
    {
        WasInitialized = true;
        ShouldShowSlideshow = false;
        ContinueFromDay = day;
    }

    public static void Reset()
    {
        WasInitialized = false;
        ShouldShowSlideshow = false;
        ContinueFromDay = 1;
    }
}
