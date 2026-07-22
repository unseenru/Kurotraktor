using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Configs/GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    public CameraSettings Camera = new CameraSettings();
    public PlayerSettings Player = new PlayerSettings();
}

[Serializable]
public class CameraSettings
{
    public float Sensitivity = 2f;
    public float Distance = 3f;
    public Vector3 TargetOffset = new Vector3(0, 1.5f, 0);
    public Vector2 VerticalLimits = new Vector2(-45f, 45f);
}

[Serializable]
public class PlayerSettings
{
    public float Speed = 10f;
    public float Gravity = -9.81f;
}