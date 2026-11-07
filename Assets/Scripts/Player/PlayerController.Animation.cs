using UnityEngine;

// Animator-facing half of PlayerController: computes the values the Synty animator controller expects
// (gait, strafe, start animations, lean, head/body look, input tap/hold) and writes them each frame.
public partial class PlayerController
{
    // Matches the CurrentGait int values expected by the Synty animator controller.
    private enum GaitState
    {
        Idle = 0,
        Walk = 1,
        Run = 2,
        Sprint = 3
    }

    private const float StrafeDirectionDampTime = 20f;
    private const float ForwardStrafeMinThreshold = -55f;
    private const float ForwardStrafeMaxThreshold = 125f;
    private const float StartDelay = 0.2f;
    private const float MaxLookRotationRate = 275f;
    private const float LookSmoothness = 5f;

    // Curve defaults are copied from the Synty sample player prefab.
    [Header("Head Look")]
    [SerializeField] private bool enableHeadLook = true;
    [SerializeField] private AnimationCurve headLookXCurve = new AnimationCurve(
        new Keyframe(-1f, -0.3f),
        new Keyframe(0f, 0f, 0.0246f, 0.0246f),
        new Keyframe(1f, 0.3f, 0.1221f, 0.1221f));

    [Header("Body Look")]
    [SerializeField] private bool enableBodyLook = true;
    [SerializeField] private AnimationCurve bodyLookXCurve = new AnimationCurve(
        new Keyframe(-1f, -1f),
        new Keyframe(0f, 0f, -0.0236f, -0.0236f),
        new Keyframe(1f, 1f));

    [Header("Lean")]
    [SerializeField] private bool enableLean = true;
    [Tooltip("Lean multiplier, evaluated at current speed / sprint speed.")]
    [SerializeField] private AnimationCurve leanCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.0958f, 0.3f, 1.1026f, 1.1026f),
        new Keyframe(0.5f, 0.65f, 0.7201f, 0.7201f),
        new Keyframe(1f, 1f, 0.6934f, 0.6934f));

    [Header("Input")]
    [SerializeField] private float buttonHoldThreshold = 0.15f;

    private GaitState currentGait;
    private float strafeDirectionX;
    private float strafeDirectionZ = 1f;
    private float forwardStrafe = 1f;
    private float movementInputDuration;
    private bool movementInputTapped;
    private bool movementInputPressed;
    private bool movementInputHeld;
    private float leanValue;
    private float headLookX;
    private float headLookY;
    private float bodyLookX;
    private float bodyLookY;
    private Vector3 previousForward;
    private bool isStarting;
    private float locomotionStartDirection;
    private float locomotionStartTimer;
    private float lookDelay;

    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int CurrentGaitHash = Animator.StringToHash("CurrentGait");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int FallingDurationHash = Animator.StringToHash("FallingDuration");
    private static readonly int InclineAngleHash = Animator.StringToHash("InclineAngle");
    private static readonly int ShuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
    private static readonly int ShuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");
    private static readonly int IsStartingHash = Animator.StringToHash("IsStarting");
    private static readonly int LocomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");
    private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsStoppedHash = Animator.StringToHash("IsStopped");
    private static readonly int IsStrafingHash = Animator.StringToHash("IsStrafing");
    private static readonly int StrafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
    private static readonly int StrafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");
    private static readonly int ForwardStrafeHash = Animator.StringToHash("ForwardStrafe");
    private static readonly int IsTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");
    private static readonly int CameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
    private static readonly int MovementInputTappedHash = Animator.StringToHash("MovementInputTapped");
    private static readonly int MovementInputPressedHash = Animator.StringToHash("MovementInputPressed");
    private static readonly int MovementInputHeldHash = Animator.StringToHash("MovementInputHeld");
    private static readonly int LeanValueHash = Animator.StringToHash("LeanValue");
    private static readonly int HeadLookXHash = Animator.StringToHash("HeadLookX");
    private static readonly int HeadLookYHash = Animator.StringToHash("HeadLookY");
    private static readonly int BodyLookXHash = Animator.StringToHash("BodyLookX");
    private static readonly int BodyLookYHash = Animator.StringToHash("BodyLookY");

    // Clears start/look/lean/input values when freezing for Build mode. Expects speed2D to be zeroed first.
    private void ResetAnimationState()
    {
        isStarting = false;
        locomotionStartTimer = 0f;
        leanValue = 0f;
        headLookX = 0f;
        bodyLookX = 0f;
        UpdateMovementInputState(false);
        UpdateGait();
    }

    // Start-moving animations (as in the Synty sample). Only when not strafing, i.e. sprinting from
    // (near) standstill: the animator plays a start clip chosen by the angle between where the
    // character faces and where the input points. Held for at least StartDelay so the clip can commit.
    private void UpdateStarting(bool hasInput, Vector3 characterForward, Vector3 inputDirection)
    {
        locomotionStartTimer = Mathf.Max(0f, locomotionStartTimer - Time.deltaTime);

        if (locomotionStartTimer > 0f)
        {
            isStarting = true;
            return;
        }

        bool startingNow = hasInput && speed2D < 1f && !isStrafing && IsGrounded && !IsJumping;

        if (startingNow)
        {
            if (!isStarting)
            {
                locomotionStartDirection = Vector3.SignedAngle(characterForward, inputDirection, Vector3.up);
            }

            locomotionStartTimer = StartDelay;
            lookDelay = StartDelay;
        }

        isStarting = startingNow;
    }

    // Additive head/body look and lean, driven by how fast the character is turning (as in the Synty sample).
    // Crouching disables lean and body look; jumping disables lean; turning in place disables both;
    // starting to move pauses all three briefly.
    private void UpdateRotationalAdditives()
    {
        lookDelay = Mathf.Max(0f, lookDelay - Time.deltaTime);
        bool startPause = isStarting || lookDelay > 0f;

        bool leanActive = enableLean && !startPause && !isCrouching && !IsJumping && !isTurningInPlace;
        bool bodyActive = enableBodyLook && !startPause && !isCrouching && !isTurningInPlace;
        bool headActive = enableHeadLook && !startPause;

        Vector3 currentForward = transform.forward;
        float rotationRate = Time.deltaTime > 0f
            ? -Vector3.SignedAngle(currentForward, previousForward, Vector3.up) / Time.deltaTime
            : 0f;
        previousForward = currentForward;

        // Lean: rotation rate scaled by a speed-based multiplier.
        float leanTarget = Mathf.Clamp(leanActive ? rotationRate / MaxLookRotationRate : 0f, -1f, 1f);
        leanTarget *= leanCurve.Evaluate(speed2D / sprintSpeed);
        leanValue = Mathf.Lerp(leanValue, leanTarget, LookSmoothness * Time.deltaTime);

        // Head: follows the camera offset while turning in place, otherwise the rotation rate.
        if (headActive && isTurningInPlace)
        {
            headLookX = Mathf.Lerp(headLookX, cameraRotationOffset / 200f, LookSmoothness * Time.deltaTime);
        }
        else
        {
            float headTarget = headLookXCurve.Evaluate(Mathf.Clamp(headActive ? rotationRate / MaxLookRotationRate : 0f, -1f, 1f));
            headLookX = Mathf.Lerp(headLookX, headTarget, LookSmoothness * Time.deltaTime);
        }

        float bodyTarget = bodyLookXCurve.Evaluate(Mathf.Clamp(bodyActive ? rotationRate / MaxLookRotationRate : 0f, -1f, 1f));
        bodyLookX = Mathf.Lerp(bodyLookX, bodyTarget, LookSmoothness * Time.deltaTime);

        // Vertical look follows camera pitch: looking down is positive, up to 1; looking up bottoms out at -0.1.
        float cameraTilt = 0f;
        if (cameraTransform != null)
        {
            cameraTilt = Mathf.Clamp(Mathf.DeltaAngle(0f, cameraTransform.eulerAngles.x) / -180f, -0.1f, 1f);
        }
        headLookY = cameraTilt;
        bodyLookY = cameraTilt;
    }

    private void UpdateMovementInputState(bool hasInput)
    {
        if (hasInput)
        {
            if (movementInputDuration == 0f)
            {
                movementInputTapped = true;
            }
            else if (movementInputDuration < buttonHoldThreshold)
            {
                movementInputTapped = false;
                movementInputPressed = true;
                movementInputHeld = false;
            }
            else
            {
                movementInputTapped = false;
                movementInputPressed = false;
                movementInputHeld = true;
            }

            movementInputDuration += Time.deltaTime;
        }
        else
        {
            movementInputDuration = 0f;
            movementInputTapped = false;
            movementInputPressed = false;
            movementInputHeld = false;
        }
    }

    private void UpdateStrafeValues(Vector3 moveDirection, bool hasInput)
    {
        float targetZ = 1f;
        float targetX = 0f;

        if (isStrafing && hasInput)
        {
            Vector3 characterForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 characterRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            Vector3 direction = moveDirection.normalized;

            targetZ = Vector3.Dot(characterForward, direction);
            targetX = Vector3.Dot(characterRight, direction);

            float strafeAngle = Vector3.SignedAngle(characterForward, direction, Vector3.up);
            float forwardTarget = strafeAngle > ForwardStrafeMinThreshold && strafeAngle < ForwardStrafeMaxThreshold ? 1f : 0f;
            forwardStrafe = Mathf.Abs(forwardStrafe - forwardTarget) <= 0.001f
                ? forwardTarget
                : Mathf.SmoothStep(forwardStrafe, forwardTarget, Mathf.Clamp01(StrafeDirectionDampTime * Time.deltaTime));
        }

        strafeDirectionZ = Mathf.Round(Mathf.Lerp(strafeDirectionZ, targetZ, AnimationDampTime * Time.deltaTime) * 1000f) / 1000f;
        strafeDirectionX = Mathf.Round(Mathf.Lerp(strafeDirectionX, targetX, AnimationDampTime * Time.deltaTime) * 1000f) / 1000f;
    }

    private void UpdateGait()
    {
        // Midpoints between the gait speeds decide which gait the animator plays.
        float runThreshold = (walkSpeed + runSpeed) * 0.5f;
        float sprintThreshold = (runSpeed + sprintSpeed) * 0.5f;

        if (speed2D < 0.01f)
        {
            currentGait = GaitState.Idle;
        }
        else if (speed2D < runThreshold)
        {
            currentGait = GaitState.Walk;
        }
        else if (speed2D < sprintThreshold)
        {
            currentGait = GaitState.Run;
        }
        else
        {
            currentGait = GaitState.Sprint;
        }
    }

    private void UpdateAnimator()
    {
        animator.SetFloat(MoveSpeedHash, speed2D);
        animator.SetInteger(CurrentGaitHash, (int)currentGait);
        animator.SetBool(IsWalkingHash, isWalking);
        animator.SetBool(IsCrouchingHash, isCrouching);
        animator.SetBool(IsStoppedHash, isStopped);
        animator.SetFloat(IsStrafingHash, isStrafing ? 1f : 0f);

        animator.SetFloat(StrafeDirectionXHash, strafeDirectionX);
        animator.SetFloat(StrafeDirectionZHash, strafeDirectionZ);
        animator.SetFloat(ForwardStrafeHash, forwardStrafe);
        animator.SetBool(IsTurningInPlaceHash, isTurningInPlace);
        animator.SetFloat(CameraRotationOffsetHash, cameraRotationOffset);

        animator.SetBool(MovementInputTappedHash, movementInputTapped);
        animator.SetBool(MovementInputPressedHash, movementInputPressed);
        animator.SetBool(MovementInputHeldHash, movementInputHeld);

        animator.SetFloat(LeanValueHash, leanValue);
        animator.SetFloat(HeadLookXHash, headLookX);
        animator.SetFloat(HeadLookYHash, headLookY);
        animator.SetFloat(BodyLookXHash, bodyLookX);
        animator.SetFloat(BodyLookYHash, bodyLookY);

        animator.SetBool(IsGroundedHash, IsGrounded);
        animator.SetFloat(FallingDurationHash, fallingDuration);
        animator.SetFloat(InclineAngleHash, inclineAngle);

        animator.SetFloat(ShuffleDirectionXHash, shuffleDirectionX);
        animator.SetFloat(ShuffleDirectionZHash, shuffleDirectionZ);
        animator.SetBool(IsStartingHash, isStarting);
        animator.SetFloat(LocomotionStartDirectionHash, locomotionStartDirection);
        animator.SetBool(IsJumpingHash, IsJumping);
    }
}
