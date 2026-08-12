
using UnityEngine;
using Zenject;

public class Chicken : MonoBehaviour,IEntity
{

    public Transform Transform => transform;

    public class Factory : PlaceholderFactory<Chicken> { }
}
