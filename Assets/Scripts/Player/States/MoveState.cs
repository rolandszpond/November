public class MoveState : PlayerGroundedState
{
    public MoveState(PlayerController player) : base(player) { }

    protected override void UpdateGrounded()
    {
        if (player.ShouldCrouch)
        {
            ChangeState(player.CrouchState);
        }
        else if (!player.HasMoveInput)
        {
            ChangeState(player.IdleState);
        }
    }
}
