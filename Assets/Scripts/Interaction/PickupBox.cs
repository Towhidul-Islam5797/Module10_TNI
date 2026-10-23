using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PickupBox : MonoBehaviour, IInteractable
{
    private Rigidbody boxRigidbody;
    private Collider boxCollider;

    public string PromptText => "Press E to pick up";

    private void Awake()
    {
        boxRigidbody = GetComponent<Rigidbody>();
        boxCollider = GetComponent<Collider>();
    }

    public void Interact(PlayerInteractor interactor)
    {
        interactor.PickUp(this);
    }

    public void AttachTo(Transform holdPoint)
    {
        boxRigidbody.isKinematic = true;
        boxCollider.enabled = false;

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Drop()
    {
        transform.SetParent(null);

        boxCollider.enabled = true;
        boxRigidbody.isKinematic = false;
    }
}
