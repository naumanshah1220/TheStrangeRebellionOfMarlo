using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Response types that affect automatic behavior
/// </summary>
public enum ResponseType
{
    Normal,      // Standard response
    Defensive,   // Getting agitated
    Cooperative, // Willing to help
    Nervous,     // Anxious about something
    Hostile      // Angry/aggressive
}

public enum InterrogationTone { Calm, Neutral, Firm }

public enum StressZone
{
    LawyeredUp, // [0.0 - 0.2)
    Deflecting, // [0.2 - 0.4)
    SweetSpot,  // [0.4 - 0.7)
    Rattled,    // [0.7 - 0.8)
    Shutdown    // [0.8 - 1.0]
}

/// <summary>
/// Tag interaction system for suspect questioning
/// </summary>
[System.Serializable]
public class TagInteraction
{
    [Header("Tag Configuration")]
    public string tagId;
    public string tagQuestion = "Tell me about '{tag}'"; // Custom question for this tag
    
    [Header("Conversation State")]
    public int timesAsked = 0;
    public int currentResponseIndex = 0; // Which response array we're currently using
    
    [Header("Responses")]
    public TagResponse[] responses = new TagResponse[1]; // Array of possible response sets
    
    [Header("Discrepancy / Truth Unlocking")]
    [Tooltip("When THIS tag is asked/presented, permanently unlock truthful answers for these tag IDs")] 
    public string[] unlocksTruthForTagIds = new string[0];
    
    [Tooltip("If the player has unlocked truth for THIS tag and the suspect previously denied it, use this response once when they 'break'")]
    public TagResponse unlockedInitialResponseIfPreviouslyDenied;
    
    [Tooltip("If the player has unlocked truth for THIS tag before ever asking about it, use this calm truthful response once")]
    public TagResponse unlockedInitialResponseIfNotDenied;
    
    [Tooltip("Follow-up responses for subsequent times after truth has been unlocked for THIS tag")]
    public TagResponse[] unlockedFollowupResponses = new TagResponse[0];

    [Header("Evidence Contradiction")]
    [Tooltip("Evidence tag IDs that contradict this interaction's lies")]
    public string[] contradictedByEvidenceTagIds = new string[0];

    [Tooltip("Response when evidence contradicts this interaction")]
    public TagResponse contradictionResponse;

    [Header("Response Variants")]
    [Tooltip("Alternative response sets selected by conditions and weight")]
    public ResponseVariantGroup[] responseVariants;

    /// <summary>
    /// Get the current response based on how many times this tag has been asked
    /// </summary>
    public TagResponse GetCurrentResponse()
    {
        if (responses.Length == 0) return new TagResponse();
        
        // If asked more times than we have responses, use the last response
        int responseIndex = Mathf.Min(timesAsked, responses.Length - 1);
        return responses[responseIndex];
    }
    
    /// <summary>
    /// Get the formatted question with tag name inserted
    /// </summary>
    public string GetFormattedQuestion()
    {
        return tagQuestion.Replace("{tag}", tagId);
    }
}

/// <summary>
/// A response that can contain multiple sequential messages
/// </summary>
[System.Serializable]
public class TagResponse
{
    [Header("Response Content")]
    [Tooltip("Multiple responses that play in sequence (e.g., 'Oh man..', 'I think I know him', 'Yes I definitely know him')")]
    public string[] responseSequence = new string[1] { "I don't know anything about that." };
    
    [Header("Response Properties")]
    public bool isLie = false;
    public float responseDelayOverride = 0f;
    
    [Header("Clickable Clue Segments")]
    [Tooltip("Define clickable parts of the response that add notes to the notebook")]
    public ClickableClueSegment[] clickableClues = new ClickableClueSegment[0];

    [Header("Interrogation Graph")]
    [Tooltip("All conditions must be met for this response to be eligible")]
    public ResponseCondition[] conditions;

    [Tooltip("Stress delta applied to citizen when this response is delivered")]
    public float stressImpact = 0f;

    public ResponseType responseType = ResponseType.Normal;

    /// <summary>
    /// Get all responses as a single string separated by periods
    /// </summary>
    public string GetCombinedResponse()
    {
        return string.Join(". ", responseSequence);
    }
    
    /// <summary>
    /// Check if this response has multiple parts
    /// </summary>
    public bool HasMultipleParts => responseSequence.Length > 1;
    
    /// <summary>
    /// Check if this response has clickable clue segments
    /// </summary>
    public bool HasClickableClues => clickableClues != null && clickableClues.Length > 0;
}

/// <summary>
/// Generic questions for different tag types
/// </summary>
[System.Serializable]
public class GenericQuestion
{
    public string tagType; // person, location, item, etc.
    public string[] questions = new string[] 
    { 
        "Tell me about '{tag}'",
        "What do you know about '{tag}'?",
        "Do you know anything about '{tag}'?"
    };
    
    /// <summary>
    /// Get a random question for this tag type
    /// </summary>
    public string GetRandomQuestion(string tagName)
    {
        if (questions.Length == 0) return $"Tell me about '{tagName}'";
        
        string question = questions[Random.Range(0, questions.Length)];
        return question.Replace("{tag}", tagName);
    }
}

/// <summary>
/// Message data for conversation history
/// </summary>
[System.Serializable]
public class ConversationMessage
{
    public string messageText;
    public bool isPlayerMessage;
    public string timestamp;
    public bool isLie;
    public ResponseType responseType;
    
    public ConversationMessage(string text, bool isPlayer, bool lie = false, ResponseType type = ResponseType.Normal)
    {
        messageText = text;
        isPlayerMessage = isPlayer;
        isLie = lie;
        responseType = type;
        timestamp = System.DateTime.Now.ToString("HH:mm");
    }
}

/// <summary>
/// Full conversation data for a suspect
/// </summary>
[System.Serializable]
public class CitizenConversation
{
    public Citizen citizen;
    public List<ConversationMessage> messages = new List<ConversationMessage>();
}

/// <summary>
/// A condition that must be met for a response or variant to be eligible
/// </summary>
[System.Serializable]
public class ResponseCondition
{
    public enum ConditionType
    {
        TagAsked,           // tagId asked >= minCount times
        TagNotAsked,        // tagId never asked
        ClueDiscovered,     // clueId found
        ClueNotDiscovered,  // clueId NOT found
        TruthUnlocked,      // truth unlocked for tagId
        StressAbove,        // citizen stress >= threshold
        StressBelow         // citizen stress < threshold
    }
    public ConditionType type;
    public string targetId;
    public int minCount = 1;
    public float threshold = 0f;
}

/// <summary>
/// A group of variant responses with conditions and weighted selection
/// </summary>
[System.Serializable]
public class ResponseVariantGroup
{
    public string variantId;
    public TagResponse[] responses;
    public ResponseCondition[] conditions;
    public float weight = 1f;
}

/// <summary>
/// Defines a clickable clue segment within a chat response
/// Similar to evidence card hotspots but for chat text
/// </summary>
[System.Serializable]
public class ClickableClueSegment
{
    [Header("Clue Identification")]
    [Tooltip("Unique identifier for this clue")]
    public string clueId;
    
    [Header("Text Matching")]
    [Tooltip("The exact text in the response that should be clickable (case-insensitive)")]
    public string clickableText;
    
    [Header("Notebook Entry")]
    [Tooltip("The note text to add to notebook when clicked (can use tags like <person>James</person>)")]
    [TextArea(3, 8)]
    public string noteText;
    
    [Header("Visual Settings")]
    [Tooltip("Color to highlight the clickable text")]
    public Color highlightColor = Color.yellow;
    
    [Tooltip("Whether this clue can only be clicked once")]
    public bool oneTimeOnly = true;
    
    // Runtime positioning data (set during text processing)
    [System.NonSerialized]
    public int frameStartIndex = -1; // Start position of the frame in the spaced text
    
    [System.NonSerialized]
    public int actualTextLength = 0; // Length of the actual clickable text
    
    /// <summary>
    /// Check if the clickable text exists in the given response text
    /// </summary>
    public bool ExistsInText(string responseText)
    {
        return !string.IsNullOrEmpty(clickableText) && 
               responseText.IndexOf(clickableText, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
} 