using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class ChallengePlayerController : MonoBehaviour
{
    private enum MovementState
    {
        Stable,
        Charging,
        Airborne
    }

    [Header("Stable Detection")]
    [SerializeField] private float stableSpeedThreshold = 0.35f;
    [SerializeField] private float groundedCheckDistance = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footInset = 0.05f;

    [Header("Launch Charge")]
    [SerializeField] private float maxChargeTime = 0.45f;
    [SerializeField] private float minLaunchForce = 4.5f;
    [SerializeField] private float maxLaunchForce = 13f;
    [SerializeField] private float maxDragDistance = 3.25f;

    [Header("Air Control")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlMultiplier = 0.08f;

    [Header("References")]
    [SerializeField] private PlayerAnimator2D playerAnimator;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerHealth2D health;
    private Camera cachedCamera;

    private MovementState state = MovementState.Airborne;
    private float horizontalInput;
    private float chargeTimer;
    private bool isGrounded;
    private bool launchQueued;
    private Vector2 queuedLaunchDirection = Vector2.up;
    private float queuedLaunchForce;
    private Vector2 cachedAimDirection = Vector2.up;

    public bool IsStable => state == MovementState.Stable;
    public bool IsCharging => state == MovementState.Charging;
    public bool IsAirborne => state == MovementState.Airborne;

    private void Reset()
    {
        CacheReferences();

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");
    }

    private void OnValidate()
    {
        stableSpeedThreshold = Mathf.Max(0f, stableSpeedThreshold);
        groundedCheckDistance = Mathf.Max(0.01f, groundedCheckDistance);
        footInset = Mathf.Max(0f, footInset);

        maxChargeTime = Mathf.Max(0.05f, maxChargeTime);
        minLaunchForce = Mathf.Max(0f, minLaunchForce);
        maxLaunchForce = Mathf.Max(minLaunchForce, maxLaunchForce);
        maxDragDistance = Mathf.Max(0.1f, maxDragDistance);
        airControlMultiplier = Mathf.Clamp01(airControlMultiplier);
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void Start()
    {
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        RefreshGrounding();
        state = ComputeMovementState();
        UpdateAnimatorState();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (health != null && health.IsHurt)
        {
            CancelCharge();
            return;
        }

        if (state == MovementState.Stable && Input.GetMouseButtonDown(0))
            BeginCharge();

        if (state != MovementState.Charging || launchQueued)
            return;

        chargeTimer += Time.deltaTime;

        Vector2 pullVector = GetPullVector();
        if (pullVector.sqrMagnitude > 0.0001f)
            cachedAimDirection = pullVector.normalized;

        if (!Input.GetMouseButton(0) || chargeTimer >= maxChargeTime)
            QueueLaunch(pullVector);
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsHurt)
        {
            CancelCharge();
            RefreshGrounding();
            state = ComputeMovementState();
            UpdateAnimatorState();
            return;
        }

        if (launchQueued)
        {
            ExecuteQueuedLaunch();
        }
        else if (state == MovementState.Charging)
        {
            HoldStableDuringCharge();
        }
        else if (state == MovementState.Airborne)
        {
            ApplyAirControl();
        }

        RefreshGrounding();
        if (state != MovementState.Charging)
            state = ComputeMovementState();

        UpdateAnimatorState();
    }

    private void BeginCharge()
    {
        state = MovementState.Charging;
        chargeTimer = 0f;
        launchQueued = false;

        Vector2 pullVector = GetPullVector();
        if (pullVector.sqrMagnitude > 0.0001f)
            cachedAimDirection = pullVector.normalized;
    }

    private void CancelCharge()
    {
        if (state != MovementState.Charging && !launchQueued)
            return;

        chargeTimer = 0f;
        launchQueued = false;
        queuedLaunchForce = 0f;

        RefreshGrounding();
        state = ComputeMovementState();
    }

    private void QueueLaunch(Vector2 pullVector)
    {
        Vector2 clampedPullVector = Vector2.ClampMagnitude(pullVector, maxDragDistance);
        float dragRatio = maxDragDistance > Mathf.Epsilon
            ? Mathf.Clamp01(clampedPullVector.magnitude / maxDragDistance)
            : 1f;

        queuedLaunchDirection = clampedPullVector.sqrMagnitude > 0.0001f
            ? clampedPullVector.normalized
            : cachedAimDirection;
        queuedLaunchForce = Mathf.Clamp(
            Mathf.Lerp(minLaunchForce, maxLaunchForce, dragRatio),
            minLaunchForce,
            maxLaunchForce);
        launchQueued = true;
        state = MovementState.Airborne;
    }

    private void ExecuteQueuedLaunch()
    {
        launchQueued = false;
        chargeTimer = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(queuedLaunchDirection * queuedLaunchForce, ForceMode2D.Impulse);

        state = MovementState.Airborne;
    }

    private void HoldStableDuringCharge()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void ApplyAirControl()
    {
        if (Mathf.Abs(horizontalInput) < 0.01f || airControlMultiplier <= 0f)
            return;

        float maxAirSpeed = maxLaunchForce * airControlMultiplier;
        float newHorizontalVelocity = Mathf.MoveTowards(
            rb.linearVelocity.x,
            horizontalInput * maxAirSpeed,
            maxAirSpeed * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newHorizontalVelocity, rb.linearVelocity.y);
    }

    private void RefreshGrounding()
    {
        isGrounded = CheckGrounded();
    }

    private MovementState ComputeMovementState()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        bool isStableNow = isGrounded && currentSpeed <= stableSpeedThreshold;
        return isStableNow ? MovementState.Stable : MovementState.Airborne;
    }

    private bool CheckGrounded()
    {
        if (col == null)
            return false;

        Bounds bounds = col.bounds;
        Vector2 leftFoot = new Vector2(bounds.min.x + footInset, bounds.min.y);
        Vector2 rightFoot = new Vector2(bounds.max.x - footInset, bounds.min.y);

        RaycastHit2D leftHit = Physics2D.Raycast(leftFoot, Vector2.down, groundedCheckDistance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightFoot, Vector2.down, groundedCheckDistance, groundLayer);

        Debug.DrawRay(leftFoot, Vector2.down * groundedCheckDistance, Color.cyan);
        Debug.DrawRay(rightFoot, Vector2.down * groundedCheckDistance, Color.cyan);

        return leftHit.collider != null || rightHit.collider != null;
    }

    private Vector2 GetPullVector()
    {
        return (Vector2)transform.position - GetMouseWorldPosition();
    }

    private Vector2 GetMouseWorldPosition()
    {
        if (cachedCamera == null)
            cachedCamera = Camera.main;

        if (cachedCamera == null)
            return transform.position;

        Vector3 mouseScreenPosition = Input.mousePosition;
        mouseScreenPosition.z = Mathf.Abs(cachedCamera.transform.position.z - transform.position.z);

        Vector3 worldPosition = cachedCamera.ScreenToWorldPoint(mouseScreenPosition);
        return new Vector2(worldPosition.x, worldPosition.y);
    }

    private void CacheReferences()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        health = GetComponent<PlayerHealth2D>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator2D>();
    }

    private void UpdateAnimatorState()
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetGrounded(isGrounded);
        playerAnimator.SetDashing(false);
    }
}
