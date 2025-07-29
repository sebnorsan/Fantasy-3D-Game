using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
	static Dictionary<string, GameObject> _lookup = new Dictionary<string, GameObject>();

	public static void Register(string id, GameObject go)
	{
		if (string.IsNullOrEmpty(id)) return;
		_lookup[id] = go;
	}

	public static void Unregister(string id)
	{
		if (string.IsNullOrEmpty(id)) return;
		_lookup.Remove(id);
	}

	public static void SetActive(string id, bool active)
	{
		if (_lookup.TryGetValue(id, out var go))
			go.SetActive(active);
		else
			Debug.LogWarning($"[DialogueManager] No target registered with ID '{id}'");
	}
}