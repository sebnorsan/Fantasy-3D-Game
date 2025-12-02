using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformChild : NetworkBehaviour
{
	[SerializeField] private List<Transform> targets = new();
	[SerializeField] private float lerpSpeed = 10f;

	private NetworkList<Quaternion> _rots = new NetworkList<Quaternion>();

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
		{
			_rots.Clear();
			for (int i = 0; i < targets.Count; i++)
				_rots.Add(targets[i] ? targets[i].localRotation : Quaternion.identity);
		}
	}

	private void Update()
	{
		if (!IsSpawned || targets.Count == 0) return;

		if (IsOwner)
		{
			// keep list sized
			while (_rots.Count < targets.Count)
				_rots.Add(Quaternion.identity);
			while (_rots.Count > targets.Count)
				_rots.RemoveAt(_rots.Count - 1);

			for (int i = 0; i < targets.Count; i++)
			{
				if (!targets[i]) continue;
				_rots[i] = targets[i].localRotation;
			}
		}
		else
		{
			int count = Mathf.Min(_rots.Count, targets.Count);
			for (int i = 0; i < count; i++)
			{
				if (!targets[i]) continue;

				targets[i].localRotation = Quaternion.Slerp(
					targets[i].localRotation,
					_rots[i],
					Time.deltaTime * lerpSpeed
				);
			}
		}
	}
}
