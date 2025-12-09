using Unity.Netcode;
using UnityEngine;

public class NetHelper : NetworkBehaviour
{
	public static NetHelper instance;

	public GameObject[] netGameObjects;
	private void Awake()
	{
		if (instance != null)
			Destroy(gameObject);
		else
			instance = this;
	}
	public void NetInstantiate(GameObject go, Vector3 pos, Quaternion rot, float destroyAfter = 0f)
	{
		int id = GetPrefabId(go);
		if (id < 0) return;

		InstantiateObjectFunction(id, pos, rot, destroyAfter);
		InstantiateObjectServerRpc(id, pos, rot, destroyAfter);
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
	private void InstantiateObjectServerRpc(int id, Vector3 pos, Quaternion rot, float destroyAfter = 0f)
	{
		InstantiateObjectClientRpc(id, pos, rot, destroyAfter);
	}

	[Rpc(SendTo.NotOwner, InvokePermission = RpcInvokePermission.Server)]
	private void InstantiateObjectClientRpc(int id, Vector3 pos, Quaternion rot, float destroyAfter = 0f)
	{
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
