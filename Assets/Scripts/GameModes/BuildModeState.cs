using UnityEngine;

// Top-down build mode. Freezes the player (and its input), activates the build Cinemachine camera (the Brain
// blends to it), stops the third-person camera's mouse orbit and frees the cursor. Exit restores all of it.
public class BuildModeState : IState
{
    private readonly GameModeController game;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    public BuildModeState(GameModeController game)
    {
        this.game = game;
    }

    public void Enter()
    {
        Debug.Log("Build mode");

        game.Player.Freeze();
        game.PlayerCameraInput.enabled = false;
        game.BuildCamera.Begin(game.Player.transform.position, Camera.main.transform.eulerAngles.y);
        game.BuildCamera.gameObject.SetActive(true);
        game.Build.enabled = true;

        previousLockState = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Update()
    {
        if (game.ToggleBuildPressed)
        {
            game.StateMachine.ChangeState(game.PlayMode);
        }
    }

    public void Exit()
    {
        Cursor.lockState = previousLockState;
        Cursor.visible = previousCursorVisible;

        game.Build.enabled = false;
        game.BuildCamera.gameObject.SetActive(false);
        game.PlayerCameraInput.enabled = true;
        game.Player.Unfreeze();
    }
}
