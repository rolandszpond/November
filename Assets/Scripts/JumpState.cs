using UnityEngine;

public class JumpState : IState
{
    private readonly PlayerController player;

    public JumpState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Jump");
        player.IsJumping = true;
        player.Jump();
    }

    public void Exit()
    {
        player.IsJumping = false;
    }

    public void Update()
    {
        player.Move();

        // Past the apex: hand over to the fall state, which times the descent for the landing animation.
        if (player.VerticalVelocity <= 0f)
        {
            player.StateMachine.ChangeState(player.FallState);
            return;
        }

        player.UpdateAnimator();
    }
}
