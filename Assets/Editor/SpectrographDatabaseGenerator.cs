using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SpectrographDatabaseGenerator : EditorWindow
{
    private const string DatabasePath = "Assets/Scripts/Core/Data/SpectrographDatabase/SpectrographDatabase.asset";

    [MenuItem("Tools/Spectrograph Database Generator")]
    public static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        SpectrographDatabaseGenerator window = (SpectrographDatabaseGenerator)GetWindow(typeof(SpectrographDatabaseGenerator));
        window.Show();
    }

    void OnGUI()
    {
        if (GUILayout.Button("Generate Database"))
        {
            Generate();
        }
    }

    private static void Generate()
    {
        // Ensure the directory exists
        string directoryPath = System.IO.Path.GetDirectoryName(DatabasePath);
        if (!System.IO.Directory.Exists(directoryPath))
        {
            System.IO.Directory.CreateDirectory(directoryPath);
        }

        // Create a new database if one doesn't exist or replace wrong-type asset
        ForeignSubstanceDatabase database = AssetDatabase.LoadAssetAtPath<ForeignSubstanceDatabase>(DatabasePath);
        if (database == null)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DatabasePath);
            if (existing != null)
            {
                // Existing asset is not the expected type. Replace it.
                AssetDatabase.DeleteAsset(DatabasePath);
            }
            database = ScriptableObject.CreateInstance<ForeignSubstanceDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
        }

        var entries = new List<ForeignSubstanceDatabase.SubstanceEntry>();
        var allTypes = (ForeignSubstanceType[])Enum.GetValues(typeof(ForeignSubstanceType));

        foreach (var t in allTypes)
        {
            if (t == ForeignSubstanceType.None) continue;
            var group = GetGroupForType(t);
            var rng = new System.Random(((int)t * 7919) ^ 0x5A17);

            // Primary band index by group (R, O, Y, G, B, I, V)
            int primary = group switch
            {
                Group.Biological => 0,      // Red
                Group.Chemical => 3,        // Green
                Group.Environmental => 4,   // Blue
                _ => 2                      // Yellow for Synthetic
            };

            var bands = new ForeignSubstanceDatabase.BandSpec[7];

            // Enable exactly 4 bands, always including the primary
            int enabledTarget = 4;
            var picks = new HashSet<int> { primary };
            while (picks.Count < enabledTarget)
            {
                int pick = rng.Next(0, 7);
                if (pick == primary) continue;
                picks.Add(pick);
            }

            // Apply enable flags and widths, primary wider by default
            for (int i = 0; i < 7; i++)
            {
                bool on = picks.Contains(i);
                bands[i].enabled = on;
                float baseWidth;
                if (!on)
                {
                    baseWidth = 10f; // unused, ignored at runtime when disabled
                }
                else if (i == primary)
                {
                    baseWidth = 20f + rng.Next(-2, 3); // wider primary line
                }
                else
                {
                    baseWidth = 12f + rng.Next(-4, 5);
                }
                bands[i].width = Mathf.Clamp(baseWidth, 6f, 28f);
            }

            entries.Add(new ForeignSubstanceDatabase.SubstanceEntry
            {
                substance = t,
                bands = bands
            });
        }

        Undo.RecordObject(database, "Populate Spectrograph Database");
        database.substances = entries;
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        Debug.Log($"[SpectrographDatabaseGenerator] Generated {entries.Count} entries at {DatabasePath}");
    }

    private enum Group { Biological, Chemical, Environmental, Synthetic }

    private static Group GetGroupForType(ForeignSubstanceType t)
    {
        switch (t)
        {
            // Biological (Red)
            case ForeignSubstanceType.Blood_Type_O:
            case ForeignSubstanceType.Blood_Type_A:
            case ForeignSubstanceType.Blood_Type_B:
            case ForeignSubstanceType.Blood_Type_AB:
            case ForeignSubstanceType.Saliva_Human:
            case ForeignSubstanceType.Saliva_Animal:
            case ForeignSubstanceType.HairFollicle_Human:
            case ForeignSubstanceType.HairFollicle_Animal:
            case ForeignSubstanceType.SkinFlakes_EpithelialTissue:
            case ForeignSubstanceType.SweatTrace:
            case ForeignSubstanceType.UrineTrace:
            case ForeignSubstanceType.BoneDust:
            case ForeignSubstanceType.NailClippings:
            case ForeignSubstanceType.Tissue_MuscleFiber:
            case ForeignSubstanceType.SemenSample:
                return Group.Biological;

            // Chemicals & Drugs (Green)
            case ForeignSubstanceType.CocaineResidue:
            case ForeignSubstanceType.Opiates_Heroin:
            case ForeignSubstanceType.Opiates_Morphine:
            case ForeignSubstanceType.CannabisTrace:
            case ForeignSubstanceType.Amphetamines_Meth:
            case ForeignSubstanceType.PrescriptionSedatives_Benzos:
            case ForeignSubstanceType.SyntheticHallucinogen_LSD_Analogue:
            case ForeignSubstanceType.Ecstasy_MDMA:
            case ForeignSubstanceType.AnabolicSteroids:
            case ForeignSubstanceType.Barbiturates:
            case ForeignSubstanceType.Ketamine:
            case ForeignSubstanceType.DesignerResearchDrugs:
            case ForeignSubstanceType.RatPoison:
            case ForeignSubstanceType.CleaningChemical_Bleach:
            case ForeignSubstanceType.CleaningChemical_Ammonia:
                return Group.Chemical;

            // Environmental / Scene Residues (Blue)
            case ForeignSubstanceType.FireplaceSoot_CharcoalAsh:
            case ForeignSubstanceType.IndustrialCoalAsh:
            case ForeignSubstanceType.VolcanicAshTrace:
            case ForeignSubstanceType.Sand_SilicaGranules:
            case ForeignSubstanceType.ClaySoil_IronRich:
            case ForeignSubstanceType.RiverSilt:
            case ForeignSubstanceType.OceanSaltCrystals:
            case ForeignSubstanceType.MoldSpores_Aspergillus_Penicillium:
            case ForeignSubstanceType.PlantPollen_Pine_Ragweed_Grass:
            case ForeignSubstanceType.DustMiteResidue:
                return Group.Environmental;

            // Synthetic & Man-Made (Yellow)
            default:
                return Group.Synthetic;
        }
    }
}


