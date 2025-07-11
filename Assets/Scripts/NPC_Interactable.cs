using UnityEngine;
using System.Collections;

public class NPC_Interactable : MonoBehaviour, IInteractable
{
	public ScriptableObject_NPC npcBase;

	public ScriptableObject_NPC_Dialogue nextDialogue;

	public ScriptableObject_NPC_Dialogue[] availableCharacterDialogues;
	public bool canInteract { get; set; }

	private int maxInteractions;
	private int currentInteractions;

	private float maxTime;
	private float currentTime;

	private bool dialogueEventActive = false;

	private void Start()
	{
		if (nextDialogue.dialogueEvent_enabled && nextDialogue.dialogueEvent_OnStart && !dialogueEventActive)
			ApplyDialogueEvent(nextDialogue.dialogueEvent);
	}
	public void Interact()
	{
		StartTalk();
	}
	public ScriptableObject_NPC_Dialogue GetDialogue(string id)
	{
		foreach (var dialogue in availableCharacterDialogues)
			if (dialogue.dialogueId == id)
				return dialogue;

		return null;
	}
	public void ChangeDialogue(string id)
	{
		foreach (var dialogue in availableCharacterDialogues)
			if (dialogue.dialogueId == id)
				nextDialogue = dialogue;

		if (nextDialogue.dialogueEvent_enabled && !nextDialogue.dialogueEvent_OnStart && !dialogueEventActive)
			ApplyDialogueEvent(nextDialogue.dialogueEvent);
	}
	private string savedDialogueEventId;
	private void ApplyDialogueEvent(DialogueEvent tempEvent)
	{
		switch (tempEvent)
		{
			case DialogueEvent.AfterSomeTime:
				maxTime = nextDialogue.dialogueEvent_timeEvent;
				StartTimer();
				break;
			case DialogueEvent.AfterSomeInteractions:
				maxInteractions = nextDialogue.dialogueEvent_interactionsEvent;
				currentInteractions = 0;
				break;
			case DialogueEvent.AfterTalkNullify:
				canInteract = false;
				break;
			default:
				break;
		}

		savedDialogueEventId = nextDialogue.dialogueEvent_continuedDialogue;
		dialogueEventActive = true;
	}

	private AudioClip currentlyPlayingAudio = null;
	private bool exitOnFinish;

	public void ContinueTalk()
	{
		if (currentlyPlayingAudio != null)
		{
			EventManager.instance.StopThisSound(currentlyPlayingAudio);
			currentlyPlayingAudio = null;
		}

		if (exitOnFinish)
		{
			StopTalk();
			return;
		}

		NPC_Canvas.singleton.SetDialogue(npcBase.npcName, nextDialogue.dialogue, nextDialogue.affectedWords);

		Sprite picToEnable;

		if (nextDialogue.imageOverride != null)
			picToEnable = nextDialogue.imageOverride;
		else
			picToEnable = npcBase.npcPic;

		NPC_Canvas.singleton.SetNpcPicture(picToEnable);

		if (nextDialogue.npcVoiceLine.audioToPlay != null)
		{
			EventManager.instance.PlayThisSound(nextDialogue.npcVoiceLine);
			currentlyPlayingAudio = nextDialogue.npcVoiceLine.audioToPlay;
		}
		if (GetDialogue(nextDialogue.continuedDialogueId) != null)
			ChangeDialogue(nextDialogue.continuedDialogueId);
		else
			exitOnFinish = true;
	}	
	
	private void StartTalk()
	{
		if (!canInteract)
			return;

		InteractionHandler.singleton.EnterInteraction_NPC(this);

		exitOnFinish = false;
		CheckDialogueEvents();

		if (nextDialogue.dialogueEvent_enabled && !nextDialogue.dialogueEvent_OnStart && !dialogueEventActive)
			ApplyDialogueEvent(nextDialogue.dialogueEvent);

		ContinueTalk();

		NPC_Canvas.singleton.ActivateCanvas();
	}
	private void CheckDialogueEvents()
	{
		if (dialogueEventActive)
		{
			if (maxTime >= 1)
			{
				if (isTimerDone)
				{
					ChangeDialogue(savedDialogueEventId);
					dialogueEventActive = false;
				}
			}
			else if (maxInteractions >= 1)
			{
				currentInteractions++;

				if (maxInteractions <= currentInteractions)
				{
					ChangeDialogue(savedDialogueEventId);
					dialogueEventActive = false;
				}
			}
		}
	}
	private void StopTalk()
	{
		InteractionHandler.singleton.ExitInteraction_NPC();
		NPC_Canvas.singleton.DeactivateCanvas();
	}

	private void StartTimer()
	{
		currentTime = 0;
		StartCoroutine(Timer());
	}
	private bool isTimerDone;
	private IEnumerator Timer()
	{
		while (maxTime >= currentTime)
		{
			currentTime += Time.deltaTime;
			yield return null;
		}
		isTimerDone = true;
	}
}
