using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformChild : NetworkBehaviour
{
	[Header("Targets to sync (children, bones, etc.)")]
	[SerializeField] private List<Transform> targets = new List<Transform>();

	[Header("Interpolation")]
	[SerializeField] private float lerpSpeed = 10f;

	// Buffer used by the owner to send data
	private Quaternion[] _sendBuffer;

	// Per-client flag: have we snapped to the correct rotations at least once?
	private bool _hasInitialSync;

	public override void OnNetworkSpawn()
	{
		if (targets == null)
			targets = new List<Transform>();

		_sendBuffer = new Quaternion[targets.Count];
		_hasInitialSync = false;
	}

	private void LateUpdate()
	{
		if (!IsSpawned || targets == null || targets.Count == 0)
			return;

		// Only the owner sends rotations
		if (!IsOwner)
			return;

		// Ensure buffer size matches target count
		if (_sendBuffer == null || _sendBuffer.Length != targets.Count)
			_sendBuffer = new Quaternion[targets.Count];

		for (int i = 0; i < targets.Count; i++)
		{
			Transform t = targets[i];
			_sendBuffer[i] = t ? t.rotation : Quaternion.identity;
		}

		SendRotationsServerRpc(_sendBuffer);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SendRotationsServerRpc(Quaternion[] rots)
	{
		ApplyRotationsClientRpc(rots);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void ApplyRotationsClientRpc(Quaternion[] rots)
	{
		// Owner already has the correct rotations locally
		if (IsOwner || !IsSpawned)
			return;

		if (targets == null || targets.Count == 0 || rots == null)
			return;

		int count = Mathf.Min(rots.Length, targets.Count);

		for (int i = 0; i < count; i++)
		{
			Transform t = targets[i];
			if (!t) continue;

			if (!_hasInitialSync)
			{
				// First packet: snap exactly to host to avoid prefab / parent offset issues
				t.rotation = rots[i];
			}
			else
			{
				// After that, just smooth towards new data
				t.rotation = Quaternion.Slerp(
					t.rotation,
					rots[i],
					Time.deltaTime * lerpSpeed
				);
			}
		}

		// After the first time we apply rotations, we consider ourselves synced
		_hasInitialSync = true;
	}
}
