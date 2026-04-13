using UnityEngine;

public class GoalZone : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool clearOnce = true;

    private bool cleared;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (cleared && clearOnce)
            return;

        PlayerMoverRB player = other.GetComponent<PlayerMoverRB>();

        if (player == null)
            player = other.GetComponentInParent<PlayerMoverRB>();

        if (player == null)
            return;

        cleared = true;
        Debug.Log("Stage Clear!");
    }
}
