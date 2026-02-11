using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Detective.UI.Commit
{
    public class VerdictOptionButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Button button;
        [SerializeField] private GameObject highlightFrame;

        private VerdictOption option;
        private System.Action<VerdictOption> onSelect;

        private void Awake()
        {
            button.onClick.AddListener(HandleClick);
        }

        public void Setup(VerdictOption verdictOption, System.Action<VerdictOption> onSelectCallback)
        {
            option = verdictOption;
            onSelect = onSelectCallback;
            label.text = option.label;
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightFrame != null)
            {
                highlightFrame.SetActive(isSelected);
            }
        }

        private void HandleClick()
        {
            onSelect?.Invoke(option);
        }
    }
}
