using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "CharacterMotor/Config", fileName = "CharacterMotorConfig")]
public class CharacterMotorConfig : ScriptableObject
{
    [Header("Character")]
    public float Height = 1.8f;
    public float Radius = 0.3f;

    [Header("Grounded Check")]
    public LayerMask GroundedLayerMask = ~0;
    public float GroundedCheckBuffer = 0.1f;
    public float GroundedCheckRadiusBuffer = 0.05f;

    [Header("Ceiling Check")]
    public LayerMask CeilingCheckLayerMask = ~0;
    public float CeilingCheckRangeBuffer = 0.1f;
    public float CeilingCheckRadiusBuffer = 0.05f;

    [Header("Step Up Check")]
    public float StepCheck_LookAheadRange = 0.1f;
    public float StepCheck_MaxStepHeight = 0.6f;

    [Header("Physics Material")]
    public PhysicMaterial Material_Default;
    public PhysicMaterial Material_Jumping;

    [Header("Camera")]
    public bool Camera_InvertY = false;
    public float Camera_HorizontalSensitivity = 10f;
    public float Camera_VerticalSensitivity = 10f;
    public float Camera_MinPitch = -75f;
    public float Camera_MaxPitch = 75f;

    [Header("Movement")]
    public float WalkSpeed = 10f;
    public float RunSpeed = 15f;
    public bool CanRun = true;
    public bool IsRunToogle = true;
    public float SlopeLimit = 60f;
    public float Acceleration = 1f;

    [Header("Falling")]
    public float FallVelocity = 1f;

    [Header("Air Control")]
    public bool CanAirControl = true;
    public float AirControlMaxSpeed = 2.5f;

    [Header("Jumping")]
    public bool CanJump = true;
    public bool CanDoubleJump = true;
    public float JumpVelocity = 15f;
    public float JumpTime = 0.1f;

    [Header("User Interface")]
    public bool SendUIInteraction = true;
    public float MaxInteractionDistance = 2f;

}
