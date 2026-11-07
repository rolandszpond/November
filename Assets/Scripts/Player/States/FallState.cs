// Airborne without a jump (walked off a ledge) or after a jump's apex. Times the fall so the
// animator can pick a soft or hard landing from FallingDuration.
public class FallState : PlayerState
{
    public FallState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.BeginFall();
    }

    public override void Update()
    {
        player.UpdateFallingDuration();

        if (player.IsGrounded)
        {
            ChangeState(player.LandingState);
        }
    }
}
