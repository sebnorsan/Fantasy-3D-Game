using System;
using UnityEngine;
using static NPC_Interactable;

[CreateAssetMenu(fileName = "Dialogue", menuName = "NPC/New Dialogue")]
public class ScriptableObject_NPC_Dialogue : ScriptableObject
{
    public string dialogueId;


	public AudioToPlay npcVoiceLine;
	public Sprite imageOverride;

	[TextArea]
    public string dialogue;
	public WordEffect[] affectedWords;

	[Space(15)]

	public string continuedDialogueId;

	[Space(15)]

	public bool dialogueEvent_enabled = false;

	public bool dialogueEvent_OnStart = false;

	[Space(15)]

	public DialogueEvent dialogueEvent;

	public float dialogueEvent_timeEvent = 0;
	public int dialogueEvent_interactionsEvent = 0;

	[Space(15)]

	public string dialogueEvent_continuedDialogue;
}
[Serializable]
public class WordEffect
{
	public string wordAffected = "";
	public TextType wordEffectType = TextType.Normal;
}
public enum DialogueEvent
{
	AfterSomeTime,
	AfterSomeInteractions,
	AfterTalkNullify
}
public enum TextType
{
	Normal,
	Aggressive,
	Whisper
}
