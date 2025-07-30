using UnityEngine;

[ExecuteAlways] // makes it update in Edit mode as well
public class Billboard : MonoBehaviour
{
	[Tooltip("If left empty, will default to Camera.main")]
	public Transform target;

	[Tooltip("If true, will only rotate around Y (for UI that should stay upright)")]
	public bool lockRotationToY = true;

	private void LateUpdate()
	{
		// Find the camera if nothing’s assigned
		if (target == null && Camera.main != null)
			target = Camera.main.transform;

		if (target == null)
			return;

		// Direction from this object to the camera
		Vector3 dir = (target.position - transform.position).normalized;

		if (lockRotationToY)
		{
			// Project onto XZ plane so it only spins around Y
			dir.y = 0;
			if (dir.sqrMagnitude < 0.001f)
				return;
			transform.rotation = Quaternion.LookRotation(dir);
		}
		else
		{
			// Fully face the camera
			transform.rotation = Quaternion.LookRotation(dir);
		}
	}
}
