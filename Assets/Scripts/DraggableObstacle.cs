using UnityEngine;

public class DraggableObstacle : MonoBehaviour
{
    ObstacleDragManager dragManager;
    bool isDragging;

    public void SetManager(ObstacleDragManager manager)
    {
        dragManager = manager;
    }

    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
    }

    void Awake()
    {
        if (dragManager == null)
        {
            dragManager = FindObjectOfType<ObstacleDragManager>();
        }
    }

    void OnMouseDown()
    {
        if (dragManager == null)
        {
            Debug.LogError(
                "DraggableObstacle: ObstacleDragManager was not found."
            );

            return;
        }

        Vector2 pointerPosition = Input.mousePosition;

        dragManager.BeginExistingObstacleDrag(this, pointerPosition);
    }

    void OnMouseDrag()
    {
        if (!isDragging || dragManager == null)
        {
            return;
        }

        Vector2 pointerPosition = Input.mousePosition;

        dragManager.ContinueExistingObstacleDrag(pointerPosition);
    }

    void OnMouseUp()
    {
        if (!isDragging || dragManager == null)
        {
            return;
        }

        dragManager.EndExistingObstacleDrag();
    }
}