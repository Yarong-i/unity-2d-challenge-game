using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Checkpoint2D : MonoBehaviour
{
    [SerializeField] private bool activateOnce = false;
    [SerializeField] private Vector3 respawnOffset = Vector3.zero;
    [SerializeField] private RespawnManager2D respawnManager;

    private bool activated;

    private void Awake()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
            Debug.LogWarning($"{name} has Checkpoint2D but its Collider2D is not a trigger.", this);

        ResolveRespawnManager();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activateOnce && activated)
            return;

        if (respawnManager == null)
            ResolveRespawnManager();

        if (respawnManager == null || !respawnManager.IsPlayer(other))
            return;

        activated = true;
        Vector3 respawnPosition = transform.position + respawnOffset;
        respawnManager.SetCheckpoint(respawnPosition);
        Debug.Log($"Checkpoint activated: {name} -> {respawnPosition}", this);
    }

    private void ResolveRespawnManager()
    {
        if (respawnManager == null)
            respawnManager = FindFirstObjectByType<RespawnManager2D>();
    }
}
