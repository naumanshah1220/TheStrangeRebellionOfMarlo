using UnityEngine;
using UnityEngine.EventSystems;
public class DraggableBullet : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string tagText;
    // (Optionally, store reference to CluesManager or the clue/note it came from)

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Visual feedback (highlight, bring to front, etc)
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Move tag with mouse
        transform.position = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        // Check if dropped on valid target
        // (use EventSystem raycast or custom slot detection)
        // Call target logic (form field, evidence, etc)
    }
}