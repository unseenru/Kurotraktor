using UnityEngine;

public interface IMovement
{
    void Move(Vector2 direction, float rotation = 0f);
}