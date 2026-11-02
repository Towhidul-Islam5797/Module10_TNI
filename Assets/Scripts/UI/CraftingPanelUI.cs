using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Opens and closes the crafting panel, with the C key or the "Craft (C)" button.
//
// While the panel is open the player is in "menu mode":
// - the mouse cursor is free and visible, so the player can drag items, and
// - the Player scripts that read the mouse and keyboard are switched off,
//   so clicking does not punch and moving the mouse does not turn the camera.
// Closing the panel switches everything back on (unless the player has died).
//
// While open, it also copies the CraftingGrid into the 9 + 1 boxes every frame,
// the same idea as HotbarUI.
//
// This script sits on an object that is always active. The panel itself is a
// child that we show and hide. (If this script were on the hidden panel,
// its Update would stop and C could never open the panel again.)
public class CraftingPanelUI : MonoBehaviour
{
    // The panel we show and hide (a child of this object).
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    // The Player. We find its scripts in Awake, the same way PlayerDeath does.
    [Header("Player")]
    [SerializeField] private GameObject player;

    // The 9 grid boxes (in order, top-left to bottom-right) and the result box.
    [Header("Slots")]
    [SerializeField] private List<CraftingSlotUI> gridSlotViews = new List<CraftingSlotUI>();
    [SerializeField] private CraftingSlotUI resultSlotView;

    private CraftingGrid craftingGrid;
    private Health health;
    private Animator animator;
    private ThirdPersonController movement;
    private PlayerCombat combat;
    private PlayerInteractor interactor;
    private PlayerItemUser itemUser;
    private InputAction toggleAction;
    private bool isOpen;

    // Other scripts (the hotbar) ask this before they allow dragging.
    public bool IsOpen
    {
        get { return isOpen; }
    }

    // Find the Player's scripts and the ToggleCrafting action (key C) once, when the game starts.
    private void Awake()
    {
        craftingGrid = player.GetComponent<CraftingGrid>();
        health = player.GetComponent<Health>();
        animator = player.GetComponentInChildren<Animator>();
        movement = player.GetComponent<ThirdPersonController>();
        combat = player.GetComponent<PlayerCombat>();
        interactor = player.GetComponent<PlayerInteractor>();
        itemUser = player.GetComponent<PlayerItemUser>();
        toggleAction = InputSystem.actions.FindAction("ToggleCrafting");

        panel.SetActive(false);
    }

    private void Update()
    {
        HandleToggleInput();
        CloseIfPlayerDied();
        RefreshSlots();
    }

    // Pressing C opens the panel when it is closed, and closes it when it is open.
    private void HandleToggleInput()
    {
        if (toggleAction.WasPressedThisFrame())
        {
            TogglePanel();
        }
    }

    // The Craft button calls this too (Button > On Click in the Inspector).
    public void TogglePanel()
    {
        if (isOpen)
        {
            ClosePanel();
        }
        else
        {
            OpenPanel();
        }
    }

    // Shows the panel, frees the cursor and pauses the player's controls.
    private void OpenPanel()
    {
        if (health.IsDead)
        {
            Debug.Log("Can't craft while dead.");
            return;
        }

        isOpen = true;
        panel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetPlayerControls(false);

        // The movement script is off now, so nobody would reset the run animation.
        // We set Speed to 0 ourselves so the player stands still instead of running on the spot.
        animator.SetFloat("Speed", 0f);

        Debug.Log("Crafting panel opened");
    }

    // Gives the items in the grid back, hides the panel and locks the cursor again.
    // The player's controls only come back if the player is still alive.
    private void ClosePanel()
    {
        craftingGrid.ReturnEverythingToInventory();

        isOpen = false;
        panel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (!health.IsDead)
        {
            SetPlayerControls(true);
        }

        Debug.Log("Crafting panel closed");
    }

    // If the enemy kills the player while the panel is open, close it.
    private void CloseIfPlayerDied()
    {
        if (isOpen && health.IsDead)
        {
            ClosePanel();
        }
    }

    // Switches the scripts that read the player's input off (false) or on (true).
    // Switching a script off with "enabled" stops its Update, the same trick PlayerDeath uses.
    private void SetPlayerControls(bool isEnabled)
    {
        movement.enabled = isEnabled;
        combat.enabled = isEnabled;
        interactor.enabled = isEnabled;
        itemUser.enabled = isEnabled;
    }

    // Copies every crafting slot into its box on screen (only while the panel is open).
    private void RefreshSlots()
    {
        if (!isOpen)
        {
            return;
        }

        for (int i = 0; i < gridSlotViews.Count; i++)
        {
            gridSlotViews[i].Show(craftingGrid.GetGridSlot(i), craftingGrid.IsGridPreview);
        }

        resultSlotView.Show(craftingGrid.ResultSlot, craftingGrid.IsResultPreview);
    }
}
