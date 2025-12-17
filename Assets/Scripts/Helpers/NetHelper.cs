using Unity.Netcode;
using UnityEngine;

public class NetHelper : NetworkBehaviour
{
	public GameObject[] netGameObjects;

	public void NetInstantiate(GameObject go, Vector3 pos, Quaternion rot, float destroyAfter = 0f)
	{
		int id = GetPrefabId(go);
		if (id < 0) return;

		ulong origin = NetworkManager.Singleton.LocalClientId;

		// local predicted
		InstantiateObjectFunction(id, pos, rot, destroyAfter);

		// network replicate
		InstantiateObjectServerRpc(id, pos, rot, destroyAfter, origin);
	}

	private int GetPrefabId(GameObject go)
	{
		for (int i = 0; i < netGameObjects.Length; i++)
		{
			if (netGameObjects[i] == go)
				return i;
		}
		return -1;
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void InstantiateObjectServerRpc(int id, Vector3 pos, Quaternion rot, float destroyAfter, ulong originClientId)
	{
		InstantiateObjectClientRpc(id, pos, rot, destroyAfter, originClientId);
	}

	[Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Server)]
	private void InstantiateObjectClientRpc(int id, Vector3 pos, Quaternion rot, float destroyAfter, ulong originClientId)
	{
		if (NetworkManager.Singleton.LocalClientId == originClientId)
			return; // already did predicted spawn locally

		InstantiateObjectFunction(id, pos, rot, destroyAfter);
	}

	private void InstantiateObjectFunction(int id, Vector3 pos, Quaternion rot, float destroyAfter = 0f)
	{
		if (id < 0 || id >= netGameObjects.Length) return;

		var prefab = netGameObjects[id];
		if (prefab == null) return;

		var obj = Instantiate(prefab, pos, rot);
		if (destroyAfter > 0)
			Destroy(obj, destroyAfter);
	}
}
