 using System.Security.Principal;

using UnityEngine;

using Zenject;



[RequireComponent(typeof(CharacterController))]

public class ChickenMovement : MonoBehaviour

{

    private CharacterController _controller;

    private ChickenSettings _settings;

    private IEntityRegistry<IEntity> _entityRegistry; // »зменили Player на общий интерфейс IEntity

    private Transform _overrideTargetTransform;

    private float _verticalVelocity;



    [Inject]

    private void Construct(IEntityRegistry<IEntity> registry)

    {

        _entityRegistry = registry;

    }







    private void Awake() => _controller = GetComponent<CharacterController>();



    private Transform TargetTransform

    {

        get

        {

            if (_overrideTargetTransform != null)

                return _overrideTargetTransform;



            if (_entityRegistry == null)

                return null;



            Transform closestTarget = null;

            float minSqrDistance = float.MaxValue;

            Vector3 currentPos = transform.position;



            // »щем ближайшую сущность из всех зарегистрированных (и игрок, и машина)

            foreach (var entity in _entityRegistry.AllEntities)

            {

                if (entity == null || entity.Transform == null) continue;



                float sqrDist = (entity.Transform.position - currentPos).sqrMagnitude;

                if (sqrDist < minSqrDistance)

                {

                    minSqrDistance = sqrDist;

                    closestTarget = entity.Transform;

                }

            }



            return closestTarget;

        }

    }



    public void Initialize(ChickenSettings settings, Transform target = null)

    {

        _settings = settings;

        if (target != null) _overrideTargetTransform = target;

    }



    public void SetTarget(Transform target) => _overrideTargetTransform = target;



    public bool IsTargetClose(float distance)

    {

        var target = TargetTransform;

        return target != null && _settings != null && GetPlanarSqrDistanceTo(target) < distance * distance;

    }



    public bool IsTargetFarEnough(float distance)

    {

        var target = TargetTransform;

        return target == null || _settings == null || GetPlanarSqrDistanceTo(target) >= distance * distance;

    }



    public Vector3 GetFleeDirection()

    {

        var target = TargetTransform;

        if (target == null) return -transform.forward;



        Vector3 direction = transform.position - target.position;

        direction.y = 0f;



        return direction.sqrMagnitude < _settings.DirectionEpsilon ? -transform.forward : direction.normalized;

    }



    public void Move(Vector3 direction, float speed)

    {

        if (_controller == null || _settings == null) return;



        if (direction.sqrMagnitude >= _settings.DirectionEpsilon)

        {

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _settings.TurnSpeed * Time.deltaTime);

        }



        _verticalVelocity = _controller.isGrounded && _verticalVelocity < 0f

            ? _settings.GroundedStickyVelocity

            : _verticalVelocity + _settings.Gravity * Time.deltaTime;



        Vector3 velocity = direction.normalized * speed + Vector3.up * _verticalVelocity;

        _controller.Move(velocity * Time.deltaTime);

    }



    private float GetPlanarSqrDistanceTo(Transform target)

    {

        Vector3 offset = transform.position - target.position;

        offset.y = 0f;

        return offset.sqrMagnitude;

    }

}

