using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IPlayerMovement
{
    [SerializeField] private float _speed = 10f;
    [SerializeField] private float _gravity = -9.81f;

    private CharacterController _characterController;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public void Move(Vector2 direction, float rotation = 0f)
    {
        // 1. Поворот вокруг своей оси
        if (Mathf.Abs(rotation) > 0.001f)
        {
            transform.Rotate(0, rotation, 0);
        }

        // 2. Расчет направления относительно поворота
        Vector3 moveDirection = new Vector3(direction.x, 0f, direction.y);
        moveDirection = transform.TransformDirection(moveDirection) * _speed;

        // 3. Расчет вертикальной скорости (гравитации)
        if (_characterController.isGrounded)
        {
            _verticalVelocity = -1f;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        moveDirection.y = _verticalVelocity;

        // 4. Применение перемещения
        _characterController.Move(moveDirection * Time.deltaTime);
    }
}