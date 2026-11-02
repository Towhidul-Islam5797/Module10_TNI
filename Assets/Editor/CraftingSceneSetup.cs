using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Teacher tool: builds everything the Crafting System topic needs in the
// ThirdPerson scene, so nobody has to hand-edit the scene file.
// It creates the new items and their icons, the recipes, the CraftingGrid on the
// Player, extra coin and gem pickups, the EventSystem, and the crafting UI
// (Craft button and crafting panel) on the HUDCanvas.
// Run Tools > Setup Inventory Scene first: crafting builds on the inventory.
// It is safe to run more than once: it reuses or replaces what it made before.
// Run it from the menu Tools > Setup Crafting Scene, or in batchmode with
// -executeMethod CraftingSceneSetup.Run
public static class CraftingSceneSetup
{
    private const string ScenePath = "Assets/Scenes/ThirdPerson.unity";
    private const string ItemsFolder = "Assets/Items";
    private const string MaterialsFolder = "Assets/Items/Materials";
    private const string CraftingFolder = "Assets/Crafting";
    private const string RecipesFolder = "Assets/Crafting/Recipes";
    private const string CraftingIconsFolder = "Assets/Crafting/Icons";
    private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
    private const string PickupsRootName = "ItemPickups";
    private const string CraftingRootName = "Crafting";
    private const int GridSlotCount = 9;
    private const float SlotSize = 80f;
    private const float ResultSlotSize = 90f;
    private const float SlotSpacing = 10f;
    private const float PanelWidth = 580f;
    private const float PanelHeight = 380f;

    [MenuItem("Tools/Setup Crafting Scene")]
    private static void RunFromMenu()
    {
        // Give the teacher a chance to save the open scene before we switch to ThirdPerson.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Run();
    }

    // The main entry point. Every step is its own method so it is easy to follow.
    public static void Run()
    {
        // Open the scene first. Opening a scene unloads assets that are only held
        // in memory, so the items and recipes are created after this.
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        ItemData coin = LoadItem("Coin");
        ItemData potion = LoadItem("HealthPotion");
        ItemData gem = LoadItem("Gem");
        if (coin == null || potion == null || gem == null)
        {
            Debug.LogError("CraftingSceneSetup: Coin, HealthPotion or Gem is missing. Run Tools > Setup Inventory Scene first.");
            return;
        }

        CreateFolders();

        ItemData goldBar = InventorySceneSetup.CreateItem("GoldBar", "Gold Bar", 10, 0, DrawGoldBarIcon());
        ItemData bigPotion = InventorySceneSetup.CreateItem("BigHealthPotion", "Big Health Potion", 3, 60, DrawBigPotionIcon());
        ItemData gemRing = InventorySceneSetup.CreateItem("GemRing", "Gem Ring", 1, 0, DrawGemRingIcon());

        CraftingRecipe goldBarRecipe = CreateRecipe("GoldBar", goldBar, coin, 4, null, 0);
        CraftingRecipe bigPotionRecipe = CreateRecipe("BigHealthPotion", bigPotion, potion, 2, gem, 1);
        CraftingRecipe gemRingRecipe = CreateRecipe("GemRing", gemRing, goldBar, 1, gem, 1);
        AssetDatabase.SaveAssets();

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("CraftingSceneSetup: no Player found in " + ScenePath);
            return;
        }

        CraftingGrid craftingGrid = InventorySceneSetup.AddComponentOnce<CraftingGrid>(player);
        AssignRecipes(craftingGrid, goldBarRecipe, bigPotionRecipe, gemRingRecipe);

        if (!CreateExtraPickups(coin, gem))
        {
            return;
        }

        GameObject hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null)
        {
            Debug.LogError("CraftingSceneSetup: no HUDCanvas found in " + ScenePath);
            return;
        }

        CreateEventSystem();
        InventorySceneSetup.AddComponentOnce<GraphicRaycaster>(hudCanvas);
        Sprite arrowSprite = InventorySceneSetup.SaveSprite(CraftingIconsFolder + "/TwoWayArrow.png", DrawTwoWayArrow());
        ItemDragUI dragIcon = CreateDragIcon(hudCanvas.transform);
        CraftingPanelUI panelUI = CreateCraftingUI(hudCanvas.transform, player, craftingGrid, dragIcon, arrowSprite);
        // The drag icon must be the last child of HUDCanvas so it is drawn on top of everything.
        dragIcon.transform.SetAsLastSibling();
        if (!WireHotbar(hudCanvas.transform, craftingGrid, panelUI, dragIcon))
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("CraftingSceneSetup: done");
    }

    // ---------------------------------------------------------------
    // Folders, items and recipes
    // ---------------------------------------------------------------

    private static void CreateFolders()
    {
        CreateFolder("Assets", "Crafting");
        CreateFolder(CraftingFolder, "Recipes");
        CreateFolder(CraftingFolder, "Icons");
    }

    private static void CreateFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static ItemData LoadItem(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<ItemData>(ItemsFolder + "/" + fileName + ".asset");
    }

    // Finds the recipe asset or makes a new one, then fills in one or two ingredients
    // and the result. Pass null as the second item for a recipe with one ingredient.
    // Reusing the asset keeps its GUID, so the CraftingGrid reference stays connected.
    private static CraftingRecipe CreateRecipe(string fileName, ItemData result,
        ItemData firstItem, int firstCount, ItemData secondItem, int secondCount)
    {
        string path = RecipesFolder + "/" + fileName + ".asset";
        CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(path);
        if (recipe == null)
        {
            recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
            AssetDatabase.CreateAsset(recipe, path);
        }

        SerializedObject serializedRecipe = new SerializedObject(recipe);
        SerializedProperty ingredients = serializedRecipe.FindProperty("ingredients");
        ingredients.arraySize = secondItem == null ? 1 : 2;
        SetIngredient(ingredients.GetArrayElementAtIndex(0), firstItem, firstCount);
        if (secondItem != null)
        {
            SetIngredient(ingredients.GetArrayElementAtIndex(1), secondItem, secondCount);
        }
        serializedRecipe.FindProperty("result").objectReferenceValue = result;
        serializedRecipe.FindProperty("resultCount").intValue = 1;
        serializedRecipe.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    private static void SetIngredient(SerializedProperty ingredient, ItemData item, int count)
    {
        ingredient.FindPropertyRelative("item").objectReferenceValue = item;
        ingredient.FindPropertyRelative("count").intValue = count;
    }

    private static void AssignRecipes(CraftingGrid craftingGrid, CraftingRecipe first, CraftingRecipe second, CraftingRecipe third)
    {
        SerializedObject serializedGrid = new SerializedObject(craftingGrid);
        SerializedProperty recipes = serializedGrid.FindProperty("recipes");
        recipes.arraySize = 3;
        recipes.GetArrayElementAtIndex(0).objectReferenceValue = first;
        recipes.GetArrayElementAtIndex(1).objectReferenceValue = second;
        recipes.GetArrayElementAtIndex(2).objectReferenceValue = third;
        serializedGrid.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------
    // Extra pickups: 4 more coins (9 in total) and 2 more gems (3 in total),
    // so every recipe can be tried in one play session.
    // ---------------------------------------------------------------

    private static bool CreateExtraPickups(ItemData coin, ItemData gem)
    {
        GameObject root = GameObject.Find(PickupsRootName);
        if (root == null)
        {
            Debug.LogError("CraftingSceneSetup: no " + PickupsRootName + " found. Run Tools > Setup Inventory Scene first.");
            return false;
        }

        Material coinMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Coin.mat");
        Material gemMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialsFolder + "/Gem.mat");

        // Same shapes as the Inventory Basics pickups.
        Vector3 coinRotation = new Vector3(90f, 0f, 0f);
        Vector3 coinScale = new Vector3(0.5f, 0.04f, 0.5f);
        Vector3[] coinPositions =
        {
            new Vector3(-3f, 0.35f, 1.6f),
            new Vector3(-3f, 0.35f, -1.8f),
            new Vector3(-5f, 0.35f, -1.8f),
            new Vector3(-6f, 0.35f, 0.8f)
        };
        for (int i = 0; i < coinPositions.Length; i++)
        {
            ReplacePickup(root.transform, "Coin_" + (6 + i), PrimitiveType.Cylinder, coin, coinMaterial,
                coinPositions[i], coinRotation, coinScale);
        }

        Vector3 gemRotation = new Vector3(45f, 0f, 45f);
        Vector3 gemScale = new Vector3(0.35f, 0.35f, 0.35f);
        ReplacePickup(root.transform, "Gem_2", PrimitiveType.Cube, gem, gemMaterial,
            new Vector3(5.5f, 0.5f, -0.5f), gemRotation, gemScale);
        ReplacePickup(root.transform, "Gem_3", PrimitiveType.Cube, gem, gemMaterial,
            new Vector3(3.5f, 0.5f, -2.5f), gemRotation, gemScale);
        return true;
    }

    // Deletes a pickup with this name (if it exists) and makes it again: no duplicates.
    private static void ReplacePickup(Transform root, string objectName, PrimitiveType shape, ItemData item,
        Material material, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        Transform old = root.Find(objectName);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        InventorySceneSetup.CreatePickup(root, objectName, shape, item, material, position, rotation, scale);
    }

    // ---------------------------------------------------------------
    // EventSystem: UI clicks and drag-and-drop need one in the scene
    // ---------------------------------------------------------------

    // Finds or makes the EventSystem and gives it an InputSystemUIInputModule
    // (never the old StandaloneInputModule, which reads the old Input Manager).
    // The module uses the UI map of our one input asset, InputSystem_Actions.
    private static void CreateEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        GameObject eventSystemObject;
        if (eventSystem == null)
        {
            eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
        }
        else
        {
            eventSystemObject = eventSystem.gameObject;
        }

        StandaloneInputModule oldModule = eventSystemObject.GetComponent<StandaloneInputModule>();
        if (oldModule != null)
        {
            Object.DestroyImmediate(oldModule);
        }

        InputSystemUIInputModule module = InventorySceneSetup.AddComponentOnce<InputSystemUIInputModule>(eventSystemObject);
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath);

        module.actionsAsset = inputAsset;
        module.point = FindUIAction(subAssets, "Point");
        module.leftClick = FindUIAction(subAssets, "Click");
        module.rightClick = FindUIAction(subAssets, "RightClick");
        module.middleClick = FindUIAction(subAssets, "MiddleClick");
        module.scrollWheel = FindUIAction(subAssets, "ScrollWheel");
        module.move = FindUIAction(subAssets, "Navigate");
        module.submit = FindUIAction(subAssets, "Submit");
        module.cancel = FindUIAction(subAssets, "Cancel");
        module.trackedDevicePosition = FindUIAction(subAssets, "TrackedDevicePosition");
        module.trackedDeviceOrientation = FindUIAction(subAssets, "TrackedDeviceOrientation");
        EditorUtility.SetDirty(module);
    }

    // Unity saves one InputActionReference per action inside the .inputactions file.
    // We look for the one of the UI map with this name, the same thing the Inspector does.
    private static InputActionReference FindUIAction(Object[] subAssets, string actionName)
    {
        for (int i = 0; i < subAssets.Length; i++)
        {
            InputActionReference reference = subAssets[i] as InputActionReference;
            if (reference != null && reference.action != null &&
                reference.action.actionMap.name == "UI" && reference.action.name == actionName)
            {
                return reference;
            }
        }

        Debug.LogError("CraftingSceneSetup: UI action " + actionName + " not found in " + InputAssetPath);
        return null;
    }

    // ---------------------------------------------------------------
    // Crafting UI
    // ---------------------------------------------------------------

    // HUDCanvas
    //   Crafting              CraftingPanelUI (always active, fills the screen, no image)
    //     CraftButton         Button "Craft (C)", bottom-right
    //     CraftingPanel       Image, centred, starts hidden
    //       Title             TMP "Crafting"
    //       Grid              Grid Layout Group 3x3
    //         GridSlot_1..9   Image (background), CraftingSlotUI > Icon, Count
    //       Arrow             Image (two-way arrow)
    //       ResultLabel       TMP "Result"
    //       ResultSlot        Image (background), CraftingSlotUI > Icon, Count
    //       Hint              TMP help line
    private static CraftingPanelUI CreateCraftingUI(Transform hudCanvas, GameObject player, CraftingGrid craftingGrid,
        ItemDragUI dragIcon, Sprite arrowSprite)
    {
        Transform oldRoot = hudCanvas.Find(CraftingRootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot.gameObject);
        }

        GameObject root = InventorySceneSetup.NewUIObject(CraftingRootName, hudCanvas);
        InventorySceneSetup.StretchWithPadding(root.GetComponent<RectTransform>(), 0f);

        Button craftButton = CreateCraftButton(root.transform);

        GameObject panel = InventorySceneSetup.NewUIObject("CraftingPanel", root.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = RoundedSprite();
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(0.08f, 0.08f, 0.10f, 0.9f);

        TMP_Text title = InventorySceneSetup.CreateText("Title", panel.transform, "Crafting", 32f,
            TextAlignmentOptions.Center, Color.white);
        title.fontStyle = FontStyles.Bold;
        PlaceAtTop(title.rectTransform, 60f);

        // Content row, measured from the panel centre: grid (260) | gap | arrow (80) | gap | result (90).
        GameObject grid = InventorySceneSetup.NewUIObject("Grid", panel.transform);
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        float gridWidth = 3 * SlotSize + 2 * SlotSpacing;
        gridRect.sizeDelta = new Vector2(gridWidth, gridWidth);
        gridRect.anchoredPosition = new Vector2(-125f, 0f);
        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(SlotSize, SlotSize);
        gridLayout.spacing = new Vector2(SlotSpacing, SlotSpacing);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;

        List<CraftingSlotUI> gridViews = new List<CraftingSlotUI>();
        for (int i = 0; i < GridSlotCount; i++)
        {
            gridViews.Add(CreateCraftingSlot("GridSlot_" + (i + 1), grid.transform, SlotSize, false, i,
                craftingGrid, dragIcon));
        }

        GameObject arrow = InventorySceneSetup.NewUIObject("Arrow", panel.transform);
        RectTransform arrowRect = arrow.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(SlotSize, SlotSize);
        arrowRect.anchoredPosition = new Vector2(85f, 0f);
        Image arrowImage = arrow.AddComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.preserveAspect = true;
        arrowImage.raycastTarget = false;

        TMP_Text resultLabel = InventorySceneSetup.CreateText("ResultLabel", panel.transform, "Result", 20f,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
        resultLabel.rectTransform.sizeDelta = new Vector2(120f, 30f);
        resultLabel.rectTransform.anchoredPosition = new Vector2(210f, 65f);

        CraftingSlotUI resultView = CreateCraftingSlot("ResultSlot", panel.transform, ResultSlotSize, true, 0,
            craftingGrid, dragIcon);
        resultView.GetComponent<RectTransform>().anchoredPosition = new Vector2(210f, 0f);

        TMP_Text hint = InventorySceneSetup.CreateText("Hint", panel.transform,
            "Drag items between the hotbar and the panel. Take the result to craft it.\n" +
            "Drop a crafted item on Result to take it apart.", 18f,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.7f));
        PlaceAtBottom(hint.rectTransform, 55f);

        CraftingPanelUI panelUI = root.AddComponent<CraftingPanelUI>();
        SerializedObject serializedPanel = new SerializedObject(panelUI);
        serializedPanel.FindProperty("panel").objectReferenceValue = panel;
        serializedPanel.FindProperty("player").objectReferenceValue = player;
        serializedPanel.FindProperty("resultSlotView").objectReferenceValue = resultView;
        SerializedProperty gridSlotViews = serializedPanel.FindProperty("gridSlotViews");
        gridSlotViews.arraySize = GridSlotCount;
        for (int i = 0; i < GridSlotCount; i++)
        {
            gridSlotViews.GetArrayElementAtIndex(i).objectReferenceValue = gridViews[i];
        }
        serializedPanel.ApplyModifiedPropertiesWithoutUndo();

        // Clicking the button calls CraftingPanelUI.TogglePanel. This is the same as
        // adding an entry under Button > On Click in the Inspector.
        UnityEventTools.AddPersistentListener(craftButton.onClick, panelUI.TogglePanel);

        panel.SetActive(false);
        return panelUI;
    }

    private static Button CreateCraftButton(Transform parent)
    {
        GameObject buttonObject = InventorySceneSetup.NewUIObject("CraftButton", parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-30f, 30f);
        buttonRect.sizeDelta = new Vector2(180f, 60f);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = RoundedSprite();
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = new Color(0.15f, 0.15f, 0.18f, 0.85f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        // No keyboard navigation: WASD is also the UI "Navigate" action and must not move a selection.
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        TMP_Text label = InventorySceneSetup.CreateText("Label", buttonObject.transform, "Craft (C)", 28f,
            TextAlignmentOptions.Center, Color.white);
        label.fontStyle = FontStyles.Bold;
        InventorySceneSetup.StretchWithPadding(label.rectTransform, 0f);
        return button;
    }

    // One crafting box: a dark rounded background that receives drops, an icon and a count.
    // Same look as a hotbar box. The result box is a little bigger and gold-tinted.
    private static CraftingSlotUI CreateCraftingSlot(string objectName, Transform parent, float size,
        bool isResultSlot, int gridIndex, CraftingGrid craftingGrid, ItemDragUI dragIcon)
    {
        GameObject slot = InventorySceneSetup.NewUIObject(objectName, parent);
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
        Image background = slot.AddComponent<Image>();
        background.sprite = RoundedSprite();
        background.type = Image.Type.Sliced;
        if (isResultSlot)
        {
            background.color = new Color(0.35f, 0.28f, 0.05f, 0.8f);
        }
        else
        {
            background.color = new Color(0f, 0f, 0f, 0.5f);
        }
        // The background must be hit by the mouse, otherwise items can't be dropped on it.
        background.raycastTarget = true;

        GameObject icon = InventorySceneSetup.NewUIObject("Icon", slot.transform);
        InventorySceneSetup.StretchWithPadding(icon.GetComponent<RectTransform>(), 12f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;

        TMP_Text countText = InventorySceneSetup.CreateText("Count", slot.transform, "", 24f,
            TextAlignmentOptions.BottomRight, Color.white);
        countText.fontStyle = FontStyles.Bold;
        InventorySceneSetup.StretchWithPadding(countText.rectTransform, 5f);

        CraftingSlotUI slotView = slot.AddComponent<CraftingSlotUI>();
        SerializedObject serializedSlot = new SerializedObject(slotView);
        serializedSlot.FindProperty("iconImage").objectReferenceValue = iconImage;
        serializedSlot.FindProperty("countText").objectReferenceValue = countText;
        serializedSlot.FindProperty("isResultSlot").boolValue = isResultSlot;
        serializedSlot.FindProperty("gridIndex").intValue = gridIndex;
        serializedSlot.FindProperty("craftingGrid").objectReferenceValue = craftingGrid;
        serializedSlot.FindProperty("dragIcon").objectReferenceValue = dragIcon;
        serializedSlot.ApplyModifiedPropertiesWithoutUndo();
        return slotView;
    }

    // ---------------------------------------------------------------
    // Drag and drop: the icon that follows the mouse, and the hotbar
    // ---------------------------------------------------------------

    // A 64x64 image with ItemDragUI. Raycast Target is off so it never hides the slot below the mouse.
    private static ItemDragUI CreateDragIcon(Transform hudCanvas)
    {
        Transform old = hudCanvas.Find("DragIcon");
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        GameObject dragObject = InventorySceneSetup.NewUIObject("DragIcon", hudCanvas);
        dragObject.GetComponent<RectTransform>().sizeDelta = new Vector2(64f, 64f);
        Image image = dragObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        return dragObject.AddComponent<ItemDragUI>();
    }

    // The hotbar was built by InventorySceneSetup. Here we only switch on Raycast Target
    // for each box's background (so it can be dragged and dropped on) and fill in the
    // new Drag and Drop fields of HotbarSlotUI.
    private static bool WireHotbar(Transform hudCanvas, CraftingGrid craftingGrid, CraftingPanelUI panelUI, ItemDragUI dragIcon)
    {
        HotbarUI hotbar = hudCanvas.GetComponentInChildren<HotbarUI>(true);
        if (hotbar == null)
        {
            Debug.LogError("CraftingSceneSetup: no HotbarUI found. Run Tools > Setup Inventory Scene first.");
            return false;
        }

        SerializedProperty slotViews = new SerializedObject(hotbar).FindProperty("slotViews");
        for (int i = 0; i < slotViews.arraySize; i++)
        {
            HotbarSlotUI slotView = slotViews.GetArrayElementAtIndex(i).objectReferenceValue as HotbarSlotUI;
            if (slotView == null)
            {
                continue;
            }

            SerializedObject serializedSlot = new SerializedObject(slotView);
            Image background = serializedSlot.FindProperty("background").objectReferenceValue as Image;
            if (background != null)
            {
                background.raycastTarget = true;
                EditorUtility.SetDirty(background);
            }

            serializedSlot.FindProperty("slotIndex").intValue = i;
            serializedSlot.FindProperty("craftingGrid").objectReferenceValue = craftingGrid;
            serializedSlot.FindProperty("craftingPanel").objectReferenceValue = panelUI;
            serializedSlot.FindProperty("dragIcon").objectReferenceValue = dragIcon;
            serializedSlot.ApplyModifiedPropertiesWithoutUndo();
        }

        return true;
    }

    private static Sprite RoundedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    // Stretches across the top of the parent, with a fixed height.
    private static void PlaceAtTop(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, height);
    }

    // Stretches across the bottom of the parent, with a fixed height and a small side margin.
    private static void PlaceAtBottom(RectTransform rect, float height)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-20f, height);
    }

    // ---------------------------------------------------------------
    // Icon drawing (reuses the flat-shape helpers of InventorySceneSetup)
    // ---------------------------------------------------------------

    // Two arrows, the top one pointing right (crafting) and the bottom one pointing left (uncrafting).
    private static Texture2D DrawTwoWayArrow()
    {
        Color color = new Color(1f, 1f, 1f, 0.85f);

        Texture2D texture = InventorySceneSetup.NewTransparentTexture();
        InventorySceneSetup.DrawRect(texture, 6f, 39f, 44f, 45f, color);
        DrawArrowHead(texture, 58f, 42f, 16f, 11f, 1f, color);
        InventorySceneSetup.DrawRect(texture, 20f, 19f, 58f, 25f, color);
        DrawArrowHead(texture, 6f, 22f, 16f, 11f, -1f, color);
        texture.Apply();
        return texture;
    }

    // A triangle with its tip at (tipX, centerY). direction 1 points right, -1 points left.
    private static void DrawArrowHead(Texture2D texture, float tipX, float centerY, float length, float halfHeight,
        float direction, Color color)
    {
        int size = InventorySceneSetup.IconSize;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // "along" is 0 at the tip and grows towards the back of the arrow head.
                float along = (tipX - (x + 0.5f)) * direction;
                float allowedHalfHeight = halfHeight * along / length;
                float outsideTip = -along;
                float outsideBack = along - length;
                float outsideSide = Mathf.Abs(y + 0.5f - centerY) - allowedHalfHeight;
                float distanceToEdge = Mathf.Max(Mathf.Max(outsideTip, outsideBack), outsideSide);
                InventorySceneSetup.BlendPixel(texture, x, y, color, distanceToEdge);
            }
        }
    }

    private static Texture2D DrawGoldBarIcon()
    {
        Color darkGold = new Color(0.72f, 0.50f, 0.08f);
        Color gold = new Color(1.00f, 0.80f, 0.20f);
        Color lightGold = new Color(1.00f, 0.92f, 0.55f);

        Texture2D texture = InventorySceneSetup.NewTransparentTexture();
        DrawTrapezoid(texture, 8f, 56f, 14f, 50f, 16f, 40f, darkGold);
        DrawTrapezoid(texture, 12f, 52f, 18f, 46f, 20f, 37f, gold);
        DrawTrapezoid(texture, 18f, 46f, 22f, 42f, 40f, 48f, lightGold);
        InventorySceneSetup.DrawRect(texture, 20f, 26f, 32f, 29f, lightGold);
        texture.Apply();
        return texture;
    }

    private static Texture2D DrawBigPotionIcon()
    {
        Color glass = new Color(0.80f, 0.85f, 0.90f);
        Color liquid = new Color(0.75f, 0.05f, 0.40f);
        Color cork = new Color(0.55f, 0.35f, 0.20f);
        Color gold = new Color(1.00f, 0.80f, 0.20f);

        Texture2D texture = InventorySceneSetup.NewTransparentTexture();
        InventorySceneSetup.DrawRect(texture, 25f, 53f, 39f, 62f, cork);
        InventorySceneSetup.DrawRect(texture, 24f, 47f, 40f, 51f, gold);
        InventorySceneSetup.DrawRect(texture, 26f, 38f, 38f, 48f, glass);
        InventorySceneSetup.DrawCircle(texture, 32f, 23f, 23f, glass);
        InventorySceneSetup.DrawCircle(texture, 32f, 23f, 20f, liquid);
        InventorySceneSetup.DrawRect(texture, 28.5f, 12f, 35.5f, 34f, Color.white);
        InventorySceneSetup.DrawRect(texture, 21f, 19.5f, 43f, 26.5f, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D DrawGemRingIcon()
    {
        Color darkGold = new Color(0.72f, 0.50f, 0.08f);
        Color gold = new Color(1.00f, 0.80f, 0.20f);

        Texture2D texture = InventorySceneSetup.NewTransparentTexture();
        DrawRing(texture, 32f, 24f, 17f, 5f, darkGold);
        DrawRing(texture, 32f, 24f, 17f, 3f, gold);
        DrawDiamondGem(texture, 32f, 46f);
        texture.Apply();
        return texture;
    }

    // The same blue gem as the Gem icon, but smaller, to sit on top of the ring.
    private static void DrawDiamondGem(Texture2D texture, float centerX, float centerY)
    {
        InventorySceneSetup.DrawDiamond(texture, centerX, centerY, 12f, 14f, new Color(0.10f, 0.45f, 0.85f));
        InventorySceneSetup.DrawDiamond(texture, centerX, centerY + 2f, 7f, 8f, new Color(0.35f, 0.80f, 1.00f));
        InventorySceneSetup.DrawCircle(texture, centerX - 3f, centerY + 5f, 2f, new Color(0.90f, 1f, 1f));
    }

    // A circle outline: pixels close to the circle's edge (within halfThickness) are filled.
    private static void DrawRing(Texture2D texture, float centerX, float centerY, float radius, float halfThickness, Color color)
    {
        int size = InventorySceneSetup.IconSize;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - centerX;
                float dy = y + 0.5f - centerY;
                float distanceToEdge = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - radius) - halfThickness;
                InventorySceneSetup.BlendPixel(texture, x, y, color, distanceToEdge);
            }
        }
    }

    // A shape with a wide bottom edge and a narrower top edge, like the side of a gold bar.
    private static void DrawTrapezoid(Texture2D texture, float bottomMinX, float bottomMaxX,
        float topMinX, float topMaxX, float minY, float maxY, Color color)
    {
        int size = InventorySceneSetup.IconSize;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float t = Mathf.Clamp01((py - minY) / (maxY - minY));
                float minX = Mathf.Lerp(bottomMinX, topMinX, t);
                float maxX = Mathf.Lerp(bottomMaxX, topMaxX, t);
                float outsideX = Mathf.Max(minX - px, px - maxX);
                float outsideY = Mathf.Max(minY - py, py - maxY);
                InventorySceneSetup.BlendPixel(texture, x, y, color, Mathf.Max(outsideX, outsideY));
            }
        }
    }
}
