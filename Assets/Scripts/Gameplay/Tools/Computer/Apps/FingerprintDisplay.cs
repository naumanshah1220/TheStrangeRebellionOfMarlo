using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple component for displaying fingerprint images
/// </summary>
public class FingerprintDisplay : MonoBehaviour
{
    [SerializeField] private Image fingerprintImage;
    
    public void SetFingerprint(Sprite fingerprint)
    {
        if (fingerprintImage != null)
        {
            fingerprintImage.sprite = fingerprint;
            fingerprintImage.preserveAspect = true;
        }
    }
    
    public void ClearFingerprint()
    {
        if (fingerprintImage != null)
        {
            fingerprintImage.sprite = null;
        }
    }
} 