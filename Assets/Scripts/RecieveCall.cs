using Unity.Netcode;
using UnityEngine;

public class RecieveCall : NetworkBehaviour
{
	[SerializeField] GameObject[] objToRemove;
	[SerializeField] private ScriptableObject_NPC_Dialogue dialogueToReplace;

	private bool fired = false;

	private void OnTriggerEnter(Collider other)
	{
		if (!NetworkManager.Singleton.IsServer || fired) return;

		if (!other.TryGetComponent<PlayerController>(out _)) return;

		fired = true;

		CallUpClientRpc(); // one call, all clients react

		foreach (var obj in objToRemove)
		{
			if (obj.TryGetComponent(out NetworkObject nObj) && nObj.IsSpawned)
				nObj.Despawn(true);
			else
				Destroy(obj);
		}
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void CallUpClientRpc()
	{
		// this runs on every client (+ host)

		var localPlayerObject = NetworkManager.Singleton.LocalClient?.PlayerObject;
		if (localPlayerObject == null) return;

		var p = localPlayerObject.GetComponent<PlayerController>();
		if (p == null) return;

		var npcRemote = p.GetComponentInChildren<NPC_Interactable_Remote>();
		if (npcRemote == null) return;

		if (dialogueToReplace != null)
		{
			if (npcRemote.npcOwner == null) npcRemote.npcOwner = FindFirstObjectByType<NPC_Interactable>();

			npcRemote.npcOwner.ChangeDialogue(dialogueToReplace);
		}

		npcRemote.CallUp();
	}
}
