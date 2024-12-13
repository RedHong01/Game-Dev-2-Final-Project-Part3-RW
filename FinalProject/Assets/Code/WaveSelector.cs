using UnityEngine;

public class WaveSelector : MonoBehaviour
{
    [Header("Wave Selector Settings")]
    public string targetSceneName; // 目标场景名称
    public int targetWaveNumber;   // 指定波次编号

    public void SelectWave()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("Target scene name is not set. Please configure it in the Inspector.");
            return;
        }

        if (targetWaveNumber < 1)
        {
            Debug.LogError("Target wave number must be greater than 0. Please configure it in the Inspector.");
            return;
        }

        // 通过 GameManager 切换场景并设置波次
      //  GameManager.Instance.LoadSceneAndWave(targetSceneName, targetWaveNumber);
    }
}