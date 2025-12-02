using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformChild : NetworkBehaviour
{
	[SerializeField] private List<Transform> targets = new List<Transform>();
	[SerializeField] private float lerpSpeed = 10f;

	private Quaternion[] _sendBuffer;

	public override void OnNetworkSpawn()
	{
		if (targets == null)
			targets = new List<Transform>();

		_sendBuffer = new Quaternion[targets.Count];
	}

	private void Update()
	{
		if (!IsSpawned || targets.Count == 0)
			return;

		// Only OWNER sends rotations
		if (!IsOwner)
			return;

		for (int i = 0; i < targets.Count; i++)
		{
			_sendBuffer[i] = targets[i]
				? targets[i].localRotation
				: Quaternion.identity;
		}

		SendRotationsServerRpc(_sendBuffer);
	}

	[ServerRpc]
	private void SendRotationsServerRpc(Quaternion[] rots)
	{
		ApplyRotationsClientRpc(rots);
	}

	[ClientRpc]
	private void ApplyRotationsClientRpc(Quaternion[] rots)
	{
		// Owner already has correct rotations locally
		if (IsOwner)
			return;

		int count = Mathf.Min(rots.Length, targets.Count);
		for (int i = 0; i < count; i++)
		{
			if (!targets[i]) continue;

			targets[i].localRotation = Quaternion.Slerp(
				targets[i].localRotation,
				rots[i],
				Time.deltaTime * lerpSpeed
			);
		}
	}
}