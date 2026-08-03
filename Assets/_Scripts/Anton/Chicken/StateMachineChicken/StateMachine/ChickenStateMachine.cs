public class ChickenStateMachine
{
    public ChickenState CurrentState { get; private set; }

    public void Initialize(ChickenState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    public void ChangeState(ChickenState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}
