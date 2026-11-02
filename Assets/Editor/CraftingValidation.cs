using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Teacher tool: checks the Crafting System code, assets and scene without entering Play mode.
// It first runs every Inventory Basics check (so crafting cannot break the inventory),
// then the crafting checks. Every check prints a [PASS] or [FAIL] line to the Console.
// Run it from the menu Tools > Validate Crafting, or in batchmode with
// -executeMethod CraftingValidation.RunAll (Unity then exits with code 1 if anything failed).
public static class CraftingValidation
{
    private const string ScenePath = "Assets/Scenes/ThirdPerson.unity";
    private const string ItemsFolder = "Assets/Items";
    private const string RecipesFolder = "Assets/Crafting/Recipes";
    private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";

    private static int passCount;
    private static int failCount;

    // The real assets, loaded once per run. The logic tests use them, so they also
    // prove that the recipe assets contain the right ingredients.
    private static ItemData coin;
    private static ItemData potion;
    private static ItemData gem;
    private static ItemData goldBar;
    private static ItemData bigPotion;
    private static ItemData gemRing;
    private static CraftingRecipe goldBarRecipe;
    private static CraftingRecipe bigPotionRecipe;
    private static CraftingRecipe gemRingRecipe;

    [MenuItem("Tools/Validate Crafting")]
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

        int inventoryFailures = InventoryValidation.RunChecks();
        Check(inventoryFailures == 0, "All Inventory Basics checks still pass (" + inventoryFailures + " failed)");

        if (LoadAssets())
        {
            CheckMatching();
            CheckCrafting();
            CheckUncrafting();
            CheckClosing();
            CheckMovingItems();
        }

        CheckScene();

        Debug.Log("[SUMMARY] Crafting validation: " + passCount + " passed, " + failCount + " failed");
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
    // Items and recipes
    // ---------------------------------------------------------------

    private static bool LoadAssets()
    {
        coin = LoadItem("Coin");
        potion = LoadItem("HealthPotion");
        gem = LoadItem("Gem");
        goldBar = CheckNewItem("GoldBar", "Gold Bar", 10, 0);
        bigPotion = CheckNewItem("BigHealthPotion", "Big Health Potion", 3, 60);
        gemRing = CheckNewItem("GemRing", "Gem Ring", 1, 0);

        goldBarRecipe = CheckRecipe("GoldBar", goldBar, coin, 4, null, 0);
        bigPotionRecipe = CheckRecipe("BigHealthPotion", bigPotion, potion, 2, gem, 1);
        gemRingRecipe = CheckRecipe("GemRing", gemRing, goldBar, 1, gem, 1);

        return coin != null && potion != null && gem != null && goldBar != null && bigPotion != null &&
               gemRing != null && goldBarRecipe != null && bigPotionRecipe != null && gemRingRecipe != null;
    }

    private static ItemData LoadItem(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>(ItemsFolder + "/" + fileName + ".asset");
    }

    private static ItemData CheckNewItem(string fileName, string itemName, int maxStackSize, int healAmount)
    {
        ItemData item = LoadItem(fileName);
        Check(item != null, fileName + " ItemData asset exists");
        if (item == null)
        {
            return null;
        }

        Check(item.ItemName == itemName && item.MaxStackSize == maxStackSize && item.HealAmount == healAmount,
            fileName + " has name '" + itemName + "', stack " + maxStackSize + ", heal " + healAmount);
        Check(item.Icon != null && item.Icon.texture.width == 64 && item.Icon.texture.height == 64,
            fileName + " has a 64x64 icon sprite");
        return item;
    }

    private static CraftingRecipe CheckRecipe(string fileName, ItemData result,
        ItemData firstItem, int firstCount, ItemData secondItem, int secondCount)
    {
        CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(RecipesFolder + "/" + fileName + ".asset");
        Check(recipe != null, "Recipe " + fileName + " exists");
        if (recipe == null)
        {
            return null;
        }

        int expectedIngredients = secondItem == null ? 1 : 2;
        bool ingredientsOk = recipe.Ingredients.Count == expectedIngredients &&
                             recipe.Ingredients[0].item == firstItem && recipe.Ingredients[0].count == firstCount;
        if (ingredientsOk && secondItem != null)
        {
            ingredientsOk = recipe.Ingredients[1].item == secondItem && recipe.Ingredients[1].count == secondCount;
        }

        Check(ingredientsOk && recipe.Result == result && recipe.ResultCount == 1,
            "Recipe " + fileName + " has the right ingredients and makes 1 " + (result != null ? result.ItemName : "?"));
        Check(recipe.Result != null && recipe.Result.Icon != null, "Recipe " + fileName + " result has an icon");
        return recipe;
    }

    // ---------------------------------------------------------------
    // A temporary "player" with an Inventory and a CraftingGrid
    // ---------------------------------------------------------------

    private static GameObject CreateTestPlayer()
    {
        GameObject testObject = new GameObject("CraftingTest");
        testObject.AddComponent<Inventory>();
        CraftingGrid craftingGrid = testObject.AddComponent<CraftingGrid>();

        SerializedObject serializedGrid = new SerializedObject(craftingGrid);
        SerializedProperty recipes = serializedGrid.FindProperty("recipes");
        recipes.arraySize = 3;
        recipes.GetArrayElementAtIndex(0).objectReferenceValue = goldBarRecipe;
        recipes.GetArrayElementAtIndex(1).objectReferenceValue = bigPotionRecipe;
        recipes.GetArrayElementAtIndex(2).objectReferenceValue = gemRingRecipe;
        serializedGrid.ApplyModifiedPropertiesWithoutUndo();

        // Awake does not run in Edit mode, so we call it ourselves (Inventory first, then CraftingGrid).
        testObject.SendMessage("Awake");
        return testObject;
    }

    // Adds one item to the inventory and moves it straight into a grid slot.
    private static bool PlaceInGrid(Inventory inventory, CraftingGrid craftingGrid, ItemData item, int gridIndex)
    {
        inventory.AddItem(item);
        return craftingGrid.MoveToGrid(FindSlotWith(inventory, item), gridIndex);
    }

    // Gives back the index of the first inventory slot that holds this item, or -1.
    private static int FindSlotWith(Inventory inventory, ItemData item)
    {
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (inventory.GetSlot(i).item == item)
            {
                return i;
            }
        }

        return -1;
    }

    // How many of this item the whole inventory holds.
    private static int CountInInventory(Inventory inventory, ItemData item)
    {
        int count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (inventory.GetSlot(i).item == item)
            {
                count += inventory.GetSlot(i).count;
            }
        }

        return count;
    }

    // Fills every empty inventory slot with a gem (gems do not stack), so nothing else fits.
    private static void FillInventoryWithGems(Inventory inventory)
    {
        while (inventory.AddItem(gem))
        {
        }
    }

    private static int CountItemsInGrid(CraftingGrid craftingGrid)
    {
        int count = 0;
        for (int i = 0; i < craftingGrid.GridSlotCount; i++)
        {
            if (!craftingGrid.GetGridSlot(i).IsEmpty())
            {
                count++;
            }
        }

        return count;
    }

    private static bool ShowsCraftPreview(CraftingGrid craftingGrid, ItemData result)
    {
        return craftingGrid.IsResultPreview && craftingGrid.ResultSlot.item == result;
    }

    // ---------------------------------------------------------------
    // Matching
    // ---------------------------------------------------------------

    private static void CheckMatching()
    {
        Check(MatchAfterPlacing(new ItemData[] { coin, coin, coin, coin }, new int[] { 0, 2, 4, 8 }) == goldBarRecipe,
            "4 coins anywhere in the grid match GoldBar");
        Check(MatchAfterPlacing(new ItemData[] { coin, coin, coin, coin }, new int[] { 5, 6, 7, 3 }) == goldBarRecipe,
            "4 coins in other slots also match GoldBar (shapeless)");
        Check(MatchAfterPlacing(new ItemData[] { coin, coin, coin }, new int[] { 0, 1, 2 }) == null,
            "3 coins do not match");
        Check(MatchAfterPlacing(new ItemData[] { coin, coin, coin, coin, coin }, new int[] { 0, 1, 2, 3, 4 }) == null,
            "5 coins do not match");
        Check(MatchAfterPlacing(new ItemData[] { coin, coin, coin, coin, gem }, new int[] { 0, 1, 2, 3, 4 }) == null,
            "4 coins + 1 gem do not match (extra item)");
        Check(MatchAfterPlacing(new ItemData[] { potion, gem, potion }, new int[] { 8, 3, 1 }) == bigPotionRecipe,
            "2 potions + 1 gem in any slots match BigHealthPotion");
        Check(MatchAfterPlacing(new ItemData[] { gem, goldBar }, new int[] { 4, 6 }) == gemRingRecipe,
            "1 gold bar + 1 gem match GemRing");
        Check(MatchAfterPlacing(new ItemData[] { potion, potion }, new int[] { 0, 1 }) == null,
            "2 potions without a gem do not match");
    }

    // Places the items in the given grid slots on a fresh test player and returns the matching recipe.
    private static CraftingRecipe MatchAfterPlacing(ItemData[] items, int[] gridIndexes)
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            for (int i = 0; i < items.Length; i++)
            {
                PlaceInGrid(inventory, craftingGrid, items[i], gridIndexes[i]);
            }

            return craftingGrid.FindMatchingRecipe();
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // ---------------------------------------------------------------
    // Crafting
    // ---------------------------------------------------------------

    private static void CheckCrafting()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            PlaceInGrid(inventory, craftingGrid, coin, 0);
            PlaceInGrid(inventory, craftingGrid, coin, 1);
            PlaceInGrid(inventory, craftingGrid, coin, 2);
            Check(craftingGrid.ResultSlot.IsEmpty() && !craftingGrid.IsResultPreview, "No preview with only 3 coins");

            PlaceInGrid(inventory, craftingGrid, coin, 7);
            Check(ShowsCraftPreview(craftingGrid, goldBar), "A Gold Bar preview appears when the grid matches");
            Check(CountInInventory(inventory, goldBar) == 0, "The preview is not real: no Gold Bar in the inventory yet");

            craftingGrid.MoveGridToInventory(7);
            Check(craftingGrid.ResultSlot.IsEmpty() && !craftingGrid.IsResultPreview,
                "The preview disappears when an ingredient is removed");
            Check(CountInInventory(inventory, coin) == 1, "The removed coin is back in the inventory");

            craftingGrid.MoveToGrid(FindSlotWith(inventory, coin), 4);
            Check(ShowsCraftPreview(craftingGrid, goldBar), "The preview comes back when the coin is put back");

            bool crafted = craftingGrid.MoveResultToInventory();
            Check(crafted && CountInInventory(inventory, goldBar) == 1, "Taking the result adds a Gold Bar to the inventory");
            Check(CountItemsInGrid(craftingGrid) == 0 && craftingGrid.ResultSlot.IsEmpty() && !craftingGrid.IsResultPreview,
                "Crafting uses up the grid and empties the result slot");
            Check(CountInInventory(inventory, coin) == 0, "The 4 coins are consumed");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        CheckCraftingWithFullInventory();
    }

    private static void CheckCraftingWithFullInventory()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            PlaceInGrid(inventory, craftingGrid, coin, 0);
            PlaceInGrid(inventory, craftingGrid, coin, 1);
            PlaceInGrid(inventory, craftingGrid, coin, 2);
            PlaceInGrid(inventory, craftingGrid, coin, 3);
            FillInventoryWithGems(inventory);

            bool crafted = craftingGrid.MoveResultToInventory();
            Check(!crafted && ShowsCraftPreview(craftingGrid, goldBar) && CountItemsInGrid(craftingGrid) == 4 &&
                  CountInInventory(inventory, goldBar) == 0 && CountInInventory(inventory, gem) == 6,
                "With a full inventory nothing changes: preview and materials stay");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // ---------------------------------------------------------------
    // Uncrafting
    // ---------------------------------------------------------------

    private static void CheckUncrafting()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            inventory.AddItem(coin);
            Check(!craftingGrid.MoveToResult(FindSlotWith(inventory, coin)) && CountInInventory(inventory, coin) == 1 &&
                  craftingGrid.ResultSlot.IsEmpty(),
                "A Coin on the result slot is rejected (no recipe makes it)");

            inventory.AddItem(goldBar);
            bool started = craftingGrid.MoveToResult(FindSlotWith(inventory, goldBar));
            Check(started && craftingGrid.ResultSlot.item == goldBar && !craftingGrid.IsResultPreview,
                "A Gold Bar on the result slot with an empty grid starts an uncraft");
            Check(CountInInventory(inventory, goldBar) == 0, "The Gold Bar left the inventory");
            Check(craftingGrid.IsGridPreview && CountItemsInGrid(craftingGrid) == 4 && craftingGrid.GetGridSlot(0).item == coin &&
                  craftingGrid.GetGridSlot(3).item == coin,
                "The grid shows 4 coin previews");
            Check(!craftingGrid.MoveToGrid(FindSlotWith(inventory, coin), 5),
                "Adding items to the grid is rejected while uncrafting");

            bool completed = craftingGrid.MoveGridToInventory(2);
            Check(completed && CountInInventory(inventory, coin) == 2, "Taking the first coin adds it to the inventory");
            Check(craftingGrid.ResultSlot.IsEmpty() && CountInInventory(inventory, goldBar) == 0,
                "The Gold Bar is consumed");
            Check(!craftingGrid.IsGridPreview && CountItemsInGrid(craftingGrid) == 3 && craftingGrid.GetGridSlot(2).IsEmpty(),
                "The other 3 coins become real items in the grid");

            craftingGrid.MoveGridToInventory(0);
            craftingGrid.MoveGridToInventory(1);
            craftingGrid.MoveGridToInventory(3);
            Check(CountInInventory(inventory, coin) == 5 && CountItemsInGrid(craftingGrid) == 0,
                "The real coins can be taken one by one (5 coins in total)");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        CheckUncraftCancelAndRejections();
    }

    private static void CheckUncraftCancelAndRejections()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            inventory.AddItem(goldBar);
            craftingGrid.MoveToResult(FindSlotWith(inventory, goldBar));
            bool cancelled = craftingGrid.MoveResultToInventory();
            Check(cancelled && CountInInventory(inventory, goldBar) == 1 && craftingGrid.ResultSlot.IsEmpty() &&
                  !craftingGrid.IsGridPreview && CountItemsInGrid(craftingGrid) == 0,
                "Cancelling an uncraft returns the Gold Bar and clears the previews");

            PlaceInGrid(inventory, craftingGrid, gem, 4);
            Check(!craftingGrid.MoveToResult(FindSlotWith(inventory, goldBar)) && CountInInventory(inventory, goldBar) == 1,
                "A Gold Bar on the result slot with a non-empty grid is rejected");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            inventory.AddItem(goldBar);
            craftingGrid.MoveToResult(FindSlotWith(inventory, goldBar));
            FillInventoryWithGems(inventory);

            bool completed = craftingGrid.MoveGridToInventory(0);
            Check(!completed && craftingGrid.ResultSlot.item == goldBar && craftingGrid.IsGridPreview &&
                  CountItemsInGrid(craftingGrid) == 4,
                "Uncrafting with a full inventory changes nothing");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // ---------------------------------------------------------------
    // Closing the panel (ReturnEverythingToInventory)
    // ---------------------------------------------------------------

    private static void CheckClosing()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            PlaceInGrid(inventory, craftingGrid, coin, 0);
            PlaceInGrid(inventory, craftingGrid, coin, 4);
            PlaceInGrid(inventory, craftingGrid, gem, 8);
            bool allReturned = craftingGrid.ReturnEverythingToInventory();
            Check(allReturned && CountItemsInGrid(craftingGrid) == 0 && CountInInventory(inventory, coin) == 2 &&
                  CountInInventory(inventory, gem) == 1,
                "Closing returns real grid items to the inventory");

            PlaceInGrid(inventory, craftingGrid, coin, 0);
            PlaceInGrid(inventory, craftingGrid, coin, 1);
            PlaceInGrid(inventory, craftingGrid, coin, 2);
            PlaceInGrid(inventory, craftingGrid, coin, 3);
            craftingGrid.ReturnEverythingToInventory();
            Check(CountInInventory(inventory, goldBar) == 0 && CountInInventory(inventory, coin) == 6 &&
                  craftingGrid.ResultSlot.IsEmpty() && !craftingGrid.IsResultPreview,
                "Closing clears a craft preview without crafting");

            inventory.AddItem(goldBar);
            craftingGrid.MoveToResult(FindSlotWith(inventory, goldBar));
            craftingGrid.ReturnEverythingToInventory();
            Check(CountInInventory(inventory, goldBar) == 1 && craftingGrid.ResultSlot.IsEmpty() &&
                  CountItemsInGrid(craftingGrid) == 0 && !craftingGrid.IsGridPreview,
                "Closing returns an uncompleted uncraft item and clears the previews");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }

        testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            PlaceInGrid(inventory, craftingGrid, gem, 0);
            PlaceInGrid(inventory, craftingGrid, gem, 1);
            FillInventoryWithGems(inventory);
            bool allReturned = craftingGrid.ReturnEverythingToInventory();
            Check(!allReturned && CountItemsInGrid(craftingGrid) == 2 && CountInInventory(inventory, gem) == 6,
                "With a full inventory the leftovers stay in the grid");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
    }

    // ---------------------------------------------------------------
    // Moving items between the inventory and the grid
    // ---------------------------------------------------------------

    private static void CheckMovingItems()
    {
        GameObject testObject = CreateTestPlayer();

        try
        {
            Inventory inventory = testObject.GetComponent<Inventory>();
            CraftingGrid craftingGrid = testObject.GetComponent<CraftingGrid>();

            inventory.AddItem(coin);
            inventory.AddItem(coin);
            inventory.AddItem(coin);
            bool moved = craftingGrid.MoveToGrid(0, 0);
            Check(moved && inventory.GetSlot(0).count == 2 && craftingGrid.GetGridSlot(0).item == coin &&
                  craftingGrid.GetGridSlot(0).count == 1,
                "Moving to a crafting slot moves one item: the stack goes from 3 to 2");

            bool movedAgain = craftingGrid.MoveToGrid(0, 0);
            Check(!movedAgain && inventory.GetSlot(0).count == 2, "Dropping on an occupied crafting slot is rejected");

            Check(!craftingGrid.MoveToGrid(3, 1) && craftingGrid.GetGridSlot(1).IsEmpty(),
                "Moving from an empty inventory slot does nothing");

            craftingGrid.MoveGridToInventory(0);
            Check(inventory.GetSlot(0).count == 3 && craftingGrid.GetGridSlot(0).IsEmpty(),
                "Moving a grid item back to the inventory returns it");
        }
        finally
        {
            Object.DestroyImmediate(testObject);
        }
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

        CraftingGrid craftingGrid = player.GetComponent<CraftingGrid>();
        Check(craftingGrid != null, "Player has a CraftingGrid");
        if (craftingGrid != null)
        {
            CheckPlayerRecipes(craftingGrid);
        }

        CheckPickupCounts();
        CheckToggleAction();
        CheckEventSystem();

        // Running Tools > Setup Crafting Scene twice must not make copies.
        int eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int panels = Object.FindObjectsByType<CraftingPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int dragIcons = Object.FindObjectsByType<ItemDragUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        Check(eventSystems == 1 && panels == 1 && dragIcons == 1,
            "No duplicates: 1 EventSystem, 1 CraftingPanelUI, 1 DragIcon (found " + eventSystems + ", " + panels + ", " + dragIcons + ")");

        GameObject hudCanvas = GameObject.Find("HUDCanvas");
        Check(hudCanvas != null && hudCanvas.GetComponent<GraphicRaycaster>() != null,
            "HUDCanvas has a GraphicRaycaster (needed for UI clicks and drops)");
        if (hudCanvas != null && craftingGrid != null)
        {
            CheckCraftingUI(hudCanvas, player, craftingGrid);
        }
    }

    private static void CheckToggleAction()
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        InputAction action = null;
        if (inputAsset != null && inputAsset.FindActionMap("Player") != null)
        {
            action = inputAsset.FindActionMap("Player").FindAction("ToggleCrafting");
        }

        bool boundToC = false;
        if (action != null)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].path == "<Keyboard>/c")
                {
                    boundToC = true;
                }
            }
        }

        Check(action != null && boundToC, "Input action ToggleCrafting exists in the Player map and is bound to <Keyboard>/c");
    }

    private static void CheckEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        Check(eventSystem != null, "ThirdPerson has an EventSystem");
        if (eventSystem == null)
        {
            return;
        }

        InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
        Check(module != null, "EventSystem uses an InputSystemUIInputModule");
        Check(eventSystem.GetComponent<StandaloneInputModule>() == null, "EventSystem has no old StandaloneInputModule");
        if (module == null)
        {
            return;
        }

        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        Check(module.actionsAsset == inputAsset, "The UI input module uses InputSystem_Actions");
        Check(IsUIAction(module.point, "Point") && IsUIAction(module.leftClick, "Click"),
            "The UI input module's Point and Click come from the UI map");
    }

    private static bool IsUIAction(InputActionReference reference, string actionName)
    {
        return reference != null && reference.action != null &&
               reference.action.actionMap.name == "UI" && reference.action.name == actionName;
    }

    private static void CheckCraftingUI(GameObject hudCanvas, GameObject player, CraftingGrid craftingGrid)
    {
        CraftingPanelUI panelUI = hudCanvas.GetComponentInChildren<CraftingPanelUI>(true);
        Check(panelUI != null, "HUDCanvas has a CraftingPanelUI");
        if (panelUI == null)
        {
            return;
        }

        Check(panelUI.gameObject.activeSelf, "The CraftingPanelUI object is active (so C works while the panel is hidden)");

        SerializedObject serializedPanel = new SerializedObject(panelUI);
        GameObject panel = serializedPanel.FindProperty("panel").objectReferenceValue as GameObject;
        Check(panel != null && !panel.activeSelf, "The crafting panel is assigned and starts hidden");
        Check(serializedPanel.FindProperty("player").objectReferenceValue == player, "CraftingPanelUI is wired to the Player");

        SerializedProperty gridSlotViews = serializedPanel.FindProperty("gridSlotViews");
        bool gridWired = gridSlotViews.arraySize == 9;
        for (int i = 0; i < gridSlotViews.arraySize; i++)
        {
            CraftingSlotUI slotView = gridSlotViews.GetArrayElementAtIndex(i).objectReferenceValue as CraftingSlotUI;
            if (!IsCraftingSlotWired(slotView, false, i))
            {
                gridWired = false;
            }
        }
        Check(gridWired, "CraftingPanelUI has 9 grid slots, numbered 0-8, with icon and count assigned");

        CraftingSlotUI resultView = serializedPanel.FindProperty("resultSlotView").objectReferenceValue as CraftingSlotUI;
        Check(IsCraftingSlotWired(resultView, true, 0), "CraftingPanelUI has a result slot with icon and count assigned");

        Image arrow = null;
        if (panel != null)
        {
            Transform arrowTransform = panel.transform.Find("Arrow");
            if (arrowTransform != null)
            {
                arrow = arrowTransform.GetComponent<Image>();
            }
        }
        Check(arrow != null && arrow.sprite != null, "The panel has a two-way arrow image");

        CheckCraftButton(panelUI);
        CheckDragAndDrop(hudCanvas, craftingGrid, panelUI, gridSlotViews, resultView);
    }

    private static void CheckDragAndDrop(GameObject hudCanvas, CraftingGrid craftingGrid, CraftingPanelUI panelUI,
        SerializedProperty gridSlotViews, CraftingSlotUI resultView)
    {
        ItemDragUI dragIcon = hudCanvas.GetComponentInChildren<ItemDragUI>(true);
        Check(dragIcon != null, "HUDCanvas has a DragIcon with ItemDragUI");
        if (dragIcon == null)
        {
            return;
        }

        Image dragImage = dragIcon.GetComponent<Image>();
        Check(dragImage != null && !dragImage.raycastTarget, "The DragIcon image has Raycast Target off");
        Check(dragIcon.transform.parent == hudCanvas.transform &&
              dragIcon.transform.GetSiblingIndex() == hudCanvas.transform.childCount - 1,
            "The DragIcon is the last child of HUDCanvas (drawn on top)");

        bool craftingSlotsWired = resultView != null && IsWiredForDragging(resultView, craftingGrid, dragIcon);
        for (int i = 0; i < gridSlotViews.arraySize; i++)
        {
            CraftingSlotUI slotView = gridSlotViews.GetArrayElementAtIndex(i).objectReferenceValue as CraftingSlotUI;
            if (slotView == null || !IsWiredForDragging(slotView, craftingGrid, dragIcon))
            {
                craftingSlotsWired = false;
            }
        }
        Check(craftingSlotsWired, "Every crafting slot knows the Player's CraftingGrid and the DragIcon");

        HotbarUI hotbar = hudCanvas.GetComponentInChildren<HotbarUI>(true);
        if (hotbar == null)
        {
            Check(false, "HUDCanvas has a HotbarUI");
            return;
        }

        SerializedProperty slotViews = new SerializedObject(hotbar).FindProperty("slotViews");
        bool hotbarWired = slotViews.arraySize == 6;
        for (int i = 0; i < slotViews.arraySize; i++)
        {
            HotbarSlotUI slotView = slotViews.GetArrayElementAtIndex(i).objectReferenceValue as HotbarSlotUI;
            if (slotView == null)
            {
                hotbarWired = false;
                continue;
            }

            SerializedObject serializedSlot = new SerializedObject(slotView);
            Image background = serializedSlot.FindProperty("background").objectReferenceValue as Image;
            if (serializedSlot.FindProperty("slotIndex").intValue != i ||
                serializedSlot.FindProperty("craftingGrid").objectReferenceValue != craftingGrid ||
                serializedSlot.FindProperty("craftingPanel").objectReferenceValue != panelUI ||
                serializedSlot.FindProperty("dragIcon").objectReferenceValue != dragIcon ||
                background == null || !background.raycastTarget || background.gameObject != slotView.gameObject)
            {
                hotbarWired = false;
            }
        }
        Check(hotbarWired,
            "Hotbar slots have indexes 0-5, Raycast Target on, and know the CraftingGrid, CraftingPanelUI and DragIcon");
    }

    private static bool IsWiredForDragging(CraftingSlotUI slotView, CraftingGrid craftingGrid, ItemDragUI dragIcon)
    {
        SerializedObject serializedSlot = new SerializedObject(slotView);
        return serializedSlot.FindProperty("craftingGrid").objectReferenceValue == craftingGrid &&
               serializedSlot.FindProperty("dragIcon").objectReferenceValue == dragIcon;
    }

    private static bool IsCraftingSlotWired(CraftingSlotUI slotView, bool isResultSlot, int gridIndex)
    {
        if (slotView == null)
        {
            return false;
        }

        SerializedObject serializedSlot = new SerializedObject(slotView);
        Image background = slotView.GetComponent<Image>();
        bool indexOk = isResultSlot || serializedSlot.FindProperty("gridIndex").intValue == gridIndex;

        return serializedSlot.FindProperty("iconImage").objectReferenceValue != null &&
               serializedSlot.FindProperty("countText").objectReferenceValue != null &&
               serializedSlot.FindProperty("isResultSlot").boolValue == isResultSlot &&
               indexOk && background != null && background.raycastTarget;
    }

    private static void CheckCraftButton(CraftingPanelUI panelUI)
    {
        Transform buttonTransform = panelUI.transform.Find("CraftButton");
        Button button = null;
        if (buttonTransform != null)
        {
            button = buttonTransform.GetComponent<Button>();
        }
        Check(button != null, "There is a Craft button");
        if (button == null)
        {
            return;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        Check(label != null && label.text == "Craft (C)", "The Craft button says 'Craft (C)'");

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        Check(buttonRect.anchorMin == new Vector2(1f, 0f) && buttonRect.anchorMax == new Vector2(1f, 0f),
            "The Craft button is anchored bottom-right");

        bool callsToggle = false;
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentTarget(i) == panelUI && button.onClick.GetPersistentMethodName(i) == "TogglePanel")
            {
                callsToggle = true;
            }
        }
        Check(callsToggle, "The Craft button calls CraftingPanelUI.TogglePanel");
    }

    private static void CheckPlayerRecipes(CraftingGrid craftingGrid)
    {
        SerializedProperty recipes = new SerializedObject(craftingGrid).FindProperty("recipes");
        bool hasGoldBar = false;
        bool hasBigPotion = false;
        bool hasGemRing = false;

        for (int i = 0; i < recipes.arraySize; i++)
        {
            Object recipe = recipes.GetArrayElementAtIndex(i).objectReferenceValue;
            if (recipe == null)
            {
                continue;
            }

            if (recipe.name == "GoldBar")
            {
                hasGoldBar = true;
            }
            else if (recipe.name == "BigHealthPotion")
            {
                hasBigPotion = true;
            }
            else if (recipe.name == "GemRing")
            {
                hasGemRing = true;
            }
        }

        Check(recipes.arraySize == 3 && hasGoldBar && hasBigPotion && hasGemRing,
            "Player CraftingGrid has the 3 recipes GoldBar, BigHealthPotion and GemRing");
    }

    private static void CheckPickupCounts()
    {
        GameObject root = GameObject.Find("ItemPickups");
        Check(root != null, "ThirdPerson has ItemPickups");
        if (root == null)
        {
            return;
        }

        ItemPickup[] pickups = root.GetComponentsInChildren<ItemPickup>(true);
        int coins = 0;
        int gems = 0;
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i].ItemData != null && pickups[i].ItemData.name == "Coin")
            {
                coins++;
            }
            else if (pickups[i].ItemData != null && pickups[i].ItemData.name == "Gem")
            {
                gems++;
            }
        }

        Check(coins == 9 && gems == 3, "ItemPickups has 9 coins and 3 gems (found " + coins + " and " + gems + ")");
    }
}
