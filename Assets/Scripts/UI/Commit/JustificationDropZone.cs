using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

namespace Detective.UI.Commit
{
    public class JustificationDropZone : MonoBehaviour, IDropHandler
    {
        [Header("UI References")]
        [SerializeField] private Transform tagsContainer;
        [SerializeField] private GameObject justificationTagPrefab;

        private Dictionary<string, GameObject> attachedTags = new Dictionary<string, GameObject>();

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag != null)
            {
                var draggableTag = eventData.pointerDrag.GetComponent<DraggableTag>();
                if (draggableTag != null)
                {
                    string tagContent = draggableTag.GetTagContent();
                    string tagId = draggableTag.GetTagID(); // Assuming DraggableTag has a way to get a unique ID.

                    if (!string.IsNullOrEmpty(tagId) && !attachedTags.ContainsKey(tagId))
                    {
                        AddJustificationTag(tagContent, tagId);
                    }
                    // Restore the original tag in the notebook so it can be used again.
                    draggableTag.RestoreOriginalTagOnly();
                }
            }
        }

        private void AddJustificationTag(string content, string id)
        {
            if (justificationTagPrefab == null) return;

            GameObject tagGO = Instantiate(justificationTagPrefab, tagsContainer);
            tagGO.GetComponentInChildren<TextMeshProUGUI>().text = content;
            
            // Add a button component to allow removal
            Button removeButton = tagGO.GetComponent<Button>();
            if (removeButton == null) removeButton = tagGO.AddComponent<Button>();

            removeButton.onClick.AddListener(() => RemoveJustificationTag(id));

            attachedTags.Add(id, tagGO);
        }

        private void RemoveJustificationTag(string id)
        {
            if (attachedTags.TryGetValue(id, out GameObject tagGO))
            {
                Destroy(tagGO);
                attachedTags.Remove(id);
            }
        }

        public List<string> GetAttachedClueIds()
        {
            return new List<string>(attachedTags.Keys);
        }

        public void Clear()
        {
            foreach (var tagGO in attachedTags.Values)
            {
                Destroy(tagGO);
            }
            attachedTags.Clear();
        }
    }
}
