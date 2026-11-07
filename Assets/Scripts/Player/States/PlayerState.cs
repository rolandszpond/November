// Base for all locomotion states. PlayerController moves the character and updates the animator every
// frame around StateMachine.Update(), so states only handle their own setup/teardown and transitions.
public abstract class PlayerState : IState
{
    protected readonly PlayerController player;

    protected PlayerState(PlayerController player)
    {
        this.player = player;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    protected void ChangeState(IState state) => player.StateMachine.ChangeState(state);
}

// Base for states where the player stands on the ground: listens for jump, and falls when the ground disappears
// (e.g. walking off a ledge). Subclasses put their own transitions in UpdateGrounded.
public abstract class PlayerGroundedState : PlayerState
{
    protected PlayerGroundedState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        player.input.onJumpPerformed += OnJump;
    }

    public override void Exit()
    {
        player.input.onJumpPerformed -= OnJump;
    }

    public sealed override void Update()
    {
        if (!player.IsGrounded)
        {
            ChangeState(player.FallState);
            return;
        }

        UpdateGrounded();
    }

    protected abstract void UpdateGrounded();

    protected virtual void OnJump()
    {
        ChangeState(player.JumpState);
    }
}
