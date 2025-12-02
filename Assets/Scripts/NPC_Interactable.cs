using UnityEngine;
using System.Collections;
using System;
using Unity.Netcode;

public class NPC_Interactable : NetworkBehaviour, IInteractable
{
	public ScriptableObject_NPC npcBase;

	public ScriptableObject_NPC_Dialogue nextDialogue;
	public bool canInteract { get; set; } = true;

	[Header("Dialogue Library (same on all clients)")]
	[SerializeField] private ScriptableObject_NPC_Dialogue[] dialogueLibrary;

	// synced state: server writes, everyone reads
	private NetworkVariable<int> currentDialogueId = new(
		-1,
		NetworkVariableReadPermission.Everyone,
		NetworkVariableWritePermission.Server
	);

	public event Action OnTalkEnded;

	public bool isTalkingToPlayer = false;

	private InteractionHandler interactionHandler;

	private void Start()
	{
		// only server sets initial state
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
		{
			int id = FindDialogueId(nextDialogue);
			if (id >= 0)
				currentDialogueId.Value = id;
		}
	}

	public override void OnNetworkSpawn()
	{
		currentDialogueId.OnValueChanged += OnDialogueIdChanged;
		ApplyDialogueLocally(currentDialogueId.Value);
	}

	public override void OnNetworkDespawn()
	{
		currentDialogueId.OnValueChanged -= OnDialogueIdChanged;
	}

	private void OnDialogueIdChanged(int oldId, int newId)
	{
		ApplyDialogueLocally(newId);
	}

	private void ApplyDialogueLocally(int id)
	{
		if (id < 0 || dialogueLibrary == null || id >= dialogueLibrary.Length) return;
		var dlg = dialogueLibrary[id];
		if (dlg == null) return;

		// local set
		nextDialogue = dlg;
	}

	private int FindDialogueId(ScriptableObject_NPC_Dialogue d)
	{
		if (d == null || dialogueLibrary == null) return -1;
		for (int i = 0; i < dialogueLibrary.Length; i++)
			if (dialogueLibrary[i] == d) return i;
		return -1;
	}

	private int FindDialogueIdByName(string dialogueName)
	{
		if (string.IsNullOrEmpty(dialogueName) || dialogueLibrary == null) return -1;

		for (int i = 0; i < dialogueLibrary.Length; i++)
		{
			var dlg = dialogueLibrary[i];
			if (dlg != null && dlg.name == dialogueName)
				return i;
		}
		return -1;
	}

	public void SetInteractionHandler(InteractionHandler iHandler)
	{
		interactionHandler = iHandler;
	}

	public void InteractRemotely()
	{
		if (!canInteract)
			return;

		StartTalk();
	}

	public void Interact()
	{
		if (!canInteract || isTalkingToPlayer)
			return;

		StartTalk();
	}

	public void ChangeDialogue(ScriptableObject_NPC_Dialogue d)
	{
		nextDialogue = d;

		// keep synced state updated when server changes dialogue
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
		{
			int id = FindDialogueId(d);
			if (id >= 0)
			{
				currentDialogueId.Value = id;
			}
		}
	}


	private AudioClip currentlyPlayingAudio = null;
	private bool exitOnFinish;
	private ScriptableObject_NPC_Dialogue prevDialogue;

	public void ContinueTalk()
	{
		if (prevDialogue != null)
		{
			//DoDialogueActions(prevDialogue);
			//if (!prevDialogue.dialogueEvent_OnStart)
			//	ApplyDialogueEvent(prevDialogue);
		}

		if (currentlyPlayingAudio != null)
		{
			AudioManagement.instance.StopThisSound(currentlyPlayingAudio);
			currentlyPlayingAudio = null;
		}

		if (exitOnFinish)
		{
			StopTalk();
			return;
		}

		prevDialogue = nextDialogue;

		NPC_Canvas.singleton.SetDialogue(npcBase.npcName, nextDialogue.dialogue, nextDialogue.affectedWords);

		Sprite picToEnable = nextDialogue.imageOverride != null ? nextDialogue.imageOverride : npcBase.npcPic;
		NPC_Canvas.singleton.SetNpcPicture(picToEnable);

		if (nextDialogue.npcVoiceLine.audioToPlay != null)
		{
			AudioManagement.instance.PlayThisSound(nextDialogue.npcVoiceLine);
			currentlyPlayingAudio = nextDialogue.npcVoiceLine.audioToPlay;
		}

		if (nextDialogue.continuedDialogue != null)
			ChangeDialogue(nextDialogue.continuedDialogue);
		else
			exitOnFinish = true;
	}

	private void StartTalk()
	{
		SetNPCToTalkingServerRpc(true);

		if (interactionHandler != null)
			interactionHandler.EnterInteraction_NPC(this);

		prevDialogue = null;
		exitOnFinish = false;
		//CheckDialogueEvents();
		ContinueTalk();

		NPC_Canvas.singleton.ActivateCanvas();
	}

	private void StopTalk()
	{
		SetNPCToTalkingServerRpc(false);

		if (interactionHandler != null)
			interactionHandler.ExitInteraction_NPC();

		NPC_Canvas.singleton.DeactivateCanvas();
		OnTalkEnded?.Invoke();
	}


	//private void DoDialogueActions(ScriptableObject_NPC_Dialogue dlg)
	//{
	//	foreach (var a in dlg.actions)
	//	{
	//		switch (a.actionType)
	//		{
	//			case DialogueAction.ActionType.EnableObject:
	//				DialogueManager.instance.SetActive(a.targetID, true);
	//				break;
	//			case DialogueAction.ActionType.DisableObject:
	//				DialogueManager.instance.SetActive(a.targetID, false);
	//				break;
	//			case DialogueAction.ActionType.FinishQuest:
	//				GetComponent<QuestObject>().FinishQuest();
	//				break;
	//		}
	//	}
	//}

	// ---------------- KING DIALOGUE (name-based) ----------------
	public void KingDialogue(ScriptableObject_NPC_Dialogue d)
	{
		if (d == null) return;

		string dialogueName = d.name;

		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
			StartKingSequenceServer(dialogueName);
		else
			KingDialogueServerRpc(dialogueName);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void KingDialogueServerRpc(string dialogueName)
	{
		StartKingSequenceServer(dialogueName);
	}

	private void StartKingSequenceServer(string dialogueName)
	{
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

		int id = FindDialogueIdByName(dialogueName);
		if (id < 0)
		{
			Debug.LogWarning($"[KingDialogue] No dialogue in library named '{dialogueName}'");
			return;
		}

		currentDialogueId.Value = id;
		nextDialogue = dialogueLibrary[id];

		StartCoroutine(KingQuestReUpdateServer(id));
	}

	private IEnumerator KingQuestReUpdateServer(int dialogueId)
	{
		yield return new WaitForSeconds(4f);

		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
			yield break;

		//var quest = GetComponent<QuestObject>();
		//if (quest != null) quest.SetQuest();

		// ensure final state is still correct
		currentDialogueId.Value = dialogueId;

		ChangeDialogue(dialogueLibrary[dialogueId]);
	}

	// ---------------- TALKING FLAG ----------------
	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void SetNPCToTalkingServerRpc(bool isTalking)
	{
		isTalkingToPlayer = isTalking;
		SetNPCToTalkingClientRpc(isTalking);
	}

	[Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Server)]
	private void SetNPCToTalkingClientRpc(bool isTalking)
	{
		isTalkingToPlayer = isTalking;
	}
}
