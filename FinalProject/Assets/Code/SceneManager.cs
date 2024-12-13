using UnityEngine;
using UnityEngine.SceneManagement;  // 引入 SceneManager 以便切换场景

public class SceneSwitcher : MonoBehaviour
{
    // 场景名称，或者你也可以使用场景的索引
    public string sceneName;

    // 这个方法可以绑定到按钮的点击事件
    public void SwitchScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);  // 切换到指定的场景
        }
        else
        {
            Debug.LogWarning("Scene name is empty or null!");
        }
    }
}
