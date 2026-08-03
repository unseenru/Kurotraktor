using UnityEngine;

public class WalkingState : ChickenState
{
    float timer;
    Vector3 direction;

    public WalkingState(ChickenController chicken, ChickenStateMachine stateMachine)
        : base(chicken, stateMachine) { }

    public override void Enter()
    {
        timer = Random.Range(2f, 5f);

        direction = Random.insideUnitSphere;
        direction.y = 0;
        direction.Normalize();

        Chicken.SetAnimation(
    false,
    false,
    true,
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

        Chicken.transform.position +=
            direction * Chicken.WalkSpeed * Time.deltaTime;

        timer -= Time.deltaTime;

        if (timer <= 0)
            StateMachine.ChangeState(Chicken.StandingState);
    }
}
