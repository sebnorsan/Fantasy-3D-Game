using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ActivateEvent : AbstractEvent
{
    public GameObject[] activated;
    public GameObject[] deactivated;
	public Collider[] collidersActivated;
	public Collider[] collidersDeactivated;

    [SerializeField] private bool update;

	private void OnValidate()
	{
        if (update)
        {
            update = !update;
			CallEvent();
		}
	}

	public override void CallEvent()
    {
        if (activated.Length > 0)
            EventManager.instance.ActivateObjects(activated);
		if (deactivated.Length > 0)
			EventManager.instance.DeactivateObjects(deactivated);
		if (collidersActivated.Length > 0)
			EventManager.instance.ActivateColliders(collidersActivated);
        if (collidersDeactivated.Length > 0)
			EventManager.instance.DeactivateColliders(collidersDeactivated);
    }
}
