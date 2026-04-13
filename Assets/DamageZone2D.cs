using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageZone2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers; // 보통 Player만 체크
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 6f;

    [Header("How to apply")]
    [SerializeField] private bool damageOnStay = true; // 닿아있는 동안 무적 끝나면 다시 데미지 가능

    [Header("Knockback source")]
    [SerializeField] private Transform source; // 넉백 방향 기준점(비우면 자기)

    private void Awake()
    {
        if (source == null) source = transform;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!damageOnStay) return;
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        int mask = 1 << other.gameObject.layer;
        if ((targetLayers.value & mask) == 0) return;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        Vector2 dir = (other.bounds.center - (Vector3)source.position);
        dmg.TakeDamage(damage, dir, knockback);
    }
}