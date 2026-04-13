using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Hitbox2D hitbox;
    [SerializeField] private Collider2D hitboxCollider;

    [Header("Attack Timing")]
    [SerializeField] private float activeTime = 0.10f;   // 히트박스 켜지는 시간
    [SerializeField] private float cooldownTime = 0.20f; // 다음 공격까지 쿨타임

    [SerializeField] private PlayerAnimator2D playerAnimator;

    private bool attackPressed;     // Update에서 입력 저장
    private float activeTimer;      // FixedUpdate에서 감소
    private float cooldownTimer;
    private PlayerHealth2D health;

    private void Awake()
    {
        health = GetComponent<PlayerHealth2D>();
        if (hitbox == null)
            hitbox = GetComponentInChildren<Hitbox2D>(true);

        if (hitboxCollider == null && hitbox != null)
            hitboxCollider = hitbox.GetComponent<Collider2D>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator2D>();
    }

    private void Start()
    {
        if (hitboxCollider != null)
            hitboxCollider.enabled = false;
    }

    private void Update()
    {
        if (health != null && health.IsHurt) return;
        // 공격 키: 일단 J로 고정(원하면 Fire1로 바꿔도 됨)
        if (Input.GetKeyDown(KeyCode.J))
            attackPressed = true;
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsHurt)
        {
            activeTimer = 0f;
            attackPressed = false;
            if (hitboxCollider != null) hitboxCollider.enabled = false;
            return;
        }
        float dt = Time.fixedDeltaTime;

        if (cooldownTimer > 0f) cooldownTimer -= dt;

        // 공격 중이면 타이머 감소, 끝나면 히트박스 끄기
        if (activeTimer > 0f)
        {
            activeTimer -= dt;
            if (activeTimer <= 0f && hitboxCollider != null)
                hitboxCollider.enabled = false;
        }

        // 새 공격 시작 조건: 입력 있음 + 쿨타임 끝 + 현재 공격 중 아님
        if (attackPressed && cooldownTimer <= 0f && activeTimer <= 0f)
        {
            StartAttack();
        }

        attackPressed = false; // 1회성 입력 초기화
    }

    private void StartAttack()
    {
        if (playerAnimator != null)
            playerAnimator.PlayAttack();
        cooldownTimer = cooldownTime;
        activeTimer = activeTime;

        if (hitbox != null) hitbox.BeginSwing();
        if (hitboxCollider != null) hitboxCollider.enabled = true;
    }
}
