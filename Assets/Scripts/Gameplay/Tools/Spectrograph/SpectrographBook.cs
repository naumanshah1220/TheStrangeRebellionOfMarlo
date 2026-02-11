using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(BigCardVisual))]
public class SpectrographBook : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ForeignSubstanceDatabase database;

    [Header("Prefabs (Spreads)")]
    [Tooltip("RectTransform prefab with PageLeft and PageRight. Each page has PageHeadingText, PageText, PageNumberText")] 
    [SerializeField] private GameObject twoPagesWithTextPrefab;
    [Tooltip("RectTransform prefab with PageLeft and PageRight. Each page has PageHeadingText, SubstanceList (5), PageNumber")] 
    [SerializeField] private GameObject twoPagesWithSubstanceListPrefab;

    [Header("Hierarchy")]
    [Tooltip("Parent transform that will contain instantiated page spreads")] 
    [SerializeField] private RectTransform pagesParent;

    [Header("Options")]
    [SerializeField] private bool autoBuildOnAwake = true;
    [SerializeField, Range(1f, 50f)] private float minBandWidth = 6f;
    [SerializeField, Range(1f, 100f)] private float maxBandWidth = 28f;

    [Header("Intro Content (Pages 1 & 2)")]
    [TextArea(2, 6)]
    [SerializeField] private string introHeadingLeft = "What is a Spectrograph?";
    [TextArea(4, 10)]
    [SerializeField] private string introTextLeft = 
        "A spectrograph separates light into its component wavelengths (colors).\n" +
        "Different substances absorb and emit light in unique patterns. These patterns\n" +
        "appear as brighter or thicker lines at specific wavelengths (the colored bands).";

    [TextArea(2, 6)]
    [SerializeField] private string introHeadingRight = "Forensic Use";
    [TextArea(4, 10)]
    [SerializeField] private string introTextRight = 
        "In forensics, spectrographs help identify trace materials—biological fluids,\n" +
        "drugs, residues, and man‑made compounds—by matching the observed band pattern\n" +
        "against a reference library. A match can corroborate a narrative or reveal\n" +
        "new investigative leads.";

    private BigCardVisual bigCardVisual;

    private void Awake()
    {
        bigCardVisual = GetComponent<BigCardVisual>();
        if (autoBuildOnAwake)
        {
            TryBuildBook();
        }
    }

    [ContextMenu("Rebuild Book")]
    public void TryBuildBook()
    {
        if (database == null)
        {
            Debug.LogError("[SpectrographBook] Database not assigned");
            return;
        }
        if (twoPagesWithTextPrefab == null || twoPagesWithSubstanceListPrefab == null)
        {
            Debug.LogError("[SpectrographBook] Prefabs not assigned");
            return;
        }
        if (pagesParent == null)
        {
            Debug.LogError("[SpectrographBook] pagesParent not assigned");
            return;
        }

        ClearExistingPages();

        int pageNumberCounter = 1;

        // Intro spread (two pages)
        var introSpread = Instantiate(twoPagesWithTextPrefab, pagesParent);
        var introLeft = FindRequiredChild(introSpread.transform, "PageLeft");
        var introRight = FindRequiredChild(introSpread.transform, "PageRight");
        SetupIntroTextPage(introLeft, introHeadingLeft, introTextLeft, pageNumberCounter++);
        SetupIntroTextPage(introRight, introHeadingRight, introTextRight, pageNumberCounter++);
        RegisterSpread(introSpread);

        // Substance pages (5 entries per single page), grouped without mixing per page
        var grouped = BuildGroupedSubstanceMap();

        GameObject currentSpread = null;
        Transform currentLeft = null;
        Transform currentRight = null;
        bool leftFilled = false;
        bool rightFilled = false;

        System.Action beginNewSpread = () =>
        {
            currentSpread = Instantiate(twoPagesWithSubstanceListPrefab, pagesParent);
            currentLeft = FindRequiredChild(currentSpread.transform, "PageLeft");
            currentRight = FindRequiredChild(currentSpread.transform, "PageRight");
            leftFilled = false;
            rightFilled = false;
        };

        System.Action<bool> commitSpreadIfReady = (bool force) =>
        {
            if (currentSpread == null) return;
            if (leftFilled && rightFilled)
            {
                RegisterSpread(currentSpread);
                currentSpread = null;
                currentLeft = null;
                currentRight = null;
                leftFilled = false;
                rightFilled = false;
                return;
            }
            if (force && (leftFilled || rightFilled))
            {
                // Register partially filled spread without adding a blank page
                RegisterSpread(currentSpread);
                currentSpread = null;
                currentLeft = null;
                currentRight = null;
                leftFilled = false;
                rightFilled = false;
            }
        };

        // Group order as described
        var groupOrder = new List<string> { "Biological", "Chemicals & Drugs", "Environmental", "Synthetic & Man-Made" };
        foreach (var groupName in groupOrder)
        {
            if (!grouped.TryGetValue(groupName, out var list) || list.Count == 0) continue;

            bool isFirstPageForGroup = true;
            int cursor = 0;
            while (cursor < list.Count)
            {
                if (currentSpread == null) beginNewSpread();

                // Determine which side to fill
                bool writeLeft = !leftFilled;
                var pageRoot = writeLeft ? currentLeft : currentRight;
                if (!writeLeft && rightFilled)
                {
                    // both filled, start a new spread
                    commitSpreadIfReady(false);
                    beginNewSpread();
                    pageRoot = currentLeft;
                    writeLeft = true;
                }

                int remaining = list.Count - cursor;
                int take = Mathf.Min(5, remaining);
                var slice = list.GetRange(cursor, take);
                cursor += take;

                string heading = isFirstPageForGroup ? groupName : string.Empty;
                SetupSubstanceListPage(pageRoot, heading, pageNumberCounter++, slice);

                if (writeLeft) leftFilled = true; else rightFilled = true;

                // If both sides filled, commit and clear
                if (leftFilled && rightFilled)
                {
                    commitSpreadIfReady(false);
                }

                isFirstPageForGroup = false;
            }

            // Do not force-close here; next group should continue on the next available page side
        }

        // Commit incomplete spread if any
        commitSpreadIfReady(true);

        // Ensure first page visible
        if (bigCardVisual.pageObjects.Count > 0)
        {
            bigCardVisual.currentPageIndex = 0;
            bigCardVisual.SetPage(0);
        }

        // Navigation is handled by BigCardVisual; do not bind listeners here.
    }

    private void ClearExistingPages()
    {
        // Clear list and destroy existing children under pagesParent
        if (bigCardVisual.pageObjects != null)
        {
            bigCardVisual.pageObjects.Clear();
        }
        for (int i = pagesParent.childCount - 1; i >= 0; i--)
        {
            var child = pagesParent.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void RegisterSpread(GameObject spread)
    {
        if (spread != null) bigCardVisual.pageObjects.Add(spread);
    }

    private void SetupIntroTextPage(Transform pageRoot, string heading, string body, int pageNumber)
    {
        // Support multiple prefab naming variants
        if (!SetTextUnder(pageRoot, "PageHeadingText", heading))
        {
            SetTextUnder(pageRoot, "Page1Heading", heading);
        }
        if (!SetTextUnder(pageRoot, "PageText", body))
        {
            SetTextUnder(pageRoot, "Page1Text", body);
        }
        SetTextUnder(pageRoot, "PageNumberText", pageNumber.ToString());
    }

    private void SetupSubstanceListPage(Transform pageRoot, string heading, int pageNumber, List<ForeignSubstanceType> entries)
    {
        bool hasHeading = !string.IsNullOrEmpty(heading);
        bool headingSet = false;
        if (hasHeading)
        {
            if (!SetTextUnder(pageRoot, "PageHeadingText", heading))
            {
                headingSet = SetTextUnder(pageRoot, "Page1Heading", heading);
            }
            else headingSet = true;
        }
        // Hide heading object entirely if no heading for this page
        if (!hasHeading || !headingSet)
        {
            var h1 = pageRoot.Find("PageHeadingText");
            if (h1 != null) h1.gameObject.SetActive(false);
            var h2 = pageRoot.Find("Page1Heading");
            if (h2 != null) h2.gameObject.SetActive(false);
        }
        // PageNumber object may be named PageNumber or PageNumberText depending on prefab
        if (!SetTextUnder(pageRoot, "PageNumber", pageNumber.ToString()))
        {
            SetTextUnder(pageRoot, "PageNumberText", pageNumber.ToString());
        }

        var listRoot = pageRoot.Find("SubstanceList");
        if (listRoot == null)
        {
            Debug.LogWarning($"[SpectrographBook] SubstanceList not found under {pageRoot.name}");
            return;
        }

        for (int i = 0; i < listRoot.childCount; i++)
        {
            var item = listRoot.GetChild(i);
            bool hasData = i < entries.Count;
            item.gameObject.SetActive(hasData);
            if (!hasData) continue;

            var type = entries[i];
            // Name (support prefab naming: SubstanceName)
            if (!SetTextUnder(item, "substanceNameText", FormatSubstanceName(type.ToString())))
            {
                SetTextUnder(item, "SubstanceName", FormatSubstanceName(type.ToString()));
            }
            // Bands
            var bandsRoot = item.Find("SpectroGraphBands");
            if (bandsRoot == null)
            {
                // Try alternative name
                bandsRoot = item.Find("SpectrographBands");
            }
            if (bandsRoot != null)
            {
                ApplyBands(bandsRoot, type);
            }
        }
    }

    private void ApplyBands(Transform bandsRoot, ForeignSubstanceType type)
    {
        if (!database.TryGetBandPattern(type, out var pattern))
        {
            // Hide all
            for (int i = 0; i < bandsRoot.childCount; i++)
            {
                bandsRoot.GetChild(i).gameObject.SetActive(false);
            }
            return;
        }

        // Collect direct Image children in sibling order
        var imageChildren = new List<Image>();
        for (int i = 0; i < bandsRoot.childCount; i++)
        {
            var child = bandsRoot.GetChild(i);
            var img = child.GetComponent<Image>();
            if (img != null) imageChildren.Add(img);
        }

        // Expect 7; proceed with min(len,7)
        int count = Mathf.Min(7, imageChildren.Count);
        for (int i = 0; i < count; i++)
        {
            var img = imageChildren[i];
            bool enabled = pattern.enabled != null && i < pattern.enabled.Length && pattern.enabled[i];
            img.gameObject.SetActive(enabled);
            var rt = img.rectTransform;
            if (rt != null)
            {
                float width = (pattern.widths != null && i < pattern.widths.Length) ? pattern.widths[i] : 12f;
                float t = Mathf.InverseLerp(6f, 28f, width);
                var sd = rt.sizeDelta;
                sd.x = Mathf.Lerp(Mathf.Min(minBandWidth, maxBandWidth), Mathf.Max(minBandWidth, maxBandWidth), t);
                rt.sizeDelta = sd;
            }
        }

        // Hide any extra band objects beyond 7
        for (int i = count; i < bandsRoot.childCount; i++)
        {
            bandsRoot.GetChild(i).gameObject.SetActive(false);
        }
    }

    private List<ForeignSubstanceType> BuildOrderedSubstanceList()
    {
        // Filter None and ensure unique by type (database last-wins already)
        var all = new List<ForeignSubstanceType>();
        foreach (var entry in database.substances)
        {
            if (entry.substance == ForeignSubstanceType.None) continue;
            if (!all.Contains(entry.substance)) all.Add(entry.substance);
        }
        // Sort by enum value to keep declared grouping order
        all.Sort((a, b) => ((int)a).CompareTo((int)b));
        return all;
    }

    private Dictionary<string, List<ForeignSubstanceType>> BuildGroupedSubstanceMap()
    {
        var map = new Dictionary<string, List<ForeignSubstanceType>>
        {
            { "Biological", new List<ForeignSubstanceType>() },
            { "Chemicals & Drugs", new List<ForeignSubstanceType>() },
            { "Environmental", new List<ForeignSubstanceType>() },
            { "Synthetic & Man-Made", new List<ForeignSubstanceType>() }
        };

        var ordered = BuildOrderedSubstanceList();
        foreach (var t in ordered)
        {
            map[GetGroupName(t)].Add(t);
        }
        return map;
    }

    private string GetGroupName(ForeignSubstanceType t)
    {
        switch (t)
        {
            // Biological
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
                return "Biological";

            // Chemicals & Drugs
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
                return "Chemicals & Drugs";

            // Environmental / Scene Residues
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
                return "Environmental";

            // Synthetic & Man-Made (default)
            default:
                return "Synthetic & Man-Made";
        }
    }

    private string FormatSubstanceName(string enumName)
    {
        // Replace underscores with spaces and tidy common tokens
        return enumName.Replace('_', ' ');
    }

    private Transform FindRequiredChild(Transform root, string childName)
    {
        var t = root.Find(childName);
        if (t == null)
        {
            Debug.LogError($"[SpectrographBook] Child '{childName}' not found under '{root.name}'");
        }
        return t;
    }

    private bool SetTextUnder(Transform root, string childName, string value)
    {
        var t = root.Find(childName);
        if (t == null) return false;
        var tmp = t.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = value;
            return true;
        }
        var ui = t.GetComponent<Text>();
        if (ui != null)
        {
            ui.text = value;
            return true;
        }
        return false;
    }
}


