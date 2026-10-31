using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Teacher tool: builds everything the Inventory Basics topic needs in the
// ThirdPerson scene, so nobody has to hand-edit the scene file.
// It creates the item icons, the ItemData assets, the item materials, adds the
// Inventory and PlayerItemUser to the Player, places item pickups near the player start and builds
// the hotbar UI on the HUDCanvas.
// It is safe to run more than once: it reuses or replaces what it made before.
// Run it from the menu Tools > Setup Inventory Scene, or in batchmode with
// -executeMethod InventorySceneSetup.Run
public static class InventorySceneSetup
{
    private const string ScenePath = "Assets/Scenes/ThirdPerson.unity";
    private const string ItemsFolder = "Assets/Items";
    private const string IconsFolder = "Assets/Items/Icons";
    private const string MaterialsFolder = "Assets/Items/Materials";
    private const string PickupsRootName = "ItemPickups";
    private const string HotbarName = "Hotbar";
    private const int HotbarSlotCount = 6;
    private const float HotbarSlotSize = 80f;
    private const float HotbarSpacing = 10f;
    private const float HotbarBottomMargin = 30f;
    private const int IconSize = 64;

    [MenuItem("Tools/Setup Inventory Scene")]
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
        // in memory, so the items are created after this.
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        CreateFolders();

        ItemData coin = CreateItem("Coin", "Coin", 99, 0, DrawCoinIcon());
        ItemData potion = CreateItem("HealthPotion", "Health Potion", 5, 25, DrawPotionIcon());
        ItemData gem = CreateItem("Gem", "Gem", 1, 0, DrawGemIcon());
        AssetDatabase.SaveAssets();

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("InventorySceneSetup: no Player found in " + ScenePath);
            return;
        }

        Inventory inventory = AddComponentOnce<Inventory>(player);
        AddComponentOnce<PlayerItemUser>(player);

        Material coinMaterial = CreateMaterial("Coin", new Color(1.00f, 0.78f, 0.15f), 0.8f, 0.7f);
        Material potionMaterial = CreateMaterial("HealthPotion", new Color(0.90f, 0.12f, 0.20f), 0f, 0.8f);
        Material gemMaterial = CreateMaterial("Gem", new Color(0.20f, 0.60f, 1.00f), 0.3f, 0.9f);
        CreatePickups(coin, potion, gem, coinMaterial, potionMaterial, gemMaterial);

        GameObject hudCanvas = GameObject.Find("HUDCanvas");
        if (hudCanvas == null)
        {
            Debug.LogError("InventorySceneSetup: no HUDCanvas found in " + ScenePath);
            return;
        }
        CreateHotbar(hudCanvas.transform, inventory);

        EditorSceneManager.MarkSceneDirty(player.scene);
        EditorSceneManager.SaveScene(player.scene);
        AssetDatabase.SaveAssets();

        Debug.Log("InventorySceneSetup: done");
    }

    // ---------------------------------------------------------------
    // Folders and assets
    // ---------------------------------------------------------------

    private static void CreateFolders()
    {
        CreateFolder("Assets", "Items");
        CreateFolder(ItemsFolder, "Icons");
        CreateFolder(ItemsFolder, "Materials");
    }

    private static void CreateFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    // Finds the ItemData asset or makes a new one, then fills in its values.
    // Reusing the asset keeps its GUID, so scene references stay connected.
    private static ItemData CreateItem(string fileName, string itemName, int maxStackSize, int healAmount, Texture2D iconTexture)
    {
        Sprite icon = SaveIcon(fileName, iconTexture);

        string path = ItemsFolder + "/" + fileName + ".asset";
        ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }

        // ItemData fields are private, so we set them the same way the Inspector does.
        SerializedObject serializedItem = new SerializedObject(item);
        serializedItem.FindProperty("itemName").stringValue = itemName;
        serializedItem.FindProperty("icon").objectReferenceValue = icon;
        serializedItem.FindProperty("maxStackSize").intValue = maxStackSize;
        serializedItem.FindProperty("healAmount").intValue = healAmount;
        serializedItem.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(item);
        return item;
    }

    // Writes the texture as a PNG and imports it as a Sprite (2D and UI).
    private static Sprite SaveIcon(string fileName, Texture2D texture)
    {
        string path = IconsFolder + "/" + fileName + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // Finds the material or makes a new URP Lit one, then sets its color and shine.
    private static Material CreateMaterial(string fileName, Color color, float metallic, float smoothness)
    {
        string path = MaterialsFolder + "/" + fileName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------------------------------------------------------------
    // Item pickups in the scene
    // ---------------------------------------------------------------

    // Deletes the old ItemPickups group (if any) and builds it again,
    // so running the tool twice never makes duplicates.
    private static void CreatePickups(ItemData coin, ItemData potion, ItemData gem,
        Material coinMaterial, Material potionMaterial, Material gemMaterial)
    {
        GameObject oldRoot = GameObject.Find(PickupsRootName);
        if (oldRoot != null)
        {
            Object.DestroyImmediate(oldRoot);
        }

        Transform root = new GameObject(PickupsRootName).transform;

        // Coins: thin cylinders standing on their edge, in a group on the left.
        Vector3 coinRotation = new Vector3(90f, 0f, 0f);
        Vector3 coinScale = new Vector3(0.5f, 0.04f, 0.5f);
        Vector3[] coinPositions =
        {
            new Vector3(-3f, 0.35f, 0f),
            new Vector3(-4f, 0.35f, 0.8f),
            new Vector3(-5f, 0.35f, 1.6f),
            new Vector3(-4f, 0.35f, -1f),
            new Vector3(-5f, 0.35f, -0.2f)
        };
        for (int i = 0; i < coinPositions.Length; i++)
        {
            CreatePickup(root, "Coin_" + (i + 1), PrimitiveType.Cylinder, coin, coinMaterial,
                coinPositions[i], coinRotation, coinScale);
        }

        // Health potions: small capsules on the right.
        Vector3 potionScale = new Vector3(0.3f, 0.3f, 0.3f);
        CreatePickup(root, "HealthPotion_1", PrimitiveType.Capsule, potion, potionMaterial,
            new Vector3(3f, 0.3f, 0f), Vector3.zero, potionScale);
        CreatePickup(root, "HealthPotion_2", PrimitiveType.Capsule, potion, potionMaterial,
            new Vector3(4f, 0.3f, 1f), Vector3.zero, potionScale);

        // Gem: a cube turned onto its corner so it looks like a diamond.
        CreatePickup(root, "Gem_1", PrimitiveType.Cube, gem, gemMaterial,
            new Vector3(4.5f, 0.5f, -1.5f), new Vector3(45f, 0f, 45f), new Vector3(0.35f, 0.35f, 0.35f));
    }

    private static void CreatePickup(Transform root, string objectName, PrimitiveType shape, ItemData item,
        Material material, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        GameObject pickup = GameObject.CreatePrimitive(shape);
        pickup.name = objectName;
        pickup.transform.SetParent(root);
        pickup.transform.position = position;
        pickup.transform.eulerAngles = rotation;
        pickup.transform.localScale = scale;
        pickup.GetComponent<MeshRenderer>().sharedMaterial = material;

        ItemPickup itemPickup = pickup.AddComponent<ItemPickup>();
        SerializedObject serializedPickup = new SerializedObject(itemPickup);
        serializedPickup.FindProperty("itemData").objectReferenceValue = item;
        serializedPickup.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------
    // Hotbar UI
    // ---------------------------------------------------------------

    // HUDCanvas
    //   Hotbar        HotbarUI, Horizontal Layout Group, bottom-centre
    //     Slot_1      Image (background), HotbarSlotUI
    //       Icon      Image
    //       Count     TMP text, bottom right
    //       KeyLabel  TMP text "1", top left
    //     Slot_2 ... Slot_6
    private static void CreateHotbar(Transform hudCanvas, Inventory inventory)
    {
        Transform oldHotbar = hudCanvas.Find(HotbarName);
        if (oldHotbar != null)
        {
            Object.DestroyImmediate(oldHotbar.gameObject);
        }

        GameObject hotbar = NewUIObject(HotbarName, hudCanvas);
        RectTransform hotbarRect = hotbar.GetComponent<RectTransform>();
        hotbarRect.anchorMin = new Vector2(0.5f, 0f);
        hotbarRect.anchorMax = new Vector2(0.5f, 0f);
        hotbarRect.pivot = new Vector2(0.5f, 0f);
        hotbarRect.anchoredPosition = new Vector2(0f, HotbarBottomMargin);
        float width = HotbarSlotCount * HotbarSlotSize + (HotbarSlotCount - 1) * HotbarSpacing;
        hotbarRect.sizeDelta = new Vector2(width, HotbarSlotSize);

        HorizontalLayoutGroup layout = hotbar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = HotbarSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        HotbarUI hotbarUI = hotbar.AddComponent<HotbarUI>();
        SerializedObject serializedHotbar = new SerializedObject(hotbarUI);
        serializedHotbar.FindProperty("inventory").objectReferenceValue = inventory;
        SerializedProperty slotViews = serializedHotbar.FindProperty("slotViews");
        slotViews.arraySize = HotbarSlotCount;

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            HotbarSlotUI slotView = CreateHotbarSlot(hotbar.transform, i + 1);
            slotViews.GetArrayElementAtIndex(i).objectReferenceValue = slotView;
        }

        serializedHotbar.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HotbarSlotUI CreateHotbarSlot(Transform hotbar, int slotNumber)
    {
        Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        GameObject slot = NewUIObject("Slot_" + slotNumber, hotbar);
        slot.GetComponent<RectTransform>().sizeDelta = new Vector2(HotbarSlotSize, HotbarSlotSize);
        Image background = slot.AddComponent<Image>();
        background.sprite = roundedSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(0f, 0f, 0f, 0.5f);
        background.raycastTarget = false;

        GameObject icon = NewUIObject("Icon", slot.transform);
        StretchWithPadding(icon.GetComponent<RectTransform>(), 12f);
        Image iconImage = icon.AddComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.enabled = false;

        TMP_Text countText = CreateText("Count", slot.transform, "", 24f, TextAlignmentOptions.BottomRight, Color.white);
        countText.fontStyle = FontStyles.Bold;
        StretchWithPadding(countText.rectTransform, 5f);

        TMP_Text keyLabel = CreateText("KeyLabel", slot.transform, slotNumber.ToString(), 16f,
            TextAlignmentOptions.TopLeft, new Color(1f, 1f, 1f, 0.7f));
        StretchWithPadding(keyLabel.rectTransform, 5f);

        HotbarSlotUI slotView = slot.AddComponent<HotbarSlotUI>();
        SerializedObject serializedSlot = new SerializedObject(slotView);
        serializedSlot.FindProperty("background").objectReferenceValue = background;
        serializedSlot.FindProperty("iconImage").objectReferenceValue = iconImage;
        serializedSlot.FindProperty("countText").objectReferenceValue = countText;
        serializedSlot.ApplyModifiedPropertiesWithoutUndo();

        return slotView;
    }

    private static TMP_Text CreateText(string objectName, Transform parent, string text, float fontSize,
        TextAlignmentOptions alignment, Color color)
    {
        GameObject textObject = NewUIObject(objectName, parent);
        TextMeshProUGUI tmpText = textObject.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.alignment = alignment;
        tmpText.color = color;
        tmpText.raycastTarget = false;
        return tmpText;
    }

    private static GameObject NewUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.layer = LayerMask.NameToLayer("UI");
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private static void StretchWithPadding(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static T AddComponentOnce<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            component = target.AddComponent<T>();
        }
        return component;
    }

    // ---------------------------------------------------------------
    // Icon drawing (flat shapes with soft, anti-aliased edges)
    // ---------------------------------------------------------------

    private static Texture2D DrawCoinIcon()
    {
        Texture2D texture = NewTransparentTexture();
        DrawCircle(texture, 32f, 32f, 27f, new Color(0.72f, 0.50f, 0.08f));
        DrawCircle(texture, 32f, 32f, 22f, new Color(1.00f, 0.80f, 0.20f));
        DrawRect(texture, 29f, 20f, 35f, 44f, new Color(0.80f, 0.58f, 0.10f));
        DrawCircle(texture, 24f, 41f, 4f, new Color(1f, 0.95f, 0.70f));
        texture.Apply();
        return texture;
    }

    private static Texture2D DrawPotionIcon()
    {
        Texture2D texture = NewTransparentTexture();
        DrawRect(texture, 26f, 50f, 38f, 60f, new Color(0.55f, 0.35f, 0.20f));
        DrawRect(texture, 27f, 38f, 37f, 51f, new Color(0.80f, 0.85f, 0.90f));
        DrawCircle(texture, 32f, 24f, 20f, new Color(0.80f, 0.85f, 0.90f));
        DrawCircle(texture, 32f, 24f, 17f, new Color(0.90f, 0.12f, 0.20f));
        DrawRect(texture, 29f, 15f, 35f, 33f, Color.white);
        DrawRect(texture, 23f, 21f, 41f, 27f, Color.white);
        texture.Apply();
        return texture;
    }

    private static Texture2D DrawGemIcon()
    {
        Texture2D texture = NewTransparentTexture();
        DrawDiamond(texture, 32f, 32f, 22f, 29f, new Color(0.10f, 0.45f, 0.85f));
        DrawDiamond(texture, 32f, 36f, 14f, 18f, new Color(0.35f, 0.80f, 1.00f));
        DrawCircle(texture, 27f, 42f, 3f, new Color(0.90f, 1f, 1f));
        texture.Apply();
        return texture;
    }

    private static Texture2D NewTransparentTexture()
    {
        Texture2D texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[IconSize * IconSize];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }
        texture.SetPixels(pixels);
        return texture;
    }

    private static void DrawCircle(Texture2D texture, float centerX, float centerY, float radius, Color color)
    {
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float dx = x + 0.5f - centerX;
                float dy = y + 0.5f - centerY;
                float distanceToEdge = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                BlendPixel(texture, x, y, color, distanceToEdge);
            }
        }
    }

    private static void DrawRect(Texture2D texture, float minX, float minY, float maxX, float maxY, Color color)
    {
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;
                float outsideX = Mathf.Max(minX - px, px - maxX);
                float outsideY = Mathf.Max(minY - py, py - maxY);
                BlendPixel(texture, x, y, color, Mathf.Max(outsideX, outsideY));
            }
        }
    }

    private static void DrawDiamond(Texture2D texture, float centerX, float centerY, float halfWidth, float halfHeight, Color color)
    {
        float edgeScale = Mathf.Min(halfWidth, halfHeight);
        for (int y = 0; y < IconSize; y++)
        {
            for (int x = 0; x < IconSize; x++)
            {
                float dx = Mathf.Abs(x + 0.5f - centerX) / halfWidth;
                float dy = Mathf.Abs(y + 0.5f - centerY) / halfHeight;
                BlendPixel(texture, x, y, color, (dx + dy - 1f) * edgeScale);
            }
        }
    }

    // distanceToEdge is negative inside the shape and positive outside.
    // Pixels right on the edge get partial coverage, which smooths the outline.
    private static void BlendPixel(Texture2D texture, int x, int y, Color color, float distanceToEdge)
    {
        float coverage = Mathf.Clamp01(0.5f - distanceToEdge);
        if (coverage <= 0f)
        {
            return;
        }

        Color below = texture.GetPixel(x, y);
        float alpha = coverage + below.a * (1f - coverage);
        Color result = (color * coverage + below * below.a * (1f - coverage)) / alpha;
        result.a = alpha;
        texture.SetPixel(x, y, result);
    }
}
