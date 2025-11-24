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

		if (other.GetComponent<PlayerController>() == null) return;

		fired = true;

		if (other.GetComponent<PlayerController>())
			CallUpClientRpc();

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
		foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
		{
			NPC_Interactable_Remote npcRemote = p.GetComponentInChildren<NPC_Interactable_Remote>();

			if (dialogueToReplace != null)
				npcRemote.npcOwner.ChangeDialogue(dialogueToReplace);

			npcRemote.CallUp();
		}
	}
}


