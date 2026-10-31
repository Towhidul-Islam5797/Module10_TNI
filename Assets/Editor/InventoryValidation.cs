using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

// Teacher tool: checks the Inventory Basics code and scene without entering Play mode.
// Every check prints a [PASS] or [FAIL] line to the Console.
// Run it from the menu Tools > Validate Inventory, or in batchmode with
// -executeMethod InventoryValidation.RunAll (Unity then exits with code 1 if anything failed).
public static class InventoryValidation
{
    private const string ScenePath = "Assets/Scenes/ThirdPerson.unity";
    private const string ItemsFolder = "Assets/Items";
    private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";

    private static int passCount;
    private static int failCount;

    [MenuItem("Tools/Validate Inventory")]
    private static void RunFromMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        RunChecks();
    }

    // Batchmode entry point: runs every check, then closes Unity with an exit code.
    public static void RunAll()
    {
        RunChecks();
        EditorApplication.Exit(failCount > 0 ? 1 : 0);
    }

    private static void RunChecks()
    {
        passCount = 0;
        failCount = 0;

        CheckInventoryLogic();
        CheckHealLogic();
        CheckItemUseLogic();
        CheckItemAssets();
        CheckInputActions();
        CheckScene();

        Debug.Log("[SUMMARY] Inventory validation: " + passCount + " passed, " + failCount + " failed");
    }

    private static void Check(bool condition, string description)
    {
        if (condition)
        {
            passCount++;
            Debug.Log("[PASS] " + description);
        }
        else
        {
            failCount++;
            Debug.LogError("[FAIL] " + description);
        }
    }

    // ---------------------------------------------------------------
    // Inventory logic on a temporary GameObject
    // ---------------------------------------------------------------

    private static void CheckInventoryLogic()
    {
        ItemData stackable = CreateTestItem("Stackable", 3, 0);
        ItemData single = CreateTestItem("Single", 1, 0);
        GameObject testObject = new GameObject("InventoryTest");

        try
        {
            Inventory inventory = testObject.AddComponent<Inventory>();
            // Awake does not run in Edit mode, so we call it ourselves to build the slots.
            testObject.SendMessage("Awake");

            Check(inventory.SlotCount == 6, "Inventory starts with 6 slots");
            Check(inventory.GetSlot(0).IsEmpty(), "Inventory slots start empty");

            inventory.AddItem(stackable);
            inventory.AddItem(stackable);
            inventory.AddItem(stackable);
            Check(inventory.GetSlot(0).count == 3 && inventory.GetSlot(1).IsEmpty(),
                "Stacking fills the existing stack first (3 in slot 1, slot 2 empty)");

            inventory.AddItem(stackable);
            Check(inventory.GetSlot(0).count == 3 && inventory.GetSlot(1).item == stackable && inventory.GetSlot(1).count == 1,
                "Max stack size is respected (4th item goes to slot 2)");

            bool addedAll = true;
            for (int i = 0; i < 4; i++)
            {
                addedAll = inventory.AddItem(single) && addedAll;
            }
            Check(addedAll && inventory.GetSlot(5).item == single && inventory.GetSlot(5).count == 1,
                "Non-stacking items each take their own slot");

            Check(!inventory.AddItem(single), "AddItem returns false when no slot has room");

            Check(inventory.AddItem(stackable) && inventory.GetSlot(1).count == 2,
                "A stack with room still accepts items when no slot is empty");

            inventory.AddItem(stackable);
            Check(!inventory.AddItem(stackable), "AddItem returns false when the inventory is completely full");

            Check(inventory.RemoveItem(1) && inventory.GetSlot(1).count == 2, "RemoveItem lowers the count by one");

            Check(inventory.RemoveItem(2) && inventory.GetSlot(2).IsEmpty() && inventory.GetSlot(2).item == null,
                "RemoveItem clears the slot when the count reaches zero");

            Check(!inventory.RemoveItem(2), "RemoveItem returns false for an empty slot");

            inventory.SelectSlot(3);
            Check(inventory.SelectedSlotIndex == 3 && inventory.GetSelectedSlot() == inventory.GetSlot(3), "SelectSlot selects a slot");

            inventory.SelectSlot(10);
            Check(inventory.SelectedSlotIndex == 3, "SelectSlot ignores numbers outside the inventory");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
            Object.DestroyImmediate(stackable);
            Object.DestroyImmediate(single);
        }
    }

    // ---------------------------------------------------------------
    // Health.Heal on a temporary GameObject
    // ---------------------------------------------------------------

    private static void CheckHealLogic()
    {
        GameObject testObject = new GameObject("HealTest");

        try
        {
            Health health = testObject.AddComponent<Health>();
            testObject.SendMessage("Awake");

            health.TakeDamage(50);
            health.Heal(25);
            Check(health.CurrentHealth == 75, "Heal adds health (50 + 25 = 75)");

            health.Heal(100);
            Check(health.CurrentHealth == health.MaxHealth, "Heal is clamped at MaxHealth");

            health.TakeDamage(1000);
            health.Heal(25);
            Check(health.IsDead && health.CurrentHealth == 0, "Heal does nothing when dead");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // ---------------------------------------------------------------
    // PlayerItemUser: using the selected item
    // ---------------------------------------------------------------

    private static void CheckItemUseLogic()
    {
        ItemData potion = CreateTestItem("Test Potion", 5, 25);
        ItemData coin = CreateTestItem("Test Coin", 99, 0);
        GameObject testObject = new GameObject("ItemUseTest");

        try
        {
            Health health = testObject.AddComponent<Health>();
            Inventory inventory = testObject.AddComponent<Inventory>();
            testObject.AddComponent<PlayerItemUser>();
            // Runs Awake on all three components, like starting the game.
            testObject.SendMessage("Awake");

            inventory.AddItem(potion);
            inventory.AddItem(potion);
            inventory.AddItem(coin);

            inventory.SelectSlot(0);
            testObject.SendMessage("UseSelectedItem");
            Check(health.CurrentHealth == health.MaxHealth && inventory.GetSlot(0).count == 2,
                "Using a potion at full health does nothing (potion is kept)");

            health.TakeDamage(50);
            testObject.SendMessage("UseSelectedItem");
            Check(health.CurrentHealth == 75 && inventory.GetSlot(0).count == 1,
                "Using a potion heals 25 and removes one from the stack");

            inventory.SelectSlot(1);
            testObject.SendMessage("UseSelectedItem");
            Check(health.CurrentHealth == 75 && inventory.GetSlot(1).count == 1,
                "Using a non-usable item changes nothing");

            inventory.SelectSlot(0);
            health.TakeDamage(1000);
            testObject.SendMessage("UseSelectedItem");
            Check(inventory.GetSlot(0).count == 1, "A dead player cannot use a potion");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
            Object.DestroyImmediate(potion);
            Object.DestroyImmediate(coin);
        }
    }

    private static ItemData CreateTestItem(string itemName, int maxStackSize, int healAmount)
    {
        ItemData item = ScriptableObject.CreateInstance<ItemData>();
        SerializedObject serializedItem = new SerializedObject(item);
        serializedItem.FindProperty("itemName").stringValue = itemName;
        serializedItem.FindProperty("maxStackSize").intValue = maxStackSize;
        serializedItem.FindProperty("healAmount").intValue = healAmount;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();
        return item;
    }

    // ---------------------------------------------------------------
    // ItemData assets
    // ---------------------------------------------------------------

    private static void CheckItemAssets()
    {
        CheckItemAsset("Coin", "Coin", 99, 0);
        CheckItemAsset("HealthPotion", "Health Potion", 5, 25);
        CheckItemAsset("Gem", "Gem", 1, 0);
    }

    private static void CheckItemAsset(string fileName, string itemName, int maxStackSize, int healAmount)
    {
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(ItemsFolder + "/" + fileName + ".asset");
        Check(item != null, fileName + " ItemData asset exists");
        if (item == null)
        {
            return;
        }

        Check(item.ItemName == itemName && item.MaxStackSize == maxStackSize && item.HealAmount == healAmount,
            fileName + " has name '" + itemName + "', stack " + maxStackSize + ", heal " + healAmount);
        Check(item.Icon != null && item.Icon.texture.width == 64 && item.Icon.texture.height == 64,
            fileName + " has a 64x64 icon sprite");
    }

    // ---------------------------------------------------------------
    // Input actions
    // ---------------------------------------------------------------

    private static void CheckInputActions()
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        Check(inputAsset != null, "InputSystem_Actions asset exists");
        if (inputAsset == null)
        {
            return;
        }

        InputActionMap playerMap = inputAsset.FindActionMap("Player");
        Check(playerMap != null, "Input asset has a Player map");
        if (playerMap == null)
        {
            return;
        }

        CheckInputAction(playerMap, "UseItem", "<Keyboard>/q");
        for (int i = 1; i <= 6; i++)
        {
            CheckInputAction(playerMap, "Slot" + i, "<Keyboard>/" + i);
        }
    }

    private static void CheckInputAction(InputActionMap map, string actionName, string bindingPath)
    {
        InputAction action = map.FindAction(actionName);
        bool hasBinding = false;

        if (action != null)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == bindingPath)
                {
                    hasBinding = true;
                }
            }
        }

        Check(action != null && hasBinding, "Input action " + actionName + " exists and is bound to " + bindingPath);
    }

    // ---------------------------------------------------------------
    // ThirdPerson scene
    // ---------------------------------------------------------------

    private static void CheckScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject player = GameObject.Find("Player");
        Check(player != null, "ThirdPerson has a Player");
        if (player == null)
        {
            return;
        }

        Inventory inventory = player.GetComponent<Inventory>();
        Check(inventory != null, "Player has an Inventory");
        Check(player.GetComponent<PlayerItemUser>() != null, "Player has a PlayerItemUser");
        Check(player.GetComponent<Health>() != null, "Player has a Health (needed to use potions)");

        CheckPickups();
        CheckHotbar(inventory);
    }

    private static void CheckHotbar(Inventory playerInventory)
    {
        GameObject hudCanvas = GameObject.Find("HUDCanvas");
        Check(hudCanvas != null, "ThirdPerson has a HUDCanvas");
        if (hudCanvas == null)
        {
            return;
        }

        HotbarUI hotbar = hudCanvas.GetComponentInChildren<HotbarUI>(true);
        Check(hotbar != null, "HUDCanvas has a HotbarUI");
        if (hotbar == null)
        {
            return;
        }

        SerializedObject serializedHotbar = new SerializedObject(hotbar);
        Object wiredInventory = serializedHotbar.FindProperty("inventory").objectReferenceValue;
        Check(wiredInventory != null && wiredInventory == playerInventory, "HotbarUI is wired to the Player's Inventory");

        SerializedProperty slotViews = serializedHotbar.FindProperty("slotViews");
        Check(slotViews.arraySize == 6, "HotbarUI has 6 slots (found " + slotViews.arraySize + ")");

        bool allSlotsWired = slotViews.arraySize > 0;
        for (int i = 0; i < slotViews.arraySize; i++)
        {
            HotbarSlotUI slotView = slotViews.GetArrayElementAtIndex(i).objectReferenceValue as HotbarSlotUI;
            if (slotView == null)
            {
                allSlotsWired = false;
                continue;
            }

            SerializedObject serializedSlot = new SerializedObject(slotView);
            if (serializedSlot.FindProperty("background").objectReferenceValue == null ||
                serializedSlot.FindProperty("iconImage").objectReferenceValue == null ||
                serializedSlot.FindProperty("countText").objectReferenceValue == null)
            {
                allSlotsWired = false;
            }
        }
        Check(allSlotsWired, "Every hotbar slot has its background, icon and count text assigned");

        RectTransform hotbarRect = hotbar.GetComponent<RectTransform>();
        Check(hotbarRect.anchorMin == new Vector2(0.5f, 0f) && hotbarRect.anchorMax == new Vector2(0.5f, 0f),
            "Hotbar is anchored to the bottom-centre of the screen");
    }

    private static void CheckPickups()
    {
        ItemPickup[] pickups = Object.FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int coins = 0;
        int potions = 0;
        int gems = 0;
        bool allValid = true;

        for (int i = 0; i < pickups.Length; i++)
        {
            ItemPickup pickup = pickups[i];
            ItemData item = pickup.ItemData;
            bool hasCollider = pickup.GetComponent<Collider>() != null;
            bool canBeHit = pickup.gameObject.layer != LayerMask.NameToLayer("Ignore Raycast");

            if (item == null || !hasCollider || !canBeHit)
            {
                allValid = false;
                Debug.LogError("Pickup " + pickup.name + " is missing ItemData or a raycast-able collider");
                continue;
            }

            if (item.name == "Coin")
            {
                coins++;
            }
            else if (item.name == "HealthPotion")
            {
                potions++;
            }
            else if (item.name == "Gem")
            {
                gems++;
            }
        }

        Check(allValid && pickups.Length > 0, "Every pickup has ItemPickup with an ItemData and a collider the raycast can hit");
        Check(coins == 5 && potions == 2 && gems == 1,
            "Scene has 5 coins, 2 health potions and 1 gem (found " + coins + ", " + potions + ", " + gems + ")");
    }
}
