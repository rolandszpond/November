using UnityEngine;

public class CrouchState : IState
{
    private readonly PlayerController player;

    public CrouchState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Crouch");
        player.SetCrouching(true);
        player.input.onJumpPerformed += OnJump;
    }

    public void Exit()
    {
        player.input.onJumpPerformed -= OnJump;
        player.SetCrouching(false);
    }

    public void Update()
    {
        player.Move();

        // Walked off a ledge: drop out of crouch and fall (as in the Synty sample).
        if (!player.IsGrounded)
        {
            player.StateMachine.ChangeState(player.FallState);
            return;
        }

        // Stand up once crouch is released, but only if there's headroom and the slide has finished.
        if (!player.CrouchHeld && !player.IsSliding && player.CanStandUp())
        {
            player.StateMachine.ChangeState(player.HasMoveInput ? player.MoveState : player.IdleState);
            return;
        }

        player.UpdateAnimator();
    }

    private void OnJump()
    {
        if (player.CanStandUp())
        {
            player.StateMachine.ChangeState(player.JumpState);
        }
    }
}
