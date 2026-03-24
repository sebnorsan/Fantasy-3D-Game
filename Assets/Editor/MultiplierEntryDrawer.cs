using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MultiplierEntry))]
public class MultiplierEntryDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		var typeProp = property.FindPropertyRelative("type");
		var valueProp = property.FindPropertyRelative("value");

		string enumName = typeProp.enumDisplayNames[typeProp.enumValueIndex];

		EditorGUI.BeginProperty(position, label, property);

		Rect left = new Rect(position.x, position.y, position.width * 0.6f, position.height);
		Rect right = new Rect(position.x + position.width * 0.62f, position.y, position.width * 0.38f, position.height);

		EditorGUI.LabelField(left, enumName);
		valueProp.floatValue = EditorGUI.FloatField(right, valueProp.floatValue);

		EditorGUI.EndProperty();
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		return EditorGUIUtility.singleLineHeight;
	}
}