using Unity.Netcode;
using UnityEngine;

public class QuestObjectCounter : MonoBehaviour
{
	public int countToReach;
	[TextArea]
	public string questDescription;
	public bool isActive = true;


	public QuestObject[] objectives;

	private void OnEnable()
	{
		Invoke(nameof(Function), 1f);
	}
	private void OnDisable()
	{
		CancelInvoke();
	}
	private void Function()
	{
		if (!NetworkManager.Singleton.IsServer) return; // only server does quest + spawn

		if (countToReach <= 0)
			countToReach = objectives.Length;

		isActive = false;

		// Make sure all children are spawned before any RPCs:
		var networkObjects = GetComponentsInChildren<NetworkObject>();
		foreach (var nwo in networkObjects)
		{
			if (!nwo.IsSpawned)
				nwo.Spawn();
		}

		objectives[0].SetQuest();
		isActive = true;
	}
	public void RemoveCount()
	{
		countToReach--;
		if (countToReach <= 1)
			isActive = false;
	}
}
