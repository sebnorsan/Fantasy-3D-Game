using Unity.Netcode;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	private Transform target;

	[Tooltip("If true, will only rotate around Y (for UI that should stay upright)")]
	public bool lockRotationToY = true;

	private void Start()
	{
		target = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.transform;
	}
	private void LateUpdate()
	{
		// Find the camera if nothing’s assigned
		if (target == null)
			target = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject.transform;

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
