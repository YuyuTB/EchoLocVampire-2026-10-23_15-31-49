using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset InputActions;

    [Header("Editable Movement Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float sprintSpeedMultiplier = 2f;
    [SerializeField] private float jumpSpeed = 5f;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSwitchSpeed = 35f;
    [SerializeField] private float crouchJumpMultiplier = 0.5f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("Component Declarations")]
    private CharacterController _controller;
    private GameObject _camera;

    [Header("Input Action Declarations")]
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _lookAction;
    private InputAction _sprintAction;
    private InputAction _crouchAction;
    private InputAction _attackAction;
    private InputAction _menuAction;
    private InputAction _interactAction;

    [Header("Movement Declarations")]
    private Vector2 _moveAmount;
    private Vector2 _lookAmount;
    private float _verticalRotation = 0f;
    private float _verticalVelocity;
    private float _moveSpeed;
    private float _standingHeight;

    private void OnEnable()
    {
        InputActions.FindActionMap("Player").Enable();
    }

    private void OnDisable()
    {
        InputActions.FindActionMap("Player").Disable();
    }

    private void Awake()
    {
        InitializeInputActions();
    }

    private void Start()
    {
        InitializeComponents();

        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Inputs, mouse look, and instant triggers (like interaction) 
        // run in Update for perfect responsiveness.
        HandleInput();
        HandleInteract();
        HandleLookVertical();
        HandleLookHorizontal();
        HandleCursorDisplay();
    }

    private void FixedUpdate()
    {
        // Physics, movement, and crouching stay in FixedUpdate
        HandleCrouch();
        HandleMoveSpeed();
        HandleMove();
        HandleJumpAndGravity();
    }

    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _standingHeight = _controller.height;
    }

    private void InitializeInputActions()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _lookAction = InputSystem.actions.FindAction("Look");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _crouchAction = InputSystem.actions.FindAction("Crouch");
        _attackAction = InputSystem.actions.FindAction("Attack");
        _menuAction = InputSystem.actions.FindAction("Menu");
        _interactAction = InputSystem.actions.FindAction("Interact");
    }

    private void HandleInteract()
    {
        if (_interactAction.WasPressedThisFrame())
        {
            RaycastHit hit;
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, interactionDistance))
            {
                // Modular approach: Check if the hit object implements IInteractable
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }

    private void HandleInput()
    {
        _moveAmount = _moveAction.ReadValue<Vector2>();
        _lookAmount = _lookAction.ReadValue<Vector2>();
    }

    private void HandleCursorDisplay()
    {
        if (_attackAction.WasPressedThisFrame())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (_menuAction.WasPressedThisFrame())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleCrouch()
    {
        float crouchSpeedOverTime = crouchSwitchSpeed * Time.fixedDeltaTime;
        if (_crouchAction.IsPressed() && _controller.height > crouchHeight)
        {
            ApplyHeightLerpForCrouch(crouchHeight, crouchSpeedOverTime);
        }
        else if (!_crouchAction.IsPressed() && _controller.height < _standingHeight)
        {
            ApplyHeightLerpForCrouch(_standingHeight, crouchSpeedOverTime);
        }
    }

    private void HandleMoveSpeed()
    {
        if (!_sprintAction.IsPressed() && !_crouchAction.IsPressed())
        {
            SetSpeedWithMultiplier(1f);
            return;
        }

        if (_sprintAction.IsPressed() && !_crouchAction.IsPressed())
        {
            SetSpeedWithMultiplier(sprintSpeedMultiplier);
            return;
        }

        if (_crouchAction.IsPressed())
        {
            SetSpeedWithMultiplier(crouchSpeedMultiplier);
            return;
        }
    }

    private void HandleMove()
    {
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        Vector3 moveDirection = (forward * _moveAmount.y) + (right * _moveAmount.x);
        Vector3 finalMove = (moveDirection * _moveSpeed) + new Vector3(0, _verticalVelocity, 0);

        _controller.Move(finalMove * Time.fixedDeltaTime);
    }

    private void HandleLookVertical()
    {
        float lookY = _lookAmount.y * lookSensitivity;

        _verticalRotation -= lookY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -70f, 30f);
        _camera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleLookHorizontal()
    {
        float lookX = _lookAmount.x * lookSensitivity;

        transform.Rotate(Vector3.up * lookX);
    }

    /********************************************************
     * JUMP & GRAVITY
     ********************************************************/
    private void HandleJumpAndGravity()
    {
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            if (_jumpAction.WasPressedThisFrame())
            {
                _verticalVelocity = jumpSpeed * (_crouchAction.IsPressed() ? crouchJumpMultiplier : 1f);
            }
        }
        else
        {
            _verticalVelocity += gravity * Time.fixedDeltaTime;
        }
    }

    private void SetSpeedWithMultiplier(float multiplier)
    {
        _moveSpeed = speed * multiplier;
    }

    private void ApplyHeightLerpForCrouch(float height, float crouchSpeedOverTime)
    {
        _controller.height = Mathf.Lerp(_controller.height, height, crouchSpeedOverTime);
    }
}