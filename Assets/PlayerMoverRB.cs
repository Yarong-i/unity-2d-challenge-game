using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerMoverRB : MonoBehaviour
{
    private enum PlayerState
    {
        Grounded,
        Air
    }

    [Header("Move (Max Speed)")]
    [SerializeField] private float maxSpeed = 5f;

    [Header("Move Feel (Accel/Decel)")]
    [SerializeField] private float groundAcceleration = 60f;
    [SerializeField] private float groundDeceleration = 80f;
    [SerializeField] private float airAcceleration = 40f;
    [SerializeField] private float airDeceleration = 30f;

    [Header("Jump")]
    [SerializeField] private float jumpPower = 8.5f;

    [Header("Jump Cut (tap = low / hold = high)")]
    [Range(0.1f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.6f;

    [Header("Coyote Time / Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.08f;
    [SerializeField] private float footInset = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool debugUI = false;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.5f; // 0.35가 빠르게 느껴지면 0.45~0.6 추천

    [SerializeField] private PlayerAnimator2D playerAnimator;

    private bool dashPressed;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;
    private int facing = 1; // 1=오른쪽, -1=왼쪽

    // 공중 대시 1회 제한용
    private bool airDashAvailable = true;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private PlayerHealth2D health;

    // Input memo (Update -> FixedUpdate)
    private float moveInput;
    private bool jumpPressed;
    private bool jumpReleased;
    private bool jumpHeld;

    // State
    private bool isGrounded;
    private PlayerState state;

    // Timers
    private float coyoteCounter;
    private float jumpBufferCounter;

    // For buffered tap jump
    private bool jumpCutQueued;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        health = GetComponent<PlayerHealth2D>();
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator2D>();
    }

    private void Start()
    {
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");

        isGrounded = CheckGrounded();
        state = isGrounded ? PlayerState.Grounded : PlayerState.Air;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;

        // 시작 시 공중이면 아직 공중 대시 1회는 남아있게
        airDashAvailable = true;
    }

    private void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
            jumpCutQueued = false;
        }

        jumpHeld = Input.GetKey(KeyCode.Space);

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpReleased = true;
            jumpCutQueued = true;
        }

        if (Input.GetKeyDown(dashKey))
            dashPressed = true;

        // 바라보는 방향 갱신(입력이 있을 때만)
        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 쿨타임 감소
        if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;

        // 대시 중이면 대시만 처리하고 끝
        if (isDashing)
        {
            dashTimer -= dt;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(facing * dashSpeed, 0f);
#else
            rb.velocity = new Vector2(facing * dashSpeed, 0f);
#endif

            if (dashTimer <= 0f)
                isDashing = false;

            dashPressed = false;
            return;
        }

        // Ground check
        isGrounded = CheckGrounded();
        state = isGrounded ? PlayerState.Grounded : PlayerState.Air;

        // 착지하면 공중 대시 1회 충전
        if (state == PlayerState.Grounded)
            airDashAvailable = true;

        // Coyote update
        if (state == PlayerState.Grounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter = Mathf.Max(0f, coyoteCounter - dt);

        // Jump buffer update
        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - dt);

        // Hurt면 조작 스킵
        bool isHurt = (health != null && health.IsHurt);
        if (isHurt)
        {
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpPressed = false;
            jumpReleased = false;
            dashPressed = false;
            return;
        }

        // Dash start
        if (dashPressed && CanStartDash())
        {
            StartDash();
        }
        dashPressed = false;

        if (playerAnimator != null)
        {
            playerAnimator.SetGrounded(isGrounded);
            playerAnimator.SetDashing(isDashing);
        }

        // Horizontal movement (Accel/Decel)
        ApplyHorizontalMovement(dt);

        // Jump execute condition (Buffer + Coyote)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            DoJump();
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
        }

        // Jump cut on release (only while rising)
        if (jumpReleased)
        {
#if UNITY_6000_0_OR_NEWER
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
#else
            if (rb.velocity.y > 0f)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
#endif
        }

        // Reset one-frame inputs
        jumpPressed = false;
        jumpReleased = false;
    }

    private bool CanStartDash()
    {
        if (dashCooldownTimer > 0f) return false;

        // 공중에서는 1회만 가능
        if (state == PlayerState.Air && !airDashAvailable)
            return false;

        return true;
    }

    private void ApplyHorizontalMovement(float dt)
    {
        float desiredX = moveInput * maxSpeed;

        bool hasInput = Mathf.Abs(moveInput) > 0.01f;

        float accel = (state == PlayerState.Grounded) ? groundAcceleration : airAcceleration;
        float decel = (state == PlayerState.Grounded) ? groundDeceleration : airDeceleration;

        float rate = hasInput ? accel : decel;

#if UNITY_6000_0_OR_NEWER
        float currentX = rb.linearVelocity.x;
        float newX = Mathf.MoveTowards(currentX, desiredX, rate * dt);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
#else
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, desiredX, rate * dt);
        rb.velocity = new Vector2(newX, rb.velocity.y);
#endif
    }

    private void DoJump()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
#else
        rb.velocity = new Vector2(rb.velocity.x, 0f);
#endif
        rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);

        // Buffered tap jump support
        if (!jumpHeld || jumpCutQueued)
        {
#if UNITY_6000_0_OR_NEWER
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
#else
            if (rb.velocity.y > 0f)
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
#endif
        }

        jumpCutQueued = false;
    }

    private bool CheckGrounded()
    {
        Bounds b = col.bounds;

        Vector2 leftFoot = new Vector2(b.min.x + footInset, b.min.y);
        Vector2 rightFoot = new Vector2(b.max.x - footInset, b.min.y);

        RaycastHit2D hitL = Physics2D.Raycast(leftFoot, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(rightFoot, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(leftFoot, Vector2.down * groundCheckDistance, Color.red);
        Debug.DrawRay(rightFoot, Vector2.down * groundCheckDistance, Color.red);

        return hitL.collider != null || hitR.collider != null;
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 공중에서 대시를 시작하면 1회 소모
        if (state == PlayerState.Air)
            airDashAvailable = false;

        // 시작 순간 y속도를 정리해서 수평 대시가 깔끔하게
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
#else
        rb.velocity = new Vector2(rb.velocity.x, 0f);
#endif
    }

    private void OnGUI()
    {
        if (!debugUI) return;

#if UNITY_6000_0_OR_NEWER
        float yVel = rb.linearVelocity.y;
        float xVel = rb.linearVelocity.x;
#else
        float yVel = rb.velocity.y;
        float xVel = rb.velocity.x;
#endif

        GUI.Label(new Rect(10, 10, 700, 20), $"State: {state} | isGrounded: {isGrounded}");
        GUI.Label(new Rect(10, 30, 700, 20), $"xVel: {xVel:F3} | yVel: {yVel:F3}");
        GUI.Label(new Rect(10, 50, 700, 20), $"coyote: {coyoteCounter:F3}/{coyoteTime:F3} | buffer: {jumpBufferCounter:F3}/{jumpBufferTime:F3}");
        GUI.Label(new Rect(10, 70, 700, 20), $"Dash: cooldown {dashCooldownTimer:F2} | airDashAvailable {airDashAvailable}");
    }
}














































/*using UnityEngine;

public class PlayerMoverRB:MonoBehaviour
{
    [Header("Move/Jump")]
    public float speed = 5f;
    public float jumpPower = 10f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.08f;

    Rigidbody2D rb;
    BoxCollider2D col;

    float moveInput;
    bool jumpPressed;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
    }

    void Start()
    {
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");
    }

    void Update()
    {
        Debug.Log(isGrounded);

        moveInput = Input.GetAxisRaw("Horizontal");

        if(Input.GetKeyDown(KeyCode.Space))
            jumpPressed = true;

        isGrounded = CheckGrounded();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
        jumpPressed = false;
    }

    bool CheckGrounded()
    {
        Bounds b= col.bounds;

        Vector2 leftFoot = new Vector2(b.min.x +0.05f,b.min.y);
        Vector2 rightFoot = new Vector2(b.max.x - 0.05f, b.min.y);

        RaycastHit2D hitL = Physics2D.Raycast(leftFoot, Vector2.down, groundCheckDistance, groundLayer);
        RaycastHit2D hitR = Physics2D.Raycast(rightFoot, Vector2.down, groundCheckDistance, groundLayer);

        Debug.DrawRay(leftFoot, Vector2.down * groundCheckDistance, Color.red);
        Debug.DrawRay(rightFoot, Vector2.down * groundCheckDistance, Color.red);

        return hitL.collider != null || hitR.collider != null;
    }
}*/






























/*using UnityEngine;

public class playerMoverRB: MonoBehaviour
{
    public float speed = 5f;
    public float jumpPower = 10f;

    Rigidbody2D rb;
    float moveInput;
    bool jumpInput;
    bool isGrounded;
    LayerMask groundLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        groundLayer = LayerMask.GetMask("Ground");
    }
    void Update()
    {
        Debug.Log(isGrounded);
        Vector2 origin = new Vector2(transform.position.x, transform.position.y-0.5f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.1f,groundLayer);

        if(hit.collider != null)
        {
            isGrounded = true;
            Debug.Log("Hit : " + hit.collider.name);
        }
            
        else
            isGrounded = false;

        moveInput = Input.GetAxis("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpInput = true;
        }
    }
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(moveInput * speed,rb.linearVelocity.y);

        if (jumpInput && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            jumpInput = false;
        }
    }
}*/









/*using UnityEngine;

public class PlayerMoveRB : MonoBehaviour
{
    public float speed = 20f;
    Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(x * speed, rb.linearVelocity.y);
    }
}*/
//using UnityEngine;

//public class playermove2: MonoBehaviour
//{
//   public float speed = 20f;
//   void Update()
//   {   Debug.Log(Input.GetAxis("Horizontal"));
//        float x = Input.GetAxis("Horizontal");
//        transform.Translate(Vector3.right*x* speed*Time.deltaTime);
//   }

//}
