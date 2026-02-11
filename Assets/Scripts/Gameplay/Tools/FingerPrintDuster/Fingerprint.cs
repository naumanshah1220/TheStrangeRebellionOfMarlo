using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Fingerprint script that handles fingerprint and powder reveal using ImageRevealBrush components
/// Supports multiple pages with separate fingerprint/powder brush systems for each page
/// Attach this to evidence cards that have fingerprint/powder brush components
/// </summary>
public class Fingerprint : MonoBehaviour
{
    [System.Serializable]
    public class FingerprintPage
    {
        [Header("Page Info")]
        public int pageNumber = 0;
        public string pageName = "";
        
        [Header("Brush Components")]
        public ImageRevealBrush powderBrush; // Required: powder brush for this page
        
        [Header("Particle System")]
        public ParticleSystem powderParticleSystem; // Optional: particle system for powder effects
        
        [Header("Citizen Association")]
        public bool hasFingerprint = false; // Does this page have a fingerprint?
        [Tooltip("Evidence ID to unlock when this page is solved and scanned")] public string associatedEvidenceId = "";
        public bool extraEvidenceUnlocked = false; // Prevent duplicate unlocks
        
        [Header("Page Settings")]
        public float requiredRevealPercentage = 100f; // Required reveal percentage (0-100) to auto-complete if used
        
        [Header("Page State")]
        public bool isCompleted = false;
        public float currentRevealPercentage = 0f;
    }
    
    // Single-page mode only
    private FingerprintPage singlePage;

    [Header("Single Page Config")]
    [SerializeField] private ImageRevealBrush singlePagePowderBrush;
    [SerializeField] private bool singlePageHasFingerprint = false;
    [SerializeField] [Range(0f, 100f)] private float singlePageRequiredRevealPercentage = 100f;
    [SerializeField] [Tooltip("Evidence ID to unlock when this page is solved and scanned")] private string singlePageAssociatedEvidenceId = "";
    
    [Header("Card Movement Settings")]
    [SerializeField] private float movementThreshold = 0.1f; // Minimum movement to trigger powder shake
    [SerializeField] private float maxFadeDistance = 1f; // Maximum distance for fade calculation
    [SerializeField] private float distanceFadeMultiplier = 0.05f; // How much distance affects fade rate
    
    [Header("Shake Rate Configuration")]
    [SerializeField] [Range(0.001f, 2.0f)] private float powderShakeRate = 0.02f; // How fast powder shakes off
    [SerializeField] [Range(0.001f, 2.0f)] private float fingerprintShakeRate = 0.01f; // How fast fingerprint shakes off (usually slower)
    [SerializeField] [Range(0f, 100f)] private float powderToFingerprintThreshold = 20f; // Progress % where we switch from powder-only to fingerprint-only shaking
    [SerializeField] [Range(0.1f, 2f)] private float shakeResponsiveness = 1f; // Overall shake responsiveness multiplier
    [SerializeField] [Range(0.001f, 0.1f)] private float maxFadeFactor = 0.05f; // Maximum fade factor per frame (0.5% to 10%)
    
    [Header("Particle System")]
    [SerializeField] private ParticleSystem powderParticleSystem;
    [SerializeField] private float maxParticleRate = 5000f;
    [SerializeField] private float minParticleRate = 500f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    // Internal state
    private int currentPageIndex = 0; // Currently active page
    private bool isCardLoadedOnDuster = false; // Whether card is loaded on FingerPrintDuster
    private bool isBrushBeingUsed = false; // Whether the brush is currently being dragged
    
    // Card movement tracking
    private Vector3 lastCardPosition;
    private bool isCardMoving = false;
    
    // References
    private FingerPrintDusterSystem dusterSystem;
    
    // Events
    public System.Action<float> OnProgressChanged; // Called when reveal progress changes (for current page)
    public System.Action<int> OnPageFingerprintCompleted; // Called when a page's fingerprint is fully revealed
    public System.Action OnAllFingerprintsCompleted; // Called when all pages with fingerprints are completed
    
    // Public properties
    public bool IsCurrentPageCompleted => GetCurrentPage()?.isCompleted ?? false;
    public float CurrentPageRevealPercentage => GetCurrentPage()?.currentRevealPercentage ?? 0f;
    public bool HasFingerprintOnCurrentPage => GetCurrentPage()?.hasFingerprint ?? false;
    public bool HasPowderOnCurrentPage => GetCurrentPage()?.powderBrush != null;
    public int CurrentPageIndex => 0;
    public int TotalPages => 1;
    public bool IsCardLoadedOnDuster => isCardLoadedOnDuster;
    
    private void Start()
    {
        // Get duster system reference before initializing page so colors are available
        dusterSystem = FindFirstObjectByType<FingerPrintDusterSystem>();

        // Single-page mode: build local page data from fields or children
        singlePage = new FingerprintPage
        {
            pageNumber = 0,
            pageName = name,
            powderBrush = singlePagePowderBrush != null ? singlePagePowderBrush : GetComponentInChildren<ImageRevealBrush>(true),
            hasFingerprint = singlePagePowderBrush != null ? singlePageHasFingerprint : (GetComponentInChildren<ImageRevealBrush>(true) != null),
            requiredRevealPercentage = Mathf.Clamp(singlePageRequiredRevealPercentage, 0f, 100f),
            associatedEvidenceId = singlePageAssociatedEvidenceId,
            isCompleted = false,
            currentRevealPercentage = 0f
        };
        InitializeAllPages();
        
        // Initialize card position tracking
        lastCardPosition = transform.position;
    }

    // Removed multi-page auto-binding; single-page mode only
    
    private void Update()
    {
        // Monitor card movement
        MonitorCardMovement();
        
        // Particle system is now managed by UpdatePowderParticleSystem() when progress changes
        // Removed: UpdateParticleSystems();
    }
    
    /// <summary>
    /// Initialize all pages
    /// </summary>
    private void InitializeAllPages()
    {
        if (singlePage == null)
        {
            Debug.LogWarning($"[Fingerprint] No fingerprint pages configured for {gameObject.name}");
            return;
        }

        InitializePage(singlePage);
        SetActivePage(0);
    }
    
    /// <summary>
    /// Initialize a single page's brush components
    /// </summary>
    private void InitializePage(FingerprintPage page)
    {
        // Ensure GraphicsRaycaster components are added and disabled by default
        EnsureGraphicsRaycasters(page);
        
        // Initialize powder brush (always available)
        if (page.powderBrush != null)
        {
            page.powderBrush.Initialize();
            page.powderBrush.OnProgressChanged += (progress) => OnBrushProgressChanged(page, progress);
            
            // Force solve percentage to 100% to allow painting until fully complete
            var brushComponent = page.powderBrush.GetComponent<ImageRevealBrush>();
            if (brushComponent != null)
            {
                brushComponent.ForceSolvePercentageTo100();
            }

            // When the brush declares the page solved, mark the fingerprint page completed
            page.powderBrush.OnRevealCompleted += () => OnPageBrushCompleted(page);
        }
        
        // Initialize particle system start colors from duster system
        if (powderParticleSystem != null && dusterSystem != null)
        {
            var main = powderParticleSystem.main;
            main.startColor = new ParticleSystem.MinMaxGradient(dusterSystem.GetPowderParticleColor1(), dusterSystem.GetPowderParticleColor2());

            // Initialize particle material emission color and start with intensity 0 (light assumed ON by default)
            var psRenderer = powderParticleSystem.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null && psRenderer.sharedMaterial != null)
            {
                var mat = psRenderer.sharedMaterial;
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", dusterSystem.GetPowderParticleEmissionColor());
                }
                if (mat.HasProperty("_EmissionIntensity"))
                {
                    mat.SetFloat("_EmissionIntensity", 0f);
                }
            }
        }
        
        // Particle system is now managed centrally by UpdatePowderParticleSystem()
    }
    
    /// <summary>
    /// Ensure GraphicsRaycaster components are added to brush GameObjects and disabled by default
    /// </summary>
    private void EnsureGraphicsRaycasters(FingerprintPage page)
    {
        // Ensure powder brush has GraphicsRaycaster
        if (page.powderBrush != null)
        {
            var powderRaycaster = page.powderBrush.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (powderRaycaster == null)
            {
                powderRaycaster = page.powderBrush.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                if (showDebugInfo)
                {
                    Debug.Log($"[Fingerprint] Added GraphicsRaycaster to powder brush for page {page.pageNumber}");
                }
            }
            // Disable by default
            powderRaycaster.enabled = false;
        }
    }
    
    /// <summary>
    /// Set the active page (called when evidence page changes)
    /// </summary>
    public void SetActivePage(int pageIndex)
    {
        // Single-page: always use page 0
        var newPage = GetCurrentPage();
        if (newPage == null) return;
        ShowPageOverlays(newPage);
        if (isCardLoadedOnDuster)
            EnablePageBrushes(newPage);
        else
            ShowPageBrushes(newPage);
        UpdatePowderParticleSystem(newPage.currentRevealPercentage);
        OnProgressChanged?.Invoke(newPage.currentRevealPercentage);
        UpdateGraphicsRaycasters();
    }
    
    /// <summary>
    /// Called when card is loaded onto FingerPrintDuster
    /// </summary>
    public void OnCardLoadedOnDuster()
    {
        isCardLoadedOnDuster = true;
        
        // Enable brushes for current page
        var currentPage = GetCurrentPage();
        if (currentPage != null)
        {
            EnablePageBrushes(currentPage);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Card loaded on duster - enabled brushes for page {currentPageIndex}");
        }
    }
    
    /// <summary>
    /// Called when card is removed from FingerPrintDuster
    /// </summary>
    public void OnCardRemovedFromDuster()
    {
        isCardLoadedOnDuster = false;
        isBrushBeingUsed = false;
        
        // Disable all GraphicsRaycasters
        DisableAllGraphicsRaycasters();
        
        // Disable brushes for current page but keep overlays visible
        var currentPage = GetCurrentPage();
        if (currentPage != null)
        {
            DisablePageBrushes(currentPage);
            
            // Ensure overlays remain visible for viewing reveal effects
            ShowPageOverlays(currentPage);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Card removed from duster - disabled brushes but kept overlays visible for page {currentPageIndex}");
        }
    }
    
    /// <summary>
    /// Called when brush starts being dragged
    /// </summary>
    public void OnBrushStartDrag()
    {
        if (!isCardLoadedOnDuster) return;
        
        isBrushBeingUsed = true;
        UpdateGraphicsRaycasters();
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Brush started dragging - enabled GraphicsRaycaster for page {currentPageIndex}");
        }
    }
    
    /// <summary>
    /// Called when brush stops being dragged
    /// </summary>
    public void OnBrushEndDrag()
    {
        isBrushBeingUsed = false;
        UpdateGraphicsRaycasters();
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Brush stopped dragging - disabled all GraphicsRaycasters");
        }
    }
    
    /// <summary>
    /// Update GraphicsRaycaster components based on current state
    /// </summary>
    private void UpdateGraphicsRaycasters()
    {
        var page = GetCurrentPage();
        bool shouldEnable = isCardLoadedOnDuster && isBrushBeingUsed;
        if (page?.powderBrush != null)
        {
            var powderRaycaster = page.powderBrush.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (powderRaycaster != null)
            {
                powderRaycaster.enabled = shouldEnable;
            }
        }
    }
    
    /// <summary>
    /// Disable all GraphicsRaycaster components
    /// </summary>
    private void DisableAllGraphicsRaycasters()
    {
        var page = GetCurrentPage();
        if (page?.powderBrush != null)
        {
            var powderRaycaster = page.powderBrush.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (powderRaycaster != null) powderRaycaster.enabled = false;
        }
    }
    
    /// <summary>
    /// Hide all page overlays (called when switching pages)
    /// </summary>
    private void HideAllPageOverlays()
    {
        var page = GetCurrentPage();
        if (page?.powderBrush != null) page.powderBrush.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Show overlays for a specific page (called when switching to that page)
    /// </summary>
    private void ShowPageOverlays(FingerprintPage page)
    {
        // Show powder brush overlay (always available)
        if (page.powderBrush != null)
        {
            page.powderBrush.gameObject.SetActive(true);
            
        }
        
        // Note: Powder brush and fingerprint brush are the same GameObject
        // The fingerprint functionality is handled by the ImageRevealBrush component
        // No need to separately activate/deactivate for fingerprints
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Showed overlays for page {page.pageNumber} ({page.pageName})");
        }
    }
    
    /// <summary>
    /// Show brushes for a specific page (without enabling them)
    /// </summary>
    private void ShowPageBrushes(FingerprintPage page)
    {
        // Overlays should already be visible from ShowPageOverlays, just disable brush functionality
        if (page.powderBrush != null)
        {
            page.powderBrush.SetBrushEnabled(false);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Showed brushes for page {page.pageNumber} ({page.pageName}) - disabled");
        }
    }
    
    /// <summary>
    /// Disable brushes for a specific page
    /// </summary>
    private void DisablePageBrushes(FingerprintPage page)
    {
        if (page.powderBrush != null)
        {
            page.powderBrush.SetBrushEnabled(false);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Disabled brushes for page {page.pageNumber} ({page.pageName})");
        }
    }
    
    /// <summary>
    /// Enable brushes for a specific page
    /// </summary>
    private void EnablePageBrushes(FingerprintPage page)
    {
        
        // Enable powder brush (overlay should already be visible from ShowPageOverlays)
        if (page.powderBrush != null)
        {
            page.powderBrush.SetBrushEnabled(true);
            Debug.Log($"[Fingerprint] Enabled powder brush for page {page.pageNumber}");
        }
        else
        {
            Debug.LogWarning($"[Fingerprint] No powder brush found for page {page.pageNumber}");
        }
        
        // Enable fingerprint brush only if page has fingerprints (overlay should already be visible from ShowPageOverlays)
        if (page.hasFingerprint && page.powderBrush != null)
        {
            page.powderBrush.SetBrushEnabled(true);
            Debug.Log($"[Fingerprint] Enabled fingerprint brush for page {page.pageNumber} (hasFingerprint=true)");
        }
        else
        {
            Debug.Log($"[Fingerprint] No fingerprint brush to enable for page {page.pageNumber} (hasFingerprint={page.hasFingerprint})");
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Enabled brushes for page {page.pageNumber} ({page.pageName})");
        }
    }
    
    /// <summary>
    /// Get the currently active page
    /// </summary>
    public FingerprintPage GetCurrentPage()
    {
        return singlePage;
    }
    
    /// <summary>
    /// Handle brush progress changes
    /// </summary>
    public void OnBrushProgressChanged(FingerprintPage page, float progress)
    {
        // If new progress is higher than current, we're painting (not shaking)
        if (progress > page.currentRevealPercentage)
        {
            ResetFadeAmounts();
            // Removed: isShakingOff = false;
        }
        
        // Update the stored reveal percentage
        page.currentRevealPercentage = progress;
        
        // Update particle color opacities to reflect powder vs fingerprint
        UpdateParticleColorsForProgress(progress);
        
        // Safety check: if reveal percentage is unreasonably high, reset it
        if (page.currentRevealPercentage > 100f)
        {
            page.currentRevealPercentage = 100f;
        }
        
        // Update opacities using the brush's centralized method
        if (page.powderBrush != null)
        {
            page.powderBrush.UpdatePowderAndFingerprintOpacity();
        }
        // Update the shared particle system based on this page's progress
        UpdatePowderParticleSystem(page.currentRevealPercentage);
        
        // Only notify if this is the current page
        if (page.pageNumber == currentPageIndex)
        {
            OnProgressChanged?.Invoke(page.currentRevealPercentage);
        }
        
        // Add debug logging
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] OnBrushProgressChanged - Page: {page.pageNumber}, Progress: {progress:F1}%, CurrentPage: {currentPageIndex}");
        }
    }
    
    /// <summary>
    /// Called when a brush is completed
    /// </summary>
    private void OnPageBrushCompleted(FingerprintPage page)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Page {page.pageNumber} brush completed");
        }

        if (!page.hasFingerprint)
        {
            return;
        }

        // Directly mark as completed when the reveal system reports solved
        if (!page.isCompleted)
        {
            page.isCompleted = true;
            OnPageFingerprintCompleted?.Invoke(page.pageNumber);
            CheckAllPagesCompleted();
            
            if (showDebugInfo)
            {
                Debug.Log($"[Fingerprint] Page {page.pageNumber} ({page.pageName}) completed on {gameObject.name}!");
            }
        }
    }
    
    /// <summary>
    /// Check if a specific page's fingerprint has been fully revealed
    /// </summary>
    private void CheckForCompletion(FingerprintPage page)
    {
        if (page.isCompleted || !page.hasFingerprint) return;
        
        if (page.currentRevealPercentage >= page.requiredRevealPercentage)
        {
            page.isCompleted = true;
            OnPageFingerprintCompleted?.Invoke(page.pageNumber);
            
            // Check if all pages with fingerprints are completed
            CheckAllPagesCompleted();
            
            if (showDebugInfo)
            {
                Debug.Log($"[Fingerprint] Page {page.pageNumber} ({page.pageName}) completed on {gameObject.name}!");
            }
        }
    }
    
    /// <summary>
    /// Check if all pages with fingerprints are completed
    /// </summary>
    private void CheckAllPagesCompleted()
    {
        if (singlePage != null && (!singlePage.hasFingerprint || singlePage.isCompleted))
        {
            OnAllFingerprintsCompleted?.Invoke();
        }
    }
    
    /// <summary>
    /// Reset all pages (for testing or reuse)
    /// </summary>
    public void ResetAllPages()
    {
        if (singlePage != null)
        {
            ResetPage(singlePage);
            SetActivePage(0);
        }
    }
    
    /// <summary>
    /// Reset a specific page
    /// </summary>
    private void ResetPage(FingerprintPage page)
    {
        // Reset powder brush
        if (page.powderBrush != null)
        {
            page.powderBrush.ResetReveal();
            page.powderBrush.SetBrushEnabled(false);
        }
        
        // Reset state
        page.isCompleted = false;
        page.currentRevealPercentage = 0f;
    }
    
    /// <summary>
    /// Get the current progress for the progress bar (current page)
    /// </summary>
    public float GetProgressBarValue()
    {
        return GetCurrentPage()?.currentRevealPercentage ?? 0f;
    }
    
    /// <summary>
    /// Get progress for a specific page
    /// </summary>
    // Legacy methods removed in single-page mode
    
    /// <summary>
    /// Check if a specific page has a fingerprint
    /// </summary>
    // Legacy methods removed in single-page mode
    
    /// <summary>
    /// Get the total number of pages with fingerprints
    /// </summary>
    public int GetTotalFingerprintPages() { return (singlePage != null && singlePage.hasFingerprint) ? 1 : 0; }
    
    /// <summary>
    /// Get the number of completed fingerprint pages
    /// </summary>
    public int GetCompletedFingerprintPages() { return (singlePage != null && singlePage.hasFingerprint && singlePage.isCompleted) ? 1 : 0; }
    
    /// <summary>
    /// Get the citizen ID associated with the current page
    /// </summary>
    public string GetCurrentPageCitizenId()
    {
        var currentPage = GetCurrentPage();
        return currentPage?.associatedEvidenceId ?? ""; // Backward: now returns evidence id
    }
    
    /// <summary>
    /// Get the citizen ID associated with a specific page
    /// </summary>
    // Legacy single-page replacement
    public string GetPageCitizenId(int pageIndex) { return GetCurrentPageAssociatedEvidenceId(); }
    
    /// <summary>
    /// Set the citizen ID for a specific page
    /// </summary>
    public void SetPageCitizenId(int pageIndex, string citizenId) { if (singlePage != null) singlePage.associatedEvidenceId = citizenId; }
    
    /// <summary>
    /// Get all citizen IDs that have fingerprints on this evidence
    /// </summary>
    public List<string> GetAssociatedCitizenIds()
    {
        var list = new List<string>();
        var p = GetCurrentPage();
        if (p != null && p.hasFingerprint && !string.IsNullOrEmpty(p.associatedEvidenceId)) list.Add(p.associatedEvidenceId);
        return list;
    }
    
    /// <summary>
    /// Check if a specific citizen has fingerprints on this evidence
    /// </summary>
    public bool HasFingerprintFromCitizen(string citizenId)
    {
        var p = GetCurrentPage();
        return p != null && p.hasFingerprint && p.associatedEvidenceId == citizenId;
    }
    
    /// <summary>
    /// Get all pages that have fingerprints from a specific citizen
    /// </summary>
    public List<FingerprintPage> GetPagesWithCitizenFingerprint(string citizenId)
    {
        var result = new List<FingerprintPage>();
        var p = GetCurrentPage();
        if (p != null && p.hasFingerprint && p.associatedEvidenceId == citizenId) result.Add(p);
        return result;
    }
    
    /// <summary>
    /// Update brush size for all pages
    /// </summary>
    public void SetBrushSize(float size)
    {
        var p = GetCurrentPage();
        if (p?.powderBrush != null) p.powderBrush.SetBrushRadius(size);
    }
    
   
    /// <summary>
    /// Restore material states for all fingerprint brushes
    /// This ensures the reveal effects persist when cards move between holders
    /// </summary>
    public void RestoreAllMaterialStates()
    {
        var p = GetCurrentPage();
        if (p?.powderBrush != null) p.powderBrush.RestoreMaterialState();
        if (p != null) ShowPageOverlays(p);
    }
    
    /// <summary>
    /// Apply brush settings to all fingerprint pages
    /// </summary>
    public void ApplyBrushSettings(float brushRadius, Color powderTintColor, Color powderEmissionColor, float powderFlow, float brushHardness)
    {
        var p = GetCurrentPage();
        if (p?.powderBrush != null)
        {
            p.powderBrush.ApplyBrushSettings(brushRadius, powderTintColor, powderEmissionColor, powderFlow, brushHardness);
        }
    }
    
    /// <summary>
    /// Manually update the current page's progress and trigger progress events
    /// </summary>
    public void UpdateCurrentPageProgress()
    {
        var currentPage = GetCurrentPage();
        if (currentPage != null && currentPage.powderBrush != null)
        {
            // Get the current progress from the brush (this is percentage 0-100)
            float currentProgress = currentPage.powderBrush.RevealPercentage;
            
            if (showDebugInfo)
            {
                Debug.Log($"[Fingerprint] UpdateCurrentPageProgress - Brush RevealPercentage: {currentProgress:F1}%");
            }
            
            // Trigger the progress changed event with the percentage value
            // FingerPrintDusterSystem.OnFingerprintProgressChanged will convert it to decimal
            OnBrushProgressChanged(currentPage, currentProgress);
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[Fingerprint] UpdateCurrentPageProgress - No current page or powder brush found");
            }
        }
    }

    /// <summary>
    /// Monitor card movement and trigger powder shake effects
    /// </summary>
    private void MonitorCardMovement()
    {
        Vector3 currentPosition = transform.position;
        float distance = Vector3.Distance(currentPosition, lastCardPosition);
        
        // Add debug logging for movement detection
        if (showDebugInfo && distance > 0.001f) // Only log if there's any movement
        {
            Debug.Log($"[Fingerprint] Card movement detected - distance: {distance:F4}, threshold: {movementThreshold:F4}");
        }
        
        // Check if card has moved significantly
        if (distance > movementThreshold)
        {
            if (!isCardMoving)
            {
                isCardMoving = true;
                if (showDebugInfo)
                {
                    Debug.Log($"[Fingerprint] Card started moving - distance: {distance:F3}");
                }
            }
            
            // Trigger powder shake effect with distance information
            TriggerPowderShake(distance);
        }
        else
        {
            if (isCardMoving)
            {
                isCardMoving = false;
                if (showDebugInfo)
                {
                    Debug.Log($"[Fingerprint] Card stopped moving");
                }
            }
        }
        
        lastCardPosition = currentPosition;
    }
    
    /// <summary>
    /// Trigger powder shake effect when card moves
    /// </summary>
    private void TriggerPowderShake(float distance)
    {
        var currentPage = GetCurrentPage();
        if (currentPage == null || currentPage.powderBrush == null) return;
        
        // Get current reveal percentage
        float currentRevealPercentage = currentPage.currentRevealPercentage / 100f; // Convert to 0-1
        
        if (currentRevealPercentage <= 0f) return; // No powder to shake off
        
        // Set shaking flag
        // Removed: isShakingOff = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Triggering powder shake - current reveal: {currentRevealPercentage:P1}, distance: {distance:F3}");
        }
        
        // SHAKING PROGRESSION (100-0%):
        // 100-20%: Powder fades away, fingerprint stays visible
        // 20-0%: Fingerprint also fades away
        
        // Calculate distance-based fade rate
        float distanceFactor = Mathf.Clamp01(distance / maxFadeDistance);
        
        // Two-phase shaking: powder first, then fingerprint
        float currentShakeRate;
        if (currentPage.currentRevealPercentage > 30f) // Changed to 30f to match new opacity system
        {
            // Phase 1: Powder shaking (faster) - powder is fully visible
            currentShakeRate = powderShakeRate;
            if (showDebugInfo)
            {
                Debug.Log($"[Fingerprint] Phase 1 - Powder shaking at rate: {currentShakeRate:F4}");
            }
        }
        else
        {
            // Phase 2: Fingerprint shaking (slower) - powder is fading, fingerprint is still visible
            currentShakeRate = fingerprintShakeRate;
            if (showDebugInfo)
            {
                Debug.Log($"[Fingerprint] Phase 2 - Fingerprint shaking at rate: {currentShakeRate:F4}");
            }
        }
        
        float adjustedFadeRate = currentShakeRate * (1f + (distanceFactor * distanceFadeMultiplier));
        
        // Limit the fade factor to prevent sudden drops - ADJUSTED for better responsiveness
        float fadeFactor = Mathf.Min(adjustedFadeRate * shakeResponsiveness, maxFadeFactor); // Maximum 0.5% fade per frame (increased from 0.2%)
        
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] Shake fade - distance: {distance:F3}, factor: {distanceFactor:F3}, rate: {adjustedFadeRate:F4}, fadeFactor: {fadeFactor:F4}");
        }
        
        FadeMaskPixels(currentPage.powderBrush, fadeFactor);
        
        // Image opacities will be updated automatically in OnBrushProgressChanged
        // when the brush calls UpdateProgress() and triggers the progress change
        
        // Particle system is now managed by UpdatePowderParticleSystem() based on progress
        // Removed old DisableParticleSystem call
    }
    
    // Removed old particle system management - now handled by UpdatePowderParticleSystem()
    
    // Removed old particle system methods - now handled by UpdatePowderParticleSystem()
    
    /// <summary>
    /// Reset fade amounts when new powder is applied
    /// </summary>
    public void ResetFadeAmounts()
    {
        // Opacity is now handled by the brush, so no need to reset local variables
        // The brush will update its own opacities when progress changes
        
        if (showDebugInfo)
        {
            Debug.Log("[Fingerprint] Reset fade amounts - opacity now handled by brush");
        }
    }
    
    /// <summary>
    /// Fade mask pixels to gradually reduce reveal percentage when shaking
    /// </summary>
    private void FadeMaskPixels(ImageRevealBrush brush, float fadeFactor)
    {
        if (brush == null) return;
        brush.ShakeOffPowder(fadeFactor);
        // ShakeOffPowder already calls UpdateProgressAndOpacity which triggers progress change
        // No need to manually update particle system here
    }

    // Add this method to control the shared particle system
    private void UpdatePowderParticleSystem(float progress)
    {
        if (powderParticleSystem == null) 
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("[Fingerprint] UpdatePowderParticleSystem - No particle system assigned!");
            }
            return;
        }
        
        float normalized = Mathf.Clamp01(progress / 100f);
        float rate = Mathf.Lerp(minParticleRate, maxParticleRate, normalized);
        var emission = powderParticleSystem.emission;
        emission.rateOverDistance = (progress <= 0f) ? 0f : rate;
        
        if (progress <= 0f)
        {
            if (powderParticleSystem.isPlaying)
            {
                powderParticleSystem.Stop();
                if (showDebugInfo)
                {
                    Debug.Log("[Fingerprint] Stopped particle system (progress <= 0)");
                }
            }
        }
        else
        {
            if (!powderParticleSystem.isPlaying)
            {
                powderParticleSystem.Play();
                if (showDebugInfo)
                {
                    Debug.Log("[Fingerprint] Started particle system (progress > 0)");
                }
            }
        }
        
        // Add debug logging
        if (showDebugInfo)
        {
            Debug.Log($"[Fingerprint] UpdatePowderParticleSystem - Progress: {progress:F1}%, Normalized: {normalized:F3}, Rate: {rate:F1}, Playing: {powderParticleSystem.isPlaying}, RateOverDistance: {emission.rateOverDistance:F1}");
        }
    }

    private void UpdateParticleColorsForProgress(float progressPercent)
    {
        if (powderParticleSystem == null || dusterSystem == null) return;
        var main = powderParticleSystem.main;
        var grad = new ParticleSystem.MinMaxGradient(dusterSystem.GetPowderParticleColor1(), dusterSystem.GetPowderParticleColor2());
        // Compute alpha for powder (color1) and fingerprint (color2) based on the same curve used for images
        float powderAlpha; float fingerprintAlpha;
        float p = Mathf.Clamp(progressPercent, 0f, 100f);
        if (p <= 0f)
        {
            powderAlpha = 0f; fingerprintAlpha = 0f;
        }
        else if (p < 10f)
        {
            powderAlpha = 0.5f * (p / 10f);
            fingerprintAlpha = p / 10f;
        }
        else if (p < 30f)
        {
            powderAlpha = 0.5f;
            fingerprintAlpha = 0.5f + (0.5f * ((p - 10f) / 20f));
        }
        else if (p < 80f)
        {
            powderAlpha = 0.5f + (0.5f * ((p - 30f) / 50f));
            fingerprintAlpha = 1f;
        }
        else // 80-100
        {
            powderAlpha = 1f; fingerprintAlpha = 1f;
        }
        Color c1 = grad.colorMin; c1.a = powderAlpha;
        Color c2 = grad.colorMax; c2.a = fingerprintAlpha;
        grad.colorMin = c1; grad.colorMax = c2;
        main.startColor = grad;
    }

    public void SetParticleEmissionIntensity(float intensity)
    {
        if (powderParticleSystem == null) return;
        var psRenderer = powderParticleSystem.GetComponent<ParticleSystemRenderer>();
        if (psRenderer == null || psRenderer.sharedMaterial == null) return;
        var mat = psRenderer.sharedMaterial;
        if (mat.HasProperty("_EmissionIntensity"))
        {
            mat.SetFloat("_EmissionIntensity", Mathf.Max(0f, intensity));
        }
        if (mat.HasProperty("_EmissionColor") && dusterSystem != null)
        {
            mat.SetColor("_EmissionColor", dusterSystem.GetPowderParticleEmissionColor());
        }
    }

    // New helpers
    public string GetCurrentPageAssociatedEvidenceId()
    {
        return GetCurrentPage()?.associatedEvidenceId ?? "";
    }

    public void MarkCurrentPageExtraEvidenceUnlocked()
    {
        var p = GetCurrentPage();
        if (p != null) p.extraEvidenceUnlocked = true;
    }
} 