using System;
using UnityEngine;

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

	public ScriptableObject_NPC_Dialogue continuedDialogue;

	[Space(15)]

	public bool dialogueEvent_enabled = false;

	public bool dialogueEvent_OnStart = false;

	[Space(15)]

	public DialogueEvent dialogueEvent;

	public float dialogueEvent_timeEvent = 0;
	public int dialogueEvent_interactionsEvent = 0;

	[Space(15)]

	public ScriptableObject_NPC_Dialogue dialogueEvent_continuedDialogue;

	[Space(10)]

	public DialogueAction[] actions;
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
[Serializable]
public class DialogueAction
{
	public enum ActionType
	{
		EnableObject,
		DisableObject,
	}

	public ActionType actionType;

	[Tooltip("Must match a DialogueTarget.targetID in the scene")]
	public string targetID;
}
