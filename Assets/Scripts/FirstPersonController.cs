using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    private const float GroundedStickForce = -2f;
    private const float AnimationDampTime = 0.1f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump and Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraRoot;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float maxLookAngle = 80f;

    private CharacterController characterController;
    private Animator animator;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private float verticalVelocity;
    private float cameraPitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();

        Vector3 moveDirection = GetMoveDirection();

        HandleGravityAndJump();
        HandleMovement(moveDirection);
        HandleAnimation(moveDirection);
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        transform.Rotate(Vector3.up, lookInput.x);

        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -maxLookAngle, maxLookAngle);
        cameraRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    private void HandleGravityAndJump()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = GroundedStickForce;
        }

        if (isGrounded && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private Vector3 GetMoveDirection()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        return transform.right * moveInput.x + transform.forward * moveInput.y;
    }

    private void HandleMovement(Vector3 moveDirection)
    {
        Vector3 velocity = moveDirection * moveSpeed + Vector3.up * verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleAnimation(Vector3 moveDirection)
    {
        float horizontalSpeed = moveDirection.magnitude * moveSpeed;

        animator.SetFloat(SpeedHash, horizontalSpeed, AnimationDampTime, Time.deltaTime);
        animator.SetBool(IsGroundedHash, characterController.isGrounded);
    }
}
