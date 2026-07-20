using UnityEngine;


[RequireComponent (typeof(CharacterController))]
public class Player:MonoBehaviour
{
    [SerializeField] private Vector2 _cameraSencivity = new Vector2(10,10);
    [SerializeField] private Vector2 _cameraVerticalBorder = new Vector2(-60, 60);
    [SerializeField] private Transform _cameraTarget;
    [SerializeField] private float _cameraDistance = 4f;
    [SerializeField] private float _playerSpeed=10f;
    [SerializeField] private float _jumpForce = 5f;

    private Camera _camera;
    private CharacterController _characterController;

    private float _currentDistance = 0.01f;
    private float _xRotation, _yRotation;
    private float _verticalVelocity;
    
    private bool _isFar;
    

    private void Awake()
    {
        _camera = Camera.main;
        _characterController = GetComponent<CharacterController>();
    }
    private void Update()
    {
        if (!Input.GetMouseButton(0))
        { 
            CameraControl();
        }
            
        PlayerControl();
    }
    private void CameraControl()
    {
        float vertical = Input.GetAxis("Mouse X") * _cameraSencivity.x;
        float horizontal = Input.GetAxis("Mouse Y") * _cameraSencivity.y;

        _xRotation -= horizontal;
        _xRotation = Mathf.Clamp(_xRotation, _cameraVerticalBorder.x, _cameraVerticalBorder.y);

        transform.Rotate(0, vertical, 0);
        _yRotation = transform.eulerAngles.y;

        if (Input.GetKeyDown(KeyCode.V))
        {
            _isFar = !_isFar;
        }

        float targetDistance = _isFar ? _cameraDistance : 0.01f;
        _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * 5f);

        Quaternion cameraRotation = Quaternion.Euler(_xRotation, _yRotation, 0f);
        Vector3 orbitPosition = _cameraTarget.position + cameraRotation * new Vector3(0, 0, -_currentDistance);

        _camera.transform.position = orbitPosition;
        _camera.transform.rotation = cameraRotation;
    }
    private void PlayerControl()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);
        moveDirection = transform.TransformDirection(moveDirection);
        moveDirection *= _playerSpeed;

        if (_characterController.isGrounded)
        {
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = _jumpForce;
            }
        }
        else
        {
            moveDirection.y = _verticalVelocity;
        }

        moveDirection.y += Physics.gravity.y * Time.deltaTime;
        _verticalVelocity = moveDirection.y;

        _characterController.Move(moveDirection * Time.deltaTime);
    }
}
