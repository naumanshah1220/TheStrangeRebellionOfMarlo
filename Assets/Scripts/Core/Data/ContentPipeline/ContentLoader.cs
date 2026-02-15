using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/// <summary>
/// Reads StreamingAssets/content/ JSON files and produces runtime ScriptableObject instances.
/// Returns a ContentLoadResult that callers (CaseManager, DaysManager) consume.
/// </summary>
public static class ContentLoader
{
    private const string ContentRoot = "content";
    private const string ManifestFile = "manifest.json";

    public class ContentLoadResult
    {
        public ContentManifest manifest;
        public List<Case> cases = new List<Case>();
    }

    // Cache so multiple callers (CaseManager.Awake, DaysManager.Start) don't re-read files
    private static ContentLoadResult _cachedResult;
    private static bool _hasLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _cachedResult = null;
        _hasLoaded = false;
    }

    private static JsonSerializerSettings CreateSettings()
    {
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
        settings.Converters.Add(new StringEnumConverter());
        settings.Converters.Add(new ColorJsonConverter());
        return settings;
    }

    /// <summary>
    /// Load all content from StreamingAssets/content/manifest.json.
    /// Returns null if the manifest is missing (graceful fallback to SO-only mode).
    /// </summary>
    public static ContentLoadResult LoadAllContent()
    {
        if (_hasLoaded) return _cachedResult;
        _hasLoaded = true;

        string manifestPath = Path.Combine(Application.streamingAssetsPath, ContentRoot, ManifestFile);

        if (!File.Exists(manifestPath))
        {
            Debug.Log("[ContentLoader] No manifest.json found — running in SO-only mode.");
            return null;
        }

        var settings = CreateSettings();
        ContentManifest manifest;

        try
        {
            string json = File.ReadAllText(manifestPath);
            manifest = JsonConvert.DeserializeObject<ContentManifest>(json, settings);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ContentLoader] Failed to parse manifest.json: {e.Message}");
            return null;
        }

        Debug.Log($"[ContentLoader] Loaded manifest v{manifest.version} with {manifest.cases?.Count ?? 0} case(s).");

        var result = new ContentLoadResult { manifest = manifest };

        if (manifest.cases == null)
        {
            _cachedResult = result;
            return result;
        }

        string contentDir = Path.Combine(Application.streamingAssetsPath, ContentRoot);

        foreach (string casePath in manifest.cases)
        {
            string fullPath = Path.Combine(contentDir, casePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[ContentLoader] Case file not found: {casePath}");
                continue;
            }

            try
            {
                string caseJson = File.ReadAllText(fullPath);
                var caseData = JsonConvert.DeserializeObject<CaseJson>(caseJson, settings);
                ResourceResolver.ValidateCaseAssets(caseData);
                Case runtimeCase = ConvertToCase(caseData);
                result.cases.Add(runtimeCase);
                Debug.Log($"[ContentLoader] Loaded case: {runtimeCase.caseID} — \"{runtimeCase.title}\"");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ContentLoader] Failed to load case {casePath}: {e.Message}");
            }
        }

        _cachedResult = result;
        return result;
    }

    // ── Case conversion ──────────────────────────────────────────

    private static Case ConvertToCase(CaseJson json)
    {
        var c = ScriptableObject.CreateInstance<Case>();
        c.name = json.caseID;

        // Identification
        c.caseID = json.caseID;
        c.title = json.title;
        c.description = json.description;
        c.caseType = ParseEnum(json.caseType, CaseType.Core);

        // Progression
        c.firstAvailableDay = json.firstAvailableDay;
        c.coreSequenceNumber = json.coreSequenceNumber;
        c.reward = json.reward;

        // Requirements
        c.requiredPreviousCaseIds = json.requiredPreviousCaseIds ?? new List<string>();
        c.unlocksNextCaseIds = json.unlocksNextCaseIds ?? new List<string>();

        // Legal
        c.lawBroken = json.lawBroken;
        c.extraRewardForState = json.extraRewardForState;
        c.suspicionReduction = json.suspicionReduction;

        // Resistance
        c.involvesResistance = json.involvesResistance;
        c.resistanceChoice = json.resistanceChoice;
        c.stateChoice = json.stateChoice;

        // Suspects
        c.suspects = new List<Citizen>();
        Citizen culprit = null;
        foreach (var sj in json.suspects)
        {
            var citizen = ConvertToCitizen(sj);
            c.suspects.Add(citizen);
            if (sj.citizenID == json.culpritCitizenID)
                culprit = citizen;
        }
        c.culprit = culprit;

        // Evidence
        c.evidences = ConvertEvidenceList(json.evidences);
        c.extraEvidences = ConvertEvidenceList(json.extraEvidences);

        // Steps
        c.steps = ConvertSteps(json.steps);

        // Verdict
        c.verdictSchema = ConvertVerdictSchema(json.verdictSchema);
        c.solutions = ConvertSolutions(json.solutions);
        c.minDiscoveredCluesToAllowCommit = json.minDiscoveredCluesToAllowCommit;
        c.allowCommitWithLowConfidence = json.allowCommitWithLowConfidence;

        // Visuals
        c.cardImage = ResourceResolver.LoadSprite(json.cardImagePath);
        c.fullCardPrefab = ResourceResolver.LoadPrefab(json.fullCardPrefabPath);

        return c;
    }

    // ── Citizen conversion ───────────────────────────────────────

    private static Citizen ConvertToCitizen(CitizenJson json)
    {
        var citizen = ScriptableObject.CreateInstance<Citizen>();
        citizen.name = json.citizenID;

        citizen.citizenID = json.citizenID;
        citizen.firstName = json.firstName;
        citizen.lastName = json.lastName;
        citizen.dateOfBirth = json.dateOfBirth;
        citizen.gender = ParseEnum(json.gender, Gender.Male);
        citizen.ethnicity = ParseEnum(json.ethnicity, Ethnicity.Caucasian);
        citizen.maritalStatus = ParseEnum(json.maritalStatus, MaritalStatus.Single);
        citizen.address = json.address;
        citizen.occupation = json.occupation ?? "";
        citizen.nervousnessLevel = json.nervousnessLevel;
        citizen.initialStress = json.initialStress;
        citizen.isGuilty = json.isGuilty;

        // Portrait
        if (!string.IsNullOrEmpty(json.picturePath))
            citizen.picture = ResourceResolver.LoadSprite(json.picturePath);
        else
            citizen.picture = ResourceResolver.LoadSprite($"Portraits/{json.citizenID}");

        // Fingerprints
        citizen.fingerprints = new List<Sprite>();
        if (json.fingerprintPaths != null && json.fingerprintPaths.Count > 0)
        {
            foreach (var fp in json.fingerprintPaths)
                citizen.fingerprints.Add(ResourceResolver.LoadSprite(fp));
        }
        else
        {
            // Default convention: Fingerprints/{citizenID}_finger_1..5
            for (int i = 1; i <= 5; i++)
            {
                var sprite = Resources.Load<Sprite>($"Fingerprints/{json.citizenID}_finger_{i}");
                if (sprite != null) citizen.fingerprints.Add(sprite);
            }
        }

        // Criminal history
        citizen.criminalHistory = new List<CriminalRecord>();
        if (json.criminalHistory != null)
        {
            foreach (var cr in json.criminalHistory)
            {
                citizen.criminalHistory.Add(new CriminalRecord
                {
                    offense = cr.offense,
                    date = cr.date,
                    description = cr.description,
                    severity = ParseEnum(cr.severity, CrimeSeverity.Minor)
                });
            }
        }

        // Tag interactions
        citizen.tagInteractions = ConvertTagInteractions(json.tagInteractions);

        // Generic questions
        if (json.genericQuestions != null && json.genericQuestions.Count > 0)
        {
            citizen.genericQuestions = new GenericQuestion[json.genericQuestions.Count];
            for (int i = 0; i < json.genericQuestions.Count; i++)
            {
                citizen.genericQuestions[i] = new GenericQuestion
                {
                    tagType = json.genericQuestions[i].tagType,
                    questions = json.genericQuestions[i].questions
                };
            }
        }
        // else: leave the field initializer defaults from Citizen.cs

        // Generic responses
        if (json.genericResponses != null && json.genericResponses.Count > 0)
        {
            citizen.genericResponses = new TagResponse[json.genericResponses.Count];
            for (int i = 0; i < json.genericResponses.Count; i++)
                citizen.genericResponses[i] = ConvertTagResponse(json.genericResponses[i]);
        }

        // Stress zone fallback responses
        citizen.lawyeredUpResponses = json.lawyeredUpResponses ?? new string[0];
        citizen.rattledResponses = json.rattledResponses ?? new string[0];
        citizen.shutdownResponses = json.shutdownResponses ?? new string[0];

        return citizen;
    }

    // ── Evidence conversion ──────────────────────────────────────

    private static List<Evidence> ConvertEvidenceList(List<EvidenceJson> jsonList)
    {
        var result = new List<Evidence>();
        if (jsonList == null) return result;

        foreach (var ej in jsonList)
        {
            var ev = ScriptableObject.CreateInstance<Evidence>();
            ev.name = ej.id;
            ev.id = ej.id;
            ev.title = ej.title;
            ev.description = ej.description;
            ev.type = ParseEnum(ej.type, EvidenceType.Document);
            ev.cardImage = ResourceResolver.LoadSprite(ej.cardImagePath);
            ev.fullCardPrefab = ResourceResolver.LoadPrefab(ej.fullCardPrefabPath);
            ev.foreignSubstance = ParseEnum(ej.foreignSubstance, ForeignSubstanceType.None);

            // Disc evidence app config — uses reflection to set private serialized field
            if (!string.IsNullOrEmpty(ej.associatedAppId))
            {
                var appConfig = ResourceResolver.LoadAppConfig(ej.associatedAppId);
                if (appConfig != null)
                    SetPrivateField(ev, "associatedApp", appConfig);
            }

            result.Add(ev);
        }
        return result;
    }

    // ── Tag interaction conversion ───────────────────────────────

    private static TagInteraction[] ConvertTagInteractions(List<TagInteractionJson> jsonList)
    {
        if (jsonList == null || jsonList.Count == 0)
            return new TagInteraction[0];

        var result = new TagInteraction[jsonList.Count];
        for (int i = 0; i < jsonList.Count; i++)
        {
            var tj = jsonList[i];
            var ti = new TagInteraction
            {
                tagId = tj.tagId,
                tagQuestion = tj.tagQuestion ?? "Tell me about '{tag}'"
            };

            // Responses
            if (tj.responses != null && tj.responses.Count > 0)
            {
                ti.responses = new TagResponse[tj.responses.Count];
                for (int r = 0; r < tj.responses.Count; r++)
                    ti.responses[r] = ConvertTagResponse(tj.responses[r]);
            }
            else
            {
                ti.responses = new TagResponse[] { new TagResponse() };
            }

            // Truth unlocking
            ti.unlocksTruthForTagIds = tj.unlocksTruthForTagIds?.ToArray() ?? new string[0];

            // Unlocked initial responses
            if (tj.unlockedInitialResponseIfPreviouslyDenied != null)
                ti.unlockedInitialResponseIfPreviouslyDenied = ConvertTagResponse(tj.unlockedInitialResponseIfPreviouslyDenied);

            if (tj.unlockedInitialResponseIfNotDenied != null)
                ti.unlockedInitialResponseIfNotDenied = ConvertTagResponse(tj.unlockedInitialResponseIfNotDenied);

            // Unlocked followups
            if (tj.unlockedFollowupResponses != null && tj.unlockedFollowupResponses.Count > 0)
            {
                ti.unlockedFollowupResponses = new TagResponse[tj.unlockedFollowupResponses.Count];
                for (int f = 0; f < tj.unlockedFollowupResponses.Count; f++)
                    ti.unlockedFollowupResponses[f] = ConvertTagResponse(tj.unlockedFollowupResponses[f]);
            }
            else
            {
                ti.unlockedFollowupResponses = new TagResponse[0];
            }

            // Evidence contradiction
            ti.contradictedByEvidenceTagIds = tj.contradictedByEvidenceTagIds?.ToArray() ?? new string[0];
            if (tj.contradictionResponse != null)
                ti.contradictionResponse = ConvertTagResponse(tj.contradictionResponse);

            // Response variants
            ti.responseVariants = ConvertResponseVariantGroups(tj.responseVariants);

            result[i] = ti;
        }
        return result;
    }

    private static TagResponse ConvertTagResponse(TagResponseJson json)
    {
        if (json == null) return new TagResponse();

        var tr = new TagResponse
        {
            responseSequence = json.responseSequence ?? new string[] { "" },
            isLie = json.isLie,
            responseDelayOverride = json.responseDelayOverride
        };

        if (json.clickableClues != null && json.clickableClues.Count > 0)
        {
            tr.clickableClues = new ClickableClueSegment[json.clickableClues.Count];
            for (int i = 0; i < json.clickableClues.Count; i++)
            {
                var cj = json.clickableClues[i];
                tr.clickableClues[i] = new ClickableClueSegment
                {
                    clueId = cj.clueId,
                    clickableText = cj.clickableText,
                    noteText = cj.noteText,
                    highlightColor = cj.highlightColor != null ? cj.highlightColor.ToUnityColor() : Color.yellow,
                    oneTimeOnly = cj.oneTimeOnly
                };
            }
        }
        else
        {
            tr.clickableClues = new ClickableClueSegment[0];
        }

        // Interrogation graph fields
        tr.conditions = ConvertResponseConditions(json.conditions);
        tr.stressImpact = json.stressImpact;
        tr.responseType = ParseEnum(json.responseType, ResponseType.Normal);

        return tr;
    }

    // ── Response condition conversion ────────────────────────────

    private static ResponseCondition[] ConvertResponseConditions(List<ResponseConditionJson> jsonList)
    {
        if (jsonList == null || jsonList.Count == 0) return null;

        var result = new ResponseCondition[jsonList.Count];
        for (int i = 0; i < jsonList.Count; i++)
        {
            var cj = jsonList[i];
            result[i] = new ResponseCondition
            {
                type = ParseEnum(cj.type, ResponseCondition.ConditionType.TagAsked),
                targetId = cj.targetId,
                minCount = cj.minCount,
                threshold = cj.threshold
            };
        }
        return result;
    }

    private static ResponseVariantGroup[] ConvertResponseVariantGroups(List<ResponseVariantGroupJson> jsonList)
    {
        if (jsonList == null || jsonList.Count == 0) return null;

        var result = new ResponseVariantGroup[jsonList.Count];
        for (int i = 0; i < jsonList.Count; i++)
        {
            var vj = jsonList[i];
            var group = new ResponseVariantGroup
            {
                variantId = vj.variantId,
                weight = vj.weight,
                conditions = ConvertResponseConditions(vj.conditions)
            };

            if (vj.responses != null && vj.responses.Count > 0)
            {
                group.responses = new TagResponse[vj.responses.Count];
                for (int r = 0; r < vj.responses.Count; r++)
                    group.responses[r] = ConvertTagResponse(vj.responses[r]);
            }
            else
            {
                group.responses = new TagResponse[0];
            }

            result[i] = group;
        }
        return result;
    }

    // ── Steps conversion ─────────────────────────────────────────

    private static List<CaseStep> ConvertSteps(List<CaseStepJson> jsonList)
    {
        var result = new List<CaseStep>();
        if (jsonList == null) return result;

        foreach (var sj in jsonList)
        {
            result.Add(new CaseStep
            {
                stepId = sj.stepId,
                stepNumber = sj.stepNumber,
                description = sj.description,
                requiredClueIds = sj.requiredClueIds ?? new List<string>(),
                unlockedClueIds = sj.unlockedClueIds ?? new List<string>()
            });
        }
        return result;
    }

    // ── Verdict conversion ───────────────────────────────────────

    private static VerdictSchema ConvertVerdictSchema(VerdictSchemaJson json)
    {
        if (json == null) return null;

        var schema = ScriptableObject.CreateInstance<VerdictSchema>();
        schema.sentenceTemplate = json.sentenceTemplate ?? "";

        schema.slots = new List<VerdictSlotDefinition>();
        if (json.slots != null)
        {
            foreach (var sj in json.slots)
            {
                schema.slots.Add(new VerdictSlotDefinition
                {
                    slotId = sj.slotId,
                    displayLabel = sj.displayLabel,
                    type = ParseEnum(sj.type, VerdictSlotType.Suspect),
                    required = sj.required,
                    minJustificationTags = sj.minJustificationTags,
                    tagTypesAccepted = sj.tagTypesAccepted,
                    optionSource = ParseEnum(sj.optionSource, OptionSource.CaseAndGlobal),
                    customPoolId = sj.customPoolId
                });
            }
        }

        if (json.justification != null)
        {
            schema.justification = new JustificationDefinition
            {
                required = json.justification.required,
                minRequired = json.justification.minRequired
            };
        }

        // globalPools stays null for JSON cases — only SO-authored schemas have pre-built pools
        return schema;
    }

    private static CaseSolution[] ConvertSolutions(List<CaseSolutionJson> jsonList)
    {
        if (jsonList == null || jsonList.Count == 0)
            return new CaseSolution[0];

        var result = new CaseSolution[jsonList.Count];
        for (int i = 0; i < jsonList.Count; i++)
        {
            var sj = jsonList[i];
            var solution = new CaseSolution
            {
                requiredJustificationTagIds = sj.requiredJustificationTagIds,
                bonusJustificationTagIds = sj.bonusJustificationTagIds,
                minConfidenceToApprove = sj.minConfidenceToApprove
            };

            solution.answers = new List<CaseSolution.SlotAnswer>();
            if (sj.answers != null)
            {
                foreach (var aj in sj.answers)
                {
                    solution.answers.Add(new CaseSolution.SlotAnswer
                    {
                        slotId = aj.slotId,
                        acceptedOptionIds = aj.acceptedOptionIds
                    });
                }
            }

            result[i] = solution;
        }
        return result;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static T ParseEnum<T>(string value, T defaultValue) where T : struct
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        if (Enum.TryParse<T>(value, true, out var parsed))
            return parsed;
        Debug.LogWarning($"[ContentLoader] Unknown enum value '{value}' for {typeof(T).Name}, using {defaultValue}");
        return defaultValue;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
            field.SetValue(target, value);
    }
}
