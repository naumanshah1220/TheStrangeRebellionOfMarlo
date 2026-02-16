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

    /// <summary>
    /// Creates a runtime SlideshowData from JSON lore data.
    /// Loads background images from Resources/ when paths are provided.
    /// </summary>
    public static SlideshowData CreateFromJson(LoreJson json)
    {
        if (json == null || json.slides == null || json.slides.Count == 0)
            return null;

        var data = CreateInstance<SlideshowData>();
        data.name = "LoreSlideshowData (JSON)";

        foreach (var slide in json.slides)
        {
            var s = new SlideshowSlide
            {
                title = slide.title ?? "",
                bodyText = slide.bodyText ?? ""
            };

            if (!string.IsNullOrEmpty(slide.backgroundImagePath))
                s.backgroundImage = Resources.Load<Sprite>(slide.backgroundImagePath);

            data.slides.Add(s);
        }

        return data;
    }
}
