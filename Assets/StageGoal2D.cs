using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StageGoal2D : MonoBehaviour
{
    [SerializeField] private StageClearController stageClearController;

    private bool clearSent;

    private void Awake()
    {
        EnsureTriggerCollider();

        if (stageClearController == null)
            stageClearController = FindFirstObjectByType<StageClearController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (clearSent || !IsPlayer(other))
            return;

        clearSent = true;

        if (stageClearController == null)
            stageClearController = FindFirstObjectByType<StageClearController>();

        if (stageClearController != null)
            stageClearController.StageClear();
    }

    public void ResetGoal()
    {
        clearSent = false;
    }

    private void EnsureTriggerCollider()
    {
        Collider2D goalCollider = GetComponent<Collider2D>();
        if (goalCollider != null)
            goalCollider.isTrigger = true;
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        Transform otherTransform = other.transform;
        if (otherTransform != null && otherTransform.name == "player")
            return true;

        if (other.GetComponentInParent<ChallengePlayerController>() != null)
            return true;

        return other.attachedRigidbody != null;
    }
}
