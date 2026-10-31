using UnityEngine;

// An item lying in the world that the player can pick up with E.
// It uses the interaction system we already have: PlayerInteractor looks at
// it with a raycast, shows our PromptText, and calls Interact when E is pressed.
// The pickup needs a Collider, otherwise the raycast cannot hit it.
[RequireComponent(typeof(Collider))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    // Which item this pickup gives the player (for example the Coin asset).
    [Header("Item")]
    [SerializeField] private ItemData itemData;

    // Lets other scripts (and the validation tool) see which item this is.
    public ItemData ItemData
    {
        get { return itemData; }
    }

    // The text PlayerInteractor shows on screen while the player looks at this item.
    public string PromptText
    {
        get { return "Press E to pick up " + itemData.ItemName; }
    }

    // Called by PlayerInteractor when the player presses E while looking at us.
    // The Inventory lives on the same Player GameObject as the PlayerInteractor,
    // so we can find it with GetComponent.
    public void Interact(PlayerInteractor interactor)
    {
        Inventory inventory = interactor.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogWarning("The player has no Inventory component.");
            return;
        }

        bool wasAdded = inventory.AddItem(itemData);

        // Only remove the item from the world if it really went into the inventory.
        // If the inventory is full, the item stays here so the player can come back for it.
        if (wasAdded)
        {
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventory full");
        }
    }
}
