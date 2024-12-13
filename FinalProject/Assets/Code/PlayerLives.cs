using UnityEngine;
using UnityEngine.SceneManagement;  // 引入 SceneManager 以便切换场景
using TMPro;  // 用于更新 TextMeshPro 的血量显示

public class PlayerLives : Lives
{
    public TextMeshProUGUI healthText;  // 用于显示玩家血量的 TextMeshPro 对象
    public string gameOverSceneName;  // 场景切换时的目标场景名称

    protected override void Start()
    {
        base.Start();
        UpdateHealthText();  // 初始化时更新血量显示
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);  // 调用父类的伤害处理逻辑

        // 每次受到伤害时，更新 UI 上的血量显示
        UpdateHealthText();
    }

    // 更新血量显示的方法
    private void UpdateHealthText()
    {
        if (healthText != null)
        {
            healthText.text = currentHealth.ToString("F0");  // 显示整数形式的血量
        }
    }

    protected override void Die()
    {
        base.Die();  // 调用父类中的死亡处理

        // 玩家死亡后切换到 Game Over 场景
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}
