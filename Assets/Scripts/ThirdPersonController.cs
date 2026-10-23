using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    private const float GroundedStickForce = -2f;
    private const float AnimationDampTime = 0.1f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    [Header("Movement")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Jump and Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private CharacterController characterController;
    private Animator animator;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private float verticalVelocity;
    private float cameraYaw;
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
        cameraYaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        Vector3 moveDirection = GetCameraRelativeDirection();

        HandleRotation(moveDirection);
        HandleLook();
        HandleGravityAndJump();
        HandleMovement(moveDirection);
        HandleAnimation(moveDirection);
    }

    private Vector3 GetCameraRelativeDirection()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 cameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        return cameraRight * moveInput.x + cameraForward * moveInput.y;
    }

    private void HandleRotation(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void HandleLook()
    {
        Vector2 lookInput = lookAction.ReadValue<Vector2>() * mouseSensitivity;

        cameraYaw += lookInput.x;
        cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, minPitch, maxPitch);

        cameraTarget.rotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
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
