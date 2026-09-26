using UnityEngine;
using UnityEngine.EventSystems;

public class ObstaclePaletteItem :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    public GameObject obstaclePrefab;

    ObstacleDragManager dragManager;

    void Awake()
    {
        dragManager = FindObjectOfType<ObstacleDragManager>();

        if (dragManager == null)
        {
            Debug.LogError(
                "ObstaclePaletteItem: ObstacleDragManager was not found."
            );
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (dragManager == null)
        {
            return;
        }

        dragManager.BeginPaletteDrag(obstaclePrefab, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragManager == null)
        {
            return;
        }

        dragManager.ContinuePaletteDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragManager == null)
        {
            return;
        }

        dragManager.EndPaletteDrag(eventData);
    }
}