using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple test helper for EnhancedCardVisual system
/// Add this to a card to quickly test the torn puzzle functionality
/// </summary>
public class EnhancedCardTestHelper : MonoBehaviour
{
    [Header("Test Setup")]
    public bool setupTestCard = false;
    public Sprite testBigTornSprite;      // Renamed from testEnhancedBigSprite
    public Sprite testSmallTornSprite;    // Renamed from testEnhancedSmallSprite
    public GameObject[] testPiecePrefabs = new GameObject[9]; // BigCardVisual prefabs for 3x3 grid
    
    private void Start()
    {
        if (setupTestCard)
        {
            SetupTestCard();
        }
    }
    
    private void SetupTestCard()
    {
        // Check if this is on a BigCardVisual or Card
        BigCardVisual bigCardVisual = GetComponent<BigCardVisual>();
        Card card = GetComponent<Card>();
        
        if (bigCardVisual == null && card == null)
        {
            Debug.LogError("[EnhancedCardTestHelper] No BigCardVisual or Card component found!");
            return;
        }
        
        // Add EnhancedCardVisual component to the appropriate GameObject
        GameObject targetObject = bigCardVisual != null ? bigCardVisual.gameObject : card.gameObject;
        EnhancedCardVisual enhanced = targetObject.GetComponent<EnhancedCardVisual>();
        if (enhanced == null)
        {
            enhanced = targetObject.AddComponent<EnhancedCardVisual>();
        }
        
        // Setup as torn card
        enhanced.cardType = EnhancedCardType.Torn;
        enhanced.big_TornCardWhenHovering = testBigTornSprite;
        enhanced.small_TornCard = testSmallTornSprite;
        
        // Setup piece prefabs (use available prefabs or duplicate the first one)
        enhanced.piecePrefabs.Clear();
        for (int i = 0; i < 9; i++) // 3x3 grid
        {
            if (i < testPiecePrefabs.Length && testPiecePrefabs[i] != null)
            {
                enhanced.piecePrefabs.Add(testPiecePrefabs[i]);
            }
            else if (testPiecePrefabs.Length > 0 && testPiecePrefabs[0] != null)
            {
                // Use first prefab as fallback
                enhanced.piecePrefabs.Add(testPiecePrefabs[0]);
            }
            else
            {
                Debug.LogWarning($"[EnhancedCardTestHelper] No prefab available for piece {i}");
            }
        }
        
        // Setup puzzle settings for testing
        enhanced.puzzleSettings.gridSize = 3;
        enhanced.puzzleSettings.pieceSnapDistance = 100f; // Larger distance for easier testing
        enhanced.puzzleSettings.glowDuration = 2f; // Longer glow for visibility
        enhanced.puzzleSettings.glowColor = Color.green;
        
        string targetType = bigCardVisual != null ? "BigCardVisual" : "Card";
        Debug.Log($"[EnhancedCardTestHelper] Test card setup complete on {targetType} with {enhanced.piecePrefabs.Count} piece prefabs");
    }
    
    [ContextMenu("Setup Test Card")]
    public void SetupTestCardFromMenu()
    {
        SetupTestCard();
    }
} 