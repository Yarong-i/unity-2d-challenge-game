using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockback = 6f;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (damageable != null)
        {
            Vector2 hitDir = (collision.transform.position - transform.position).normalized;
            damageable.TakeDamage(damage, hitDir, knockback);
        }
    }
}