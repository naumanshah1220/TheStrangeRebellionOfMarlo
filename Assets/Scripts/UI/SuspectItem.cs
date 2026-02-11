using UnityEngine;
using UnityEngine.UI;
using TMPro;

    public class SuspectItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image suspectPhoto;
        public TextMeshProUGUI suspectNameText;
        public TextMeshProUGUI citizenIdText;
        
        private Citizen suspect;
        
        public void Setup(Citizen suspectData)
        {
            suspect = suspectData;
            
            if (suspectPhoto != null && suspect.picture != null)
                suspectPhoto.sprite = suspect.picture;
            
            if (suspectNameText != null)
                suspectNameText.text = suspect.GetFullName();
            
            if (citizenIdText != null)
                citizenIdText.text = $"ID: {suspect.citizenID}";
        }
        
        public Citizen GetSuspect()
        {
            return suspect;
        }
    } 