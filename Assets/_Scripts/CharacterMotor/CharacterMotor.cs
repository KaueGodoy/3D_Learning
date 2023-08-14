using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] protected CharacterMotorConfig Config;
    [SerializeField] Transform LinkedCamera;

    [Header("Debugging")]
    [SerializeField] Logger logger;
    [SerializeField] protected bool Debug_OverrideMovement = false;
    [SerializeField] protected Vector2 Debug_MovementInput;

    public UnityEvent<bool> OnRunChanged = new UnityEvent<bool>();
    public UnityEvent OnHitGround = new UnityEvent();

    protected Rigidbody LinkedRB;
    protected Collider LinkedCollider;

    protected float CurrentCameraPitch = 0f;

    public bool IsJumping { get; private set; } = false;
    public float JumpCount { get; private set; } = 0f;
    protected float JumpTimeRemaining = 0f;

    public bool IsRunning { get; protected set; } = false;
    public bool IsGrounded { get; protected set; }
    public bool SendUIInteraction { get; set; } = true;
    public float CurrentMaxSpeed
    {
        get
        {
            if (IsGrounded)
                return IsRunning ? Config.RunSpeed : Config.WalkSpeed;
            return Config.CanAirControl ? Config.AirControlMaxSpeed : 0f;
        }
    }

    #region Input System Handling

    protected Vector2 _Input_Move;
    protected void OnMove(InputValue value)
    {
        _Input_Move = value.Get<Vector2>();
        //Debug.Log($"Move {value}");
    }

    protected Vector2 _Input_Look;
    protected void OnLook(InputValue value)
    {
        _Input_Look = value.Get<Vector2>();
    }

    protected bool _Input_Jump;
    protected void OnJump(InputValue value)
    {
        _Input_Jump = value.isPressed;
    }

    protected bool _Input_Run;
    protected void OnRun(InputValue value)
    {
        _Input_Run = value.isPressed;
    }

    protected bool _Input_PrimaryAction;
    protected void OnPrimaryAction(InputValue value)
    {
        _Input_PrimaryAction = value.isPressed;


        // need to inject pointer event
        if (_Input_PrimaryAction && SendUIInteraction)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = Mouse.current.position.ReadValue();

            // raycast against the UI
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (RaycastResult result in results)
            {
                if (result.distance < Config.MaxInteractionDistance)
                {
                    ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                }
            }
        }
    }

    protected bool _Input_SecondaryAction;
    protected void OnSecondaryAction(InputValue value)
    {
        _Input_SecondaryAction = value.isPressed;
    }

    #endregion

    private void Awake()
    {
        LinkedRB = GetComponent<Rigidbody>();
        LinkedCollider = GetComponent<Collider>();
        SendUIInteraction = Config.SendUIInteraction;
    }

    void Start()
    {
        SetCursorLock(true);

        LinkedCollider.material = Config.Material_Default;
    }

    void Update()
    {

    }

    protected void FixedUpdate()
    {
        RaycastHit groundCheckResult = UpdateIsGrounded();

        bool wasRunning = IsRunning;
        bool wasGrounded = IsGrounded;
        UpdateRunning(groundCheckResult);

        if (wasRunning != IsRunning)
            OnRunChanged.Invoke(IsRunning);

        if (wasGrounded != IsGrounded)
            LinkedCollider.material = Config.Material_Default;

        //Debug.Log(IsRunning);
        UpdateMovement(groundCheckResult);
    }

    protected void LateUpdate()
    {
        UpdateCamera();
    }

    protected RaycastHit UpdateIsGrounded()
    {
        RaycastHit hitResult;

        if (JumpTimeRemaining > 0)
        {
            IsGrounded = false;
            return new RaycastHit();
        }

        Vector3 startPos = LinkedRB.position + Vector3.up * Config.Height * 0.5f;
        float groundCheckRadius = Config.Radius + Config.GroundedCheckRadiusBuffer;
        float groundCheckDistance = (Config.Height * 0.5f) - Config.Radius + Config.GroundedCheckBuffer;

        // perform spherecast
        if (Physics.SphereCast(startPos, groundCheckRadius, Vector3.down, out hitResult, groundCheckDistance,
                            Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (!IsGrounded)
                OnHitGround.Invoke();

            IsGrounded = true;
            JumpCount = 0f;
            JumpTimeRemaining = 0f;
            // add auto parenting (moving platform)
        }
        else
            IsGrounded = false;

        logger.Log("Grounded: " + IsGrounded, this);

        return hitResult;
    }

    protected void UpdateMovement(RaycastHit groundCheckResult)
    {
        if (Debug_OverrideMovement)
            _Input_Move = Debug_MovementInput;

        // stop running if no input
        if (_Input_Move.magnitude < float.Epsilon)
            IsRunning = false;

        Vector3 movementVector = transform.forward * _Input_Move.y + transform.right * _Input_Move.x;
        movementVector *= CurrentMaxSpeed;

        if (IsGrounded)
        {
            // project onto current surface 
            movementVector = Vector3.ProjectOnPlane(movementVector, groundCheckResult.normal);

            // trying to move up too steep a slope
            if (movementVector.y > 0 && Vector3.Angle(Vector3.up, groundCheckResult.normal) > Config.SlopeLimit)
                movementVector = Vector3.zero;
        }
        else
        {
            movementVector += Vector3.down * Config.FallVelocity;
        }

        UpdateJumping(ref movementVector);

        if (IsGrounded && !IsJumping)
            CheckForStepUp(ref movementVector);

        LinkedRB.velocity = Vector3.MoveTowards(LinkedRB.velocity, movementVector, Config.Acceleration);
    }

    protected void CheckForStepUp(ref Vector3 movementVector)
    {
        Vector3 lookAheadStartPoint = LinkedRB.position + Vector3.up * (Config.StepCheck_MaxStepHeight * 0.5f);
        Vector3 lookAheadDirection = movementVector.normalized;
        float lookAheadDistance = Config.Radius + Config.StepCheck_LookAheadRange;

        // check if there is potential step ahead
        if (Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance,
                            Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
        {
            lookAheadStartPoint = LinkedRB.position + Vector3.up * Config.StepCheck_MaxStepHeight;

            // check if there is clear space above the step
            if (!Physics.Raycast(lookAheadStartPoint, lookAheadDirection, lookAheadDistance,
                            Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 candidatePoint = lookAheadStartPoint + lookAheadDirection * lookAheadDistance;

                // check the surface of the step
                RaycastHit hitResult;

                if (Physics.Raycast(candidatePoint, Vector3.down, out hitResult, Config.StepCheck_MaxStepHeight * 2f,
                                    Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    // is the step shallow enough in slope
                    if (Vector3.Angle(Vector3.up, hitResult.normal) <= Config.SlopeLimit)
                    {
                        LinkedRB.position = hitResult.point;
                    }
                }
            }
        }
    }

    protected void UpdateJumping(ref Vector3 movementVector)
    {
        bool triggeredJumpThisFrame = false;
        // input
        if (_Input_Jump)
        {
            _Input_Jump = false;

            // check if can jump
            bool triggerJump = true;
            int numJumpsPermitted = Config.CanDoubleJump ? 2 : 1;
            if (JumpCount >= numJumpsPermitted)
                triggerJump = false;
            if (!IsGrounded && !IsJumping)
                triggerJump = false;

            // trigger jump
            if (triggerJump)
            {
                if (JumpCount == 0)
                    triggeredJumpThisFrame = true;

                LinkedCollider.material = Config.Material_Jumping;
                JumpTimeRemaining += Config.JumpTime;
                IsJumping = true;
                ++JumpCount;
            }
        }

        // jump
        if (IsJumping)
        {
            logger.Log("Jumping", this);
            if (!triggeredJumpThisFrame)
                JumpTimeRemaining -= Time.deltaTime;

            if (JumpTimeRemaining <= 0)
                IsJumping = false;
            else
            {
                Vector3 startPos = LinkedRB.position + Vector3.up * Config.Height * 0.5f;
                float ceilingCheckRadius = Config.Radius + Config.CeilingCheckRadiusBuffer;
                float ceilingCheckDistance = (Config.Height * 0.5f) - Config.Radius + Config.GroundedCheckBuffer;

                // perform spherecast
                RaycastHit ceilingHitResult;
                if (Physics.SphereCast(startPos, ceilingCheckRadius, Vector3.up, out ceilingHitResult,
                                      ceilingCheckDistance, Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
                {
                    IsJumping = false;
                    JumpTimeRemaining = 0;
                    movementVector.y = 0f;
                }
                else
                {
                    movementVector.y = Config.JumpVelocity;
                }


            }
        }
    }


    protected void UpdateRunning(RaycastHit groundCheckResult)
    {
        if (!IsGrounded)
        {
            IsRunning = false;
            return;
        }

        if (!Config.CanRun)
        {
            IsRunning = false;
            return;
        }

        // setup run toggle
        if (Config.IsRunToogle)
        {
            if (_Input_Run && !IsRunning)
                IsRunning = true;
        }
        else
            IsRunning = _Input_Run;
    }

    protected void UpdateCamera()
    {
        // calculate camera input
        float cameraYawDelta = _Input_Look.x * Config.Camera_HorizontalSensitivity * Time.deltaTime;
        float cameraPitchDelta = _Input_Look.y * Config.Camera_VerticalSensitivity * Time.deltaTime *
                                 (Config.Camera_InvertY ? 1f : -1f);

        // rotate the character
        transform.localRotation = transform.localRotation * Quaternion.Euler(0f, cameraYawDelta, 0f);

        // tilt the camera
        CurrentCameraPitch = Mathf.Clamp(CurrentCameraPitch + cameraPitchDelta,
                                         Config.Camera_MinPitch,
                                         Config.Camera_MaxPitch);

        LinkedCamera.transform.localRotation = Quaternion.Euler(CurrentCameraPitch, 0f, 0f);
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
