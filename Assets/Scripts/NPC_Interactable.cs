using UnityEngine;
using System.Linq;
using System.Collections;
using System.Net.NetworkInformation;
using System;

public class NPC_Interactable : MonoBehaviour, IInteractable
{
	public ScriptableObject_NPC npcBase;

	public ScriptableObject_NPC_Dialogue nextDialogue;
	public bool canInteract { get; set; } = true;

	private int maxInteractions;
	private int currentInteractions;

	private float maxTime;
	private float currentTime;

	private bool dialogueEventActive = false;

	public event Action OnTalkEnded;

	/// <summary>
	/// Hey lille luder, ændrer den her path for at finde dine resources tingenoter
	/// hvor dine små klamme bøsse dialoger er :DDD
	/// var lig ved at skriv n-ordet
	/// </summary>

	private ScriptableObject_NPC_Dialogue[] allDialogues;
	private string dialoguePath = "ScriptableObjects/NPC_Dialogue";
	private void Awake()
	{
		allDialogues = Resources.LoadAll<ScriptableObject_NPC_Dialogue>(dialoguePath);
	}
	private void Start()
	{
		if (nextDialogue.dialogueEvent_enabled && nextDialogue.dialogueEvent_OnStart && !dialogueEventActive)
			ApplyDialogueEvent(nextDialogue.dialogueEvent);
	}
	public void Interact()
	{
		StartTalk();
	}
	public void ChangeDialogue(ScriptableObject_NPC_Dialogue d)
	{
		nextDialogue = d;

		if (nextDialogue.dialogueEvent_enabled && !nextDialogue.dialogueEvent_OnStart && !dialogueEventActive)
			ApplyDialogueEvent(nextDialogue.dialogueEvent);
	}
	private ScriptableObject_NPC_Dialogue savedDialogueEvent;
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

		savedDialogueEvent = nextDialogue.dialogueEvent_continuedDialogue;
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
		if (nextDialogue.continuedDialogue != null)
			ChangeDialogue(nextDialogue.continuedDialogue);
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
					ChangeDialogue(savedDialogueEvent);
					dialogueEventActive = false;
				}
			}
			else if (maxInteractions >= 1)
			{
				currentInteractions++;

				if (maxInteractions <= currentInteractions)
				{
					ChangeDialogue(savedDialogueEvent);
					dialogueEventActive = false;
				}
			}
		}
	}
	private void StopTalk()
	{
		InteractionHandler.singleton.ExitInteraction_NPC();
		NPC_Canvas.singleton.DeactivateCanvas();

		OnTalkEnded?.Invoke();
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
