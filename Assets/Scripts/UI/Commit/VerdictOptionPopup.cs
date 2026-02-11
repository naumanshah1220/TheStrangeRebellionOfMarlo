using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

namespace Detective.UI.Commit
{
    public class VerdictOptionPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Button closeButton;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionButtonPrefab;
        [SerializeField] private Button prevPageButton;
        [SerializeField] private Button nextPageButton;
        [SerializeField] private TextMeshProUGUI pageInfoText;
        [SerializeField] private CanvasGroup canvasGroup;


        private List<VerdictOption> allOptions;
        private List<VerdictOption> filteredOptions;
        private System.Action<VerdictOption> onOptionSelected;
        private string currentSelectionId;
        
        private int currentPage = 0;
        private int itemsPerPage = 8; // Reduced to fit "Unknown" and have better spacing
        private int totalPages = 0;

        private void Awake()
        {
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (searchInput != null) searchInput.onValueChanged.AddListener(OnSearchChanged);
            if (prevPageButton != null) prevPageButton.onClick.AddListener(PrevPage);
            if (nextPageButton != null) nextPageButton.onClick.AddListener(NextPage);
        }

        public void Show(string title, List<VerdictOption> options, System.Action<VerdictOption> callback, string preselectedOptionId = null)
        {
            titleText.text = title;
            currentSelectionId = preselectedOptionId;

            // Create a mutable list and add "Unknown" at the start
            allOptions = new List<VerdictOption>(options);
            allOptions.Insert(0, new VerdictOption { id = "unknown", label = "Unknown" });

            onOptionSelected = callback;
            
            if (searchInput != null)
            {
                searchInput.text = "";
            }
            OnSearchChanged("");
            
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = true;
            gameObject.SetActive(true);
            DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 1f, 0.3f);
        }

        private void OnSearchChanged(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                filteredOptions = new List<VerdictOption>(allOptions);
            }
            else
            {
                string lowerQuery = query.ToLower().Trim();
                // Make sure to include "Unknown" if it matches, or always include it
                filteredOptions = allOptions.Where(opt => opt.label.ToLower().Contains(lowerQuery)).ToList();
            }
            currentPage = 0;
            UpdatePagination();
            DisplayPage(currentPage);
        }
        
        private void UpdatePagination()
        {
            totalPages = Mathf.CeilToInt((float)filteredOptions.Count / itemsPerPage);
            if (totalPages == 0) totalPages = 1; // Always show at least one page
            if (prevPageButton != null) prevPageButton.interactable = currentPage > 0;
            if (nextPageButton != null) nextPageButton.interactable = currentPage < totalPages - 1;
            if (pageInfoText != null) pageInfoText.text = $"{currentPage + 1} / {totalPages}";
        }

        private void DisplayPage(int pageIndex)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }

            int startIndex = pageIndex * itemsPerPage;
            for (int i = 0; i < itemsPerPage; i++)
            {
                int currentIndex = startIndex + i;
                if (currentIndex >= filteredOptions.Count) break;

                var option = filteredOptions[currentIndex];
                var buttonGO = Instantiate(optionButtonPrefab, optionsContainer);
                var optionButton = buttonGO.GetComponent<VerdictOptionButton>();
                
                optionButton.Setup(option, SelectOption);

                // Check if this option should be highlighted
                bool isSelected = (!string.IsNullOrEmpty(currentSelectionId) && option.id == currentSelectionId) || (string.IsNullOrEmpty(currentSelectionId) && option.id == "unknown");
                optionButton.SetSelected(isSelected);
            }
        }

        private void SelectOption(VerdictOption option)
        {
            onOptionSelected?.Invoke(option);
            Close();
        }

        private void PrevPage()
        {
            if (currentPage > 0)
            {
                currentPage--;
                UpdatePagination();
                DisplayPage(currentPage);
            }
        }

        private void NextPage()
        {
            if (currentPage < totalPages - 1)
            {
                currentPage++;
                UpdatePagination();
                DisplayPage(currentPage);
            }
        }

        public void Close()
        {
            if (canvasGroup == null)
            {
                if(gameObject != null) Destroy(gameObject);
                return;
            }

            // Use DOFade, a DOTween extension method that is safer and handles destroyed objects gracefully.
            canvasGroup.DOFade(0f, 0.3f).OnComplete(() => {
                if (gameObject != null)
                {
                    Destroy(gameObject);
                }
            });
        }

        private void OnDestroy()
        {
            // Kill any tweens targeting this object's components to prevent errors after destruction.
            if (canvasGroup != null)
            {
                DOTween.Kill(canvasGroup);
            }
        }
    }
}
