using UnityEngine;

public class StandingState : ChickenState
{
    float timer;

    public StandingState(ChickenController chicken, ChickenStateMachine stateMachine)
        : base(chicken, stateMachine) { }

    public override void Enter()
    {
        timer = Random.Range(1f, 3f);

        Chicken.SetAnimation(
    false,
    true,
    false,
    false
);
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
        {
            if (Random.value > 0.5f)
                StateMachine.ChangeState(Chicken.WalkingState);
            else
                StateMachine.ChangeState(Chicken.EatingState);
        }
    }
}
