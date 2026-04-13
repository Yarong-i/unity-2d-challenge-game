using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hitbox2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers; // 보통 Enemy 레이어
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 6f;
    [SerializeField] private Transform owner; // 공격 주체(플레이어). 비워두면 부모 Transform 사용

    private readonly HashSet<IDamageable> hitThisSwing = new();

    private void Awake()
    {
        if (owner == null) owner = transform.root;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void BeginSwing()
    {
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 레이어 필터
        int otherLayerMask = 1 << other.gameObject.layer;
        if ((targetLayers.value & otherLayerMask) == 0) return;

        // IDamageable 찾기(자식 콜라이더를 쓸 수도 있어서 Parent까지 검색)
        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg == null) return;

        // 한 번의 공격에서 같은 대상 중복 타격 방지
        if (hitThisSwing.Contains(dmg)) return;
        hitThisSwing.Add(dmg);

        // 넉백 방향: 공격 주체 -> 대상
        Vector2 dir = (other.bounds.center - owner.position);
        if (dir.sqrMagnitude < 0.0001f)
        {
            // 혹시 완전히 겹치면 플레이어 바라보는 방향 기준(스케일 이용)
            float sign = owner.localScale.x >= 0 ? 1f : -1f;
            dir = Vector2.right * sign;
        }

        dmg.TakeDamage(damage, dir, knockback);
    }
}