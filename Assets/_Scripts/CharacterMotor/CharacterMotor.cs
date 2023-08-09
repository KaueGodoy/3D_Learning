using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CharacterMotor : MonoBehaviour
{
    [SerializeField] protected CharacterMotorConfig Config;
    [SerializeField] Transform LinkedCamera;

    protected Rigidbody LinkedRB;

    protected float CurrentCameraPitch = 0f;

    public bool IsRunning { get; protected set; } = false;
    public bool IsGrounded { get; protected set; }
    public float CurrentMaxSpeed => IsRunning ? Config.RunSpeed : Config.WalkSpeed;

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
    }

    void Start()
    {
        SetCursorLock(true);
    }

    void Update()
    {

    }

    protected void FixedUpdate()
    {
        RaycastHit groundCheckResult = UpdateIsGrounded();
        UpdateRunning(groundCheckResult);
        Debug.Log(IsRunning);
        UpdateMovement(groundCheckResult);
    }

    protected void LateUpdate()
    {
        UpdateCamera();
    }

    protected RaycastHit UpdateIsGrounded()
    {
        Vector3 startPos = LinkedRB.position + Vector3.up * Config.Height * 0.5f;
        float groundCheckDistance = (Config.Height * 0.5f) + Config.GroundedCheckBuffer;

        // perform spherecast
        RaycastHit hitResult;
        if (Physics.SphereCast(startPos, Config.Radius + Config.GroundedCheckRadiusBuffer, Vector3.down, out hitResult,
                              groundCheckDistance, Config.GroundedLayerMask, QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            // add auto parenting (moving platform)
        }
        else
            IsGrounded = false;

        return hitResult;
    }

    protected void UpdateMovement(RaycastHit groundCheckResult)
    {
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
            Debug.Log("Not grounded");

        LinkedRB.velocity = movementVector;
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
