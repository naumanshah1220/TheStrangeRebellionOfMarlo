using System;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;


public class BrushReveal : MonoBehaviour
{
    public GameObject revealObj;
    public UnityEvent onSolved;
    public int r = 50;

    [Range(0, 100)]
    public float solvePercentage = 100f;

    RectTransform rectTransform;
    Texture2D texture;
    Color32 [] originalPixels;
    bool solved = false;

    void Start()
    {
        RawImage img = GetComponent<RawImage>();
        Texture2D tex = (Texture2D)img.texture;

        revealObj.GetComponent<RawImage>().texture = tex;
        originalPixels = tex.GetPixels32();
        texture = new Texture2D(tex.width, tex.height);

        Color32[] pixels = new Color32[tex.width * tex.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(0, 0, 0, 0);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        img.texture = texture;

    }

    public void OnDrag(PointerEventData eventData)
    {

        if (solved) return;

        Vector2 localCursor = new Vector2();
        
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor)) 
        {
            Vector2 pivotCancelledCursor = new Vector2(localCursor.x - rectTransform.rect.x, localCursor.y - rectTransform.rect.y);
            
           if (WithinRange(pivotCancelledCursor.x, rectTransform.rect.width) && WithinRange(pivotCancelledCursor.y, rectTransform.rect.height)) 
           {
               Vector2 normalizedCursor = new Vector2(pivotCancelledCursor.x/rectTransform.rect.width, pivotCancelledCursor.y/rectTransform.rect.height);
               Vector2 coordinateInImage = new Vector2(texture.width * normalizedCursor.x, texture.height * normalizedCursor.y);

               int x, y, px, nx, py, ny, d;
               Color32[] tempArray = texture.GetPixels32();
               int cx = (int) coordinateInImage.x;
               int cy = (int) coordinateInImage.y;

               for (x = 0; x <= r; x++)
               {
                    d = (int) Mathf.Ceil(Mathf.Sqrt(r * r - x * x));
                    for (y = 0; y <= r; y++)
                    {
                        px = cx + x;
                        nx = cx - x;
                        py = cy + y;
                        ny = cy - y;

                        if (WithinRange(py, texture.height) && WithinRange(px, texture.width))
                        {
                            tempArray[py * texture.width + px] = Color.white;
                        }

                        if (WithinRange(ny, texture.height) && WithinRange(nx, texture.width))
                        {
                            tempArray[ny * texture.width + nx] = Color.white;
                        }

                        if (WithinRange(py, texture.height) && WithinRange(nx, texture.width))
                        {
                            tempArray[py * texture.width + nx] = Color.white;
                        }

                        if (WithinRange(ny, texture.height) && WithinRange(px, texture.width))
                        {
                            tempArray[ny * texture.width + px] = Color.white;
                        }                                     

                    }
               }

                texture.SetPixels32(tempArray);
                texture.Apply();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (solved) return;

        Color32[] tempArray = texture.GetPixels32();
        int totalAlphaPixelCount = 0;
        int revealedCount = 0;

        for (int i = 0; i < originalPixels.Length; i++)
        {
            Color32 pixel = originalPixels[i];
            if (pixel.a == 255)
            {
                totalAlphaPixelCount++;

                if (tempArray[i].a == 255)
                {
                    revealedCount++;
                }

            }
        }

        float revealedPercentage = (float) revealedCount * 100/ totalAlphaPixelCount;

        if (revealedPercentage >= solvePercentage)
        {
            solved = true;

            for (int i=0; i<tempArray.Length; i++)
            {
                tempArray[i] = Color.white;

            }

            texture.SetPixels32(tempArray);
            texture.Apply();

            onSolved.Invoke();

        }
    }

    public bool WithinRange(float val, float range)
    {
        if (val >= 0 && val <= range) return true;
        else return false;
    }
}