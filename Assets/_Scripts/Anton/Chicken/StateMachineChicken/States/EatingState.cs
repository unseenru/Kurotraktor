using UnityEngine;

public class EatingState : ChickenState
{
    float timer;

    public EatingState(ChickenController chicken, ChickenStateMachine stateMachine)
        : base(chicken, stateMachine) { }

    public override void Enter()
    {
        timer = Random.Range(2f, 5f);

        Chicken.SetAnimation(ChickenController.ChickenAnimation.Eating);
    }

    public override void Update()
    {
        if (Chicken.IsPlayerClose())
        {
            StateMachine.ChangeState(Chicken.RunningState);
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0)
            StateMachine.ChangeState(Chicken.StandingState);
    }
}
