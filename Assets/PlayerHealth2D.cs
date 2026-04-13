using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class PlayerHealth2D : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 5;

    [Header("Invincibility (i-frames)")]
    [SerializeField] private float invincibleTime = 0.6f;

    [Header("Hurt (input lock)")]
    [SerializeField] private float hurtTime = 0.15f; // 맞고 잠깐 조작 잠금

    [Header("Knockback")]
    [SerializeField] private float knockbackMultiplier = 1f;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    [SerializeField] private float knockUpFactor = 0.6f; // 위로 튀는 비율(0.4~0.8 사이 추천)

    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float blinkInterval = 0.08f; // 깜빡임 간격(초)

    [SerializeField] private PlayerAnimator2D playerAnimator;

    private float blinkTimer;

    public event Action<int, int> OnHpChanged; // (current, max)
    public int CurrentHP => hp;
    public int MaxHP => maxHP;

    public bool IsHurt => hurtTimer > 0f;
    public bool IsInvincible => invTimer > 0f;

    private int hp;
    private float invTimer;
    private float hurtTimer;
    private Rigidbody2D rb;

    private void Awake()
    {
        hp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null)
            sprite = GetComponentInChildren<SpriteRenderer>(true);

        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator2D>();
    }

    private void Update()
    {
        if (invTimer > 0f) invTimer -= Time.deltaTime;
        if (hurtTimer > 0f) hurtTimer -= Time.deltaTime;
        if (sprite != null)
        {
            if (invTimer > 0f)
            {
                blinkTimer -= Time.deltaTime;
                if (blinkTimer <= 0f)
                {
                    sprite.enabled = !sprite.enabled;
                    blinkTimer = blinkInterval;
                }
            }
            else
            {
                // 무적 끝나면 항상 켜진 상태로 복구
                if (!sprite.enabled) sprite.enabled = true;
                blinkTimer = 0f;
            }
        }
    }

    public void TakeDamage(int amount, Vector2 hitDir, float knockback)
    {
        // 무적이면 무시
        if (invTimer > 0f) return;

        invTimer = invincibleTime;
        blinkTimer = 0f;
        hurtTimer = hurtTime;

        hp -= amount;
        OnHpChanged?.Invoke(hp, maxHP);
        if (debugLog)
            Debug.Log($"Player took {amount} dmg. HP: {hp}/{maxHP}");
        if (playerAnimator != null)
            playerAnimator.PlayHurt();

        // 넉백(일정하게 만들기: 가로는 좌/우, 세로는 항상 위로)
        if (rb != null && knockback > 0f)
        {
            float signX = Mathf.Sign(hitDir.x);
            if (Mathf.Abs(signX) < 0.01f) signX = 1f; // 거의 0이면 기본 오른쪽

            float k = knockback * knockbackMultiplier;
            float kx = k * signX;
            float ky = k * knockUpFactor;

#if UNITY_6000_0_OR_NEWER
    // 이미 상승 중이면(점프 중) 위로 더 뻥튀기 되는 걸 방지
    if (rb.linearVelocity.y > 0f) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

    // "추가"가 아니라 "덮어쓰기"로 일정하게
    rb.linearVelocity = new Vector2(kx, ky);
#else
            if (rb.velocity.y > 0f) rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.velocity = new Vector2(kx, ky);
#endif
        }

        // 사망 처리(간단 리스폰)
        if (hp <= 0)
        {
            Respawn();
        }
    }

    private void Respawn()
    {
        hp = maxHP;
        OnHpChanged?.Invoke(hp, maxHP);
        invTimer = invincibleTime;
        hurtTimer = 0f;

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
        }
        if (playerAnimator != null)
            playerAnimator.PlayDeath();

        if (respawnPoint != null)
            transform.position = respawnPoint.position;

        if (debugLog)
            Debug.Log("Player respawned.");
        if (playerAnimator != null)
            playerAnimator.ResetState();
    }
}