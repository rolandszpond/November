using Synty.AnimationBaseLocomotion.Samples.InputSystem;
using UnityEngine;

// Movement, ground/crouch checks, Build-mode freezing and the locomotion FSM.
// Animator parameters (gait, strafe, lean, head/body look, start animations) live in PlayerController.Animation.cs.
[RequireComponent(typeof(CharacterController))]
public partial class PlayerController : MonoBehaviour
{
    private const float AnimationDampTime = 5f;

    private CharacterController controller;
    public InputReader input;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.4f;
    [SerializeField] private float runSpeed = 2.5f;
    [SerializeField] private float sprintSpeed = 7f;
    [SerializeField] private float crouchSpeed = 1.4f;
    [SerializeField] private float speedChangeDamping = 10f;
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float gravity = -9.81f;
    [Tooltip("Gravity is multiplied by this (Synty sample uses 2). Fall speed is capped at |gravity|.")]
    [SerializeField] private float gravityMultiplier = 2f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Turn In Place")]
    [Tooltip("Angle (degrees) between character and camera before the character turns to face the camera while idle.")]
    [SerializeField] private float turnInPlaceThreshold = 45f;
    [Tooltip("Turning stops once the character is within this angle of the camera direction.")]
    [SerializeField] private float turnInPlaceStopAngle = 5f;

    [Header("Crouch")]
    [Tooltip("CharacterController height while crouching. Standing height/center are read from the CharacterController.")]
    [SerializeField] private float crouchHeight = 1.2f;
    [Tooltip("Layers that block standing up from a crouch.")]
    [SerializeField] private LayerMask ceilingLayers = ~0;

    // Defaults match the Synty sample player prefab.
    [Header("Ground")]
    [Tooltip("Layers counted as ground. The player's own layer is always excluded.")]
    [SerializeField] private LayerMask groundLayers = 1; // Default
    [Tooltip("Ground check sphere sits this far below the transform (negative = up). Sample: -0.14.")]
    [SerializeField] private float groundedOffset = -0.14f;
    [Tooltip("Height above the feet that the slope rays start from.")]
    [SerializeField] private float inclineRayHeight = 0.8f;
    [Tooltip("Distance in front of / behind the character for the two slope rays.")]
    [SerializeField] private float inclineRayOffset = 0.15f;

    private float verticalVelocity;
    private Vector3 horizontalVelocity;
    private float currentMaxSpeed;
    private float speed2D;
    private bool isSprinting;
    private bool isWalking;
    private bool isCrouching;
    private float standingHeight;
    private Vector3 standingCenter;

    private bool isGrounded = true;
    private float inclineAngle;
    private float fallingDuration;

    // Facing / turning
    private bool isStrafing = true;
    private bool isStopped = true;
    private bool isTurningInPlace;
    private float cameraRotationOffset;
    private float turnAngleToCamera;
    private float shuffleDirectionX;
    private float shuffleDirectionZ = 1f;

    public StateMachine StateMachine { get; private set; }
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public CrouchState CrouchState { get; private set; }
    public FallState FallState { get; private set; }

    public bool HasMoveInput => input._moveComposite.sqrMagnitude > 0f;
    public bool IsGrounded => isGrounded;
    public float VerticalVelocity => verticalVelocity;
    public bool IsJumping { get; set; }
    public bool CrouchHeld { get; private set; }
    public bool ShouldCrouch => CrouchHeld && IsGrounded;
    public bool IsSliding { get; private set; }

    // Where to go on landing.
    public IState LandingState => ShouldCrouch ? CrouchState : HasMoveInput ? (IState)MoveState : IdleState;

    private void Awake()
    {
        if (input == null)
        {
            input = GetComponent<InputReader>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Root motion is handled in OnAnimatorMove (turn-in-place rotation, slide distance).
        animator.applyRootMotion = true;

        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        previousForward = transform.forward;
        StateMachine = new StateMachine();
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        JumpState = new JumpState(this);
        FallState = new FallState(this);
        CrouchState = new CrouchState(this);

        StateMachine.ChangeState(IdleState);

        input.onSprintActivated += ActivateSprint;
        input.onSprintDeactivated += () => isSprinting = false;
        input.onWalkToggled += ToggleWalk;
        input.onCrouchActivated += () => CrouchHeld = true;
        input.onCrouchDeactivated += () => CrouchHeld = false;
    }

    // Every state moves the same way, so movement and the animator are driven here; states only decide transitions.
    private void Update()
    {
        GroundedCheck();
        Move();
        StateMachine.Update();
        UpdateAnimator();
    }

    // ---------------------------------------------------------------- Ground

    // Sphere check at the feet, like the Synty sample. More reliable than CharacterController.isGrounded,
    // which flickers on slopes and steps.
    private void GroundedCheck()
    {
        int mask = groundLayers & ~(1 << gameObject.layer);
        Vector3 spherePosition = transform.position + Vector3.down * groundedOffset;
        isGrounded = Physics.CheckSphere(spherePosition, controller.radius, mask, QueryTriggerInteraction.Ignore);

        if (isGrounded)
        {
            GroundInclineCheck(mask);
        }
    }

    // Two downward rays in front of and behind the character; the height difference gives the slope
    // (positive = uphill) for the animator's incline blend.
    private void GroundInclineCheck(int mask)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 rayStart = transform.position + Vector3.up * inclineRayHeight;
        Vector3 frontStart = rayStart + forward * inclineRayOffset;
        Vector3 rearStart = rayStart - forward * inclineRayOffset;
        float rayLength = inclineRayHeight + 2f;

        float targetAngle = 0f;
        if (Physics.Raycast(frontStart, Vector3.down, out RaycastHit frontHit, rayLength, mask, QueryTriggerInteraction.Ignore)
            && Physics.Raycast(rearStart, Vector3.down, out RaycastHit rearHit, rayLength, mask, QueryTriggerInteraction.Ignore))
        {
            Vector3 hitDifference = frontHit.point - rearHit.point;
            float horizontalLength = new Vector2(hitDifference.x, hitDifference.z).magnitude;
            targetAngle = Mathf.Atan2(hitDifference.y, horizontalLength) * Mathf.Rad2Deg;
        }

        inclineAngle = Mathf.Lerp(inclineAngle, targetAngle, 20f * Time.deltaTime);
    }

    // ---------------------------------------------------------------- Jump / fall

    public void Jump()
    {
        verticalVelocity = jumpForce;
    }

    // Called when entering the fall state (as in the sample, vertical speed restarts from zero).
    public void BeginFall()
    {
        fallingDuration = 0f;
        verticalVelocity = 0f;
    }

    public void UpdateFallingDuration()
    {
        fallingDuration += Time.deltaTime;
    }

    // ---------------------------------------------------------------- Sprint / walk

    private void ActivateSprint()
    {
        if (isCrouching)
        {
            return;
        }

        isWalking = false;
        isSprinting = true;
    }

    private void ToggleWalk()
    {
        isWalking = !isWalking && IsGrounded && !isSprinting;
    }

    // ---------------------------------------------------------------- Crouch / slide

    // Animation events on the Synty sprint-to-crouch slide clip. Only valid while crouched.
    public void ActivateSliding()
    {
        IsSliding = isCrouching;
    }

    public void DeactivateSliding()
    {
        IsSliding = false;
    }

    // Shrinks/restores the capsule, keeping its bottom on the ground.
    public void SetCrouching(bool crouching)
    {
        isCrouching = crouching;
        if (!crouching)
        {
            IsSliding = false;
        }

        if (crouching)
        {
            isSprinting = false;
            float bottom = standingCenter.y - standingHeight * 0.5f;
            controller.height = crouchHeight;
            controller.center = new Vector3(standingCenter.x, bottom + crouchHeight * 0.5f, standingCenter.z);
        }
        else
        {
            controller.height = standingHeight;
            controller.center = standingCenter;
        }
    }

    // True if nothing above the crouched capsule would block the full standing height.
    public bool CanStandUp()
    {
        float radius = controller.radius * 0.95f;
        float bottom = standingCenter.y - standingHeight * 0.5f;
        Vector3 origin = transform.position + Vector3.up * (bottom + crouchHeight - radius);
        float distance = standingHeight - crouchHeight;

        // The cast starts inside our own capsule, so the CharacterController itself isn't reported.
        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ceilingLayers, QueryTriggerInteraction.Ignore);
    }

    // ---------------------------------------------------------------- Build mode freeze

    // Build mode is only allowed from a settled, grounded state (not mid-air, crouched or sliding).
    public bool CanEnterBuildMode => !IsFrozen && IsGrounded && !IsSliding
        && (StateMachine.CurrentState == IdleState || StateMachine.CurrentState == MoveState);

    public bool IsFrozen { get; private set; }

    // Stops the character for Build mode: input off, momentum and held buttons cleared, animator sent to
    // idle, then this component disabled so the locomotion FSM stops updating.
    public void Freeze()
    {
        if (IsFrozen)
        {
            return;
        }

        IsFrozen = true;

        // Disabling the reader disables its action map, so no callbacks (jump, crouch, ...) fire while frozen.
        // Its cached values aren't cleared by that, so zero them here.
        input.enabled = false;
        input._moveComposite = Vector2.zero;
        input._mouseDelta = Vector2.zero;
        input._movementInputDetected = false;

        isSprinting = false;
        CrouchHeld = false;
        horizontalVelocity = Vector3.zero;
        currentMaxSpeed = 0f;
        speed2D = 0f;
        isStopped = true;
        isTurningInPlace = false;
        cameraRotationOffset = 0f;
        ResetAnimationState();

        StateMachine.ChangeState(IdleState);
        UpdateAnimator();

        enabled = false;
    }

    public void Unfreeze()
    {
        if (!IsFrozen)
        {
            return;
        }

        IsFrozen = false;
        enabled = true;
        input.enabled = true;
    }

    // ---------------------------------------------------------------- Movement

    private void Move()
    {
        Vector2 moveInput = Vector2.ClampMagnitude(input._moveComposite, 1f);

        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;
        if (cameraTransform != null)
        {
            cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        bool hasInput = moveDirection.sqrMagnitude > 0.0001f;

        // Keep momentum in the air; otherwise blend toward sprint, walk (toggled) or run (default) speed.
        if (IsGrounded)
        {
            float targetMaxSpeed = isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : isWalking ? walkSpeed : runSpeed;
            currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMaxSpeed, AnimationDampTime * Time.deltaTime);
        }

        Vector3 targetVelocity = moveDirection * currentMaxSpeed;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, speedChangeDamping * Time.deltaTime);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        // Same as the sample: stronger gravity, but stop accelerating once falling at |gravity| m/s.
        if (verticalVelocity > gravity)
        {
            verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }

        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        speed2D = Mathf.Round(horizontalVelocity.magnitude * 1000f) / 1000f;

        // Sprinting faces the move direction; otherwise strafe facing the camera.
        isStrafing = !isSprinting;

        Vector3 characterForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 characterRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 inputDirection = hasInput ? moveDirection.normalized : Vector3.zero;

        UpdateStarting(hasInput, characterForward, inputDirection);

        if (IsSliding)
        {
            // Hold the facing so the slide travels in a straight line.
            isTurningInPlace = false;
        }
        else if (isStrafing && hasInput)
        {
            // Shuffle direction is set instantly (not smoothed) and keeps its last value after input is
            // released, so a tapped shuffle plays to the end of its clip in the right direction.
            shuffleDirectionZ = Vector3.Dot(characterForward, inputDirection);
            shuffleDirectionX = Vector3.Dot(characterRight, inputDirection);

            isTurningInPlace = false;
            FaceDirection(cameraForward);
            cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSpeed * Time.deltaTime);
        }
        else if (isStrafing)
        {
            UpdateTurnInPlace(cameraForward);
        }
        else
        {
            shuffleDirectionZ = 1f;
            shuffleDirectionX = 0f;

            isTurningInPlace = false;
            cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSpeed * Time.deltaTime);

            // Face the actual velocity (as in the sample), so the turn lags behind input while the
            // start animation plays.
            FaceDirection(horizontalVelocity);
        }

        UpdateMovementInputState(hasInput);
        UpdateStrafeValues(moveDirection, hasInput);
        UpdateGait();
        isStopped = !hasInput && speed2D < 0.5f;
        UpdateRotationalAdditives();
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // ---------------------------------------------------------------- Turn in place / root motion

    // Standing still: the character ignores small camera turns. Once the camera is more than
    // turnInPlaceThreshold degrees away, the turn animation plays and its root motion rotates
    // the character until it faces the camera again.
    private void UpdateTurnInPlace(Vector3 cameraForward)
    {
        Vector3 characterForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float angleToCamera = Vector3.SignedAngle(characterForward, cameraForward, Vector3.up);

        if (!isTurningInPlace && IsGrounded && Mathf.Abs(angleToCamera) > turnInPlaceThreshold)
        {
            isTurningInPlace = true;
        }

        if (isTurningInPlace && Mathf.Abs(angleToCamera) <= turnInPlaceStopAngle)
        {
            isTurningInPlace = false;
        }

        turnAngleToCamera = angleToCamera;

        // Drives the left/right turn blend tree (thresholds -25..25 in the Synty controller).
        float targetOffset = isTurningInPlace ? angleToCamera : 0f;
        cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, targetOffset, 20f * Time.deltaTime);
    }

    // Unity hands root motion here instead of applying it. Position normally comes from Move(), so root
    // translation is only used while sliding; root rotation is only used while turning in place.
    private void OnAnimatorMove()
    {
        Quaternion deltaRotation = animator.deltaRotation;

        // Like the Synty sample, root translation is added on top of Move()'s velocity. The only clip
        // with real translation is the sprint-to-crouch slide, which adds its ~4.4 m to the sprint momentum.
        Vector3 rootDelta = Vector3.ProjectOnPlane(animator.deltaPosition, Vector3.up);
        if (rootDelta.sqrMagnitude > 0f)
        {
            controller.Move(rootDelta);
        }

        if (!isTurningInPlace)
        {
            return;
        }

        // Use the animation's yaw speed, but always rotate toward the camera and never past it.
        float animYaw = Mathf.Abs(Mathf.DeltaAngle(0f, deltaRotation.eulerAngles.y));
        float step = Mathf.Min(animYaw, Mathf.Abs(turnAngleToCamera)) * Mathf.Sign(turnAngleToCamera);
        transform.rotation = Quaternion.Euler(0f, step, 0f) * transform.rotation;
        turnAngleToCamera -= step;
    }
}
