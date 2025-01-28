using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // 用于场景切换
using TMPro; // 用于显示血量 UI

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(GunController))]
public class Player : Lives
{
    [Header("UI Settings")]
    public TextMeshProUGUI healthText; // 用于显示玩家血量的 TextMeshPro
    public string gameOverSceneName;   // 玩家死亡后切换的场景名称

    [Header("Health Bar Settings")]
    public Image currentHealthImage;   // 实时血量条
    public Image delayHealthImage;     // 延迟血量条
    public float delayTime = 1f;       // 延迟血量条参数

    [Header("Player Movement")]
    public float moveSpeed = 5f;       // 玩家移动速度
    private Camera viewCamera;
    private PlayerController controller;
    private GunController gunController;

    // 用于记录延迟血量
    private float delayHealth;

    // 用于协程控制
    private Coroutine updateHealth;

    protected override void Start()
    {
        base.Start();
        controller = GetComponent<PlayerController>();
        gunController = GetComponent<GunController>();
        viewCamera = Camera.main;

        delayHealth = startingHealth; // 初始化延迟血量

        // 尝试找到 Health 和 Damage 对象
        AssignHealthBarObjects();

        UpdateHealthUI(); // 初始化时更新血量显示
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage); // 调用 Lives 的伤害逻辑
        SetHealth(); // 更新血量 UI 和延迟条逻辑
    }

    void Update()
    {
        // 玩家移动输入
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 moveVelocity = moveInput.normalized * moveSpeed;
        controller.Move(moveVelocity);

        // 玩家面朝方向
        Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            controller.LookAt(point);
        }

        // 开火
        if (Input.GetMouseButton(0))
        {
            gunController.Shoot();
        }
    }

    protected override void Die()
    {
        SavePlayerData(); // 保存当前玩家数据
        base.Die(); // 调用基类的死亡逻辑
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName); // 切换到 Game Over 场景
        }
    }

    private void SavePlayerData()
    {
        // LastPlayerData = new PlayerData
        // {
        //     health = currentHealth,
        //     moveSpeed = moveSpeed,
        //     position = transform.position,
        //     rotation = transform.rotation
        // };
    }

    private void UpdateHealthUI()
    {
        if (currentHealthImage != null)
        {
            // 更新实时血量条的填充比例
            currentHealthImage.fillAmount = currentHealth / startingHealth;
        }

        if (healthText != null)
        {
            // 更新血量文本
            healthText.text = Mathf.Ceil(currentHealth).ToString("F0");
        }
    }

    private void SetHealth()
    {
        if (updateHealth != null)
        {
            StopCoroutine(updateHealth); // 血条停止更新
        }
        updateHealth = StartCoroutine(UpdateEffectImage()); // 启动新的协程
    }

    private IEnumerator UpdateEffectImage()
    {
        // 更新实时血量条
        currentHealthImage.fillAmount = currentHealth / startingHealth;

        // 计算延迟条
        float length = (delayHealth - currentHealth) / startingHealth;

        // 平滑更新延迟血量条
        while (delayHealthImage.fillAmount - currentHealthImage.fillAmount > 0)
        {
            delayHealthImage.fillAmount -= 0.01f * length / delayTime;
            yield return new WaitForSeconds(0.01f);
        }

        delayHealthImage.fillAmount = currentHealthImage.fillAmount;
        delayHealth = currentHealth;
    }

    private void AssignHealthBarObjects()
    {
        
        GameObject healthObject = GameObject.Find("Health");
        if (healthObject != null)
        {
            currentHealthImage = healthObject.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("Health object not found in the scene!");
        }

        
        GameObject damageObject = GameObject.Find("Damage");
        if (damageObject != null)
        {
            delayHealthImage = damageObject.GetComponent<Image>();
        }
        else
        {
            Debug.LogWarning("Damage object not found in the scene!");
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        public float health; // 当前血量
        public float moveSpeed; // 移动速度
        public Vector3 position; // 位置信息
        public Quaternion rotation; // 旋转信息
    }
}