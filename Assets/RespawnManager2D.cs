using UnityEngine;

public class RespawnManager2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D playerRb;

    [Header("Respawn")]
    [SerializeField] private float fallYThreshold = -10f;
    [SerializeField] private KeyCode manualRespawnKey = KeyCode.R;
    [SerializeField] private float respawnGraceTime = 0.15f;
    [SerializeField] private bool resetVelocityOnRespawn = true;

    private Vector3 currentRespawnPosition;
    private float graceTimer;
    private bool hasRespawnPosition;

    public Vector3 CurrentRespawnPosition => currentRespawnPosition;
    public bool IsInRespawnGrace => graceTimer > 0f;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (player == null)
            ResolveReferences();

        if (graceTimer > 0f)
            graceTimer -= Time.deltaTime;

        if (player == null)
            return;

        if (Input.GetKeyDown(manualRespawnKey))
        {
            Respawn();
            return;
        }

        if (graceTimer <= 0f && player.position.y < fallYThreshold)
            Respawn();
    }

    public void SetCheckpoint(Vector3 respawnPosition)
    {
        currentRespawnPosition = respawnPosition;
        hasRespawnPosition = true;
    }

    public void Respawn()
    {
        if (player == null)
            ResolveReferences();

        if (player == null)
            return;

        if (!hasRespawnPosition)
            SetCheckpoint(player.position);

        if (resetVelocityOnRespawn && playerRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            playerRb.linearVelocity = Vector2.zero;
#else
            playerRb.velocity = Vector2.zero;
#endif
            playerRb.angularVelocity = 0f;
        }

        player.position = currentRespawnPosition;

        if (playerRb != null)
            playerRb.position = currentRespawnPosition;

        graceTimer = Mathf.Max(0f, respawnGraceTime);
    }

    public bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;

        if (player == null)
            ResolveReferences();

        return player != null && other.transform == player;
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.Find("player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerRb == null && player != null)
            playerRb = player.GetComponent<Rigidbody2D>();

        if (!hasRespawnPosition && player != null)
            SetCheckpoint(player.position);
    }
}
