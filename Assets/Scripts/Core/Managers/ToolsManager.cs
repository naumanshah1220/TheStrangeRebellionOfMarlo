using System.Collections.Generic;
using UnityEngine;

public class ToolsManager : SingletonMonoBehaviour<ToolsManager>
{

    public List<Tool> tools = new List<Tool>();
    public int activatedToolIndex = -1;


    private DragManager dM;
    public Camera mainCamera;

    protected override void OnSingletonAwake()
    {
        if (!mainCamera)
            mainCamera = Camera.main;
    }

    private void Start()
    {
        dM = DragManager.Instance;
        
        // Find all tools in scene
        FindAllTools();
    }

    public void FindAllTools()
    {
        // Find all tools in scene (or populate manually)
        tools.Clear();
        foreach (var tool in FindObjectsByType<Tool>(FindObjectsSortMode.None))
        {
            tools.Add(tool);
        }
    }

    private void Update()
    {
        if (dM.IsDragging && dM.CurrentCard)
        {
            // 1. Clear all tools' hover states and trigger hover end events
            for (int i = 0; i < tools.Count; i++)
            {
                // If tool was previously hovered, trigger hover end event
                if (tools[i].isHovering && dM.CurrentCard != null)
                {
                    tools[i].OnCardHoverEnd(dM.CurrentCard);
                }
            }

            // 2. Set for the hovered tool that can accept the card
            for (int i = 0; i < tools.Count; i++)
            {
                if (tools[i] == null) continue;

                // Allow an override hover rect on the tool for more precise hit areas
                RectTransform rect = tools[i].hoverRectOverride != null ? tools[i].hoverRectOverride : tools[i].GetComponent<RectTransform>();
                bool isOverTool = IsPointerOverArea(Input.mousePosition, rect);

                if (isOverTool)
                {
                    // Check if tool can accept this card
                    if (tools[i].CanAcceptCard(dM.CurrentCard))
                    {
                        tools[i].OnCardHoverStart(dM.CurrentCard);
                        break; // Only hover one tool at a time
                    }
                }
            }
        }
        else
        {
            // Not dragging: clear all and trigger hover end events
            for (int i = 0; i < tools.Count; i++)
            {
                // If tool was previously hovered, trigger hover end event
                if (tools[i].isHovering && dM.CurrentCard != null)
                {
                    tools[i].OnCardHoverEnd(dM.CurrentCard);
                }
            }
        }
    }

    // Called when a card is dropped on a tool
    public void OnEvidenceDroppedOnTool(Tool tool, Card card)
    {
        // Check if tool can accept this card
        if (!tool.CanAcceptCard(card))
        {
            Debug.LogWarning($"[ToolsManager] Tool '{tool.displayName}' cannot accept card '{card.name}'");
            return;
        }
        
        // Let the tool handle the card drop - tool will delegate to its system
        tool.OnCardDropped(card);
    }
    
    private bool IsPointerOverArea(Vector2 screenPos, RectTransform area)
    {
        if (area == null) return false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPos, mainCamera, out var localPoint);
        return area.rect.Contains(localPoint);
    }
}
