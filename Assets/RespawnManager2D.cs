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

    private Vector3 runStartPosition;
    private float graceTimer;
    private bool hasRunStartPosition;

    public Vector3 RunStartPosition => runStartPosition;
    public bool IsInRespawnGrace => graceTimer > 0f;

    private void Awake()
    {
        ResolveReferences();
        CaptureRunStartPosition();
    }

    private void Update()
    {
        if (player == null)
            ResolveReferences();

        if (graceTimer > 0f)
            graceTimer -= Time.deltaTime;

        if (player == null)
            return;

        if (!CanProcessRespawnChecks())
            return;

        if (Input.GetKeyDown(manualRespawnKey))
        {
            Respawn();
            return;
        }

        if (graceTimer <= 0f && player.position.y < fallYThreshold)
            Respawn();
    }

    [System.Obsolete("Checkpoint respawns are no longer used. RespawnManager2D now respawns to the run start position.")]
    public void SetCheckpoint(Vector3 respawnPosition)
    {
        Debug.LogWarning("SetCheckpoint is ignored because this game mode respawns from the run start position.", this);
    }

    public void Respawn()
    {
        if (player == null)
            ResolveReferences();

        if (player == null)
            return;

        if (!hasRunStartPosition)
            CaptureRunStartPosition();

        if (resetVelocityOnRespawn && playerRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            playerRb.linearVelocity = Vector2.zero;
#else
            playerRb.velocity = Vector2.zero;
#endif
            playerRb.angularVelocity = 0f;
        }

        player.position = runStartPosition;

        if (playerRb != null)
            playerRb.position = runStartPosition;

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
    }

    private void CaptureRunStartPosition()
    {
        if (player == null)
            return;

        runStartPosition = player.position;
        hasRunStartPosition = true;
    }

    private bool CanProcessRespawnChecks()
    {
        return Time.timeScale > 0f;
    }
}
