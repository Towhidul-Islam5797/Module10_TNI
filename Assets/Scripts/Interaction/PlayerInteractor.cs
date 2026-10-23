using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    private const string DropPromptText = "Press E to drop";

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform holdPoint;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private TMP_Text promptText;

    private InputAction interactAction;
    private IInteractable currentInteractable;
    private PickupBox heldBox;

    private void Awake()
    {
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    private void Update()
    {
        FindInteractable();
        HandleInteractInput();
        UpdatePrompt();
    }

    private void FindInteractable()
    {
        currentInteractable = null;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, interactDistance);

        Debug.DrawRay(ray.origin, ray.direction * interactDistance, hasHit ? Color.green : Color.red);

        if (hasHit)
        {
            currentInteractable = hit.collider.GetComponent<IInteractable>();
        }
    }

    private void HandleInteractInput()
    {
        if (!interactAction.WasPressedThisFrame())
        {
            return;
        }

        if (heldBox != null)
        {
            heldBox.Drop();
            heldBox = null;
            return;
        }

        currentInteractable?.Interact(this);
    }

    private void UpdatePrompt()
    {
        if (promptText == null)
        {
            return;
        }

        if (heldBox != null)
        {
            promptText.text = DropPromptText;
        }
        else if (currentInteractable != null)
        {
            promptText.text = currentInteractable.PromptText;
        }
        else
        {
            promptText.text = string.Empty;
        }
    }

    public void PickUp(PickupBox box)
    {
        heldBox = box;
        heldBox.AttachTo(holdPoint);
    }
}
