using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTarget : MonoBehaviour
{
	[Tooltip("Unique key this object responds to.")]
	public string targetID;

	private void OnEnable()
	{
		DialogueManager.Register(targetID, gameObject);
	}

	private void OnDisable()
	{
		DialogueManager.Unregister(targetID);
	}
}