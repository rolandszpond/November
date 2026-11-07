using UnityEngine;

public class MoveState : IState
{
    private readonly PlayerController player;

    public MoveState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Move");
        player.input.onJumpPerformed += OnJump;
    }

    public void Exit()
    {
        player.input.onJumpPerformed -= OnJump;
    }

    public void Update()
    {
        if (!player.HasMoveInput)
        {
            player.StateMachine.ChangeState(player.IdleState);
            return;
        }

        player.Move();

        if (!player.IsGrounded)
        {
            player.StateMachine.ChangeState(player.FallState);
            return;
        }

        if (player.ShouldCrouch)
        {
            player.StateMachine.ChangeState(player.CrouchState);
            return;
        }

        player.UpdateAnimator();
    }

    private void OnJump()
    {
        player.StateMachine.ChangeState(player.JumpState);
    }
}
