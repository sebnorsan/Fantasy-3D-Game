using UnityEngine;

public class EnableRigidbodyEvent : AbstractEvent
{
    public Rigidbody rbAffected;

    [Space(10)]

    [SerializeField] private float massChange = -1;
    //[SerializeField] private float linearDampingChange = -1;
    //[SerializeField] private float angularDampingChange = -1;

    [Space(10)]

    [SerializeField] bool useGravity;
    [SerializeField] bool useKinematic;

    [Space(10)]

    [SerializeField] RigidbodyConstraints constraintsChange = RigidbodyConstraints.None;

	public override void CallEvent()
	{
        if (massChange != -1)
            rbAffected.mass = massChange;
        //if (linearDampingChange != -1)
        //    rbAffected.linearDamping = linearDampingChange;
        //if (angularDampingChange != -1)
        //    rbAffected.angularDamping = angularDampingChange;

        rbAffected.useGravity = useGravity;
        rbAffected.isKinematic = useKinematic;

        rbAffected.constraints = constraintsChange;
	}
}
