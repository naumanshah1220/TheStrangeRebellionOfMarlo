using UnityEngine;
using UnityEngine.UI;
using TMPro;

    public class CrimeItem : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI crimeNameText;
        public TextMeshProUGUI crimeDescriptionText;
        public Image crimeIcon;
        
        private string crimeName;
        private string crimeDescription;
        
        public void Setup(string name, string description = "")
        {
            crimeName = name;
            crimeDescription = description;
            
            if (crimeNameText != null)
                crimeNameText.text = crimeName;
            
            if (crimeDescriptionText != null)
                crimeDescriptionText.text = crimeDescription;
        }
        
        public string GetCrimeName()
        {
            return crimeName;
        }
        
        public string GetCrimeDescription()
        {
            return crimeDescription;
        }
    } 