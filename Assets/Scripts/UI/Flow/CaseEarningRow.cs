using UnityEngine;
using TMPro;

public class CaseEarningRow : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text rewardText;

    public void Setup(string title, float reward)
    {
        if (titleText) titleText.text = title;
        if (rewardText) rewardText.text = $"+${reward:F0}";
    }
}
