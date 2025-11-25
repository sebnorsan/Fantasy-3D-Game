using Unity.Netcode;
using UnityEngine;

public class Well : NetworkBehaviour, IInteractable
{
	public bool canInteract { get; set; } = true;

	public void Interact()
	{
		// Don't check IsOwner here – the Well is owned by the server.
		if (!canInteract)
			return;

		// Ask the server to process the interaction
		InteractServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void InteractServerRpc()
	{
		if (!canInteract) return;
		canInteract = false;

		// sync canInteract to everyone
		SetInteractClientRpc(false);

		// do Well gameplay on server
		var quest = GetComponent<QuestObject>();
		if (quest != null)
			quest.FinishQuest();

		// play sound on all clients
		PlayAudioClientRpc();
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetInteractClientRpc(bool value)
	{
		canInteract = value;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void PlayAudioClientRpc()
	{
		var audio = GetComponent<AudioSource>();
		if (audio != null)
			audio.Play();
	}
}
