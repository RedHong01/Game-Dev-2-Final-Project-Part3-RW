using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections; // 添加此命名空间以支持 IEnumerator

public class UIElementController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Fade Settings")]
    public CanvasGroup uiCanvasGroup; // UI 的 CanvasGroup 控制透明度
    [Range(0.1f, 5f)] public float fadeSpeed = 1f; // 控制淡入淡出的速度（Inspector 可调整，共享参数）

    private Coroutine fadeCoroutine; // 控制当前的淡入或淡出协程
    private bool isPointerOver = false; // 指针是否在 UI 区域内

    void Start()
    {
        // 确保 CanvasGroup 存在
        if (uiCanvasGroup == null)
        {
            uiCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (uiCanvasGroup == null)
        {
            Debug.LogError("No CanvasGroup found! Please assign a CanvasGroup component to this UI element.");
        }

        // 初始化透明度
        uiCanvasGroup.alpha = 1;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 鼠标进入时，触发淡出
        isPointerOver = true;
        StartFade(0);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标移出时，触发淡入
        isPointerOver = false;
        StartFade(1);
    }

    private void StartFade(float targetAlpha)
    {
        // 如果已有淡入淡出的协程，停止它
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        // 启动新的淡入或淡出协程
        fadeCoroutine = StartCoroutine(FadeToAlpha(targetAlpha));
    }

    private IEnumerator FadeToAlpha(float targetAlpha)
    {
        float startAlpha = uiCanvasGroup.alpha; // 当前透明度
        float elapsedTime = 0f;

        while (!Mathf.Approximately(uiCanvasGroup.alpha, targetAlpha))
        {
            elapsedTime += Time.deltaTime * fadeSpeed;
            uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime);
            yield return null; // 等待下一帧
        }

        uiCanvasGroup.alpha = targetAlpha; // 确保透明度最终达到目标值
        fadeCoroutine = null; // 清除当前协程
    }
}