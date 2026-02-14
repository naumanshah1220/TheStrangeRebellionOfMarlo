using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button quitButton;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeDuration = 0.8f;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Detective 6.0";

    private void Start()
    {
        if (newGameButton) newGameButton.onClick.AddListener(OnNewGame);
        if (continueButton)
        {
            continueButton.onClick.AddListener(OnContinue);
            continueButton.interactable = false; // Disabled until save system exists
        }
        if (quitButton) quitButton.onClick.AddListener(OnQuit);

        // Fade in from black on start
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 1f;
            fadeOverlay.DOFade(0f, fadeDuration).SetEase(Ease.OutQuad)
                .OnComplete(() => fadeOverlay.blocksRaycasts = false);
        }
    }

    private void OnNewGame()
    {
        FlowBootstrap.SetNewGame();
        TransitionToGame();
    }

    private void OnContinue()
    {
        // TODO: Load save data to determine day
        FlowBootstrap.SetContinueGame(1);
        TransitionToGame();
    }

    private void TransitionToGame()
    {
        // Disable buttons during transition
        if (newGameButton) newGameButton.interactable = false;
        if (continueButton) continueButton.interactable = false;
        if (quitButton) quitButton.interactable = false;

        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            fadeOverlay.DOFade(1f, fadeDuration).SetEase(Ease.InQuad)
                .OnComplete(() => SceneManager.LoadScene(gameSceneName));
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
