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

    private const float GroundedStabilityGraceTime = 0.08f;
    private const float OriginalMaxChargeTime = 0.45f;
    private const float OriginalMinLaunchForce = 4.5f;
    private const float OriginalMaxLaunchForce = 13f;
    private const float OriginalMaxDragDistance = 3.25f;
    private const float OriginalAirControlMultiplier = 0.08f;

    private const float PreviousStableHorizontalSpeedThreshold = 0.90f;
    private const float PreviousStableVerticalSpeedThreshold = 0.35f;
    private const float PreviousStableSettleTime = 0.14f;
    private const float PreviousGroundDeceleration = 55f;
    private const float PreviousStopSnapSpeed = 0.22f;
    private const float PreviousMaxChargeTime = 1f;
    private const float PreviousMinLaunchForce = 0.9f;
    private const float PreviousMaxLaunchForce = 4.8f;
    private const float PreviousMaxDragDistance = 1.1f;
    private const float PreviousDragDeadzone = 0.08f;
    private const float PreviousAirControlMultiplier = 0.03f;
    private const float PreviousGroundedCheckDistance = 0.12f;
    private const float TransitionMaxChargeTime = 1.55f;
    private const float TransitionMinLaunchForce = 0.9f;
    private const float TransitionMaxLaunchForce = 6.4f;
    private const float TransitionMaxDragDistance = 1.05f;
    private const float TransitionDragDeadzone = 0.10f;

    [Header("Stable Detection")]
    [SerializeField] private float stableHorizontalSpeedThreshold = 1.10f;
    [SerializeField] private float stableVerticalSpeedThreshold = 0.45f;
    [SerializeField] private float stableSettleTime = 0.22f;
    [SerializeField] private float groundedCheckDistance = 0.14f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footInset = 0.05f;

    [Header("Ground Movement")]
    [SerializeField] private float groundDeceleration = 70f;
    [SerializeField] private float stopSnapSpeed = 0.32f;

    [Header("Launch Charge")]
    [SerializeField] private float maxChargeTime = 2.10f;
    [SerializeField] private float minLaunchForce = 1.2f;
    [SerializeField] private float maxLaunchForce = 9.2f;
    [SerializeField] private float maxDragDistance = 1.2f;
    [SerializeField] private float dragDeadzone = 0.09f;

    [Header("Air Control")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlMultiplier = 0.02f;

    [Header("Charge Preview")]
    [SerializeField] private LineRenderer chargePreviewLine;
    [SerializeField] private float previewStartWidth = 0.16f;
    [SerializeField] private float previewEndWidth = 0.08f;
    [SerializeField] private Color previewLowChargeColor = new Color(0.35f, 0.95f, 1f, 0.8f);
    [SerializeField] private Color previewHighChargeColor = new Color(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private Color previewDeadzoneColor = new Color(1f, 1f, 1f, 0.35f);

    [Header("References")]
    [SerializeField] private PlayerAnimator2D playerAnimator;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerHealth2D health;
    private SpriteRenderer spriteRenderer;
    private Camera cachedCamera;
    private Material runtimePreviewMaterial;

    private MovementState state = MovementState.Airborne;
    private float horizontalInput;
    private float chargeTimer;
    private float stableSettleTimer;
    private float groundedStabilityGraceTimer;
    private bool isGrounded;
    private bool launchQueued;
    private Vector2 dragStartWorld;
    private Vector2 currentMouseWorld;
    private Vector2 queuedLaunchDirection = Vector2.left;
    private float queuedLaunchForce;

    public bool IsStable => state == MovementState.Stable;
    public bool IsCharging => state == MovementState.Charging;
    public bool IsAirborne => state == MovementState.Airborne;

    private void Reset()
    {
        CacheReferences();
        UpgradeLegacyValuesIfNeeded();

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        if (TryGetComponent(out LineRenderer existingPreviewLine))
            chargePreviewLine = existingPreviewLine;

        ApplyPreviewLineSettings();
    }

    private void OnValidate()
    {
        UpgradeLegacyValuesIfNeeded();

        stableHorizontalSpeedThreshold = Mathf.Max(0f, stableHorizontalSpeedThreshold);
        stableVerticalSpeedThreshold = Mathf.Max(0f, stableVerticalSpeedThreshold);
        stableSettleTime = Mathf.Max(0.01f, stableSettleTime);
        groundedCheckDistance = Mathf.Max(0.01f, groundedCheckDistance);
        footInset = Mathf.Max(0f, footInset);

        groundDeceleration = Mathf.Max(0f, groundDeceleration);
        stopSnapSpeed = Mathf.Max(0f, stopSnapSpeed);

        maxChargeTime = Mathf.Max(0.05f, maxChargeTime);
        minLaunchForce = Mathf.Max(0f, minLaunchForce);
        maxLaunchForce = Mathf.Max(minLaunchForce, maxLaunchForce);
        maxDragDistance = Mathf.Max(0.1f, maxDragDistance);
        dragDeadzone = Mathf.Clamp(dragDeadzone, 0f, maxDragDistance);
        airControlMultiplier = Mathf.Clamp01(airControlMultiplier);

        previewStartWidth = Mathf.Max(0.01f, previewStartWidth);
        previewEndWidth = Mathf.Max(0.01f, previewEndWidth);

        ApplyPreviewLineSettings();
    }

    private void Awake()
    {
        CacheReferences();
        UpgradeLegacyValuesIfNeeded();
        EnsurePreviewLine();
        HideChargePreview();
    }

    private void Start()
    {
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        RefreshGrounding();
        groundedStabilityGraceTimer = isGrounded ? GroundedStabilityGraceTime : 0f;
        stableSettleTimer = CanSettleIntoStable() ? stableSettleTime : 0f;
        state = CanEnterStableState() ? MovementState.Stable : MovementState.Airborne;
        UpdateAnimatorState();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (health != null && health.IsHurt)
        {
            CancelCharge();
            UpdateChargePreview();
            return;
        }

        if (state == MovementState.Stable && Input.GetMouseButtonDown(0))
            BeginCharge();

        if (state == MovementState.Charging && !launchQueued)
            UpdateChargingState();

        UpdateChargePreview();
    }

    private void FixedUpdate()
    {
        RefreshGrounding();

        if (health != null && health.IsHurt)
        {
            CancelCharge();
            UpdateGroundedStabilityGrace(Time.fixedDeltaTime);
            state = ComputeMovementState(Time.fixedDeltaTime);
            UpdateAnimatorState();
            return;
        }

        if (launchQueued)
        {
            ExecuteQueuedLaunch();
            UpdateAnimatorState();
            return;
        }

        if (state == MovementState.Charging)
        {
            HoldStableDuringCharge();
        }
        else if (isGrounded)
        {
            ApplyGroundDeceleration();
        }
        else
        {
            ApplyAirControl();
        }

        RefreshGrounding();
        UpdateGroundedStabilityGrace(Time.fixedDeltaTime);
        if (state != MovementState.Charging)
            state = ComputeMovementState(Time.fixedDeltaTime);

        UpdateAnimatorState();
    }

    private void OnDisable()
    {
        HideChargePreview();
    }

    private void OnDestroy()
    {
        if (runtimePreviewMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimePreviewMaterial);
        else
            DestroyImmediate(runtimePreviewMaterial);
    }

    private void BeginCharge()
    {
        state = MovementState.Charging;
        chargeTimer = 0f;
        launchQueued = false;
        queuedLaunchForce = 0f;
        currentMouseWorld = GetMouseWorldPosition();
        dragStartWorld = currentMouseWorld;
    }

    private void UpdateChargingState()
    {
        currentMouseWorld = GetMouseWorldPosition();
        chargeTimer = Mathf.Min(maxChargeTime, chargeTimer + Time.deltaTime);

        bool isHolding = Input.GetMouseButton(0);
        bool reachedChargeLimit = chargeTimer >= maxChargeTime;

        if (!isHolding)
        {
            TryQueueLaunchOrCancel(onRelease: true);
            return;
        }

        if (reachedChargeLimit)
            TryQueueLaunchOrCancel(onRelease: false);
    }

    private void TryQueueLaunchOrCancel(bool onRelease)
    {
        Vector2 dragVector = GetCurrentDragVector();
        float dragMagnitude = dragVector.magnitude;

        if (dragMagnitude < dragDeadzone)
        {
            if (onRelease)
                CancelCharge();

            return;
        }

        QueueLaunch(dragVector);
    }

    private void CancelCharge()
    {
        chargeTimer = 0f;
        launchQueued = false;
        queuedLaunchForce = 0f;
        HideChargePreview();

        RefreshGrounding();
        if (isGrounded)
            groundedStabilityGraceTimer = GroundedStabilityGraceTime;

        if (CanSettleIntoStable())
            stableSettleTimer = stableSettleTime;

        state = CanEnterStableState() ? MovementState.Stable : MovementState.Airborne;
    }

    private void QueueLaunch(Vector2 dragVector)
    {
        CalculateLaunchData(dragVector, out queuedLaunchDirection, out queuedLaunchForce, out _);
        launchQueued = true;
        stableSettleTimer = 0f;
        groundedStabilityGraceTimer = 0f;
        state = MovementState.Airborne;
        HideChargePreview();
    }

    private void ExecuteQueuedLaunch()
    {
        launchQueued = false;
        chargeTimer = 0f;
        stableSettleTimer = 0f;
        groundedStabilityGraceTimer = 0f;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(queuedLaunchDirection * queuedLaunchForce, ForceMode2D.Impulse);

        state = MovementState.Airborne;
        HideChargePreview();
    }

    private void HoldStableDuringCharge()
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void ApplyGroundDeceleration()
    {
        Vector2 velocity = rb.linearVelocity;
        float newHorizontalVelocity = Mathf.MoveTowards(velocity.x, 0f, groundDeceleration * Time.fixedDeltaTime);
        if (Mathf.Abs(newHorizontalVelocity) <= stopSnapSpeed)
            newHorizontalVelocity = 0f;

        float newVerticalVelocity = velocity.y;
        if (Mathf.Abs(newVerticalVelocity) <= stableVerticalSpeedThreshold)
            newVerticalVelocity = 0f;

        rb.linearVelocity = new Vector2(newHorizontalVelocity, newVerticalVelocity);
    }

    private void ApplyAirControl()
    {
        if (Mathf.Abs(horizontalInput) < 0.01f || airControlMultiplier <= 0f)
            return;

        float maxAirSpeed = Mathf.Max(minLaunchForce, maxLaunchForce * airControlMultiplier);
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

    private void UpdateGroundedStabilityGrace(float deltaTime)
    {
        if (isGrounded)
        {
            groundedStabilityGraceTimer = GroundedStabilityGraceTime;
            return;
        }

        groundedStabilityGraceTimer = Mathf.Max(0f, groundedStabilityGraceTimer - deltaTime);
    }

    private MovementState ComputeMovementState(float deltaTime)
    {
        UpdateStableSettleTimer(deltaTime);
        return CanEnterStableState() ? MovementState.Stable : MovementState.Airborne;
    }

    private void UpdateStableSettleTimer(float deltaTime)
    {
        if (CanSettleIntoStable())
        {
            stableSettleTimer = Mathf.Min(stableSettleTime, stableSettleTimer + deltaTime);
            return;
        }

        float drainSpeed = HasGroundContactForStability() ? 0.35f : 2f;
        stableSettleTimer = Mathf.Max(0f, stableSettleTimer - (deltaTime * drainSpeed));
    }

    private bool HasGroundContactForStability()
    {
        return isGrounded || groundedStabilityGraceTimer > 0f;
    }

    private bool CanSettleIntoStable()
    {
        if (!HasGroundContactForStability())
            return false;

        Vector2 velocity = rb.linearVelocity;
        return Mathf.Abs(velocity.x) <= stableHorizontalSpeedThreshold &&
            Mathf.Abs(velocity.y) <= stableVerticalSpeedThreshold;
    }

    private bool CanEnterStableState()
    {
        return HasGroundContactForStability() && stableSettleTimer >= stableSettleTime;
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

    private Vector2 GetCurrentDragVector()
    {
        return currentMouseWorld - dragStartWorld;
    }

    private void CalculateLaunchData(
        Vector2 dragVector,
        out Vector2 launchDirection,
        out float launchForce,
        out float normalizedForce)
    {
        Vector2 clampedDragVector = Vector2.ClampMagnitude(dragVector, maxDragDistance);
        float dragRatio = maxDragDistance > Mathf.Epsilon
            ? Mathf.Clamp01(clampedDragVector.magnitude / maxDragDistance)
            : 1f;

        float curvedDragRatio = Mathf.Pow(dragRatio, 1.35f);
        launchDirection = clampedDragVector.sqrMagnitude > 0.0001f
            ? -clampedDragVector.normalized
            : Vector2.zero;
        launchForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, curvedDragRatio);
        normalizedForce = dragRatio;
    }

    private void UpdateChargePreview()
    {
        if (chargePreviewLine == null)
            return;

        if (state != MovementState.Charging || launchQueued)
        {
            HideChargePreview();
            return;
        }

        Vector2 dragVector = GetCurrentDragVector();
        Vector2 clampedDragVector = Vector2.ClampMagnitude(dragVector, maxDragDistance);
        float dragMagnitude = clampedDragVector.magnitude;
        float normalizedDrag = maxDragDistance > Mathf.Epsilon
            ? Mathf.Clamp01(dragMagnitude / maxDragDistance)
            : 0f;

        chargePreviewLine.enabled = true;
        chargePreviewLine.startWidth = previewStartWidth;
        chargePreviewLine.endWidth = Mathf.Lerp(previewEndWidth * 0.85f, previewEndWidth, normalizedDrag);

        if (dragMagnitude < dragDeadzone)
        {
            chargePreviewLine.positionCount = 2;
            chargePreviewLine.startColor = previewDeadzoneColor;
            chargePreviewLine.endColor = previewDeadzoneColor;
            chargePreviewLine.SetPosition(0, dragStartWorld);
            chargePreviewLine.SetPosition(1, dragStartWorld + clampedDragVector);
            return;
        }

        CalculateLaunchData(clampedDragVector, out Vector2 launchDirection, out _, out float normalizedForce);
        float launchPreviewLength = Mathf.Lerp(dragDeadzone, maxDragDistance * 1.55f, normalizedForce);
        Color dragSideColor = Color.Lerp(previewDeadzoneColor, previewLowChargeColor, 0.45f);
        Color launchSideColor = Color.Lerp(previewLowChargeColor, previewHighChargeColor, normalizedForce);

        chargePreviewLine.positionCount = 3;
        chargePreviewLine.startColor = dragSideColor;
        chargePreviewLine.endColor = launchSideColor;
        chargePreviewLine.SetPosition(0, dragStartWorld + clampedDragVector);
        chargePreviewLine.SetPosition(1, dragStartWorld);
        chargePreviewLine.SetPosition(2, dragStartWorld + (launchDirection * launchPreviewLength));
    }

    private void HideChargePreview()
    {
        if (chargePreviewLine != null)
            chargePreviewLine.enabled = false;
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
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator2D>();
    }

    private void EnsurePreviewLine()
    {
        if (chargePreviewLine == null)
            chargePreviewLine = GetComponent<LineRenderer>();

        if (chargePreviewLine == null)
            chargePreviewLine = gameObject.AddComponent<LineRenderer>();

        if (chargePreviewLine.sharedMaterial == null)
        {
            Shader previewShader = Shader.Find("Sprites/Default");
            if (previewShader == null)
                previewShader = Shader.Find("Unlit/Color");

            if (previewShader != null)
            {
                runtimePreviewMaterial = new Material(previewShader);
                chargePreviewLine.sharedMaterial = runtimePreviewMaterial;
            }
        }

        ApplyPreviewLineSettings();
    }

    private void ApplyPreviewLineSettings()
    {
        if (chargePreviewLine == null)
            return;

        chargePreviewLine.useWorldSpace = true;
        chargePreviewLine.loop = false;
        chargePreviewLine.positionCount = 3;
        chargePreviewLine.numCapVertices = 8;
        chargePreviewLine.numCornerVertices = 6;
        chargePreviewLine.alignment = LineAlignment.View;
        chargePreviewLine.textureMode = LineTextureMode.Stretch;
        chargePreviewLine.startWidth = previewStartWidth;
        chargePreviewLine.endWidth = previewEndWidth;
        chargePreviewLine.sortingLayerName = spriteRenderer != null ? spriteRenderer.sortingLayerName : "Default";
        chargePreviewLine.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 2 : 10;
        chargePreviewLine.enabled = false;
    }

    private void UpgradeLegacyValuesIfNeeded()
    {
        if (!NeedsLatestTuningUpgrade())
            return;

        stableHorizontalSpeedThreshold = 1.10f;
        stableVerticalSpeedThreshold = 0.45f;
        stableSettleTime = 0.22f;
        groundDeceleration = 70f;
        stopSnapSpeed = 0.32f;
        maxChargeTime = 2.10f;
        minLaunchForce = 1.2f;
        maxLaunchForce = 9.2f;
        maxDragDistance = 1.2f;
        dragDeadzone = 0.09f;
        airControlMultiplier = 0.02f;
        groundedCheckDistance = 0.14f;
    }

    private bool NeedsLatestTuningUpgrade()
    {
        return HasOriginalLaunchTuning() ||
            HasPreviousRequestedTuning() ||
            HasTransitionForceTuning() ||
            HasMixedTransitionTuning();
    }

    private bool HasOriginalLaunchTuning()
    {
        return Approximately(maxChargeTime, OriginalMaxChargeTime) &&
            Approximately(minLaunchForce, OriginalMinLaunchForce) &&
            Approximately(maxLaunchForce, OriginalMaxLaunchForce) &&
            Approximately(maxDragDistance, OriginalMaxDragDistance) &&
            Approximately(airControlMultiplier, OriginalAirControlMultiplier);
    }

    private bool HasPreviousRequestedTuning()
    {
        return Approximately(stableHorizontalSpeedThreshold, PreviousStableHorizontalSpeedThreshold) &&
            Approximately(stableVerticalSpeedThreshold, PreviousStableVerticalSpeedThreshold) &&
            Approximately(stableSettleTime, PreviousStableSettleTime) &&
            Approximately(groundDeceleration, PreviousGroundDeceleration) &&
            Approximately(stopSnapSpeed, PreviousStopSnapSpeed) &&
            Approximately(maxChargeTime, PreviousMaxChargeTime) &&
            Approximately(minLaunchForce, PreviousMinLaunchForce) &&
            Approximately(maxLaunchForce, PreviousMaxLaunchForce) &&
            Approximately(maxDragDistance, PreviousMaxDragDistance) &&
            Approximately(dragDeadzone, PreviousDragDeadzone) &&
            Approximately(airControlMultiplier, PreviousAirControlMultiplier) &&
            Approximately(groundedCheckDistance, PreviousGroundedCheckDistance);
    }

    private bool HasMixedTransitionTuning()
    {
        return Approximately(stableHorizontalSpeedThreshold, PreviousStableHorizontalSpeedThreshold) &&
            Approximately(stableVerticalSpeedThreshold, PreviousStableVerticalSpeedThreshold) &&
            Approximately(stableSettleTime, PreviousStableSettleTime) &&
            Approximately(groundDeceleration, PreviousGroundDeceleration) &&
            Approximately(stopSnapSpeed, PreviousStopSnapSpeed) &&
            Approximately(groundedCheckDistance, PreviousGroundedCheckDistance) &&
            HasOriginalLaunchTuning();
    }

    private bool HasTransitionForceTuning()
    {
        return Approximately(stableHorizontalSpeedThreshold, 1.10f) &&
            Approximately(stableVerticalSpeedThreshold, 0.45f) &&
            Approximately(stableSettleTime, 0.22f) &&
            Approximately(groundDeceleration, 70f) &&
            Approximately(stopSnapSpeed, 0.32f) &&
            Approximately(maxChargeTime, TransitionMaxChargeTime) &&
            Approximately(minLaunchForce, TransitionMinLaunchForce) &&
            Approximately(maxLaunchForce, TransitionMaxLaunchForce) &&
            Approximately(maxDragDistance, TransitionMaxDragDistance) &&
            Approximately(dragDeadzone, TransitionDragDeadzone) &&
            Approximately(airControlMultiplier, 0.02f) &&
            Approximately(groundedCheckDistance, 0.14f);
    }

    private static bool Approximately(float a, float b)
    {
        return Mathf.Abs(a - b) <= 0.0001f;
    }

    private void UpdateAnimatorState()
    {
        if (playerAnimator == null)
            return;

        playerAnimator.SetGrounded(isGrounded);
        playerAnimator.SetDashing(false);
    }
}
