using UnityEngine;
using UnityEditor;

// 1) Target the SO type
[CustomEditor(typeof(ScriptableObject_NPC_Dialogue))]
public class ScriptableObjectNPCDialogueEditor : Editor
{
	// 2) SerializedProperties for every field
	SerializedProperty dialogueIdProp;
	SerializedProperty npcVoiceLineProp;
	SerializedProperty imageOverrideProp;
	SerializedProperty dialogueProp;
	SerializedProperty affectedWordsProp;
	SerializedProperty continuedDialogueIdProp;

	SerializedProperty evtEnabledProp;
	SerializedProperty evtOnStartProp;
	SerializedProperty evtTypeProp;
	SerializedProperty evtTimeProp;
	SerializedProperty evtInteractionsProp;
	SerializedProperty evtContinuedDialogueProp;

	void OnEnable()
	{
		// link properties by name
		dialogueIdProp = serializedObject.FindProperty("dialogueId");                            // :contentReference[oaicite:0]{index=0}
		npcVoiceLineProp = serializedObject.FindProperty("npcVoiceLine");
		imageOverrideProp = serializedObject.FindProperty("imageOverride");
		dialogueProp = serializedObject.FindProperty("dialogue");
		affectedWordsProp = serializedObject.FindProperty("affectedWords");
		continuedDialogueIdProp = serializedObject.FindProperty("continuedDialogueId");

		evtEnabledProp = serializedObject.FindProperty("dialogueEvent_enabled");
		evtOnStartProp = serializedObject.FindProperty("dialogueEvent_OnStart");
		evtTypeProp = serializedObject.FindProperty("dialogueEvent");
		evtTimeProp = serializedObject.FindProperty("dialogueEvent_timeEvent");
		evtInteractionsProp = serializedObject.FindProperty("dialogueEvent_interactionsEvent");
		evtContinuedDialogueProp = serializedObject.FindProperty("dialogueEvent_continuedDialogue");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();                                                                         // :contentReference[oaicite:1]{index=1}

		// draw the always-visible fields
		EditorGUILayout.PropertyField(dialogueIdProp);
		EditorGUILayout.PropertyField(npcVoiceLineProp);
		EditorGUILayout.PropertyField(imageOverrideProp);
		EditorGUILayout.PropertyField(dialogueProp);
		EditorGUILayout.PropertyField(affectedWordsProp, true);
		EditorGUILayout.PropertyField(continuedDialogueIdProp);

		// draw the master toggle
		EditorGUILayout.PropertyField(evtEnabledProp);
		if (evtEnabledProp.boolValue)                                                                      // :contentReference[oaicite:2]{index=2}
		{
			// once enabled, show OnStart and enum
			EditorGUILayout.PropertyField(evtOnStartProp);
			EditorGUILayout.PropertyField(evtTypeProp);

			// only show matching sub-field for the selected enum
			var mode = (DialogueEvent)evtTypeProp.enumValueIndex;
			switch (mode)                                                                                  // :contentReference[oaicite:3]{index=3}
			{
				case DialogueEvent.AfterSomeTime:
					EditorGUILayout.PropertyField(evtTimeProp, new GUIContent("Time Delay"));
					EditorGUILayout.PropertyField(evtContinuedDialogueProp);
					break;

				case DialogueEvent.AfterSomeInteractions:
					EditorGUILayout.PropertyField(evtInteractionsProp, new GUIContent("Max Interactions"));
					EditorGUILayout.PropertyField(evtContinuedDialogueProp);
					break;

				case DialogueEvent.AfterTalkNullify:
					// no extra fields
					break;
			}
		}

		serializedObject.ApplyModifiedProperties();                                                        // :contentReference[oaicite:4]{index=4}
	}
}
