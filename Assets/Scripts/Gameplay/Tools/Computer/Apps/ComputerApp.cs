using UnityEngine;
using UnityEngine.UI;

public abstract class ComputerApp : MonoBehaviour
{
    protected AppConfig config;
    protected RectTransform contentArea;
    protected ScrollRect scrollView;
    
    public virtual void Initialize(AppConfig appConfig)
    {
        config = appConfig;
        contentArea = GetComponent<RectTransform>();
        scrollView = GetComponent<ScrollRect>();
    }
    
    public virtual void OnAppOpen()
    {
        gameObject.SetActive(true);
    }
    
    public virtual void OnAppClose()
    {
        gameObject.SetActive(false);
    }
    
    public virtual void OnAppFocus()
    {
        // Called when window is brought to front
    }
    
    public virtual void OnAppUnfocus()
    {
        // Called when window is no longer in front
    }
    
    protected virtual void OnEnable()
    {
        OnAppOpen();
    }
    
    protected virtual void OnDisable()
    {
        OnAppClose();
    }
} 