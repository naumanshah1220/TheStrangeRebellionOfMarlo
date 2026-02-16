using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic; // Added for List

/// <summary>
/// Represents a single suspect entry in the suspects list with progressive discovery
/// </summary>
public class SuspectEntry : MonoBehaviour
{
    [Header("UI References")]
    public Image portraitImage;
    public TextMeshProUGUI citizenIdText;
    public TextMeshProUGUI firstNameText;
    public TextMeshProUGUI lastNameText;
    
    [Header("Completion Squares")]
    public Image[] completionSquares = new Image[4]; // 0=portrait, 1=citizenId, 2=firstName, 3=lastName
    
    [Header("Visual Settings")]
    public Color filledSquareColor = Color.green;
    public Color unfilledSquareColor = Color.gray;
    public Sprite unknownPortrait; // Placeholder image
    
    [Header("Complete Suspect")]
    public GameObject completeSuspectTagPrefab; // The <suspect> tag that appears when complete
    public RectTransform completeSuspectContainer; // Where to instantiate the complete tag
    
    [Header("Animation Settings")]
    public float fadeInDuration = 1.0f;
    public bool startInvisible = false; // Whether to start the entry invisible for fade-in
    public float fadeInDelay = 0.0f; // Delay before starting fade-in animation
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Custom easing curve
    
    // Data
    private SuspectData suspectData;
    private bool isComplete = false;
    private GameObject completeSuspectTag;
    private CanvasGroup canvasGroup;
    private bool hasAnimatedIn = false;
    
    /// <summary>
    /// Data structure for suspect information
    /// </summary>
    [System.Serializable]
    public class SuspectData
    {
        public string suspectId; // Unique identifier for this suspect entry
        public Sprite portrait;
        public string citizenId = "???";
        public string firstName = "???";
        public string lastName = "???";
        
        // Completion tracking
        public bool hasPortrait = false;
        public bool hasCitizenId = false;
        public bool hasFirstName = false;
        public bool hasLastName = false;
        
        public SuspectData(string id)
        {
            suspectId = id;
        }
        
        public bool IsComplete()
        {
            return hasPortrait && hasCitizenId && hasFirstName && hasLastName;
        }
        
        public int GetCompletionCount()
        {
            int count = 0;
            if (hasPortrait) count++;
            if (hasCitizenId) count++;
            if (hasFirstName) count++;
            if (hasLastName) count++;
            return count;
        }
    }
    
    private void Awake()
    {
        if (suspectData == null)
        {
            suspectData = new SuspectData(System.Guid.NewGuid().ToString());
        }
        
        // Set up CanvasGroup for fade-in animation
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Start invisible if configured to do so
        if (startInvisible)
        {
            canvasGroup.alpha = 0f;
        }
        
        // Ensure completeSuspectContainer is properly set up
        SetupCompleteSuspectContainer();
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Ensure the completeSuspectContainer is properly positioned and doesn't overlap with main content
    /// </summary>
    private void SetupCompleteSuspectContainer()
    {
        if (completeSuspectContainer == null)
        {
            // Create a new container for the SuspectTag if it doesn't exist
            GameObject containerObj = new GameObject("CompleteSuspectContainer");
            completeSuspectContainer = containerObj.AddComponent<RectTransform>();
            completeSuspectContainer.SetParent(transform, false);
            
            // Position it to not overlap with the main content
            completeSuspectContainer.anchorMin = new Vector2(0, 0);
            completeSuspectContainer.anchorMax = new Vector2(1, 1);
            completeSuspectContainer.anchoredPosition = Vector2.zero;
            completeSuspectContainer.sizeDelta = Vector2.zero;
            
            // Set it as the last sibling so it renders on top
            completeSuspectContainer.SetAsLastSibling();
            
            Debug.Log("[SuspectEntry] Created completeSuspectContainer");
        }
        else
        {
            // Ensure existing container is properly positioned
            completeSuspectContainer.SetAsLastSibling();
        }
    }
    
    /// <summary>
    /// Initialize with existing suspect data
    /// </summary>
    public void Initialize(SuspectData data)
    {
        suspectData = data;
        UpdateVisuals();
    }
    
    /// <summary>
    /// Initialize with a citizen (pre-assigned suspects)
    /// </summary>
    public void Initialize(Citizen citizen)
    {
        if (citizen == null) return;
        
        Debug.Log($"[SuspectEntry] Initializing with citizen: {citizen.FullName} (ID: {citizen.citizenID})");
        Debug.Log($"[SuspectEntry] Citizen data - FirstName: '{citizen.firstName}', LastName: '{citizen.lastName}', Picture: {citizen.picture != null}");
        
        suspectData = new SuspectData(citizen.citizenID);
        suspectData.portrait = citizen.picture;
        suspectData.citizenId = citizen.citizenID;
        suspectData.firstName = citizen.firstName;
        suspectData.lastName = citizen.lastName;
        
        // Mark all fields as complete for pre-assigned suspects
        // Portrait: if no sprite was loaded, still mark as "has" for pre-assigned suspects
        // (they'll show the unknown/placeholder portrait but won't block interrogation)
        suspectData.hasPortrait = true;
        suspectData.hasCitizenId = !string.IsNullOrEmpty(citizen.citizenID);
        suspectData.hasFirstName = !string.IsNullOrEmpty(citizen.firstName);
        suspectData.hasLastName = !string.IsNullOrEmpty(citizen.lastName);
        
        Debug.Log($"[SuspectEntry] SuspectData after initialization:");
        Debug.Log($"[SuspectEntry] - FirstName: '{suspectData.firstName}' (hasFirstName: {suspectData.hasFirstName})");
        Debug.Log($"[SuspectEntry] - LastName: '{suspectData.lastName}' (hasLastName: {suspectData.hasLastName})");

        Debug.Log($"[SuspectEntry] - Portrait: {suspectData.portrait != null} (hasPortrait: {suspectData.hasPortrait})");
        Debug.Log($"[SuspectEntry] - IsComplete: {suspectData.IsComplete()}");
        
        UpdateVisuals();
        
        // Check completion to create SuspectTag for pre-assigned suspects
        CheckCompletion();
    }
    
    /// <summary>
    /// Update suspect data from discovered information
    /// </summary>
    public bool UpdateField(string fieldType, string value, Sprite portraitSprite = null)
    {
        bool wasUpdated = false;
        
        switch (fieldType.ToLower())
        {
            case "suspect_fname":
            case "firstname":
                if (!suspectData.hasFirstName)
                {
                    suspectData.firstName = value;
                    suspectData.hasFirstName = true;
                    wasUpdated = true;
                }
                break;
                
            case "suspect_lname":
            case "lastname":
                if (!suspectData.hasLastName)
                {
                    suspectData.lastName = value;
                    suspectData.hasLastName = true;
                    wasUpdated = true;
                }
                break;
                
            case "suspect_id":
            case "citizenid":
                if (!suspectData.hasCitizenId)
                {
                    suspectData.citizenId = value;
                    suspectData.hasCitizenId = true;
                    wasUpdated = true;
                    
                    // If we now have a citizen ID, try to load their portrait from the database
                    if (portraitSprite == null)
                    {
                        TryLoadPortraitFromDatabase();
                    }
                }
                break;
                
            case "suspect_portrait":
            case "portrait":
                if (!suspectData.hasPortrait && portraitSprite != null)
                {
                    suspectData.portrait = portraitSprite;
                    suspectData.hasPortrait = true;
                    wasUpdated = true;
                }
                break;
        }
        
        if (wasUpdated)
        {
            UpdateVisuals();
            CheckCompletion();
        }
        
        return wasUpdated;
    }
    
    /// <summary>
    /// Try to load portrait from citizen database if we have a citizen ID
    /// </summary>
    private void TryLoadPortraitFromDatabase()
    {
        if (suspectData.hasCitizenId && !suspectData.hasPortrait)
        {
            // Try to find citizen in database via SuspectManager
            var suspectManager = SuspectManager.Instance;
            if (suspectManager != null && suspectManager.citizenDatabase != null)
            {
                var citizen = suspectManager.citizenDatabase.GetCitizenById(suspectData.citizenId);
                if (citizen != null && citizen.picture != null)
                {
                    suspectData.portrait = citizen.picture;
                    suspectData.hasPortrait = true;
                    UpdateVisuals();
                }
            }
        }
    }
    
    /// <summary>
    /// Update the visual representation
    /// </summary>
    private void UpdateVisuals()
    {
        // Update portrait
        if (portraitImage != null)
        {
            portraitImage.sprite = suspectData.hasPortrait ? suspectData.portrait : unknownPortrait;
        }
        
        // Update text fields
        if (citizenIdText != null)
            citizenIdText.text = suspectData.hasCitizenId ? suspectData.citizenId : "???";
        
        if (firstNameText != null)
            firstNameText.text = suspectData.hasFirstName ? suspectData.firstName : "???";
        
        if (lastNameText != null)
            lastNameText.text = suspectData.hasLastName ? suspectData.lastName : "???";
        
        // Update completion squares
        UpdateCompletionSquares();
    }
    
    /// <summary>
    /// Update the completion squares visual
    /// </summary>
    private void UpdateCompletionSquares()
    {
        if (completionSquares == null || completionSquares.Length < 4) return;
        
        // Create a list of completion status for sorting
        var completionStatus = new List<(bool isComplete, int index)>
        {
            (suspectData.hasPortrait, 0),      // Portrait
            (suspectData.hasCitizenId, 1),     // Citizen ID
            (suspectData.hasFirstName, 2),     // First Name
            (suspectData.hasLastName, 3)       // Last Name
        };
        
        // Sort by completion status: completed first, then incomplete
        completionStatus.Sort((a, b) => b.isComplete.CompareTo(a.isComplete));
        
        // Update squares based on sorted order
        for (int i = 0; i < completionSquares.Length && i < completionStatus.Count; i++)
        {
            if (completionSquares[i] != null)
            {
                completionSquares[i].color = completionStatus[i].isComplete ? filledSquareColor : unfilledSquareColor;
            }
        }
        

    }
    
    /// <summary>
    /// Check if suspect is complete and create complete tag if needed
    /// </summary>
    private void CheckCompletion()
    {
        bool wasComplete = isComplete;
        isComplete = suspectData.IsComplete();
        
        Debug.Log($"[SuspectEntry] CheckCompletion - wasComplete: {wasComplete}, isComplete: {isComplete}");

        
        if (isComplete && !wasComplete)
        {
            Debug.Log("[SuspectEntry] Suspect became complete, creating SuspectTag...");
            CreateCompleteSuspectTag();
        }
        else if (!isComplete && completeSuspectTag != null)
        {
            Debug.Log("[SuspectEntry] Suspect became incomplete, destroying SuspectTag...");
            Destroy(completeSuspectTag);
            completeSuspectTag = null;
        }
    }
    
    /// <summary>
    /// Create the complete suspect tag that can be dragged to monitors
    /// </summary>
    private void CreateCompleteSuspectTag()
    {
        Debug.Log($"[SuspectEntry] CreateCompleteSuspectTag called - prefab: {completeSuspectTagPrefab != null}, container: {completeSuspectContainer != null}");
        
        if (completeSuspectTagPrefab == null || completeSuspectContainer == null) 
        {
            Debug.LogError("[SuspectEntry] Missing completeSuspectTagPrefab or completeSuspectContainer!");
            return;
        }
        
        // Destroy existing tag if any
        if (completeSuspectTag != null)
        {
            Destroy(completeSuspectTag);
        }
        
        // Create new complete suspect tag
        completeSuspectTag = Instantiate(completeSuspectTagPrefab, completeSuspectContainer);
        Debug.Log($"[SuspectEntry] Created SuspectTag GameObject: {completeSuspectTag != null}");
        
        // Initialize the tag with suspect data
        var suspectTagComponent = completeSuspectTag.GetComponent<SuspectTag>();
        if (suspectTagComponent != null)
        {
            Debug.Log($"[SuspectEntry] Found SuspectTag component, initializing with data...");
            suspectTagComponent.Initialize(suspectData);
        }
        else
        {
            Debug.LogError("[SuspectEntry] SuspectTag component not found on completeSuspectTagPrefab!");
        }
        
        Debug.Log($"[SuspectEntry] Created complete suspect tag for: {suspectData.firstName} {suspectData.lastName}");
    }
    
    /// <summary>
    /// Get the suspect data
    /// </summary>
    public SuspectData GetSuspectData()
    {
        return suspectData;
    }
    
    /// <summary>
    /// Check if this entry matches any of the given identifiers
    /// </summary>
    public bool MatchesIdentifier(string identifier)
    {
        return suspectData.suspectId == identifier ||
               (suspectData.hasCitizenId && suspectData.citizenId == identifier) ||
               (suspectData.hasFirstName && suspectData.firstName.Equals(identifier, System.StringComparison.OrdinalIgnoreCase)) ||
               (suspectData.hasLastName && suspectData.lastName.Equals(identifier, System.StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Get citizen from database if complete
    /// </summary>
    public Citizen GetCitizen()
    {
        if (!isComplete || !suspectData.hasCitizenId) return null;
        
        var suspectManager = SuspectManager.Instance;
        if (suspectManager != null && suspectManager.citizenDatabase != null)
        {
            return suspectManager.citizenDatabase.GetCitizenById(suspectData.citizenId);
        }
        
        return null;
    }
    
    /// <summary>
    /// Start the fade-in animation
    /// </summary>
    public void StartFadeInAnimation()
    {
        if (hasAnimatedIn) return;
        
        hasAnimatedIn = true;
        StartCoroutine(FadeInAnimation());
    }
    
    /// <summary>
    /// Fade-in animation coroutine
    /// </summary>
    private System.Collections.IEnumerator FadeInAnimation()
    {
        if (canvasGroup == null) yield break;
        
        // Wait for delay
        if (fadeInDelay > 0f)
        {
            yield return new WaitForSeconds(fadeInDelay);
        }
        
        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;
        float targetAlpha = 1f;
        
        while (Time.time < startTime + fadeInDuration)
        {
            float t = (Time.time - startTime) / fadeInDuration;
            float curveValue = fadeInCurve.Evaluate(t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
    }
    
    /// <summary>
    /// Check if the fade-in animation has completed
    /// </summary>
    public bool HasAnimatedIn()
    {
        return hasAnimatedIn;
    }
    
    /// <summary>
    /// Manually trigger fade-in animation (useful for testing or external control)
    /// </summary>
    public void TriggerFadeIn()
    {
        if (!hasAnimatedIn)
        {
            StartFadeInAnimation();
        }
    }
} 