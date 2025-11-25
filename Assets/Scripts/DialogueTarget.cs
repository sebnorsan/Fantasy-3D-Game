using Unity.Netcode;
using UnityEngine;

public class DialogueTarget : MonoBehaviour
{
	[Tooltip("Unique key this object responds to.")]
	public string targetID;
	[SerializeField] private bool teleportToPlayer = false;
	private void Awake()
	{
		OnRegister();
		gameObject.SetActive(false);
	}
	private void OnEnable()
	{
		if (teleportToPlayer)
			Invoke(nameof(Teleport), .1f);
	}
	private void OnDisable()
	{
		if (teleportToPlayer)
			CancelInvoke(nameof(Teleport));
	}
	private void Teleport()
	{
		if (!NetworkManager.Singleton.IsServer) return;

		if (teleportToPlayer)
			transform.position = FindFirstObjectByType<PlayerController>().transform.position;
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