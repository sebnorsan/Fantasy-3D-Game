using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class DialogueManager : NetworkBehaviour
{
	public static DialogueManager instance;

	public Dictionary<string, GameObject> _lookup = new Dictionary<string, GameObject>();
	private void Awake()
	{
		if (instance == null) instance = this;
	}
	public void Register(string id, GameObject go)
	{
		if (string.IsNullOrEmpty(id)) return;
		_lookup[id] = go;
	}

	public void Unregister(string id)
	{
		if (string.IsNullOrEmpty(id)) return;
		_lookup.Remove(id);
	}

	public void SetActive(string id, bool active)
	{
		SetActiveServerRpc(id, active);
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void SetActiveServerRpc(string id, bool active)
	{
		SetActiveClientRpc(id, active);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	public void SetActiveClientRpc(string id, bool active)
	{
		if (_lookup.TryGetValue(id, out var go))
		{
			go.SetActive(active);
		}
		else
			Debug.LogWarning($"[DialogueManager] No target registered with ID '{id}'");
	}
}