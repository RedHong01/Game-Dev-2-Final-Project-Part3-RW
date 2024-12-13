using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5f; // 摄像机的移动速度
    public float scrollSensitivity = 5f; // 滚轮缩放灵敏度
    public Vector3 minPosition; // 摄像机的最小位置
    public Vector3 maxPosition; // 摄像机的最大位置
    public RectTransform canvasRectTransform; // 引用 Canvas 的 RectTransform

    private Vector3 initialPosition; // 摄像机初始位置
    private bool isLocked; // 是否锁定摄像机
    private bool isDraggingGameBoard; // 是否正在拖拽游戏板

    private void Start()
    {
        initialPosition = transform.position; // 记录初始位置
    }

    private void Update()
    {
        HandleInput();
        if (!isLocked && !isDraggingGameBoard)
        {
            HandleCanvasBasedMovement(); // 基于 Canvas 的鼠标移动逻辑
            HandleScrollZoom();
        }
        ClampCameraPosition();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) isDraggingGameBoard = true;
        if (Input.GetMouseButtonUp(0)) isDraggingGameBoard = false;
        if (Input.GetKeyDown(KeyCode.Space)) isLocked = !isLocked;
        if (Input.GetKeyDown(KeyCode.C)) ResetCameraPosition();
    }

    private void HandleCanvasBasedMovement()
    {
        // 获取鼠标在 Canvas 空间中的位置
        Vector2 mousePosition = Input.mousePosition;

        // 将鼠标屏幕坐标转换为 Canvas 内部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, mousePosition, null, out Vector2 localPoint);

        // 计算鼠标相对于 Canvas 中心的偏移量
        Vector2 canvasCenter = canvasRectTransform.rect.size / 2f;
        Vector2 offset = localPoint - canvasCenter;

        // 根据偏移量计算方向
        Vector3 direction = new Vector3(offset.x, 0, offset.y).normalized;

        // 根据偏移量比例计算移动距离
        float distanceFromCenter = offset.magnitude;
        float moveDistance = distanceFromCenter / (canvasRectTransform.rect.width / 2f) * moveSpeed * Time.deltaTime;

        // 更新摄像机位置（预计算新的位置）
        Vector3 newPosition = transform.position + direction * moveDistance;

        // 限制新的位置
        newPosition.x = Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x);
        newPosition.z = Mathf.Clamp(newPosition.z, minPosition.z, maxPosition.z);

        // 应用更新后的位置
        transform.position = newPosition;
    }

    private void HandleScrollZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        Vector3 newPosition = transform.position + Vector3.up * scrollInput * scrollSensitivity;

        // 限制新的位置
        newPosition.x = Mathf.Clamp(newPosition.x, minPosition.x, maxPosition.x);
        newPosition.y = Mathf.Clamp(newPosition.y, minPosition.y, maxPosition.y);
        newPosition.z = Mathf.Clamp(newPosition.z, minPosition.z, maxPosition.z);

        transform.position = newPosition;
    }

    private void ClampCameraPosition()
    {
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minPosition.x, maxPosition.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, minPosition.y, maxPosition.y);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minPosition.z, maxPosition.z);
        transform.position = clampedPosition;
    }

    private void ResetCameraPosition()
    {
        transform.position = initialPosition;
    }
}