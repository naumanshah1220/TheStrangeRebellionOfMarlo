using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ForeignSubstanceDatabase", menuName = "Game/Spectrograph/Foreign Substance Database")]
public class ForeignSubstanceDatabase : ScriptableObject
{
    [Serializable]
    public struct BandSpec
    {
        public bool enabled;           // whether this spectral band is shown
        [Range(6f, 28f)] public float width; // band thickness in UI pixels
    }

    [Serializable]
    public struct SubstanceEntry
    {
        public ForeignSubstanceType substance;
        [Tooltip("7 band specs in order: Red, Orange, Yellow, Green, Blue, Indigo, Violet")]
        public BandSpec[] bands; // length 7
    }

    [Serializable]
    public struct BandPattern
    {
        public bool[] enabled; // length 7
        public float[] widths;  // length 7
    }

    [Header("Spectrograph Substance Library")]
    [Header("Substances (bands: Red, Orange, Yellow, Green, Blue, Indigo, Violet)")]
    public List<SubstanceEntry> substances = new List<SubstanceEntry>();

    private Dictionary<ForeignSubstanceType, SubstanceEntry> _map;

    private void OnEnable()
    {
        BuildMap();
    }

    private void BuildMap()
    {
        _map = new Dictionary<ForeignSubstanceType, SubstanceEntry>();
        for (int i = 0; i < substances.Count; i++)
        {
            var entry = substances[i];
            if (entry.bands == null || entry.bands.Length != 7)
            {
                entry.bands = new BandSpec[7];
                substances[i] = entry;
            }
            if (!_map.ContainsKey(entry.substance))
            {
                _map.Add(entry.substance, entry);
            }
            else
            {
                _map[entry.substance] = entry; // last wins
            }
        }
    }

    public bool TryGetBandPattern(ForeignSubstanceType substance, out BandPattern pattern)
    {
        if (_map == null || _map.Count != substances.Count)
        {
            BuildMap();
        }
        if (_map != null && _map.TryGetValue(substance, out var entry))
        {
            var enabled = new bool[7];
            var widths = new float[7];
            for (int i = 0; i < 7; i++)
            {
                var b = (entry.bands != null && entry.bands.Length > i) ? entry.bands[i] : default;
                enabled[i] = b.enabled;
                widths[i] = Mathf.Clamp(b.width <= 0 ? 12f : b.width, 6f, 28f);
            }
            pattern = new BandPattern { enabled = enabled, widths = widths };
            return true;
        }
        pattern = default;
        return false;
    }
}


