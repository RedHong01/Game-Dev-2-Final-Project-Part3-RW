using UnityEngine;
using System;
using System.Collections;

public class Lives : MonoBehaviour, Damageable
{
    [Header("Health Settings")]
    [SerializeField] public float startingHealth; // 初始生命值
    protected float currentHealth; // 当前血量
    protected bool isDead;          // 是否已死亡
    protected bool dead;            // 是否已死亡

    public event Action OnDeath; // 死亡事件

    [Header("Hit Effect Settings")]
    public AudioClip hitSound; // 受击音效
    public float hitEffectDuration = 0.2f; // 受击效果持续时间
    public Color hitEffectColor = Color.red; // 受击时的颜色
    private Color originalColor; // 原始颜色

    private AudioSource audioSource; // 音频播放组件
    private Renderer objectRenderer; // 渲染器，用于变色

    protected virtual void Start()
    {
        currentHealth = startingHealth; // 初始化血量

        // 初始化音效组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && hitSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 初始化渲染器和颜色
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalColor = objectRenderer.material.color;
        }
    }

    public virtual void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"{gameObject.name} took {damage} damage.");
        // 播放受击音效
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // 开启受击效果
        if (objectRenderer != null)
        {
            StartCoroutine(PlayHitEffect());
        }

        // 调用伤害处理
        TakeDamage(damage);
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage; // 扣减血量

        if (currentHealth <= 0 && !dead)
        {
            Die(); // 血量为零且未死亡时调用死亡逻辑
        }
    }

    [ContextMenu("Self Destruct")]
    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        dead = true;
        OnDeath?.Invoke(); // 触发死亡事件
        Destroy(gameObject); // 销毁当前对象
    }

    private IEnumerator PlayHitEffect()
    {
        // 改变颜色为受击颜色
        if (objectRenderer != null)
        {
            objectRenderer.material.color = hitEffectColor;
        }

        // 等待受击效果持续时间
        yield return new WaitForSeconds(hitEffectDuration);

        // 恢复原始颜色
        if (objectRenderer != null)
        {
            objectRenderer.material.color = originalColor;
        }
    }
}