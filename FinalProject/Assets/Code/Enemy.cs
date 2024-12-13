using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : Lives
{
    [Header("UI Settings")]
    public Image currentHealthImage;   // 实时血量条
    public Image delayHealthImage;     // 延迟血量条
    public float delayTime = 1f;       // 延迟血量条的平滑时间
    public Transform healthBarCanvas; // 血条的 UI 画布对象

    [Header("Enemy Settings")]
    public ParticleSystem deathEffect; // 死亡特效
    private NavMeshAgent pathfinder; // 导航代理
    private Transform target; // 玩家目标
    private Lives targetEntity; // 玩家生命系统
    private Material skinMaterial; // 敌人材质，用于变色效果
    private Color originalColour; // 敌人初始颜色

    private float attackDistanceThreshold = 0.5f; // 攻击的距离阈值
    private float timeBetweenAttacks = 1f; // 攻击间隔
    private float damage = 1f; // 每次攻击的伤害

    private float nextAttackTime; // 下一次攻击时间
    private float myCollisionRadius; // 敌人碰撞体半径
    private float targetCollisionRadius; // 玩家碰撞体半径

    private bool hasTarget; // 是否存在目标
    private bool isPaused = false; // 是否暂停行为

    // 用于记录延迟血量
    private float delayHealth;

    // 用于协程控制
    private Coroutine updateHealth;

    public enum State { Idle, Chasing, Attacking }; // 敌人的状态
    State currentState;

    protected override void Start()
    {
        base.Start();

        pathfinder = GetComponent<NavMeshAgent>();
        skinMaterial = GetComponent<Renderer>().material;
        originalColour = skinMaterial.color;

        delayHealth = startingHealth; // 初始化延迟血量

        UpdateTarget(); // 初始化时查找目标
        UpdateHealthUI(); // 初始化时更新血量显示

        if (target != null)
        {
            currentState = State.Chasing; // 初始状态为追逐
            hasTarget = true;

            StartCoroutine(UpdatePath()); // 启动导航路径更新协程
        }
        else
        {
            currentState = State.Idle; // 没有目标时进入闲置状态
        }
    }

    private void UpdateTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
            targetEntity = player.GetComponent<Lives>();

            if (targetEntity != null)
            {
                targetEntity.OnDeath += OnTargetDeath; // 注册目标死亡事件
            }

            myCollisionRadius = GetComponent<CapsuleCollider>().radius;
            targetCollisionRadius = target.GetComponent<CapsuleCollider>().radius;
        }
        else
        {
            target = null;
            targetEntity = null;
            hasTarget = false;
        }
    }

    public override void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        Debug.Log($"{gameObject.name} took {damage} damage.");
        if (damage >= currentHealth && deathEffect != null)
        {
            // 播放死亡特效
            var effect = Instantiate(deathEffect.gameObject, hitPoint, Quaternion.FromToRotation(Vector3.forward, hitDirection));
            Destroy(effect, deathEffect.main.startLifetime.constant);
        }

        base.TakeHit(damage, hitPoint, hitDirection); // 调用基类的受击逻辑
        SetHealth(); // 更新血量 UI 和延迟条逻辑
    }

    private void UpdateHealthUI()
    {
        if (currentHealthImage != null)
        {
            // 更新实时血量条的填充比例
            currentHealthImage.fillAmount = currentHealth / startingHealth;
        }
    }

    private void SetHealth()
    {
        if (updateHealth != null)
        {
            StopCoroutine(updateHealth); // 停止上一次协程
        }
        updateHealth = StartCoroutine(UpdateEffectImage()); // 启动新的协程
    }

    private IEnumerator UpdateEffectImage()
    {
        // 更新实时血量条
        currentHealthImage.fillAmount = currentHealth / startingHealth;

        // 计算延迟条的差值
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

    private void OnTargetDeath()
    {
        hasTarget = false;
        currentState = State.Idle; // 目标死亡后进入闲置状态
        target = null; // 清除对目标的引用
    }

    IEnumerator UpdatePath()
    {
        float refreshRate = 0.25f;

        while (hasTarget && target != null) // 检查目标是否存在
        {
            if (currentState == State.Chasing && !dead)
            {
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                Vector3 targetPosition = target.position - dirToTarget * (myCollisionRadius + targetCollisionRadius + attackDistanceThreshold / 2);

                if (pathfinder != null)
                {
                    pathfinder.SetDestination(targetPosition); // 设置导航目标位置
                }
            }
            yield return new WaitForSeconds(refreshRate);
        }
    }

    void Update()
    {
        if (isPaused)
        {
            return; // 暂停时不执行逻辑
        }

        if (hasTarget && target == null)
        {
            UpdateTarget(); // 如果目标丢失，重新查找目标
        }

        if (hasTarget && target != null && Time.time > nextAttackTime) // 确保目标存在
        {
            float sqrDstToTarget = (target.position - transform.position).sqrMagnitude;
            float attackRadius = Mathf.Pow(attackDistanceThreshold + myCollisionRadius + targetCollisionRadius, 2);

            if (sqrDstToTarget < attackRadius)
            {
                nextAttackTime = Time.time + timeBetweenAttacks;
                StartCoroutine(Attack()); // 进入攻击行为
            }
        }

        // 更新血条的朝向
        UpdateHealthBarRotation();
    }

    private void UpdateHealthBarRotation()
    {
        if (healthBarCanvas != null)
        {
            healthBarCanvas.rotation = Quaternion.LookRotation(healthBarCanvas.position - Camera.main.transform.position);
        }
    }

    public void Pause(bool pause)
    {
        isPaused = pause;
    }

    IEnumerator Attack()
    {
        currentState = State.Attacking;
        pathfinder.enabled = false; // 暂停导航

        Vector3 originalPosition = transform.position;
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        Vector3 attackPosition = target.position - dirToTarget * myCollisionRadius;

        float attackSpeed = 3f;
        float percent = 0f;

        skinMaterial.color = Color.red; // 改变颜色为红色
        bool hasAppliedDamage = false;

        while (percent <= 1f)
        {
            if (percent >= 0.5f && !hasAppliedDamage)
            {
                hasAppliedDamage = true;
                if (targetEntity != null)
                {
                    targetEntity.TakeDamage(damage); // 对目标造成伤害
                }
            }

            percent += Time.deltaTime * attackSpeed;
            float interpolation = (-Mathf.Pow(percent, 2) + percent) * 4; // 插值曲线
            transform.position = Vector3.Lerp(originalPosition, attackPosition, interpolation);

            yield return null;
        }

        skinMaterial.color = originalColour; // 恢复原始颜色
        currentState = State.Chasing; // 恢复追逐状态
        pathfinder.enabled = true; // 重新启用导航
    }
}