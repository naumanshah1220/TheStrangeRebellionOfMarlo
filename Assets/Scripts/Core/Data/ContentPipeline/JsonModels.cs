using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plain C# POCOs for JSON deserialization of the content pipeline.
/// These map 1:1 to the JSON schema in StreamingAssets/content/.
/// </summary>

// ── Manifest ──────────────────────────────────────────────────────

[System.Serializable]
public class ContentManifest
{
    public string version;
    public List<DayScheduleEntry> daySchedule;
    public List<string> cases;
    public string lore;
    public string days;
}

[System.Serializable]
public class DayScheduleEntry
{
    public int day;
    public int maxCasesForDay = 5;
    public int maxCoreCases = 1;
    public int minSecondaryCases = 1;
}

// ── Case ──────────────────────────────────────────────────────────

[System.Serializable]
public class CaseJson
{
    // Identification
    public string caseID;
    public string title;
    public string description;
    public string caseType = "Core"; // "Core" or "Secondary"

    // Progression
    public int firstAvailableDay;
    public int coreSequenceNumber;
    public float reward = 50f;

    // Requirements
    public List<string> requiredPreviousCaseIds = new List<string>();
    public List<string> unlocksNextCaseIds = new List<string>();

    // Legal
    public string lawBroken;
    public float extraRewardForState;
    public float suspicionReduction;

    // Resistance
    public bool involvesResistance;
    public string resistanceChoice;
    public string stateChoice;

    // Suspects
    public List<CitizenJson> suspects = new List<CitizenJson>();
    public string culpritCitizenID;

    // Evidence
    public List<EvidenceJson> evidences = new List<EvidenceJson>();
    public List<EvidenceJson> extraEvidences = new List<EvidenceJson>();

    // Steps
    public List<CaseStepJson> steps = new List<CaseStepJson>();

    // Verdict
    public VerdictSchemaJson verdictSchema;
    public List<CaseSolutionJson> solutions = new List<CaseSolutionJson>();
    public int minDiscoveredCluesToAllowCommit = 3;
    public bool allowCommitWithLowConfidence = true;

    // Clue → Verdict option mappings (which clues reveal which verdict options)
    public List<ClueVerdictMappingJson> clueVerdictMappings;

    // Visuals
    public string cardImagePath;
    public string fullCardPrefabPath;
}

[System.Serializable]
public class ClueVerdictMappingJson
{
    public string clueId;
    public string slotId;
    public string optionId;
    public string label;
}

// ── Citizen ───────────────────────────────────────────────────────

[System.Serializable]
public class CitizenJson
{
    public string citizenID;
    public string firstName;
    public string lastName;

    // Personal
    public string picturePath;
    public string dateOfBirth;
    public string gender = "Male";
    public string ethnicity = "Caucasian";
    public string maritalStatus = "Single";
    public string address;
    public string occupation = "";

    // Criminal record
    public List<CriminalRecordJson> criminalHistory = new List<CriminalRecordJson>();

    // Biometrics
    public List<string> fingerprintPaths = new List<string>();

    // Interrogation
    public float nervousnessLevel = 0.3f;
    public float initialStress = -1f;
    public bool isGuilty;

    // Tag interactions
    public List<TagInteractionJson> tagInteractions = new List<TagInteractionJson>();

    // Generic questions/responses (optional override)
    public List<GenericQuestionJson> genericQuestions;
    public List<TagResponseJson> genericResponses;

    // Stress zone fallback responses
    public string[] lawyeredUpResponses;
    public string[] rattledResponses;
    public string[] shutdownResponses;
}

[System.Serializable]
public class CriminalRecordJson
{
    public string offense;
    public string date;
    public string description;
    public string severity = "Minor"; // Minor, Moderate, Major, Severe
}

[System.Serializable]
public class GenericQuestionJson
{
    public string tagType;
    public string[] questions;
}

// ── Evidence ──────────────────────────────────────────────────────

[System.Serializable]
public class EvidenceJson
{
    public string id;
    public string title;
    public string description;
    public string type = "Document"; // Document, Photo, Disc, Item
    public string cardImagePath;
    public string fullCardPrefabPath;

    // Spectrograph
    public string foreignSubstance = "None";

    // Disc evidence
    public string associatedAppId;

    // Hotspots (clickable clue regions on the evidence card)
    public List<EvidenceHotspotJson> hotspots;
}

// ── Evidence Hotspots ────────────────────────────────────────────

[System.Serializable]
public class EvidenceHotspotJson
{
    public string clueId;
    public string noteText;
    public int pageIndex;
    public float positionX = 0.5f;  // normalized 0-1
    public float positionY = 0.5f;  // normalized 0-1
    public float width = 0.3f;      // normalized 0-1
    public float height = 0.1f;     // normalized 0-1
}

// ── Dialogue / Interrogation ─────────────────────────────────────

[System.Serializable]
public class TagInteractionJson
{
    public string tagId;
    public string tagQuestion = "Tell me about '{tag}'";
    public List<TagResponseJson> responses = new List<TagResponseJson>();

    // Truth unlocking
    public List<string> unlocksTruthForTagIds = new List<string>();
    public TagResponseJson unlockedInitialResponseIfPreviouslyDenied;
    public TagResponseJson unlockedInitialResponseIfNotDenied;
    public List<TagResponseJson> unlockedFollowupResponses = new List<TagResponseJson>();

    // Evidence contradiction
    public List<string> contradictedByEvidenceTagIds;
    public TagResponseJson contradictionResponse;

    // Response variants
    public List<ResponseVariantGroupJson> responseVariants;
}

[System.Serializable]
public class TagResponseJson
{
    public string[] responseSequence;
    public bool isLie;
    public float responseDelayOverride;
    public List<ClickableClueSegmentJson> clickableClues = new List<ClickableClueSegmentJson>();

    // Interrogation graph
    public List<ResponseConditionJson> conditions;
    public float stressImpact;
    public string responseType;
}

[System.Serializable]
public class ResponseConditionJson
{
    public string type;     // ConditionType enum as string
    public string targetId;
    public int minCount = 1;
    public float threshold;
}

[System.Serializable]
public class ResponseVariantGroupJson
{
    public string variantId;
    public List<TagResponseJson> responses;
    public List<ResponseConditionJson> conditions;
    public float weight = 1f;
}

[System.Serializable]
public class ClickableClueSegmentJson
{
    public string clueId;
    public string clickableText;
    public string noteText;
    public ColorJson highlightColor;
    public bool oneTimeOnly = true;
}

[System.Serializable]
public class ColorJson
{
    public float r = 1f;
    public float g = 1f;
    public float b = 0f;
    public float a = 1f;

    public Color ToUnityColor() => new Color(r, g, b, a);
}

// ── Case Steps ───────────────────────────────────────────────────

[System.Serializable]
public class CaseStepJson
{
    public string stepId;
    public int stepNumber;
    public string description;
    public List<string> requiredClueIds = new List<string>();
    public List<string> unlockedClueIds = new List<string>();
}

// ── Verdict ──────────────────────────────────────────────────────

[System.Serializable]
public class VerdictSchemaJson
{
    public string sentenceTemplate;
    public List<VerdictSlotDefinitionJson> slots = new List<VerdictSlotDefinitionJson>();
    public JustificationDefinitionJson justification;
}

[System.Serializable]
public class VerdictSlotDefinitionJson
{
    public string slotId;
    public string displayLabel;
    public string type = "Suspect"; // VerdictSlotType enum as string
    public bool required = true;
    public int minJustificationTags;
    public string[] tagTypesAccepted;
    public string optionSource = "CaseAndGlobal"; // OptionSource enum as string
    public string customPoolId;
}

[System.Serializable]
public class JustificationDefinitionJson
{
    public bool required = true;
    public int minRequired = 1;
}

[System.Serializable]
public class CaseSolutionJson
{
    public List<SlotAnswerJson> answers = new List<SlotAnswerJson>();
    public string[] requiredJustificationTagIds;
    public string[] bonusJustificationTagIds;
    public int minConfidenceToApprove = 100;
}

[System.Serializable]
public class SlotAnswerJson
{
    public string slotId;
    public string[] acceptedOptionIds;
}

// ── Lore Slideshow ──────────────────────────────────────────────

[System.Serializable]
public class LoreJson
{
    public List<LoreSlideJson> slides = new List<LoreSlideJson>();
}

[System.Serializable]
public class LoreSlideJson
{
    public string title;
    public string bodyText;
    public string backgroundImagePath;
}

// ── Day Briefing ────────────────────────────────────────────────

[System.Serializable]
public class DaysBriefingDataJson
{
    public List<DayBriefingJson> days = new List<DayBriefingJson>();
}

[System.Serializable]
public class DayBriefingJson
{
    public int day;
    public string headline;
    public string subheadline;
    public FamilyLetterJson familyLetter;
    public List<string> unlockNotices = new List<string>();
    public List<CaseOutcomeHeadlineJson> caseOutcomeHeadlines = new List<CaseOutcomeHeadlineJson>();
}

[System.Serializable]
public class FamilyLetterJson
{
    public string from;
    public string body;
}

[System.Serializable]
public class CaseOutcomeHeadlineJson
{
    public string caseId;
    public string headlineIfSolved;
    public string headlineIfUnsolved;
    public int priority = 1;
}
