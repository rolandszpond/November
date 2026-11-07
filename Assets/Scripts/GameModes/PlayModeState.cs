using UnityEngine;

// Normal gameplay. PlayerController and the Synty camera run as usual, along with the play-only player components
// (interaction, hotbar, placement). Watches for the switch to Build mode (B) or the inventory (Tab). The other
// modes undo their own changes in Exit.
public class PlayModeState : IState
{
    private readonly GameModeController game;

    public PlayModeState(GameModeController game)
    {
        this.game = game;
    }

    public void Enter()
    {
        Debug.Log("Play mode");
        game.SetPlayModeBehaviours(true);
    }

    public void Update()
    {
        // Both menus freeze the player, so only allow them from a settled, grounded state.
        if (game.ToggleBuildPressed && game.Player.CanEnterBuildMode)
        {
            game.StateMachine.ChangeState(game.BuildMode);
        }
        else if (game.ToggleInventoryPressed && game.Player.CanEnterBuildMode)
        {
            game.OpenInventory(InventoryContext.Personal());
        }
    }

    public void Exit()
    {
        game.SetPlayModeBehaviours(false);
    }
}
