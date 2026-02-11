using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective.UI
{
    public class SuspectDetailsUI : MonoBehaviour
    {
        public Image suspectImage;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI citizenIdText;

        private Citizen suspect;

        public void Setup(Citizen suspect)
        {
            this.suspect = suspect;
            
            if (suspectImage != null)
            {
                suspectImage.sprite = suspect.GetPortraitSprite();
            }
            
            if (nameText != null)
            {
                nameText.text = suspect.GetFullName();
            }
            
            if (citizenIdText != null)
            {
                citizenIdText.text = suspect.GetCitizenId();
            }
        }

        public Citizen GetSuspect()
        {
            return suspect;
        }
    }
} 