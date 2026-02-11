using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to any UI element to block drag events from reaching a parent ScrollRect.
/// </summary>
public class BlockScrollRectDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }
}