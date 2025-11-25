using Unity.Netcode;
using UnityEngine;

public class Well : NetworkBehaviour, IInteractable
{
	public bool canInteract { get; set; } = true;

	public void Interact()
	{
		if (!IsOwner) return;
		if (!canInteract)
			return;

		SetInteractServerRpc(false);
		
		GetComponent<AudioSource>().Play();
		GetComponent<QuestObject>().FinishQuest();
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetInteractServerRpc(bool value)
	{
		SetInteractClientRpc(value);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetInteractClientRpc(bool value)
	{
		canInteract = value;
	}
}
