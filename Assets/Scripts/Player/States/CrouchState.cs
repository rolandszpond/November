// Walking off a ledge drops out of crouch into a fall (handled by PlayerGroundedState, as in the Synty sample).
public class CrouchState : PlayerGroundedState
{
    public CrouchState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.SetCrouching(true);
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
        player.SetCrouching(false);
    }

    protected override void UpdateGrounded()
    {
        // Stand up once crouch is released, but only if there's headroom and the slide has finished.
        if (!player.CrouchHeld && !player.IsSliding && player.CanStandUp())
        {
            ChangeState(player.HasMoveInput ? player.MoveState : player.IdleState);
        }
    }

    protected override void OnJump()
    {
        if (player.CanStandUp())
        {
            base.OnJump();
        }
    }
}
