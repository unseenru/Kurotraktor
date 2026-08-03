using UnityEngine;

public class RunningState : ChickenState
{
    public RunningState(ChickenController chicken, ChickenStateMachine stateMachine)
        : base(chicken, stateMachine) { }

    public override void Enter()
    {
        Chicken.SetAnimation(ChickenController.ChickenAnimation.Running);
    }

    public override void Update()
    {
        Vector3 direction = Chicken.GetFleeDirection();
        Chicken.Move(direction, Chicken.RunSpeed);

        if (Chicken.IsPlayerFarEnough())
        {
            StateMachine.ChangeState(Chicken.StandingState);
        }
    }
}
