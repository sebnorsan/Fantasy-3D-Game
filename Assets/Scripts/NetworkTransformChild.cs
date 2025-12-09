using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformChild : NetworkBehaviour
{
	[SerializeField] private List<Transform> targets = new List<Transform>();
	[SerializeField] private float lerpSpeed = 10f;

	private Quaternion[] _rotBuffer;
	private Vector3[] _posBuffer;
	private bool _hasInitialSync;

	public override void OnNetworkSpawn()
	{
		if (targets == null)
			targets = new List<Transform>();

		_rotBuffer = new Quaternion[targets.Count];
		_posBuffer = new Vector3[targets.Count];
		_hasInitialSync = false;
	}

	private void Update()
	{
		if (!IsSpawned || targets == null || targets.Count == 0)
			return;
		if (!IsOwner)
			return;

		if (_rotBuffer == null || _rotBuffer.Length != targets.Count)
		{
			_rotBuffer = new Quaternion[targets.Count];
			_posBuffer = new Vector3[targets.Count];
		}

		for (int i = 0; i < targets.Count; i++)
		{
			Transform t = targets[i];
			if (!t)
			{
				_rotBuffer[i] = Quaternion.identity;
				_posBuffer[i] = Vector3.zero;
				continue;
			}

			_rotBuffer[i] = t.localRotation;
			_posBuffer[i] = t.localPosition;
		}

		SendTransformServerRpc(_rotBuffer, _posBuffer);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SendTransformServerRpc(Quaternion[] rots, Vector3[] poss)
	{
		ApplyTransformClientRpc(rots, poss);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void ApplyTransformClientRpc(Quaternion[] rots, Vector3[] poss)
	{
		if (IsOwner || !IsSpawned) return;
		if (targets == null || targets.Count == 0 || rots == null || poss == null) return;

		int count = Mathf.Min(targets.Count, rots.Length, poss.Length);

		for (int i = 0; i < count; i++)
		{
			Transform t = targets[i];
			if (!t) continue;

			if (!_hasInitialSync)
			{
				t.localRotation = rots[i];
				t.localPosition = poss[i];
			}
			else
			{
				t.localRotation = Quaternion.Slerp(
					t.localRotation, rots[i],
					Time.deltaTime * lerpSpeed
				);

				t.localPosition = Vector3.Lerp(
					t.localPosition, poss[i],
					Time.deltaTime * lerpSpeed
				);
			}
		}

		_hasInitialSync = true;
	}
}
