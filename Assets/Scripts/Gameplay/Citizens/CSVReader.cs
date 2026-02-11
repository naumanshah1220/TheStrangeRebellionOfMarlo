using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class CSVReader
{
    private static readonly string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
    private static readonly string LINE_SPLIT_RE = @"\r\n|\n\r|\n|\r";
    private static readonly char[] TRIM_CHARS = { '\"' };

    /// <summary>
    /// Read CSV data from a TextAsset
    /// </summary>
    public static List<Dictionary<string, object>> Read(TextAsset csvFile)
    {
        if (csvFile == null)
        {
            Debug.LogError("[CSVReader] CSV file is null");
            return new List<Dictionary<string, object>>();
        }

        return Read(csvFile.text);
    }

    /// <summary>
    /// Read CSV data from a string
    /// </summary>
    public static List<Dictionary<string, object>> Read(string csvText)
    {
        var list = new List<Dictionary<string, object>>();
        var lines = Regex.Split(csvText, LINE_SPLIT_RE);

        if (lines.Length <= 1)
        {
            Debug.LogWarning("[CSVReader] CSV has no data rows");
            return list;
        }

        var header = Regex.Split(lines[0], SPLIT_RE);
        for (var i = 1; i < lines.Length; i++)
        {
            var values = Regex.Split(lines[i], SPLIT_RE);
            if (values.Length == 0 || values[0] == "") continue;

            var entry = new Dictionary<string, object>();
            for (var j = 0; j < header.Length && j < values.Length; j++)
            {
                string value = values[j];
                value = value.TrimStart(TRIM_CHARS).TrimEnd(TRIM_CHARS).Replace("\"\"", "\"");
                entry[header[j]] = value;
            }
            list.Add(entry);
        }
        return list;
    }

    /// <summary>
    /// Parse criminal records from a semicolon-separated string
    /// Format: "offense1|date1|description1|severity1;offense2|date2|description2|severity2"
    /// </summary>
    public static List<CriminalRecord> ParseCriminalRecords(string criminalRecordsString)
    {
        var records = new List<CriminalRecord>();
        
        if (string.IsNullOrEmpty(criminalRecordsString))
            return records;

        // Split by semicolon to get individual records
        var recordStrings = criminalRecordsString.Split(';');
        
        foreach (var recordString in recordStrings)
        {
            if (string.IsNullOrEmpty(recordString.Trim()))
                continue;

            // Split by pipe to get record components
            var parts = recordString.Split('|');
            
            if (parts.Length >= 3)
            {
                var record = new CriminalRecord
                {
                    offense = parts[0].Trim(),
                    date = parts[1].Trim(),
                    description = parts[2].Trim()
                };

                // Parse severity if provided
                if (parts.Length >= 4)
                {
                    if (System.Enum.TryParse<CrimeSeverity>(parts[3].Trim(), true, out CrimeSeverity severity))
                    {
                        record.severity = severity;
                    }
                }

                records.Add(record);
            }
        }

        return records;
    }

    /// <summary>
    /// Parse enum value from string with fallback
    /// </summary>
    public static T ParseEnum<T>(string value, T defaultValue) where T : struct, System.Enum
    {
        if (System.Enum.TryParse<T>(value, true, out T result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Parse float value from string with fallback
    /// </summary>
    public static float ParseFloat(string value, float defaultValue = 0f)
    {
        if (float.TryParse(value, out float result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Parse bool value from string with fallback
    /// </summary>
    public static bool ParseBool(string value, bool defaultValue = false)
    {
        if (bool.TryParse(value, out bool result))
        {
            return result;
        }
        
        // Handle common string representations
        string lower = value.ToLower().Trim();
        if (lower == "yes" || lower == "true" || lower == "1")
            return true;
        if (lower == "no" || lower == "false" || lower == "0")
            return false;
            
        return defaultValue;
    }
} 