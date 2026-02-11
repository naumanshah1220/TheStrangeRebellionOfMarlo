using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Image viewer app for displaying photo files
/// </summary>
public class ImageViewer : ComputerApp, IFileContent
{
    [Header("Image Viewer References")]
    [SerializeField] private Image imageDisplay;
    [SerializeField] private ScrollRect imageScrollView;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button fitToWindowButton;
    [SerializeField] private Transform contentContainer; // Container for content prefabs
    
    [Header("Settings")]
    [SerializeField] private float[] zoomLevels = { 0.5f, 1f, 1.5f, 2f };
    
    private DiscFile currentFile;
    private int currentZoomIndex = 1; // Start at 1x (index 1)
    private Vector2 originalSize;
    
    public override void Initialize(AppConfig appConfig)
    {
        base.Initialize(appConfig);
        
        // Set up zoom buttons
        if (zoomInButton != null)
        {
            zoomInButton.onClick.AddListener(ZoomIn);
        }
        
        if (zoomOutButton != null)
        {
            zoomOutButton.onClick.AddListener(ZoomOut);
        }
        
        if (fitToWindowButton != null)
        {
            fitToWindowButton.onClick.AddListener(FitToWindow);
        }
        
        // Set up scroll view
        if (imageScrollView != null)
        {
            imageScrollView.horizontal = true;
            imageScrollView.vertical = true;
        }
    }
    
    public void Initialize(DiscFile file)
    {
        currentFile = file;
        
        if (file == null) return;
        
        // Load image content
        LoadImageContent();
    }
    
    private void LoadImageContent()
    {
        if (currentFile == null) return;
        
        // Clear any existing content
        ClearContent();
        
        // Try to get image from content prefab first (new system)
        if (currentFile.contentPrefab != null)
        {
            // Instantiate the content prefab
            if (contentContainer != null)
            {
                GameObject contentInstance = Instantiate(currentFile.contentPrefab, contentContainer);
                
                // Try to get an Image component from the content prefab
                Image contentImage = contentInstance.GetComponent<Image>();
                if (contentImage != null)
                {
                    // Use this image for zooming and fitting
                    SetupImageForViewing(contentImage);
                }
                else
                {
                    // Fallback to legacy image content
                    LoadLegacyImageContent();
                }
            }
            else
            {
                LoadLegacyImageContent();
            }
        }
        else
        {
            // Fallback to legacy image content
            LoadLegacyImageContent();
        }
    }
    
    private void LoadLegacyImageContent()
    {
        if (imageDisplay == null) return;
        
        // Legacy image content is no longer supported
        // Use content prefab or file description instead
        Debug.LogWarning($"[ImageViewer] Legacy image content not supported for file: {currentFile.fileName}");
        
        if (imageDisplay != null)
        {
            imageDisplay.sprite = null;
        }
    }
    
    private void SetupImageForViewing(Image image)
    {
        if (image == null) return;
        
        image.preserveAspect = true;
        
        // Wait a frame for the image to be properly set up, then get its actual displayed size
        StartCoroutine(SetupImageSizeAfterFrame(image));
    }
    
    private System.Collections.IEnumerator SetupImageSizeAfterFrame(Image image)
    {
        // Wait for the end of frame to ensure the image is properly set up
        yield return new WaitForEndOfFrame();
        
        // Store the original size and scale of the image
        originalSize = image.rectTransform.sizeDelta;
        Vector3 originalScale = image.transform.localScale;
        
        // Set ScrollView content size and scale to match the image
        if (imageScrollView != null && imageScrollView.content != null)
        {
            imageScrollView.content.sizeDelta = originalSize;
            imageScrollView.content.localScale = originalScale;
            LayoutRebuilder.ForceRebuildLayoutImmediate(imageScrollView.content);
        }
        
        // Start at 1x zoom initially
        currentZoomIndex = 1;
        ApplyZoom();
        UpdateZoomButtons();
    }
    
    private void ClearContent()
    {
        if (contentContainer != null)
        {
            // Destroy all child objects
            for (int i = contentContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(contentContainer.GetChild(i).gameObject);
            }
        }
    }
    
    public void ZoomIn()
    {
        if (currentZoomIndex < zoomLevels.Length - 1)
        {
            currentZoomIndex++;
            ApplyZoom();
            UpdateZoomButtons();
        }
    }
    
    public void ZoomOut()
    {
        if (currentZoomIndex > 0)
        {
            currentZoomIndex--;
            ApplyZoom();
            UpdateZoomButtons();
        }
    }
    
    public void FitToWindow()
    {
        // Try to get the current image (either from content prefab or main display)
        Image currentImage = GetCurrentImage();
        if (currentImage == null || currentImage.sprite == null) 
        {
            return;
        }
        
        // Calculate fit-to-window zoom
        RectTransform contentRect = imageScrollView != null ? imageScrollView.content : currentImage.rectTransform;
        if (contentRect == null) return;
        
        Vector2 viewportSize = imageScrollView != null ? imageScrollView.viewport.rect.size : contentRect.rect.size;
        Vector2 imageSize = currentImage.sprite.rect.size;
        
        float scaleX = viewportSize.x / imageSize.x;
        float scaleY = viewportSize.y / imageSize.y;
        float fitZoom = Mathf.Min(scaleX, scaleY, 1f); // Don't zoom in beyond 100%
        
        // Find the closest zoom level
        int closestIndex = 1; // Default to 1x
        float closestDistance = Mathf.Abs(zoomLevels[1] - fitZoom);
        
        for (int i = 0; i < zoomLevels.Length; i++)
        {
            float distance = Mathf.Abs(zoomLevels[i] - fitZoom);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        
        currentZoomIndex = closestIndex;
        ApplyZoom();
        UpdateZoomButtons();
    }
    
    private void ApplyZoom()
    {
        float zoomLevel = zoomLevels[currentZoomIndex];
        
        Image currentImage = GetCurrentImage();
        if (currentImage != null)
        {
            // Scale the image GameObject
            GameObject imageGameObject = currentImage.gameObject;
            imageGameObject.transform.localScale = Vector3.one * zoomLevel;
            
            // Scale the ScrollView content to match
            if (imageScrollView != null && imageScrollView.content != null)
            {
                imageScrollView.content.localScale = Vector3.one * zoomLevel;
                LayoutRebuilder.ForceRebuildLayoutImmediate(imageScrollView.content);
            }
            
            // Force layout update on the image
            LayoutRebuilder.ForceRebuildLayoutImmediate(currentImage.rectTransform);
            
            // Additional check - if scale didn't change, try scaling the parent
            if (imageGameObject.transform.localScale != Vector3.one * zoomLevel)
            {
                Transform parent = imageGameObject.transform.parent;
                if (parent != null)
                {
                    parent.localScale = Vector3.one * zoomLevel;
                }
            }
        }
    }
    
    private void UpdateZoomButtons()
    {
        bool canZoomIn = currentZoomIndex < zoomLevels.Length - 1;
        bool canZoomOut = currentZoomIndex > 0;
        
        if (zoomInButton != null)
        {
            zoomInButton.interactable = canZoomIn;
        }
        
        if (zoomOutButton != null)
        {
            zoomOutButton.interactable = canZoomOut;
        }
    }
    
    private Image GetCurrentImage()
    {
        // First try to get image from content container
        if (contentContainer != null && contentContainer.childCount > 0)
        {
            Image contentImage = contentContainer.GetChild(0).GetComponent<Image>();
            if (contentImage != null)
            {
                return contentImage;
            }
        }
        
        // Fallback to main image display
        return imageDisplay;
    }
    
    public override void OnAppOpen()
    {
        base.OnAppOpen();
        
        // Reset zoom when app opens
        currentZoomIndex = 1; // Reset to 1x
        Image currentImage = GetCurrentImage();
        if (currentImage != null)
        {
            currentImage.gameObject.transform.localScale = Vector3.one;
            
            // Reset ScrollView content size and scale
            if (imageScrollView != null && imageScrollView.content != null)
            {
                imageScrollView.content.sizeDelta = originalSize;
                imageScrollView.content.localScale = Vector3.one;
                LayoutRebuilder.ForceRebuildLayoutImmediate(imageScrollView.content);
            }
        }
        UpdateZoomButtons();
    }
} 