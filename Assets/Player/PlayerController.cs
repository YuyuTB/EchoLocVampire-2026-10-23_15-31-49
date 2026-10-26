using System.Collections;
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
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSwitchSpeed = 35f;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3f;
    public bool hasExitKey = false;
    public bool isInCutscene = false;
    public bool isGameOver = false; // Sécurité pour empêcher le joueur de recapturer la souris en Game Over

    [Header("Component Declarations")]
    private CharacterController _controller;
    private GameObject _camera;
    private AudioSource _audioSource;

    [Header("Input Action Declarations")]
    private InputAction _moveAction;
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

        // Bloque le joueur et désactive ses inputs pendant 1 seconde
        StartCoroutine(InitializationDelayRoutine());
    }

    private IEnumerator InitializationDelayRoutine()
    {
        isInCutscene = true;

        InputActions.FindActionMap("Player").Disable();
        yield return new WaitForSeconds(0.5f);
        InputActions.FindActionMap("Player").Enable();
        isInCutscene = false;
    }

    private void Update()
    {
        if (isGameOver) return; // Stoppe le contrôle si c'est Game Over

        StopMoveSoundInCutscene();
        if (isInCutscene) return;

        HandleInput();
        HandleInteract();
        HandleLookVertical();
        HandleLookHorizontal();
        HandleCursorDisplay();
    }

    private void FixedUpdate()
    {
        if (isGameOver || isInCutscene) return;

        HandleCrouch();
        HandleMoveSpeed();
        HandleMove();
        HandleGravity();
    }

    private void StopMoveSoundInCutscene()
    {
        if (isInCutscene && _audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    private void InitializeComponents()
    {
        _controller = GetComponent<CharacterController>();
        _camera = GameObject.FindGameObjectWithTag("MainCamera");
        _standingHeight = _controller.height;
        _audioSource = GetComponent<AudioSource>();
    }

    private void InitializeInputActions()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _lookAction = InputSystem.actions.FindAction("Look");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _crouchAction = InputSystem.actions.FindAction("Crouch");
        _attackAction = InputSystem.actions.FindAction("Attack");
        _interactAction = InputSystem.actions.FindAction("Interact");
        _menuAction = InputSystem.actions.FindAction("Menu");
    }

    private void HandlePlayFootstepSound()
    {
        if (_sprintAction.IsPressed() && _moveAmount.magnitude > 0.1f)
        {
            _audioSource.pitch = sprintSpeedMultiplier;
            _audioSource.volume = 0.75f;
        }
        else if (_crouchAction.IsPressed() && _moveAmount.magnitude > 0.1f)
        {
            _audioSource.pitch = crouchSpeedMultiplier;
            _audioSource.volume = 0.25f;
        }
        else
        {
            _audioSource.pitch = 1f;
            _audioSource.volume = 0.5f;
        }

        if (_moveAmount.magnitude > 0.1f)
        {
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
        else
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }
    }

    private void HandleInteract()
    {
        if (_interactAction.WasPressedThisFrame())
        {
            RaycastHit hit;
            if (Physics.Raycast(_camera.transform.position, _camera.transform.forward, out hit, interactionDistance))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this);
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
        if (isGameOver) return; // Empêche de reverrouiller la souris si on clique sur l'UI

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
        HandlePlayFootstepSound();
    }

    private void HandleLookVertical()
    {
        float lookY = _lookAmount.y * lookSensitivity;

        _verticalRotation -= lookY;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -70f, 70f);
        _camera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleLookHorizontal()
    {
        float lookX = _lookAmount.x * lookSensitivity;

        transform.Rotate(Vector3.up * lookX);
    }

    private void HandleGravity()
    {
        if (_controller.isGrounded)
        {
            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
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