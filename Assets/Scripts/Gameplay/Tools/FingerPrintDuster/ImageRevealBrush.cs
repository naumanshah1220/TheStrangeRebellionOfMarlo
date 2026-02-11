using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic; // Added for List

/// <summary>
/// Brush system that reveals a powder texture by painting white on a transparent mask.
/// Now also supports revealing a fingerprint texture simultaneously with the powder.
/// Dynamically creates unique materials for each card to avoid sharing issues.
/// Follows the exact same pattern as BrushReveal.cs
/// </summary>
public class ImageRevealBrush : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject revealObj; // The powder image that gets revealed
    [SerializeField] private GameObject fingerprintObj; // The fingerprint image that gets revealed (optional)
    
    // Material templates removed – using default UI/AlphaReveal materials
    
    [Header("Solve Settings")]
    [Range(0, 100)]
    [SerializeField] private float solvePercentage = 100f; // Only mark as solved when both powder and fingerprint are 100% complete
    
    [Header("Events")]
    [SerializeField] private UnityEvent onSolved; // Called when solved
    
    [Header("Debug Info")]
    [SerializeField] private float revealedPercentage = 0f; // Current revealed percentage (read-only)
    [SerializeField] private bool solved = false; // Whether the reveal is completed (read-only)
    [SerializeField] private bool brushEnabled = false; // Whether brushing is currently enabled
    [SerializeField] private bool showDebugInfo = false; // Whether to show debug information
    
    [Header("Mask Erasure Settings")]
    [SerializeField] [Range(0.001f, 0.1f)] private float maskErasureRate = 0.002f; // How much of painted pixels to erase per frame (0.1% to 10%)
    
    [Header("Progress Display Settings")]
    [SerializeField] [Range(0f, 0.5f)] private float progressOffset = 0.25f; // Offset to "cheat" progress display (0.25 = 25% offset)
    [SerializeField] private bool useProgressOffset = true; // Whether to use the progress offset
    
    [Header("Performance Settings")]
    [SerializeField] [Range(1, 10)] private int maxUpdatesPerSecond = 30; // Limit updates per second to reduce frame drops
    [SerializeField] private bool enablePerformanceOptimization = true; // Enable performance optimizations
    
    // Internal state
    private RectTransform rectTransform;
    private Texture2D maskTexture; // Transparent texture that gets painted white
    private Color32[] originalPixels; // Original powder texture pixels
    private Camera mainCamera;
    private Material powderMaterial; // Dynamically created material for powder
    private Material fingerprintMaterial; // Dynamically created material for fingerprint
    private RawImage revealImage; // Store reference to the reveal image
    private RawImage fingerprintImage; // Store reference to the fingerprint image
    private int brushRadius = 100; // Brush radius in pixels (set by FingerPrintDusterSystem)
    private FingerPrintDusterSystem dusterSystem; // Reference to the duster system for color settings
    
    // Emission control
    private Color originalFingerprintEmissionColor; // Store original emission color
    private float originalFingerprintEmissionIntensity; // Store original emission intensity
    private Color originalFingerprintTintColor; // Store original tint color
    
    // Brush settings (set by FingerPrintDusterSystem)
    private float brushFlow = 1f; // How much powder is applied per stroke (0-1)
    private float brushHardness = 0.5f; // 0 = soft edges, 1 = hard edges
    
    // Performance optimization
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.033f; // 30 FPS default
    private bool needsProgressUpdate = false;
    private bool needsOpacityUpdate = false;
    
    // Events
    public System.Action<float> OnProgressChanged;
    public System.Action OnRevealCompleted;
    
    // Properties
    public float RevealPercentage => GetRevealedPercentage();
    public bool IsFullyRevealed => solved;
    
    private void Start()
    {
        Initialize();
        InitializePerformanceSettings();
    }
    
    /// <summary>
    /// Initialize performance optimization settings
    /// </summary>
    private void InitializePerformanceSettings()
    {
        if (enablePerformanceOptimization)
        {
            updateInterval = 1f / maxUpdatesPerSecond;
        }
    }
    
    /// <summary>
    /// Initialize the brush system
    /// </summary>
    public void Initialize()
    {
        // Get required components
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[ImageRevealBrush] RectTransform required!");
            return;
        }
        
        if (revealObj == null)
        {
            Debug.LogError("[ImageRevealBrush] Reveal object must be assigned!");
            return;
        }
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[ImageRevealBrush] Main camera not found!");
            return;
        }
        
        CreateMaskTexture();
    }
    
    /// <summary>
    /// Create a transparent mask texture that will be painted white
    /// </summary>
    private void CreateMaskTexture()
    {
        // Get the original powder texture from this GameObject's RawImage
        RawImage maskImage = GetComponent<RawImage>();
        maskImage.color = new Color(1, 1, 1, 0); // Invisible to start

        if (maskImage == null)
        {
            Debug.LogError("[ImageRevealBrush] This GameObject must have a RawImage component!");
            return;
        }
        
        // Get the reveal image and its texture
        revealImage = revealObj.GetComponent<RawImage>();
        if (revealImage == null)
        {
            Debug.LogError("[ImageRevealBrush] Reveal object must have a RawImage component!");
            return;
        }
        
        // Get the powder texture from the reveal image
        Texture2D powderTexture = revealImage.texture as Texture2D;
        if (powderTexture == null)
        {
            Debug.LogError($"[ImageRevealBrush] Reveal object '{revealObj.name}' must have a texture assigned to its RawImage component!");
            Debug.LogError($"[ImageRevealBrush] Current texture: {(revealImage.texture != null ? revealImage.texture.name : "NULL")}");
            Debug.LogError($"[ImageRevealBrush] Please assign a texture to the RawImage component on '{revealObj.name}'");
            return;
        }
        
        originalPixels = powderTexture.GetPixels32();
        for (int i = 0; i < originalPixels.Length; i++)
        {
            originalPixels[i].a = 255; // Force alpha to fully opaque
        }
        
        // Setup fingerprint image if available
        if (fingerprintObj != null)
        {
            fingerprintImage = fingerprintObj.GetComponent<RawImage>();
            if (fingerprintImage == null)
            {
                Debug.LogWarning("[ImageRevealBrush] Fingerprint object must have a RawImage component!");
            }
            else
            {
                // Don't set alpha to 0 - let the shader handle the reveal
                // The fingerprint should be visible but controlled by the mask
                fingerprintImage.color = Color.white;
            }
        }
               
        // Create transparent mask texture
        maskTexture = new Texture2D(powderTexture.width, powderTexture.height);
        
        // Fill with transparent pixels
        Color32[] maskPixels = new Color32[maskTexture.width * maskTexture.height];
        for (int i = 0; i < maskPixels.Length; i++)
        {
            maskPixels[i] = new Color32(0, 0, 0, 0); // Transparent
        }
        
        maskTexture.SetPixels32(maskPixels);
        maskTexture.Apply();
        maskTexture.wrapMode = TextureWrapMode.Clamp;

        // Create unique materials for this card
        CreateUniqueMaterials();
        
        // Apply mask to materials
        ApplyMaskToMaterials();
        
        // Assign the transparent mask texture to this GameObject's RawImage
        maskImage.texture = maskTexture;
    }

    /// <summary>
    /// Create unique materials for this card to avoid sharing issues
    /// </summary>
    private void CreateUniqueMaterials()
    {
        // Create powder material
        powderMaterial = new Material(Shader.Find("UI/AlphaReveal"));
        
        // Get duster system reference
        dusterSystem = FindFirstObjectByType<FingerPrintDusterSystem>();
        
        // Apply colors to powder material
        if (dusterSystem != null)
        {
            Color powderTint = dusterSystem.GetPowderTintColor();
            Color powderEmission = dusterSystem.GetPowderEmissionColor();
            ApplyColorsToMaterial(powderMaterial, powderTint, powderEmission, 0f, "powder");
        }
        
        // Create fingerprint material if needed
        if (fingerprintImage != null)
        {
            fingerprintMaterial = new Material(Shader.Find("UI/AlphaReveal"));
            
            // Apply colors to fingerprint material
            if (dusterSystem != null)
            {
                Color fingerprintTint = dusterSystem.GetFingerprintTintColor();
                Color fingerprintEmission = dusterSystem.GetFingerprintEmissionColor();
                // Initialize with normal intensity (1f); flip switch will override at runtime
                ApplyColorsToMaterial(fingerprintMaterial, fingerprintTint, fingerprintEmission, 1f, "fingerprint");
            }
        }
        
        // Apply textures to materials
        ApplyTexturesToMaterials();
    }
    
    /// <summary>
    /// Apply colors to a material
    /// </summary>
    private void ApplyColorsToMaterial(Material material, Color color, Color emissionColor, float emissionIntensity, string materialType)
    {
        if (material == null) return;
        
        // Apply base color
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        
        // Apply emission color and intensity
        if (material.HasProperty("_EmissionColor"))
        {
            Color finalEmissionColor = emissionColor * emissionIntensity;
            material.SetColor("_EmissionColor", finalEmissionColor);
            
            // Store original values for fingerprint materials
            if (materialType == "fingerprint")
            {
                originalFingerprintEmissionColor = emissionColor;
                originalFingerprintEmissionIntensity = emissionIntensity;
                originalFingerprintTintColor = color; // Store original tint color
            }
        }
    }
    
    /// <summary>
    /// Apply textures to the dynamically created materials
    /// </summary>
    private void ApplyTexturesToMaterials()
    {
        // Apply to powder material
        if (powderMaterial != null && revealImage != null)
        {
            Texture2D powderTexture = revealImage.texture as Texture2D;
            if (powderTexture != null)
            {
                powderMaterial.SetTexture("_MainTex", powderTexture);
                revealImage.material = powderMaterial;
            }
        }
        
        // Apply to fingerprint material
        if (fingerprintMaterial != null && fingerprintImage != null)
        {
            Texture2D fingerprintTexture = fingerprintImage.texture as Texture2D;
            if (fingerprintTexture != null)
            {
                fingerprintMaterial.SetTexture("_MainTex", fingerprintTexture);
                fingerprintImage.material = fingerprintMaterial;
            }
        }
        
        // Check for parent RectMask2D and apply appropriate masking
        ApplyParentMasking();
    }
    
    /// <summary>
    /// Apply parent masking to ensure powder/fingerprint respect the same mask as the card
    /// </summary>
    private void ApplyParentMasking()
    {
        // Find parent RectMask2D (like the one on FBDHand)
        RectMask2D parentRectMask = GetComponentInParent<RectMask2D>();
        if (parentRectMask != null)
        {
            // The RectMask2D should automatically clip child UI elements
            // But we need to ensure our materials work with it
            // For now, let's just log that we found it
        }
    }
    
    /// <summary>
    /// Apply mask to the materials
    /// </summary>
    private void ApplyMaskToMaterials()
    {
        // Apply to powder material
        if (powderMaterial != null)
        {
            powderMaterial.SetTexture("_RevealMask", maskTexture);
        }
        
        // Apply to fingerprint material
        if (fingerprintMaterial != null)
        {
            fingerprintMaterial.SetTexture("_RevealMask", maskTexture);
        }
    }
    
    /// <summary>
    /// Restore material state when card moves between holders
    /// This ensures the reveal effect persists when the card location changes
    /// </summary>
    public void RestoreMaterialState()
    {
        if (maskTexture == null)
        {
            Debug.LogWarning("[ImageRevealBrush] Cannot restore material state - missing mask texture");
            return;
        }
        
        // Reapply mask to materials
        ApplyMaskToMaterials();
    }
    
    /// <summary>
    /// Force re-initialization of materials when mask is completely empty
    /// This fixes the issue where brush doesn't work on first try after shaking off all powder
    /// </summary>
    private void ForceReinitializeMaterials()
    {
        if (maskTexture == null)
        {
            Debug.LogWarning("[ImageRevealBrush] Cannot reinitialize materials - missing mask texture");
            return;
        }
        
        // Reapply mask to materials
        ApplyMaskToMaterials();
        
        // Ensure materials are properly assigned to images
        if (revealImage != null && powderMaterial != null)
        {
            revealImage.material = powderMaterial;
        }
        
        if (fingerprintImage != null && fingerprintMaterial != null)
        {
            fingerprintImage.material = fingerprintMaterial;
        }
    }
    
    /// <summary>
    /// Force update progress and opacity (called when brush drag ends)
    /// </summary>
    public void ForceUpdate()
    {
        if (needsProgressUpdate || needsOpacityUpdate)
        {
            UpdateProgressAndOpacity();
            needsProgressUpdate = false;
            needsOpacityUpdate = false;
        }
    }
    
    /// <summary>
    /// Paint at a specific world position (called when brush head touches this mask)
    /// </summary>
    public void PaintAtWorldPosition(Vector3 worldPosition)
    {
        if (solved || !brushEnabled) 
        {
            return;
        }
        
        // Force re-initialization if mask is completely empty (after shaking off all powder)
        if (maskTexture != null && GetRevealedPercentage() <= 0.1f)
        {
            ForceReinitializeMaterials();
        }
        
        // Determine correct event camera based on parent canvas render mode
        Canvas parent = GetComponentInParent<Canvas>();
        Camera eventCam = null;
        if (parent != null && parent.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCam = parent.worldCamera != null ? parent.worldCamera : Camera.main;
        }
        
        // Convert world position to local position on this mask
        Vector2 localPosition;
        Vector3 screenPoint = (eventCam != null ? eventCam : Camera.main).WorldToScreenPoint(worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCam, out localPosition))
        {
            // Convert local position to texture coordinates
            Vector2 texturePosition = LocalToTextureCoordinates(localPosition);
            
            // Apply brush stroke at texture position
            ApplyBrushStroke(texturePosition);
        }
    }
    
    /// <summary>
    /// Convert local position to texture coordinates
    /// </summary>
    private Vector2 LocalToTextureCoordinates(Vector2 localPosition)
    {
        Vector2 pivotCancelledCursor = new Vector2(localPosition.x - rectTransform.rect.x, localPosition.y - rectTransform.rect.y);
        Vector2 normalizedCursor = new Vector2(pivotCancelledCursor.x / rectTransform.rect.width, pivotCancelledCursor.y / rectTransform.rect.height);
        return new Vector2(maskTexture.width * normalizedCursor.x, maskTexture.height * normalizedCursor.y);
    }
    
    /// <summary>
    /// Apply brush stroke at texture coordinates
    /// </summary>
    private void ApplyBrushStroke(Vector2 texturePosition)
    {
        int x, y, px, nx, py, ny, d;
        Color32[] tempArray = maskTexture.GetPixels32();
        int cx = (int)texturePosition.x;
        int cy = (int)texturePosition.y;
        
        int pixelsPainted = 0;
        
        // Debug logging to see what brush settings are being used during painting
        Debug.Log($"[ImageRevealBrush] Painting at ({cx}, {cy}) with brushRadius={brushRadius}, brushFlow={brushFlow}, brushHardness={brushHardness}");
        
        // Debug: Make very small brush radius more noticeable
        int effectiveRadius = brushRadius;
        if (brushRadius < 10)
        {
            effectiveRadius = Mathf.Max(1, brushRadius); // Ensure at least 1 pixel radius
        }
        
        for (x = 0; x <= effectiveRadius; x++)
        {
            d = (int)Mathf.Ceil(Mathf.Sqrt(effectiveRadius * effectiveRadius - x * x));
            for (y = 0; y <= d; y++)
            {
                px = cx + x;
                nx = cx - x;
                py = cy + y;
                ny = cy - y;
                
                // Calculate distance from center for hardness effect
                float distanceFromCenter = Mathf.Sqrt(x * x + y * y) / effectiveRadius;
                
                // Apply hardness effect (0 = soft edges, 1 = hard edges)
                float hardnessFactor = 1f;
                if (brushHardness < 1f)
                {
                    // Soft edges: fade out towards the edge
                    // Use a smoother curve for better control
                    float softness = 1f - brushHardness;
                    hardnessFactor = 1f - (distanceFromCenter * distanceFromCenter * softness);
                    hardnessFactor = Mathf.Clamp01(hardnessFactor);
                }
                
                // Debug: Make hardness effect more visible
                if (brushHardness > 0.8f)
                {
                    // Very hard brush: sharp cutoff
                    if (distanceFromCenter > 0.8f)
                    {
                        hardnessFactor = 0f;
                    }
                }
                
                // Apply flow effect (how much powder is applied)
                // Make flow effect more pronounced for testing
                float flowFactor = brushFlow * hardnessFactor;
                
                // Debug: Make flow effect more visible
                if (brushFlow < 0.5f)
                {
                    flowFactor *= 0.3f; // Make low flow much more visible
                }
                
                // Always paint, but with varying intensity based on flow and hardness
                Color32 paintColor = Color.white;
                
                // Use flow factor to determine alpha (transparency)
                // This creates a more realistic powder effect
                paintColor.a = (byte)(255 * flowFactor);
                
                // Use WithinRange helper for bounds checking (like BrushReveal.cs)
                if (WithinRange(py, maskTexture.height) && WithinRange(px, maskTexture.width))
                {
                    // Blend with existing paint instead of overwriting
                    Color32 existingColor = tempArray[py * maskTexture.width + px];
                    Color32 blendedColor = Color32.Lerp(existingColor, paintColor, flowFactor);
                    tempArray[py * maskTexture.width + px] = blendedColor;
                    pixelsPainted++;
                }

                if (WithinRange(ny, maskTexture.height) && WithinRange(nx, maskTexture.width))
                {
                    Color32 existingColor = tempArray[ny * maskTexture.width + nx];
                    Color32 blendedColor = Color32.Lerp(existingColor, paintColor, flowFactor);
                    tempArray[ny * maskTexture.width + nx] = blendedColor;
                    pixelsPainted++;
                }

                if (WithinRange(py, maskTexture.height) && WithinRange(nx, maskTexture.width))
                {
                    Color32 existingColor = tempArray[py * maskTexture.width + nx];
                    Color32 blendedColor = Color32.Lerp(existingColor, paintColor, flowFactor);
                    tempArray[py * maskTexture.width + nx] = blendedColor;
                    pixelsPainted++;
                }

                if (WithinRange(ny, maskTexture.height) && WithinRange(px, maskTexture.width))
                {
                    Color32 existingColor = tempArray[ny * maskTexture.width + px];
                    Color32 blendedColor = Color32.Lerp(existingColor, paintColor, flowFactor);
                    tempArray[ny * maskTexture.width + px] = blendedColor;
                    pixelsPainted++;
                }
            }
        }
        
        maskTexture.SetPixels32(tempArray);
        maskTexture.Apply();
        
        // Mark that we need updates, but don't call them immediately
        needsProgressUpdate = true;
        needsOpacityUpdate = true;
        
        // Only update progress/opacity if enough time has passed (performance optimization)
        if (enablePerformanceOptimization)
        {
            float currentTime = Time.time;
            if (currentTime - lastUpdateTime >= updateInterval)
            {
                UpdateProgressAndOpacity();
                lastUpdateTime = currentTime;
                needsProgressUpdate = false;
                needsOpacityUpdate = false;
            }
        }
        else
        {
            // Update immediately if optimization is disabled
            UpdateProgressAndOpacity();
        }
    }
    
    /// <summary>
    /// Get the current revealed percentage
    /// </summary>
    private float GetRevealedPercentage()
    {
        if (maskTexture == null || originalPixels == null) return 0f;
        
        Color32[] tempArray = maskTexture.GetPixels32();
        int totalAlphaPixelCount = 0;
        int revealedCount = 0;
        
        for (int i = 0; i < originalPixels.Length; i++)
        {
            Color32 originalPixel = originalPixels[i];
            Color32 maskPixel = tempArray[i];
            
            // Only count pixels that have alpha in the original texture
            if (originalPixel.a > 0)
            {
                totalAlphaPixelCount++;
                
                // Check if this pixel has been revealed (white in mask)
                if (maskPixel.r > 0 || maskPixel.g > 0 || maskPixel.b > 0) // Any color means revealed
                {
                    revealedCount++;
                }
            }
        }
        
        if (totalAlphaPixelCount == 0) return 0f;
        float rawPercentage = (float)revealedCount * 100 / totalAlphaPixelCount;
        
        // Apply progress offset to "cheat" the display
        float finalPercentage = rawPercentage;
        if (useProgressOffset && progressOffset > 0f)
        {
            // Convert offset to percentage (0.25 = 25%)
            float offsetPercentage = progressOffset * 100f;
            
            // Apply offset: when mask is at offset%, display as 0%
            if (rawPercentage <= offsetPercentage)
            {
                finalPercentage = 0f;
            }
            else
            {
                // Scale the remaining percentage to fill 0-100% range
                finalPercentage = ((rawPercentage - offsetPercentage) / (100f - offsetPercentage)) * 100f;
            }
            
            // Clamp to valid range
            finalPercentage = Mathf.Clamp(finalPercentage, 0f, 100f);
        }
        
        return finalPercentage;
    }
    
    /// <summary>
    /// Check if value is within range - exactly like BrushReveal.cs
    /// </summary>
    public bool WithinRange(float val, float range)
    {
        if (val >= 0 && val < range) return true; // Changed from <= to < for array bounds
        else return false;
    }
    
    /// <summary>
    /// Reset the mask to fully transparent
    /// </summary>
    public void ResetReveal()
    {
        if (maskTexture == null) return;
        
        Color32[] maskPixels = maskTexture.GetPixels32();
        for (int i = 0; i < maskPixels.Length; i++)
        {
            maskPixels[i] = new Color32(0, 0, 0, 0); // Transparent
        }
        
        maskTexture.SetPixels32(maskPixels);
        maskTexture.Apply();
        
        solved = false;
        revealedPercentage = 0f;
        OnProgressChanged?.Invoke(0f);
    }
    
    /// <summary>
    /// Set brush radius
    /// </summary>
    public void SetBrushRadius(float radius)
    {
        brushRadius = Mathf.RoundToInt(Mathf.Max(1f, radius));
    }
    
    /// <summary>
    /// Apply comprehensive brush settings to this brush
    /// </summary>
    public void ApplyBrushSettings(float radius, Color tintColor, Color emissionColor, float flow, float hardness)
    {
        // Apply brush radius (allow very small values for testing)
        brushRadius = Mathf.RoundToInt(Mathf.Max(0.1f, radius));
        
        // Store brush settings for painting algorithm
        brushFlow = Mathf.Clamp01(flow);
        brushHardness = Mathf.Clamp01(hardness);
        
        // Debug logging to see what values are actually being set
        Debug.Log($"[ImageRevealBrush] ApplyBrushSettings called on {gameObject.name}: radius={radius} -> brushRadius={brushRadius}, flow={flow} -> brushFlow={brushFlow}, hardness={hardness} -> brushHardness={brushHardness}");
        
        // Apply material settings if material exists
        if (powderMaterial != null)
        {
            // Always preserve pre-assigned colors - only apply brush functionality settings
            // Always apply flow and hardness as they're needed for brush functionality
            if (powderMaterial.HasProperty("_Flow"))
            {
                powderMaterial.SetFloat("_Flow", brushFlow);
            }
            if (powderMaterial.HasProperty("_Hardness"))
            {
                powderMaterial.SetFloat("_Hardness", brushHardness);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[ImageRevealBrush] Applied brush settings (preserving colors) to {gameObject.name}: flow={brushFlow}, hardness={brushHardness}");
            }
        }
        else
        {
            // If material doesn't exist yet, create it
            Debug.LogWarning($"[ImageRevealBrush] Powder material is null for {gameObject.name}, creating mask texture");
            CreateMaskTexture();
            
            // Try to apply settings again after creating material
            if (powderMaterial != null)
            {
                // Always preserve pre-assigned colors - only apply brush functionality settings
                // Always apply flow and hardness
                if (powderMaterial.HasProperty("_Flow"))
                {
                    powderMaterial.SetFloat("_Flow", brushFlow);
                }
                if (powderMaterial.HasProperty("_Hardness"))
                {
                    powderMaterial.SetFloat("_Hardness", brushHardness);
                }
                
                if (showDebugInfo)
                {
                    Debug.Log($"[ImageRevealBrush] Applied brush settings (preserving colors) after creation to {gameObject.name}");
                }
            }
        }
        
        // Apply same settings to fingerprint material if available
        if (fingerprintMaterial != null)
        {
            // Always preserve pre-assigned colors - only apply brush functionality settings
            // Always apply flow and hardness
            if (fingerprintMaterial.HasProperty("_Flow"))
            {
                fingerprintMaterial.SetFloat("_Flow", brushFlow);
            }
            if (fingerprintMaterial.HasProperty("_Hardness"))
            {
                fingerprintMaterial.SetFloat("_Hardness", brushHardness);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[ImageRevealBrush] Applied brush settings (preserving colors) to fingerprint material");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[ImageRevealBrush] Applied brush settings: radius={brushRadius}, flow={brushFlow}, hardness={brushHardness}");
        }
    }
    
    /// <summary>
    /// Set solve percentage
    /// </summary>
    public void SetSolvePercentage(float percentage)
    {
        solvePercentage = Mathf.Clamp(percentage, 0f, 100f);
        if (showDebugInfo)
        {
            Debug.Log($"[ImageRevealBrush] Solve percentage set to: {solvePercentage}%");
        }
    }
    
    /// <summary>
    /// Force solve percentage to 100% to allow painting until fully complete
    /// </summary>
    public void ForceSolvePercentageTo100()
    {
        solvePercentage = 100f;
        if (showDebugInfo)
        {
            Debug.Log($"[ImageRevealBrush] Forced solve percentage to 100%");
        }
    }

    /// <summary>
    /// Enable or disable brushing
    /// </summary>
    public void SetBrushEnabled(bool enabled)
    {
        brushEnabled = enabled;
    }
    
    /// <summary>
    /// Manually reset the solved flag to allow painting again
    /// </summary>
    public void ResetSolvedFlag()
    {
        if (solved)
        {
            solved = false;
            if (showDebugInfo)
            {
                Debug.Log($"[ImageRevealBrush] Manually reset solved flag - Revealed: {revealedPercentage}%");
            }
        }
    }
    
    /// <summary>
    /// Update the revealed percentage and check for completion
    /// Call this when you want to check progress (e.g., when brush drag ends)
    /// </summary>
    public void UpdateProgress()
    {
        float oldRevealedPercentage = revealedPercentage;
        revealedPercentage = GetRevealedPercentage();
        OnProgressChanged?.Invoke(revealedPercentage);
        
        if (showDebugInfo)
        {
            Debug.Log($"[ImageRevealBrush] Progress updated: {revealedPercentage}% (was: {oldRevealedPercentage}%)");
        }
        
        // Check if solved
        if (revealedPercentage >= solvePercentage && !solved)
        {
            solved = true;
            onSolved?.Invoke();
            OnRevealCompleted?.Invoke();
            
            if (showDebugInfo)
            {
                Debug.Log($"[ImageRevealBrush] Solved! Revealed: {revealedPercentage}%");
            }
        }
        // Reset solved flag if percentage drops below solve threshold
        else if (revealedPercentage < solvePercentage && solved)
        {
            solved = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"[ImageRevealBrush] Reset solved flag - Revealed: {revealedPercentage}% (below {solvePercentage}%)");
            }
        }
        
        // Always log the solved state for debugging
        if (showDebugInfo)
        {
            Debug.Log($"[ImageRevealBrush] Current state - solved: {solved}, revealed: {revealedPercentage}%, solveThreshold: {solvePercentage}%");
        }
    }
    
    // Centralized opacity update based on progress
    public void UpdatePowderAndFingerprintOpacity()
    {
        float progress = RevealPercentage;
        float powderOpacity, fingerprintOpacity;

        if (progress <= 0f)
        {
            powderOpacity = 0f;
            fingerprintOpacity = 0f;
        }
        else if (progress < 10f)
        {
            // Last 10%: Powder fades from 50% to 0%
            powderOpacity = 0.5f * (progress / 10f);
            fingerprintOpacity = progress / 10f;
        }
        else if (progress < 30f)
        {
            // 10-30%: Powder stays at 50%, fingerprint fades from 50% to 100%
            powderOpacity = 0.5f;
            fingerprintOpacity = 0.5f + (0.5f * ((progress - 10f) / 20f));
        }
        else if (progress < 80f)
        {
            // 30-80%: Powder goes from 50% to 100%, fingerprint stays at 100%
            powderOpacity = 0.5f + (0.5f * ((progress - 30f) / 50f));
            fingerprintOpacity = 1f;
        }
        else if (progress < 100f)
        {
            // 80-100%: Both stay at 100%
            powderOpacity = 1f;
            fingerprintOpacity = 1f;
        }
        else // progress == 100
        {
            powderOpacity = 1f;
            fingerprintOpacity = 1f;
        }

        // Set opacities on materials
        if (powderMaterial != null)
        {
            Color c = powderMaterial.GetColor("_Color");
            c.a = powderOpacity;
            powderMaterial.SetColor("_Color", c);
        }
        if (fingerprintMaterial != null)
        {
            Color c = fingerprintMaterial.GetColor("_Color");
            c.a = fingerprintOpacity;
            fingerprintMaterial.SetColor("_Color", c);
        }
    }

    // Call this after every brush or shake operation
    public void UpdateProgressAndOpacity()
    {
        revealedPercentage = GetRevealedPercentage();
        
        // If progress dropped below solve threshold, clear solved so painting isn't blocked
        if (solved && revealedPercentage < solvePercentage)
        {
            solved = false;
        }
        
        UpdatePowderAndFingerprintOpacity();
        // Particle system is now managed by Fingerprint
        OnProgressChanged?.Invoke(revealedPercentage);
    }

    // Controlled mask erasure for shaking
    public void ShakeOffPowder(float fadeFactor)
    {
        if (maskTexture == null) return;
        Color32[] pixels = maskTexture.GetPixels32();
        int fadedPixels = 0;
        int totalPaintedPixels = 0;

        // Count total painted pixels first
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.r > 0 || pixel.g > 0 || pixel.b > 0)
            {
                totalPaintedPixels++;
            }
        }

        if (totalPaintedPixels == 0) return; // Nothing to erase

        // Calculate how many pixels to fade this frame
        int maxPixelsToFade = Mathf.Max(1, Mathf.RoundToInt(totalPaintedPixels * maskErasureRate));
        int pixelsFadedThisFrame = 0;

        // Create a list of painted pixel positions for more controlled erasure
        List<int> paintedPixelIndices = new List<int>();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.r > 0 || pixel.g > 0 || pixel.b > 0)
            {
                paintedPixelIndices.Add(i);
            }
        }

        // Shuffle the list to simulate random shaking (but still controlled)
        for (int i = 0; i < paintedPixelIndices.Count && pixelsFadedThisFrame < maxPixelsToFade; i++)
        {
            // Randomly select pixels to fade
            int randomIndex = Random.Range(i, paintedPixelIndices.Count);
            int pixelIndex = paintedPixelIndices[randomIndex];
            
            // Swap to avoid re-selecting the same pixel
            int temp = paintedPixelIndices[i];
            paintedPixelIndices[i] = paintedPixelIndices[randomIndex];
            paintedPixelIndices[randomIndex] = temp;

            Color32 pixel = pixels[pixelIndex];
            
            // More aggressive fade factor for immediate visual feedback
            float aggressiveFadeFactor = Mathf.Max(fadeFactor * 2f, 0.1f); // At least 10% fade per frame
            
            byte newR = (byte)Mathf.Max(0, pixel.r - (byte)(aggressiveFadeFactor * 255f));
            byte newG = (byte)Mathf.Max(0, pixel.g - (byte)(aggressiveFadeFactor * 255f));
            byte newB = (byte)Mathf.Max(0, pixel.b - (byte)(aggressiveFadeFactor * 255f));
            
            if (newR == 0 && newG == 0 && newB == 0)
            {
                fadedPixels++;
                pixelsFadedThisFrame++;
            }
            
            pixels[pixelIndex] = new Color32(newR, newG, newB, pixel.a);
        }

        maskTexture.SetPixels32(pixels);
        maskTexture.Apply();

        // After erasing, update progress and opacities
        UpdateProgressAndOpacity();
    }

    /// <summary>
    /// Get the fingerprint material for external control
    /// </summary>
    public Material GetFingerprintMaterial()
    {
        return fingerprintMaterial;
    }
    
    /// <summary>
    /// Set the fingerprint emission intensity
    /// </summary>
    public void SetFingerprintEmissionIntensity(float intensity)
    {
        if (fingerprintMaterial != null && fingerprintMaterial.HasProperty("_EmissionColor"))
        {
            // Use the stored original emission color and apply the new intensity
            Color newEmission = originalFingerprintEmissionColor * intensity;
            fingerprintMaterial.SetColor("_EmissionColor", newEmission);
            
            // Ensure the original tint color is preserved
            if (fingerprintMaterial.HasProperty("_Color"))
            {
                Color tint = originalFingerprintTintColor;
                // Preserve current alpha
                Color current = fingerprintMaterial.GetColor("_Color");
                tint.a = current.a;
                fingerprintMaterial.SetColor("_Color", tint);
            }
        }
    }
    
    /// <summary>
    /// Apply fingerprint light state: sets tint to the provided base color (preserving current alpha)
    /// and sets emission to emissionColor * emissionIntensity.
    /// </summary>
    public void SetFingerprintLightState(Color baseTintColor, Color emissionColor, float emissionIntensity)
    {
        if (fingerprintMaterial == null) return;

        // Preserve current alpha from material to keep opacity curve intact
        float currentAlpha = 1f;
        if (fingerprintMaterial.HasProperty("_Color"))
        {
            Color c = fingerprintMaterial.GetColor("_Color");
            currentAlpha = c.a;
        }

        if (fingerprintMaterial.HasProperty("_Color"))
        {
            Color newTint = baseTintColor;
            newTint.a = currentAlpha;
            fingerprintMaterial.SetColor("_Color", newTint);
        }

        if (fingerprintMaterial.HasProperty("_EmissionColor"))
        {
            Color finalEmission = emissionColor * Mathf.Max(0f, emissionIntensity);
            fingerprintMaterial.SetColor("_EmissionColor", finalEmission);
        }
    }
    
    // --- INTEGRATION POINTS ---
    // In your brushing code (e.g., after ApplyBrushStroke), call UpdateProgressAndOpacity().
    // In your shaking code (from Fingerprint), call ShakeOffPowder(fadeFactor).
    
    private void OnDestroy()
    {
        if (maskTexture != null)
        {
            DestroyImmediate(maskTexture);
        }
        
        if (powderMaterial != null)
        {
            DestroyImmediate(powderMaterial);
        }
        
        if (fingerprintMaterial != null)
        {
            DestroyImmediate(fingerprintMaterial);
        }
    }
} 