using UnityEngine;

[DisallowMultipleComponent]
public class DialogueTarget : MonoBehaviour
{
	[Tooltip("Unique key this object responds to.")]
	public string targetID;

	private void Awake()
	{
		OnRegister();
		gameObject.SetActive(false);
	}
	private void OnDestroy()
	{
		OnUnregister();
	}
	private void OnRegister()
	{
		DialogueManager.Register(targetID, gameObject);
	}

	private void OnUnregister()
	{
		DialogueManager.Unregister(targetID);
	}
}