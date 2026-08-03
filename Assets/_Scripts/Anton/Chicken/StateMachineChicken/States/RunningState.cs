using UnityEngine;

public class RunningState : ChickenState
{
    public RunningState(ChickenController chicken, ChickenStateMachine stateMachine)
        : base(chicken, stateMachine) { }

    public override void Enter()
    {
        Chicken.SetAnimation(
    false,
    false,
    false,
    true
);
    }

    public override void Update()
    {
        // Направление от игрока
        Vector3 dir = Chicken.transform.position - Chicken.Player.position;

        // Игнорируем высоту
        dir.y = 0f;
        dir.Normalize();

        // Поворот в сторону движения
        if (dir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dir);
            Chicken.transform.rotation = Quaternion.Slerp(
                Chicken.transform.rotation,
                targetRotation,
                8f * Time.deltaTime);
        }

        // Движение
        Chicken.transform.position += dir * Chicken.RunSpeed * Time.deltaTime;

        if (!Chicken.IsPlayerClose())
        {
            StateMachine.ChangeState(Chicken.StandingState);
        }
    }
}
