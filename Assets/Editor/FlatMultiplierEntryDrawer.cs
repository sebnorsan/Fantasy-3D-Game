using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(FlatMultiplierEntry))]
public class FlatMultiplierEntryDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var typeProp = property.FindPropertyRelative("type");
		var valueProp = property.FindPropertyRelative("value");

		string enumName = typeProp.enumDisplayNames[typeProp.enumValueIndex];

		Rect leftRect = new Rect(position.x, position.y, position.width * 0.65f, EditorGUIUtility.singleLineHeight);
		Rect rightRect = new Rect(position.x + position.width * 0.67f, position.y, position.width * 0.33f, EditorGUIUtility.singleLineHeight);

		EditorGUI.LabelField(leftRect, enumName);
		valueProp.floatValue = EditorGUI.FloatField(rightRect, valueProp.floatValue);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}
}