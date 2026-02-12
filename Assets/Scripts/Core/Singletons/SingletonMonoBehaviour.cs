using UnityEngine;

/// <summary>
/// Generic singleton base class for MonoBehaviour managers.
/// Provides automatic Instance management with duplicate detection and cleanup.
/// Usage: public class MyManager : SingletonMonoBehaviour&lt;MyManager&gt; { }
/// Override OnSingletonAwake() instead of Awake() for initialization logic.
/// Override OnSingletonDestroy() instead of OnDestroy() for cleanup logic.
/// </summary>
public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = (T)(MonoBehaviour)this;
        OnSingletonAwake();
    }

    /// <summary>
    /// Called once when the singleton is first initialized.
    /// Use this instead of Awake() in derived classes.
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            OnSingletonDestroy();
            Instance = null;
        }
    }

    /// <summary>
    /// Called when the singleton instance is being destroyed.
    /// Use this instead of OnDestroy() in derived classes.
    /// </summary>
    protected virtual void OnSingletonDestroy() { }
}
