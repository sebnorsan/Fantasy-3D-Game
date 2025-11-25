using EasyTextEffects.Editor.MyBoxCopy.Extensions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.iOS;
using UnityEngine.UIElements;

public class QuestObject : NetworkBehaviour
{
	[TextArea]
	[SerializeField] private string questDesc;
    public enum QuestType
    {
        MainQuest,
        SideQuest
    }
    public QuestType questType;
	[SerializeField] private bool reenableKingQuest;
	[SerializeField] private bool setKingSideQuest;
	[SerializeField] ScriptableObject_NPC_Dialogue dia;

	public int xpGain = 100;

	public string actionToEnable = "";

	public QuestObjectCounter counter;
	public bool questFinished;

	public override void OnNetworkSpawn()
	{
		//if (!NetworkManager.Singleton.IsServer) return;
		//SetQuestClientRpc(); // already on server, no need for server RPC
	}
	private void OnEnable()
	{
		if (!NetworkManager.Singleton.IsServer) return;
		Invoke(nameof(SetQuestClientRpc), 6f);
		//SetQuestClientRpc(); // already on server, no need for server RPC
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	public void FinishQuest()
	{
		FinishQuestServerRpc();
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void FinishQuestServerRpc()
	{
		if (questFinished) return;
		questFinished = true;

		// only server needs the counter
		if (counter != null && counter.isActive)
		{
			counter.RemoveCount();
			return;
		}

		AddXpClientRpc(xpGain);
		// now broadcast result
		FinishQuestClientRpc();
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	public void AddXpClientRpc(int xp)
	{
		GameManager.instance.AddXp(xp);
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void FinishQuestClientRpc()
	{
		// NO counter usage here anymore

		if (!actionToEnable.IsNullOrEmpty())
			DialogueManager.instance.SetActive(actionToEnable, true);

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.FinishMainQuest();
				break;
			case QuestType.SideQuest:
				QuestManager.instance.FinishSideQuest();
				break;
		}

		if (setKingSideQuest)
			FindFirstObjectByType<NPC_Interactable>().gameObject
				.GetComponent<QuestObject>().questType = QuestType.SideQuest;

		if (reenableKingQuest)
			FindFirstObjectByType<NPC_Interactable>().KingDialogue(dia);
	}

	public void SetQuest()
	{
		if (NetworkManager.Singleton.IsServer)
			SetQuestClientRpc();        // server -> everyone
		else
			SetQuestServerRpc();        // client -> server
	}
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetQuestServerRpc()
	{
		SetQuestClientRpc();
	}
	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetQuestClientRpc()
	{
		questFinished = false;

		var questCounter = counter;
		if (questCounter != null && questCounter.isActive)
			return;

		switch (questType)
		{
			case QuestType.MainQuest:
				QuestManager.instance.SetMainQuest(questDesc);
				break;
			case QuestType.SideQuest:
				QuestManager.instance.SetSideQuest(questDesc);
				break;
		}
	}
}
