using UnityEngine;

// Normal gameplay. PlayerController and the Synty camera run as usual; this state only watches for the
// switch to Build mode. Build mode undoes its own changes in Exit, so there is nothing to set up here.
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
    }

    public void Update()
    {
        if (game.ToggleBuildPressed && game.Player.CanEnterBuildMode)
        {
            game.StateMachine.ChangeState(game.BuildMode);
        }
    }

    public void Exit()
    {
    }
}
