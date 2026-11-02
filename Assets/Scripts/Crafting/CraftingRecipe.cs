using System.Collections.Generic;
using UnityEngine;

// A recipe says: "these ingredients make this result".
// Like ItemData, it is a ScriptableObject, so every recipe is an asset file.
// Adding a new recipe to the game means making a new asset, not writing new code.
// Our recipes are "shapeless": only WHICH items and HOW MANY count,
// not where they sit in the 3x3 grid.
// Right click in the Project window > Create > Crafting > Recipe to make a new one.
[CreateAssetMenu(fileName = "NewRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    // What has to be in the crafting grid, for example "4 Coin",
    // or "2 Health Potion" and "1 Gem".
    [Header("Ingredients")]
    [SerializeField] private List<CraftingIngredient> ingredients = new List<CraftingIngredient>();

    // What the player gets, and how many.
    [Header("Result")]
    [SerializeField] private ItemData result;
    [SerializeField] private int resultCount = 1;

    // Other scripts can read the recipe, but only the asset in the Inspector can change it.
    public List<CraftingIngredient> Ingredients
    {
        get { return ingredients; }
    }

    public ItemData Result
    {
        get { return result; }
    }

    public int ResultCount
    {
        get { return resultCount; }
    }

    // Adds up all the ingredient counts. "2 Health Potion + 1 Gem" gives 3.
    // The crafting grid uses this to check there are no extra items in the grid.
    public int GetTotalIngredientCount()
    {
        int total = 0;

        for (int i = 0; i < ingredients.Count; i++)
        {
            total += ingredients[i].count;
        }

        return total;
    }

    // Writes the ingredients as text for the Console, for example "2 Health Potion + 1 Gem".
    public string DescribeIngredients()
    {
        string text = "";

        for (int i = 0; i < ingredients.Count; i++)
        {
            if (i > 0)
            {
                text += " + ";
            }

            text += ingredients[i].count + " " + ingredients[i].item.ItemName;
        }

        return text;
    }
}
