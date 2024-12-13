using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public Wave[] waves; // 波次配置
    public Enemy enemyPrefab; // 敌人预制件
    public GameObject restPhaseObject; // 休整阶段显示的对象
    public TMP_Text waveText; // 用于显示波次文字的 TMP 组件
    public float restPhaseDuration = 5f; // 休整阶段持续时间
    public float fadeSpeed = 1f; // 控制淡入淡出的速度

    [Header("Spawner State")]
    private MapGenerator mapGenerator; // 地图生成器

    private Wave currentWave;
    private int currentWaveNumber = 0; // 当前波次编号

    private int enemiesRemainingToSpawn;
    private int enemiesRemainingAlive;
    private float nextSpawnTime;

    private bool isWaveActive = false; // 当前波次是否进行中
    private bool waveFinished = false; // 当前波次是否结束

    private List<Enemy> activeEnemies = new List<Enemy>(); // 跟踪所有活动的敌人

    void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();

        if (mapGenerator == null)
        {
            Debug.LogError("MapGenerator not found in the scene. Please ensure a MapGenerator component is present.");
            return;
        }

        // 确保 restPhaseObject 初始为 inactive
        if (restPhaseObject != null)
        {
            restPhaseObject.SetActive(false);
        }

        // 第一次调用 RestPhase
        StartCoroutine(RestPhase(true));
    }

    public void ResetWave()
    {
        Debug.Log($"Resetting Wave {currentWaveNumber + 1}...");

        // 停止当前波次的所有活动
        StopAllCoroutines();

        // 销毁所有活跃敌人
        foreach (Enemy enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        activeEnemies.Clear();

        // 重置波次状态
        isWaveActive = false;
        waveFinished = false;

        // 重新生成地图
        UpdateMapForWave(currentWaveNumber);

        // 重新启动当前波次
        StartWave(currentWaveNumber);
    }

    private void StartWave(int waveIndex)
    {
        if (waveIndex >= waves.Length)
        {
            Debug.Log("All waves completed!");
            return;
        }

        Debug.Log($"Starting Wave {waveIndex + 1}");
        currentWave = waves[waveIndex];
        enemiesRemainingToSpawn = currentWave.enemyCount;
        enemiesRemainingAlive = currentWave.enemyCount;

        UpdateMapForWave(waveIndex);

        isWaveActive = true;
        waveFinished = false;
        StartCoroutine(SpawnWave());
    }

    private IEnumerator SpawnWave()
    {
        while (enemiesRemainingToSpawn > 0)
        {
            enemiesRemainingToSpawn--;
            StartCoroutine(SpawnEnemy());
            yield return new WaitForSeconds(currentWave.timeBetweenSpawns);
        }

        // 等待所有敌人被消灭
        while (enemiesRemainingAlive > 0)
        {
            yield return null;
        }

        // 进入下一阶段
        waveFinished = true;
        isWaveActive = false;

        StartCoroutine(RestPhase(false));
    }

    private IEnumerator SpawnEnemy()
    {
        float spawnDelay = 1f; // 生成延迟
        float tileFlashSpeed = 4f; // 瓦片闪烁速度

        Transform spawnTile = mapGenerator.GetRandomOpenTile();
        if (spawnTile == null)
        {
            Debug.LogError("No valid open tiles available for spawning.");
            yield break;
        }

        Renderer tileRenderer = spawnTile.GetComponent<Renderer>();
        Material tileMaterial = tileRenderer.material;
        Color initialColour = tileMaterial.color;
        Color flashColour = Color.red;
        float spawnTimer = 0f;

        while (spawnTimer < spawnDelay)
        {
            tileMaterial.color = Color.Lerp(initialColour, flashColour, Mathf.PingPong(Time.time * tileFlashSpeed, 1));
            spawnTimer += Time.deltaTime;
            yield return null;
        }

        tileMaterial.color = initialColour;
        Vector3 spawnPosition = spawnTile.position + Vector3.up;
        Enemy spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        spawnedEnemy.OnDeath += OnEnemyDeath;
        activeEnemies.Add(spawnedEnemy);
    }

    private void OnEnemyDeath()
    {
        enemiesRemainingAlive--;

        if (enemiesRemainingAlive == 0)
        {
            waveFinished = true;
            isWaveActive = false;

            // 进入下一波次
            if (currentWaveNumber < waves.Length - 1)
            {
                currentWaveNumber++;
                Debug.Log($"Wave {currentWaveNumber + 1} is now active.");
            }
            else
            {
                Debug.Log("All waves completed!");
            }
        }
    }

    private IEnumerator RestPhase(bool isBeforeFirstWave)
    {
        Debug.Log("Entering Rest Phase...");

        waveFinished = false;
        isWaveActive = false;

        CanvasGroup canvasGroup = restPhaseObject != null ? restPhaseObject.GetComponent<CanvasGroup>() : null;

        if (restPhaseObject != null && canvasGroup == null)
        {
            canvasGroup = restPhaseObject.AddComponent<CanvasGroup>();
        }

        if (restPhaseObject != null)
        {
            restPhaseObject.SetActive(true);

            if (canvasGroup != null)
            {
                float elapsedTime = 0f;

                while (canvasGroup.alpha < 1)
                {
                    elapsedTime += Time.deltaTime * fadeSpeed;
                    canvasGroup.alpha = Mathf.Clamp01(elapsedTime);
                    yield return null;
                }
                canvasGroup.alpha = 1;
            }

            if (waveText != null)
            {
                waveText.text = isBeforeFirstWave ? "Prepare for the first wave!" : $"Wave {currentWaveNumber + 1}";
            }
        }

        yield return new WaitForSeconds(restPhaseDuration);

        if (canvasGroup != null)
        {
            float elapsedTime = 0f;

            while (canvasGroup.alpha > 0)
            {
                elapsedTime += Time.deltaTime * fadeSpeed;
                canvasGroup.alpha = Mathf.Clamp01(1 - elapsedTime);
                yield return null;
            }
            canvasGroup.alpha = 0;
        }

        if (restPhaseObject != null)
        {
            restPhaseObject.SetActive(false);
        }

        if (isBeforeFirstWave)
        {
            StartWave(currentWaveNumber);
        }
        else if (waveFinished)
        {
            StartWave(currentWaveNumber);
        }
    }

    private void UpdateMapForWave(int waveIndex)
    {
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap(waveIndex); // 更新地图索引并重新生成地图
            Debug.Log($"Map updated for Wave {waveIndex + 1}");
        }
    }

    [System.Serializable]
    public class Wave
    {
        public int enemyCount;
        public float timeBetweenSpawns;
    }
}