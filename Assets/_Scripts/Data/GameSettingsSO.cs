using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Configs/GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    public CameraSettings Camera = new();
    public PlayerSettings Player = new();
    public ChickenSettings Chicken = new();
}

[Serializable]
public class CameraSettings
{
    public float Sensitivity = 2f;
    public float Distance = 3f;
    public Vector3 TargetOffset = new Vector3(0, 1.5f, 0);
    public Vector2 VerticalLimits = new Vector2(-45f, 45f);
    public KeyCode Key = KeyCode.V;
    public CameraMode Mode = CameraMode.ThirdPerson;
    public float MaxDistance = 3f;
    public float MinDistance = 0.01f;
}

[Serializable]
public class PlayerSettings
{
    public float Speed = 10f;
    public float Gravity = -9.81f;
}
[Serializable]
public class ChickenSettings
{
    [Header("Movement")]
    [Min(0f)] public float WalkSpeed = 1.5f;
    [Min(0f)] public float RunSpeed = 5f;
    [Min(0f)] public float TurnSpeed = 8f;
    public float Gravity = -9.81f;
    public float GroundedStickyVelocity = -2f;
    public float DirectionEpsilon = 0.0001f;

    [Header("Detection")]
    [Min(0f)] public float FleeDistance = 5f;
    [Min(0f)] public float CalmDistance = 7f;

    [Header("State Timers (Min, Max)")]
    public Vector2 EatingTimeRange = new(2f, 5f);
    public Vector2 StandingTimeRange = new(1.5f, 3f);
    public Vector2 WalkingTimeRange = new(3f, 6f);
}

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}