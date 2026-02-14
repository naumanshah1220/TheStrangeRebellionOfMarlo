using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SlideshowSlide
{
    public string title;
    [TextArea(3, 8)]
    public string bodyText;
    public Sprite backgroundImage;
}

[CreateAssetMenu(fileName = "NewSlideshowData", menuName = "Detective/Slideshow Data")]
public class SlideshowData : ScriptableObject
{
    public List<SlideshowSlide> slides = new List<SlideshowSlide>();
}
