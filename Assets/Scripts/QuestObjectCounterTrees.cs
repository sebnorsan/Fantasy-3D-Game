using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using Unity.Netcode;
using UnityEngine;

public class QuestObjectCounterTrees : NetworkBehaviour
{
	public int countToReach;
	[TextArea] public string questDescription;
	public bool isActive = true;

	public enum QuestType
	{
		MainQuest,
		SideQuest
	}

	[SerializeField] private QuestType questType;

	public int xpGain = 100;
	public string actionToEnable = "";

	// ================= NETCODE FLOW =================

	public override void OnNetworkSpawn()
	{
		//if (!NetworkManager.Singleton.IsServer) return;
		//SetQuestClientRpc(); // already on server, no need for server RPC
	}
	private void OnEnable()
	{
		if (!NetworkManager.Singleton.IsServer) return;
		Invoke(nameof(SetQuestServer), 6f);

		//SetQuestClientRpc(); // already on server, no need for server RPC
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	// called from KillableObject.OnNetworkDespawn on SERVER only
	public void RemoveCount()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		countToReach--;
		if (countToReach <= 0 && isActive)
		{
			isActive = false;
			FinishQuestServer();
		}
	}

	// ---------------- SERVER SIDE ----------------

	private void SetQuestServer()
	{
		// enable tree-cutting for everyone
		ToggleCutTreesClientRpc(true);

		// update quest text on all clients
		SetQuestClientRpc();
	}

	private void FinishQuestServer()
	{
		// stop cutting trees for everyone
		ToggleCutTreesClientRpc(false);

		// UI + dialogue, etc. on all clients
		FinishQuestClientRpc();

		// this object is probably server-only bookkeeping, despawn/destroy on server
		if (TryGetComponent(out NetworkObject nwo) && nwo.IsSpawned)
			nwo.Despawn(true);
		else
			Destroy(gameObject);
	}

	// ---------------- CLIENT RPCs ----------------

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void ToggleCutTreesClientRpc(bool canCut)
	{
		var bow = FindFirstObjectByType<BowScript>();
		if (bow != null)
			bow.arrowCutsTrees = canCut;
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetQuestClientRpc()
	{
		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.SetMainQuest(questDescription);
				break;
			case QuestType.SideQuest:
				QuestManager.instance.SetSideQuest(questDescription);
				break;
		}
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void FinishQuestClientRpc()
	{
		if (!actionToEnable.IsNullOrEmpty())
			DialogueManager.instance.SetActive(actionToEnable, true);

		GameManager.instance.AddXp(xpGain);

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.FinishMainQuest();
				break;
			case QuestType.SideQuest:
				QuestManager.instance.FinishSideQuest();
				break;
		}
	}
}
