using UnityEngine;

// Airborne without a jump (walked off a ledge) or after a jump's apex. Times the fall so the
// animator can pick a soft or hard landing from FallingDuration.
public class FallState : IState
{
    private readonly PlayerController player;

    public FallState(PlayerController player)
    {
        this.player = player;
    }

    public void Enter()
    {
        Debug.Log("Fall");
        player.BeginFall();
    }

    public void Exit()
    {
    }

    public void Update()
    {
        player.Move();
        player.UpdateFallingDuration();

        if (player.IsGrounded)
        {
            player.StateMachine.ChangeState(player.GroundedState);
            return;
        }

        player.UpdateAnimator();
    }
}
