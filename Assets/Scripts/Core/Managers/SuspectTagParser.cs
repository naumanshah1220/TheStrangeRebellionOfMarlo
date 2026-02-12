using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Static utility for parsing suspect tags from clue text.
/// Handles both nested format (<suspect><fname>...</fname></suspect>)
/// and individual format (<suspect_fname>...</suspect_fname>).
/// </summary>
public static class SuspectTagParser
{
    // Nested tag patterns (inside <suspect>...</suspect> blocks)
    private static readonly (string pattern, string fieldType)[] NestedPatterns =
    {
        (@"<fname>(.*?)</fname>", "suspect_fname"),
        (@"<lname>(.*?)</lname>", "suspect_lname"),
        (@"<id>(.*?)</id>", "suspect_id"),
        (@"<portrait>(.*?)</portrait>", "suspect_portrait"),
    };

    // Individual tag patterns (standalone tags)
    private static readonly (string pattern, string fieldType)[] IndividualPatterns =
    {
        (@"<suspect_fname>(.*?)</suspect_fname>", "suspect_fname"),
        (@"<suspect_lname>(.*?)</suspect_lname>", "suspect_lname"),
        (@"<suspect_id>(.*?)</suspect_id>", "suspect_id"),
        (@"<suspect_portrait>(.*?)</suspect_portrait>", "suspect_portrait"),
    };

    /// <summary>
    /// Check if clue text contains any suspect tags
    /// </summary>
    public static bool ContainsSuspectTags(string clueText)
    {
        return clueText.Contains("<suspect>") ||
               clueText.Contains("<suspect_fname>") ||
               clueText.Contains("<suspect_lname>") ||
               clueText.Contains("<suspect_id>") ||
               clueText.Contains("<suspect_portrait>");
    }

    /// <summary>
    /// Process suspect tags — convert nested structure to individual tags and remove portrait tags.
    /// Returns text suitable for display in the notebook clue note.
    /// </summary>
    public static string ProcessSuspectTags(string clueText)
    {
        string result = clueText;

        // Handle nested <suspect> tags
        var suspectMatches = Regex.Matches(result, @"<suspect>(.*?)</suspect>", RegexOptions.Singleline);
        foreach (Match match in suspectMatches)
        {
            string processed = ProcessNestedSuspectContent(match.Groups[1].Value);
            result = result.Replace(match.Value, processed);
        }

        // Handle remaining individual portrait tags
        result = RemovePortraitTags(result);

        return result;
    }

    /// <summary>
    /// Extract grouped suspect information from clue text.
    /// Each inner list represents one suspect's discovered fields.
    /// </summary>
    public static List<List<SuspectsListManager.SuspectDiscoveryInfo>> ExtractGroupedSuspectInformation(string clueText)
    {
        var suspectGroups = new List<List<SuspectsListManager.SuspectDiscoveryInfo>>();

        // Handle nested <suspect> tags (new format)
        var suspectMatches = Regex.Matches(clueText, @"<suspect>(.*?)</suspect>", RegexOptions.Singleline);
        foreach (Match match in suspectMatches)
        {
            var info = ExtractFieldsByPatterns(match.Groups[1].Value, NestedPatterns);
            if (info.Count > 0)
                suspectGroups.Add(info);
        }

        // Fallback to individual tags if no nested suspects found
        if (suspectGroups.Count == 0)
        {
            var info = ExtractFieldsByPatterns(clueText, IndividualPatterns);
            if (info.Count > 0)
                suspectGroups.Add(info);
        }

        return suspectGroups;
    }

    /// <summary>
    /// Process content inside a &lt;suspect&gt; tag — convert nested tags to individual suspect tags
    /// and remove portrait tags from visible text.
    /// </summary>
    private static string ProcessNestedSuspectContent(string suspectContent)
    {
        string result = suspectContent;

        result = Regex.Replace(result, @"<fname>(.*?)</fname>", @"<suspect_fname>$1</suspect_fname>");
        result = Regex.Replace(result, @"<lname>(.*?)</lname>", @"<suspect_lname>$1</suspect_lname>");
        result = Regex.Replace(result, @"<id>(.*?)</id>", @"<suspect_id>$1</suspect_id>");
        result = Regex.Replace(result, @"<portrait>(.*?)</portrait>", "");
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    /// <summary>
    /// Remove portrait tags from text (they shouldn't be displayed).
    /// </summary>
    private static string RemovePortraitTags(string text)
    {
        string result = Regex.Replace(text, @"<suspect_portrait>(.*?)</suspect_portrait>", "");
        result = Regex.Replace(result, @"\s+", " ").Trim();
        return result;
    }

    /// <summary>
    /// Shared helper: extract discovery info by matching an array of (regex, fieldType) pairs.
    /// </summary>
    private static List<SuspectsListManager.SuspectDiscoveryInfo> ExtractFieldsByPatterns(
        string text, (string pattern, string fieldType)[] patterns)
    {
        var discoveries = new List<SuspectsListManager.SuspectDiscoveryInfo>();

        foreach (var (pattern, fieldType) in patterns)
        {
            var matches = Regex.Matches(text, pattern);
            foreach (Match match in matches)
            {
                discoveries.Add(new SuspectsListManager.SuspectDiscoveryInfo
                {
                    fieldType = fieldType,
                    value = match.Groups[1].Value
                });
            }
        }

        return discoveries;
    }
}
