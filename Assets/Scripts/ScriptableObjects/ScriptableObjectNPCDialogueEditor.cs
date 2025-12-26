#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// 1) Target the SO type
[CustomEditor(typeof(ScriptableObject_NPC_Dialogue))]
public class ScriptableObjectNPCDialogueEditor : Editor
{
	SerializedProperty dialogueIdProp;
	SerializedProperty npcVoiceLineProp;
	SerializedProperty imageOverrideProp;
	SerializedProperty dialogueProp;
	SerializedProperty affectedWordsProp;
	SerializedProperty continuedDialogueProp;

	SerializedProperty evtEnabledProp;
	SerializedProperty evtOnStartProp;
	SerializedProperty evtTypeProp;
	SerializedProperty evtTimeProp;
	SerializedProperty evtInteractionsProp;
	SerializedProperty evtContinuedDialogueProp;

	SerializedProperty actionsProp;

	void OnEnable()
	{
		dialogueIdProp = serializedObject.FindProperty("dialogueId");
		npcVoiceLineProp = serializedObject.FindProperty("npcVoiceLine");
		imageOverrideProp = serializedObject.FindProperty("imageOverride");
		dialogueProp = serializedObject.FindProperty("dialogue");
		affectedWordsProp = serializedObject.FindProperty("affectedWords");
		continuedDialogueProp = serializedObject.FindProperty("continuedDialogue");

		evtEnabledProp = serializedObject.FindProperty("dialogueEvent_enabled");
		evtOnStartProp = serializedObject.FindProperty("dialogueEvent_OnStart");
		evtTypeProp = serializedObject.FindProperty("dialogueEvent");
		evtTimeProp = serializedObject.FindProperty("dialogueEvent_timeEvent");
		evtInteractionsProp = serializedObject.FindProperty("dialogueEvent_interactionsEvent");
		evtContinuedDialogueProp = serializedObject.FindProperty("dialogueEvent_continuedDialogue");

		actionsProp = serializedObject.FindProperty("actions");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		if (dialogueIdProp != null) EditorGUILayout.PropertyField(dialogueIdProp);
		if (npcVoiceLineProp != null) EditorGUILayout.PropertyField(npcVoiceLineProp);
		if (imageOverrideProp != null) EditorGUILayout.PropertyField(imageOverrideProp);
		if (dialogueProp != null) EditorGUILayout.PropertyField(dialogueProp);
		if (affectedWordsProp != null) EditorGUILayout.PropertyField(affectedWordsProp, true);
		if (continuedDialogueProp != null) EditorGUILayout.PropertyField(continuedDialogueProp, new GUIContent("Continued Dialogue"));

		if (evtEnabledProp != null)
		{
			EditorGUILayout.PropertyField(evtEnabledProp);
			if (evtEnabledProp.boolValue)
			{
				if (evtOnStartProp != null) EditorGUILayout.PropertyField(evtOnStartProp);
				if (evtTypeProp != null) EditorGUILayout.PropertyField(evtTypeProp);

				if (evtTypeProp != null && evtContinuedDialogueProp != null)
				{
					var mode = (DialogueEvent)evtTypeProp.enumValueIndex;
					switch (mode)
					{
						case DialogueEvent.AfterSomeTime:
							if (evtTimeProp != null) EditorGUILayout.PropertyField(evtTimeProp, new GUIContent("Time Delay"));
							EditorGUILayout.PropertyField(evtContinuedDialogueProp, new GUIContent("Post-Event Dialogue"));
							break;
						case DialogueEvent.AfterSomeInteractions:
							if (evtInteractionsProp != null) EditorGUILayout.PropertyField(evtInteractionsProp, new GUIContent("Max Interactions"));
							EditorGUILayout.PropertyField(evtContinuedDialogueProp, new GUIContent("Post-Event Dialogue"));
							break;
						case DialogueEvent.AfterTalkNullify:
							break;
					}
				}
			}
		}

		EditorGUILayout.Space(10);
		if (actionsProp != null) EditorGUILayout.PropertyField(actionsProp, true);

		serializedObject.ApplyModifiedProperties();
	}
}
#endif