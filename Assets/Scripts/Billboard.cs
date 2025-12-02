using Unity.Netcode;
using UnityEngine;

public class Billboard : MonoBehaviour
{
	private Transform target;
	public bool lockRotationToY = true;

	private bool init = false;

	private void Start()
	{
		Invoke(nameof(SetInit), 5f);
	}
	private void SetInit() => init = true;

	private void LateUpdate()
	{
		if (!init) return;

		if (target == null)
		{
			TryFindTarget();
			if (target == null) return;
		}

		Vector3 dir = (target.position - transform.position).normalized;

		if (lockRotationToY)
		{
			dir.y = 0;
			if (dir.sqrMagnitude < 0.001f) return;
		}

		transform.rotation = Quaternion.LookRotation(dir);
	}

	private void TryFindTarget()
	{
		if (NetworkManager.Singleton == null) return;

		var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
		if (playerObj != null)
			target = playerObj.transform;
	}
}
