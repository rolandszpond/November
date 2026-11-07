using UnityEngine;

public class IdleState : IState
{
    private readonly PlayerController player;

    public IdleState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Idle");
        player.input.onJumpPerformed += OnJump;
    }

    public void Exit()
    {
        player.input.onJumpPerformed -= OnJump;
    }

    public void Update()
    {
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

        if (player.HasMoveInput)
        {
            player.StateMachine.ChangeState(player.MoveState);
        }

        player.UpdateAnimator();
    }

    private void OnJump()
    {
        player.StateMachine.ChangeState(player.JumpState);
    }
}
