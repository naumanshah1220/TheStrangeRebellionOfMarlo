using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves string paths from JSON to Unity assets via Resources.Load.
/// All paths are relative to a Resources/ folder.
/// </summary>
public static class ResourceResolver
{
    public static Sprite LoadSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var sprite = Resources.Load<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"[ResourceResolver] Sprite not found: Resources/{path}");
        return sprite;
    }

    public static GameObject LoadPrefab(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
            Debug.LogWarning($"[ResourceResolver] Prefab not found: Resources/{path}");
        return prefab;
    }

    public static AppConfig LoadAppConfig(string appId)
    {
        if (string.IsNullOrEmpty(appId)) return null;

        // Search all loaded AppConfigs by ID
        var configs = Resources.LoadAll<AppConfig>("");
        foreach (var config in configs)
        {
            if (config.AppId == appId) return config;
        }
        Debug.LogWarning($"[ResourceResolver] AppConfig not found for appId: {appId}");
        return null;
    }

    /// <summary>
    /// Validates that all assets referenced by a case JSON exist in Resources/.
    /// Logs warnings for missing assets. Call during content authoring/debug.
    /// </summary>
    public static void ValidateCaseAssets(CaseJson caseData)
    {
        if (caseData == null) return;

        var missing = new List<string>();
        string caseId = caseData.caseID;

        // Case card image
        string caseCardPath = caseData.cardImagePath ?? $"Cases/{caseId}/card";
        if (Resources.Load<Sprite>(caseCardPath) == null)
            missing.Add($"Case card: Resources/{caseCardPath}");

        // Evidence
        if (caseData.evidences != null)
        {
            foreach (var ev in caseData.evidences)
                ValidateEvidenceAssets(ev, caseId, missing);
        }
        if (caseData.extraEvidences != null)
        {
            foreach (var ev in caseData.extraEvidences)
                ValidateEvidenceAssets(ev, caseId, missing);
        }

        // Suspects
        if (caseData.suspects != null)
        {
            foreach (var s in caseData.suspects)
            {
                string portraitPath = s.picturePath ?? $"Portraits/{s.citizenID}";
                if (Resources.Load<Sprite>(portraitPath) == null)
                    missing.Add($"Portrait: Resources/{portraitPath}");

                // Fingerprints (check convention paths if none specified)
                if (s.fingerprintPaths != null && s.fingerprintPaths.Count > 0)
                {
                    foreach (var fp in s.fingerprintPaths)
                    {
                        if (Resources.Load<Sprite>(fp) == null)
                            missing.Add($"Fingerprint: Resources/{fp}");
                    }
                }
                else
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        string fpPath = $"Fingerprints/{s.citizenID}_finger_{i}";
                        if (Resources.Load<Sprite>(fpPath) == null)
                            missing.Add($"Fingerprint: Resources/{fpPath}");
                    }
                }
            }
        }

        if (missing.Count == 0)
        {
            Debug.Log($"[ResourceResolver] Case '{caseId}': all assets found.");
        }
        else
        {
            Debug.LogWarning($"[ResourceResolver] Case '{caseId}': {missing.Count} missing asset(s):\n  - " +
                string.Join("\n  - ", missing));
        }
    }

    private static void ValidateEvidenceAssets(EvidenceJson ev, string caseId, List<string> missing)
    {
        string cardPath = ev.cardImagePath ?? $"Evidence/{caseId}/{ev.id}_card";
        if (Resources.Load<Sprite>(cardPath) == null)
            missing.Add($"Evidence card: Resources/{cardPath}");

        // Full card prefab is optional — fallback is used when null
        if (!string.IsNullOrEmpty(ev.fullCardPrefabPath))
        {
            if (Resources.Load<GameObject>(ev.fullCardPrefabPath) == null)
                missing.Add($"Evidence prefab: Resources/{ev.fullCardPrefabPath}");
        }
    }
}
