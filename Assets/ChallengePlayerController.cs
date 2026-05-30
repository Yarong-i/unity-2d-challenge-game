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
    [SerializeField] private float stableContactSettleTime = 0.12f;
    [SerializeField] private float stableContactGraceTime = 0.12f;
    [SerializeField] private float edgeStableHorizontalSpeedThreshold = 1.20f;
    [SerializeField] private float edgeStableVerticalSpeedThreshold = 0.55f;
    [SerializeField] private float minStableContactNormalY = 0.25f;
    [SerializeField] private bool allowEdgeContactStable = true;
    [SerializeField] private bool showStableDebug = true;
    [SerializeField] private float groundedCheckDistance = 0.14f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float footInset = 0.05f;

    [Header("Wall Collision")]
    [SerializeField] private bool enableWallBounce = true;
    [SerializeField] private bool enableGenericWallBounce = true;
    [SerializeField] private float minWallBounceSpeed = 0.35f;
    [SerializeField] private float wallBounceMultiplier = 0.74f;
    [SerializeField] private float wallBounceUpwardBoost = 0.48f;
    [SerializeField] private float maxWallBounceSpeed = 6.1f;
    [SerializeField] private float minWallNormalAbsX = 0.45f;
    [SerializeField] private float maxWallNormalY = 0.55f;
    [SerializeField] private float wallBounceCooldown = 0.10f;
    [SerializeField] private float wallDetachSpeed = 1.55f;
    [SerializeField] private float wallStableLockoutTime = 0.16f;
    [SerializeField] private float genericWallMinBounceSpeed = 0.25f;
    [SerializeField] private float genericWallBounceMultiplier = 0.45f;
    [SerializeField] private float genericWallUpwardBoost = 0.26f;
    [SerializeField] private float genericWallDetachSpeed = 0.95f;
    [SerializeField] private float genericWallMaxBounceSpeed = 3.8f;
    [SerializeField] private float genericWallStableLockoutTime = 0.06f;
    [SerializeField] private bool useRuntimeNoFrictionMaterial = true;
    [SerializeField] private bool showWallBounceDebug = false;

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
    private PhysicsMaterial2D runtimeNoFrictionMaterial;

    private MovementState state = MovementState.Airborne;
    private float horizontalInput;
    private float chargeTimer;
    private float stableSettleTimer;
    private float groundedStabilityGraceTimer;
    private float stableContactSettleTimer;
    private float stableContactGraceTimer;
    private float wallBounceCooldownTimer;
    private float wallStableLockoutTimer;
    private bool isGrounded;
    private bool hasStableGroundContact;
    private bool hasEdgeStableGroundContact;
    private bool hasWallContact;
    private bool launchQueued;
    private bool wallBounceAppliedThisStep;
    private Vector2 dragStartWorld;
    private Vector2 currentMouseWorld;
    private Vector2 preCollisionVelocity;
    private Vector2 wallContactNormal;
    private Vector2 queuedLaunchDirection = Vector2.left;
    private float queuedLaunchForce;
    private readonly ContactPoint2D[] groundContacts = new ContactPoint2D[8];

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
        stableContactSettleTime = Mathf.Max(0.01f, stableContactSettleTime);
        stableContactGraceTime = Mathf.Max(0f, stableContactGraceTime);
        edgeStableHorizontalSpeedThreshold = Mathf.Max(0f, edgeStableHorizontalSpeedThreshold);
        edgeStableVerticalSpeedThreshold = Mathf.Max(0f, edgeStableVerticalSpeedThreshold);
        minStableContactNormalY = Mathf.Clamp(minStableContactNormalY, -1f, 1f);
        groundedCheckDistance = Mathf.Max(0.01f, groundedCheckDistance);
        footInset = Mathf.Max(0f, footInset);

        minWallBounceSpeed = Mathf.Max(0f, minWallBounceSpeed);
        wallBounceMultiplier = Mathf.Max(0f, wallBounceMultiplier);
        wallBounceUpwardBoost = Mathf.Max(0f, wallBounceUpwardBoost);
        maxWallBounceSpeed = Mathf.Max(0f, maxWallBounceSpeed);
        minWallNormalAbsX = Mathf.Clamp01(minWallNormalAbsX);
        maxWallNormalY = Mathf.Clamp(maxWallNormalY, -1f, 1f);
        wallBounceCooldown = Mathf.Max(0f, wallBounceCooldown);
        wallDetachSpeed = Mathf.Max(0f, wallDetachSpeed);
        wallStableLockoutTime = Mathf.Max(0f, wallStableLockoutTime);
        genericWallMinBounceSpeed = Mathf.Max(0f, genericWallMinBounceSpeed);
        genericWallBounceMultiplier = Mathf.Max(0f, genericWallBounceMultiplier);
        genericWallUpwardBoost = Mathf.Max(0f, genericWallUpwardBoost);
        genericWallDetachSpeed = Mathf.Max(0f, genericWallDetachSpeed);
        genericWallMaxBounceSpeed = Mathf.Max(0f, genericWallMaxBounceSpeed);
        genericWallStableLockoutTime = Mathf.Max(0f, genericWallStableLockoutTime);

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
        ApplyRuntimeNoFrictionMaterial();
        EnsurePreviewLine();
        HideChargePreview();
    }

    private void Start()
    {
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        RefreshGrounding();
        groundedStabilityGraceTimer = isGrounded ? GroundedStabilityGraceTime : 0f;
        stableContactGraceTimer = HasContactStableCandidate() ? stableContactGraceTime : 0f;
        stableContactSettleTimer = HasContactStableCandidate() ? stableContactSettleTime : 0f;
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
        wallBounceAppliedThisStep = false;
        RefreshGrounding();
        UpdateWallTimers(Time.fixedDeltaTime);

        if (health != null && health.IsHurt)
        {
            CancelCharge();
            UpdateGroundedStabilityGrace(Time.fixedDeltaTime);
            state = ComputeMovementState(Time.fixedDeltaTime);
            StorePreCollisionVelocity();
            UpdateAnimatorState();
            return;
        }

        if (launchQueued)
        {
            ExecuteQueuedLaunch();
            StorePreCollisionVelocity();
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
        if (state != MovementState.Charging && !wallBounceAppliedThisStep)
            state = ComputeMovementState(Time.fixedDeltaTime);

        StorePreCollisionVelocity();
        UpdateAnimatorState();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryApplyWallBounce(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryApplyWallBounce(collision);
    }

    private void OnDisable()
    {
        HideChargePreview();
    }

    private void OnDestroy()
    {
        if (runtimePreviewMaterial == null)
        {
            DestroyRuntimeNoFrictionMaterial();
            return;
        }

        if (Application.isPlaying)
            Destroy(runtimePreviewMaterial);
        else
            DestroyImmediate(runtimePreviewMaterial);

        DestroyRuntimeNoFrictionMaterial();
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

        if (HasContactStableCandidate())
        {
            stableContactGraceTimer = stableContactGraceTime;
            stableContactSettleTimer = stableContactSettleTime;
        }

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
        stableContactSettleTimer = 0f;
        stableContactGraceTimer = 0f;
        wallBounceCooldownTimer = 0f;
        wallStableLockoutTimer = 0f;
        state = MovementState.Airborne;
        HideChargePreview();
    }

    private void ExecuteQueuedLaunch()
    {
        launchQueued = false;
        chargeTimer = 0f;
        stableSettleTimer = 0f;
        groundedStabilityGraceTimer = 0f;
        stableContactSettleTimer = 0f;
        stableContactGraceTimer = 0f;
        wallBounceCooldownTimer = 0f;
        wallStableLockoutTimer = 0f;

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

    private void StorePreCollisionVelocity()
    {
        preCollisionVelocity = rb.linearVelocity;
    }

    private void RefreshGrounding()
    {
        isGrounded = CheckGrounded();
        RefreshGroundContacts();
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
        bool canEnterStable = CanEnterStableState();
        if (canEnterStable)
            ApplyStableVelocityCleanup(deltaTime);

        return canEnterStable ? MovementState.Stable : MovementState.Airborne;
    }

    private void UpdateStableSettleTimer(float deltaTime)
    {
        UpdateStableContactTimers(deltaTime);

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
        return isGrounded || groundedStabilityGraceTimer > 0f || HasSettledContactForStability();
    }

    private bool CanSettleIntoStable()
    {
        if (wallStableLockoutTimer > 0f)
            return false;

        if (!HasGroundContactForStability())
            return false;

        if (hasWallContact && !hasEdgeStableGroundContact && !isGrounded && groundedStabilityGraceTimer <= 0f)
            return false;

        Vector2 velocity = rb.linearVelocity;
        if (isGrounded || groundedStabilityGraceTimer > 0f || hasStableGroundContact)
        {
            return Mathf.Abs(velocity.x) <= stableHorizontalSpeedThreshold &&
                Mathf.Abs(velocity.y) <= stableVerticalSpeedThreshold;
        }

        return HasSettledContactForStability() &&
            Mathf.Abs(velocity.x) <= edgeStableHorizontalSpeedThreshold &&
            Mathf.Abs(velocity.y) <= edgeStableVerticalSpeedThreshold;
    }

    private bool CanEnterStableState()
    {
        return HasGroundContactForStability() && stableSettleTimer >= stableSettleTime;
    }

    private void UpdateStableContactTimers(float deltaTime)
    {
        bool contactCandidate = HasContactStableCandidate();
        if (contactCandidate)
        {
            stableContactSettleTimer = Mathf.Min(stableContactSettleTime, stableContactSettleTimer + deltaTime);
            stableContactGraceTimer = stableContactSettleTimer >= stableContactSettleTime
                ? stableContactGraceTime
                : 0f;
            return;
        }

        stableContactGraceTimer = Mathf.Max(0f, stableContactGraceTimer - deltaTime);
        if (stableContactGraceTimer <= 0f)
            stableContactSettleTimer = 0f;
    }

    private bool HasContactStableCandidate()
    {
        if (hasStableGroundContact)
            return true;

        return allowEdgeContactStable &&
            hasEdgeStableGroundContact &&
            IsMovingSlowEnoughForEdgeStable();
    }

    private bool HasSettledContactForStability()
    {
        return stableContactSettleTimer >= stableContactSettleTime || stableContactGraceTimer > 0f;
    }

    private bool IsMovingSlowEnoughForEdgeStable()
    {
        Vector2 velocity = rb.linearVelocity;
        return Mathf.Abs(velocity.x) <= edgeStableHorizontalSpeedThreshold &&
            Mathf.Abs(velocity.y) <= edgeStableVerticalSpeedThreshold;
    }

    private void ApplyStableVelocityCleanup(float deltaTime)
    {
        Vector2 velocity = rb.linearVelocity;
        if (velocity.sqrMagnitude <= stopSnapSpeed * stopSnapSpeed)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float newHorizontalVelocity = Mathf.MoveTowards(velocity.x, 0f, groundDeceleration * deltaTime);
        if (Mathf.Abs(newHorizontalVelocity) <= stopSnapSpeed)
            newHorizontalVelocity = 0f;

        float newVerticalVelocity = Mathf.MoveTowards(velocity.y, 0f, groundDeceleration * deltaTime);
        if (Mathf.Abs(newVerticalVelocity) <= stopSnapSpeed)
            newVerticalVelocity = 0f;

        rb.linearVelocity = new Vector2(newHorizontalVelocity, newVerticalVelocity);
    }

    private void RefreshGroundContacts()
    {
        hasStableGroundContact = false;
        hasEdgeStableGroundContact = false;
        hasWallContact = false;
        wallContactNormal = Vector2.zero;

        if (rb == null || col == null)
            return;

        int contactCount = rb.GetContacts(groundContacts);
        Bounds bounds = col.bounds;
        float edgeContactMaxY = bounds.min.y + Mathf.Max(groundedCheckDistance * 2f, bounds.size.y * 0.35f);

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            if (contact.collider == null || !IsInGroundLayer(contact.collider.gameObject.layer))
                continue;

            bool isWallContact = IsWallContactNormal(contact.normal);
            if (isWallContact)
            {
                hasWallContact = true;
                if (Mathf.Abs(contact.normal.x) > Mathf.Abs(wallContactNormal.x))
                    wallContactNormal = contact.normal;
            }

            if (!isWallContact && contact.normal.y >= minStableContactNormalY)
            {
                hasStableGroundContact = true;
                continue;
            }

            bool isLowEdgeContact = !isWallContact &&
                contact.point.y <= edgeContactMaxY &&
                contact.normal.y >= -0.05f;
            if (isLowEdgeContact)
                hasEdgeStableGroundContact = true;
        }
    }

    private void UpdateWallTimers(float deltaTime)
    {
        wallBounceCooldownTimer = Mathf.Max(0f, wallBounceCooldownTimer - deltaTime);
        wallStableLockoutTimer = Mathf.Max(0f, wallStableLockoutTimer - deltaTime);
    }

    private void TryApplyWallBounce(Collision2D collision)
    {
        if (!enableWallBounce || collision.collider == null)
            return;

        WallBounceSurface2D bounceSurface = collision.collider.GetComponentInParent<WallBounceSurface2D>();
        bool hasExplicitSurface = bounceSurface != null && bounceSurface.forceBounce;

        if (wallBounceCooldownTimer > 0f)
        {
            LogWallBounceSkip("cooldown");
            return;
        }

        if (!hasExplicitSurface && !enableGenericWallBounce)
        {
            LogWallBounceSkip("generic wall bounce ignored");
            return;
        }

        if (state == MovementState.Charging || (!hasExplicitSurface && (state != MovementState.Airborne || isGrounded)))
        {
            LogWallBounceSkip("not airborne");
            return;
        }

        if (!hasExplicitSurface && !IsInGroundLayer(collision.collider.gameObject.layer))
        {
            LogWallBounceSkip("not ground layer");
            return;
        }

        Vector2 incomingVelocity = GetWallBounceIncomingVelocity(collision);
        float speed = incomingVelocity.magnitude;
        float minBounceSpeed = hasExplicitSurface ? minWallBounceSpeed : genericWallMinBounceSpeed;
        if (speed < minBounceSpeed)
        {
            LogWallBounceSkip($"speed {speed:0.00} < {minBounceSpeed:0.00}");
            return;
        }

        bool ignoredTopContact = false;
        bool ignoredBottomContact = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (IsIgnoredWallBounceSurfaceContact(bounceSurface, contact.normal, out string ignoredReason))
            {
                ignoredTopContact |= ignoredReason == "top";
                ignoredBottomContact |= ignoredReason == "bottom";
                continue;
            }

            if (TryGetWallBounceNormal(contact.normal, incomingVelocity, hasExplicitSurface, out Vector2 bounceNormal))
            {
                ApplyWallBounce(incomingVelocity, bounceNormal, bounceSurface, hasExplicitSurface);
                return;
            }
        }

        if (ignoredTopContact)
            LogWallBounceSkip("ignored top contact");
        else if (ignoredBottomContact)
            LogWallBounceSkip("ignored bottom contact");
        else
            LogWallBounceSkip("no side contact");
    }

    private bool IsIgnoredWallBounceSurfaceContact(
        WallBounceSurface2D bounceSurface,
        Vector2 contactNormal,
        out string reason)
    {
        reason = string.Empty;
        if (bounceSurface == null || bounceSurface.IsBounceAllowedForNormal(contactNormal))
            return false;

        reason = bounceSurface.IsTopContact(contactNormal)
            ? "top"
            : bounceSurface.IsBottomContact(contactNormal)
                ? "bottom"
                : "non-side";

        return true;
    }

    private Vector2 GetWallBounceIncomingVelocity(Collision2D collision)
    {
        Vector2 relativeVelocity = collision.relativeVelocity;
        return relativeVelocity.sqrMagnitude > preCollisionVelocity.sqrMagnitude
            ? relativeVelocity
            : preCollisionVelocity;
    }

    private void ApplyWallBounce(
        Vector2 incomingVelocity,
        Vector2 bounceNormal,
        WallBounceSurface2D bounceSurface,
        bool hasExplicitSurface)
    {
        float defaultBounceMultiplier = hasExplicitSurface ? wallBounceMultiplier : genericWallBounceMultiplier;
        float defaultUpwardBoost = hasExplicitSurface ? wallBounceUpwardBoost : genericWallUpwardBoost;
        float defaultDetachSpeed = hasExplicitSurface ? wallDetachSpeed : genericWallDetachSpeed;
        float maxBounceSpeed = hasExplicitSurface ? maxWallBounceSpeed : genericWallMaxBounceSpeed;
        float stableLockoutTime = hasExplicitSurface ? wallStableLockoutTime : genericWallStableLockoutTime;

        float bounceMultiplier = GetOverrideOrDefault(
            hasExplicitSurface && bounceSurface != null ? bounceSurface.bounceMultiplierOverride : -1f,
            defaultBounceMultiplier);
        float upwardBoost = GetOverrideOrDefault(
            hasExplicitSurface && bounceSurface != null ? bounceSurface.upwardBoostOverride : -1f,
            defaultUpwardBoost);
        float detachSpeed = GetOverrideOrDefault(
            hasExplicitSurface && bounceSurface != null ? bounceSurface.detachSpeedOverride : -1f,
            defaultDetachSpeed);

        Vector2 bouncedVelocity = Vector2.Reflect(incomingVelocity, bounceNormal) * bounceMultiplier;
        bouncedVelocity += Vector2.up * upwardBoost;
        bouncedVelocity = EnsureMinimumDetachVelocity(bouncedVelocity, bounceNormal, detachSpeed, maxBounceSpeed);

        rb.linearVelocity = bouncedVelocity;
        wallBounceCooldownTimer = wallBounceCooldown;
        wallStableLockoutTimer = stableLockoutTime;
        wallBounceAppliedThisStep = true;
        stableSettleTimer = 0f;
        stableContactSettleTimer = 0f;
        stableContactGraceTimer = 0f;
        groundedStabilityGraceTimer = 0f;
        state = MovementState.Airborne;

        if (showWallBounceDebug)
        {
            string bounceType = hasExplicitSurface ? "explicit surface bounce" : "generic wall bounce";
            Debug.Log($"Wall bounce {bounceType}: incoming={incomingVelocity}, normal={bounceNormal}, outgoing={bouncedVelocity}");
        }
    }

    private float GetOverrideOrDefault(float overrideValue, float defaultValue)
    {
        return overrideValue >= 0f ? overrideValue : defaultValue;
    }

    private bool TryGetWallBounceNormal(
        Vector2 contactNormal,
        Vector2 incomingVelocity,
        bool forceWallSurface,
        out Vector2 bounceNormal)
    {
        bounceNormal = contactNormal.normalized;
        if (!forceWallSurface && !IsWallContactNormal(bounceNormal))
            return false;

        float normalVelocity = Vector2.Dot(incomingVelocity, bounceNormal);
        if (normalVelocity > 0f)
        {
            bounceNormal = -bounceNormal;
            normalVelocity = Vector2.Dot(incomingVelocity, bounceNormal);
        }

        return normalVelocity < 0f;
    }

    private Vector2 EnsureMinimumDetachVelocity(
        Vector2 velocity,
        Vector2 wallNormal,
        float detachSpeed,
        float maxBounceSpeed)
    {
        float maxSpeed = Mathf.Max(0f, maxBounceSpeed);
        float normalSpeed = Mathf.Max(Vector2.Dot(velocity, wallNormal), Mathf.Max(0f, detachSpeed));
        if (maxSpeed > 0f)
            normalSpeed = Mathf.Min(normalSpeed, maxSpeed);

        Vector2 normalVelocity = wallNormal * normalSpeed;
        Vector2 tangentVelocity = velocity - (wallNormal * Vector2.Dot(velocity, wallNormal));

        if (maxSpeed <= 0f)
            return normalVelocity;

        float tangentSpeedLimit = Mathf.Sqrt(Mathf.Max(0f, (maxSpeed * maxSpeed) - normalVelocity.sqrMagnitude));
        tangentVelocity = Vector2.ClampMagnitude(tangentVelocity, tangentSpeedLimit);
        return normalVelocity + tangentVelocity;
    }

    private bool IsWallContactNormal(Vector2 normal)
    {
        return Mathf.Abs(normal.x) >= minWallNormalAbsX && Mathf.Abs(normal.y) <= maxWallNormalY;
    }

    private void LogWallBounceSkip(string reason)
    {
        if (showWallBounceDebug)
            Debug.Log($"Wall bounce skipped: {reason}");
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

        if (showStableDebug)
        {
            Debug.DrawRay(leftFoot, Vector2.down * groundedCheckDistance, Color.cyan);
            Debug.DrawRay(rightFoot, Vector2.down * groundedCheckDistance, Color.cyan);
        }

        return leftHit.collider != null || rightHit.collider != null;
    }

    private bool IsInGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
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

    private void ApplyRuntimeNoFrictionMaterial()
    {
        if (!Application.isPlaying || !useRuntimeNoFrictionMaterial || col == null)
            return;

        runtimeNoFrictionMaterial = new PhysicsMaterial2D("Runtime Player No Friction")
        {
            friction = 0f,
            bounciness = 0f
        };
        col.sharedMaterial = runtimeNoFrictionMaterial;
    }

    private void DestroyRuntimeNoFrictionMaterial()
    {
        if (runtimeNoFrictionMaterial == null)
            return;

        if (Application.isPlaying)
            Destroy(runtimeNoFrictionMaterial);
        else
            DestroyImmediate(runtimeNoFrictionMaterial);
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
        {
            UpgradeWallBounceValuesIfNeeded();
            return;
        }

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
        UpgradeWallBounceValuesIfNeeded();
    }

    private void UpgradeWallBounceValuesIfNeeded()
    {
        bool hasFirstWallBounceDefaults =
            Approximately(minWallBounceSpeed, 1.8f) &&
            Approximately(wallBounceMultiplier, 0.45f) &&
            Approximately(wallBounceUpwardBoost, 0.35f) &&
            Approximately(maxWallBounceSpeed, 4.5f) &&
            Approximately(minWallNormalAbsX, 0.65f) &&
            Approximately(maxWallNormalY, 0.35f) &&
            Approximately(wallBounceCooldown, 0.12f);

        bool hasStrongWallBounceDefaults =
            Approximately(minWallBounceSpeed, 0.75f) &&
            Approximately(wallBounceMultiplier, 0.75f) &&
            Approximately(wallBounceUpwardBoost, 0.55f) &&
            Approximately(maxWallBounceSpeed, 6.5f) &&
            Approximately(minWallNormalAbsX, 0.60f) &&
            Approximately(maxWallNormalY, 0.40f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.60f);

        bool hasSoftWallBounceDefaults =
            Approximately(minWallBounceSpeed, 0.75f) &&
            Approximately(wallBounceMultiplier, 0.55f) &&
            Approximately(wallBounceUpwardBoost, 0.30f) &&
            Approximately(maxWallBounceSpeed, 4.8f) &&
            Approximately(minWallNormalAbsX, 0.60f) &&
            Approximately(maxWallNormalY, 0.40f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.15f);

        bool hasOverpoweredSurfaceDefaults =
            Approximately(minWallBounceSpeed, 0.25f) &&
            Approximately(wallBounceMultiplier, 0.90f) &&
            Approximately(wallBounceUpwardBoost, 0.75f) &&
            Approximately(maxWallBounceSpeed, 7.5f) &&
            Approximately(minWallNormalAbsX, 0.40f) &&
            Approximately(maxWallNormalY, 0.75f) &&
            Approximately(wallBounceCooldown, 0.06f) &&
            Approximately(wallDetachSpeed, 2.2f);

        bool hasModerateSurfaceDefaults =
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.62f) &&
            Approximately(wallBounceUpwardBoost, 0.38f) &&
            Approximately(maxWallBounceSpeed, 5.2f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.25f);

        bool hasReducedSurfaceDefaultsWithGenericDisabled =
            !enableGenericWallBounce &&
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.50f) &&
            Approximately(wallBounceUpwardBoost, 0.25f) &&
            Approximately(maxWallBounceSpeed, 4.4f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 0.95f);

        bool hasReducedSurfaceDefaultsWithGenericEnabled =
            enableGenericWallBounce &&
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.50f) &&
            Approximately(wallBounceUpwardBoost, 0.25f) &&
            Approximately(maxWallBounceSpeed, 4.4f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 0.95f);

        bool hasMidSurfaceDefaultsWithWeakGeneric =
            enableGenericWallBounce &&
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.56f) &&
            Approximately(wallBounceUpwardBoost, 0.31f) &&
            Approximately(maxWallBounceSpeed, 4.8f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.10f) &&
            Approximately(genericWallBounceMultiplier, 0.18f) &&
            Approximately(genericWallUpwardBoost, 0.08f) &&
            Approximately(genericWallDetachSpeed, 0.35f) &&
            Approximately(genericWallMaxBounceSpeed, 2.2f);

        bool hasDedicatedSurfaceDefaultsWithSoftGeneric =
            enableGenericWallBounce &&
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.68f) &&
            Approximately(wallBounceUpwardBoost, 0.42f) &&
            Approximately(maxWallBounceSpeed, 5.6f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.35f) &&
            Approximately(genericWallBounceMultiplier, 0.28f) &&
            Approximately(genericWallUpwardBoost, 0.14f) &&
            Approximately(genericWallDetachSpeed, 0.55f) &&
            Approximately(genericWallMaxBounceSpeed, 2.8f);

        bool hasDedicatedSurfaceDefaultsWithCurrentGeneric =
            enableGenericWallBounce &&
            Approximately(minWallBounceSpeed, 0.35f) &&
            Approximately(wallBounceMultiplier, 0.68f) &&
            Approximately(wallBounceUpwardBoost, 0.42f) &&
            Approximately(maxWallBounceSpeed, 5.6f) &&
            Approximately(minWallNormalAbsX, 0.45f) &&
            Approximately(maxWallNormalY, 0.55f) &&
            Approximately(wallBounceCooldown, 0.10f) &&
            Approximately(wallDetachSpeed, 1.35f) &&
            Approximately(genericWallBounceMultiplier, 0.38f) &&
            Approximately(genericWallUpwardBoost, 0.20f) &&
            Approximately(genericWallDetachSpeed, 0.75f) &&
            Approximately(genericWallMaxBounceSpeed, 3.3f);

        bool hasUninitializedWallBounceDefaults =
            Approximately(minWallBounceSpeed, 0f) &&
            Approximately(wallBounceMultiplier, 0f) &&
            Approximately(wallBounceUpwardBoost, 0f) &&
            Approximately(maxWallBounceSpeed, 0f) &&
            Approximately(minWallNormalAbsX, 0f) &&
            Approximately(maxWallNormalY, 0f) &&
            Approximately(wallBounceCooldown, 0f);

        if (!hasFirstWallBounceDefaults &&
            !hasStrongWallBounceDefaults &&
            !hasSoftWallBounceDefaults &&
            !hasOverpoweredSurfaceDefaults &&
            !hasModerateSurfaceDefaults &&
            !hasReducedSurfaceDefaultsWithGenericDisabled &&
            !hasReducedSurfaceDefaultsWithGenericEnabled &&
            !hasMidSurfaceDefaultsWithWeakGeneric &&
            !hasDedicatedSurfaceDefaultsWithSoftGeneric &&
            !hasDedicatedSurfaceDefaultsWithCurrentGeneric &&
            !hasUninitializedWallBounceDefaults)
        {
            return;
        }

        enableGenericWallBounce = true;
        minWallBounceSpeed = 0.35f;
        wallBounceMultiplier = 0.74f;
        wallBounceUpwardBoost = 0.48f;
        maxWallBounceSpeed = 6.1f;
        minWallNormalAbsX = 0.45f;
        maxWallNormalY = 0.55f;
        wallBounceCooldown = 0.10f;
        wallDetachSpeed = 1.55f;
        wallStableLockoutTime = 0.16f;
        genericWallMinBounceSpeed = 0.25f;
        genericWallBounceMultiplier = 0.45f;
        genericWallUpwardBoost = 0.26f;
        genericWallDetachSpeed = 0.95f;
        genericWallMaxBounceSpeed = 3.8f;
        genericWallStableLockoutTime = 0.06f;
        useRuntimeNoFrictionMaterial = true;
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
