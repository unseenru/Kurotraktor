using UnityEngine;

public class ChickenController : MonoBehaviour
{
    public Transform Player;
    public Animator _animator;

    [Header("Movement")]
    public float WalkSpeed = 1.5f;
    public float RunSpeed = 5f;

    [Header("Detection")]
    public float RunDistance = 5f;

    public ChickenStateMachine StateMachine;

    public EatingState EatingState;
    public StandingState StandingState;
    public WalkingState WalkingState;
    public RunningState RunningState;

    private void Awake()
    {
        StateMachine = new ChickenStateMachine();

        EatingState = new EatingState(this, StateMachine);
        StandingState = new StandingState(this, StateMachine);
        WalkingState = new WalkingState(this, StateMachine);
        RunningState = new RunningState(this, StateMachine);
    }

    public void SetAnimation(
    bool eating,
    bool standing,
    bool walking,
    bool running)
    {
        _animator.SetBool("IsEating", eating);
        _animator.SetBool("IsStanding", standing);
        _animator.SetBool("IsWalking", walking);
        _animator.SetBool("IsRunning", running);
    }

    private void Start()
    {
        StateMachine.Initialize(EatingState);
    }

    private void Update()
    {
        StateMachine.Update();
    }

    public bool IsPlayerClose()
    {
        return Vector3.Distance(transform.position, Player.position) < RunDistance;
    }
}
