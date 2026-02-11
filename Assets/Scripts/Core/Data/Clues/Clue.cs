using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class Clue : MonoBehaviour, IPointerClickHandler
{
    [Header("Clue Settings")]
    public string clueID;
    public int pageNumber; // Which page of the card is the clue on
    [TextArea(3, 10)]
    public string noteText;  // The text to add to notebook, can use <person>name</person>, <location>place</location>, etc.

    [Header("Visual Feedback")]
    public bool hideAfterFound = true;
    public GameObject visualFeedback;  // Optional particle effect or highlight

    private bool isFound = false;
    private Button button;
    private NotebookManager notebook;

    private void Awake()
    {
        button = GetComponent<Button>();
        notebook = FindFirstObjectByType<NotebookManager>();

        if (!notebook)
            Debug.LogError("NotebookManager not found in scene!");

        if (visualFeedback)
            visualFeedback.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isFound)
        {
            FindClue();
        }
        else
        {
            // Clue already found, try to highlight it in the notebook
            notebook.AddClueNote(noteText, clueID);
        }
    }

    public void FindClue()
    {
        if (isFound) return;

        isFound = true;

        // Show visual feedback if any
        if (visualFeedback)
        {
            visualFeedback.SetActive(true);
            // You might want to add animation or particle effect here
        }

        // Add note to notebook
        notebook.AddClueNote(noteText, clueID);

        // Hide the clue if specified
        if (hideAfterFound)
        {
            button.interactable = false;
            // You might want to fade out or animate the disappearance
        }

        // Notify any listeners (like case progress tracking)
        CluesManager.Instance?.OnClueFound(this);
    }

    public bool GetIsFound() => isFound;

    // Optional: Method to reset clue state (useful for new game or testing)
    public void Reset()
    {
        isFound = false;
        button.interactable = true;
        if (visualFeedback)
            visualFeedback.SetActive(false);
    }
}
