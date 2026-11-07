public class JumpState : PlayerState
{
    public JumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.IsJumping = true;
        player.Jump();
    }

    public override void Exit()
    {
        player.IsJumping = false;
    }

    public override void Update()
    {
        // Past the apex: hand over to the fall state, which times the descent for the landing animation.
        if (player.VerticalVelocity <= 0f)
        {
            ChangeState(player.FallState);
        }
    }
}
