using UnityEngine;
using UnityEngine.EventSystems;

public class ObstacleDragManager : MonoBehaviour
{
    [Header("References")]
    public Camera worldCamera;
    public Transform obstacleParent;

    [Header("Map Bounds")]
    public Vector2 mapCenter = Vector2.zero;
    public Vector2 mapSize = new Vector2(60f, 35f);

    GameObject draggingObstacle;
    Vector2 draggingOffset;

    void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }
    }

    public void BeginPaletteDrag(GameObject obstaclePrefab, PointerEventData eventData)
    {
        if (obstaclePrefab == null)
        {
            Debug.LogError("ObstacleDragManager: obstacle prefab is not assigned.");
            return;
        }

        Vector2 worldPosition = ScreenToWorldPosition(eventData.position);

        draggingObstacle = Instantiate(
            obstaclePrefab,
            worldPosition,
            Quaternion.identity,
            obstacleParent
        );

        draggingObstacle.name = obstaclePrefab.name;

        DraggableObstacle draggableObstacle =
            draggingObstacle.GetComponent<DraggableObstacle>();

        if (draggableObstacle == null)
        {
            Debug.LogError(
                "ObstacleDragManager: obstacle prefab must contain DraggableObstacle."
            );

            Destroy(draggingObstacle);
            draggingObstacle = null;
            return;
        }

        draggableObstacle.SetManager(this);
        draggableObstacle.SetDragging(true);

        draggingOffset = Vector2.zero;
        MoveDraggingObstacle(eventData.position);
    }

    public void ContinuePaletteDrag(PointerEventData eventData)
    {
        if (draggingObstacle == null)
        {
            return;
        }

        MoveDraggingObstacle(eventData.position);
    }

    public void EndPaletteDrag(PointerEventData eventData)
    {
        if (draggingObstacle == null)
        {
            return;
        }

        bool releasedOverUi = EventSystem.current != null &&
                              EventSystem.current.IsPointerOverGameObject(
                                  eventData.pointerId
                              );

        if (releasedOverUi)
        {
            Destroy(draggingObstacle);
        }
        else
        {
            MoveDraggingObstacle(eventData.position);

            DraggableObstacle draggableObstacle =
                draggingObstacle.GetComponent<DraggableObstacle>();

            draggableObstacle.SetDragging(false);
        }

        draggingObstacle = null;
    }

    public void BeginExistingObstacleDrag(
        DraggableObstacle obstacle,
        Vector2 pointerScreenPosition
    )
    {
        if (obstacle == null)
        {
            return;
        }

        draggingObstacle = obstacle.gameObject;
        draggingOffset =
            (Vector2)draggingObstacle.transform.position -
            ScreenToWorldPosition(pointerScreenPosition);

        obstacle.SetDragging(true);
    }

    public void ContinueExistingObstacleDrag(Vector2 pointerScreenPosition)
    {
        if (draggingObstacle == null)
        {
            return;
        }

        Vector2 worldPosition =
            ScreenToWorldPosition(pointerScreenPosition) + draggingOffset;

        draggingObstacle.transform.position =
            ClampToMapBounds(draggingObstacle, worldPosition);
    }

    public void EndExistingObstacleDrag()
    {
        if (draggingObstacle == null)
        {
            return;
        }

        DraggableObstacle draggableObstacle =
            draggingObstacle.GetComponent<DraggableObstacle>();

        if (draggableObstacle != null)
        {
            draggableObstacle.SetDragging(false);
        }

        draggingObstacle = null;
        draggingOffset = Vector2.zero;
    }

    void MoveDraggingObstacle(Vector2 pointerScreenPosition)
    {
        if (draggingObstacle == null)
        {
            return;
        }

        Vector2 worldPosition =
            ScreenToWorldPosition(pointerScreenPosition) + draggingOffset;

        draggingObstacle.transform.position =
            ClampToMapBounds(draggingObstacle, worldPosition);
    }

    Vector2 ScreenToWorldPosition(Vector2 screenPosition)
    {
        if (worldCamera == null)
        {
            Debug.LogError("ObstacleDragManager: world camera is not assigned.");
            return Vector2.zero;
        }

        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(worldCamera.transform.position.z)
        );

        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(screenPoint);

        return new Vector2(worldPoint.x, worldPoint.y);
    }

    Vector2 ClampToMapBounds(GameObject obstacle, Vector2 position)
    {
        Collider2D obstacleCollider =
            obstacle.GetComponent<Collider2D>();

        if (obstacleCollider == null)
        {
            Debug.LogError(
                "ObstacleDragManager: obstacle prefab must contain Collider2D."
            );

            return position;
        }

        Bounds obstacleBounds = obstacleCollider.bounds;

        float halfWidth = obstacleBounds.extents.x;
        float halfHeight = obstacleBounds.extents.y;

        float minX = mapCenter.x - mapSize.x * 0.5f + halfWidth;
        float maxX = mapCenter.x + mapSize.x * 0.5f - halfWidth;
        float minY = mapCenter.y - mapSize.y * 0.5f + halfHeight;
        float maxY = mapCenter.y + mapSize.y * 0.5f - halfHeight;

        return new Vector2(
            Mathf.Clamp(position.x, minX, maxX),
            Mathf.Clamp(position.y, minY, maxY)
        );
    }
}