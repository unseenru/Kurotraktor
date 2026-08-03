using UnityEngine;
using UnityEngine.Serialization;

public class ChickenController : MonoBehaviour
{
    public enum ChickenAnimation { Eating, Standing, Walking, Running }

    private static readonly int IsEating = Animator.StringToHash("IsEating");
    private static readonly int IsStanding = Animator.StringToHash("IsStanding");
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int IsRunning = Animator.StringToHash("IsRunning");

    [Header("References")]
    [FormerlySerializedAs("Player")]
    [SerializeField] private Transform player;
    [FormerlySerializedAs("_animator")]
    [SerializeField] private Animator animator;

    [Header("Movement")]
    [FormerlySerializedAs("WalkSpeed")]
    [Min(0f)] [SerializeField] private float walkSpeed = 1.5f;
    [FormerlySerializedAs("RunSpeed")]
    [Min(0f)] [SerializeField] private float runSpeed = 5f;
    [Min(0f)] [SerializeField] private float turnSpeed = 8f;

    [Header("Detection")]
    [FormerlySerializedAs("RunDistance")]
    [Min(0f)] [SerializeField] private float fleeDistance = 5f;
    [Min(0f)] [SerializeField] private float calmDistance = 7f;

    public float WalkSpeed => walkSpeed;
    public float RunSpeed => runSpeed;
    public ChickenStateMachine StateMachine { get; private set; }

    public EatingState EatingState { get; private set; }
    public StandingState StandingState { get; private set; }
    public WalkingState WalkingState { get; private set; }
    public RunningState RunningState { get; private set; }

    private void Awake()
    {
        calmDistance = Mathf.Max(calmDistance, fleeDistance);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null || animator == null)
        {
            Debug.LogError(
                $"{nameof(ChickenController)} on '{name}' requires Player and Animator references.",
                this);
            enabled = false;
            return;
        }

        StateMachine = new ChickenStateMachine();

        EatingState = new EatingState(this, StateMachine);
        StandingState = new StandingState(this, StateMachine);
        WalkingState = new WalkingState(this, StateMachine);
        RunningState = new RunningState(this, StateMachine);
    }

    public void SetAnimation(ChickenAnimation animation)
    {
        animator.SetBool(IsEating, animation == ChickenAnimation.Eating);
        animator.SetBool(IsStanding, animation == ChickenAnimation.Standing);
        animator.SetBool(IsWalking, animation == ChickenAnimation.Walking);
        animator.SetBool(IsRunning, animation == ChickenAnimation.Running);
    }

    private void Start()
    {
        if (!enabled)
            return;

        StateMachine.Initialize(EatingState);
    }

    private void Update()
    {
        StateMachine.Update();
    }

    public bool IsPlayerClose()
    {
        return GetPlanarSqrDistanceToPlayer() < fleeDistance * fleeDistance;
    }

    public bool IsPlayerFarEnough()
    {
        return GetPlanarSqrDistanceToPlayer() >= calmDistance * calmDistance;
    }

    public Vector3 GetFleeDirection()
    {
        Vector3 direction = transform.position - player.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = -transform.forward;
            direction.y = 0f;
        }

        return direction.normalized;
    }

    public void Move(Vector3 direction, float speed)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        transform.position += direction * speed * Time.deltaTime;
    }

    private float GetPlanarSqrDistanceToPlayer()
    {
        Vector3 offset = transform.position - player.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    private void OnValidate()
    {
        if (calmDistance < fleeDistance)
            calmDistance = fleeDistance;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
}
