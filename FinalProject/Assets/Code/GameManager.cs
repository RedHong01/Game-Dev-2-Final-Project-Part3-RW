using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Destroy Zone")]
      public Spawner spawner;
    public Collider destroyZone;
    public string playerTag = "Player";

    [Header("Player Settings")]
    public GameObject playerPrefab;
    private GameObject playerInstance;
    private bool isPlayerDestroyed = false;

    [Header("Timer Settings")]
    public float countdownTime = 60f;
    public string nextSceneName;
    public TextMeshProUGUI countdownText;

    [Header("Score Settings")]
    public int score = 0;

    private float initialCountdownTime;
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;

    private void Start()
    {
        // 查找场景中带有 Player Tag 的对象并记录其初始位置
        GameObject existingPlayer = GameObject.FindGameObjectWithTag(playerTag);
        if (existingPlayer != null)
        {
            initialPlayerPosition = existingPlayer.transform.position;
            initialPlayerRotation = existingPlayer.transform.rotation;

            // 将场景中的玩家对象赋值为当前玩家实例
            playerInstance = existingPlayer;
        }
        else if (playerPrefab != null)
        {
            // 如果场景中没有 Player Tag 对象，则实例化一个玩家
            SpawnPlayer();
        }

        initialCountdownTime = countdownTime;
    }

    private void Update()
    {
        countdownTime -= Time.deltaTime;
        if (countdownText != null)
            countdownText.text = "Time Remaining: " + Mathf.CeilToInt(countdownTime);

        if (countdownTime <= 0)
            SwitchToNextScene();

        if (playerInstance == null && !isPlayerDestroyed)
        {
            isPlayerDestroyed = true;
            RespawnPlayer();
        }

        if (Input.GetKeyDown(KeyCode.R)) // 使用 R 键重置游戏
            ResetGame();
    }

    private void SpawnPlayer()
    {
        if (playerPrefab != null)
        {
            playerInstance = Instantiate(playerPrefab, initialPlayerPosition, initialPlayerRotation);
            playerInstance.tag = playerTag; // 确保新实例有正确的 Tag
            isPlayerDestroyed = false;
        }
    }

    private void RespawnPlayer()
    {
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
        SpawnPlayer();
    }

    private void ResetGame()
    {
        // 重置计分
        score = 0;

        // 重置计时器
        countdownTime = initialCountdownTime;

        // 删除旧玩家并生成新玩家
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
        SpawnPlayer();
        // 重置波次
        if (spawner != null)
        {
            spawner.ResetWave();
        }

        // 更新 UI
        if (countdownText != null)
            countdownText.text = "Time Remaining: " + Mathf.CeilToInt(countdownTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            HandlePlayerDestruction(other.gameObject);
        }
    }

    private void HandlePlayerDestruction(GameObject player)
    {
        if (player == playerInstance)
        {
            isPlayerDestroyed = true;
            Destroy(playerInstance);
        }
    }

    private void SwitchToNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    public void AddScore()
    {
        score++;
    }
}