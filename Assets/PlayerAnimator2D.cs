using UnityEngine;

public class PlayerAnimator2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    [Header("Runtime State")]
    [SerializeField] private bool grounded;
    [SerializeField] private bool dashing;
    [SerializeField] private bool dead;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (animator == null || rb == null)
            return;

        animator.SetFloat("MoveSpeed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetFloat("YVelocity", rb.linearVelocity.y);
        animator.SetBool("Grounded", grounded);
        animator.SetBool("Dashing", dashing);
        animator.SetBool("Dead", dead);
    }

    public void SetGrounded(bool value)
    {
        grounded = value;
    }

    public void SetDashing(bool value)
    {
        dashing = value;
    }

    public void PlayAttack()
    {
        if (dead || animator == null)
            return;

        animator.ResetTrigger("Hurt");
        animator.SetTrigger("Attack");
    }

    public void PlayHurt()
    {
        if (dead || animator == null)
            return;

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Hurt");
    }

    public void PlayDeath()
    {
        dead = true;
    }

    public void ResetState()
    {
        dead = false;

        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
    }
}
