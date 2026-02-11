using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Sprite animation player for displaying animated sprites frame by frame
/// </summary>
public class VideoPlayerApp : ComputerApp, IFileContent
{
    [Header("Animation Player References")]
    [SerializeField] private Image frameDisplay;
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text frameInfoText;
    [SerializeField] private Slider frameSlider;
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button previousFrameButton;
    [SerializeField] private Button nextFrameButton;
    [SerializeField] private Button saveFrameButton;
    [SerializeField] private Button loopButton;
    
    [Header("Settings")]
    [SerializeField] private float defaultFrameRate = 12f; // Frames per second
    [SerializeField] private bool autoPlay = false;
    
    private DiscFile currentFile;
    private List<Sprite> animationFrames = new List<Sprite>();
    private int currentFrameIndex = 0;
    private bool isPlaying = false;
    private bool isLooping = false;
    private bool isDraggingSlider = false;
    private float frameTimer = 0f;
    private float frameInterval;
    
    public override void Initialize(AppConfig appConfig)
    {
        base.Initialize(appConfig);
        
        // Calculate frame interval
        frameInterval = 1f / defaultFrameRate;
        
        // Set up UI controls
        if (playPauseButton != null)
        {
            playPauseButton.onClick.AddListener(TogglePlayPause);
        }
        
        if (stopButton != null)
        {
            stopButton.onClick.AddListener(Stop);
        }
        
        if (previousFrameButton != null)
        {
            previousFrameButton.onClick.AddListener(PreviousFrame);
        }
        
        if (nextFrameButton != null)
        {
            nextFrameButton.onClick.AddListener(NextFrame);
        }
        
        if (saveFrameButton != null)
        {
            saveFrameButton.onClick.AddListener(SaveCurrentFrame);
        }
        
        if (loopButton != null)
        {
            loopButton.onClick.AddListener(ToggleLoop);
        }
        
        if (frameSlider != null)
        {
            frameSlider.onValueChanged.AddListener(OnFrameSliderChanged);
        }
    }
    
    public void Initialize(DiscFile file)
    {
        currentFile = file;
        
        if (file == null) return;
        
        // Set file name
        if (fileNameText != null)
        {
            fileNameText.text = file.fileName;
        }
        
        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = file.fileDescription;
        }
        
        // Load animation content
        LoadAnimationContent();
    }
    
    private void LoadAnimationContent()
    {
        if (currentFile == null) return;
        
        // Try to get animation from content prefab first (new system)
        if (currentFile.contentPrefab != null)
        {
            // The content prefab should contain the animation frames
            // This will be handled by the prefab itself
            return;
        }
        
        // Legacy image content is no longer supported
        // Use content prefab instead
        Debug.LogWarning($"[VideoPlayer] Legacy image content not supported for file: {currentFile.fileName}");
            if (fileNameText != null)
            {
            fileNameText.text = "No animation content available";
        }
    }
    
    private void SetupAnimationPlayer()
    {
        if (animationFrames.Count == 0) return;
        
        // Set up frame slider
        if (frameSlider != null)
        {
            frameSlider.minValue = 0f;
            frameSlider.maxValue = animationFrames.Count - 1;
            frameSlider.value = 0f;
        }
        
        // Display first frame
        currentFrameIndex = 0;
        DisplayCurrentFrame();
        
        // Auto-play if configured
        if (autoPlay && animationFrames.Count > 1)
        {
            Play();
        }
    }
    
    private void DisplayCurrentFrame()
    {
        if (frameDisplay == null || currentFrameIndex < 0 || currentFrameIndex >= animationFrames.Count) return;
        
        frameDisplay.sprite = animationFrames[currentFrameIndex];
        
        // Update frame info
        if (frameInfoText != null)
        {
            frameInfoText.text = $"Frame {currentFrameIndex + 1} of {animationFrames.Count}";
        }
        
        // Update slider (without triggering the callback)
        if (frameSlider != null && !isDraggingSlider)
        {
            frameSlider.value = currentFrameIndex;
        }
    }
    
    public void TogglePlayPause()
    {
        if (animationFrames.Count <= 1) return;
        
        if (isPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }
    
    public void Play()
    {
        if (animationFrames.Count <= 1) return;
        
        isPlaying = true;
        frameTimer = 0f;
        UpdatePlayPauseButton();
    }
    
    public void Pause()
    {
        isPlaying = false;
        UpdatePlayPauseButton();
    }
    
    public void Stop()
    {
        isPlaying = false;
        currentFrameIndex = 0;
        frameTimer = 0f;
        DisplayCurrentFrame();
        UpdatePlayPauseButton();
    }
    
    public void PreviousFrame()
    {
        if (animationFrames.Count == 0) return;
        
        currentFrameIndex--;
        if (currentFrameIndex < 0)
        {
            if (isLooping)
            {
                currentFrameIndex = animationFrames.Count - 1;
            }
            else
            {
                currentFrameIndex = 0;
            }
        }
        
        DisplayCurrentFrame();
    }
    
    public void NextFrame()
    {
        if (animationFrames.Count == 0) return;
        
        currentFrameIndex++;
        if (currentFrameIndex >= animationFrames.Count)
        {
            if (isLooping)
            {
                currentFrameIndex = 0;
            }
            else
            {
                currentFrameIndex = animationFrames.Count - 1;
                isPlaying = false;
                UpdatePlayPauseButton();
            }
        }
        
        DisplayCurrentFrame();
    }
    
    public void ToggleLoop()
    {
        isLooping = !isLooping;
        UpdateLoopButton();
    }
    
    public void SaveCurrentFrame()
    {
        if (currentFrameIndex < 0 || currentFrameIndex >= animationFrames.Count) return;
        
        Sprite currentSprite = animationFrames[currentFrameIndex];
        if (currentSprite == null) return;
        
        // Create a new DiscFile for the saved frame
        DiscFile savedFrame = CreateFrameFile(currentSprite);
        
        if (savedFrame != null)
        {
            Debug.Log($"[VideoPlayer] Saved frame {currentFrameIndex + 1} as: {savedFrame.fileName}");
            
            // TODO: Add the saved frame to the current disc's folder
            // This would require integration with the folder system
        }
    }
    
    private DiscFile CreateFrameFile(Sprite sprite)
    {
        // Create a new DiscFile asset for the saved frame
        DiscFile frameFile = ScriptableObject.CreateInstance<DiscFile>();
        frameFile.fileName = $"{currentFile.fileName}_frame_{currentFrameIndex + 1:D3}";
        frameFile.fileType = FileType.Photo;
        frameFile.fileDescription = $"Frame {currentFrameIndex + 1} from {currentFile.fileName}";
        
        // Note: Legacy imageContent is no longer supported
        // TODO: Implement content prefab system for frame saving
        // This would require proper asset creation and saving
        
        return frameFile;
    }
    
    private void OnFrameSliderChanged(float value)
    {
        if (isDraggingSlider) return;
        
        int newFrameIndex = Mathf.RoundToInt(value);
        if (newFrameIndex != currentFrameIndex && newFrameIndex >= 0 && newFrameIndex < animationFrames.Count)
        {
            currentFrameIndex = newFrameIndex;
            DisplayCurrentFrame();
        }
    }
    
    private void UpdatePlayPauseButton()
    {
        if (playPauseButton == null) return;
        
        // Update button text or icon based on play state
        var buttonText = playPauseButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isPlaying ? "Pause" : "Play";
        }
    }
    
    private void UpdateLoopButton()
    {
        if (loopButton == null) return;
        
        // Update button text or icon based on loop state
        var buttonText = loopButton.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.text = isLooping ? "Loop: On" : "Loop: Off";
        }
    }
    
    private void Update()
    {
        if (!isPlaying || animationFrames.Count <= 1) return;
        
        frameTimer += Time.deltaTime;
        
        if (frameTimer >= frameInterval)
        {
            frameTimer = 0f;
            NextFrame();
        }
    }
    
    public override void OnAppOpen()
    {
        base.OnAppOpen();
        
        // Reset player state
        isPlaying = false;
        isLooping = false;
        currentFrameIndex = 0;
        frameTimer = 0f;
        
        UpdatePlayPauseButton();
        UpdateLoopButton();
        
        if (frameSlider != null)
        {
            frameSlider.value = 0f;
        }
        
        if (frameInfoText != null)
        {
            frameInfoText.text = "No animation loaded";
        }
    }
    
    public override void OnAppClose()
    {
        // Stop animation when app closes
        isPlaying = false;
        
        base.OnAppClose();
    }
} 