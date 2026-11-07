using UnityEngine;

// The inventory / crafting / loot screen. Like Build mode: freezes the player, stops the camera orbiting and frees
// the cursor; Exit restores all of it. The context (hand crafting, a station, a loot chest) is set by
// GameModeController.OpenInventory before entering.
public class InventoryModeState : IState
{
    private readonly GameModeController game;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    public InventoryContext Context { get; set; }

    public InventoryModeState(GameModeController game)
    {
        this.game = game;
    }

    public void Enter()
    {
        game.Player.Freeze();
        game.PlayerCameraInput.enabled = false;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        game.InventoryScreen.Open(Context ?? InventoryContext.Personal());
    }

    public void Update()
    {
        if (game.ToggleInventoryPressed || game.ClosePressed)
        {
            game.StateMachine.ChangeState(game.PlayMode);
        }
    }

    public void Exit()
    {
        game.InventoryScreen.Close();

        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        game.PlayerCameraInput.enabled = true;
        game.Player.Unfreeze();
        Context = null;
    }
}
