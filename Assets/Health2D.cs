using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Health2D : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float invincibleTime = 0.10f; // 맞고 잠깐 무적(중복 타격 방지)

    [Header("Hit Feedback")]
    [SerializeField] private Color hitColor = new Color(1f, 0.6f, 0.6f, 1f); // 맞았을 때 색
    [SerializeField] private float hitFlashTime = 0.08f;                      // 색/크기 연출 시간
    [SerializeField] private float hitScaleMultiplier = 1.12f;                // 맞았을 때 살짝 커지는 배율

    private int hp;
    private float invTimer;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    private Color defaultColor = Color.white;
    private Vector3 defaultScale;
    private Coroutine hitFxCo;
    private bool isDead;

    private void Awake()
    {
        hp = maxHP;
        rb = GetComponent<Rigidbody2D>();

        // 적 스프라이트 찾기 (본인 또는 자식)
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            defaultColor = sr.color;

        defaultScale = transform.localScale;
    }

    private void Update()
    {
        if (invTimer > 0f)
            invTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount, Vector2 hitDir, float knockback)
    {
        // 이미 죽는 중이면 무시
        if (isDead) return;

        // 무적시간 중이면 무시
        if (invTimer > 0f) return;

        invTimer = invincibleTime;
        hp -= amount;

        // 넉백
        if (rb != null && knockback > 0f)
        {
            rb.AddForce(hitDir.normalized * knockback, ForceMode2D.Impulse);
        }

        // 피격 연출 시작
        PlayHitFeedback();

        // HP가 0 이하면 잠깐 연출 후 제거
        if (hp <= 0)
        {
            StartCoroutine(DieRoutine());
        }
    }

    private void PlayHitFeedback()
    {
        if (hitFxCo != null)
            StopCoroutine(hitFxCo);

        hitFxCo = StartCoroutine(HitFeedbackRoutine());
    }

    private IEnumerator HitFeedbackRoutine()
    {
        // 색 바꾸기
        if (sr != null)
            sr.color = hitColor;

        // 살짝 커지기
        transform.localScale = defaultScale * hitScaleMultiplier;

        yield return new WaitForSeconds(hitFlashTime);

        // 죽는 중이 아니면 원래 상태로 복구
        if (!isDead)
        {
            if (sr != null)
                sr.color = defaultColor;

            transform.localScale = defaultScale;
        }

        hitFxCo = null;
    }

    private IEnumerator DieRoutine()
    {
        if (isDead) yield break;
        isDead = true;

        // 죽는 순간 추가 타격 방지
        invTimer = 999f;

        // 피격 연출이 보이도록 잠깐 기다렸다가 제거
        yield return new WaitForSeconds(hitFlashTime);

        Destroy(gameObject);
    }
}
